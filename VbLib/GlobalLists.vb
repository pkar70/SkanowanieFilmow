
' właściwie jako 'module', ale chcę init z DataFolder


Imports pkar

Public Module Globs

    Private _folder As String
    Private Const APP_NAME As String = "PicSorter"

    Public Sub Init(folder As String)
        Dim appname As String = GetSettingsString("name", APP_NAME)
        _folder = IO.Path.Combine(folder, appname)

        ' aktualizacja Stage.Requirs
        StageReadRequir()


    End Sub

    ''' <summary>
    ''' zwraca ścieżkę do plików konfiguracyjnych
    ''' </summary>
    Public Function GetDataFolder(Optional bThrowNotExist As Boolean = True) As String

        If Not bThrowNotExist Then Return _folder

        If IO.Directory.Exists(_folder) Then Return _folder

        Throw New ArgumentException("Folder for configs doesnt exist")
    End Function

    Public Function GetDataFolder(sSubfolder As String, Optional bThrowNotExist As Boolean = True) As String
        Dim sFolder As String = GetDataFolder(bThrowNotExist)

        If sSubfolder = "" Then Return sFolder

        sFolder = IO.Path.Combine(sFolder, sSubfolder)
        If Not IO.Directory.Exists(sFolder) Then IO.Directory.CreateDirectory(sFolder)

        Return sFolder

    End Function


    ''' <summary>
    ''' podaj sciezke do pliku, w podanym podkatalogu
    ''' </summary>
    ''' <param name="sSubfolder">podkatalog w DataRoot</param>
    ''' <param name="sFilename">nazwa pliku</param>
    ''' <param name="bThrowNotExist">czy throw gdy nie ma DataRoot</param>
    ''' <returns></returns>
    Public Function GetDataFile(sSubfolder As String, sFilename As String, Optional bThrowNotExist As Boolean = True) As String
        Dim sFolder As String = GetDataFolder(sSubfolder, bThrowNotExist)

        Dim sFile As String = IO.Path.Combine(sFolder, sFilename)
        Return sFile

    End Function

    ''' <summary>
    ''' Usuwa pliki z katalogu tymczasowego, które są starsze niż maxHours
    ''' </summary>
    ''' <returns>Liczba usuniętych plików</returns>
    Public Function TempDirPrepare(maxHours As Integer) As Integer
        DumpCurrMethod()

        Dim limitDate As Date = Date.Now.AddHours(-maxHours)
        Dim path As String = TempDirGetPath()
        Dim iCnt As Integer = 0
        For Each sFile As String In IO.Directory.GetFiles(path)
            If IO.File.GetCreationTime(sFile) < limitDate Then
                IO.File.Delete(sFile)
                iCnt += 1
            End If
        Next

        DumpMessage($"Usunąłem {iCnt} plików")
        Return iCnt
    End Function

    Public Function TempDirGetPath() As String
        Dim katalog As String = IO.Path.Combine(IO.Path.GetTempPath, APP_NAME)
        IO.Directory.CreateDirectory(katalog)
        Return katalog
    End Function

    ''' <summary>
    ''' zwraca pathname pliku tymczasowego, dokładność jedna setna sekundy
    ''' </summary>
    Public Function TempDirCreateTempFilename() As String
        Dim basename As String = Date.Now.ToString("dd-HH-mm-ss")
        Dim filename As String = IO.Path.Combine(TempDirGetPath, basename & ".tmp")
        If Not IO.File.Exists(filename) Then Return filename
        basename = Date.Now.ToString("dd-HH-mm-ss-f")
        filename = IO.Path.Combine(TempDirGetPath, basename & "tmp")
        If Not IO.File.Exists(filename) Then Return filename
        basename = Date.Now.ToString("dd-HH-mm-ss-ff")
        filename = IO.Path.Combine(TempDirGetPath, basename & "tmp")
        If Not IO.File.Exists(filename) Then Return filename
        Return ""
    End Function

    Private gBuffer As Vblib.BufferSortowania
    Public Function GetBuffer() As Vblib.BufferSortowania
        If gBuffer Is Nothing Then
            ' w tym NEW jest LOAD oraz RecalcSumsy, w tym GEO z # do EXIF w metadanych!
            gBuffer = New Vblib.BufferSortowania(GetDataFolder)
        End If
        Return gBuffer
    End Function


    Private gKeywords As Vblib.KeywordsList

    Public Function GetKeywords() As Vblib.KeywordsList
        If gKeywords Is Nothing Then
            gKeywords = New Vblib.KeywordsList(GetDataFolder)
            gKeywords.Load()
        End If
        Return gKeywords
    End Function


    Private gDirtree As Vblib.DirsList

    Public Function GetDirTree() As Vblib.DirsList
        If gDirtree Is Nothing Then
            gDirtree = New Vblib.DirsList(GetDataFolder)
            gDirtree.Load()
        End If
        Return gDirtree
    End Function



    Private gQueries As BaseList(Of SearchQuery)

    Public Function GetQueries() As BaseList(Of SearchQuery)
        If gQueries IsNot Nothing Then Return gQueries
        gQueries = New BaseList(Of SearchQuery)(GetDataFolder, "queries.json")
        gQueries.Load()
        Return gQueries
    End Function


    Private gShareChannels As ShareChannelsList

    Public Function GetShareChannels() As ShareChannelsList
        If gShareChannels IsNot Nothing Then Return gShareChannels

        gShareChannels = New ShareChannelsList(GetDataFolder, GetQueries)
        gShareChannels.Load()
        Return gShareChannels
    End Function

    Private gShareLogins As ShareLoginsList

    Public Function GetShareLogins() As ShareLoginsList
        If gShareLogins IsNot Nothing Then Return gShareLogins

        gShareLogins = New ShareLoginsList(GetDataFolder, GetShareChannels)
        gShareLogins.Load()
        Return gShareLogins
    End Function

    Private gShareServers As ShareServerList

    Public Function GetShareServers() As ShareServerList
        If gShareServers IsNot Nothing Then Return gShareServers

        gShareServers = New ShareServerList(GetDataFolder)
        gShareServers.Load()
        Return gShareServers
    End Function

    Private gShareDescriptionsIn As ShareDescInList

    Public Function GetShareDescriptionsIn() As ShareDescInList
        If gShareDescriptionsIn IsNot Nothing Then Return gShareDescriptionsIn

        gShareDescriptionsIn = New ShareDescInList(GetDataFolder)
        gShareDescriptionsIn.Load()
        Return gShareDescriptionsIn
    End Function

    Private gShareDescriptionsOut As ShareDescOutList

    Public Function GetShareDescriptionsOut() As ShareDescOutList
        If gShareDescriptionsOut IsNot Nothing Then Return gShareDescriptionsOut

        gShareDescriptionsOut = New ShareDescOutList(GetDataFolder)
        gShareDescriptionsOut.Load()
        Return gShareDescriptionsOut
    End Function


    Private gArchQuerender As ArchiveIndex

    Public Function GetArchIndex() As ArchiveIndex
        If gArchQuerender Is Nothing Then
            gArchQuerender = New ArchiveIndex(GetDataFolder)
        End If
        Return gArchQuerender
    End Function


    Public gLastLoginSharing As New Vblib.ShareLoginData

    ' ustawiane w MainWindow
    Public gAutoTagery As Vblib.AutotaggerBase()
    Public Function GetTagger(nazwa As String) As Vblib.AutotaggerBase
        Return gAutoTagery.FirstOrDefault(Function(x) x.Nazwa = nazwa)
    End Function


#Region "cloudArchives"
    ' to tylko fragment, bo chodzi o to żeby były dostępne z vblib, ale read/write musi być z application
    Public LibgCloudArchives As List(Of Vblib.CloudArchive)

#End Region

#Region "SequenceCheckers"

    Public SequenceCheckers As SequenceStageBase() =
        {
        New SequenceStage_AutoExif,
    New SequenceStage_CropRotate,
    New SequenceStage_Keywords,
    New SequenceStage_Dates,
    New SequenceStage_Geotags,
    New SequenceStage_AutoTaggers,
    New SequenceStage_Descriptions,
    New SequenceStage_TargetDir,
    New SequenceStage_Publish,
    New SequenceStage_CloudArch,
    New SequenceStage_LocalArch
        }
#End Region

    Public Sub StageReadRequir()
        Dim requirs As String = GetSettingsString("uiListaSteps")

        For Each stage As SequenceStageBase In SequenceCheckers
            stage.IsRequired = requirs.Contains(stage.Nazwa & "|")
        Next

    End Sub




    Public gPostProcesory As Vblib.PostProcBase()

End Module

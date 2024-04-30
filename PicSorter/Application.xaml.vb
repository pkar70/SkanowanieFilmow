
Imports vb14 = Vblib.pkarlibmodule14
Imports pkar
Imports Vblib
Imports System.Reflection


Partial Class Application

    ' Application-level events, such as Startup, Exit, and DispatcherUnhandledException
    ' can be handled in this file.

#Disable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
    Public Async Function AppServiceLocalCommand(sCommand As String) As Task(Of String)
#Enable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
        Return ""
    End Function

    Public Shared Sub ShowWait(bShow As Boolean)
        If bShow Then
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait
        Else
            Mouse.OverrideCursor = Nothing
        End If
    End Sub


    Public Shared Function GetOneDrivePath() As String
        Dim sOneDrivePath As String = Environment.GetEnvironmentVariable("OneDriveConsumer")
        If sOneDrivePath Is Nothing Then Return ""

        If Not IO.Directory.Exists(sOneDrivePath) Then Return ""

        sOneDrivePath = IO.Path.Combine(sOneDrivePath, "Apps")
        If Not IO.Directory.Exists(sOneDrivePath) Then IO.Directory.CreateDirectory(sOneDrivePath)

        Dim appName As String = GetAppName()

        sOneDrivePath = IO.Path.Combine(sOneDrivePath, appName)
        If Not IO.Directory.Exists(sOneDrivePath) Then IO.Directory.CreateDirectory(sOneDrivePath)

        Return sOneDrivePath
    End Function

    Public Shared Function GetDataFolder(Optional bThrowNotExist As Boolean = True) As String

        ' próba czy mamy commandline zmieniające nazwę
        Dim appname As String = GetSettingsString("name")
        If String.IsNullOrEmpty(appname) Then
            appname = GetAppName()
            'Return GetAppDataFolder()
            'Else
            '    ' Narzucona nazwa instancji
            '    Return IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), appname)
        End If
        Return IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), appname)

        'Return GetAppDataFolder()
        'Dim sFolder As String = vb14.GetSettingsString("uiFolderData")
        'If bThrowNotExist Then
        '    If sFolder = "" OrElse Not IO.Directory.Exists(sFolder) Then
        '        vb14.DialogBox("Katalog danych musi istnieć - uruchom app jeszcze raz, i przejdź do Settings")
        '        Throw New Exception("uiFolderData is not set or directory doesn't exist")
        '    End If
        'End If

        'Return sFolder
    End Function

    Public Shared Function GetDataFolder(sSubfolder As String, Optional bThrowNotExist As Boolean = True) As String
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
    Public Shared Function GetDataFile(sSubfolder As String, sFilename As String, Optional bThrowNotExist As Boolean = True) As String
        Dim sFolder As String = GetDataFolder(sSubfolder, bThrowNotExist)

        Dim sFile As String = IO.Path.Combine(sFolder, sFilename)
        Return sFile

    End Function

    ''' <summary>
    ''' Usuwa pliki z katalogu tymczasowego, które są starsze niż maxHours
    ''' </summary>
    ''' <returns>Liczba usuniętych plików</returns>
    Public Shared Function TempDirPrepare(maxHours As Integer) As Integer
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

    Public Shared Function TempDirGetPath() As String
        Dim katalog As String = IO.Path.Combine(IO.Path.GetTempPath, GetAppName)
        IO.Directory.CreateDirectory(katalog)
        Return katalog
    End Function

    ''' <summary>
    ''' zwraca pathname pliku tymczasowego, dokładność jedna setna sekundy
    ''' </summary>
    Public Shared Function TempDirCreateTempFilename() As String
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

    ' przeniesione do ArchiveQuerender
    'Public Shared Sub AddToGlobalJsonIndex(sIndexShortJson As String, sIndexLongJson As String)

    '    Dim sJsonFile As String = GetDataFile("", "archIndex.json", False)
    '    Vblib.LocalStorage.AddToJsonIndexMain(sJsonFile, sIndexShortJson)

    '    sJsonFile = GetDataFile("", "archIndex.full.json", False)
    '    Vblib.LocalStorage.AddToJsonIndexMain(sJsonFile, sIndexLongJson)

    'End Sub


    Private Shared gSourcesList As lib_PicSource.PicSourceList
    Private Shared gBuffer As Vblib.BufferSortowania

    Public Shared Function GetSourcesList() As lib_PicSource.PicSourceList

        If gSourcesList Is Nothing Then
            'Vblib.PicSourceBase._dataFolder = Application.GetDataFolder ' żeby JSON mógł wczytać sources
            gSourcesList = New lib_PicSource.PicSourceList(Application.GetDataFolder)
            gSourcesList.Load()
            gSourcesList.InitDataDirectory()
        End If
        Return gSourcesList
    End Function

    Public Shared Function GetBuffer() As Vblib.BufferSortowania
        If gBuffer Is Nothing Then
            gBuffer = New Vblib.BufferSortowania(Application.GetDataFolder)
        End If
        Return gBuffer
    End Function

    Public Shared Sub ResetBuffer()
        IO.File.Delete(IO.Path.Combine(Application.GetDataFolder, "buffer.json"))
        gBuffer = Nothing
    End Sub

    Private Shared gArchiveList As BaseList(Of lib_PicSource.LocalStorageMiddle)
    Public Shared Function GetArchivesList() As BaseList(Of lib_PicSource.LocalStorageMiddle)

        If gArchiveList Is Nothing OrElse gArchiveList.Count < 1 Then
            gArchiveList = New BaseList(Of lib_PicSource.LocalStorageMiddle)(Application.GetDataFolder, "archives.json")
            gArchiveList.Load()
        End If
        Return gArchiveList
    End Function


    Private Shared gKeywords As Vblib.KeywordsList

    Public Shared Function GetKeywords() As Vblib.KeywordsList
        If gKeywords Is Nothing Then
            gKeywords = New Vblib.KeywordsList(Application.GetDataFolder)
            gKeywords.Load()
        End If
        Return gKeywords
    End Function

#Region "DirTree"

    Private Shared gDirtree As Vblib.DirsList

    Public Shared Function GetDirTree() As Vblib.DirsList
        If gDirtree Is Nothing Then
            gDirtree = New Vblib.DirsList(Application.GetDataFolder)
            gDirtree.Load()
        End If
        Return gDirtree
    End Function
#End Region

#Region "Cloud publish/archives"

    Private Shared gCloudPublishers As CloudPublishersList

    Public Shared Function GetCloudPublishers() As CloudPublishersList
        If gCloudPublishers Is Nothing Then
            gCloudPublishers = New CloudPublishersList(Application.GetDataFolder)
            gCloudPublishers.Load()
        End If
        Return gCloudPublishers
    End Function


    Private Shared gCloudArchives As CloudArchivesList

    Public Shared Function GetCloudArchives() As CloudArchivesList
        If gCloudArchives Is Nothing Then
            gCloudArchives = New CloudArchivesList(Application.GetDataFolder)
            gCloudArchives.Load()
        End If
        Return gCloudArchives
    End Function
#End Region

#Region "Queries"
    Private Shared gQueries As BaseList(Of SearchQuery)

    Public Shared Function GetQueries() As BaseList(Of SearchQuery)
        If gQueries IsNot Nothing Then Return gQueries
        gQueries = New BaseList(Of SearchQuery)(Application.GetDataFolder, "queries.json")
        gQueries.Load()
        Return gQueries
    End Function
#End Region

#Region "sharing channels/logins"
    Private Shared gShareChannels As ShareChannelsList

    Public Shared Function GetShareChannels() As ShareChannelsList
        If gShareChannels IsNot Nothing Then Return gShareChannels

        gShareChannels = New ShareChannelsList(Application.GetDataFolder, GetQueries)
        gShareChannels.Load()
        Return gShareChannels
    End Function

    Private Shared gShareLogins As ShareLoginsList

    Public Shared Function GetShareLogins() As ShareLoginsList
        If gShareLogins IsNot Nothing Then Return gShareLogins

        gShareLogins = New ShareLoginsList(Application.GetDataFolder, GetShareChannels)
        gShareLogins.Load()
        Return gShareLogins
    End Function

    Private Shared gShareServers As ShareServerList

    Public Shared Function GetShareServers() As ShareServerList
        If gShareServers IsNot Nothing Then Return gShareServers

        gShareServers = New ShareServerList(Application.GetDataFolder)
        gShareServers.Load()
        Return gShareServers
    End Function

    Private Shared gShareDescriptionsIn As ShareDescInList

    Public Shared Function GetShareDescriptionsIn() As ShareDescInList
        If gShareDescriptionsIn IsNot Nothing Then Return gShareDescriptionsIn

        gShareDescriptionsIn = New ShareDescInList(Application.GetDataFolder)
        gShareDescriptionsIn.Load()
        Return gShareDescriptionsIn
    End Function

    Private Shared gShareDescriptionsOut As ShareDescOutList

    Public Shared Function GetShareDescriptionsOut() As ShareDescOutList
        If gShareDescriptionsOut IsNot Nothing Then Return gShareDescriptionsOut

        gShareDescriptionsOut = New ShareDescOutList(Application.GetDataFolder)
        gShareDescriptionsOut.Load()
        Return gShareDescriptionsOut
    End Function


#End Region

    Public Shared gAutoTagery As Vblib.AutotaggerBase() = {
        New Vblib.AutoTag_EXIF,
        New Vblib.AutoTag_FullEXIF,
        New Auto_std2_Astro.Auto_MoonPhase,
        New Auto_std2_Astro.Auto_Astro,
        New Auto_std2_Astro.Auto_Pogoda,
        New Auto_std2_Meteo.Auto_Meteo_Opad(Application.GetDataFolder),
        New Vblib.Auto_GeoNamePl(Application.GetDataFolder),
        New Vblib.Auto_OSM_POI(Application.GetDataFolder),
        New Auto_WinOCR.AutoTag_WinOCR,
        New Auto_WinFace.Auto_WinFace,
        New Vblib.Auto_AzureTest(New Process_ResizeHalf)
    }
    ' New Auto_CreateId(New UniqID(Application.GetDataFolder))

    Public Shared gPostProcesory As Vblib.PostProcBase() = {
        New Process_AutoRotate,
        New Process_Resize800,
        New Process_Resize1024,
        New Process_Resize1280,
        New Process_Resize1600,
        New Process_Resize2048,
        New Process_ResizeHalf,
        New Process_FlipHorizontal,
        New Process_EmbedBasicExif,
        New Process_EmbedExif,
        New Process_RemoveExif,
        New Process_Signature.Process_FaceRemove,
        New Process_Signature.Process_Signature,
        New Process_Watermark
    }

    ' Public Shared gCloudProviders As New CloudProviders

    Private Shared gUniqId As UniqID
    Public Shared Function GetUniqId() As UniqID
        If gUniqId Is Nothing Then
            gUniqId = New UniqID(Application.GetDataFolder)
        End If
        Return gUniqId
    End Function

    Private Shared gArchQuerender As ArchiveIndex
    Public Shared Function GetArchIndex() As ArchiveIndex
        If gArchQuerender Is Nothing Then
            gArchQuerender = New ArchiveIndex(Application.GetDataFolder)
        End If
        Return gArchQuerender
    End Function

    Public Shared gDbase As New Databases(Application.GetDataFolder)


    Public Shared gWcfServer As lib_sharingNetwork.ServerWrapper
    Public Shared gLastLoginSharing As New Vblib.ShareLoginData

End Class

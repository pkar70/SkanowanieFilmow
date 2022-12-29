
Imports vb14 = Vblib.pkarlibmodule14

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


    Public Shared Function GetDataFolder(Optional bThrowNotExist As Boolean = True) As String
        Dim sFolder As String = vb14.GetSettingsString("uiFolderData")
        If bThrowNotExist Then
            If sFolder = "" OrElse Not IO.Directory.Exists(sFolder) Then
                vb14.DialogBox("Katalog danych musi istnieć - uruchom app jeszcze raz, i przejdź do Settings")
                Throw New Exception("uiFolderData is not set or directory doesn't exist")
            End If
        End If

        Return sFolder
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


    Public Shared Sub AddToGlobalJsonIndex(sContent As String)

        Dim sJsonFile As String = GetDataFile("", "archIndex.json", False)
        Vblib.LocalStorage.AddToJsonIndexMain(sJsonFile, sContent)

    End Sub


    Private Shared gSourcesList As VbLibCore3_picSource.PicSourceList
    Private Shared gBuffer As Vblib.Buffer

    Public Shared Function GetSourcesList() As VbLibCore3_picSource.PicSourceList

        If gSourcesList Is Nothing Then
            'Vblib.PicSourceBase._dataFolder = Application.GetDataFolder ' żeby JSON mógł wczytać sources
            gSourcesList = New VbLibCore3_picSource.PicSourceList(Application.GetDataFolder)
            gSourcesList.Load()
            gSourcesList.InitDataDirectory()
        End If
        Return gSourcesList
    End Function

    Public Shared Function GetBuffer() As Vblib.Buffer
        If gBuffer Is Nothing Then
            gBuffer = New Vblib.Buffer(Application.GetDataFolder)
        End If
        Return gBuffer
    End Function

    Private Shared gArchiveList As Vblib.MojaLista(Of VbLibCore3_picSource.LocalStorageMiddle)
    Public Shared Function GetArchivesList() As Vblib.MojaLista(Of VbLibCore3_picSource.LocalStorageMiddle)

        If gArchiveList Is Nothing OrElse gArchiveList.Count < 1 Then
            gArchiveList = New Vblib.MojaLista(Of VbLibCore3_picSource.LocalStorageMiddle)(Application.GetDataFolder, "archives.json")
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

    Private Shared gDirtree As Vblib.DirsList

    Public Shared Function GetDirTree() As Vblib.DirsList
        If gDirtree Is Nothing Then
            gDirtree = New Vblib.DirsList(Application.GetDataFolder)
            gDirtree.Load()
        End If
        Return gDirtree
    End Function


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

    Public Shared gAutoTagery As Vblib.AutotaggerBase() = {
        New Vblib.AutoTag_EXIF,
        New Taggers_OCR.AutoTag_WinOCR,
        New Auto_WinFace.Auto_WinFace,
        New Vblib.Auto_GeoNamePl(Application.GetDataFolder),
        New Vblib.Auto_OSM_POI(Application.GetDataFolder),
        New Vblib.Auto_AzureTest(New Process_ResizeHalf)
    }

    Public Shared gPostProcesory As Vblib.PostProcBase() = {
        New Process_AutoRotate,
        New Process_Resize800,
        New Process_Resize1024,
        New Process_Resize1280,
        New Process_Resize1600,
        New Process_Resize2048,
        New Process_ResizeHalf,
        New Process_EmbedBasicExif,
        New Process_EmbedExif,
        New Process_RemoveExif,
        New Process_Signature.Process_FaceRemove,
        New Process_Signature.Process_Signature,
        New Process_Watermark
    }

    ' Public Shared gCloudProviders As New CloudProviders

End Class

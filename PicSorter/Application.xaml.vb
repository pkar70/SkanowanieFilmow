
Imports vb14 = Vblib.pkarlibmodule14

Partial Class Application

    ' Application-level events, such as Startup, Exit, and DispatcherUnhandledException
    ' can be handled in this file.

    Public Async Function AppServiceLocalCommand(sCommand As String) As Task(Of String)
        Return ""
    End Function

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

    Public Shared Function GetDataFile(sSubfolder As String, sFilename As String, Optional bThrowNotExist As Boolean = True)
        Dim sFolder As String = GetDataFolder(sSubfolder, bThrowNotExist)

        Dim sFile As String = IO.Path.Combine(sFolder, sFilename)
        Return sFile

    End Function

    Private Shared gSourcesList As VbLib20.PicSourceList
    Private Shared gBuffer As Vblib.Buffer

    Public Shared Function GetSourcesList() As VbLib20.PicSourceList

        If gSourcesList Is Nothing Then
            Vblib.PicSourceBase._dataFolder = Application.GetDataFolder ' żeby JSON mógł wczytać sources
            gSourcesList = New VbLib20.PicSourceList(Application.GetDataFolder)
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

    Private Shared gArchiveList As Vblib.MojaLista(Of Vblib.LocalStorage)
    Public Shared Function GetArchivesList() As Vblib.MojaLista(Of Vblib.LocalStorage)

        If gArchiveList Is Nothing OrElse gArchiveList.Count < 1 Then
            gArchiveList = New Vblib.MojaLista(Of Vblib.LocalStorage)(Application.GetDataFolder, "archives.json")
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


    Public Shared gAutoTagery As Vblib.AutotaggerBase() = {
        New Vblib.AutoTag_EXIF,
        New Taggers_OCR.AutoTag_WinOCR,
        New Auto_WinFace.Auto_WinFace,
        New Vblib.Auto_GeoNamePl(Application.GetDataFolder),
        New Vblib.Auto_OSM_POI(Application.GetDataFolder),
        New Vblib.Auto_AzureTest
    }

    Public Shared gPostProcesory As Vblib.PostProcBase() = {
        New Process_AutoRotate,
        New Process_Resize800,
        New Process_Resize1024,
        New Process_Resize1280,
        New Process_Resize1600,
        New Process_EmbedExif
    }


End Class

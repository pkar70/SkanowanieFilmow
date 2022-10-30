
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

    Public Shared Function GetSourcesList() As Vblib.PicSourceList
        If gSourcesList Is Nothing Then
            gSourcesList = New Vblib.PicSourceList(Application.GetDataFolder)
            gSourcesList.Load()
        End If
        Return gSourcesList
    End Function

    Public Shared gSourcesList As Vblib.PicSourceList

End Class

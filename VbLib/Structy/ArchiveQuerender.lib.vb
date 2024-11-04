

Public Class ArchiveIndex

    Private _dataFilenameFull As String
    'Private _dataFilenameFlat As String
    Private _prefix As String
    Private _idlist As List(Of String)

    Public Const FOLDER_INDEX_FILE As String = "picsort.arch.json"

    Public Sub New(datafolder As String)
        _dataFilenameFull = IO.Path.Combine(datafolder, "archIndexFull.json")
        '_dataFilenameFlat = IO.Path.Combine(datafolder, "archIndexFlat.json")
    End Sub

    'Public Sub AddToGlobalJsonIndex(sIndexShortJson As String, sIndexLongJson As String)
    '    AddToJsonIndexMain(_dataFilenameFlat, sIndexShortJson)
    '    AddToJsonIndexMain(_dataFilenameFull, sIndexLongJson)
    'End Sub

    ' wywoływane z zewnątrz, do dodawania do picsort.arch.json
    Public Shared Sub AddToFolderJsonIndex(sFolder As String, sIndexLongJson As String)
        Dim sFilename As String = IO.Path.Combine(sFolder, FOLDER_INDEX_FILE)
        AddToJsonIndexMain(sFilename, sIndexLongJson)
    End Sub

    Private Shared Sub AddToJsonIndexMain(sIndexFilename As String, sContent As String)

        If Not IO.File.Exists(sIndexFilename) Then
            IO.File.WriteAllText(sIndexFilename, "[")
        Else
            ' skoro już mamy coś w pliku, to teraz dodajemy do tego przecinek - pomiędzy itemami
            sContent = "," & vbCrLf & sContent
        End If

        IO.File.AppendAllText(sIndexFilename, sContent)

    End Sub

End Class

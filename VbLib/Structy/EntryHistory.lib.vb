
' support class
' na listę Copyright oraz listę Author/Artist

Public Class EntryHistory
    Private _filepath As String
    Private _lista As List(Of String)

    Sub New(sDataFolder As String, sFileName As String)
        Dim sPath As String = IO.Path.Combine(sDataFolder, sFileName & ".txt")
        _filepath = sPath

        ' load
        If IO.File.Exists(_filepath) Then
            _lista = IO.File.ReadAllLines(_filepath).ToList
        Else
            _lista = New List(Of String)
        End If
    End Sub

    Sub Save()
        IO.File.WriteAllLines(_filepath, _lista)
    End Sub

    Sub Delete(sItem As String)
        _lista.Remove(sItem)
    End Sub

    Sub Add(sItem As String)
        If String.IsNullOrWhiteSpace(sItem) Then Return
        _lista.Add(sItem)
    End Sub

    Function GetList() As List(Of String)
        Return _lista
    End Function

End Class

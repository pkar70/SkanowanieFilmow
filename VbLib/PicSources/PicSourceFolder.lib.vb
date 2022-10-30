' operacje na katalogu

Public Class PicSourceFolder
    Inherits PicSourceBase

    Public Sub New(sDataFolder As String)
        MyBase.New(sDataFolder)
    End Sub

    Public Overrides ReadOnly Property Typ As String = "FOLDER"

    Public Overrides Function ReadDirectory() As Integer
        ' *TODO* wczytanie katalogu do _listaPlikow, zakładam że będzie posortowany według daty
        Throw New NotImplementedException()
    End Function

    Protected Overrides Function DeleteFile(sId As String) As Object
        ' *TODO* usunięcie konkretnego pliku (sId to pathname)
        Throw New NotImplementedException()
    End Function

    Protected Overrides Function IsPresent_Main() As Boolean
        ' *TODO* sprawdzenie czy katalog (VolLabel, Path) jest dostępny
        Throw New NotImplementedException()
    End Function
End Class

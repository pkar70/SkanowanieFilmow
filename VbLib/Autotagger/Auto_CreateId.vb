Public Class Auto_CreateId
    Inherits AutotaggerBase

    Public Overrides ReadOnly Property Typek As AutoTaggerType = AutoTaggerType.Local
    Public Overrides ReadOnly Property Nazwa As String = "AUTO_GUID"
    Public Overrides ReadOnly Property MinWinVersion As String = "7.0"
    Public Overrides ReadOnly Property DymekAbout As String = "Generuje GUID dla zdjęć"
    Public Overrides ReadOnly Property includeMask As String = "*.*"

    Private _uniqId As UniqID

    Public Sub New(uniqID As UniqID)
        _uniqId = uniqID
    End Sub

#Disable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
    Public Overrides Async Function GetForFile(oFile As OnePic) As Task(Of ExifTag)
        _uniqId.SetGUIDforPic(oFile)
        If String.IsNullOrWhiteSpace(oFile.PicGuid) Then Return Nothing

        Return Nothing  ' tym razem to nie znaczy że błąd, ale że już zrobił i nie trzeba dodawać EXIFa
    End Function
#Enable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
End Class

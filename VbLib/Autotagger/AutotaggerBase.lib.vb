Public MustInherit Class AutotaggerBase
    Public MustOverride ReadOnly Property Typek As AutoTaggerType    ' lokalny, web, web z autoryzacją
    Public MustOverride ReadOnly Property Nazwa As String
    Public MustOverride ReadOnly Property MinWinVersion As String
    Public MustOverride ReadOnly Property DymekAbout As String
    ''' <summary>
    ''' Zwraca przygotowany EXIFtag, albo NULL, gdy błąd. EXIFtag może być pusty!
    ''' </summary>
    ''' <param name="oFile"></param>
    ''' <returns></returns>
    Public MustOverride Async Function GetForFile(oFile As OnePic) As Task(Of ExifTag)
End Class



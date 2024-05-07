Public MustInherit Class AutotaggerBase
    Public MustOverride ReadOnly Property Typek As AutoTaggerType    ' lokalny, web, web z autoryzacją
    Public MustOverride ReadOnly Property Nazwa As String
    Public MustOverride ReadOnly Property MinWinVersion As String   ' teoretycznie jakoś w której wersji to działa
    Public MustOverride ReadOnly Property DymekAbout As String

    Public Overridable ReadOnly Property MaxSize As Integer = 0 ' 0: no limit (najwyżej będzie Cancel), albo w kB do ilu zmniejszać przed wywołaniem

    Public Overridable ReadOnly Property includeMask As String = OnePic.ExtsPic & ";" & OnePic.ExtsStereo

    ''' <summary>
    ''' Zwraca przygotowany EXIFtag, albo NULL, gdy błąd. EXIFtag może być pusty!
    ''' </summary>
    ''' <param name="oFile"></param>
    ''' <returns></returns>
    Public MustOverride Async Function GetForFile(oFile As OnePic) As Task(Of ExifTag)

    Public Overridable ReadOnly Property IsWeb As Boolean = False

End Class



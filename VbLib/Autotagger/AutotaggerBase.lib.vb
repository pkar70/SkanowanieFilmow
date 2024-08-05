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
    Public Overridable ReadOnly Property RequireDate As Boolean = False
    Public Overridable ReadOnly Property RequireGeo As Boolean = False

    Public Shared ReadOnly Property IconWeb As String = "🔗"
    Public Shared ReadOnly Property IconGeo As String = "🌍"
    Public Shared ReadOnly Property IconCal As String = "📆"


    Public ReadOnly Property Ikony As String
        Get
            Dim temp As String = ""
            If RequireDate Then temp &= IconCal
            If RequireGeo Then temp &= IconGeo
            If IsWeb Then temp &= IconWeb
            Return temp.Trim
        End Get
    End Property

    Public ReadOnly Property IkonyDymek As String
        Get
            Dim temp As String = ""
            If RequireDate Then temp &= "wymaga daty; "
            If RequireGeo Then temp &= "wymaga danych geo; "
            If IsWeb Then temp &= "wymaga dostępu do Internet; "
            Return temp.Trim
        End Get
    End Property

    ''' <summary>
    ''' Sprawdza czy da się zrobić tak - bez override to tylko sprawdzenie maski
    ''' </summary>
    Public Overridable Function CanTag(oFile As OnePic) As Boolean
        If Not oFile.MatchesMasks(includeMask) Then
            DumpMessage("nie spełnia maski")
            Return False
        End If

        If oFile.GetSumOfDescriptionsKwds.Contains(GetAutoTagDisableKwd) Then
            DumpMessage("Skippin because " & GetAutoTagDisableKwd())
            Return False
        End If

        Return True
    End Function

    Public Function GetAutoTagDisableKwd() As String
        Dim ret As String = Nazwa
        Dim iInd As Integer = ret.IndexOf("_")
        If iInd < 1 Then Return "=NO:" & ret
        Return "=NO:" & ret.Substring(iInd + 1)
    End Function
End Class



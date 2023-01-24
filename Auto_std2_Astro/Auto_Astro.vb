

Imports CosineKitty
'Imports Vblib
Imports Vblib.Extensions


Public Class Auto_MoonPhase
    Inherits Vblib.AutotaggerBase

    Public Overrides ReadOnly Property Typek As Vblib.AutoTaggerType = Vblib.AutoTaggerType.Local
    Public Overrides ReadOnly Property Nazwa As String = Vblib.ExifSource.AutoMoon
    Public Overrides ReadOnly Property MinWinVersion As String = "7.0"
    Public Overrides ReadOnly Property DymekAbout As String = "Wylicza (dla daty zdjêcia) fazê Ksiê¿yca"
    Public Overrides ReadOnly Property includeMask As String = "*.*"

    Public Overrides Async Function GetForFile(oFile As Vblib.OnePic) As Task(Of Vblib.ExifTag)

        ' musimy mieæ prawdziw¹ datê
        Dim oExif As Vblib.ExifTag = oFile.GetExifOfType(Vblib.ExifSource.FileExif)
        If oExif Is Nothing Then Return Nothing
        Dim data As Date = Vblib.OnePic.ExifDateToDate(oExif.DateTimeOriginal)
        If Not data.IsDateValid Then Return Nothing

        Dim oAstro As New Vblib.AutoWeatherDay
        oAstro.moonphase = Auto_Astro.GetMoonPhase(data)
        Dim oPogoda As New Vblib.CacheAutoWeather_Item
        oPogoda.day = oAstro

        oExif = New Vblib.ExifTag(Vblib.ExifSource.AutoMoon)
        oExif.PogodaAstro = oPogoda

        Return oExif

    End Function

End Class


Public Class Auto_Astro
    Inherits Vblib.AutotaggerBase

    Public Overrides ReadOnly Property Typek As Vblib.AutoTaggerType = Vblib.AutoTaggerType.Local
    Public Overrides ReadOnly Property Nazwa As String = Vblib.ExifSource.AutoAstro
    Public Overrides ReadOnly Property MinWinVersion As String = "7.0"
    Public Overrides ReadOnly Property DymekAbout As String = "Wylicza (dla daty zdjêcia) fazê Ksiê¿yca, wschód i zachód S³oñca oraz Ksiê¿yca"
    Public Overrides ReadOnly Property includeMask As String = "*.*"

    Public Overrides Async Function GetForFile(oFile As Vblib.OnePic) As Task(Of Vblib.ExifTag)

        ' musimy mieæ prawdziw¹ datê
        Dim oExif As Vblib.ExifTag = oFile.GetExifOfType(Vblib.ExifSource.FileExif)
        If oExif Is Nothing Then Return Nothing
        Dim data As Date = Vblib.OnePic.ExifDateToDate(oExif.DateTimeOriginal)
        If Not data.IsDateValid Then Return Nothing

        ' geo niby nie musimy mieæ, ale dla uproszczenia (AutoTag, AddGeo, AutoTag - nie doda³by wschodów/zachodów)
        Dim oGeo As pkar.BasicGeopos = oFile.GetGeoTag
        If oGeo Is Nothing Then Return Nothing

        Dim oAstro As New Vblib.AutoWeatherDay
        oAstro.moonphase = GetMoonPhase(data)

        Dim szukajPo As New Date(data.Year, data.Month, data.Day)
        Dim obserwator As New Observer(oGeo.Latitude, oGeo.Longitude, oGeo.Altitude)

        Dim wschod As Date = GetCzas(szukajPo, Body.Sun, obserwator, oGeo, Direction.Rise)
        Dim zachod As Date = GetCzas(szukajPo, Body.Sun, obserwator, oGeo, Direction.Set)

        oAstro.sunrise = wschod.ToString("HH:mm:ss")
        oAstro.sunset = zachod.ToString("HH:mm:ss")

        'Dim timeZone As Integer = 1
        'If Not oGeo.IsInsideEU Then
        '    timeZone = GuessTimeZone(oGeo)
        'Else
        '    If EuroDST(data) Then timeZone += 1
        'End If

        oAstro.sunhour = Math.Round(CalculateSunHour(data, wschod.ToLocalTime, zachod.ToLocalTime), 2)

        Dim moonrise As Date = GetCzas(szukajPo, Body.Moon, obserwator, oGeo, Direction.Rise)
        Dim moonset As Date = GetCzas(szukajPo, Body.Moon, obserwator, oGeo, Direction.Set)

        If moonset.Day = data.Day Then oAstro.moonset = moonset.ToString("HH:mm:ss")

        If moonrise < moonset Then
            If moonrise.Day = data.Day Then oAstro.moonrise = moonrise.ToString("HH:mm:ss")
        End If

        Dim oPogoda As New Vblib.CacheAutoWeather_Item
        oPogoda.day = oAstro
        oExif = New Vblib.ExifTag(Vblib.ExifSource.AutoAstro)

        oExif.PogodaAstro = oPogoda

        Return oExif

    End Function


    Public Shared Function GuessTimeZone(oGeo As pkar.BasicGeopos)
        Dim dLon As Double = oGeo.Longitude
        Dim iGodzin As Integer = Math.Round(dLon / 15)
        If dLon < 0 Then iGodzin *= -1
        If dLon > 180 Then iGodzin *= -1
        Return iGodzin
    End Function

    ''' <summary>
    ''' sprawdŸ czy to czas letni, czyli czy przesuwaæ o godzinê wzglêdem czasu normalnego (wed³ug regu³ EU)
    ''' </summary>
    ''' <param name="oDate"></param>
    ''' <returns></returns>
    Public Shared Function EuroDST(oDate As Date) As Boolean

        Dim iDay03 As Integer
        For iDay03 = 31 To 22 Step -1
            If (New Date(oDate.Year, 3, iDay03)).DayOfWeek = DayOfWeek.Sunday Then Exit For
        Next

        Dim iDay10 As Integer
        For iDay10 = 31 To 22 Step -1
            If (New Date(oDate.Year, 10, iDay10)).DayOfWeek = DayOfWeek.Sunday Then Exit For
        Next

        If oDate < New Date(oDate.Year, 3, iDay03) Then Return False
        If oDate > New Date(oDate.Year, 10, iDay10) Then Return False
        Return True
        ' begins (clocks go forward) at 01:00 UTC on the last Sunday in March, and ends (clocks go back) at 01:00 UTC on the last Sunday in October:
    End Function


    ''' <summary>
    ''' zwraca godzinê s³oneczn¹, +1 to 1 w dzieñ; -1 to pierwsza w nocy; 100 to nie umie policzyæ
    ''' </summary>
    Public Shared Function CalculateSunHour(data As Date, wschod As Date, zachod As Date) As Double

        ' gdy mamy do czynienia z krêgiem polarnym :)
        If zachod < wschod OrElse (zachod - wschod).TotalHours > 24 Then Return 100

        Dim SunHourLen As Double = (zachod - wschod).TotalHours / 12
        Dim SunNightLen As Double = (24 - (zachod - wschod).TotalHours) / 12

        If data > zachod Then
            ' po zachodzie S³oñca
            Return -1 * (data - zachod).TotalHours / SunNightLen
        ElseIf data > wschod Then
            ' mamy dzieñ
            Return (data - wschod).TotalHours / SunHourLen
        Else
            ' przed wschodem S³oñca
            Return -1 * 12 - ((wschod - data).TotalHours / SunNightLen)
        End If
    End Function

    ''' <summary>
    ''' 0..1, od nowiu do pe³ni (pojawia siê, D); -1..0 od pe³ni do nowiu (znika, C)
    ''' </summary>
    ''' <param name="data"></param>
    ''' <returns></returns>
    Public Shared Function GetMoonPhase(data As Date) As Integer
        Dim dAngle As Double = Astronomy.MoonPhase(New AstroTime(data))

        If dAngle <= 180 Then Return Math.Round(100.0 / 180 * dAngle)
        dAngle = 360 - dAngle
        Return Math.Round(-(100.0 / 180 * dAngle))
    End Function

    ''' <summary>
    ''' zwraca czas w standardzie "HH:mm:ss" dla zdarzenia
    ''' </summary>
    ''' <returns></returns>
    Private Shared Function GetCzas(data As Date, cialo As Body, oObs As Observer, oGeo As pkar.BasicGeopos, riseset As Direction) As Date

        Dim astroTime As AstroTime = New AstroTime(data)
        astroTime = Astronomy.SearchRiseSet(cialo, oObs, riseset, astroTime, 1)
        If astroTime Is Nothing Then Return Nothing

        Return astroTime.ToUtcDateTime
    End Function

End Class

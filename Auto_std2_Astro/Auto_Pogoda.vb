Imports Microsoft.Rest.Azure
Imports Vblib


Public Class Auto_Pogoda
    Inherits Vblib.AutotaggerBase

    Public Overrides ReadOnly Property Typek As Vblib.AutoTaggerType = Vblib.AutoTaggerType.WebAccount
    Public Overrides ReadOnly Property Nazwa As String = Vblib.ExifSource.AutoVisCrosWeather
    Public Overrides ReadOnly Property MinWinVersion As String = "7.0"
    Public Overrides ReadOnly Property DymekAbout As String = "Ściąga informacje o pogodzie, używając visualcrossing.com/weather." & vbCrLf & "Limit 1000 szukań dziennie!"
    Public Overrides ReadOnly Property includeMask As String = "*.*"
    Public Overrides ReadOnly Property RequireDate As Boolean = True
    Public Overrides ReadOnly Property RequireGeo As Boolean = True

    Public Shared Property maxGuard As Integer = 800

    Private _cache As New List(Of Vblib.CacheAutoWeather_Item)

    Public Overrides Async Function GetForFile(oFile As Vblib.OnePic) As Task(Of Vblib.ExifTag)
        If Not oFile.MatchesMasks(includeMask, "") Then Return Nothing

        If oFile.Exifs Is Nothing Then Return Nothing

        ' musimy mieć zarówno GEO jak i DATE
        Dim oGeo As pkar.BasicGeopos = oFile.GetGeoTag
        If oGeo Is Nothing Then Return Nothing
        Dim oData As Date = oFile.GetMostProbablyDate(True)
        If Not oData.IsDateValid Then Return Nothing

        If Vblib.GetSettingsString("uiVisualCrossSubscriptionKey").Length < 5 Then Return Nothing

        Dim oWeather As Vblib.CacheAutoWeather_Item = TryFromCache(oGeo, oData.ToUniversalTime)

        If oWeather Is Nothing Then
            If maxGuard < 0 Then Return Nothing

            maxGuard -= 1
            Vblib.DumpMessage($"Weather guard: {maxGuard}")
            oWeather = Await TryFromWWW(oGeo, oData)
            If oWeather Is Nothing Then Return Nothing

            ChangeMoonPhase(oWeather)
            CalculateSunHour(oWeather, oData)

            _cache.Add(oWeather)
        End If

        Dim oExif As New Vblib.ExifTag(Vblib.ExifSource.AutoVisCrosWeather)
        oExif.PogodaAstro = oWeather

        Return oExif

    End Function

    Private Sub ChangeMoonPhase(oWeather As CacheAutoWeather_Item)
        ' IN: 0 nów, 0.5 pełnia, 1 nowy nów
        ' OUT: 0 ... 100, -, 0

        If oWeather.day.moonphase > 0.5 Then
            oWeather.day.moonphase = -Math.Round(200 * (1 - oWeather.day.moonphase))
        Else
            ' 0 .. 0.5 na 0 .. 100
            oWeather.day.moonphase = Math.Round(200 * oWeather.day.moonphase)
        End If

    End Sub

    Private Sub CalculateSunHour(oWeather As CacheAutoWeather_Item, oData As Date)

        Dim wschod As New Date(DateTimeOffset.FromUnixTimeSeconds(oWeather.day.sunriseEpoch).ToLocalTime.Ticks)
        Dim zachod As New Date(DateTimeOffset.FromUnixTimeSeconds(oWeather.day.sunsetEpoch).ToLocalTime.Ticks)

        oWeather.day.sunhour = Math.Round(Auto_Astro.CalculateSunHour(oData, wschod, zachod), 2)

    End Sub

    Private Function TryFromCache(oGeo As pkar.BasicGeopos, oDataUTC As Date) As Vblib.CacheAutoWeather_Item
        ' zabieramy z cache, jeśli takowy jest - limit odległości geo (20 km) oraz limit czasu (godzina)

        For Each oItem As Vblib.CacheAutoWeather_Item In _cache

            If oGeo.DistanceTo(oItem.latitude, oItem.longitude) > 20000 Then Continue For

            Dim cacheDataUTC As DateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(oItem.currentConditions.datetimeEpoch)
            If cacheDataUTC.Year <> oDataUTC.Year Then Continue For
            If cacheDataUTC.Month <> oDataUTC.Month Then Continue For
            If cacheDataUTC.Day <> oDataUTC.Day Then Continue For
            If cacheDataUTC.Hour <> oDataUTC.Hour Then Continue For

            Return oItem
        Next

        Return Nothing

    End Function

    Private Sub ModifyFazaKsiezyca(oWeather As Vblib.CacheAutoWeather_Item)
        Throw New NotImplementedException()
    End Sub

    Private Shared _oHttp As Net.Http.HttpClient


    Private Async Function TryFromWWW(oGeo As pkar.BasicGeopos, oDataLocal As Date) As Task(Of Vblib.CacheAutoWeather_Item)

        Dim key As String = Vblib.GetSettingsString("uiVisualCrossSubscriptionKey")
        Dim data As String = oDataLocal.ToString("yyyy-MM-ddTHH:00:00")

        ' https://www.visualcrossing.com/resources/documentation/weather-api/timeline-weather-api/
        Dim sUri As String = "https://weather.visualcrossing.com/VisualCrossingWebServices/rest/services/timeline"
        ' Kraków: 50.0605,19.9324
        Dim sCmd As String = $"/{oGeo.Latitude},{oGeo.Longitude}/{data}"
        sCmd &= $"?include=current&unitGroup=metric&key={key}&options=nonulls&maxDistance=20000"
        sCmd &= "&elements=datetime,datetimeEpoch,temp,feelslike,humidity,dew,precip,snow,snowdepth,preciptype,windgust,windspeed,winddir,pressure,visibility,cloudcover,solarradiation,solarenergy,uvindex,conditions,icon,tempmax,tempmin,feelslikemax,feelslikemin,precipprob,precipcover,sunrise,sunriseEpoch,sunset,sunsetEpoch,sunhour,moonphase,moonrise,moonset,description"
        Dim sPage As String

        If _oHttp Is Nothing Then
            _oHttp = New Net.Http.HttpClient()
            _oHttp.DefaultRequestHeaders.UserAgent.TryParseAdd(Nazwa)
            _oHttp.DefaultRequestHeaders.Add("Accept", "application/json; charset=UTF-8")
        End If
        Dim oResp As Net.Http.HttpResponseMessage = Await _oHttp.GetAsync(New Uri(sUri & sCmd))
        sPage = Await oResp.Content.ReadAsStringAsync()

        Await Task.Delay(100)

        If String.IsNullOrWhiteSpace(sPage) Then Return Nothing

        Dim nowyItem As Vblib.CacheAutoWeather_Item
        Try
            nowyItem = Newtonsoft.Json.JsonConvert.DeserializeObject(sPage, GetType(Vblib.CacheAutoWeather_Item))
            nowyItem.day = nowyItem.days(0) ' przepisujemy by było ładniej, z pominięciem listy
            nowyItem.days = Nothing
        Catch ex As Exception
            Return Nothing
        End Try

        Return nowyItem
    End Function



End Class






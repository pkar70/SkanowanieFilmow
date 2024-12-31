Imports pkar

Public Class Auto_OSM_POI
    Inherits AutotaggerBase

    Public Overrides ReadOnly Property Typek As Vblib.AutoTaggerType = Vblib.AutoTaggerType.WebPublic
    Public Overrides ReadOnly Property Nazwa As String = Vblib.ExifSource.AutoOSM
    Public Overrides ReadOnly Property MinWinVersion As String = "7.0"
    Public Overrides ReadOnly Property DymekAbout As String = "Zamienia współrzędne na nazwę miejsca (tylko pierwszy znacznik), używając OpenStreetMap." & vbCrLf & "Limit 1 szukanie na sekundę!"
    Public Overrides ReadOnly Property includeMask As String = "*.*"
    Public Overrides ReadOnly Property IsWeb As Boolean = True
    Public Overrides ReadOnly Property RequireGeo As Boolean = True

    Private _cacheDataFolder As String

    Public Sub New()
        _cacheDataFolder = Vblib.GetDataFolder
    End Sub

    ''' <summary>
    ''' zwraca zDepolitowaną nazwę
    ''' </summary>
    ''' <param name="sFullGeoName"></param>
    ''' <returns></returns>
    Public Shared Function FullGeoNameToFolderName(sFullGeoName As String) As String
        Dim sNazwa As String = sFullGeoName
        Dim iInd As Integer
        For i As Integer = 1 To 3
            iInd = sNazwa.LastIndexOf(",")
            If iInd > 0 Then sNazwa = sNazwa.Substring(0, iInd)
        Next
        iInd = sNazwa.LastIndexOf(",")
        If iInd > 0 Then sNazwa = sNazwa.Substring(iInd + 1).Trim
        Return sNazwa.Depolit ' DropAccents gdy bedzie tu .NetStd2
    End Function

    Public Overrides Function CanTag(oFile As OnePic) As Boolean
        Return (oFile.sumOfGeo IsNot Nothing)
    End Function


    Public Overrides Async Function GetForFile(oFile As OnePic) As Task(Of ExifTag)
        If Not CanTag(oFile) Then Return Nothing

        Dim oGeo As BasicGeoposWithRadius = oFile.sumOfGeo ' .GetGeoTag
        Dim oNew As New ExifTag(Nazwa)
        oNew.GeoTag = oFile.GetGeoTag
        oNew.GeoName = OSMnameZgrubne(Await GetNameForGeoPos(oGeo), oGeo.Radius > 1000)
        DumpMessage("geoname: " & oNew.GeoName)
        Return oNew

    End Function

    Private Shared _lastGeo As pkar.BasicGeopos = pkar.BasicGeopos.Empty
    Private Shared _lastName As String

    Private Async Function GetNameForGeoPos(oPos As pkar.BasicGeopos) As Task(Of String)
        DumpCurrMethod()
        EnsureCache()

        If _lastGeo.DistanceTo(oPos) < 20 Then
            DumpMessage("AUTO_OSM_POI same as last")
            Return _lastName
        End If

        _lastName = Await GetNameForGeoPosMain(oPos)
        _lastGeo = oPos

        Return _lastName
    End Function

    Private Async Function GetNameForGeoPosMain(oPos As pkar.BasicGeopos) As Task(Of String)
        EnsureCache()

        Dim sGeoName As String = TryFromCache(oPos)
        If sGeoName <> "" Then
            DumpMessage("AUTO_OSM_POI from cache")
            Return sGeoName
        End If

        sGeoName = Await TryAddToCache(oPos)
        If sGeoName <> "" Then
            DumpMessage("AUTO_OSM_POI from web")
            Return sGeoName
        End If

        Return ""
    End Function

    Private Shared _oHttp As Net.Http.HttpClient

    Private Function OSMnameZgrubne(OSMname As String, zgrubne As Boolean)
        If Not zgrubne Then Return OSMname

        ' "Rynek, Osiedle 70-lecia Niepodległości Polski, Zatyle, Lanckorona, gmina Lanckorona, powiat wadowicki, województwo małopolskie, 34-143, Polska"
        ' od 6 przecinka od konca

        ' "Zakościele, Podkościele, Podksięże, Sucha Beskidzka, powiat suski, województwo małopolskie, 34-200, Polska"
        ' od 5 przecinka od konca

        ' "Nowosądecka, Kozłówka, Bieżanów-Prokocim, Kraków, województwo małopolskie, 30-681, Polska" 
        ' od 4 przecinka od konca


        Dim iPrzecinkow As Integer = 4
        If OSMname.Contains("powiat") Then iPrzecinkow += 1
        If OSMname.Contains("gmina") Then iPrzecinkow += 1

        Dim iInd As Integer = OSMname.Length - 1
        For iLp As Integer = 1 To iPrzecinkow
            Dim iInd1 As Integer = OSMname.LastIndexOf(",", iInd)
            If iInd1 < 0 Then Return OSMname
            iInd = iInd1
        Next

        Return OSMname.Substring(iInd + 1).Trim

    End Function

    Private Async Function TryAddToCache(oPos As pkar.BasicGeopos) As Task(Of String)
        Dim sUri As String = $"https://nominatim.openstreetmap.org/reverse?lat={oPos.StringLat}&lon={oPos.StringLon}&format=jsonv2&zoom=17"

        ' z tego daje not found
        ' Dim sPage As String = Await HttpPageAsync(sUri)
        Dim sPage As String

        If _oHttp Is Nothing Then
            _oHttp = New Net.Http.HttpClient()
            _oHttp.DefaultRequestHeaders.UserAgent.TryParseAdd(Nazwa)
            _oHttp.DefaultRequestHeaders.Add("Accept", "application/json; charset=UTF-8")
        End If
        Dim oResp As Net.Http.HttpResponseMessage = Await _oHttp.GetAsync(New Uri(sUri))
        sPage = Await oResp.Content.ReadAsStringAsync()

        Await Task.Delay(1000)  ' wymogi licencyjne

        If String.IsNullOrWhiteSpace(sPage) Then Return ""

        ' jako list of CacheImgw_Item
        Dim nowyItem As CacheOSMPOI_Item
        Try
            nowyItem = Newtonsoft.Json.JsonConvert.DeserializeObject(sPage, GetType(CacheOSMPOI_Item))
        Catch ex As Exception
            Return ""
        End Try

        ' jeśli czegoś nie ma w _CacheLista, to dodaj - i zapisz
        Dim bNieZnam As Boolean = True
        For Each oItem As CacheOSMPOI_Item In _CacheLista
            If oItem.place_id = nowyItem.place_id AndAlso oItem.osm_id = nowyItem.osm_id Then
                bNieZnam = False
                Exit For
            End If
        Next

        If bNieZnam Then _CacheLista.Add(nowyItem)

        _CacheLista.Save()

        Return nowyItem.display_name
    End Function

    Private Function TryFromCache(oPos As pkar.BasicGeopos) As String

        Dim oBliskie As CacheOSMPOI_Item = FindNearestPoint(_CacheLista, oPos)
        If oBliskie Is Nothing Then Return ""

        If oPos.DistanceTo(oBliskie.lat, oBliskie.lon) < 500 Then Return oBliskie.display_name

        Return ""

    End Function

    Private Sub EnsureCache()
        If _CacheLista IsNot Nothing Then Return

        _CacheLista = New pkar.BaseList(Of CacheOSMPOI_Item)(_cacheDataFolder, Nazwa & ".json")
        _CacheLista.Load()
    End Sub

    Private Shared _CacheLista As pkar.BaseList(Of CacheOSMPOI_Item)

    Private Function FindNearestPoint(oLista As List(Of CacheOSMPOI_Item), oPos As pkar.BasicGeopos) As CacheOSMPOI_Item
        Dim dMinOdl As Double = Double.MaxValue
        Dim Najblizsze As CacheOSMPOI_Item = Nothing

        For Each oItem As CacheOSMPOI_Item In oLista
            Dim dOdl As Double = oPos.DistanceTo(oItem.lat, oItem.lon)
            If dOdl < dMinOdl Then
                dMinOdl = dOdl
                Najblizsze = oItem
            End If
        Next

        Return Najblizsze
    End Function



    Public Class CacheOSMPOI_Item
        Inherits pkar.BaseStruct
        Public Property place_id As String
        'Public Property licence As String
        Public Property osm_type As String
        Public Property osm_id As String
        Public Property lat As String
        Public Property lon As String
        'Public Property place_rank As String
        'Public Property category As String
        'Public Property type As String
        'Public Property importance As String
        'Public Property addresstype As String
        Public Property display_name As String
        'Public Property name As String
        'Public Property address As CacheOSMPOI_Address
        'Public Property boundingbox() As String
    End Class

    'Public Class CacheOSMPOI_Address
    '    Public Property road As String
    '    Public Property village As String
    '    Public Property state_district As String
    '    Public Property state As String
    '    Public Property postcode As String
    '    Public Property country As String
    '    Public Property country_code As String
    'End Class


End Class




' utrzymuje własny cache - na razie nielimitowany, może kiedyś trzeba będzie ograniczać

' jeśli znajdzie w cache < 500 metrów, to tego używa (najbliższego z limitem 500)
' wczytuje z WEB serię nowych punktów
' i znajduje najbliższą znalezioną (niezależnie od odległości)


Public Class Auto_GeoNamePl
    Inherits AutotaggerBase

    Public Overrides ReadOnly Property Typek As Vblib.AutoTaggerType = Vblib.AutoTaggerType.WebPublic
    Public Overrides ReadOnly Property Nazwa As String = Vblib.ExifSource.AutoImgw
    Public Overrides ReadOnly Property MinWinVersion As String = "7.0"
    Public Overrides ReadOnly Property DymekAbout As String = "Zamienia współrzędne na nazwę miejscowości (tylko pierwszy znacznik)"
    Public Overrides ReadOnly Property includeMask As String = "*.*"

    Private _cacheDataFolder As String

    Public Sub New(dataFolder As String)
        _cacheDataFolder = dataFolder
    End Sub

    Public Overrides Async Function GetForFile(oFile As OnePic) As Task(Of ExifTag)
        If Not oFile.MatchesMasks(includeMask) Then Return Nothing

        If oFile.Exifs Is Nothing Then Return Nothing

        For Each oItem As ExifTag In oFile.Exifs
            If oItem.GeoName <> "" Then Continue For
            If oItem.GeoTag Is Nothing Then Continue For
            If oItem.GeoTag.IsEmpty Then Continue For
            If Not oItem.GeoTag.IsInsidePoland Then Continue For

            Dim oNew As New ExifTag(Nazwa)
            oNew.GeoTag = oItem.GeoTag
            oNew.GeoName = Await GetNameForGeoPos(oItem.GeoTag)
            Return oNew
        Next

        Return Nothing

    End Function

    Private Shared _lastGeo As MyBasicGeoposition = MyBasicGeoposition.EmptyGeoPos
    Private Shared _lastName As String

    Private Async Function GetNameForGeoPos(oPos As MyBasicGeoposition) As Task(Of String)
        DumpCurrMethod()
        EnsureCache()

        If _lastGeo.DistanceTo(oPos) < 20 Then
            DumpMessage("AUTO_GEONAME_PL same as last")
            Return _lastName
        End If

        _lastName = Await GetNameForGeoPosMain(oPos)
        _lastGeo = oPos

        Return _lastName
    End Function
    Private Async Function GetNameForGeoPosMain(oPos As MyBasicGeoposition) As Task(Of String)

        Dim sGeoName As String = TryFromCache(oPos)
        If sGeoName <> "" Then
            DumpMessage("AUTO_GEONAME_PL from cache")
            Return sGeoName
        End If

        sGeoName = Await TryAddToCache(oPos)
        If sGeoName <> "" Then
            DumpMessage("AUTO_GEONAME_PL from web")
            Return sGeoName
        End If

        Return ""
    End Function


    Private Async Function TryAddToCache(oPos As MyBasicGeoposition) As Task(Of String)
        Dim sUri As String = "https://meteo.imgw.pl/api/geo/v2/revers/search/" & oPos.StringLat & "/" & oPos.StringLon

        Dim sPage As String = Await HttpPageAsync(sUri)
        Await Task.Delay(100)  ' żeby nie zrobił zbyt dużego ruchu

        If String.IsNullOrWhiteSpace(sPage) Then Return ""

        ' jako list of CacheImgw_Item
        Dim nowaLista As New List(Of CacheImgw_Item)
        Try
            nowaLista = Newtonsoft.Json.JsonConvert.DeserializeObject(sPage, GetType(List(Of CacheImgw_Item)))
        Catch ex As Exception
            Return ""
        End Try

        If nowaLista.Count = 0 Then
            DumpMessage("AUTO_GEONAME_PL nie znajduje - to może Gdańsk :)")
            Return ""
        End If

        ' jeśli czegoś nie ma w _CacheLista, to dodaj - i zapisz
        For Each oNew As CacheImgw_Item In nowaLista
            Dim bNieZnam As Boolean = True
            For Each oItem As CacheImgw_Item In _CacheLista.GetList
                If oItem.teryt = oNew.teryt AndAlso oItem.name = oNew.name Then
                    bNieZnam = False
                    Exit For
                End If
            Next

            If bNieZnam Then _CacheLista.Add(oNew)
        Next

        _CacheLista.Save()

        Dim oBliskie As CacheImgw_Item = FindNearestPoint(nowaLista, oPos)
        Return oBliskie.DisplayName
    End Function

    Private Function TryFromCache(oPos As MyBasicGeoposition) As String

        Dim oBliskie As CacheImgw_Item = FindNearestPoint(_CacheLista.GetList, oPos)
        If oBliskie Is Nothing Then Return ""

        If oPos.DistanceTo(oBliskie.lat, oBliskie.lon) < 500 Then Return oBliskie.DisplayName

        Return ""

    End Function

    Private Sub EnsureCache()
        If _CacheLista IsNot Nothing Then Return

        _CacheLista = New MojaLista(Of CacheImgw_Item)(_cacheDataFolder, Nazwa & ".json")
        _CacheLista.Load()
    End Sub

    Private Shared _CacheLista As MojaLista(Of CacheImgw_Item)

    Private Function FindNearestPoint(oLista As List(Of CacheImgw_Item), oPos As MyBasicGeoposition) As CacheImgw_Item
        Dim dMinOdl As Double = Double.MaxValue
        Dim Najblizsze As CacheImgw_Item = Nothing

        For Each oItem As CacheImgw_Item In oLista
            Dim dOdl As Double = oPos.DistanceTo(oItem.lat, oItem.lon)
            If dOdl < dMinOdl Then
                dMinOdl = dOdl
                Najblizsze = oItem
            End If
        Next

        Return Najblizsze
    End Function

    Public Class CacheImgw_Item
        Inherits MojaStruct
        'Public Property identifier As String
        Public Property name As String
        Public Property lat As String
        Public Property lon As String
        'Public Property catchment As String
        Public Property teryt As String
        Public Property province As String
        Public Property district As String
        Public Property commune As String
        'Public Property rank As String
        'Public Property dist As String
        'Public Property synoptic As Boolean

        Public Function DisplayName() As String
            Return province & " » " & district & " » " & commune & " » " & name
        End Function
    End Class

End Class

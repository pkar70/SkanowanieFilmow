﻿

' utrzymuje własny cache - na razie nielimitowany, może kiedyś trzeba będzie ograniczać

' jeśli znajdzie w cache < 500 metrów, to tego używa (najbliższego z limitem 500)
' wczytuje z WEB serię nowych punktów
' i znajduje najbliższą znalezioną (niezależnie od odległości)


Imports pkar

Public Class Auto_GeoNamePl
    Inherits AutotaggerBase

    Public Overrides ReadOnly Property Typek As Vblib.AutoTaggerType = Vblib.AutoTaggerType.WebPublic
    Public Overrides ReadOnly Property Nazwa As String = Vblib.ExifSource.AutoImgw
    Public Overrides ReadOnly Property MinWinVersion As String = "7.0"
    Public Overrides ReadOnly Property DymekAbout As String = "Zamienia współrzędne na nazwę miejscowości (tylko pierwszy znacznik)"
    Public Overrides ReadOnly Property includeMask As String = "*.*"
    Public Overrides ReadOnly Property IsWeb As Boolean = True
    Public Overrides ReadOnly Property RequireGeo As Boolean = True

    Private _cacheDataFolder As String

    Public Sub New()
        _cacheDataFolder = Vblib.GetDataFolder
    End Sub

    Public Overrides Function CanTag(oFile As OnePic) As Boolean

        Dim oGeo As BasicGeoposWithRadius = oFile.sumOfGeo

        If oGeo Is Nothing Then
            DumpMessage("nie mam danych geograficznych")
            Return False
        End If

        If Not oGeo.IsInsidePoland Then
            DumpMessage("poza Polską")
            Return False
        End If

        Return True
    End Function


    Public Overrides Async Function GetForFile(oFile As OnePic) As Task(Of ExifTag)
        DumpCurrMethod("file: " & oFile.sSuggestedFilename)
        If Not CanTag(oFile) Then Return Nothing

        Dim oGeo As BasicGeoposWithRadius = oFile.sumOfGeo ' .GetGeoTag
        Dim oNew As New ExifTag(Nazwa)
        oNew.GeoTag = oFile.GetGeoTag
        oNew.GeoName = Await GetNameForGeoPos(oGeo, oGeo.Radius > 1000)
        DumpMessage("geoname: " & oNew.GeoName)
        Return oNew

    End Function

    Private Shared _lastGeo As pkar.BasicGeopos = pkar.BasicGeopos.Empty
    Private Shared _lastName As String

    Private Async Function GetNameForGeoPos(oPos As pkar.BasicGeopos, bZgrubne As Boolean) As Task(Of String)
        DumpCurrMethod()
        EnsureCache()

        If _lastGeo.DistanceTo(oPos) < 20 Then
            DumpMessage("AUTO_GEONAME_PL same as last")
            Return _lastName
        End If

        _lastName = Await GetNameForGeoPosMain(oPos, bZgrubne)
        _lastGeo = oPos

        Return _lastName
    End Function
    Private Async Function GetNameForGeoPosMain(oPos As pkar.BasicGeopos, bZgrubne As Boolean) As Task(Of String)

        Dim sGeoName As String = TryFromCache(oPos, bZgrubne)
        If sGeoName <> "" Then
            DumpMessage("AUTO_GEONAME_PL from cache")
            Return sGeoName
        End If

        sGeoName = Await TryAddToCache(oPos, bZgrubne)
        If sGeoName <> "" Then
            DumpMessage("AUTO_GEONAME_PL from web")
            Return sGeoName
        End If

        Return ""
    End Function


    Private Async Function TryAddToCache(oPos As pkar.BasicGeopos, bZgrubne As Boolean) As Task(Of String)
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
            For Each oItem As CacheImgw_Item In _CacheLista
                If oItem.teryt = oNew.teryt AndAlso oItem.name = oNew.name Then
                    bNieZnam = False
                    Exit For
                End If
            Next

            If bNieZnam Then _CacheLista.Add(oNew)
        Next

        _CacheLista.Save()

        Dim oBliskie As CacheImgw_Item = FindNearestPoint(nowaLista, oPos)
        Return oBliskie.DisplayName(bZgrubne)
    End Function

    Private Function TryFromCache(oPos As pkar.BasicGeopos, bZgrubne As Boolean) As String

        Dim oBliskie As CacheImgw_Item = FindNearestPoint(_CacheLista, oPos)
        If oBliskie Is Nothing Then Return ""

        If oPos.DistanceTo(oBliskie.lat, oBliskie.lon) < 500 Then Return oBliskie.DisplayName(bZgrubne)

        Return ""

    End Function

    Private Sub EnsureCache()
        If _CacheLista IsNot Nothing Then Return

        _CacheLista = New pkar.BaseList(Of CacheImgw_Item)(_cacheDataFolder, Nazwa & ".json")
        _CacheLista.Load()
    End Sub

    Private Shared _CacheLista As pkar.BaseList(Of CacheImgw_Item)

    Private Function FindNearestPoint(oLista As List(Of CacheImgw_Item), oPos As pkar.BasicGeopos) As CacheImgw_Item
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
        Inherits pkar.BaseStruct
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

        Public Function DisplayName(bZgrubne As Boolean) As String
            If bZgrubne Then Return province & " » " & district & " » " & commune
            Return province & " » " & district & " » " & commune & " » " & name
        End Function
    End Class

End Class



Imports System.Globalization
Imports System.IO
Imports System.Net
Imports System.Linq
Imports pkar.DotNetExtensions

Partial Public Class Auto_Meteo_Opad
    Inherits Auto_Meteo_Base

    Public Overrides ReadOnly Property Nazwa As String = Vblib.ExifSource.AutoMeteoOpad
    Public Overrides ReadOnly Property DymekAbout As String = "Dane meteo - opad (Polska)"

    Public Sub New(sDataFolder As String)
        MyBase.New(sDataFolder)
    End Sub

    'Public Sub New(dataFolder As String)
    '    _cacheDataFolder = IO.Path.Combine(dataFolder, "AutoMeteoOpad")
    '    IO.Directory.CreateDirectory(_cacheDataFolder) ' nie ma exception gdy istnieje
    'End Sub

    Public Overrides Async Function GetForFile(oFile As Vblib.OnePic) As Task(Of Vblib.ExifTag)
        If oFile.GetMostProbablyDate(True).Year < 1950 Then Return Nothing
        Dim oGeo As pkar.BasicGeopos = oFile.GetGeoTag
        If oGeo Is Nothing Then Return Nothing
        If Not oGeo.IsInsidePoland Then Return Nothing

        Dim opad As Vblib.Meteo_Opad = ConstructOpadData(oFile.GetMostProbablyDate(True), oGeo)
        If opad Is Nothing Then Return Nothing

        Dim oExif As New Vblib.ExifTag(Vblib.ExifSource.AutoMeteoOpad)
        oExif.MeteoOpad = opad

        Return oExif

    End Function


    ' pliki trzymamy w postaci ZIP - 72 kB vs 1025 kB (dla 2001.01)

    Private Function GetCacheZipFileNameForDate(dlaDaty As Date) As String
        If dlaDaty.Year < 1950 Then Return ""

        If dlaDaty.Year > 2000 Then
            Return IO.Path.Combine(_cacheDataFolder, dlaDaty.ToString("yyyy_MM") & "_o.zip")
        End If

        Return IO.Path.Combine(_cacheDataFolder, dlaDaty.ToString("yyyy") & "_o.zip")

    End Function

    Private Function GetZipFileUrlForDate(dlaDaty As Date) As String
        If dlaDaty.Year < 1950 Then Return ""

        Dim uribase As String = "https://danepubliczne.imgw.pl/data/dane_pomiarowo_obserwacyjne/dane_meteorologiczne/dobowe/opad/"

        If dlaDaty.Year > 2000 Then

            Return uribase & "/" & dlaDaty.Year & "/" & dlaDaty.ToString("yyyy_MM") & "_o.zip"
        End If

        If dlaDaty.Year > 1955 Then
            Dim rokMax As Integer = 1960
            While rokMax < dlaDaty.Year
                rokMax += 5
            End While

            Return uribase & "/" & rokMax - 4 & "-" & rokMax & "/" & dlaDaty.ToString("yyyy_MM") & "_o.zip"
        End If

        Return uribase & "/1950-1955/" & dlaDaty.ToString("yyyy") & "_o.zip"

    End Function


    Private Function ReadFile(dlaDaty As Date) As Boolean

        ' próbujemy z cache, jak nie ma - to ściągamy z Internetu
        Dim sCachedFile As String = GetCacheZipFileNameForDate(dlaDaty)
        If Not IO.File.Exists(sCachedFile) Then
            Dim sUri As String = GetZipFileUrlForDate(dlaDaty)

            Dim client As New WebClient
            client.DownloadFile(New Uri(sUri), sCachedFile)
        End If

        If Not IO.File.Exists(sCachedFile) Then Return False    ' nieudane ściągnięcie pliku

        ' mamy plik ZIP, to wypakowujemy
        Dim oArchive As IO.Compression.ZipArchive = IO.Compression.ZipFile.OpenRead(sCachedFile)
        For Each oInArch As IO.Compression.ZipArchiveEntry In oArchive.Entries
            If Not IO.Path.GetExtension(oInArch.Name).EqualsCI(".csv") Then Continue For

            ' mamy CSV (właściwie mogłoby być bez testowania, bo tam nic innego być nie powinno)
            Using oStreamTemp As Stream = oInArch.Open
                Using txtRdr As New StreamReader(oStreamTemp)

                    Dim csvConfig As New CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
                    csvConfig.HasHeaderRecord = False

                    Using csvRdr As New CsvHelper.CsvReader(txtRdr, csvConfig)
                        _dataMonth = csvRdr.GetRecords(Of Meteo_Opad_Cache).ToList
                    End Using
                End Using
            End Using

            Exit For
        Next

        _data = dlaDaty

        Return True
    End Function

    ''' <summary>
    '''  uzupełnia Meteo_Opad_Cache o pole oItem.geopos, wedle _dataMonth
    ''' </summary>
    Private Sub UzupelnijWspolrzedne()
        ' uzupełnij współrzędne w dataMonth wedle słownika
        Dim prev As String = ""
        For Each oItem As Meteo_Opad_Cache In _dataMonth
            Dim key As String = GetKodMeteoFromKodStacji(oItem.Kod_stacji)
            If String.IsNullOrWhiteSpace(key) Then
                Vblib.DumpMessage($"brak kodu w słowniku ({oItem.Kod_stacji})")
                Continue For
            End If

            'Dim key As String = oItem.Nazwa_stacji.ToUpperInvariant
            If Geo_Stacje_Opad.Stacje.ContainsKey(key) Then
                Geo_Stacje_Opad.Stacje.TryGetValue(key, oItem.geopos)
                If prev <> key Then
                    Vblib.DumpMessage($"dla stacji {oItem.Nazwa_stacji} dodaje geo {oItem.geopos.DumpAsJson}")
                    prev = key
                End If
            End If
        Next
    End Sub

    Private Sub DumpBezGeo()

        Dim braki As String = ""

        For Each oItem As Meteo_Opad_Cache In _dataMonth
            If oItem.geopos IsNot Nothing Then Continue For

            Dim nazwa As String = "|" & oItem.Nazwa_stacji.ToUpperInvariant & "|"
            If Not braki.Contains(nazwa) Then Vblib.DumpMessage("brak geo dla stacji " & nazwa)
        Next

    End Sub

    Private Function ReadData(dlaDaty As Date) As Boolean

        If dlaDaty.Year < 1950 Then Return False ' nie ma danych dla takiego roku

        If dlaDaty.Year < 2001 Then
            If _data.Year = dlaDaty.Year Then Return True   ' ten sam rok, więc już jest wczytane (starsze dane)
        End If

        If _data.Year = dlaDaty.Year AndAlso _data.Month = dlaDaty.Month Then Return True   ' ten sam rok i miesiąc, więc już jest wczytane (nowsze dane)

        If Not ReadFile(dlaDaty) Then Return False
        UzupelnijWspolrzedne()

        DumpBezGeo

        Return True
    End Function

    Private Function ConstructOpadData(oData As Date, oGeo As pkar.BasicGeopos) As Vblib.Meteo_Opad
        If Not ReadData(oData) Then Return Nothing

        ' pusty element
        Dim oOpad As New Vblib.Meteo_Opad With {
            .SumaDobowaOpadowMM = -1,
        .RodzajOpadu = "",
        .WysokPokrywySnieznejCM = -1,
        .WysokSwiezoSpadlegoSnieguCM = -1,
        .GatunekSniegu = "",
        .RodzajPokrywySnieznej = ""
        }

        ' odległości aktualne
        Dim sdb As Integer = Integer.MaxValue
        Dim wsp As Integer = Integer.MaxValue
        Dim wsss As Integer = Integer.MaxValue
        Dim gs As Integer = Integer.MaxValue
        Dim rps As Integer = Integer.MaxValue

        Dim minodl As Integer = Integer.MaxValue
        Dim minNazwa As String = ""

        Dim bBylo As Boolean = False

        For Each oCache As Meteo_Opad_Cache In _dataMonth
            If oCache.Rok <> oData.Year Then Continue For
            If oCache.Miesiac <> oData.Month Then Continue For
            If oCache.Dzien <> oData.Day Then Continue For

            Vblib.DumpMessage("Trying " & oCache.Nazwa_stacji)
            If oCache.geopos Is Nothing Then
                Vblib.DumpMessage(" - ale nie ma geotag")
                Continue For
            End If

            Dim dist As Double = oGeo.DistanceKmTo(oCache.geopos)
            If dist < minodl Then
                minodl = dist
                minNazwa = oCache.Nazwa_stacji
            End If

            If dist > 25 Then
                Vblib.DumpMessage($" - ale za daleko ({dist})")
                Continue For
            End If

            bBylo = True

            ' dla każdego parametru może być inaczej - bo różne dane z różnych stacji są!
            If dist < sdb Then
                If oCache.Status_pomiaru_SMDB <> "8" Then
                    sdb = dist
                    oOpad.SumaDobowaOpadowMM = oCache.Suma_dobowa_opadow
                    oOpad.RodzajOpadu = oCache.Rodzaj_opadu
                End If
            End If

            If dist < wsp Then
                If oCache.Status_pomiaru_PKSN <> "8" Then
                    wsp = dist
                    oOpad.WysokPokrywySnieznejCM = oCache.Wysokosc_pokrywy_snieznej
                End If
            End If

            If dist < wsss Then
                If oCache.Status_pomiaru_HSS <> "8" Then
                    wsss = dist
                    oOpad.WysokSwiezoSpadlegoSnieguCM = oCache.Wysokosc_swiezo_spadlego_sniegu
                End If
            End If

            If dist < gs Then
                If oCache.Status_pomiaru_GATS <> "8" Then
                    gs = dist
                    oOpad.GatunekSniegu = oCache.Gatunek_sniegu
                End If
            End If

            If dist < rps Then
                If oCache.Status_pomiaru_RPSN <> "8" Then
                    rps = dist
                    oOpad.RodzajPokrywySnieznej = oCache.Rodzaj_pokrywy_snieznej
                End If
            End If

        Next

        If Not bBylo Then
            Vblib.DumpMessage($"Nie ma, najblizsza stacja {minNazwa} w odległości {minodl} km")
            Return Nothing
        End If

        Return oOpad

    End Function


    Private _data As Date
    Private _dataMonth As List(Of Meteo_Opad_Cache)

    ' "249180020","WARSZOWICE","2001","02","01",.3,"","S",0,"8",0,"8","","8","","8"
    Protected Class Meteo_Opad_Cache
        <CsvHelper.Configuration.Attributes.Index(0)>
        Public Property Kod_stacji As String
        <CsvHelper.Configuration.Attributes.Index(1)>
        Public Property Nazwa_stacji As String
        <CsvHelper.Configuration.Attributes.Index(2)>
        Public Property Rok As String
        <CsvHelper.Configuration.Attributes.Index(3)>
        Public Property Miesiac As String
        <CsvHelper.Configuration.Attributes.Index(4)>
        Public Property Dzien As String
        <CsvHelper.Configuration.Attributes.Index(5)>
        Public Property Suma_dobowa_opadow As Double ' mm
        <CsvHelper.Configuration.Attributes.Index(6)>
        Public Property Status_pomiaru_SMDB As String
        <CsvHelper.Configuration.Attributes.Index(7)>
        Public Property Rodzaj_opadu As String
        <CsvHelper.Configuration.Attributes.Index(8)>
        Public Property Wysokosc_pokrywy_snieznej As Double 'cm
        <CsvHelper.Configuration.Attributes.Index(9)>
        Public Property Status_pomiaru_PKSN As String
        <CsvHelper.Configuration.Attributes.Index(10)>
        Public Property Wysokosc_swiezo_spadlego_sniegu As Double ' cm
        <CsvHelper.Configuration.Attributes.Index(11)>
        Public Property Status_pomiaru_HSS As String
        <CsvHelper.Configuration.Attributes.Index(12)>
        Public Property Gatunek_sniegu As String
        <CsvHelper.Configuration.Attributes.Index(13)>
        Public Property Status_pomiaru_GATS As String
        <CsvHelper.Configuration.Attributes.Index(14)>
        Public Property Rodzaj_pokrywy_snieznej As String
        <CsvHelper.Configuration.Attributes.Index(15)>
        Public Property Status_pomiaru_RPSN As String

        <CsvHelper.Configuration.Attributes.Ignore>
        Public Property geopos As pkar.BasicGeopos
    End Class



End Class


' wyciąganie danych z najblizszego z istnieniem - ale z dokładnością do pomiaru, a nie rekordu
' https://danepubliczne.imgw.pl/data/dane_pomiarowo_obserwacyjne/dane_meteorologiczne/dobowe/opad/


' "249180020","WARSZOWICE","2001","02","01",.3,"","S",0,"8",0,"8","","8","","8"




' kod: 8 brak pomiaru, 9 brak zjawiska
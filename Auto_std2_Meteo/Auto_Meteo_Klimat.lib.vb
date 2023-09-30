

Imports System.Globalization
Imports System.IO
Imports System.Net
Imports System.Linq
Imports pkar.DotNetExtensions

Partial Public Class Auto_Meteo_Klimat
    Inherits Auto_Meteo_Base

    Public Overrides ReadOnly Property Nazwa As String = Vblib.ExifSource.AutoMeteoKlimat
    Public Overrides ReadOnly Property DymekAbout As String = "Dane meteo - klimat (Polska)"

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

        Dim meteo As Vblib.Meteo_Klimat = ConstructMeteoData(oFile.GetMostProbablyDate(True), oGeo)
        If meteo Is Nothing Then Return Nothing

        Dim oExif As New Vblib.ExifTag(Vblib.ExifSource.AutoMeteoKlimat)
        oExif.MeteoKlimat = meteo

        Return oExif

    End Function


    ' pliki trzymamy w postaci ZIP - 72 kB vs 1025 kB (dla 2001.01)

    Private Function GetCacheZipFileNameForDate(dlaDaty As Date) As String
        If dlaDaty.Year < 1950 Then Return ""

        If dlaDaty.Year > 2000 Then
            Return IO.Path.Combine(_cacheDataFolder, dlaDaty.ToString("yyyy_MM") & "_k.zip")
        End If

        Return IO.Path.Combine(_cacheDataFolder, dlaDaty.ToString("yyyy") & "_k.zip")

    End Function

    Private Function GetZipFileUrlForDate(dlaDaty As Date) As String
        If dlaDaty.Year < 1951 Then Return ""

        Dim uribase As String = "https://danepubliczne.imgw.pl/data/dane_pomiarowo_obserwacyjne/dane_meteorologiczne/dobowe/klimat/"

        If dlaDaty.Year > 2000 Then

            Return uribase & "/" & dlaDaty.Year & "/" & dlaDaty.ToString("yyyy_MM") & "_k.zip"
        End If

        If dlaDaty.Year > 1955 Then
            Dim rokMax As Integer = 1960
            While rokMax < dlaDaty.Year
                rokMax += 5
            End While

            Return uribase & "/" & rokMax - 4 & "-" & rokMax & "/" & dlaDaty.ToString("yyyy_MM") & "_k.zip"
        End If

        Return uribase & "/1951-1955/" & dlaDaty.ToString("yyyy") & "_k.zip"

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

            Using oStreamTemp As Stream = oInArch.Open
                Using txtRdr As New StreamReader(oStreamTemp)

                    Dim csvConfig As New CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
                    csvConfig.HasHeaderRecord = False

                    Using csvRdr As New CsvHelper.CsvReader(txtRdr, csvConfig)
                        If oInArch.Name.ContainsCI("k_d_t") Then
                            ' plik z T
                            _dataMonth_KT = csvRdr.GetRecords(Of Meteo_KlimatT_Cache).ToList
                        ElseIf oInArch.Name.ContainsCI("k_d") Then
                            ' plik bez T
                            _dataMonth_K = csvRdr.GetRecords(Of Meteo_Klimat_Cache).ToList
                        End If
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
        For Each oItem As Meteo_Klimat_Cache In _dataMonth_K
            Dim key As String = GetKodMeteoFromKodStacji(oItem.Kod_stacji)
            If String.IsNullOrWhiteSpace(key) Then
                Vblib.DumpMessage($"brak kodu w słowniku ({oItem.Kod_stacji})")
                Continue For
            End If

            'Dim key As String = oItem.Nazwa_stacji.ToUpperInvariant
            If Geo_Stacje_Klimat.Stacje.ContainsKey(key) Then
                Geo_Stacje_Klimat.Stacje.TryGetValue(key, oItem.geopos)
                If prev <> key Then
                    Vblib.DumpMessage($"dla stacji {oItem.Nazwa_stacji} dodaje geo {oItem.geopos.DumpAsJson}")
                    prev = key
                End If
            End If
        Next

        prev = ""
        For Each oItem As Meteo_KlimatT_Cache In _dataMonth_KT
            Dim key As String = GetKodMeteoFromKodStacji(oItem.Kod_stacji)
            If String.IsNullOrWhiteSpace(key) Then
                Vblib.DumpMessage($"brak kodu w słowniku ({oItem.Kod_stacji})")
                Continue For
            End If

            'Dim key As String = oItem.Nazwa_stacji.ToUpperInvariant
            If Geo_Stacje_Klimat.Stacje.ContainsKey(key) Then
                Geo_Stacje_Klimat.Stacje.TryGetValue(key, oItem.geopos)
                If prev <> key Then
                    Vblib.DumpMessage($"dla stacji {oItem.Nazwa_stacji} dodaje geo {oItem.geopos.DumpAsJson}")
                    prev = key
                End If
            End If
        Next


    End Sub

    Private Sub DumpBezGeo()

        Dim braki As String = ""

        For Each oItem As Meteo_Klimat_Cache In _dataMonth_K
            If oItem.geopos IsNot Nothing Then Continue For

            Dim nazwa As String = "|" & oItem.Nazwa_stacji.ToUpperInvariant & "|"
            If Not braki.Contains(nazwa) Then Vblib.DumpMessage("brak geo dla stacji klimat " & nazwa)
        Next

        braki = ""

        For Each oItem As Meteo_KlimatT_Cache In _dataMonth_KT
            If oItem.geopos IsNot Nothing Then Continue For

            Dim nazwa As String = "|" & oItem.Nazwa_stacji.ToUpperInvariant & "|"
            If Not braki.Contains(nazwa) Then Vblib.DumpMessage("brak geo dla stacji klimatT " & nazwa)
        Next


    End Sub

    Private Function ReadData(dlaDaty As Date) As Boolean

        If dlaDaty.Year < 1951 Then Return False ' nie ma danych dla takiego roku

        If dlaDaty.Year < 2001 Then
            If _data.Year = dlaDaty.Year Then Return True   ' ten sam rok, więc już jest wczytane (starsze dane)
        End If

        If _data.Year = dlaDaty.Year AndAlso _data.Month = dlaDaty.Month Then Return True   ' ten sam rok i miesiąc, więc już jest wczytane (nowsze dane)

        If Not ReadFile(dlaDaty) Then Return False
        UzupelnijWspolrzedne()

        DumpBezGeo()

        Return True
    End Function

    Private Function ConstructMeteoData(oData As Date, oGeo As pkar.BasicGeopos) As Vblib.Meteo_Klimat
        If Not ReadData(oData) Then Return Nothing

        ' pusty element
        Dim oKlimat As New Vblib.Meteo_Klimat With {
                .TempMax = -1,
    .TempMin = -1,
        .TempAvg = -1,
    .TempMinGrunt = -1,
    .SumaOpadowMM = -1,
    .RodzajOpadu = -1,
    .WysokPokrywySnieznejCM = -1,
        .HigroAvg = -1,
    .WindSpeedAvgMS = -1,
    .ZachmurzenieOktantyAvg = -1
        }

        ' odległości aktualne
        Dim PKSN As Integer = Integer.MaxValue
        Dim SMDB As Integer = Integer.MaxValue
        Dim TMAX As Integer = Integer.MaxValue
        Dim TMIN As Integer = Integer.MaxValue
        Dim TMNG As Integer = Integer.MaxValue
        Dim STD As Integer = Integer.MaxValue

        Dim minodl As Integer = Integer.MaxValue
        Dim minNazwa As String = ""

        Dim bBylo As Boolean = False

        For Each oCache As Meteo_Klimat_Cache In _dataMonth_K
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

            If dist < SMDB Then
                If oCache.Status_pomiaru_SMDB <> "8" Then
                    SMDB = dist
                    oKlimat.SumaOpadowMM = oCache.Suma_dobowa_opadow
                    oKlimat.RodzajOpadu = oCache.Rodzaj_opadu
                End If
            End If

            If dist < PKSN Then
                If oCache.Status_pomiaru_PKSN <> "8" Then
                    PKSN = dist
                    oKlimat.WysokPokrywySnieznejCM = oCache.Wysokosc_pokrywy_snieznej
                End If
            End If

            If dist < TMAX Then
                If oCache.Status_pomiaru_TMAX <> "8" Then
                    TMAX = dist
                    oKlimat.TempMax = oCache.Maksymalna_temperatura_dobowa
                End If
            End If

            If dist < TMIN Then
                If oCache.Status_pomiaru_TMIN <> "8" Then
                    TMIN = dist
                    oKlimat.TempMin = oCache.Minimalna_temperatura_dobowa
                End If
            End If

            If dist < TMNG Then
                If oCache.Status_pomiaru_TMNG <> "8" Then
                    TMNG = dist
                    oKlimat.TempMinGrunt = oCache.Temperatura_minimalna_przy_gruncie
                End If
            End If

            If dist < STD Then
                If oCache.Status_pomiaru_STD <> "8" Then
                    STD = dist
                    oKlimat.TempAvg = oCache.Srednia_temperatura_dobowa
                End If
            End If

        Next


        Dim WLGS As Integer = Integer.MaxValue
        Dim FWS As Integer = Integer.MaxValue
        Dim NOS As Integer = Integer.MaxValue

        For Each oCache As Meteo_KlimatT_Cache In _dataMonth_KT
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

            minodl = Integer.MaxValue
            minNazwa = ""

            If dist < WLGS Then
                If oCache.Status_pomiaru_WLGS <> "8" Then
                    WLGS = dist
                    oKlimat.HigroAvg = oCache.Srednia_dobowa_wilgotnosc_wzgledna
                End If
            End If

            If dist < FWS Then
                If oCache.Status_pomiaru_FWS <> "8" Then
                    FWS = dist
                    oKlimat.WindSpeedAvgMS = oCache.Srednia_dobowa_predkosc_wiatru
                End If
            End If

            If dist < NOS Then
                If oCache.Status_pomiaru_NOS <> "8" Then
                    NOS = dist
                    oKlimat.ZachmurzenieOktantyAvg = oCache.Srednie_dobowe_zachmurzenie_ogolne
                End If
            End If

        Next

        If Not bBylo Then
            Vblib.DumpMessage($"Nie ma, najblizsza stacja {minNazwa} w odległości {minodl} km")
            Return Nothing
        End If

        Return oKlimat

    End Function


    Private _data As Date
    Private _dataMonth_K As List(Of Meteo_Klimat_Cache)
    Private _dataMonth_KT As List(Of Meteo_KlimatT_Cache)

    ' "249180020","WARSZOWICE","2001","02","01",.3,"","S",0,"8",0,"8","","8","","8"
    Protected Class Meteo_Klimat_Cache 'Meteo_KlimatT_Cache
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
        Public Property Maksymalna_temperatura_dobowa As Double
        <CsvHelper.Configuration.Attributes.Index(6)>
        Public Property Status_pomiaru_TMAX As String
        <CsvHelper.Configuration.Attributes.Index(7)>
        Public Property Minimalna_temperatura_dobowa As Double
        <CsvHelper.Configuration.Attributes.Index(8)>
        Public Property Status_pomiaru_TMIN As String
        <CsvHelper.Configuration.Attributes.Index(9)>
        Public Property Srednia_temperatura_dobowa As Double
        <CsvHelper.Configuration.Attributes.Index(10)>
        Public Property Status_pomiaru_STD As String
        <CsvHelper.Configuration.Attributes.Index(11)>
        Public Property Temperatura_minimalna_przy_gruncie As Double
        <CsvHelper.Configuration.Attributes.Index(12)>
        Public Property Status_pomiaru_TMNG As String
        <CsvHelper.Configuration.Attributes.Index(13)>
        Public Property Suma_dobowa_opadow As Double
        <CsvHelper.Configuration.Attributes.Index(14)>
        Public Property Status_pomiaru_SMDB As String
        <CsvHelper.Configuration.Attributes.Index(15)>
        Public Property Rodzaj_opadu As String
        <CsvHelper.Configuration.Attributes.Index(16)>
        Public Property Wysokosc_pokrywy_snieznej As Double
        <CsvHelper.Configuration.Attributes.Index(17)>
        Public Property Status_pomiaru_PKSN As String

        <CsvHelper.Configuration.Attributes.Ignore>
        Public Property geopos As pkar.BasicGeopos
    End Class

    Protected Class Meteo_KlimatT_Cache
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
        Public Property Srednia_dobowa_temperatura As Double
        <CsvHelper.Configuration.Attributes.Index(6)>
        Public Property Status_pomiaru_TEMP As String
        <CsvHelper.Configuration.Attributes.Index(7)>
        Public Property Srednia_dobowa_wilgotnosc_wzgledna As Double
        <CsvHelper.Configuration.Attributes.Index(8)>
        Public Property Status_pomiaru_WLGS As String
        <CsvHelper.Configuration.Attributes.Index(9)>
        Public Property Srednia_dobowa_predkosc_wiatru As Double
        <CsvHelper.Configuration.Attributes.Index(10)>
        Public Property Status_pomiaru_FWS As String
        <CsvHelper.Configuration.Attributes.Index(11)>
        Public Property Srednie_dobowe_zachmurzenie_ogolne As Double
        <CsvHelper.Configuration.Attributes.Index(12)>
        Public Property Status_pomiaru_NOS As String


        <CsvHelper.Configuration.Attributes.Ignore>
        Public Property geopos As pkar.BasicGeopos
    End Class


End Class



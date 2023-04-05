Imports System.Globalization
Imports System.IO
Imports System.Net

Public MustInherit Class Auto_Meteo_Base
    Inherits Vblib.AutotaggerBase

    Public Overrides ReadOnly Property Typek As Vblib.AutoTaggerType = Vblib.AutoTaggerType.WebPublic
    Public Overrides ReadOnly Property MinWinVersion As String = "7.0"
    Public Overrides ReadOnly Property includeMask As String = "*.*"

    Protected _cacheDataFolder As String

    Public Sub New(dataFolder As String)
        _cacheDataFolder = IO.Path.Combine(dataFolder, "AutoMeteo")
        IO.Directory.CreateDirectory(_cacheDataFolder) ' nie ma exception gdy istnieje
    End Sub

    Protected Sub SciagnijSlownikKodow()

        Dim sSlownikPath As String = IO.Path.Combine(_cacheDataFolder, "wykaz_stacji.csv")
        Dim bSciagnij As Boolean = False

        If Not IO.File.Exists(sSlownikPath) Then
            bSciagnij = True
        Else
            If IO.File.GetLastWriteTime(sSlownikPath).AddDays(14) < Date.Now Then bSciagnij = True
        End If

        If bSciagnij Then
            Dim client As New WebClient
            Dim sUri As String = "https://danepubliczne.imgw.pl/data/dane_pomiarowo_obserwacyjne/dane_meteorologiczne/wykaz_stacji.csv"
            client.DownloadFile(New Uri(sUri), sSlownikPath)
        End If

        Using txtRdr As New StreamReader(sSlownikPath)
            Dim csvConfig As New CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
            csvConfig.HasHeaderRecord = False
            Using csvRdr As New CsvHelper.CsvReader(txtRdr, csvConfig)
                _slownik = csvRdr.GetRecords(Of Meteo_Stacje_Cache).ToList
            End Using
        End Using

    End Sub

    Private Shared _slownik As List(Of Meteo_Stacje_Cache)

    Protected Function GetKodMeteoFromKodStacji(sKodFromCsv As String) As String
        If _slownik Is Nothing Then
            SciagnijSlownikKodow()
        End If

        For Each oItem As Meteo_Stacje_Cache In _slownik
            If oItem.Kod_stacji = sKodFromCsv Then Return oItem.Kod_Imgw
        Next

        Return ""
    End Function

    Private Class Meteo_Stacje_Cache
        <CsvHelper.Configuration.Attributes.Index(0)>
        Public Property Kod_stacji As String

        '<CsvHelper.Configuration.Attributes.Index(1)>
        'Public Property Nazwa As String

        <CsvHelper.Configuration.Attributes.Index(2)>
        Public Property Kod_Imgw As String

    End Class

End Class

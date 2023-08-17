

'EXIF 2.32(2010.04.26)
'https://www.cipa.jp/std/documents/e/DC-X008-Translation-2019-E.pdf


Imports System.IO
Imports pkar
''' <summary>
''' Tags that can be added to file
''' </summary>
Public Class ExifTag
    Inherits pkar.BaseStruct

    ' najpierw te, które mogą być narzucone przez SOURCE_DIR

    Public Property ExifSource As String ' ExifSource.SourceFile, ...
    Public Property FileSourceDeviceType As FileSourceDeviceTypeEnum
    Public Property Author As String
    Public Property Copyright As String
    ' Public Property CameraMaker As String
    Public Property CameraModel As String

    ' daty, które mają różne znaczenie w różnych kontekstach
    ' dla pliku kopiowanego z aparatu via explorer: CREATE = data skopiowania, MODIFY = data z aparatu (wczesniejsza)
    Public Property DateMin As DateTime     ' min i max data, jeśli nie mamy pełnej daty (np. "na pewno po 1943, bo jest tata, i na pewno przed 1955, bo wtedy most odbudowano)
    Public Property DateMax As DateTime


    ' te, które mogą być z nazwy pliku ExifSource.SourceFile

    ' + ExifSource, FileSourceDeviceType, DateMin, DateMax (tu: min/max data CreateTime/ModifyTime)
    Public Property DateTimeOriginal As String


    ' ExifSource.SourceFile

    ' + ExifSource
    Public Property DateTimeScanned As String


    ' z innych źródeł (czyli już zawsze, bo SOURCE_EXIF)
    Public Property Keywords As String  ' ImageDescription (only ASCII)
    Public Property UserComment As String  ' UserComment, 9286
    Public Property Restrictions As String ' 0x9blic 212 SecurityClassification string ExifIFD (C/R/S/T/U), do "tajne" :) (ale jest tez non-writable, 0xa212)
    Public Property Orientation As OrientationEnum  ' do usuwania z pliku, bo jego rotate podczas import?
    Public Property PicGuid As String   ' 0xA420 ImageUniqueID ASCII!
    Public Property ReelName As String   ' 0xc789	ReelName	string	IFD0
    Public Property GeoTag As pkar.BasicGeopos    ' 0x87b1	GeoTiffAsciiParams IFD0 (string)
    Public Property GeoName As String ' GeoTiffAsciiParams
    Public Property GeoZgrubne As Boolean = False
    Public Property OriginalRAW As String   ' Tag 0xc68b (9 bytes, string[9])

    'Public Property AlienTags As List(Of String)    ' importowane z różnych miejsc, autorozpoznawanie -> ExifSource

    Public Property AzureAnalysis As MojeAzure
    Public Property PogodaAstro As CacheAutoWeather_Item
    Public Property MeteoOpad As Meteo_Opad
    Public Property MeteoKlimat As Meteo_Klimat
    Public Property MeteoSynop As Meteo_Synop

    Public Sub New(sSource As String)
        ExifSource = sSource
    End Sub

    'Public Function Clone() As ExifTag
    '    Dim sTxt As String = Newtonsoft.Json.JsonConvert.SerializeObject(Me, Newtonsoft.Json.Formatting.Indented)
    '    Dim oNew As ExifTag = Newtonsoft.Json.JsonConvert.DeserializeObject(sTxt, GetType(ExifTag))
    '    Return oNew
    'End Function

End Class

Partial Public Module Extensions

    ''' <summary>
    ''' sprawdza czy data jest z zakresu 1800..2100 (resztę do zdjęć uznaję za błędne)
    ''' </summary>
    ''' <returns></returns>
    <Runtime.CompilerServices.Extension()>
    Public Function IsDateValid(ByVal oDate1 As Date) As Boolean
        If oDate1.Year < 1800 Then Return False
        If oDate1.Year > 2100 Then Return False
        Return True
    End Function


    ''' <summary>
    ''' zwraca datę tekstowo, w formacie Exif 2.3
    ''' </summary>
    ''' <param name="oDate"></param>
    ''' <returns></returns>
    ' <Runtime.CompilerServices.Extension()>
    ' Public Function ToExifString(ByVal oDate As Date) As String
    '     Return oDate.ToString("yyyy.MM.dd HH:mm:ss")
    ' End Function


    '''' <summary>
    '''' porównuje dwie daty, uznając za "poprawniejszą" pierwszą, a drugą za valid tylko gdy rok 1800..2100
    '''' </summary>
    '''' <param name="oDate1"></param>
    '''' <param name="oDate2"></param>
    '''' <returns></returns>
    '<Runtime.CompilerServices.Extension()>
    'Public Function DateMin(ByVal oDate1 As Date, oDate2 As Date) As Date
    '    If Not oDate2.IsDateValid Then Return oDate1

    '    If oDate2 < oDate1 Then Return oDate2
    '    Return oDate1
    'End Function

    '''' <summary>
    '''' porównuje dwie daty, uznając za "poprawniejszą" pierwszą, a drugą za valid tylko gdy rok 1800..2100
    '''' </summary>
    '''' <param name="oDate1"></param>
    '''' <param name="oDate2"></param>
    '''' <returns></returns>
    '<Runtime.CompilerServices.Extension()>
    'Public Function DateMax(ByVal oDate1 As Date, oDate2 As Date) As Date
    '    If Not oDate2.IsDateValid Then Return oDate1

    '    If oDate2 > oDate1 Then Return oDate2
    '    Return oDate1
    'End Function

    ' to nie może być w Nuget, bo mamy tu specjalne traktowanie dla "=" oraz "-"

    <Runtime.CompilerServices.Extension()>
    Public Function ConcatenateWithComma(ByVal sFirstString As String, sSecondString As String) As String
        Return sFirstString.ConcatenateWithSeparator(sSecondString, ", ")
    End Function

    <Runtime.CompilerServices.Extension()>
    Public Function ConcatenateWithPipe(ByVal sFirstString As String, sSecondString As String) As String
        Return sFirstString.ConcatenateWithSeparator(sSecondString, " | ")
    End Function

    <Runtime.CompilerServices.Extension()>
    Public Function ConcatenateWithSeparator(ByVal sFirstString As String, sSecondString As String, sSeparator As String)
        If String.IsNullOrWhiteSpace(sSecondString) Then Return sFirstString
        If sSecondString = "-" Then Return ""
        If sSecondString.StartsWith("==") Then Return sSecondString.Substring(2)

        If sFirstString = "" Then Return sSecondString
        Return sFirstString & sSeparator & sSecondString
    End Function

End Module

Public Class CacheAutoWeather_Item
    Inherits BaseStruct

    Public Property queryCost As Integer
    Public Property latitude As Single
    Public Property longitude As Single
    'Public Property resolvedAddress As String
    'Public Property address As String
    Public Property timezone As String
    'Public Property tzoffset As Double
    Public Property days As List(Of AutoWeatherDay)
    Public Property day As AutoWeatherDay
    'Public Property stations As AutoWeatherStations

    Public Property currentConditions As AutoWeatherHourSingle  ' tylko przy jednogodzinnym
End Class

Public Class AutoWeatherDay
    Inherits AutoWeatherHourCommon

    Public Property tempmax As Double
    Public Property tempmin As Double
    Public Property feelslikemax As Double
    Public Property feelslikemin As Double
    Public Property precipprob As Double
    Public Property precipcover As Double
    'Public Property solarradiation As Single   ' nie w darmowym
    'Public Property solarenergy As Single  ' nie w darmowym
    Public Property sunrise As String ' HH:mm:ss, local time
    Public Property sunriseEpoch As Long ' UTC
    Public Property sunset As String ' HH:mm:ss, local time
    Public Property sunsetEpoch As Long ' UTC
    Public Property moonphase As Double
    Public Property moonrise As String ' HH:mm:ss, local time
    Public Property moonset As String ' HH:mm:ss, local time

    Public Property description As String

    'Public Property stations() As String
    'Public Property source As String
    ' Public Property hours() As AutoWeatherHourInDay ' tylko przy dobowym, przy request dla godziny tego nie ma
End Class

'Public Class AutoWeatherHourInDay
'    Inherits AutoWeatherHourCommon

'    Public Property source As String
'End Class

Public Class AutoWeatherHourSingle
    Inherits AutoWeatherHourCommon

    'Public Property severerisk As Single ' tylko w forecast
    'Public Property sunrise As String  ' jest w dniu
    'Public Property sunriseEpoch As Integer
    'Public Property sunset As String ' jest w dniu
    'Public Property sunsetEpoch As Integer
    'Public Property moonphase As Single ' jest w dniu
End Class

Public Class AutoWeatherHourCommon
    Public Property datetime As String ' HH:mm:ss
    Public Property datetimeEpoch As Integer ' to jest w UTC :) , używam
    Public Property temp As Double
    Public Property feelslike As Double
    Public Property humidity As Double
    Public Property dew As Double
    Public Property precip As Double
    'Public Property precipprob As Integer
    Public Property snow As Double
    Public Property snowdepth As Double
    Public Property preciptype As String()
    Public Property windgust As Double
    Public Property windspeed As Double
    Public Property winddir As Double
    Public Property pressure As Double
    Public Property visibility As Double
    Public Property cloudcover As Double
    Public Property solarradiation As Double  ' nie w darmowej
    Public Property solarenergy As Double ' nie w darmowej
    Public Property uvindex As Double
    Public Property conditions As String
    Public Property icon As String
    'Public Property stations() As String

    Public Property sunhour As Double   ' mój dodatek

End Class




'Image title	ImageDescription ASCII(any)
'Person who created the image	Artist	ASCII(any)
'Copyright holder	Copyright	ASCII(any)
'File change Date And time	DateTime	ASCII(20)

'Dla nie-ASCII To Exif Private tag UserComment

'DateTimeDigitized
'DateTimeOriginal
'UserComment

'GPS:


'Od strony tagów EXIF
'FileSource: 1 - skaner prezzroczysty, 2 - skaner reflex, 3 - cyfrowe
'DateTimeOriginal: dla WP_ z nazwy
'DateTimeDigitized: pliku, minimum w katalogu z TIFF
'Artist: z path
'Copyright: z path
'ImageDescription: numer filmu, ramka, itp., ew. ID z storefile? filename dla TIFFa (bez keywordow - opisów), albo wlasnie z keywordami
'UserComment: osoby, miejsca(z filename brane) - mogą być krótkie (-JA) albo średnie (Piotr) albo długie (Piotr Karocki)
'- ważne że krótkie w miarę powinno być jako całość

'Pytanie co zrobić gdy już coś tam jest - więc na początek zrobić weryfikację czy są To pola puste (picparam w SQL można spróbować)
'- bo można potem zapisywać EXIFa ponownie, już bez dat: aktualizacja ImageDescription/UserComment


'Sub ProcessExif(oFolder)
'    For Each folder In oFolder.folders
'        ProcessExif(folder)
'    Next

'    tiffDate = TryFindDate(oFolder)

'    For Each file In oFolder.files
'        processExif(file, tiffDate)
'    Next
'End Sub

'Sub processExif(oFile, tiffDate)
'    DateTimeDigitized = oFile.DateTimeDigitized
'    If isnull(DateTimeDigitized) Then DateTimeDigitized = tiffDate

'    DateTimeOriginal = oFile.DateTimeOriginal
'    If isnull(DateTimeOriginal) Then
'        If oFile.SourceName.startswith("WP-") Then DateTimeOriginal = oFile.SourceName
'        ' z filename, bo czasem tam są; dla digital - takze data z file.datecreated
'    End If

'    Artist = oFile.Artist
'    If isnull(Artist) Then Artist = PathToArtist(oFile.Path)

'    Copyright = oFile.Copyright
'    If isnull(Copyright) Then Copyright = PathToCopyright(oFile.Path)

'    FileSource = oFile.FileSource
'    If isnull(FileSource) Then FileSource = PathToFileSource(oFile.Path)

'    ImageDescription = oFile.ImageDescription
'    ' tu bez warunku - zawsze wstawiamy?
'    ImageDescription = PathToDescr(oFile.Path)  ' film, ramka, id dla odbitki

'    UserComment = oFile.UserComment
'    ' tez bez warunku?
'    UserComment = NameToComment(oFile.SourceName) ' tagi z filename


'    oFile.ExifSave()


'End Sub


'Function PathToArtist(sPath)
'    If sPath.Contains("dWladziu") Then Return "Wladyslaw Karocki"
'    ...
'End Function

'Function PathToCopyright(sPath)
'    If sPath.Contains("dWladziu") Then Return "(C) Wladyslaw Karocki. All rights reserved."
'    ...
'End Function

'Function PathToFileSource(sPath)
'    If sPath.Contains("cyfrowe") Then Return 3
'    If sPath.Contains("analog") Then Return 1
'    Return 2    ' odbitki, skaner reflect
'    ...
'End Function

'Function TryFindDate(oFolder)
'    oFolder.TIFF
'    oFolder.SourceName & TIFF ale na zewnetrznym nosniku (zeby robic aktualizacje na dysku lokalnym a nie na zewnetrzntm?)


'ImageProperties.PeopleNames 'readonly
'    ImageProperties.Keywords ' .Add, itp.


'About: uuid : faf5bdd5-ba3d - 11Da-ad31-d33d75182f1b - na wp_kefir.jpg


'ExifTool: (podkreslone, czyli oficjalne)
'ImageDescription    IFD0
'Orientation = 1	IFD0
'ModifyDate IFD0(called DateTime by the EXIF spec.)
'Artist IFD0
'Copyright   IFD0(may contain copyright notices For photographer And editor)
'DateTimeOriginal    ExifIFD(Date / time When original image was taken)
'CreateDate  ExifIFD(called DateTimeDigitized by the EXIF spec.)
'UserComment ExifIFD(undef)
'FileSource  ExifIFD
'0xa420 ImageUniqueID	ExifIFD string ASCII(33)



'niepodkreslone:
'0x87b1	GeoTiffAsciiParams IFD0 (string)
'0x9212 SecurityClassification string ExifIFD (C/R/S/T/U), do "tajne" :) (ale jest tez non-writable, 0xa212)
'0x9211 ImageNumber int32u ExifIFD (ale jest tez non-writable, 0xa211)
'0xc789	ReelName	string	IFD0
'0xc68b	OriginalRawFileName	string!	IFD0
'0xc614	UniqueCameraModel	string	IFD0 (dla Certo i Belplasca mozna to zrobic, takze Lumia)

'FileSource: 1 - skaner prezzroczysty, 2 - skaner reflex, 3 - cyfrowe


'gdy Software = "Windows 10" To wtedy Do Camera mozna wstawiac "Lumia", ale sprawdzic czy Lumia 650/550 daje Lumia Do srodka (bo moze daje, i wtedy nie trzeba rozrozniac samemu pomiedzy roznymi telefonami)
'tak, Lumia 650 wpisuje Camera że Lumia 650

'mozna wlasne tagi zdefiniowac, i To byloby dobre może? Ale w ExifTool To mozna zrobic, nie wiem jak we wlasnym programie

'Pytanie co zrobić gdy już coś tam jest - więc na początek zrobić weryfikację czy są to pola puste (picparam w SQL można spróbować)
'- bo można potem zapisywać EXIFa ponownie, już bez dat: aktualizacja ImageDescription / UserComment


'IDpliku w bazie zapisany w EXIF pozwala potem na rename jakieś, które działa takze w StoreFiles podmieniając nazwy

'mając StoreFiles, mozna zrobic testy działania (wymyślania co wpisać), bo przeciez są tam wszystkie nazwy plików

'Tags -tak samo jak miałem w tym pliku, -JA i takze #miejsce (tak jak było planowane)



'IDy ramek
'[A].a[T|M|W|P|A].t[N|P|O].f[filmID].[ramkaId]
'[A].a[T|M|W|P|A].t[N|P|O].p[pudelkoID].[odbitkaID]

'[D].a[T|P|A|AK|x].f[folderID].[fotkaID]

'analog/ digital
'autor: tata, dM, dW, ja, aska, X - unknown, AK - Anka, MK - Marcin Kurcz, 8WL - SP, Wojtek Lewandowski, 5MD - 5 LO, Marcin Dziurzynski
'typ: negatyw, pozytyw, odbitka
'filmID, a dla odbitek: id pudelka
'ramkaID, a dla odbitek: id w pudelku (co nie znaczy ze nie moze zostac tak jak jest teraz, ciagla numeracja)
'folderID: id katalogu, jakis prosty (typu sygnatura, 0-Krakow, 01-Krakow, DniMiasta, itp.; albo szukanie poprzez bazę danych, i tylko numer kolejny)

'Osoby do zaznaczania: combo, z filtrem, DODAJ; drzewko hierarchiczne (rodzina, SP, LO) ?


' https://www.codeproject.com/Articles/5251929/CompactExifLib-Access-to-EXIF-Tags-in-JPEG-TIFF-an



Imports System.Dynamic
Imports System.Globalization
Imports System.IO
Imports pkar.DotNetExtensions

Public Class AutoTag_EXIF
    Inherits AutotaggerBase

    Public Overrides ReadOnly Property Typek As Vblib.AutoTaggerType = Vblib.AutoTaggerType.Local
    Public Overrides ReadOnly Property Nazwa As String = "AUTO_EXIF"
    Public Overrides ReadOnly Property MinWinVersion As String = "7.0"
    Public Overrides ReadOnly Property DymekAbout As String = "Wczytuje znaczniki EXIF z pliku zdjęcia"
    Public Shared ReadOnly Property includeMask As String = "*.jpg;*.jpg.thumb;*.mov;*.mp4;*.avi;*.nar"

    ' *TODO* dla NAR (Lumia950), MP4 (Lumia*), AVI (Fuji), MOV (iPhone) są specjalne obsługi


#Disable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
    Public Overrides Async Function GetForFile(oFile As Vblib.OnePic) As Task(Of Vblib.ExifTag)
#Enable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
        If Not oFile.MatchesMasks(includeMask) Then Return Nothing

        ' najpierw to, co umie CompactExif
        If oFile.MatchesMasks("*.jpg;*.jpg.thumb;*.tif;*.tiff;*.png") Then Return GetForFileCompact(oFile)

        ' teraz NAR - wyciągnięcie pliku ze środka
        If oFile.MatchesMasks("*.nar") Then Return GetForNARCompact(oFile)

        ' filmy: mov, mp4
        If oFile.MatchesMasks("*.mp4;*.mov;*.avi") Then Return GetForMovieFile(oFile)

        ' filmy: avi
        'If oFile.MatchesMasks("*.avi", "") Then Return GetForAviFile(oFile)
        ' AVI title, subtitle, contributing artist, year, media created, copyright, parenting rating

        Return Nothing  ' nie umiemy jeszcze, ale chcemy umieć (bo w includeMask jest że umiemy)
    End Function

    Public Shared Function CanInterpret(oFile As Vblib.OnePic) As Boolean
        Return oFile.MatchesMasks(includeMask)
    End Function

#Region "MetadataExtractor"

    Private Function GetForMovieFile(oFile As Vblib.OnePic) As Vblib.ExifTag
        ' ale to jest tylko extract, bez zapisu - więc trzeba tego potem pilnować (przy postprocesor.save, reset, itp.)

        Dim oNewExif As New Vblib.ExifTag(Nazwa)

        Dim oCos = MetadataExtractor.ImageMetadataReader.ReadMetadata(oFile.InBufferPathName)

        For Each oDir As MetadataExtractor.Directory In MetadataExtractor.ImageMetadataReader.ReadMetadata(oFile.InBufferPathName)
            ' MP4 title, subtitle, tags, comments, contributing artist, year, producers, publisher, media created, copyright, parenting rating
            ' MOV title, subtitle, comments, contributing artist, year, producers, publisher, media created, copyright, parenting rating

            ' teoretycznie dane będą tylko w "QuickTime Metadata Header"

            For Each oTag As MetadataExtractor.Tag In oDir.Tags
                ' *TODO* z filmów mamy tylko wersję uproszczoną - tylko to co jest ustawiane będzie

                ' table 7, D
                ' oNewExif.UserComment = oRdr.GetString(CompactExifLib.ExifTag.UserComment) ' ANY(ANY)
                ' .MOV
                If oTag.Name = "Creation Date" Then
                    Dim sData As String = oTag.Description  ' 2022-07-22T17:29:03+0200  -> "yyyy.MM.dd HH:mm:ss"
                    sData = sData.Replace("-", ".").Replace("T", " ")
                    oNewExif.DateTimeOriginal = sData.Substring(0, 19)
                End If

                ' .MP4
                If oTag.Name = "Created" Then
                    Dim sData As String = oTag.Description  ' Thu Feb 10 15:04:49 2022
                    Dim dData As DateTime
                    If DateTime.TryParseExact(sData, "ddd MMM dd HH:mm:ss yyyy", New CultureInfo("en-US"), Globalization.DateTimeStyles.AllowWhiteSpaces Or Globalization.DateTimeStyles.AssumeLocal, dData) Then
                        oNewExif.DateTimeOriginal = dData.ToExifString
                    Else
                        oNewExif.DateTimeOriginal = sData
                    End If
                End If

                '        oNewExif.DateTimeScanned = oRdr.GetDate(CompactExifLib.ExifTag.DateTimeDigitized) ' ASCII(20)?
                '        ' table 7, H
                '        oNewExif.PicGuid = oRdr.GetString(CompactExifLib.ExifTag.ImageUniqueId) ' 0xA420, ASCII(33)
                '        oNewExif.FileSourceDeviceType = oRdr.GetInt(CompactExifLib.ExifTag.FileSource) ' ANY(1) 41728 = a300

                '        ' TIFF mandatory, EXIF 2.32
                '        ' table 4, A
                '        oNewExif.Orientation = oRdr.GetInt(CompactExifLib.ExifTag.Orientation) ' SHORT

                '        ' table 4, D
                '        oNewExif.Author = oRdr.GetString(CompactExifLib.ExifTag.Artist) ' 315 = 013b ASCII(ANY)
                '        oNewExif.Copyright = oRdr.GetString(CompactExifLib.ExifTag.Copyright) ' 33432 = 8298 ASCII(ANY)
                '        oNewExif.Keywords = oRdr.GetString(CompactExifLib.ExifTag.ImageDescription)  ' tylko ASCII, 010e ASCII(ANY)
                If oTag.Name = "Make" Then oNewExif.CameraModel = AddSecondTagString(oNewExif.CameraModel, oTag.Description, " # ")
                If oTag.Name = "Model" Then oNewExif.CameraModel = AddSecondTagString(oNewExif.CameraModel, oTag.Description, " # ")

                ' GPS, table 15
                If oTag.Name = "GPS Location" Then
                    ' "+50.0798+20.0537/"
                    Dim sVal As String = oTag.Description
                    'Dim iInd As Integer = sVal.IndexOfAny({"+", "-"}, 2)
                    'If iInd > 2 Then
                    '    Dim sLat As String = sVal.Substring(0, iInd)
                    '    Dim sLon As String = sVal.Substring(iInd)
                    '    iInd = sLon.IndexOfAny({"+", "-"}, 2)
                    '    If iInd > 0 Then sLon = sLon.Substring(0, iInd)
                    '    sLon = sLon.Replace("/", "") ' nie wiem po co on tam jest, ale jest
                    'oNewExif.GeoTag = New pkar.BasicGeopos(sLat, sLon)
                    'End If
                    oNewExif.GeoTag = pkar.BasicGeopos.FromExifString(sVal)
                End If
                '| | GPSCoordinates = ...+50.0940+20.0244/
                '| | - Tag '\xa9xyz' (21 bytes):
                '| |   92fd7c: 00 11 15 c7 2b 35 30 2e 30 39 34 30 2b 32 30 2e [....+50.0940+20.]
                '| |   92fd8c: 30 32 34 34 2f                                  [0244/]

                '        oNewExif.GeoTag = oRdr.GetGeoPos
                '        oNewExif.GeoName = oRdr.GetString(CompactExifLib.ExifTag.GpsAreaInformation)

                '        ' spoza EXIF
                '        oNewExif.Restrictions = oRdr.GetString(CompactExifLib.ExifTag.PkarRestriction) ' 0x9212 SecurityClassification string ExifIFD (C/R/S/T/U), do "tajne" :) (ale jest tez non-writable, 0xa212)
                '        oNewExif.ReelName = oRdr.GetString(CompactExifLib.ExifTag.PkarReelName)

                '        ' oraz wersja Windows, tagi które nie występują "normalnie"

                '        ' tag XPTitle is ignored by Windows Explorer if ImageDescription exists)
                '        If oNewExif.Keywords = "" Then oNewExif.Keywords = oRdr.GetStringMicrosoft(CompactExifLib.ExifTag.XpTitle)
                '        ' (ignored by Windows Explorer if Artist exists)
                '        If oNewExif.Author = "" Then oNewExif.Author = oRdr.GetStringMicrosoft(CompactExifLib.ExifTag.XpAuthor)

                '        oNewExif.Keywords = AddSecondTagString(oNewExif.Keywords, oRdr.GetStringMicrosoft(CompactExifLib.ExifTag.XpKeywords))
                '        oNewExif.UserComment = AddSecondTagString(oNewExif.UserComment, oRdr.GetStringMicrosoft(CompactExifLib.ExifTag.XpComment))
                '        oNewExif.UserComment = AddSecondTagString(oNewExif.UserComment, oRdr.GetStringMicrosoft(CompactExifLib.ExifTag.XpSubject))

                '        ' no i nie Microsoftowe
                '        oNewExif.OriginalRAW = oRdr.GetString(CompactExifLib.ExifTag.PkarOriginalRAW) ' OriginalRawFileName
                '        oNewExif.CameraModel = AddSecondTagString(oNewExif.CameraModel, oRdr.GetString(&HC614)) ' UniqueCameraModel = Certo SL110


            Next

        Next

        Return oNewExif


    End Function
#End Region

#Region "compact EXIF"


    Public Function GetForNARCompact(oFile As Vblib.OnePic) As Vblib.ExifTag

        Dim oExif As Vblib.ExifTag = Nothing

        Try

            ' traktuj oFile jako ZIP - znajdź pierwszy JPG
            Using oArchive As IO.Compression.ZipArchive = IO.Compression.ZipFile.OpenRead(oFile.InBufferPathName)

                For Each oInArch As IO.Compression.ZipArchiveEntry In oArchive.Entries
                    If Not oInArch.Name.ToLowerInvariant.EndsWith("jpg") Then Continue For

                    ' mamy JPGa, to z niego czytamy EXIFa
                    Using oStream As Stream = oInArch.Open
                        ' ale z takim nie zadziała, bo Stream takowy nie ma Seek
                        Using oSeekable As New MemoryStream
                            oStream.CopyTo(oSeekable)
                            oSeekable.Position = 0

                            Dim oRdr As New CompactExifLib.ExifData(oSeekable)
                            oExif = GetForReaderCompact(oRdr)
                        End Using
                    End Using
                    Exit For
                Next

            End Using
            'oArchive.Dispose()
        Catch ex As Exception
            Return Nothing
        End Try

        Return oExif
    End Function


    Public Function GetForFileCompact(oFile As Vblib.OnePic) As Vblib.ExifTag
        Try
            Dim oRdr As New CompactExifLib.ExifData(oFile.InBufferPathName)
            Return GetForReaderCompact(oRdr)
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    Public Function GetForReaderCompact(oRdr As CompactExifLib.ExifData) As Vblib.ExifTag

        Dim oNewExif As New Vblib.ExifTag(Nazwa)

        ' EXIF 2.32, 

        ' table 7, D
        oNewExif.UserComment = oRdr.GetString(CompactExifLib.ExifTag.UserComment) ' ANY(ANY)
        ' table 7, F
        oNewExif.DateTimeOriginal = oRdr.GetDate(CompactExifLib.ExifTag.DateTimeOriginal) ' ASCII(20)?

        'If Not String.IsNullOrWhiteSpace(oNewExif.DateTimeOriginal) AndAlso oNewExif.DateTimeOriginal.Length > 15 Then
        '    ' 2022.05.06 12:27:47 
        '    oFile.sortOrder = oNewExif.DateTimeOriginal
        'End If


        oNewExif.DateTimeScanned = oRdr.GetDate(CompactExifLib.ExifTag.DateTimeDigitized) ' ASCII(20)?
        ' table 7, H
        oNewExif.PicGuid = oRdr.GetString(CompactExifLib.ExifTag.ImageUniqueId) ' 0xA420, ASCII(33)
        oNewExif.FileSourceDeviceType = oRdr.GetInt(CompactExifLib.ExifTag.FileSource) ' ANY(1) 41728 = a300

        ' TIFF mandatory, EXIF 2.32
        ' table 4, A
        oNewExif.Orientation = oRdr.GetInt(CompactExifLib.ExifTag.Orientation) ' SHORT

        ' table 4, D
        oNewExif.Author = oRdr.GetString(CompactExifLib.ExifTag.Artist) ' 315 = 013b ASCII(ANY)
        oNewExif.Copyright = oRdr.GetString(CompactExifLib.ExifTag.Copyright) ' 33432 = 8298 ASCII(ANY)
        oNewExif.Keywords = oRdr.GetString(CompactExifLib.ExifTag.ImageDescription)  ' tylko ASCII, 010e ASCII(ANY)
        oNewExif.CameraModel = AddSecondTagString(
            oRdr.GetString(CompactExifLib.ExifTag.Make), ' ASCII(ANY)
            oRdr.GetString(CompactExifLib.ExifTag.Model), ' 272 = 0110 ASCII(ANY)
            " # ")

        ' GPS, table 15
        oNewExif.GeoTag = oRdr.GetGeoPos
        oNewExif.GeoName = oRdr.GetString(CompactExifLib.ExifTag.GpsAreaInformation)

        ' spoza EXIF
        oNewExif.Restrictions = oRdr.GetString(CompactExifLib.ExifTag.PkarRestriction) ' 0x9212 SecurityClassification string ExifIFD (C/R/S/T/U), do "tajne" :) (ale jest tez non-writable, 0xa212)
        oNewExif.ReelName = oRdr.GetString(CompactExifLib.ExifTag.PkarReelName)

        ' oraz wersja Windows, tagi które nie występują "normalnie"

        ' tag XPTitle is ignored by Windows Explorer if ImageDescription exists)
        If oNewExif.Keywords = "" Then oNewExif.Keywords = oRdr.GetStringMicrosoft(CompactExifLib.ExifTag.XpTitle)
        ' (ignored by Windows Explorer if Artist exists)
        If oNewExif.Author = "" Then oNewExif.Author = oRdr.GetStringMicrosoft(CompactExifLib.ExifTag.XpAuthor)

        oNewExif.Keywords = AddSecondTagString(oNewExif.Keywords, oRdr.GetStringMicrosoft(CompactExifLib.ExifTag.XpKeywords))
        oNewExif.UserComment = AddSecondTagString(oNewExif.UserComment, oRdr.GetStringMicrosoft(CompactExifLib.ExifTag.XpComment))
        oNewExif.UserComment = AddSecondTagString(oNewExif.UserComment, oRdr.GetStringMicrosoft(CompactExifLib.ExifTag.XpSubject))

        ' no i nie Microsoftowe
        oNewExif.OriginalRAW = oRdr.GetString(CompactExifLib.ExifTag.PkarOriginalRAW) ' OriginalRawFileName
        oNewExif.CameraModel = AddSecondTagString(oNewExif.CameraModel, oRdr.GetString(&HC614)) ' UniqueCameraModel = Certo SL110


        Return oNewExif
    End Function
#End Region

    Private Function AddSecondTagString(str1 As String, str2 As String, Optional separator As String = "; ") As String
        If String.IsNullOrWhiteSpace(str2) Then Return str1
        If String.IsNullOrWhiteSpace(str1) Then Return str2

        If str1.ToLower = str2.ToLower Then Return str1

        Return str1 & separator & str2
    End Function


End Class

Partial Public Module Extensions

    <Runtime.CompilerServices.Extension>
    Public Function GetString(ByVal oExifData As CompactExifLib.ExifData, tag As CompactExifLib.ExifTag) As String
        Dim temp As String = ""
        If oExifData.GetTagValue(tag, temp, CompactExifLib.StrCoding.UsAscii) Then
            ' jeśli to jest empty, to wolę nothing, bo wtedy nie będzie zapisywany do JSONdump
            If String.IsNullOrWhiteSpace(temp) Then Return Nothing
            Return temp
        Else
            Return Nothing
        End If
    End Function


    <Runtime.CompilerServices.Extension>
    Public Function GetStringMicrosoft(ByVal oExifData As CompactExifLib.ExifData, tag As CompactExifLib.ExifTag) As String
        Dim temp As String = ""
        If oExifData.GetTagValue(tag, temp, CompactExifLib.StrCoding.Utf16Le_Byte) Then
            Return temp.Trim
        Else
            Return Nothing
        End If
    End Function

    <Runtime.CompilerServices.Extension>
    Public Function GetDate(ByVal oExifData As CompactExifLib.ExifData, tag As CompactExifLib.ExifTag) As String
        Dim temp As String = oExifData.GetString(tag)
        If temp = "" Then Return ""

        ' "2019:12:22 15:23:47"
        Dim sRet As String = temp.Substring(0, 10).Replace(":", ".") & temp.Substring(10)
        Return sRet
    End Function

    <Runtime.CompilerServices.Extension>
    Public Function GetInt(ByVal oExifData As CompactExifLib.ExifData, tag As CompactExifLib.ExifTag) As Integer
        Dim temp As Integer = 0
        If oExifData.GetTagValue(tag, temp) Then Return temp
        Return 0
    End Function

    <Runtime.CompilerServices.Extension>
    Public Function ToDecimal(ByVal geoCord As CompactExifLib.GeoCoordinate) As Decimal
        Return CompactExifLib.GeoCoordinate.ToDecimal(geoCord)
    End Function


    <Runtime.CompilerServices.Extension>
    Public Function GetGeoPos(ByVal oExifData As CompactExifLib.ExifData) As pkar.BasicGeopos
        Dim latit As CompactExifLib.GeoCoordinate
        If Not oExifData.GetGpsLatitude(latit) Then Return Nothing
        Dim longit As CompactExifLib.GeoCoordinate
        If Not oExifData.GetGpsLongitude(longit) Then Return Nothing
        Dim altit As Decimal
        If Not oExifData.GetGpsAltitude(altit) Then altit = 0 ' altitude być nie musi, ale może


        Return New pkar.BasicGeopos(latit.ToDecimal, longit.ToDecimal, altit)
    End Function

End Module



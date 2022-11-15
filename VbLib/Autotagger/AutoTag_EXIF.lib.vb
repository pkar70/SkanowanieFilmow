
' https://www.codeproject.com/Articles/5251929/CompactExifLib-Access-to-EXIF-Tags-in-JPEG-TIFF-an

Public Class AutoTag_EXIF
    Inherits AutotaggerBase

    Public Overrides ReadOnly Property Typek As Vblib.AutoTaggerType = Vblib.AutoTaggerType.Local
    Public Overrides ReadOnly Property Nazwa As String = "AUTO_EXIF"
    Public Overrides ReadOnly Property MinWinVersion As String = "7.0"
    Public Overrides ReadOnly Property DymekAbout As String = "Wczytuje znaczniki EXIF z pliku zdjęcia"
    'Public Shared Function GetJSONDump(oFile As Vblib.OnePic) As String
    '    Dim oRdr As New CompactExifLib.ExifData(oFile.InBufferPathName)
    '    Dim sJson As String = Newtonsoft.Json.JsonConvert.SerializeObject(oRdr.tagta, Newtonsoft.Json.Formatting.Indented)

    '    Return sJson
    'End Function


    Public Overrides Async Function GetForFile(oFile As Vblib.OnePic) As Task(Of Vblib.ExifTag)

        Dim oNewExif As New Vblib.ExifTag(Nazwa)

        Dim oRdr As New CompactExifLib.ExifData(oFile.InBufferPathName)

        ' EXIF 2.32, 

        ' table 7, D
        oNewExif.UserComment = oRdr.GetString(CompactExifLib.ExifTag.UserComment) ' ANY(ANY)
        ' table 7, F
        oNewExif.DateTimeOriginal = oRdr.GetDate(CompactExifLib.ExifTag.DateTimeOriginal) ' ASCII(20)?
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

        ' XPTitle is ignored by Windows Explorer if ImageDescription exists)
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
            Return temp
        Else
            Return Nothing
        End If
    End Function


    <Runtime.CompilerServices.Extension>
    Public Function GetStringMicrosoft(ByVal oExifData As CompactExifLib.ExifData, tag As CompactExifLib.ExifTag) As String
        Dim temp As String = ""
        If oExifData.GetTagValue(tag, temp, CompactExifLib.StrCoding.Utf16Le_Byte) Then
            Return temp
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
    Public Function GetGeoPos(ByVal oExifData As CompactExifLib.ExifData) As MyBasicGeoposition
        Dim latit As CompactExifLib.GeoCoordinate
        If Not oExifData.GetGpsLatitude(latit) Then Return Nothing
        Dim longit As CompactExifLib.GeoCoordinate
        If Not oExifData.GetGpsLongitude(longit) Then Return Nothing
        Dim altit As Decimal
        If Not oExifData.GetGpsAltitude(altit) Then altit = 0 ' altitude być nie musi, ale może


        Return New MyBasicGeoposition(latit.ToDecimal, longit.ToDecimal, altit)
    End Function

End Module



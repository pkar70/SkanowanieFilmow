
Imports System.IO
Imports winstreams = Windows.Storage.Streams
Imports wingraph = Windows.Graphics.Imaging
Imports vb14 = Vblib.pkarlibmodule14
Imports Vblib

Public Class Process_EmbedExif
    Inherits Vblib.PostProcBase

    Public Overrides Property Nazwa As String = "EmbedExif"

    Public Overrides Property dymekAbout As String = "Wklejenie znaczników EXIF do zdjęcia"

#If SUPPORT_CALL_WITH_EXIF Then
    Protected Overrides Async Function ApplyMain(oPic As Vblib.OnePic, oExif As Vblib.ExifTag, sNewName As String) As Task(Of Boolean)
        ' oExif tutaj jest ignorowany
        Return Await ApplyMain(oPic, sNewName)
    End Function
#End If

    Protected Overrides Async Function ApplyMain(oPic As Vblib.OnePic, bPipeline As Boolean, params As String) As Task(Of Boolean)

        If Not OperatingSystem.IsWindows Then Return False
        If Not OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240) Then Return False

        Dim bRet As Boolean = False

        oPic.InitEdit(bPipeline)

        Using oStream As New winstreams.InMemoryRandomAccessStream

            Dim oEncoder As wingraph.BitmapEncoder = Await Process_AutoRotate.GetJpgEncoderAsync(oStream)

            oEncoder.SetSoftwareBitmap(Await Process_AutoRotate.LoadSoftBitmapAsync(oPic))

            ' gdy to robię na zwyklym AsRandomAccessStream to się wiesza
            Await oEncoder.FlushAsync()
            '    oStream.Seek(0)

            Process_AutoRotate.SaveSoftBitmap(oStream, oPic)
        End Using

        Dim oExifLib As New CompactExifLib.ExifData(oPic._PipelineInput)
        oExifLib.SetTagValue(CompactExifLib.ExifTag.Orientation, 1, CompactExifLib.ExifTagType.UShort)

        ' dane z EXIF
        Dim oExif As Vblib.ExifTag = oPic.FlattenExifs(True)

        If oExif.FileSourceDeviceType <> 0 Then
            oExifLib.SetTagValue(CompactExifLib.ExifTag.FileSource, oExif.FileSourceDeviceType, CompactExifLib.ExifTagType.Byte)
        End If

        If Not String.IsNullOrWhiteSpace(oExif.Author) Then
            oExifLib.SetTagValue(CompactExifLib.ExifTag.Artist, oExif.Author, CompactExifLib.StrCoding.UsAscii)
        End If
        If Not String.IsNullOrWhiteSpace(oExif.Copyright) Then
            oExifLib.SetTagValue(CompactExifLib.ExifTag.Copyright, oExif.Copyright, CompactExifLib.StrCoding.UsAscii)
        End If
        If Not String.IsNullOrWhiteSpace(oExif.CameraModel) Then
            oExifLib.SetTagValue(CompactExifLib.ExifTag.Model, oExif.CameraModel, CompactExifLib.StrCoding.UsAscii)
        End If
        If Not String.IsNullOrWhiteSpace(oExif.DateTimeOriginal) Then
            oExifLib.SetTagValue(CompactExifLib.ExifTag.DateTimeOriginal, oExif.DateTimeOriginal, CompactExifLib.StrCoding.UsAscii)
        End If
        If Not String.IsNullOrWhiteSpace(oExif.DateTimeScanned) Then
            oExifLib.SetTagValue(CompactExifLib.ExifTag.DateTimeDigitized, oExif.DateTimeScanned, CompactExifLib.StrCoding.UsAscii)
        End If

        If Not String.IsNullOrWhiteSpace(oExif.Restrictions) Then
            oExifLib.SetTagValue(CompactExifLib.ExifTag.PkarRestriction, oExif.Restrictions, CompactExifLib.StrCoding.UsAscii)
        End If

        ' identyfikator
        Dim tempGUID As String = oPic.GetFormattedSerNo
        If Not String.IsNullOrWhiteSpace(tempGUID) Then
            oExifLib.SetTagValue(CompactExifLib.ExifTag.ImageUniqueId, tempGUID, CompactExifLib.StrCoding.UsAscii)
        End If

        If Not String.IsNullOrWhiteSpace(oExif.ReelName) Then
            oExifLib.SetTagValue(CompactExifLib.ExifTag.PkarReelName, oExif.ReelName, CompactExifLib.StrCoding.UsAscii)
        End If
        If Not String.IsNullOrWhiteSpace(oExif.OriginalRAW) Then
            oExifLib.SetTagValue(CompactExifLib.ExifTag.PkarOriginalRAW, oExif.OriginalRAW, CompactExifLib.StrCoding.UsAscii)
        End If
        If Not String.IsNullOrWhiteSpace(oExif.Keywords) Then
            oExifLib.SetTagValue(CompactExifLib.ExifTag.XpKeywords, oExif.Keywords, CompactExifLib.StrCoding.Utf16Le_Byte)
        End If
        If Not String.IsNullOrWhiteSpace(oExif.GeoName) Then
            oExifLib.SetTagValue(CompactExifLib.ExifTag.GpsAreaInformation, oExif.GeoName, CompactExifLib.StrCoding.UsAscii)
        End If

        Dim sDaty As String = ""
        Dim dMax As Date = oExif.DateMax
        Dim dMin As Date = oExif.DateMin
        If dMin.IsDateValid Or dMax.IsDateValid Then
            If dMin.IsDateValid Then sDaty &= dMin.ToString("yyyy-MM-dd HH:mm")
            sDaty &= " ... "
            If dMax.IsDateValid Then sDaty &= dMax.ToString("yyyy-MM-dd HH:mm")
            oExif.UserComment = oExif.UserComment.ConcatenateWithPipe("Zakres dat: " & sDaty)
        End If

        If Not String.IsNullOrWhiteSpace(oExif.UserComment) Then
            oExifLib.SetTagValue(CompactExifLib.ExifTag.UserComment, oExif.UserComment, CompactExifLib.StrCoding.UsAscii)
        End If


        ' geotag ma MANUAL jako override, i wtedy nie sprawdzamy innych, inaczej: ostatni znaleziony
        If oExif.GeoTag IsNot Nothing Then

            Dim oGeoCord As CompactExifLib.GeoCoordinate
            oGeoCord = CompactExifLib.GeoCoordinate.FromDecimal(oExif.GeoTag.Latitude, True)
            oExifLib.SetGpsLatitude(oGeoCord)

            oGeoCord = CompactExifLib.GeoCoordinate.FromDecimal(oExif.GeoTag.Longitude, False)
            oExifLib.SetGpsLongitude(oGeoCord)

        End If

        'Using oFileStream As Stream = IO.File.Open(oPic.sFilenameEditDst, IO.FileMode.Create)
        ' oExifLib.Save(oStream.AsStream, oFileStream, 0) ' (orgFileName)
        oExifLib.Save(oPic._PipelineInput, oPic._PipelineOutput) ' (orgFileName)
        'End Using

        oPic.EndEdit(False, False)

        'End Using

        Return True
    End Function



End Class

Public Class Process_EmbedBasicExif
    Inherits Vblib.PostProcBase

    Public Overrides Property Nazwa As String = "EmbedBasicExif"

    Public Overrides Property dymekAbout As String = "Wklejenie podstawowych znaczników EXIF do zdjęcia"

    Protected Overrides Async Function ApplyMain(oPic As Vblib.OnePic, bPipeline As Boolean, params As String) As Task(Of Boolean)
        If Not OperatingSystem.IsWindows Then Return False
        If Not OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240) Then Return False

        Dim bRet As Boolean = False

        oPic.InitEdit(bPipeline)

        Using oStream As New winstreams.InMemoryRandomAccessStream

            Dim oEncoder As wingraph.BitmapEncoder = Await Process_AutoRotate.GetJpgEncoderAsync(oStream)

            oEncoder.SetSoftwareBitmap(Await Process_AutoRotate.LoadSoftBitmapAsync(oPic))

            ' gdy to robię na zwyklym AsRandomAccessStream to się wiesza
            Await oEncoder.FlushAsync()
            '    oStream.Seek(0)

            Process_AutoRotate.SaveSoftBitmap(oStream, oPic)
        End Using

        oPic._PipelineInput.Seek(0, SeekOrigin.Begin)
        Dim oExifLib As New CompactExifLib.ExifData(oPic._PipelineInput)
        oExifLib.SetTagValue(CompactExifLib.ExifTag.Orientation, 1, CompactExifLib.ExifTagType.UShort)

        ' dane z EXIF
        Dim oExif As Vblib.ExifTag = oPic.FlattenExifs(False)

        If Not String.IsNullOrWhiteSpace(oExif.Author) Then
            oExifLib.SetTagValue(CompactExifLib.ExifTag.Artist, oExif.Author, CompactExifLib.StrCoding.UsAscii)
        End If
        If Not String.IsNullOrWhiteSpace(oExif.Copyright) Then
            oExifLib.SetTagValue(CompactExifLib.ExifTag.Copyright, oExif.Copyright, CompactExifLib.StrCoding.UsAscii)
        End If

        ' identyfikator
        Dim tempGUID As String = oPic.GetFormattedSerNo
        If String.IsNullOrWhiteSpace(tempGUID) Then tempGUID = oPic.sSuggestedFilename
        If Not String.IsNullOrWhiteSpace(tempGUID) Then
            oExifLib.SetTagValue(CompactExifLib.ExifTag.ImageUniqueId, tempGUID, CompactExifLib.StrCoding.UsAscii)
            oExifLib.SetTagValue(CompactExifLib.ExifTag.UserComment, tempGUID, CompactExifLib.StrCoding.UsAscii)
        End If

        'Using oFileStream As Stream = IO.File.Open(oPic.sFilenameEditDst, IO.FileMode.Create)
        ' oExifLib.Save(oStream.AsStream, oFileStream, 0) ' (orgFileName)
        oPic._PipelineInput.Seek(0, SeekOrigin.Begin)
        oPic._PipelineOutput.Seek(0, SeekOrigin.Begin)
        oExifLib.Save(oPic._PipelineInput, oPic._PipelineOutput) ' (orgFileName)
        'End Using

        oPic.EndEdit(False, False)

        'End Using

        Return True
    End Function

End Class


Public Class Process_RemoveExif
    Inherits Vblib.PostProcBase

    Public Overrides Property Nazwa As String = "RemoveExif"

    Public Overrides Property dymekAbout As String = "Usunięcie danych EXIF ze zdjęcia"

    Protected Overrides Async Function ApplyMain(oPic As Vblib.OnePic, bPipeline As Boolean, params As String) As Task(Of Boolean)
        If Not OperatingSystem.IsWindows Then Return False
        If Not OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240) Then Return False

        Dim bRet As Boolean = False

        oPic.InitEdit(bPipeline)

        Using oStream As New winstreams.InMemoryRandomAccessStream

            Dim oEncoder As wingraph.BitmapEncoder = Await Process_AutoRotate.GetJpgEncoderAsync(oStream)

            oEncoder.SetSoftwareBitmap(Await Process_AutoRotate.LoadSoftBitmapAsync(oPic))

            ' gdy to robię na zwyklym AsRandomAccessStream to się wiesza
            Await oEncoder.FlushAsync()
            '    oStream.Seek(0)

            Process_AutoRotate.SaveSoftBitmap(oStream, oPic)
        End Using

        oPic._PipelineInput.Seek(0, SeekOrigin.Begin)
        Dim oExifLib As New CompactExifLib.ExifData(oPic._PipelineInput)
        oExifLib.RemoveAllTags()

        oPic._PipelineInput.Seek(0, SeekOrigin.Begin)
        oExifLib.Save(oPic._PipelineInput, oPic._PipelineOutput) ' (orgFileName)

        oPic.EndEdit(False, False)


        Return True
    End Function



End Class

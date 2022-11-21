
Imports System.IO
Imports winstreams = Windows.Storage.Streams
Imports wingraph = Windows.Graphics.Imaging
Imports vb14 = Vblib.pkarlibmodule14

Public Class Process_AutoRotate
    Inherits Vblib.PostProcBase

    Public Overrides Property Nazwa As String = "AutoRotate"

    Public Overrides Property dymekAbout As String = "Obracanie zdjęć wedle EXIF"

#If SUPPORT_CALL_WITH_EXIF Then
    Protected Overrides Async Function ApplyMain(oPic As Vblib.OnePic, oExif As Vblib.ExifTag, sNewName As String) As Task(Of Boolean)
        ' oExif tutaj jest ignorowany
        Return Await ApplyMain(oPic, sNewName)
    End Function
#End If

    Protected Overrides Async Function ApplyMain(oPic As Vblib.OnePic, bPipeline As Boolean) As Task(Of Boolean)

        Dim oExif As Vblib.ExifTag = oPic.GetExifOfType(Vblib.ExifSource.FileExif)
        If oExif Is Nothing Then Return False
        If oExif.Orientation = Vblib.OrientationEnum.topLeft Then Return True

        Dim bRet As Boolean = False

        oPic.InitEdit(bPipeline)

        Using oStream As New winstreams.InMemoryRandomAccessStream

            Dim oEncoder As wingraph.BitmapEncoder = Await Process_AutoRotate.GetJpgEncoderAsync(oStream)

            oEncoder.SetSoftwareBitmap(Await Process_AutoRotate.LoadSoftBitmapAsync(oPic.sFilenameEditSrc))

            ' gdy to robię na zwyklym AsRandomAccessStream to się wiesza
            Await oEncoder.FlushAsync()

            bRet = Process_AutoRotate.SaveSoftBitmap(oStream, oPic.sFilenameEditDst, oPic.sFilenameEditSrc)

        End Using

        oPic.EndEdit()

        Return bRet
    End Function

#Region "biblioteka operacji na SoftwareBitmap"

    Public Shared Async Function LoadSoftBitmapAsync(sFilePathName As String) As Task(Of wingraph.SoftwareBitmap)
        Dim oSoftBitmap As wingraph.SoftwareBitmap
        Using oStream As winstreams.IRandomAccessStream =
                IO.File.Open(sFilePathName, IO.FileMode.Open, IO.FileAccess.Read).AsRandomAccessStream
            Dim oDec As wingraph.BitmapDecoder = Await wingraph.BitmapDecoder.CreateAsync(oStream)
            oSoftBitmap = Await oDec.GetSoftwareBitmapAsync()
        End Using

        Return oSoftBitmap
    End Function

    Public Shared Function SaveSoftBitmap(oStream As winstreams.InMemoryRandomAccessStream, sDestFilename As String, sExifSrcFilename As String) As Boolean

        oStream.Seek(0)

        Using oFileStream As Stream = IO.File.Open(sDestFilename, IO.FileMode.Create)
            Dim oExifLib As New CompactExifLib.ExifData(sExifSrcFilename)
            oExifLib.SetTagValue(CompactExifLib.ExifTag.Orientation, 1, CompactExifLib.ExifTagType.UShort)
            oExifLib.Save(oStream.AsStream, oFileStream, 0) ' (orgFileName)
        End Using

        Return True

    End Function

    Public Shared Async Function GetJpgEncoderAsync(oStream As winstreams.InMemoryRandomAccessStream) As Task(Of wingraph.BitmapEncoder)
        Dim qualityValue As New wingraph.BitmapTypedValue(
                    vb14.GetSettingsInt("uiJpgQuality") / 100.0,
                    Windows.Foundation.PropertyType.Single)
        Dim oPropertySet As New wingraph.BitmapPropertySet
        oPropertySet.Add("ImageQuality", qualityValue)

        Dim oEncoder As wingraph.BitmapEncoder =
            Await wingraph.BitmapEncoder.CreateAsync(wingraph.BitmapEncoder.JpegEncoderId, oStream, oPropertySet)

        Return oEncoder
    End Function

#End Region


End Class

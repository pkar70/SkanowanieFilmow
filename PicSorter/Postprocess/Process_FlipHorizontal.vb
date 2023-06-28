Imports System.IO
Imports winstreams = Windows.Storage.Streams
Imports wingraph = Windows.Graphics.Imaging
Imports vb14 = Vblib.pkarlibmodule14

Public Class Process_FlipHorizontal
    Inherits Vblib.PostProcBase

    Public Overrides Property Nazwa As String = "FlipHorizontal"

    Public Overrides Property dymekAbout As String = "Lustrowanie zdjęcia (prawo-lewo)"

#If SUPPORT_CALL_WITH_EXIF Then
    Protected Overrides Async Function ApplyMain(oPic As Vblib.OnePic, oExif As Vblib.ExifTag, sNewName As String) As Task(Of Boolean)
        ' oExif tutaj jest ignorowany
        Return Await ApplyMain(oPic, sNewName)
    End Function
#End If

    Protected Overrides Async Function ApplyMain(oPic As Vblib.OnePic, bPipeline As Boolean, params As String) As Task(Of Boolean)

        oPic.InitEdit(bPipeline)

        Using oSoftBitmap As wingraph.SoftwareBitmap = Await Process_AutoRotate.LoadSoftBitmapAsync(oPic)

            Using oStream As New Windows.Storage.Streams.InMemoryRandomAccessStream

                Dim oEncoder As wingraph.BitmapEncoder = Await Process_AutoRotate.GetJpgEncoderAsync(oStream)

                oEncoder.BitmapTransform.Flip = Windows.Graphics.Imaging.BitmapFlip.Horizontal
                oEncoder.SetSoftwareBitmap(oSoftBitmap)

                ' gdy to robię na zwyklym AsRandomAccessStream to się wiesza
                Await oEncoder.FlushAsync()

                Process_AutoRotate.SaveSoftBitmap(oStream, oPic)
                'Process_AutoRotate.SaveSoftBitmap(oStream, oPic.sFilenameEditDst, oPic.sFilenameEditSrc)

            End Using
        End Using

        oPic.EndEdit(True, True)

        'Dim oExif As Vblib.ExifTag = oPic.GetExifOfType(Vblib.ExifSource.FileExif)
        'If oExif IsNot Nothing Then oExif.Orientation = 1

        Return True
    End Function

End Class


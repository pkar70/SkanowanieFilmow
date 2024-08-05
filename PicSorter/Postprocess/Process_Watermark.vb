
Imports System.IO
Imports vb14 = Vblib.pkarlibmodule14

' https://github.com/mchall/HiddenWatermark


Public Class Process_Watermark
    Inherits Vblib.PostProcBase

    Public Overrides Property Nazwa As String = "Watermark"

    Public Overrides Property dymekAbout As String = "Dodawanie znaku wodnego"

#If SUPPORT_CALL_WITH_EXIF Then
    Protected Overrides Async Function ApplyMain(oPic As Vblib.OnePic, oExif As Vblib.ExifTag, sNewName As String) As Task(Of Boolean)
        ' oExif tutaj jest ignorowany
        Return Await ApplyMain(oPic, sNewName)
    End Function
#End If


    Private Shared _watermark As HiddenWatermark.Watermark

    Private Shared Function EnsureWatermarkData() As Boolean
        If _watermark IsNot Nothing Then Return True

        Dim sWatermarkFile As String = vblib.GetDataFile("", "watermark.jpg")
        If Not IO.File.Exists(sWatermarkFile) Then Return False     ' musimy mieć plik ze znakiem wodnym

        Dim watermarkBytes As Byte() = File.ReadAllBytes(sWatermarkFile)
        _watermark = New HiddenWatermark.Watermark(watermarkBytes, True)

        Return True

    End Function

    Protected Overrides Async Function ApplyMain(oPic As Vblib.OnePic, bPipeline As Boolean, params As String) As Task(Of Boolean)


        If Not EnsureWatermarkData() Then Return False

        oPic.InitEdit(bPipeline)

        Dim fileBytes As Byte()

        Using memoryStream As New MemoryStream
            Await oPic._PipelineInput.CopyToAsync(memoryStream)
            fileBytes = memoryStream.ToArray
        End Using

        ' Dim fileBytes As Byte() = File.ReadAllBytes(oPic.sFilenameEditSrc)

        Dim newFileBytes As Byte() = _watermark.EmbedWatermark(fileBytes, vb14.GetSettingsInt("uiJpgQuality"))
        'Dim newFileBytes As Byte() = _watermark.EmbedWatermark(fileBytes, 99)

        Await oPic._PipelineOutput.WriteAsync(newFileBytes)

        ' File.WriteAllBytes(oPic.sFilenameEditDst, newFileBytes)

        oPic.EndEdit(True, False)
        Return True
    End Function
End Class

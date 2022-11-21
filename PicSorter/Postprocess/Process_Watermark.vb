﻿
Imports System.IO

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

    Private Function EnsureWatermarkData() As Boolean
        If _watermark IsNot Nothing Then Return True

        Dim sWatermarkFile As String = Application.GetDataFile("", "watermark.png")
        If Not IO.File.Exists(sWatermarkFile) Then Return False     ' musimy mieć plik ze znakiem wodnym

        Dim watermarkBytes As Byte() = File.ReadAllBytes(sWatermarkFile)
        _watermark = New HiddenWatermark.Watermark(watermarkBytes, True)

        Return True

    End Function

    Protected Overrides Async Function ApplyMain(oPic As Vblib.OnePic, bPipeline As Boolean) As Task(Of Boolean)


        If Not EnsureWatermarkData() Then Return False

        oPic.InitEdit(bPipeline)

        Dim fileBytes As Byte() = File.ReadAllBytes(oPic.sFilenameEditSrc)

        Dim newFileBytes As Byte() = _watermark.EmbedWatermark(fileBytes)

        File.WriteAllBytes(oPic.sFilenameEditDst, newFileBytes)

        oPic.EndEdit()
        Return True
    End Function
End Class

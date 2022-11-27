' https://edi.wang/post/2018/10/12/add-watermark-to-uploaded-image-aspnet-core

Imports System.IO
Imports System.Drawing

Public Class Process_Signature
    Inherits Vblib.PostProcBase

    Public Overrides Property Nazwa As String = "Signature"

    Public Overrides Property dymekAbout As String = "Dodawanie podpisu"




    Protected Overrides Async Function ApplyMain(oPic As Vblib.OnePic, bPipeline As Boolean) As Task(Of Boolean)

        oPic.InitEdit(bPipeline)

        ' Dim watermarkedStream As IO.Stream = IO.File.OpenWrite(oPic.sFilenameEditDst)

        Dim signature As String = GetSignatureString(oPic)

        ' Using oStream As IO.Stream = IO.File.OpenRead(oPic.sFilenameEditSrc)

        Using img = Image.FromStream(oPic._PipelineInput)   ' oStream

            Dim fontSize As Integer = 20        ' minimalna wielkoœæ
            fontSize = Math.Max(fontSize, img.Height * 0.05)    ' 5 % obrazka
            fontSize = Math.Min(fontSize, 50)       ' ale nie wiêcej ni¿ 50 px

            Using graphic = Graphics.FromImage(img)
                Dim oFont As New Font(FontFamily.GenericSansSerif, fontSize, FontStyle.Bold, GraphicsUnit.Pixel)

                graphic.PageUnit = GraphicsUnit.Pixel
                Dim oMeasure As SizeF = graphic.MeasureString(signature, oFont)

                Dim iWhereX As Integer = img.Width - oMeasure.Height
                iWhereX = Math.Max(iWhereX, 10)


                Dim oBrush As New SolidBrush(Color.Gray)
                Dim oPoint As New Point(iWhereX, img.Height - fontSize * 2)

                graphic.DrawString(signature, oFont, oBrush, oPoint)
                img.Save(oPic._PipelineOutput, Imaging.ImageFormat.Jpeg)    '   watermarkedStream

            End Using
        End Using
        ' End Using


        oPic.EndEdit()

        Return True
    End Function

    Private Function GetSignatureString(oPic As Vblib.OnePic) As String
        Dim sSignature As String = ""

        For Each oExif As Vblib.ExifTag In oPic.Exifs
            If Not String.IsNullOrWhiteSpace(oExif.Copyright) Then sSignature = oExif.Copyright
        Next

        Dim iInd As Integer = sSignature.IndexOfAny({".", ","})
        If iInd > 1 Then sSignature = sSignature.Substring(0, iInd)

        Return sSignature
    End Function


End Class
' https://edi.wang/post/2018/10/12/add-watermark-to-uploaded-image-aspnet-core

Imports System.IO
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Text
Imports vb14 = Vblib.pkarlibmodule14


Public Class Process_Signature
    Inherits Vblib.PostProcBase

    Public Overrides Property Nazwa As String = "Signature"

    Public Overrides Property dymekAbout As String = "Dodawanie podpisu"




#Disable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
    Protected Overrides Async Function ApplyMain(oPic As Vblib.OnePic, bPipeline As Boolean) As Task(Of Boolean)
#Enable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously

        oPic.InitEdit(bPipeline)

        ' Dim watermarkedStream As IO.Stream = IO.File.OpenWrite(oPic.sFilenameEditDst)

        Dim signature As String = GetSignatureString(oPic)

        ' Using oStream As IO.Stream = IO.File.OpenRead(oPic.sFilenameEditSrc)

        Using img = Image.FromStream(oPic._PipelineInput)   ' oStream

            Dim fontSize As Integer = 20        ' minimalna wielko??
            fontSize = Math.Max(fontSize, img.Height * 0.05)    ' 5 % obrazka
            fontSize = Math.Min(fontSize, 50)       ' ale nie wi?cej ni? 50 px

            Using graphic = Graphics.FromImage(img)
                Dim oFont As New Font(FontFamily.GenericSansSerif, fontSize, FontStyle.Bold, GraphicsUnit.Pixel)

                graphic.PageUnit = GraphicsUnit.Pixel
                Dim oMeasure As SizeF = graphic.MeasureString(signature, oFont)

                Dim iWhereX As Integer = img.Width - oMeasure.Width
                iWhereX = Math.Max(iWhereX, 10)


                Dim oBrush As New SolidBrush(Color.Gray)
                Dim oPoint As New Point(iWhereX, img.Height - fontSize * 2)

                graphic.DrawString(signature, oFont, oBrush, oPoint)
#If STARA_WERSJA Then
                img.Save(oPic._PipelineOutput, Imaging.ImageFormat.Jpeg)    '   watermarkedStream
#Else
                img.Save(oPic._PipelineOutput, GetEncoder(ImageFormat.Jpeg), GetJpgQuality)
#End If


            End Using
        End Using
        ' End Using


        oPic.EndEdit(True, False)

        Return True
    End Function

    Public Shared Function GetJpgQuality(Optional iQuality As Integer = 0) As EncoderParameters
        If iQuality = 0 Then iQuality = vb14.GetSettingsInt("uiJpgQuality")

        ' https://stackoverflow.com/questions/1484759/quality-of-a-saved-jpg-in-c-sharp
        Dim myEncoder As Imaging.Encoder = Imaging.Encoder.Quality
        Dim myEncoderParameters As New EncoderParameters(1)
        myEncoderParameters.Param(0) = New EncoderParameter(myEncoder, iQuality)
        Return myEncoderParameters
    End Function

    Public Shared Function GetEncoder(format As ImageFormat) As ImageCodecInfo

        For Each codec As ImageCodecInfo In ImageCodecInfo.GetImageDecoders()
            If codec.FormatID = format.Guid Then Return codec
        Next
        Return Nothing
    End Function

    Private Function GetSignatureString(oPic As Vblib.OnePic) As String

        Dim oExif As Vblib.ExifTag = oPic.FlattenExifs
        Return oExif.Copyright
        'Dim sSignature As String = ""

        'For Each oExif As Vblib.ExifTag In oPic.Exifs
        '    If Not String.IsNullOrWhiteSpace(oExif.Copyright) Then sSignature = oExif.Copyright
        'Next

        'Dim iInd As Integer = sSignature.IndexOfAny({".", ","})
        'If iInd > 1 Then sSignature = sSignature.Substring(0, iInd)

        'Return sSignature
    End Function


End Class
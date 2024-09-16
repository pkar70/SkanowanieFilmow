' https://edi.wang/post/2018/10/12/add-watermark-to-uploaded-image-aspnet-core

Imports System.IO
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Text
Imports vb14 = Vblib.pkarlibmodule14


Public Class Process_EmbedTexts
    Inherits Vblib.PostProcBase

    Public Overrides Property Nazwa As String = "EmbedTexts()"

    Public Overrides Property dymekAbout As String = "Dodawanie napisów"

    ''' <summary>
    ''' Wdrukowanie tekstów wedle params
    ''' </summary>
    ''' <param name="params">Sk³adanka obszarów, attribs|min|pct|max|txt, attrib: Bold, Italic, Underline, Strikeout, wkedn - kolory, lrtp - po³o¿enie</param>
    Public Shared Async Function ApplyMainShared(oPic As Vblib.OnePic, bPipeline As Boolean, params As String) As Task(Of Boolean)

        Dim oEngine As New Process_EmbedTexts
        Return Await oEngine.ApplyMain(oPic, bPipeline, params)
    End Function



#Disable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
    Protected Overrides Async Function ApplyMain(oPic As Vblib.OnePic, bPipeline As Boolean, params As String) As Task(Of Boolean)
#Enable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously

        oPic.InitEdit(bPipeline)
        If String.IsNullOrWhiteSpace(params) Then
            oPic.SkipEdit()
            Return False ' nie ma parametrów, czyli nic nie robimy
        End If


        Using img = Image.FromStream(oPic._PipelineInput)   ' oStream

            Using graphic = Graphics.FromImage(img)

                Dim obszary As String() = params.Split("||")

                For Each obszar As String In obszary

                    ' gdzie|minsize|%obrazka|maxsize|string
                    Dim dane As String() = obszar.Split("|")
                    If dane.Length <> 5 Then Continue For

                    Dim minSize, percent, maxSize As Double
                    If Not Double.TryParse(dane(1), minSize) Then Continue For
                    If Not Double.TryParse(dane(2), percent) Then Continue For
                    If Not Double.TryParse(dane(3), maxSize) Then Continue For

                    Dim tekst As String = ZastosujPodmiany(oPic, dane(4))
                    If String.IsNullOrWhiteSpace(tekst) Then Continue For

                    Dim attribs As String = dane(0)

                    Dim fontSize As Integer = minSize
                    fontSize = Math.Max(fontSize, img.Height * percent)
                    fontSize = Math.Min(fontSize, maxSize)

                    Dim styl As FontStyle = FontStyle.Regular
                    If attribs.Contains("B") Then styl = styl Or FontStyle.Bold
                    If attribs.Contains("I") Then styl = styl Or FontStyle.Italic
                    If attribs.Contains("U") Then styl = styl Or FontStyle.Underline
                    If attribs.Contains("S") Then styl = styl Or FontStyle.Strikeout

                    Dim oBrush As New SolidBrush(Color.Gray)
                    If attribs.Contains("w") Then oBrush = New SolidBrush(Color.WhiteSmoke)
                    If attribs.Contains("k") Then oBrush = New SolidBrush(Color.Black)
                    If attribs.Contains("e") Then oBrush = New SolidBrush(Color.Blue)
                    If attribs.Contains("d") Then oBrush = New SolidBrush(Color.Red)
                    If attribs.Contains("n") Then oBrush = New SolidBrush(Color.Green)

                    Dim oFont As New Font(FontFamily.GenericSansSerif, fontSize, styl, GraphicsUnit.Pixel)

                    graphic.PageUnit = GraphicsUnit.Pixel
                    Dim oMeasure As SizeF = graphic.MeasureString(tekst, oFont)

                    Dim iWhereX As Integer
                    ' If dane(0).Contains("c") Then
                    iWhereX = (img.Width - oMeasure.Width) / 2  ' default: center
                    If attribs.Contains("l") Then iWhereX = 10
                    If attribs.Contains("r") Then iWhereX = img.Width - oMeasure.Width - 10
                    iWhereX = Math.Max(iWhereX, 10)

                    Dim iWhereY As Integer = img.Height - fontSize * 2 ' default: jak w Signature
                    If attribs.Contains("t") Then iWhereY = 10
                    If attribs.Contains("b") Then iWhereY = img.Height - fontSize - 10

                    Dim oPoint As New Point(iWhereX, iWhereY)

                    graphic.DrawString(tekst, oFont, oBrush, oPoint)
                Next


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

    Private Shared Function ZastosujPodmiany(oPic As Vblib.OnePic, tekst As String) As String
        tekst = tekst.Replace("%f", IO.Path.GetFileNameWithoutExtension(oPic.sSuggestedFilename))
        tekst = tekst.Replace("%p", oPic.TargetDir)

        tekst = tekst.Replace("%u", oPic.PicGuid)

        tekst = tekst.Replace("%d", oPic.GetMostProbablyDate.ToString("yyyy.MM.dd"))
        tekst = tekst.Replace("%t", oPic.GetMostProbablyDate.ToString("yyyy.MM.dd HH:mm"))

        Dim minDate, maxDate As String
        minDate = oPic.GetMinDate.ToString("yyyy.MM.dd")
        maxDate = oPic.GetMaxDate.ToString("yyyy.MM.dd")
        tekst = tekst.Replace("%m", minDate)
        tekst = tekst.Replace("%x", maxDate)
        If maxDate = minDate Then
            tekst = tekst.Replace("%r", minDate)
        Else
            tekst = tekst.Replace("%r", minDate & "..." & maxDate)
        End If

        ' to robi bez flatten, bo descriptions nie s¹ w Exifach
        tekst = tekst.Replace("%d", oPic.GetSumOfDescriptionsText)

        Dim oFlat = oPic.FlattenExifs(False)
        tekst = tekst.Replace("%k", oFlat.Keywords)
        tekst = tekst.Replace("%a", oFlat.Author)
        tekst = tekst.Replace("%c", oFlat.Copyright)
        Return tekst
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

End Class

Public Class Process_SerNo
    Inherits Vblib.PostProcBase

    Public Overrides Property Nazwa As String = "Serno"

    Public Overrides Property dymekAbout As String = "Dodawanie #serno"

    Protected Overrides Async Function ApplyMain(oPic As Vblib.OnePic, bPipeline As Boolean, params As String) As Task(Of Boolean)

        Return Await Process_EmbedTexts.ApplyMainShared(oPic, bPipeline, "Bl|12|0.03|30|" & oPic.FormattedSerNo)

    End Function


End Class

Public Class Process_Signature
    Inherits Vblib.PostProcBase

    Public Overrides Property Nazwa As String = "Signature"

    Public Overrides Property dymekAbout As String = "Dodawanie podpisu"


    Protected Overrides Async Function ApplyMain(oPic As Vblib.OnePic, bPipeline As Boolean, params As String) As Task(Of Boolean)

        Dim signature As String = params
        If String.IsNullOrWhiteSpace(signature) Then signature = GetSignatureString(oPic)

        Return Await Process_EmbedTexts.ApplyMainShared(oPic, bPipeline, "Br|20|0.05|50|" & signature)

    End Function

    ''' <summary>
    ''' zwraca Copyright po FlattenExifs
    ''' </summary>
    Private Function GetSignatureString(oPic As Vblib.OnePic) As String

        Dim oExif As Vblib.ExifTag = oPic.FlattenExifs(False)
        If Not String.IsNullOrWhiteSpace(oExif.Copyright) Then Return oExif.Copyright
        Return Vblib.GetSettingsString("uiDefaultCopyr")

    End Function


End Class
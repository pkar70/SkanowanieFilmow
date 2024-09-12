Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.IO
Imports Vblib

Public Class Process_FaceRemove
    Inherits Vblib.PostProcBase

    Public Overrides Property Nazwa As String = "FaceRemove"

    Public Overrides Property dymekAbout As String = "Zakrywanie twarzy"

    Private _brush As New SolidBrush(Color.Gray)

#Disable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
    Protected Overrides Async Function ApplyMain(oPic As Vblib.OnePic, bPipeline As Boolean, params As String) As Task(Of Boolean)
#Enable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously

        Dim oExif As Vblib.ExifTag = oPic.GetExifOfType(Vblib.ExifSource.AutoAzure)
        If oExif Is Nothing Then oExif = oPic.GetExifOfType(Vblib.ExifSource.AutoWinFace)

        oPic.InitEdit(bPipeline)

        ' 2024.06.28: maxDate nie na średnią (1800..maxDate, co daje często <1900), tylko na maxDate gdy nie ma realnej daty
        Dim maxDate As Date
        If oPic.HasRealDate Then
            maxDate = oPic.GetMostProbablyDate
        Else
            maxDate = oPic.GetMaxDate
        End If

        ' 2024.05.15: limit ukrywania twarzy (default: 90 lat)
        If (Date.Now - maxDate).TotalDays > 365 * Vblib.GetSettingsInt("uiWinFaceMaxAge") Then
            oPic.SkipEdit()
            Return True
        End If

        If oExif?.AzureAnalysis?.Faces?.lista Is Nothing Then
            oPic.SkipEdit()
            Return True
        End If
        If oExif.AzureAnalysis.Faces.lista.Count < 1 Then
            oPic.SkipEdit()
            Return True
        End If

        If SprawdzCzyWszyscyDawnoZmarli(oPic.sumOfKwds, oExif.AzureAnalysis.Faces.lista.Count) Then
            oPic.SkipEdit()
            Return True
        End If

        Dim limitRozmiaru As Integer = Vblib.GetSettingsInt("uiWinFaceMinSize")

        oPic._PipelineInput.Seek(0, SeekOrigin.Begin)
        Using img = Image.FromStream(oPic._PipelineInput)

            Using graphic = Graphics.FromImage(img)

                For Each oFace As Vblib.TextWithProbAndBox In oExif.AzureAnalysis.Faces.lista

                    ' zabezpieczenie przed starszą wersją, gdy był point a nie było rozmiaru
                    If oFace.Width + oFace.Height = 0 Then Continue For
                    ' zabezpieczenie przed WinFace sprzed pamiętania box
                    If oFace.X + oFace.Y = 0 Then Continue For
                    ' rozmiar mniejszy niż limit
                    If img.Height < limitRozmiaru Then Continue For
                    If img.Width < limitRozmiaru Then Continue For

                    ' wyliczamy środek
                    Dim iX As Integer = img.Width * oFace.X / 100
                    Dim iW As Integer = img.Width * oFace.Width / 100
                    Dim iY As Integer = img.Height * oFace.Y / 100
                    Dim iH As Integer = img.Height * oFace.Height / 100
                    ' jakieś dziwne mamy dane, więc sprawdzamy - to jest zawsze dobre
                    If oFace.Width > 90 Then iW = img.Width / 10
                    If oFace.Height > 90 Then iH = img.Height / 10
                    If oFace.X + oFace.Width > 100 Then iW = img.Width / 10
                    If oFace.Y + oFace.Height > 100 Then iH = img.Height / 10

                    ' oraz limit na razie, może później do usunięcia
                    If oFace.Width > 30 Then iW = img.Width / 10
                    If oFace.Height > 30 Then iH = img.Height / 10


                    ' i robimy owal
                    graphic.FillEllipse(_brush, iX, iY, iW, iH)
                Next

                img.Save(oPic._PipelineOutput, Process_EmbedTexts.GetEncoder(ImageFormat.Jpeg), Process_EmbedTexts.GetJpgQuality)
            End Using
        End Using

        oPic.EndEdit(True, False)

        Return True
    End Function

    Private Function GetSignatureString(oPic As Vblib.OnePic) As String
        Dim sSignature As String = ""

        For Each oExif As Vblib.ExifTag In oPic.Exifs
            If Not String.IsNullOrWhiteSpace(oExif.Copyright) Then sSignature = oExif.Copyright
        Next

        Dim iInd As Integer = sSignature.IndexOf(".")
        If iInd > 1 Then sSignature = sSignature.Substring(0, iInd)

        Return sSignature
    End Function

    Private Function SprawdzCzyWszyscyDawnoZmarli(sumKwds As String, facescount As Integer) As Boolean

        Dim monthsDelay As Integer = Vblib.GetSettingsInt("uiWinFaceAfterDeath")

        Dim oKwds As List(Of Vblib.OneKeyword) = Globs.GetKeywords.GetKeywordsList(sumKwds)
        Dim cntKwdsOsob As Integer = 0

        For Each kwd As OneKeyword In oKwds
            If Not kwd.sId.StartsWith("-") Then Continue For
            cntKwdsOsob += 1
            ' jeśli nie wiemy kiedy ktoś zmarł, to zakładamy że nie zmarł
            If Not kwd.maxDate.IsDateValid Then Return False

            ' jeszcze nie minęło odpowiednio dużo czasu
            If kwd.maxDate.AddMonths(monthsDelay) > Date.Now Then Return False

        Next

        ' nie wszyscy są zidentyfikowani
        If facescount <> cntKwdsOsob Then Return False

        Return True
    End Function


End Class
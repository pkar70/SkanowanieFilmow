Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.IO

Public Class Process_FaceRemove
    Inherits Vblib.PostProcBase

    Public Overrides Property Nazwa As String = "FaceRemove"

    Public Overrides Property dymekAbout As String = "Zakrywanie twarzy"

    Private _brush As New SolidBrush(Color.Gray)

    Protected Overrides Async Function ApplyMain(oPic As Vblib.OnePic, bPipeline As Boolean) As Task(Of Boolean)

        Dim oExif As Vblib.ExifTag = oPic.GetExifOfType(Vblib.ExifSource.AutoAzure)
        If oExif Is Nothing Then oExif = oPic.GetExifOfType(Vblib.ExifSource.AutoWinFace)

        If oExif?.AzureAnalysis?.Faces?.lista Is Nothing Then Return False
        If oExif.AzureAnalysis.Faces.lista.Count < 1 Then Return False

        oPic.InitEdit(bPipeline)

        oPic._PipelineInput.Seek(0, SeekOrigin.Begin)
        Using img = Image.FromStream(oPic._PipelineInput)

            Using graphic = Graphics.FromImage(img)

                For Each oFace As Vblib.TextWithProbAndBox In oExif.AzureAnalysis.Faces.lista

                    ' zabezpieczenie przed starszą wersją, gdy był point a nie było rozmiaru
                    If oFace.Width + oFace.Height = 0 Then Continue For
                    ' zabezpieczenie przed WinFace sprzed pamiętania box
                    If oFace.X + oFace.Y = 0 Then Continue For

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

                img.Save(oPic._PipelineOutput, Process_Signature.GetEncoder(ImageFormat.Jpeg), Process_Signature.GetJpgQuality)
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


End Class
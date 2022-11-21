Imports System.Drawing

Imports System.IO

Public Class Process_FaceRemove
    Inherits Vblib.PostProcBase

    Public Overrides Property Nazwa As String = "FaceRemove"

    Public Overrides Property dymekAbout As String = "Zakrywanie twarzy"


    Protected Overrides Async Function ApplyMain(oPic As Vblib.OnePic, bPipeline As Boolean) As Task(Of Boolean)

        Dim oExif As Vblib.ExifTag = oPic.GetExifOfType(Vblib.ExifSource.AutoAzure)
        If oExif Is Nothing Then oExif = oPic.GetExifOfType(Vblib.ExifSource.AutoWinFace)

        If oExif?.AzureAnalysis?.Faces?.lista Is Nothing Then Return False
        If oExif.AzureAnalysis.Faces.lista.Count < 1 Then Return False

        oPic.InitEdit(bPipeline)

        Dim outputStream As IO.Stream = IO.File.OpenWrite(oPic.sFilenameEditDst)

        Dim signature As String = GetSignatureString(oPic)

        Using oStream As IO.Stream = IO.File.OpenRead(oPic.sFilenameEditSrc)

            Using img = Image.FromStream(oStream)

                Using graphic = Graphics.FromImage(img)

                    For Each oFace As Vblib.TextWithProbAndBox In oExif.AzureAnalysis.Faces.lista
                        ' wyliczamy środek
                        Dim iX As Integer = img.Width * oFace.X
                        Dim iW As Integer = img.Width * oFace.Width
                        Dim iY As Integer = img.Height * oFace.Y
                        Dim iH As Integer = img.Height * oFace.Height
                        ' i robimy owal
                        Dim oPen As New Pen(New SolidBrush(Color.Gray))
                        graphic.DrawEllipse(oPen, iX, iY, iW, iH)
                    Next

                    img.Save(outputStream, Imaging.ImageFormat.Jpeg)
                End Using
            End Using
        End Using

        oPic.EndEdit()

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
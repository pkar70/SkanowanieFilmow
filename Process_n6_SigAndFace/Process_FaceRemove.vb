Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.IO
Imports Vblib

Public Class Process_FaceRemove
    Inherits Vblib.PostProcBase

    Public Overrides Property Nazwa As String = "FaceRemove"

    Public Overrides Property dymekAbout As String = "Zakrywanie twarzy"


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

        Dim pedzel As SolidBrush 'New SolidBrush(Color.Gray)
        ' ustalane w SettingsPipeline
        Dim r As Integer = Vblib.GetSettingsInt("uiWinFaceR")
        Dim g As Integer = Vblib.GetSettingsInt("uiWinFaceG")
        Dim b As Integer = Vblib.GetSettingsInt("uiWinFaceB")
        Dim a As Integer = Vblib.GetSettingsInt("uiWinFaceA")

        pedzel = New SolidBrush(Color.FromArgb(a, r, g, b))

        Dim limitRozmiaru As Integer = Vblib.GetSettingsInt("uiWinFaceMinSize")

        'Dim bUseAverage As Boolean = Vblib.GetSettingsBool("uiWinFaceAverage")
        Dim trybZamazywania As Integer = Vblib.GetSettingsInt("uiTrybZamazywania")

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

                    If trybZamazywania = 2 Then
                        BlurEllipse(img, New Rectangle(iX, iY, iW, iH))
                    Else
                        If trybZamazywania = 1 Then
                            ' pędzelek jest do zmiany za każdym przebiegiem
                            pedzel = New SolidBrush(CalculateAverageColour(img, iX, iY, iW, iH))
                        End If

                        graphic.FillEllipse(pedzel, iX, iY, iW, iH)
                    End If

                Next

                img.Save(oPic._PipelineOutput, Process_EmbedTexts.GetEncoder(ImageFormat.Jpeg), Process_EmbedTexts.GetJpgQuality)
            End Using
        End Using

        oPic.EndEdit(True, False)

        Return True
    End Function

#Region "próba blur"

    Public Sub BlurEllipse(image As Bitmap, ellipseBounds As Rectangle)
        ' Create a temporary bitmap for the blurred region
        Using blurredRegion As New Bitmap(ellipseBounds.Width, ellipseBounds.Height)
            ' Copy the elliptical region to the temporary bitmap
            Using g As Graphics = Graphics.FromImage(blurredRegion)
                g.DrawImage(image, New Rectangle(0, 0, ellipseBounds.Width, ellipseBounds.Height), ellipseBounds, GraphicsUnit.Pixel)
            End Using

            ' Apply a blur effect to the temporary bitmap
            ApplyBoxBlur(blurredRegion)

            ' Draw the blurred region back onto the original image
            Using g As Graphics = Graphics.FromImage(image)
                Dim clipPath As New Drawing2D.GraphicsPath()
                clipPath.AddEllipse(ellipseBounds)
                g.SetClip(clipPath)
                g.DrawImage(blurredRegion, ellipseBounds)
            End Using
        End Using
    End Sub

    Private Sub ApplyBoxBlur(bitmap As Bitmap)
        ' Simple box blur implementation
        Dim rect As New Rectangle(0, 0, bitmap.Width, bitmap.Height)
        Dim bmpData As BitmapData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb)

        Try
            Dim stride As Integer = bmpData.Stride
            Dim scan0 As IntPtr = bmpData.Scan0
            Dim bytes As Integer = Math.Abs(stride) * bitmap.Height
            Dim pixelData(bytes - 1) As Byte
            System.Runtime.InteropServices.Marshal.Copy(scan0, pixelData, 0, bytes)

            ' Apply a simple box blur (average of neighboring pixels)
            Dim blurredData(bytes - 1) As Byte

            Dim rozmiarBlur As Integer = Math.Min(bitmap.Height, bitmap.Width) / Vblib.GetSettingsInt("uiWinFaceBlurDivider", 5)

            ' Debug.WriteLine($"bede iterował Y od 0 do {bitmap.Height - 1} i X od 0 do {bitmap.Width - 1}")

            For y As Integer = 0 To bitmap.Height - 1
                For x As Integer = 0 To bitmap.Width - 1
                    Dim pixelIndex As Integer = (y * stride) + (x * 3)

                    'Debug.WriteLine($"docelowy pixel {pixelIndex} dla Y={y} i X={x}")

                    For channel As Integer = 0 To 2 ' B, G, R channels
                        ' 1.0 * bo inaczej jest overflow, jako że używa pewnie Byte

                        Dim sumaPikseli As Double = 0
                        Dim cntPikseli As Integer = 0

                        'Debug.WriteLine($"channel {channel}, bede iterował iBlurRow od {Math.Max(0, y - 5)} do {Math.Min(y + 5, bitmap.Height)} ")

                        ' w kwadracie 5×5
                        For iBlurRow As Integer = Math.Max(0, y - rozmiarBlur) To Math.Min(y + rozmiarBlur, bitmap.Height - 1)
                            'Debug.WriteLine($"iBluRow = {iBlurRow}; bede iterował iBlurCol od do {Math.Min(x + 5, bitmap.Width)}")
                            For iBlurCol As Integer = Math.Max(0, x - rozmiarBlur) To Math.Min(x + rozmiarBlur, bitmap.Width - 1)

                                ' Debug.WriteLine($"sprawdzam pixelData dla {iBlurCol * 3 + iBlurRow * stride + channel}")

                                sumaPikseli += pixelData(iBlurCol * 3 + iBlurRow * stride + channel)
                                cntPikseli += 1
                            Next
                        Next

                        'Debug.WriteLine($"wyliczylem {sumaPikseli} z {cntPikseli}")

                        blurredData(pixelIndex + channel) = CInt(sumaPikseli / cntPikseli)

                        'blurredData(pixelIndex + channel) =
                        '(1.0 * pixelData(pixelIndex + channel) +
                        ' 1.0 * pixelData(pixelIndex + channel - 3) +
                        ' 1.0 * pixelData(pixelIndex + channel + 3) +
                        ' 1.0 * pixelData(pixelIndex + channel - stride) +
                        ' 1.0 * pixelData(pixelIndex + channel + stride)) \ 5
                    Next
                Next
            Next

            ' Copy the blurred data back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(blurredData, 0, scan0, bytes)
        Finally
            bitmap.UnlockBits(bmpData)
        End Try
    End Sub

#End Region


    Private Function CalculateAverageColour(obrazek As Image, iX As Integer, iY As Integer, iW As Integer, iH As Integer) As Color
        Dim totalR As Long = 0
        Dim totalG As Long = 0
        Dim totalB As Long = 0
        Dim pixelCount As Integer = 0

        Dim bmp As Bitmap = TryCast(obrazek, Bitmap)

        ' Lock the bitmap's bits for faster access
        Dim rect As New Rectangle(0, 0, obrazek.Width, obrazek.Height)
        Dim bmpData As BitmapData = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb)

        Try
            Dim stride As Integer = bmpData.Stride
            Dim scan0 As IntPtr = bmpData.Scan0
            Dim bytes As Integer = Math.Abs(stride) * obrazek.Height
            Dim pixelData(bytes - 1) As Byte
            System.Runtime.InteropServices.Marshal.Copy(scan0, pixelData, 0, bytes)

            ' Loop through the bounding rectangle of the ellipse
            For y As Integer = iY To iY + iH - 1
                For x As Integer = iX To iX + iW - 1
                    ' Check if the pixel is inside the ellipse
                    Dim dx As Double = (x - (iX + iW / 2.0)) / (iW / 2.0)
                    Dim dy As Double = (y - (iY + iH / 2.0)) / (iH / 2.0)
                    If dx * dx + dy * dy <= 1 Then
                        ' Calculate the pixel's position in the byte array
                        Dim pixelIndex As Integer = (y * stride) + (x * 3)
                        Dim b As Byte = pixelData(pixelIndex)
                        Dim g As Byte = pixelData(pixelIndex + 1)
                        Dim r As Byte = pixelData(pixelIndex + 2)

                        totalR += r
                        totalG += g
                        totalB += b
                        pixelCount += 1
                    End If
                Next
            Next
        Finally
            ' Unlock the bits
            bmp.UnlockBits(bmpData)
        End Try

        ' Avoid division by zero
        If pixelCount = 0 Then Return Color.Black

        ' Calculate the average color
        Dim avgR As Integer = CInt(totalR / pixelCount)
        Dim avgG As Integer = CInt(totalG / pixelCount)
        Dim avgB As Integer = CInt(totalB / pixelCount)

        Return Color.FromArgb(avgR, avgG, avgB)
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
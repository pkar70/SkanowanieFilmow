
Imports System.IO   ' for MemoryStream itp.
Imports Vblib
Imports pkar.DotNetExtensions

Imports Windows.Graphics.Imaging
Imports Windows.Media
Imports Windows.Media.FaceAnalysis


Public Class Auto_WinFace
    Inherits Vblib.AutotaggerBase
    Public Overrides ReadOnly Property Typek As Vblib.AutoTaggerType = Vblib.AutoTaggerType.Local
    Public Overrides ReadOnly Property Nazwa As String = Vblib.ExifSource.AutoWinFace
    Public Overrides ReadOnly Property MinWinVersion As String = "10.0"
    Public Overrides ReadOnly Property DymekAbout As String = "Próbuje policzyæ twarze u¿ywaj¹c Windows." & vbCrLf & "U¿ywa keyword -fN, gdzie n to liczba twarzy"
    'Public Overrides ReadOnly Property includeMask As String = "*.jpg;*.jpg.thumb;*.nar"

    Public Overrides Async Function GetForFile(oFile As OnePic) As Task(Of ExifTag)
        If Not oFile.MatchesMasks(includeMask) Then Return Nothing
        Return Await ZrobMain(oFile)

    End Function

    Private Shared _FaceEngine As FaceAnalysis.FaceDetector = Nothing


    ' https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/detect-and-track-faces-in-an-image

    ' The face detection process is quicker with a smaller image and so you may want to scale the source image down to a smaller size.
    Private Const sourceImageHeightLimit As Integer = 1280
    ' In the current version, the FaceDetector class only supports images in Gray8 or Nv12.
    Private Const faceDetectionPixelFormat As BitmapPixelFormat = BitmapPixelFormat.Gray8

    Private Async Function ZrobMain(oFile As Vblib.OnePic) As Task(Of Vblib.ExifTag)
        ' zabezpieczenie przed wywo³aniem na starszych Windows
        If Not OperatingSystem.IsWindows Then Return Nothing
        If Not OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240) Then Return Nothing

        ' jakby Create zajmowa³o wiêcej czasu - a przecie¿ moze byæ setki zdjêæ do OCR po kolei
        If _FaceEngine Is Nothing Then _FaceEngine = Await FaceDetector.CreateAsync

        ' na bitmape i OCR
        Dim oStream As IO.Stream = Nothing
        Dim oArchive As IO.Compression.ZipArchive = Nothing

        If oFile.MatchesMasks("*.nar") Then
            oArchive = IO.Compression.ZipFile.OpenRead(oFile.InBufferPathName)
            For Each oInArch As IO.Compression.ZipArchiveEntry In oArchive.Entries
                If Not IO.Path.GetExtension(oInArch.Name).EqualsCI(".jpg") Then Continue For

                ' mamy JPGa (a nie XML na przyk³ad)
                oStream = New MemoryStream
                Dim oStreamTemp As Stream = oInArch.Open
                Await oStreamTemp.CopyToAsync(oStream)
                oStreamTemp.Dispose()
                Exit For
            Next
        Else
            ' zak³adamy w takim razie ¿e to JPG
            oStream = IO.File.OpenRead(oFile.InBufferPathName)
        End If

        Dim oDecoder As BitmapDecoder
        Try
            oDecoder = Await BitmapDecoder.CreateAsync(oStream.AsRandomAccessStream)
        Catch
            Return Nothing
        End Try

        Dim oTransform As New BitmapTransform()

        If oDecoder.PixelHeight > sourceImageHeightLimit Then
            Dim scalingFactor As Double = sourceImageHeightLimit / oDecoder.PixelHeight
            oTransform.ScaledWidth = Math.Floor(oDecoder.PixelWidth * scalingFactor)
            oTransform.ScaledHeight = Math.Floor(oDecoder.PixelHeight * scalingFactor)
        End If

        Using softbitmap As SoftwareBitmap = Await oDecoder.GetSoftwareBitmapAsync(
                oDecoder.BitmapPixelFormat, BitmapAlphaMode.Premultiplied, oTransform,
                ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage)

            Dim convertedBitmap As SoftwareBitmap
            ' If FaceDetector.IsBitmapPixelFormatSupported(softbitmap.BitmapPixelFormat) Then
            If softbitmap.BitmapPixelFormat <> faceDetectionPixelFormat Then
                convertedBitmap = SoftwareBitmap.Convert(softbitmap, faceDetectionPixelFormat)
            Else
                convertedBitmap = softbitmap
            End If

            Dim detectedFacesList = Await _FaceEngine.DetectFacesAsync(convertedBitmap)

            Dim oAzure As MojeAzure = AzureFromFacesList(detectedFacesList, convertedBitmap.PixelWidth, convertedBitmap.PixelHeight)
            convertedBitmap.Dispose()

            If detectedFacesList Is Nothing Then Return Nothing

            Dim oExif As New Vblib.ExifTag(Nazwa)
            oExif.Keywords = "-f" & oAzure.Faces.lista.Count
            oExif.AzureAnalysis = oAzure

            Return oExif
        End Using

        oStream?.Dispose()
        oArchive?.Dispose()

    End Function

    Private Function AzureFromFacesList(detectedFacesList As IList(Of DetectedFace), iWidth As Integer, iHeight As Integer) As MojeAzure
        ' zabezpieczenie przed wywo³aniem na starszych Windows
        If Not OperatingSystem.IsWindows Then Return Nothing
        If Not OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240) Then Return Nothing

        Dim oRet As New MojeAzure
        oRet.Faces = New ListTextWithProbabAndBox
        If detectedFacesList Is Nothing Then Return oRet
        If detectedFacesList.Count < 1 Then Return oRet

        For Each oFace As DetectedFace In detectedFacesList
            Dim oNew As New TextWithProbAndBox
            oNew.tekst = "face"
            oNew.X = 100.0 * oFace.FaceBox.X / iWidth
            oNew.Y = 100.0 * oFace.FaceBox.Y / iHeight
            oNew.Width = 100.0 * oFace.FaceBox.Width / iWidth
            oNew.Height = 100.0 * oFace.FaceBox.Height / iHeight
            oRet.Faces.Add(oNew)
        Next

        Return oRet
    End Function
End Class


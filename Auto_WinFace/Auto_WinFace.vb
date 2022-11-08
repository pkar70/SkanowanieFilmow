Imports System.IO
Imports System.Reflection.Metadata
Imports System.Text
Imports Vblib
Imports Windows.Graphics.Imaging
Imports Windows.Media
Imports Windows.Media.FaceAnalysis
Imports Windows.UI.Xaml.Controls
Imports Windows.UI.Xaml.Media


Public Class Auto_WinFace
    Inherits Vblib.AutotaggerBase
    Public Overrides ReadOnly Property Typek As Vblib.AutoTaggerType = Vblib.AutoTaggerType.Local
    Public Overrides ReadOnly Property Nazwa As String = "AUTO_WINFACE"
    Public Overrides ReadOnly Property MinWinVersion As String = "10.0"
    Public Overrides ReadOnly Property DymekAbout As String = "Próbuje policzyæ twarze u¿ywaj¹c Windows." & vbCrLf & "U¿ywa keyword -fN, gdzie n to liczba twarzy"

    Public Overrides Async Function GetForFile(oFile As OnePic) As Task(Of ExifTag)
        Dim licznik As Integer = Await ZrobMain(oFile)

        Dim oExif As New Vblib.ExifTag(Nazwa)
        oExif.Keywords = "-f" & licznik
        Return oExif
    End Function

    Private Shared _FaceEngine As FaceAnalysis.FaceDetector = Nothing


    ' https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/detect-and-track-faces-in-an-image

    ' The face detection process is quicker with a smaller image and so you may want to scale the source image down to a smaller size.
    Private Const sourceImageHeightLimit As Integer = 1280
    ' In the current version, the FaceDetector class only supports images in Gray8 or Nv12.
    Private Const faceDetectionPixelFormat As BitmapPixelFormat = BitmapPixelFormat.Gray8

    Private Async Function ZrobMain(oFile As Vblib.OnePic) As Task(Of Integer)

        ' jakby Create zajmowa³o wiêcej czasu - a przecie¿ moze byæ setki zdjêæ do OCR po kolei
        If _FaceEngine Is Nothing Then _FaceEngine = Await FaceDetector.CreateAsync

        Dim facesCount As Integer = 0

        ' na bitmape i OCR
        Using oStream As IO.Stream = IO.File.OpenRead(oFile.InBufferPathName)
            Dim oDecoder As BitmapDecoder = Await BitmapDecoder.CreateAsync(oStream.AsRandomAccessStream)

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
                If detectedFacesList IsNot Nothing Then facesCount = detectedFacesList.Count
                convertedBitmap.Dispose()
            End Using
        End Using

        Return facesCount
    End Function

End Class

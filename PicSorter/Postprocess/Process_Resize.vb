

Imports wingraph = Windows.Graphics.Imaging
Imports pkar.DotNetExtensions

Public Class Process_Resize800
    Inherits Process_ResizeBase

    Protected Overrides Property _iMaxSize As Integer = 800
    Public Overrides Property Nazwa As String = "Resize800"
    Public Overrides Property dymekAbout As String = "Zmniejszanie do 800 px na dłuższym boku (~SVGA, ~WVGA)"

End Class

' 800 + 28 %
Public Class Process_Resize1024
    Inherits Process_ResizeBase

    Protected Overrides Property _iMaxSize As Integer = 1024
    Public Overrides Property Nazwa As String = "Resize1024"
    Public Overrides Property dymekAbout As String = "Zmniejszanie do 1024 px na dłuższym boku (~XGA, ~WSVGA)"
End Class

' 1024 + 25 %
Public Class Process_Resize1280
    Inherits Process_ResizeBase

    Protected Overrides Property _iMaxSize As Integer = 1280
    Public Overrides Property Nazwa As String = $"Resize1280"
    Public Overrides Property dymekAbout As String = $"Zmniejszanie do 1280 px na dłuższym boku (~WXGA)"
End Class

' 1280 + 25 %
Public Class Process_Resize1600
    Inherits Process_ResizeBase

    Protected Overrides Property _iMaxSize As Integer = 1600
    Public Overrides Property Nazwa As String = $"Resize1600"
    Public Overrides Property dymekAbout As String = $"Zmniejszanie do 1600 px na dłuższym boku (~UXGA)"
End Class

' 1600 + 28 %
Public Class Process_Resize2048
    Inherits Process_ResizeBase

    Protected Overrides Property _iMaxSize As Integer = 2048
    Public Overrides Property Nazwa As String = $"Resize2048"
    Public Overrides Property dymekAbout As String = $"Zmniejszanie do 2048 px na dłuższym boku (~QXGA)"
End Class


Public Class Process_ResizeHalf
    Inherits Process_ResizeBase

    Protected Overrides Property _iMaxSize As Integer = -2
    Public Overrides Property Nazwa As String = $"ResizeHalf"
    Public Overrides Property dymekAbout As String = $"Zmniejszanie dwukrotne"
End Class

Public Class Process_ScalePic
    Inherits Process_ResizeBase

    Protected Overrides Property _iMaxSize As Integer = -2
    Public Overrides Property Nazwa As String = $"ScalePic()"
    Public Overrides Property dymekAbout As String = $"Skalowanie wg podanej skali (<1 zmniejszanie; zakres 0.2 do 5)"

    Protected Overrides Async Function ApplyMain(oPic As Vblib.OnePic, bPipeline As Boolean, params As String) As Task(Of Boolean)
        If Not OperatingSystem.IsWindows Then Return False
        If Not OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240) Then Return False

        oPic.InitEdit(bPipeline)

        If String.IsNullOrWhiteSpace(params) Then
            oPic.SkipEdit()
            Return False ' nie skalujemy, bo nie ma parametru
        End If

        Dim dScale As Double
        If Not Double.TryParse(params, dScale) Then
            oPic.SkipEdit()
            Return False ' nie skalujemy, bo nie ma parametru
        End If

        dScale = Math.Abs(dScale)

        If dScale < 0.2 OrElse dScale > 5 Then
            oPic.SkipEdit()
            Return False ' poza zakresem
        End If

        Using oSoftBitmap As wingraph.SoftwareBitmap = Await Process_AutoRotate.LoadSoftBitmapAsync(oPic)

            ' przelicz jaką skalę należy przyjąć
            Dim iHeight As Integer = oSoftBitmap.PixelHeight
            Dim iWidth As Integer = oSoftBitmap.PixelWidth

            Using oStream As New Windows.Storage.Streams.InMemoryRandomAccessStream

                Dim oEncoder As wingraph.BitmapEncoder = Await Process_AutoRotate.GetJpgEncoderAsync(oStream)

                ' kopiujemy informacje o tym co jest do zrobienia
                oEncoder.BitmapTransform.ScaledHeight = iHeight * dScale
                oEncoder.BitmapTransform.ScaledWidth = iWidth * dScale

                oEncoder.SetSoftwareBitmap(oSoftBitmap)

                ' gdy to robię na zwyklym AsRandomAccessStream to się wiesza
                Await oEncoder.FlushAsync()

                Process_AutoRotate.SaveSoftBitmap(oStream, oPic)

            End Using
        End Using

        oPic.EndEdit(True, True)

        Return True

    End Function
End Class


Public MustInherit Class Process_ResizeBase
    Inherits Vblib.PostProcBase

    Protected MustOverride Property _iMaxSize As Integer

#If SUPPORT_CALL_WITH_EXIF Then
    Protected Overrides Async Function ApplyMain(oPic As Vblib.OnePic, oExif As Vblib.ExifTag, sNewName As String) As Task(Of Boolean)
        ' oExif tutaj jest ignorowany
        Return Await ApplyMain(oPic, sNewName)
    End Function
#End If

    Protected Overrides Async Function ApplyMain(oPic As Vblib.OnePic, bPipeline As Boolean, params As String) As Task(Of Boolean)

        If Not OperatingSystem.IsWindows Then Return False
        If Not OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240) Then Return False


        oPic.InitEdit(bPipeline)

        Using oSoftBitmap As wingraph.SoftwareBitmap = Await Process_AutoRotate.LoadSoftBitmapAsync(oPic)

            ' przelicz jaką skalę należy przyjąć
            Dim iHeight As Integer = oSoftBitmap.PixelHeight
            Dim iWidth As Integer = oSoftBitmap.PixelWidth
            Dim dScale As Double

            If _iMaxSize > 0 Then
                ' mamy podany maxsize
                If iHeight > iWidth Then
                    dScale = _iMaxSize / iHeight
                Else
                    dScale = _iMaxSize / iWidth
                End If

                If dScale >= 1 Then
                    oPic.SkipEdit()
                    Return True ' nie skalujemy, bo plik jest mniejszy
                End If
            Else
                ' mamy podany współczynnik skalowania
                dScale = 1 / (-1 * _iMaxSize)
            End If


            Using oStream As New Windows.Storage.Streams.InMemoryRandomAccessStream

                Dim oEncoder As wingraph.BitmapEncoder = Await Process_AutoRotate.GetJpgEncoderAsync(oStream)

                ' kopiujemy informacje o tym co jest do zrobienia
                'oEncoder.BitmapTransform.Rotation
                oEncoder.BitmapTransform.ScaledHeight = iHeight * dScale
                oEncoder.BitmapTransform.ScaledWidth = iWidth * dScale

                oEncoder.SetSoftwareBitmap(oSoftBitmap)

                ' gdy to robię na zwyklym AsRandomAccessStream to się wiesza
                Await oEncoder.FlushAsync()

                Process_AutoRotate.SaveSoftBitmap(oStream, oPic)
                'Process_AutoRotate.SaveSoftBitmap(oStream, oPic.sFilenameEditDst, oPic.sFilenameEditSrc)

            End Using
        End Using

        oPic.EndEdit(True, True)

        'Dim oExif As Vblib.ExifTag = oPic.GetExifOfType(Vblib.ExifSource.FileExif)
        'If oExif IsNot Nothing Then oExif.Orientation = 1

        Return True
    End Function

End Class

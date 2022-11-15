

Imports wingraph = Windows.Graphics.Imaging


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


Public MustInherit Class Process_ResizeBase
    Inherits Vblib.PostProcBase

    Protected MustOverride Property _iMaxSize As Integer

    Protected Overrides Async Function ApplyMain(oPic As Vblib.OnePic, oExif As Vblib.ExifTag, sNewName As String) As Task(Of Boolean)
        ' oExif tutaj jest ignorowany
        Return Await ApplyMain(oPic, sNewName)
    End Function


    Protected Overrides Async Function ApplyMain(oPic As Vblib.OnePic, sNewName As String) As Task(Of Boolean)

        Dim srcFilename As String = oPic.InitEdit

        Dim oSoftBitmap As wingraph.SoftwareBitmap = Await Process_AutoRotate.LoadSoftBitmapAsync(srcFilename)

        ' przelicz jaką skalę należy przyjąć
        Dim iHeight As Integer = oSoftBitmap.PixelHeight
        Dim iWidth As Integer = oSoftBitmap.PixelWidth
        Dim dScale As Double
        If iHeight > iWidth Then
            dScale = _iMaxSize / iHeight
        Else
            dScale = _iMaxSize / iWidth
        End If

        If dScale < 1 Then Return False ' nie skalujemy, bo plik jest mniejszy

        Using oStream As New Windows.Storage.Streams.InMemoryRandomAccessStream

            Dim oEncoder As wingraph.BitmapEncoder = Await Process_AutoRotate.GetJpgEncoderAsync(oStream)

            ' kopiujemy informacje o tym co jest do zrobienia
            oEncoder.BitmapTransform.ScaledHeight = iHeight * dScale
            oEncoder.BitmapTransform.ScaledWidth = iWidth * dScale

            oEncoder.SetSoftwareBitmap(oSoftBitmap)

            ' gdy to robię na zwyklym AsRandomAccessStream to się wiesza
            Await oEncoder.FlushAsync()

            Process_AutoRotate.SaveSoftBitmap(oStream, oPic.InBufferPathName, srcFilename)

        End Using

        oPic.EndEdit()

        Return True
    End Function

End Class

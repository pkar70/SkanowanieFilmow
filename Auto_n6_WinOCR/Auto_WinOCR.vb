


Imports Windows.Media.Ocr
Imports Windows.Graphics.Imaging
Imports System.IO
Imports Vblib
Imports pkar.DotNetExtensions


Public Class AutoTag_WinOCR
    Inherits Vblib.AutotaggerBase
    Public Overrides ReadOnly Property Typek As Vblib.AutoTaggerType = Vblib.AutoTaggerType.Local
    Public Overrides ReadOnly Property Nazwa As String = ExifSource.AutoWinOCR ' "AUTO_WINOCR"
    Public Overrides ReadOnly Property MinWinVersion As String = "10.0"
    Public Overrides ReadOnly Property DymekAbout As String = "Próbuje zrobiæ OCR u¿ywaj¹c Windows." & vbCrLf & "U¿ywa pola UserComment"
    Public Shared ReadOnly Property includeMask As String = OnePic.ExtsPic & OnePic.ExtsStereo

    Public Overrides Async Function GetForFile(oFile As Vblib.OnePic) As Task(Of Vblib.ExifTag)
        If Not oFile.MatchesMasks(includeMask) Then Return Nothing

        Dim teksty As String = Await ZrobOCR(oFile)
        If teksty Is Nothing Then Return Nothing

        Dim oExif As New Vblib.ExifTag(Nazwa)
        oExif.UserComment = teksty
        Return oExif

    End Function

    Private Shared _OcrEngine As OcrEngine = Nothing

    ' g³ównie z ComixInMyLang, bo tam to robi³em kiedyœ
    Private Async Function ZrobOCR(oFile As Vblib.OnePic) As Task(Of String)

        ' zabezpieczenie przed wywo³aniem na starszych Windows
        If Not OperatingSystem.IsWindows Then Return False
        If Not OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240) Then Return False

        ' jakby Create zajmowa³o wiêcej czasu - a przecie¿ moze byæ setki zdjêæ do OCR po kolei
        If _OcrEngine Is Nothing Then _OcrEngine = OcrEngine.TryCreateFromLanguage(New Windows.Globalization.Language("pl-PL"))

        Dim rOCR As OcrResult

        ' na bitmape i OCR
        Using oStream As IO.Stream = oFile.SinglePicFromMulti ' Nothing


            'Dim oArchive As IO.Compression.ZipArchive = Nothing

            'If oFile.MatchesMasks("*.nar") Then
            '    oArchive = IO.Compression.ZipFile.OpenRead(oFile.InBufferPathName)
            '    For Each oInArch As IO.Compression.ZipArchiveEntry In oArchive.Entries
            '        If Not IO.Path.GetExtension(oInArch.Name).EqualsCI(".jpg") Then Continue For

            '        ' mamy JPGa (a nie XML na przyk³ad)
            '        oStream = New MemoryStream
            '        Dim oStreamTemp As Stream = oInArch.Open
            '        Await oStreamTemp.CopyToAsync(oStream)
            '        oStreamTemp.Dispose()
            '        Exit For
            '    Next
            'Else
            '    ' zak³adamy w takim razie ¿e to JPG
            '    oStream = IO.File.OpenRead(oFile.InBufferPathName)
            'End If

            Dim oDecoder As BitmapDecoder = Await BitmapDecoder.CreateAsync(oStream.AsRandomAccessStream)

            ' jakby by³o za du¿e, to siê poddaj
            If oDecoder.PixelWidth >= OcrEngine.MaxImageDimension Then Return Nothing
            If oDecoder.PixelHeight >= OcrEngine.MaxImageDimension Then Return Nothing

            Using softbitmap As SoftwareBitmap = Await oDecoder.GetSoftwareBitmapAsync()
                Try
                    rOCR = Await _OcrEngine.RecognizeAsync(softbitmap)
                Catch ex As Exception
                    Return Nothing ' np. za du¿y rozmiar
                    ' Image dimensions are too large! Check MaxImageDimension for maximum allowed image dimensions.
                    ' PanoramaZKopca.jpg, Image Size  : 16845x2444

                End Try
            End Using

            ' oStream?.Dispose()
        End Using
        'oArchive?.Dispose()

        Dim sRet As String = ""

        For Each oLine As OcrLine In rOCR.Lines
            sRet = sRet & "|" & oLine.Text
        Next
        If sRet = "" Then Return ""
        Return sRet.Substring(1)    ' bez pierwszego "|"
    End Function

End Class



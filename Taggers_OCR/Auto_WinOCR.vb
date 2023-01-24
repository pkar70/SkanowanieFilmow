


Imports Windows.Media.Ocr
Imports Windows.Graphics.Imaging
Imports System.IO
Imports Vblib

Public Class AutoTag_WinOCR
    Inherits Vblib.AutotaggerBase
    Public Overrides ReadOnly Property Typek As Vblib.AutoTaggerType = Vblib.AutoTaggerType.Local
    Public Overrides ReadOnly Property Nazwa As String = ExifSource.AutoWinOCR ' "AUTO_WINOCR"
    Public Overrides ReadOnly Property MinWinVersion As String = "10.0"
    Public Overrides ReadOnly Property DymekAbout As String = "Próbuje zrobiæ OCR u¿ywaj¹c Windows." & vbCrLf & "U¿ywa pola UserComment"
    Public Overrides ReadOnly Property includeMask As String = "*.jpg;*.jpg.thumb;*.nar"

    Public Overrides Async Function GetForFile(oFile As Vblib.OnePic) As Task(Of Vblib.ExifTag)
        If Not oFile.MatchesMasks(includeMask) Then Return Nothing

        Dim teksty As String = Await ZrobOCR(oFile)

        Dim oExif As New Vblib.ExifTag(Nazwa)
        oExif.UserComment = teksty
        Return oExif

    End Function

    Private Shared _OcrEngine As OcrEngine = Nothing

    ' g³ównie z ComixInMyLang, bo tam to robi³em kiedyœ
    Private Async Function ZrobOCR(oFile As Vblib.OnePic) As Task(Of String)

        ' jakby Create zajmowa³o wiêcej czasu - a przecie¿ moze byæ setki zdjêæ do OCR po kolei
        If _OcrEngine Is Nothing Then _OcrEngine = OcrEngine.TryCreateFromLanguage(New Windows.Globalization.Language("pl-PL"))

        Dim rOCR As OcrResult

        ' na bitmape i OCR
        Dim oStream As IO.Stream = Nothing
        Dim oArchive As IO.Compression.ZipArchive = Nothing

        If oFile.MatchesMasks("*.nar") Then
            oArchive = IO.Compression.ZipFile.OpenRead(oFile.InBufferPathName)
            For Each oInArch As IO.Compression.ZipArchiveEntry In oArchive.Entries
                If Not oInArch.Name.ToLowerInvariant.EndsWith("jpg") Then Continue For

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

        Dim oDecoder As BitmapDecoder = Await BitmapDecoder.CreateAsync(oStream.AsRandomAccessStream)
        Using softbitmap As SoftwareBitmap = Await oDecoder.GetSoftwareBitmapAsync()
            rOCR = Await _OcrEngine.RecognizeAsync(softbitmap)
        End Using

        oStream?.Dispose()
        oArchive?.dispose()

        Dim sRet As String = ""

        For Each oLine As OcrLine In rOCR.Lines
            sRet = sRet & "|" & oLine.Text
        Next
        If sRet = "" Then Return ""
        Return sRet.Substring(1)    ' bez pierwszego "|"
    End Function

End Class



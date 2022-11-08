


Imports Windows.Media.Ocr
Imports Windows.Graphics.Imaging
Imports System.IO

Public Class AutoTag_WinOCR
    Inherits Vblib.AutotaggerBase
    Public Overrides ReadOnly Property Typek As Vblib.AutoTaggerType = Vblib.AutoTaggerType.Local
    Public Overrides ReadOnly Property Nazwa As String = "AUTO_WINOCR"
    Public Overrides ReadOnly Property MinWinVersion As String = "10.0"
    Public Overrides ReadOnly Property DymekAbout As String = "Próbuje zrobiæ OCR u¿ywaj¹c Windows." & vbCrLf & "U¿ywa pola UserComment"
    Public Overrides Async Function GetForFile(oFile As Vblib.OnePic) As Task(Of Vblib.ExifTag)
        Dim teksty As String = Await ZrobOCR(oFile)

        Dim oExif As New Vblib.ExifTag(Nazwa)
        oExif.UserComment = teksty
        Return oExif

    End Function

    Private Shared _OcrEngine As OcrEngine = Nothing

    ' g³ównie z ComixInMyLang, bo tam to robi³em kiedyœ
    Private Async Function ZrobOCR(oFile As Vblib.OnePic) As Task(Of String)

        ' jakby Create zajmowa³o wiêcej czasu - a przecie¿ moze byæ setki zdjêæ do OCR po kolei
        If _OcrEngine Is Nothing Then _OcrEngine = OcrEngine.TryCreateFromUserProfileLanguages

        Dim rOCR As OcrResult

        ' na bitmape i OCR
        Using oStream As IO.Stream = IO.File.OpenRead(oFile.InBufferPathName)
            Dim oDecoder As BitmapDecoder = Await BitmapDecoder.CreateAsync(oStream.AsRandomAccessStream)
            Using softbitmap As SoftwareBitmap = Await oDecoder.GetSoftwareBitmapAsync()
                rOCR = Await _OcrEngine.RecognizeAsync(softbitmap)
            End Using
        End Using

        Dim sRet As String = ""

        For Each oLine As OcrLine In rOCR.Lines
            sRet = sRet & "|" & oLine.Text
        Next
        If sRet = "" Then Return ""
        Return sRet.Substring(1)    ' bez pierwszego "|"
    End Function

End Class

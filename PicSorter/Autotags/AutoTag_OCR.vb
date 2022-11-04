
' wykorzystuje Microsoft.Windows.SDK.Contracts

Public Class AutoTag_OCR
    Inherits Vblib.AutotaggerBase

    Public Overrides ReadOnly Property Typek As Vblib.AutoTaggerType = Vblib.AutoTaggerType.Local
    Public Overrides ReadOnly Property Nazwa As String = "AUTO_OCR"
    Public Overrides ReadOnly Property MinWinVersion As String = "7.0"

    Public Overrides Async Function GetForFile(oFile As Vblib.OnePic) As Task(Of Vblib.ExifTag)
        Dim oNewExif As New Vblib.ExifTag(Nazwa)


    End Function


    'Private Async Function ZrobOCR() As Task(Of Boolean)
    '    If mlTeksty Is Nothing Then mlTeksty = New Collection(Of JedenText)

    '    Dim rOCR As Windows.Media.Ocr.OcrResult = Await Windows.Media.Ocr.OcrEngine.TryCreateFromUserProfileLanguages().RecognizeAsync(moSoftBmp)

    '    Dim sInput As String = uiTextOrg.Text
    '    If rOCR.Lines.Count < 1 Then Return False

    '    Dim oNew As JedenText = New JedenText
    '    oNew.sFileName = msCurrFile
    '    oNew.sOriginal = vbCrLf & vbCrLf & vbCrLf & vbCrLf & vbCrLf & msCurrFile & vbCrLf & vbCrLf
    '    mlTeksty.Add(oNew)

    '    sInput = sInput & oNew.sOriginal

    '    For Each oLine As Windows.Media.Ocr.OcrLine In rOCR.Lines
    '        oNew = New JedenText
    '        oNew.sFileName = msCurrFile
    '        oNew.sOriginal = oLine.Text.ToLower
    '        mlTeksty.Add(oNew)
    '        sInput = sInput & vbCrLf & oNew.sOriginal
    '    Next

    '    uiTextOrg.Text = sInput
    '    ' clipboard copy
    '    Return True
    'End Function

End Class

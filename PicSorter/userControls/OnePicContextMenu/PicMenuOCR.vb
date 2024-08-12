
Imports pkar.UI.Extensions


Public Class PicMenuOCR
    Inherits PicMenuBase

    Public Shared _Clip As String = "" ' public dla OCRwnd.xaml
    Private _PasteDesc As MenuItem
    Private _CopyOCRclip As MenuItem
    Private _CopyOCRlocal As MenuItem

    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If _wasApplied Then Return

        MyBase.OnApplyTemplate()

        If Not InitEnableDisable("OCR/descr", "Operacje na OCR", True) Then Return

        Me.Items.Clear()

        Me.Items.Add(NewMenuItem("OCR window", "Otworz okno zarządzania OCR", AddressOf uiOpenOCRwnd_Click))

        _CopyOCRclip = NewMenuItem("OCR to Clipboard", "Wyślij OCR do clipboard", AddressOf uiOCRtoClip_Click)
        Me.Items.Add(_CopyOCRclip)
        _CopyOCRlocal = NewMenuItem("Copy OCR", "Skopiowanie OCR do lokalnego schowka", AddressOf uiOCRcopy_Click)
        Me.Items.Add(_CopyOCRlocal)

        AddPasteMenu("Paste OCR", "Narzucenie zdjęciom OCR wg lokalnego schowka")
        _PasteDesc = NewMenuItem("Paste as Descr", "Dodanie OCR z lokalnego schowka jako Description", AddressOf uiOCRpasteDescr_Click, False)
        Me.Items.Add(_PasteDesc)

        _wasApplied = True
    End Sub

    Public Overrides Sub MenuOtwieramy()
        MyBase.MenuOtwieramy()

        If Not _wasApplied Then Return

        If Not UseSelectedItems Then
            Dim oExif As Vblib.ExifTag = GetFromDataContext()?.GetExifOfType(Vblib.ExifSource.AutoWinOCR)
            _CopyOCRclip.IsEnabled = oExif IsNot Nothing
            _CopyOCRlocal.IsEnabled = oExif IsNot Nothing
        End If
    End Sub


    Private Sub uiOCRpasteDescr_Click(sender As Object, e As RoutedEventArgs)
        ' z zabezpieczeniem przed wielokrotnym dodawaniem
        OneOrMany(Sub(x) If Not x.sumOfDescr.Contains(_Clip) Then x.AddDescription(New Vblib.OneDescription(_Clip, "")))
    End Sub

    Protected Overrides Sub Pastecalled()
        OneOrMany(Sub(x) x.ReplaceOrAddExif(New Vblib.ExifTag(Vblib.ExifSource.AutoWinOCR) With {.UserComment = _Clip}))
    End Sub

    Private Sub uiOCRcopy_Click(sender As Object, e As RoutedEventArgs)
        _Clip = SumaOCR()
        _miPaste.IsEnabled = _Clip IsNot Nothing
        _PasteDesc.IsEnabled = _Clip IsNot Nothing
    End Sub

    Private Sub uiOCRtoClip_Click(sender As Object, e As RoutedEventArgs)
        SumaOCR.SendToClipboard
    End Sub

    Private Sub uiOpenOCRwnd_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As OCRwnd

        If UseSelectedItems Then
            Dim exifek As New Vblib.ExifTag(Vblib.ExifSource.AutoWinOCR) With {.UserComment = SumaOCR()}
            oWnd = New OCRwnd(exifek)
        Else
            oWnd = New OCRwnd(GetFromDataContext)
        End If

        oWnd.Show()
    End Sub

    Private Function SumaOCR() As String
        Dim ret As String = ""
        OneOrMany(Sub(x)
                      Dim ocrExif As Vblib.ExifTag = x.GetExifOfType(Vblib.ExifSource.AutoWinOCR)
                      If ocrExif IsNot Nothing Then
                          ret = ret & ocrExif.UserComment & vbCrLf
                      End If
                  End Sub)
        Return ret
    End Function
End Class

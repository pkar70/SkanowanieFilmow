
Imports pkar.UI.Extensions


Public Class PicMenuOCR
    Inherits PicMenuBase

    Public Shared _myClip As String = ""
    Private _miPaste As MenuItem
    Private _miPasteDesc As MenuItem
    Private _miCopyOCRclip As MenuItem
    Private _miCopyOCRlocal As MenuItem

    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If _wasApplied Then Return

        MyBase.OnApplyTemplate()

        If Not InitEnableDisable("OCR/descr", "Operacje na OCR", True) Then Return

        Me.Items.Clear()

        Me.Items.Add(NewMenuItem("OCR window", "Otworz okno zarządzania OCR", AddressOf uiOpenOCRwnd_Click))

        _miCopyOCRclip = NewMenuItem("OCR to Clipboard", "Wyślij OCR do clipboard", AddressOf uiOCRtoClip_Click)
        Me.Items.Add(_miCopyOCRclip)
        _miCopyOCRlocal = NewMenuItem("Copy OCR", "Skopiowanie OCR do lokalnego schowka", AddressOf uiOCRcopy_Click)
        Me.Items.Add(_miCopyOCRlocal)

        _miPaste = NewMenuItem("Paste OCR", "Narzucenie zdjęciom OCR wg lokalnego schowka", AddressOf uiOCRpaste_Click)
        Me.Items.Add(_miPaste)
        _miPasteDesc = NewMenuItem("Paste as Descr", "Dodanie OCR z lokalnego schowka jako Description", AddressOf uiOCRpasteDescr_Click)
        Me.Items.Add(_miPasteDesc)

        AddHandler Me.SubmenuOpened, AddressOf StworzMenuOCR
        _wasApplied = True
    End Sub

    Private Sub StworzMenuOCR(sender As Object, e As RoutedEventArgs)

        _miPasteDesc.IsEnabled = Not String.IsNullOrEmpty(_myClip)
        _miPaste.IsEnabled = Not String.IsNullOrEmpty(_myClip)

        If Not UseSelectedItems Then
            Dim oExif As Vblib.ExifTag = GetFromDataContext.GetExifOfType(Vblib.ExifSource.AutoWinOCR)
            _miCopyOCRclip.IsEnabled = oExif IsNot Nothing
            _miCopyOCRlocal.IsEnabled = oExif IsNot Nothing
        End If

    End Sub

    Private Sub uiOCRpasteDescr_Click(sender As Object, e As RoutedEventArgs)
        ' z zabezpieczeniem przed wielokrotnym dodawaniem
        OneOrMany(Sub(x) If Not x.sumOfDescr.Contains(_myClip) Then x.AddDescription(New Vblib.OneDescription(_myClip, "")))
    End Sub

    Private Sub uiOCRpaste_Click(sender As Object, e As RoutedEventArgs)
        OneOrMany(Sub(x) x.ReplaceOrAddExif(New Vblib.ExifTag(Vblib.ExifSource.AutoWinOCR) With {.UserComment = _myClip}))
    End Sub

    Private Sub uiOCRcopy_Click(sender As Object, e As RoutedEventArgs)
        _myClip = SumaOCR()
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

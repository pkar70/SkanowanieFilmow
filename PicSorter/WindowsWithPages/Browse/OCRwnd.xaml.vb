Imports pkar.UI.Configs
Imports pkar.UI.Extensions

Public Class OCRwnd

    Private _oExif As Vblib.ExifTag
    Private _oPic As Vblib.OnePic

    Public Sub New(oExif As Vblib.ExifTag)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        _oExif = oExif
    End Sub
    Public Sub New(oPic As Vblib.OnePic)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        _oPic = oPic
        _oExif = oPic.GetExifOfType(Vblib.ExifSource.AutoWinOCR)
    End Sub

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        Me.InitDialogs

        If _oPic Is Nothing Then
            uiDoOCR.IsEnabled = False
            uiDoOCR.ToolTip = "Okno wywołane nie dla jednego zdjęcia"
            uiPicname.Text = ""
            uiSetPicOCR.IsEnabled = False
            uiSetPicOCR.ToolTip = "Okno wywołane nie dla jednego zdjęcia"
            uiSetPicDesc.IsEnabled = False
            uiSetPicDesc.ToolTip = "Okno wywołane nie dla jednego zdjęcia"
            'uiSetPicDesc
        Else
            uiDoOCR.IsEnabled = True
            uiDoOCR.ToolTip = "Zrób OCR na zdjęciu"
            uiPicname.Text = _oPic.sSuggestedFilename
            uiSetPicOCR.IsEnabled = True
            uiSetPicOCR.ToolTip = "Zapisz OCR do metadanych zdjęcia"
            uiSetPicDesc.IsEnabled = True
            uiSetPicDesc.ToolTip = "Zapisz OCR jako description zdjęcia"
        End If

        uiSpellCheck.GetSettingsBool
        uiLang_SelectionChanged(Nothing, Nothing)
    End Sub
    Private Async Sub uiDoOCR_Click(sender As Object, e As RoutedEventArgs)
        If _oPic Is Nothing Then
            Me.MsgBox("Ale nie mam oPic!")
            Return
        End If

        ' użyje wszystkich z OCR w nazwie, może w efekcie trafi na jakiś który umie ten typ pliku obsłużyć
        For Each engine As Vblib.AutotaggerBase In Application.gAutoTagery
            If Not engine.Nazwa.Contains("OCR") Then Continue For

            Dim newOCR As Vblib.ExifTag = Await engine.GetForFile(_oPic)
            If newOCR Is Nothing Then Continue For

            If uiUseCompact.IsChecked Then
                uiOCR.Text = newOCR.UserComment.Replace(vbCrLf, "|")
            Else
                uiOCR.Text = newOCR.UserComment.Replace("|", vbCrLf)
            End If
            Return
        Next

        Me.MsgBox("Nie widzę żadnego mechanizmu OCR")
    End Sub

    Private Sub uiLang_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        If uiLang.SelectedItem Is Nothing Then Return
        Dim oCI As ComboBoxItem = TryCast(uiLang.SelectedItem, ComboBoxItem)
        Dim jezyk As String = TryCast(oCI?.Content, String)
        If String.IsNullOrWhiteSpace(jezyk) Then Return

        If uiOCR Is Nothing Then Return
        uiOCR.Language = Markup.XmlLanguage.GetLanguage(jezyk)
    End Sub

    Private Sub uiCopy_Click(sender As Object, e As RoutedEventArgs)
        PicMenuOCR._myClip = uiOCR.Text
    End Sub

    Private Sub uiSendClip_Click(sender As Object, e As RoutedEventArgs)
        uiOCR.Text.SendToClipboard
    End Sub

    Private Sub uiSetPicOCR_Click(sender As Object, e As RoutedEventArgs) Handles uiSetPicOCR.Click
        If _oPic Is Nothing Then
            Me.MsgBox("Ale nie mam oPic!")
            Return
        End If

        Dim newOCR As New Vblib.ExifTag(Vblib.ExifSource.AutoWinOCR)
        newOCR.UserComment = uiOCR.Text
        _oPic.ReplaceOrAddExif(newOCR)
    End Sub

    Private Sub uiUseCompact_Click(sender As Object, e As RoutedEventArgs)
        If uiUseCompact.IsChecked Then
            uiOCR.Text = uiOCR.Text.Replace(vbCrLf, "|")
        Else
            uiOCR.Text = uiOCR.Text.Replace("|", vbCrLf)
        End If

    End Sub

    Private Sub uiSetPicDesc_Click(sender As Object, e As RoutedEventArgs)
        If _oPic Is Nothing Then
            Me.MsgBox("Ale nie mam oPic!")
            Return
        End If

        _oPic.AddDescription(New Vblib.OneDescription(uiOCR.Text, ""))

    End Sub

    Private Sub Window_DataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs)
        Dim oPic As Vblib.OnePic = e.NewValue
        If oPic Is Nothing Then Return
        _oPic = oPic
        _oExif = oPic.GetExifOfType(Vblib.ExifSource.AutoWinOCR)

        Window_Loaded(Nothing, Nothing)
    End Sub
End Class

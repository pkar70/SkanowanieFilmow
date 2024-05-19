Public Class SimpleOCR

    Private _orgOCR As String
    Private _readonly As Boolean

    Public Sub New(bReadOnly As Boolean)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        _readonly = bReadOnly
    End Sub


    Private Async Sub uiApply_Click(sender As Object, e As RoutedEventArgs)

        ' bez zmian
        If _orgOCR = uiAllOCR.Text Then Return

        Dim oPicek As ProcessBrowse.ThumbPicek = DataContext

        If uiAllOCR.Text.Trim.Length < 1 Then
            If Not Await Vblib.DialogBoxYNAsync("Na pewno skasować OCR?") Then
                uiAllOCR.Text = _orgOCR
                Return
            End If

            oPicek.oPic.RemoveExifOfType(Vblib.ExifSource.AutoWinOCR)
        Else
            Dim oExif As Vblib.ExifTag = oPicek.oPic.GetExifOfType(Vblib.ExifSource.AutoWinOCR)
            oExif.UserComment = uiAllOCR.Text
        End If

    End Sub

    Private Sub Window_DataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs)

        If uiPinUnpin.IsPinned Then Return

        Dim oPicek As ProcessBrowse.ThumbPicek = uiPinUnpin.EffectiveDatacontext

        'uiFileName.Text = oPicek.oPic.sSuggestedFilename

        Dim oExif As Vblib.ExifTag = oPicek.oPic.GetExifOfType(Vblib.ExifSource.AutoWinOCR)
        If oExif Is Nothing Then
            uiAllOCR.Text = ""
            uiAllOCR.IsEnabled = False
            uiApply.IsEnabled = False
            Return
        End If

        _orgOCR = oExif.UserComment
        uiAllOCR.Text = _orgOCR
        uiAllOCR.IsEnabled = True
        uiApply.IsEnabled = True

        If _readonly Then uiApply.IsEnabled = False
    End Sub
End Class


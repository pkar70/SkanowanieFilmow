

Class ProcessPic
    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        AktualizujGuziki()
    End Sub

    Private Sub AktualizujGuziki()
        Dim counter As Integer = Application.GetBuffer.Count
        uiBrowse.Content = $"Browse ({counter})"
        uiAutotag.Content = $"Try autotag ({counter})"
        uiLocalArch.Content = $"Local arch ({counter})"
        uiPublish.Content = $"Publish ({counter})"

        If counter = 0 Then
            uiAutotag.IsEnabled = False
            uiBatchEdit.IsEnabled = False
            uiLocalArch.IsEnabled = False
            uiPublish.IsEnabled = False
        End If

    End Sub


    Private Sub uiBrowse_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New ProcessBrowse
        oWnd.ShowDialog()
        AktualizujGuziki()
    End Sub

    Private Sub uiAutotag_Click(sender As Object, e As RoutedEventArgs) Handles uiAutotag.Click
        Dim oWnd As New AutoTags
        oWnd.ShowDialog()
    End Sub

    Private Sub uiBatchEdit_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New BatchEdit
        oWnd.ShowDialog()
    End Sub
End Class

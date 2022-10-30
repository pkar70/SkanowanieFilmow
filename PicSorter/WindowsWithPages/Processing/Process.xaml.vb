Class ProcessPic
    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        Dim counter As Integer = 0
        uiAutotag.Content = $"Try autotag ({counter})"
        uiApplyTag.Content = $"Apply tags ({counter})"
        uiLocalArch.Content = $"Local arch ({counter})"
        uiPublish.Content = $"Publish ({counter})"
    End Sub
End Class

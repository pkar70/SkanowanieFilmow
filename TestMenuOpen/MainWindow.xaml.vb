Class MainWindow
    Private Sub uiActionOpen_Click(sender As Object, e As RoutedEventArgs)
        uiActionsPopup.IsOpen = Not uiActionsPopup.IsOpen
    End Sub

    Private Sub uiActionSelectFilter_Click(sender As Object, e As RoutedEventArgs)
        Debug.WriteLine("select click")
    End Sub

    Private Sub MenuItem_SubmenuOpened(sender As Object, e As RoutedEventArgs)
        Debug.WriteLine("MenuItem_SubmenuOpened: " & TryCast(sender, MenuItem).Header)
    End Sub

    Private Sub uiActionSelectSubmenu_Click(sender As Object, e As RoutedEventArgs)
        Debug.WriteLine("submenu click")
    End Sub

    Private Sub uiActionsPopup_Opened(sender As Object, e As EventArgs)
        Debug.WriteLine("popup opened")
    End Sub

    Private Sub Button_Click(sender As Object, e As RoutedEventArgs)

    End Sub

    Private Sub MenuItem_Click(sender As Object, e As RoutedEventArgs)
        MsgBox("klikłeś " & TryCast(sender, MenuItem).Header)
    End Sub

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        'Dim cosik = New MenuVerticalV
    End Sub
End Class

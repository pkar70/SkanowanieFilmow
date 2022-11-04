


Class SettingsMisc
    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        uiFullJSON.GetSettingsBool
    End Sub

    Private Sub uiOK_Click(sender As Object, e As RoutedEventArgs)
        uiFullJSON.SetSettingsBool
        Me.NavigationService.GoBack()
    End Sub
End Class

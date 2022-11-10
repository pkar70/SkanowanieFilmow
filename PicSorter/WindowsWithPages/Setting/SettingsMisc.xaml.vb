


Class SettingsMisc
    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        uiFullJSON.GetSettingsBool
        uiAzureEndpoint.GetSettingsString
        uiAzureSubscriptionKey.GetSettingsString
        uiAzurePaid.GetSettingsBool
        uiNoDelConfirm.GetSettingsBool
    End Sub

    Private Sub uiOK_Click(sender As Object, e As RoutedEventArgs)
        uiFullJSON.SetSettingsBool
        uiAzureEndpoint.SetSettingsString
        uiAzureSubscriptionKey.SetSettingsString
        uiAzurePaid.SetSettingsBool
        uiNoDelConfirm.SetSettingsBool
        Me.NavigationService.GoBack()
    End Sub

    Private Sub uiSettMaps_Click(sender As Object, e As RoutedEventArgs)
        Me.NavigationService.Navigate(New SettingsMapsy)
    End Sub
End Class

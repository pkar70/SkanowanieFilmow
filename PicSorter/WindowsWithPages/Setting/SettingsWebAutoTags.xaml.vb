Imports pkar.UI.Configs.Extensions

Class SettingsWebAutoTags

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        uiAzureEndpoint.GetSettingsString
        uiAzureSubscriptionKey.GetSettingsString
        uiAzurePaid.GetSettingsBool
        uiAzureMaxBatch.GetSettingsInt()

        uiVisualCrossSubscriptionKey.GetSettingsString
        uiVisualCrossPaid.GetSettingsBool
        uiVisualCrossMaxBatch.GetSettingsInt
    End Sub

    Private Sub uiOK_Click(sender As Object, e As RoutedEventArgs)

        uiAzureEndpoint.SetSettingsString
        uiAzureSubscriptionKey.SetSettingsString
        uiAzurePaid.SetSettingsBool
        uiAzureMaxBatch.SetSettingsInt()

        uiVisualCrossSubscriptionKey.SetSettingsString
        uiVisualCrossPaid.SetSettingsBool
        uiVisualCrossMaxBatch.GetSettingsInt

        Me.NavigationService.GoBack()
    End Sub


End Class

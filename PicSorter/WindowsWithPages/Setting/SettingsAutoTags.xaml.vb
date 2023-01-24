﻿Class SettingsAutoTags

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        uiAzureEndpoint.GetSettingsString
        uiAzureSubscriptionKey.GetSettingsString
        uiAzurePaid.GetSettingsBool

        uiVisualCrossSubscriptionKey.GetSettingsString
        uiVisualCrossPaid.GetSettingsBool
    End Sub

    Private Sub uiOK_Click(sender As Object, e As RoutedEventArgs)

        uiAzureEndpoint.SetSettingsString
        uiAzureSubscriptionKey.SetSettingsString
        uiAzurePaid.SetSettingsBool

        uiVisualCrossSubscriptionKey.SetSettingsString
        uiVisualCrossPaid.SetSettingsBool

        Me.NavigationService.GoBack()
    End Sub


End Class

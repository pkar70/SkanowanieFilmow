Imports pkar.UI.Configs.Extensions
Imports pkar.UI.Extensions


Class SettingsPublishOptions
    Private Sub uiOK_Click(sender As Object, e As RoutedEventArgs)
        uiPublishUseAzure.SetSettingsBool
        uiPublishAddMaplink.SetSettingsBool
        uiPublishShowSerno.SetSettingsBool
        uiDefaultCopyr.SetSettingsString
    End Sub

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        uiPublishUseAzure.GetSettingsBool
        uiPublishAddMaplink.GetSettingsBool
        uiPublishShowSerno.GetSettingsBool
        uiDefaultCopyr.GetSettingsString
    End Sub
End Class

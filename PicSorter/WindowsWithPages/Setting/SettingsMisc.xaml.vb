
Imports vb14 = Vblib.pkarlibmodule14


Class SettingsMisc
    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        uiFullJSON.GetSettingsBool
        uiAzureEndpoint.GetSettingsString
        uiAzureSubscriptionKey.GetSettingsString
        uiAzurePaid.GetSettingsBool
        uiNoDelConfirm.GetSettingsBool
        uiBakDelayDays.GetSettingsInt(iDefault:=7)
        uiJpgQuality.GetSettingsInt(iDefault:=80)
    End Sub

    Private Sub uiOK_Click(sender As Object, e As RoutedEventArgs)

        If uiJpgQuality.Value < 60 Then
            vb14.DialogBox($"Za niska jakość JPG ({uiJpgQuality.Value} < 60)")
            Return
        End If

        uiFullJSON.SetSettingsBool
        uiAzureEndpoint.SetSettingsString
        uiAzureSubscriptionKey.SetSettingsString
        uiAzurePaid.SetSettingsBool
        uiNoDelConfirm.SetSettingsBool
        uiBakDelayDays.SetSettingsInt
        uiJpgQuality.SetSettingsInt()
        Me.NavigationService.GoBack()
    End Sub

    Private Sub uiSettMaps_Click(sender As Object, e As RoutedEventArgs)
        Me.NavigationService.Navigate(New SettingsMapsy)
    End Sub
End Class

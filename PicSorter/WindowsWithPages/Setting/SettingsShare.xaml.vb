
Imports pkar.WPF.Configs

Class SettingsShare

    Private Sub uiShareChannels_Click(sender As Object, e As RoutedEventArgs)
        Me.NavigationService.Navigate(New SettingsShareChannels)
    End Sub

    Private Sub uiShareLogins_Click(sender As Object, e As RoutedEventArgs)
        Me.NavigationService.Navigate(New SettingsShareLogins)
    End Sub

    Private Sub uiShareServers_Click(sender As Object, e As RoutedEventArgs)

    End Sub

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        uiServerEnabled.GetSettingsBool    ' na razie zawsze FALSE
        uiServerEnabled.IsEnabled = (Application.GetShareLogins.Count > 0)
    End Sub

    Private Sub uiSrvEnable_Check(sender As Object, e As RoutedEventArgs)
        uiServerEnabled.SetSettingsBool

        If uiServerEnabled.IsChecked Then
            Application.gWcfServer = New lib_n6_httpSrv.ServerWrapper(
                Application.GetShareLogins, Application.gDbase)
            Application.gWcfServer.StartSvc()
        Else
            If Application.gWcfServer IsNot Nothing Then Application.gWcfServer.StopSvc()
        End If
    End Sub
End Class


Imports pkar.WPF.Configs

Class SettingsShare

    Private Sub uiShareChannels_Click(sender As Object, e As RoutedEventArgs)
        Me.NavigationService.Navigate(New SettingsShareChannels)
    End Sub

    Private Sub uiShareLogins_Click(sender As Object, e As RoutedEventArgs)
        Me.NavigationService.Navigate(New SettingsShareLogins)
    End Sub

    Private Sub uiShareServers_Click(sender As Object, e As RoutedEventArgs)
        Me.NavigationService.Navigate(New SettingsShareServers)
    End Sub

    Dim _loading As Boolean = True

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        uiServerEnabled.GetSettingsBool
        uiUploadBlocked.GetSettingsBool
        uiServerEnabled.IsEnabled = (Application.GetShareLogins.Count > 0)
        uiMyName.Text = Environment.MachineName
        uiLastAccess.DataContext = Application.gLastLoginSharing
        uiSharingAutoUploadComment.GetSettingsBool
        _loading = False
    End Sub

    Private Sub uiSrvEnable_Check(sender As Object, e As RoutedEventArgs)
        If _loading Then Return

        uiServerEnabled.SetSettingsBool
        If uiServerEnabled.IsChecked Then
            StartServicing()
        Else
            If Application.gWcfServer IsNot Nothing Then Application.gWcfServer.StopSvc()
        End If
    End Sub

    Private Sub uiUploadBlocked_Check(sender As Object, e As RoutedEventArgs)
        uiUploadBlocked.SetSettingsBool
    End Sub

    Public Shared Sub StartServicing()
        Application.gWcfServer = New lib_sharingNetwork.ServerWrapper(
                Application.GetShareLogins, Application.gDbase,
                Application.gLastLoginSharing, Application.GetBuffer,
                Application.GetShareDescriptionsIn, Application.GetShareDescriptionsOut)
        Application.gWcfServer.StartSvc()

    End Sub

    Private Sub uiSharingAutoUploadComment_Checked(sender As Object, e As RoutedEventArgs)
        uiSharingAutoUploadComment.SetSettingsBool
    End Sub
End Class

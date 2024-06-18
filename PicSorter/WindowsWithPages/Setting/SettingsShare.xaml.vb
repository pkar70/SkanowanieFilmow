
Imports pkar.UI.Configs

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

    Private Sub uiShareAsWeb_Click(sender As Object, e As RoutedEventArgs)
        Me.NavigationService.Navigate(New SettingsShareAsWeb)
    End Sub
    Private Sub uiShareQueries_Click(sender As Object, e As RoutedEventArgs)
        Me.NavigationService.Navigate(New SettingsShareQueries)
    End Sub

    Dim _loading As Boolean = True

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        uiServerEnabled.GetSettingsBool
        uiUploadBlocked.GetSettingsBool
        uiServerEnabled.IsEnabled = (Application.GetShareLogins.Count > 0)
        uiMyName.Text = Environment.MachineName
        uiLastAccess.DataContext = Application.gLastLoginSharing
        uiSharingAutoUploadComment.GetSettingsBool
        'uiWebBuffPicLimit.GetSettingsInt()
        'uiHttpLog.GetSettingsBool
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

    Public Shared Sub StartServicing()
        Application.gWcfServer = New lib_sharingNetwork.ServerWrapper(
                Application.GetShareLogins, Application.gDbase,
                Application.gLastLoginSharing, Application.GetBuffer,
                Application.GetShareDescriptionsIn, Application.GetShareDescriptionsOut,
                Application.gPostProcesory,
                Application.GetDataFolder)
        Application.gWcfServer.StartSvc()

    End Sub

    Private Sub Page_Unloaded(sender As Object, e As RoutedEventArgs)
        'uiWebBuffPicLimit.SetSettingsInt()
        'uiHttpLog.SetSettingsBool
        uiSharingAutoUploadComment.SetSettingsBool
        uiUploadBlocked.SetSettingsBool
    End Sub

    Private Sub uiOpenLog_Click(sender As Object, e As RoutedEventArgs)
        Dim logpath As String = Application.gWcfServer.GetLogDir
        If String.IsNullOrWhiteSpace(logpath) Then Return

        Dim storFolder As New StorageFolder(logpath)
        storFolder.OpenExplorer()
    End Sub

End Class

Imports pkar.UI.Configs

Class SettingsShareAsWeb
    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        uiAsWebServer.GetSettingsBool
        uiWebBuffPicLimit.GetSettingsInt()
        uiAsWebPrintKwd.GetSettingsBool
        uiAsWebPrintDescr.GetSettingsBool
        uiAsWebPrintGeo.GetSettingsBool
        uiHttpLog.GetSettingsBool
        uiAsWebPrintFilename.GetSettingsBool
        uiAsWebPrintSerno.GetSettingsBool
        uiAsWebPrintDates.GetSettingsBool

    End Sub

    Private Sub uiOK_Click(sender As Object, e As RoutedEventArgs)
        uiAsWebServer.SetSettingsBool
        uiWebBuffPicLimit.SetSettingsInt()
        uiAsWebPrintKwd.SetSettingsBool
        uiAsWebPrintDescr.SetSettingsBool
        uiAsWebPrintGeo.SetSettingsBool
        uiHttpLog.SetSettingsBool
        uiAsWebPrintFilename.SetSettingsBool
        uiAsWebPrintSerno.SetSettingsBool
        uiAsWebPrintDates.SetSettingsBool
    End Sub

    Private Sub uiOpenLog_Click(sender As Object, e As RoutedEventArgs)
        Dim logpath As String = Application.gWcfServer.GetLogDir
        If String.IsNullOrWhiteSpace(logpath) Then Return

        Dim storFolder As New StorageFolder(logpath)
        storFolder.OpenExplorer()
    End Sub
End Class

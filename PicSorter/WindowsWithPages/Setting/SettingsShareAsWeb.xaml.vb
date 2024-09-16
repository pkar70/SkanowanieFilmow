Imports pkar.UI.Configs

' wydzielenie noHttpLog po to, by guzik OpenLog był ładnie...

Class SettingsShareAsWeb
    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)

        Dim defVals As Vblib.PublishMetadataOptions = Vblib.PublishMetadataOptions.GetDefault

        uiMetaOptions.DataContext = defVals

        uiAsWebServer.GetSettingsBool
        uiWebBuffPicLimit.Value = defVals.PicLimit
        uiHttpLog.IsChecked = Not defVals.noHttpLog
    End Sub

    Private Sub uiOK_Click(sender As Object, e As RoutedEventArgs)

        Dim defVals As Vblib.PublishMetadataOptions = uiMetaOptions.DataContext
        defVals.PicLimit = uiWebBuffPicLimit.Value
        defVals.noHttpLog = Not uiHttpLog.IsChecked

        defVals.SaveAsDefaults()
    End Sub

    Private Sub uiOpenLog_Click(sender As Object, e As RoutedEventArgs)
        Dim logpath As String = Application.gWcfServer.GetLogDir
        If String.IsNullOrWhiteSpace(logpath) Then Return

        Dim storFolder As New StorageFolder(logpath)
        storFolder.OpenExplorer()
    End Sub
End Class

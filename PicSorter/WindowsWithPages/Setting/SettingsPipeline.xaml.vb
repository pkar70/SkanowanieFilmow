Imports pkar.UI.Configs.Extensions
Imports pkar.UI.Extensions


Class SettingsPipeline
    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        uiWinFaceMaxAge.GetSettingsInt()
        uiWinFaceMinSize.GetSettingsInt()
    End Sub

    Private Sub uiOK_Click(sender As Object, e As RoutedEventArgs)
        uiWinFaceMaxAge.SetSettingsInt()
        uiWinFaceMinSize.SetSettingsInt()
    End Sub

    Private Sub uiWatermark_Click(sender As Object, e As RoutedEventArgs)
        Me.NavigationService.Navigate(New SettingsWatermark)
    End Sub

End Class

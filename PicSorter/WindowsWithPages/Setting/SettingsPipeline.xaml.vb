Imports pkar.UI.Configs.Extensions
Imports pkar.UI.Extensions


' być może wykorzystać
' https://github.com/PixiEditor/ColorPicker

Class SettingsPipeline
    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        uiWinFaceMaxAge.GetSettingsInt()
        uiWinFaceMinSize.GetSettingsInt()
        uiWinFaceAfterDeath.GetSettingsInt()
        uiWinFaceR.GetSettingsInt()
        uiWinFaceG.GetSettingsInt()
        uiWinFaceB.GetSettingsInt()
        uiWinFaceA.GetSettingsInt()
        uiEmbedTxtR.GetSettingsInt()
        uiEmbedTxtG.GetSettingsInt()
        uiEmbedTxtB.GetSettingsInt()
        uiEmbedTxtBwR.GetSettingsInt()
        uiEmbedTxtBwG.GetSettingsInt()
        uiEmbedTxtBwB.GetSettingsInt()
    End Sub

    Private Sub uiOK_Click(sender As Object, e As RoutedEventArgs)
        uiWinFaceMaxAge.SetSettingsInt()
        uiWinFaceMinSize.SetSettingsInt()
        uiWinFaceAfterDeath.SetSettingsInt()
        uiWinFaceR.SetSettingsInt()
        uiWinFaceG.SetSettingsInt()
        uiWinFaceB.SetSettingsInt()
        uiWinFaceA.SetSettingsInt()
        uiEmbedTxtR.SetSettingsInt()
        uiEmbedTxtG.SetSettingsInt()
        uiEmbedTxtB.SetSettingsInt()
        uiEmbedTxtBwR.SetSettingsInt()
        uiEmbedTxtBwG.SetSettingsInt()
        uiEmbedTxtBwB.SetSettingsInt()
    End Sub

    Private Sub uiWatermark_Click(sender As Object, e As RoutedEventArgs)
        Me.NavigationService.Navigate(New SettingsWatermark)
    End Sub

End Class

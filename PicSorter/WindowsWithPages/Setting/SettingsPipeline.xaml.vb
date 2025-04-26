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
        'uiWinFaceAverage.GetSettingsBool
        uiTrybZamazywania.GetSettingsInt
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
        'uiWinFaceAverage.SetSettingsBool
        uiTrybZamazywania.SetSettingsInt

    End Sub

    Private Sub uiWatermark_Click(sender As Object, e As RoutedEventArgs)
        Me.NavigationService.Navigate(New SettingsWatermark)
    End Sub

    'Private Sub uiWinFaceAverage_Checked(sender As Object, e As RoutedEventArgs)
    '    uiKolorekTwarzowy.Visibility = If(uiWinFaceAverage.IsChecked, Visibility.Collapsed, Visibility.Visible)
    'End Sub

    Private Sub uiTrybZamazywania_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        Dim tryb As Integer = uiTrybZamazywania.SelectedIndex

        uiKolorekTwarzowy.Visibility = Visibility.Collapsed
        uiBlurSettings.Visibility = Visibility.Collapsed

        Select Case tryb
            Case 0
                uiKolorekTwarzowy.Visibility = Visibility.Visible
            Case 1

            Case 2
                uiBlurSettings.Visibility = Visibility.Visible
        End Select

    End Sub
End Class


'Imports vb14 = Vblib.pkarlibmodule14
Imports pkar.UI.Configs.Extensions
Imports pkar.UI.Extensions

Class SettingsMisc
    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        Me.InitDialogs

        uiFullJSON.GetSettingsBool
        uiNoDelConfirm.GetSettingsBool
        uiBakDelayDays.GetSettingsInt()
        uiJpgQuality.GetSettingsInt()
        uiMaxThumbs.GetSettingsInt()
        uiCacheThumbs.GetSettingsBool()
        uiHideKeywords.GetSettingsBool
        uiHideThumbs.GetSettingsBool
        uiBigPicSize.GetSettingsInt()
        uiDragOutThumbs.GetSettingsBool
        uiWinFaceMaxAge.GetSettingsInt
        uiUseSpellCheck.GetSettingsBool
        uiAstroNotWhenWether.GetSettingsBool
        'uiStereoThumb.GetSettingsInt()
    End Sub

    Private Sub uiOK_Click(sender As Object, e As RoutedEventArgs)

        If uiJpgQuality.Value < 60 Then
            Me.MsgBox($"Za niska jakość JPG ({uiJpgQuality.Value} < 60)")
            Return
        End If

        uiFullJSON.SetSettingsBool
        uiNoDelConfirm.SetSettingsBool
        uiBakDelayDays.SetSettingsInt
        uiJpgQuality.SetSettingsInt()
        uiMaxThumbs.SetSettingsInt()
        uiCacheThumbs.SetSettingsBool()
        uiHideKeywords.SetSettingsBool
        uiHideThumbs.SetSettingsBool
        uiBigPicSize.SetSettingsInt
        uiDragOutThumbs.SetSettingsBool
        uiWinFaceMaxAge.SetSettingsInt
        uiUseSpellCheck.SetSettingsBool
        uiAstroNotWhenWether.SetSettingsBool
        'uiStereoThumb.SetSettingsInt

        Me.NavigationService.GoBack()
    End Sub

    Private Sub uiSettStereo_Click(sender As Object, e As RoutedEventArgs)
        Me.NavigationService.Navigate(New SettingsStereo)
    End Sub


    Private Sub uiSettMaps_Click(sender As Object, e As RoutedEventArgs)
        Me.NavigationService.Navigate(New SettingsMapsy)
    End Sub

    Private Sub uiWatermark_Click(sender As Object, e As RoutedEventArgs)
        Me.NavigationService.Navigate(New SettingsWatermark)
    End Sub
End Class

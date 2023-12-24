﻿
Imports vb14 = Vblib.pkarlibmodule14
Imports pkar.WPF.Configs.Extensions


Class SettingsMisc
    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
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
        'uiStereoThumb.GetSettingsInt()
    End Sub

    Private Sub uiOK_Click(sender As Object, e As RoutedEventArgs)

        If uiJpgQuality.Value < 60 Then
            vb14.DialogBox($"Za niska jakość JPG ({uiJpgQuality.Value} < 60)")
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

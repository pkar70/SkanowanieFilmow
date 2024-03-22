

Imports vb14 = Vblib.pkarlibmodule14
Imports pkar.UI.Configs.Extensions

Public Class AutoSplitWindow
    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        uiDayChange.GetSettingsBool
        uiHourGapOn.GetSettingsBool()
        uiHourGapInt.GetSettingsInt()
        uiGeoGapOn.GetSettingsBool()
        uiGeoGapInt.GetSettingsInt()
    End Sub

    Private Sub uiOK_Click(sender As Object, e As RoutedEventArgs)
        uiDayChange.SetSettingsBool
        uiHourGapOn.SetSettingsBool
        uiHourGapInt.SetSettingsInt
        uiGeoGapOn.SetSettingsBool
        uiGeoGapInt.SetSettingsInt
        Me.Close()
    End Sub
End Class

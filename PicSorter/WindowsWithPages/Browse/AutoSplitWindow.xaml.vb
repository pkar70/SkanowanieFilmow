

Imports vb14 = Vblib.pkarlibmodule14

Public Class AutoSplitWindow
    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        uiDayChange.GetSettingsBool
        uiHourGapOn.GetSettingsBool(bDefault:=True)
        uiHourGapInt.GetSettingsInt(iDefault:=36)
        uiGeoGapOn.GetSettingsBool(bDefault:=True)
            uiGeoGapInt.GetSettingsInt(iDefault:=20)
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

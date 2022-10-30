﻿Imports vb14 = Vblib.pkarlibmodule14

Class SettingsMain
    Private Sub uiMiscSett_Click(sender As Object, e As RoutedEventArgs)
        Me.NavigationService.Navigate(New SettingMisc)
    End Sub

    Private Sub uiGlobalSett_Click(sender As Object, e As RoutedEventArgs)
        Me.NavigationService.Navigate(New SettingsGlobal)
    End Sub

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        uiVersion.ShowAppVers
        If App.GetDataFolder(False) = "" Then
            uiGlobalSett_Click(Nothing, Nothing)
        End If
    End Sub

    Private Sub uiSettSources_Click(sender As Object, e As RoutedEventArgs)
        Me.NavigationService.Navigate(New SettingsSources)
    End Sub
End Class

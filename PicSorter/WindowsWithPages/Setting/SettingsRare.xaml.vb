
'Imports vb14 = Vblib.pkarlibmodule14
Imports pkar.UI.Configs.Extensions
Imports pkar.UI.Extensions

Class SettingsRare

    Private Sub uiDirTree_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New SettingsDirTree(True)
        oWnd.ShowDialog()
    End Sub

    Private Sub uiKeywords_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New SettingsKeywords
        oWnd.ShowDialog()
        ' Me.NavigationService.Navigate(New SettingsKeywords)
    End Sub

    Private Sub uiListSett_Click(sender As Object, e As RoutedEventArgs)
        Me.NavigationService.Navigate(New SettingListy)
    End Sub
    Private Sub uiGlobalSett_Click(sender As Object, e As RoutedEventArgs)
        Me.NavigationService.Navigate(New SettingsGlobal)
    End Sub

    Private Sub uiWebAutoTags_Click(sender As Object, e As RoutedEventArgs)
        Me.NavigationService.Navigate(New SettingsWebAutoTags)
    End Sub

    Private Sub uiAutoTagsDefs_Click(sender As Object, e As RoutedEventArgs)
        Me.NavigationService.Navigate(New SettingsAutoTagsDef)
    End Sub

    Private Sub uiSequenceSett_Click(sender As Object, e As RoutedEventArgs)
        Me.NavigationService.Navigate(New SettingsSequence)
    End Sub
End Class


Class SettingMisc
    Private Sub uiListCopyr_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New EditEntryHist(App.GetDataFolder, "Copyrights", "Dodaj właściciela praw", "(c) KTO, All rights reserved.")
        oWnd.Show()
    End Sub

    Private Sub uiListAuthor_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New EditEntryHist(App.GetDataFolder, "Authors", "Dodaj autora (zwykle: imię nazwisko)", "")
        oWnd.Show()
    End Sub

    Private Sub uiListCamera_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New EditEntryHist(App.GetDataFolder, "Cameras", "Dodaj model aparatu/skanera", "")
        oWnd.Show()
    End Sub
End Class

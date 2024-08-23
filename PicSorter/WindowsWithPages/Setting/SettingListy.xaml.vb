
Class SettingListy
    Private Sub uiListCopyr_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New EditEntryHist("Copyrights", "Dodaj właściciela praw", "(c) KTO. All rights reserved.")
        oWnd.Show()
    End Sub

    Private Sub uiListAuthor_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New EditEntryHist("Authors", "Dodaj autora (zwykle: imię nazwisko)", "")
        oWnd.Show()
    End Sub

    Private Sub uiListInetAuthor_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New EditEntryHist("inetauthors", "Dodaj autora (zwykle: imię nazwisko) zdjęć z Internet", "")
        oWnd.Show()
    End Sub
    'Private Sub uiListCameraMakers_Click(sender As Object, e As RoutedEventArgs)
    '    Dim oWnd As New EditEntryHist(App.GetDataFolder, "CameraMakers", "Dodaj producenta aparatu/skanera", "")
    '    oWnd.Show()
    'End Sub
    Private Sub uiListCameraModels_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New EditEntryHist("Cameras", "Dodaj model aparatu/skanera (lub producent # model)", "")
        oWnd.Show()
    End Sub

    ' shared, bo także z user context menu tychże
    Public Shared Sub uiListExec_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New EditEntryHist("Execs", "Dodaj executable do edycji/podglądu zdjęcia, użyj %f jako pic placeholder. Pamiętaj o cudzysłowach!", "")
        oWnd.Show()
    End Sub


    Private Sub uiSettMaps_Click(sender As Object, e As RoutedEventArgs)
        Me.NavigationService.Navigate(New SettingsMapsy)
    End Sub


End Class

﻿
Class SettingListy
    Private Sub uiListCopyr_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New EditEntryHist(App.GetDataFolder, "Copyrights", "Dodaj właściciela praw", "(c) KTO. All rights reserved.")
        oWnd.Show()
    End Sub

    Private Sub uiListAuthor_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New EditEntryHist(App.GetDataFolder, "Authors", "Dodaj autora (zwykle: imię nazwisko)", "")
        oWnd.Show()
    End Sub

    'Private Sub uiListCameraMakers_Click(sender As Object, e As RoutedEventArgs)
    '    Dim oWnd As New EditEntryHist(App.GetDataFolder, "CameraMakers", "Dodaj producenta aparatu/skanera", "")
    '    oWnd.Show()
    'End Sub
    Private Sub uiListCameraModels_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New EditEntryHist(App.GetDataFolder, "Cameras", "Dodaj model aparatu/skanera (lub producent # model)", "")
        oWnd.Show()
    End Sub

    Private Sub uiGeoPlaces_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New SettingsGeoPlaces
        oWnd.Show()
    End Sub
End Class

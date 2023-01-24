Class SettingsSlideshow
    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        uiSlideShowSeconds.GetSettingsInt
        uiSlideShowAlsoX.GetSettingsBool
    End Sub

    Private Sub uiOK_Click(sender As Object, e As RoutedEventArgs)

        uiSlideShowSeconds.SetSettingsInt
        uiSlideShowAlsoX.SetSettingsBool

        Me.NavigationService.GoBack()
    End Sub
End Class

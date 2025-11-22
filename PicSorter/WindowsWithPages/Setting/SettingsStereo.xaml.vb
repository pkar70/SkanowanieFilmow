Imports pkar.UI.Configs
Imports pkar
Imports pkar.UI.Extensions
Class SettingsStereo
    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        uiStereoSPMPath.GetSettingsString
        uiStereoBigAnaglyph.GetSettingsBool
        uiStereoMaxDiffSecs.GetSettingsInt
        uiStereoMaxDiffMeteres.GetSettingsInt()
        uiStereoThumbAnaglyph.GetSettingsBool
    End Sub

    Private Sub uiOK_Click(sender As Object, e As RoutedEventArgs)

        uiStereoSPMPath.SetSettingsString
        uiStereoBigAnaglyph.SetSettingsBool
        uiStereoMaxDiffSecs.SetSettingsInt
        uiStereoMaxDiffMeteres.SetSettingsInt
        uiStereoThumbAnaglyph.SetSettingsBool

        Me.NavigationService.GoBack()
    End Sub


    Private Sub uiBrowseSPMFolder(sender As Object, e As RoutedEventArgs)
        Dim sciezka As String = SettingsGlobal.FileOpenBrowser("C:\Program Files\StereoPhotoMaker", "wskaż plik StereoPhotoMaker", "*.exe")
        If String.IsNullOrEmpty(sciezka) Then Return

        If Not IO.Path.GetExtension(sciezka).EqualsCI(".exe") Then
            Vblib.DialogBox("Wskazany plik nie jest .exe!")
            Return
        End If

        uiStereoSPMPath.Text = sciezka
    End Sub

    Private Sub TextBox_MouseDoubleClick(sender As Object, e As MouseButtonEventArgs)
        Dim urik As New Uri("https://stereo.jpn.org/eng/stphmkr/")
        urik.OpenBrowser
    End Sub
End Class

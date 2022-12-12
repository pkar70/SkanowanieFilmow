Class MainWindow
    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)

        Dim oFbConfig As New Vblib.CloudConfig

        oFbConfig.sUsername = ""
        oFbConfig.sPswd = ""

        Dim oFB As New Publish_std2_Facebook.Publish_Facebook_Post
        oFB.konfiguracja = oFbConfig

        oFB.Login()

        ' https://developers.facebook.com/docs/development/register
    End Sub
End Class

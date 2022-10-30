
'główne settings

Public Class SettingsWindow
    Inherits NavigationWindow

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        Me.Content = New SettingsMain
    End Sub
End Class

Class MainWindow
    Inherits Window

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        InitLib(Nothing)
        Me.Content = New MainPage
    End Sub
End Class

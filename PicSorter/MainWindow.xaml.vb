

Class MainWindow
    Inherits Window

    Private Async Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        InitLib(Nothing)
        Me.Content = New MainPage


        'Dim chomik As New CloudArch_std14_Chomikuj.Cloud_Chomikuj
        'Await chomik.Login

    End Sub

    Private Sub Window_Closing(sender As Object, e As ComponentModel.CancelEventArgs)
        If Application.Current.Windows.Count > 2 Then
            Dim sAppName As String = Application.Current.MainWindow.GetType().Assembly.GetName.Name
            Dim iRet As MessageBoxResult = MessageBox.Show("Zamknąć program?", sAppName, MessageBoxButton.YesNo)
            If iRet = MessageBoxResult.Yes Then Application.Current.Shutdown()
        End If
    End Sub
End Class

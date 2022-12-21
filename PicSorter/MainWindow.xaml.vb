


Class MainWindow
    Inherits Window

    Private Async Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        InitLib(Nothing)
        Me.Content = New MainPage

        ' *TODO* to tylko czasowo
        'Dim sChcemy As String = "Degoo"
        ' Dim sChcemy As String = "Shutterfly"
        Dim sChcemy As String = "NIC"
        For Each oItem In Application.GetCloudArchives.GetList
            'If oItem.sProvider = "Degoo" Then
            If oItem.sProvider = sChcemy Then Await oItem.Login
        Next

    End Sub

    Private Sub Window_Closing(sender As Object, e As ComponentModel.CancelEventArgs)
        If Application.Current.Windows.Count > 2 Then
            Dim sAppName As String = Application.Current.MainWindow.GetType().Assembly.GetName.Name
            Dim iRet As MessageBoxResult = MessageBox.Show("Zamknąć program?", sAppName, MessageBoxButton.YesNo)
            If iRet = MessageBoxResult.Yes Then Application.Current.Shutdown()
        End If
    End Sub
End Class

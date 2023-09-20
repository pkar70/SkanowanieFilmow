Imports pkar
Imports Vblib


Class SettingsShareLogins

    Dim _channels As List(Of String)

    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        uiLista.ItemsSource = Application.GetShareLogins.GetList
        Dim adres As String = Await vb14_GetMyIP.GetMyIP.GetIPString
        _channels = Application.GetShareChannels.GetList.Select(Of String)(Function(x) x.nazwa).ToList
        _channels.Sort()

    End Sub

#Region "lista loginów"

    Private Sub uiEdit_Click(sender As Object, e As RoutedEventArgs)
        Dim oLogin As ShareLogin = TryCast(sender, FrameworkElement)?.DataContext
        If oLogin Is Nothing Then Return
        ShowToEdit(oLogin)
    End Sub

    Private Async Sub uiEmail_Click(sender As Object, e As RoutedEventArgs)
        Dim oLogin As ShareLogin = TryCast(sender, FrameworkElement)?.DataContext
        If oLogin Is Nothing Then Return

        Dim adres As String = Await vb14_GetMyIP.GetMyIP.GetIPString

        ' wysłanie email
        Dim subject As String = "Dane logowania do mojego PicSort"
        Dim body As String = $"PicSort://{adres}/{oLogin.login}"
        Dim email As New AsNuget_UseMapi.SendFileTo.MAPI
        email.SendMailPopup(subject, body)

        If Await Vblib.DialogBoxYNAsync("Poczekać na połączenie (i zabezpieczyć je)? 'Nie' oznacza mniejsze bezpieczeństwo") Then
            ' czekamy
        Else
            ' bez zabezpieczeń
            oLogin.ipAddr = ""
            oLogin.netmask = ""
            oLogin.remoteHostName = ""
        End If

        Application.GetShareLogins.Save() ' zmienione uprawnienia
    End Sub

    Private Async Sub uiDel_Click(sender As Object, e As RoutedEventArgs)
        Dim oLogin As ShareLogin = TryCast(sender, FrameworkElement)?.DataContext
        If oLogin Is Nothing Then Return

        If Not Await Vblib.DialogBoxYNAsync($"Usunąć login {oLogin.displayName}?") Then Return

        Application.GetShareLogins.Remove(oLogin)
        Page_Loaded(Nothing, Nothing)
    End Sub

    Private Sub ShowToEdit(oLogin As ShareLogin)
        uiEditLogin.Visibility = Visibility.Visible
        uiEditLogin.DataContext = oLogin.Clone
    End Sub

    Private Sub uiAddLogin_Click(sender As Object, e As RoutedEventArgs)
        Dim oNew As New ShareLogin
        ShowToEdit(oNew)
    End Sub

#End Region

    Private Sub uiAddChannel_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oLogin As ShareLogin = oFE?.DataContext
        If oLogin Is Nothing Then Return

        Dim oNew As New ShareChannel
        If oLogin.channels Is Nothing Then oLogin.channels = New List(Of ShareChannel)
        oLogin.channels.Add(oNew)

        uiListaKanalow.ItemsSource = Nothing
        uiListaKanalow.ItemsSource = oLogin.channels

    End Sub

    Private Sub uiComboQuery_Loaded(sender As Object, e As RoutedEventArgs)
        Dim oCB As ComboBox = sender
        If oCB Is Nothing Then Return

        oCB.ItemsSource = _channels
    End Sub

    Private Sub uiDelChannel_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oChannel As ShareChannel = oFE?.DataContext
        If oChannel Is Nothing Then Return

        Dim oLogin As ShareLogin = uiEditLogin.DataContext
        oLogin.channels.Remove(oChannel)

        uiListaKanalow.ItemsSource = Nothing
        uiListaKanalow.ItemsSource = oLogin.channels
    End Sub

    Private Async Sub uiOK_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oLogin As ShareLogin = oFE?.DataContext
        If oLogin Is Nothing Then Return

        ' default name
        If String.IsNullOrWhiteSpace(oLogin.displayName) Then
            oLogin.displayName = "login " & Date.Now.ToString("yyyy.MM.dd")
            oLogin.login = Guid.NewGuid
        End If

        Dim sName As String = Await Vblib.DialogBoxInputAllDirectAsync("Podaj nazwę loginu", oLogin.displayName)
        If String.IsNullOrWhiteSpace(sName) Then Return

        For Each oLog As ShareLogin In Application.GetShareLogins.GetList
            If oLog.displayName = sName Then
                If Not Await Vblib.DialogBoxYNAsync($"Login '{sName}' już istnieje, zastąpić?") Then Return
                Application.GetShareLogins.Remove(oLog)
                Exit For
            End If
        Next

        ' tu mamy Clone oryginału, którego nie zmieniamy

        With Application.GetShareLogins
            .GetList.Add(oLogin)
            .ReResolveChannels()
            .Save()
        End With

        uiEditLogin.Visibility = Visibility.Collapsed

        Page_Loaded(Nothing, Nothing)
    End Sub

    Private Sub uiEnabled_Checked(sender As Object, e As RoutedEventArgs)
        ' check oraz uncheck
        Application.GetShareLogins.Save()
    End Sub
End Class

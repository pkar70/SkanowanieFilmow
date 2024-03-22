Imports pkar
Imports Vblib
Imports pkar.UI.Configs

Class SettingsShareLogins

    Dim _channels As List(Of String)

    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        uiLista.ItemsSource = Application.GetShareLogins '.OrderBy(Function(x) x.displayName)
        uiAdresOverride.GetSettingsString
        Dim adres As String = Await vb14_GetMyIP.GetMyIP.GetIPString
        _channels = Application.GetShareChannels.Select(Of String)(Function(x) x.nazwa).ToList
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

        Dim adres As String = Vblib.GetSettingsString("uiAdresOverride") ' DDNS, jak u mnie - nie adres fizyczny ale symboliczny, spisek...
        If String.IsNullOrWhiteSpace(adres) Then adres = Await vb14_GetMyIP.GetMyIP.GetIPString

        ' wysłanie email
        Dim subject As String = "Dane logowania do mojego PicSort"
        Dim body As String = $"PicSort://{adres}/{oLogin.login}"
        Dim email As New AsNuget_UseMapi.SendFileTo.MAPI
        email.SendMailPopup(subject, body)

        'If Await Vblib.DialogBoxYNAsync("Poczekać na połączenie (i zabezpieczyć je)? 'Nie' oznacza mniejsze bezpieczeństwo") Then
        '    ' czekamy
        'Else
        ' bez zabezpieczeń
        oLogin.allowedLogin.IPaddr = ""
        oLogin.allowedLogin.netmask = ""
        oLogin.allowedLogin.remoteHostName = ""
        'End If

        Application.GetShareLogins.Save(True) ' zmienione uprawnienia
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

        If String.IsNullOrWhiteSpace(oLogin.allowedLogin.remoteHostName) Then
            uiRemHostName.Visibility = Visibility.Visible
            uiUseRemHostName.Visibility = Visibility.Visible
        Else
            uiRemHostName.Visibility = Visibility.Collapsed
            uiUseRemHostName.Visibility = Visibility.Collapsed
        End If
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

        If oLogin.channels Is Nothing Then oLogin.channels = New List(Of ShareChannelProcess)
        Dim oNew As New ShareChannelProcess
        oNew.channel = Nothing
        oLogin.channels.Add(oNew)

        uiListaKanalow.ItemsSource = Nothing
        uiListaKanalow.ItemsSource = oLogin.channels

    End Sub

    Private Sub uiComboQuery_Loaded(sender As Object, e As RoutedEventArgs)
        Dim oCB As ComboBox = sender
        If oCB Is Nothing Then Return

        oCB.ItemsSource = _channels
    End Sub

    'Private Sub uiDelChannel_Click(sender As Object, e As RoutedEventArgs)
    '    Dim oFE As FrameworkElement = sender
    '    Dim oLogin As ShareChannel = oFE?.DataContext
    '    If oLogin Is Nothing Then Return

    '    Dim oLogin As ShareLogin = uiEditLogin.DataContext
    '    oLogin.channels.Remove(oLogin)

    '    uiListaKanalow.ItemsSource = Nothing
    '    uiListaKanalow.ItemsSource = oLogin.channels
    'End Sub


    Private Sub uiDelChannel_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oQuery As ShareChannelProcess = oFE?.DataContext
        If oQuery Is Nothing Then Return

        Dim oLogin As ShareLogin = uiEditLogin.DataContext
        oLogin.channels.Remove(oQuery)

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

        For Each oLog As ShareLogin In Application.GetShareLogins
            If oLog.displayName = sName Then
                If Not Await Vblib.DialogBoxYNAsync($"Login '{sName}' już istnieje, zastąpić?") Then Return
                Application.GetShareLogins.Remove(oLog)
                Exit For
            End If
        Next

        oLogin.displayName = sName

        ' tu mamy Clone oryginału, którego nie zmieniamy
        uiLista.ItemsSource = Nothing   ' żeby nie pokazywał w kółko tego samego
        With Application.GetShareLogins
            .Add(oLogin)
            .ReResolveChannels()
            .Save(True)
        End With

        uiEditLogin.Visibility = Visibility.Collapsed

        Page_Loaded(Nothing, Nothing)
    End Sub

    Private Sub uiEnabled_Checked(sender As Object, e As RoutedEventArgs)
        ' check oraz uncheck
        Application.GetShareLogins.Save(True)
    End Sub

    Private Sub uiUseRemHostName_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oLogin As ShareLogin = oFE?.DataContext
        If oLogin Is Nothing Then Return

        oLogin.allowedLogin.remoteHostName = oLogin.lastLogin.remoteHostName.ToUpperInvariant
        uiRemoteHostName.Text = oLogin.allowedLogin.remoteHostName
    End Sub

    Private Sub uiAdresOverride_TextChanged(sender As Object, e As TextChangedEventArgs)
        uiAdresOverrideSet.Visibility = Visibility.Visible
    End Sub

    Private Sub uiAdresOverrideSet_Click(sender As Object, e As RoutedEventArgs)
        uiAdresOverride.SetSettingsString
        uiAdresOverrideSet.Visibility = Visibility.Collapsed
    End Sub
End Class

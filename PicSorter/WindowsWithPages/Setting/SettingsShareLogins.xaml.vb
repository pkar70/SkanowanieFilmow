Imports pkar
Imports Vblib
Imports pkar.UI.Configs
Imports pkar.UI.Extensions
Imports Microsoft.VisualBasic.Logging

Class SettingsShareLogins

    Dim _channels As List(Of String)

    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        Me.InitDialogs
        uiLista.ItemsSource = vblib.GetShareLogins.OrderBy(Function(x) x.displayName)
        uiAdresOverride.GetSettingsString
        Dim adres As String = Await vb14_GetMyIP.GetMyIP.GetIPString
        _channels = vblib.GetShareChannels.Select(Of String)(Function(x) x.nazwa).ToList
        _channels.Sort()

    End Sub

#Region "lista loginów"

    Private Sub uiEdit_Click(sender As Object, e As RoutedEventArgs)
        Dim oLogin As ShareLogin = TryCast(sender, FrameworkElement)?.DataContext
        If oLogin Is Nothing Then Return
        uiPeerID.IsEnabled = False
        ShowToEdit(oLogin)
    End Sub
    Private Sub uiDump_Click(sender As Object, e As RoutedEventArgs)
        Dim oLogin As ShareLogin = TryCast(sender, FrameworkElement)?.DataContext
        If oLogin Is Nothing Then Return

        Dim sTxt As String = "Login: " & oLogin.displayName

        If Not String.IsNullOrWhiteSpace(oLogin.processing) Then
            sTxt &= vbCrLf & $" (⚙: {oLogin.processing})"
        End If

        If oLogin?.channels IsNot Nothing Then

            For Each kanal As Vblib.ShareChannelProcess In oLogin.channels
                sTxt = sTxt & vbCrLf & $"+ Channel: {kanal.channelName}"
                If Not String.IsNullOrWhiteSpace(kanal.processing) Then
                    sTxt &= $"  (⚙: {kanal.processing})"
                End If

                If kanal?.channel?.queries IsNot Nothing Then
                    For Each kwerenda As Vblib.ShareQueryProcess In kanal.channel.queries
                        sTxt = sTxt & vbCrLf & $"  + Query:" ' {kwerenda.queryName}" - nie trzeba nazwy, bo nazwa będzie w AsDymek
                        If Not String.IsNullOrWhiteSpace(kwerenda.processing) Then
                            sTxt &= $" (⚙: {kwerenda.processing})"
                        End If

                        sTxt = sTxt & kwerenda.query.AsDymek ' zaczynamy od zmiany linii

                    Next
                End If

            Next

        End If

        sTxt.SendToClipboard
        Me.MsgBox(sTxt)
    End Sub

    Public Shared Async Function GetCurrentMeAsWeb() As Task(Of String)
        Dim adres As String = Vblib.GetSettingsString("uiAdresOverride") ' DDNS, jak u mnie - nie adres fizyczny ale symboliczny, spisek...
        If String.IsNullOrWhiteSpace(adres) Then adres = Await vb14_GetMyIP.GetMyIP.GetIPString
        Return adres
    End Function

    Private Async Sub uiEmail_Click(sender As Object, e As RoutedEventArgs)
        Dim oLogin As ShareLogin = TryCast(sender, FrameworkElement)?.DataContext
        If oLogin Is Nothing Then Return

        Dim adres As String = Await GetCurrentMeAsWeb()

        ' wysłanie email
        Dim subject As String = "Dane logowania do mojego PicSort"
        Dim body As String = "Pełny dostęp:" & vbCrLf & $"PicSort://{adres}/{oLogin.login}"
        body &= vbCrLf & "Dostęp via WWW:" & vbCrLf & $"http://{adres}:{APP_HTTP_PORT}/webbuf?guid={oLogin.login}"
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

        vblib.GetShareLogins.Save(True) ' zmienione uprawnienia
    End Sub

    Private Async Sub uiDel_Click(sender As Object, e As RoutedEventArgs)
        Dim oLogin As ShareLogin = TryCast(sender, FrameworkElement)?.DataContext
        If oLogin Is Nothing Then Return

        If Not Await Me.DialogBoxYNAsync($"Usunąć login {oLogin.displayName}?") Then Return

        vblib.GetShareLogins.Remove(oLogin)
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
        uiPeerID.IsEnabled = True
        oNew.displayName = "login " & Date.Now.ToString("yyyy.MM.dd")
        oNew.login = Guid.NewGuid
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
        uiListaKanalow.ItemsSource = oLogin.channels.OrderBy(Of String)(Function(x) x.channelName)

    End Sub

    Private Sub uiComboQuery_Loaded(sender As Object, e As RoutedEventArgs)
        Dim oCB As ComboBox = sender
        If oCB Is Nothing Then Return

        oCB.ItemsSource = _channels
    End Sub

    Private Sub uiDelChannel_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oQuery As ShareChannelProcess = oFE?.DataContext
        If oQuery Is Nothing Then Return

        Dim oLogin As ShareLogin = uiEditLogin.DataContext
        oLogin.channels.Remove(oQuery)

        uiListaKanalow.ItemsSource = Nothing
        uiListaKanalow.ItemsSource = oLogin.channels
    End Sub


    Private Sub uiOK_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oLogin As ShareLogin = oFE?.DataContext
        If oLogin Is Nothing Then Return

        If uiPeerID.IsEnabled Then
            ' a więc mamy do czynienia z NEW
            If String.IsNullOrWhiteSpace(oLogin.ID) Then
                Me.MsgBox("Musisz nadać identyfikator dla tego loginu")
                Return
            End If

            Dim peer As ShareLogin = vblib.GetShareLogins.FindByID(oLogin.ID)
            If peer IsNot Nothing Then
                Me.MsgBox($"Taki ID już istnieje (dla '{peer.displayName}')")
                Return
            End If
        Else
            ' usuwamy aktualny
            Dim peer As ShareLogin = vblib.GetShareLogins.FindByID(oLogin.ID)
            If peer IsNot Nothing Then vblib.GetShareLogins.Remove(peer)
        End If
        ' tylko przy new, bo EDIT i tak bezpośrednio na obiekcie jest
        vblib.GetShareLogins.Add(oLogin)

        ' tu mamy Clone oryginału, którego nie zmieniamy
        uiLista.ItemsSource = Nothing   ' żeby nie pokazywał w kółko tego samego
        With vblib.GetShareLogins
            .ReResolveChannels()
            .Save(True)
        End With

        uiEditLogin.Visibility = Visibility.Collapsed

        Page_Loaded(Nothing, Nothing)
    End Sub

    Private Sub uiEnabled_Checked(sender As Object, e As RoutedEventArgs)
        ' check oraz uncheck
        vblib.GetShareLogins.Save(True)
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

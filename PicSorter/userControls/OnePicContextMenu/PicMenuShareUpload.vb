


Imports Vblib
Imports pkar.DotNetExtensions


Public NotInheritable Class PicMenuShareUpload
    Inherits PicMenuBase

    Private _menuAllow As MenuItem
    Private _menuDeny As MenuItem


    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If _wasApplied Then Return

        MyBase.OnApplyTemplate()

        If Not InitEnableDisable("Share peers", "Dzielenie się zdjęciami", True) Then Return

        If vblib.GetShareLogins.Count > 0 Then
            _menuAllow = New MenuItem With {.Header = "Force allow"}
            AddHandler _menuAllow.SubmenuOpened, AddressOf OpeningForceAllowMenu
            WypelnMenuLogins(_menuAllow, AddressOf ActionSharingLogin)
            Me.Items.Add(_menuAllow)

            _menuDeny = New MenuItem With {.Header = "Force deny"}
            AddHandler _menuDeny.SubmenuOpened, AddressOf OpeningForceDenyMenu
            WypelnMenuLogins(_menuDeny, AddressOf ActionSharingLoginUnMark)
            Me.Items.Add(_menuDeny)

        End If

        If vblib.GetShareServers.Count > 0 Then
            Dim oNew As New MenuItem With {.Header = "Send to Server"}
            Me.Items.Add(oNew)
            WypelnMenuServers(oNew, AddressOf ActionSharingServer)
        End If
        _wasApplied = True
    End Sub

    Private Sub OpeningForceDenyMenu(sender As Object, e As RoutedEventArgs)
        For Each oMI As MenuItem In _menuDeny.Items
            Dim oLogin As Vblib.ShareLogin = TryCast(oMI.DataContext, Vblib.ShareLogin)
            If oLogin Is Nothing Then Continue For

            Dim oPic As Vblib.OnePic = GetFromDataContext()

            oMI.IsEnabled = True

            If oPic Is Nothing Then Continue For ' gdy otwieramy dla SelectedItems

            ' 1) wymuszony DENY: można go wyłączyć
            If oPic.PeerIsForcedDeny(oLogin) Then
                oMI.IsChecked = True
                Continue For
            End If

            ' 2) wymuszony ALLOW: można zablokować (jest ważniejszy niż ALLOW)
            If oPic.PeerIsForceAllowed(oLogin) Then
                oMI.IsChecked = False
                Continue For
            End If

            ' 3) podpada pod Login (bo Query): można zablokować
            If oPic.PeerIsForLogin(oLogin) Then
                oMI.IsChecked = False
                Continue For
            End If

            ' 4) nie podpada pod Login, więc nie ma sensu blokować
            oMI.IsChecked = False
            oMI.IsEnabled = False

        Next

    End Sub

    Private Sub OpeningForceAllowMenu(sender As Object, e As RoutedEventArgs)
        For Each oMI As MenuItem In _menuAllow.Items
            Dim oLogin As Vblib.ShareLogin = TryCast(oMI.DataContext, Vblib.ShareLogin)
            If oLogin Is Nothing Then Continue For

            Dim oPic As Vblib.OnePic = GetFromDataContext()

            oMI.IsEnabled = True

            If oPic Is Nothing Then Continue For ' gdy otwieramy dla SelectedItems

            ' 1) wymuszony DENY: z Allow nic nie można zrobić
            If oPic.PeerIsForcedDeny(oLogin) Then
                oMI.IsChecked = False
                oMI.IsEnabled = False
                Continue For
            End If

            ' 2) wymuszony ALLOW: można wyłączyć
            If oPic.PeerIsForceAllowed(oLogin) Then
                oMI.IsChecked = True
                Continue For
            End If

            ' 3) podpada pod Login (bo Query): nie ma sensu nic robić
            If oPic.PeerIsForLogin(oLogin) Then
                oMI.IsChecked = True
                oMI.IsEnabled = False
                Continue For
            End If

            ' 4) nie podpada pod Login, więc nie ma sensu blokować
            oMI.IsChecked = False

        Next
    End Sub

    Private Sub OpeningForceMenu(meni As MenuItem, inDenyMenu As Boolean)
        If UseSelectedItems Then Return

        For Each oMI As MenuItem In meni.Items
            Dim oLogin As Vblib.ShareLogin = TryCast(oMI.DataContext, Vblib.ShareLogin)
            If oLogin Is Nothing Then Continue For

            Dim oPic As Vblib.OnePic = GetFromDataContext()

            ' 1) wymuszony DENY, to nie można włączyć - można jedynie UNCHECK w MenuDeny
            If oPic.PeerIsForcedDeny(oLogin) Then
                If inDenyMenu Then
                    oMI.IsChecked = True
                    oMI.IsEnabled = True
                Else
                    oMI.IsChecked = False
                    oMI.IsEnabled = False
                End If

                Continue For
            End If

            ' 2) wymuszony ALLOW
            If oPic.PeerIsForceAllowed(oLogin) Then
                If inDenyMenu Then
                    oMI.IsChecked = False
                    oMI.IsEnabled = True
                Else
                    oMI.IsChecked = True
                    oMI.IsEnabled = True
                End If

                Continue For
            End If


            If oPic.PeerIsForLogin(oLogin) Then
                If inDenyMenu Then
                    oMI.IsChecked = False
                    oMI.IsEnabled = True
                Else
                    oMI.IsChecked = True
                    oMI.IsEnabled = True
                End If

                Continue For
            End If

            If inDenyMenu Then
                oMI.IsChecked = False
                oMI.IsEnabled = True
            Else
                oMI.IsChecked = False
                oMI.IsEnabled = True
            End If


        Next
    End Sub




#Region "submenu logins"

    Private _ShareLogin As Vblib.ShareLogin

    Private Sub WypelnMenuLogins(oMenuItem As MenuItem, oEventHandler As RoutedEventHandler)
        oMenuItem.Items.Clear()

        For Each oLogin As Vblib.ShareLogin In vblib.GetShareLogins
            If oLogin.displayName.EqualsCI("FORPICSEARCH") Then Continue For
            Dim oNew As New MenuItem
            oNew.Header = oLogin.displayName
            oNew.DataContext = oLogin
            oNew.IsCheckable = True

            AddHandler oNew.Click, oEventHandler

            oMenuItem.Items.Add(oNew)
        Next

    End Sub

    Private Async Sub ActionSharingLogin(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        _ShareLogin = TryCast(oFE?.DataContext, ShareLogin)
        If _ShareLogin Is Nothing Then Return

        Await OneOrManyAsync(AddressOf MarkOnePicForLogin)

    End Sub

    Private Async Sub ActionSharingLoginUnMark(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        _ShareLogin = TryCast(oFE?.DataContext, ShareLogin)
        If _ShareLogin Is Nothing Then Return

        Await OneOrManyAsync(AddressOf UnMarkOnePicForLogin)

    End Sub

    Private Async Function MarkOnePicForLogin(oPic As OnePic) As Task
        ' *TODO* dla tego loginu, w zależności od aktualnego stanu, dopuść sharing
        oPic.PeerForceAllow(_ShareLogin)
    End Function

    Private Async Function UnMarkOnePicForLogin(oPic As OnePic) As Task
        ' *TODO* dla tego loginu, w zależności od aktualnego stanu, zablokuj sharing
        oPic.PeerForceDeny(_ShareLogin)
    End Function

#End Region

#Region "submenu servers"

    Public Shared Sub WypelnMenuServers(oMenuItem As MenuItem, oEventHandler As RoutedEventHandler)
        oMenuItem.Items.Clear()

        For Each oLogin As Vblib.ShareServer In vblib.GetShareServers

            Dim oNew As New MenuItem
            oNew.Header = oLogin.displayName
            oNew.DataContext = oLogin

            AddHandler oNew.Click, oEventHandler

            oMenuItem.Items.Add(oNew)
        Next

    End Sub

    Private _allErrs As String = ""
    Private _ShareSrvr As Vblib.ShareServer

    Private Async Sub ActionSharingServer(sender As Object, e As RoutedEventArgs)

        Dim oFE As FrameworkElement = sender
        _ShareSrvr = oFE?.DataContext
        If _ShareSrvr Is Nothing Then Return

        Dim bOnlyMark As Boolean = Not Await Vblib.DialogBoxYNAsync("Wysłać od razu? (mogę tylko zaznaczyć)")

        Dim sRet As String = Await lib14_httpClnt.httpKlient.TryConnect(_ShareSrvr)
        If Not sRet.StartsWith("OK") Then
            If Await Vblib.DialogBoxYNAsync("Błąd podłączenia do serwera: " & sRet & vbCrLf & "Zaznaczyć na później? (NIE=cancel") Then
                Return
            End If
            bOnlyMark = True
        End If

        sRet = Await lib14_httpClnt.httpKlient.CanUpload(_ShareSrvr)
        If Not sRet.StartsWith("YES") Then
            Vblib.DialogBox("Upload jest niedostępny: " & sRet)
            Return
        End If

        If bOnlyMark Then
            Await OneOrManyAsync(AddressOf UploadOnePic)
        Else
            Await OneOrManyAsync(AddressOf MarkOnePic)
        End If


        If _allErrs <> "" Then
            Vblib.ClipPut(_allErrs)
            Vblib.DialogBox(_allErrs)
        End If

    End Sub


    Private Async Function MarkOnePic(oPic As OnePic) As Task
        oPic.AddCloudPublished("S:" & _ShareSrvr.login.ToString, "")
    End Function

    Public Async Function UploadOnePic(oPic As Vblib.OnePic) As Task

        If oPic.sharingLockSharing Then
            _allErrs &= oPic.sSuggestedFilename & " is excluded from sharing, ignoring" & vbCrLf
        Else

            oPic.ResetPipeline()
            Dim ret As String = Await oPic.RunPipeline(_ShareSrvr.uploadProcessing, Vblib.gPostProcesory, False)
            If ret <> "" Then
                ' jakiś błąd
                _allErrs &= ret & vbCrLf
            Else
                ' pipeline OK
                ret = Await lib14_httpClnt.httpKlient.UploadPic(_ShareSrvr, oPic)
                _allErrs &= ret & vbCrLf
            End If

        End If

        oPic.ResetPipeline() ' zwolnienie streamów, readerów, i tak dalej
    End Function
#End Region

End Class




Imports Vblib

Public NotInheritable Class PicMenuShareUpload
    Inherits PicMenuBase

    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If _wasApplied Then Return

        MyBase.OnApplyTemplate()

        If Not InitEnableDisable("Share peers", True) Then Return

        If Application.GetShareLogins.Count > 0 Then
            Dim oNew As New MenuItem With {.Header = "Mark for Login"}
            Me.Items.Add(oNew)
            WypelnMenuLogins(oNew, AddressOf ActionSharingLogin)

            oNew = New MenuItem With {.Header = "UnMark for Login"}
            Me.Items.Add(oNew)
            WypelnMenuLogins(oNew, AddressOf ActionSharingLoginUnMark)

        End If

        If Application.GetShareServers.Count > 0 Then
            Dim oNew As New MenuItem With {.Header = "Send to Server"}
            Me.Items.Add(oNew)
            WypelnMenuServers(oNew, AddressOf ActionSharingServer)
        End If
        _wasApplied = True
    End Sub

#Region "submenu logins"

    Private _ShareLogin As Vblib.ShareLogin

    Private Sub WypelnMenuLogins(oMenuItem As MenuItem, oEventHandler As RoutedEventHandler)
        oMenuItem.Items.Clear()

        For Each oLogin As Vblib.ShareLogin In Application.GetShareLogins

            Dim oNew As New MenuItem
            oNew.Header = oLogin.displayName
            oNew.DataContext = oLogin

            AddHandler oNew.Click, oEventHandler

            oMenuItem.Items.Add(oNew)
        Next

    End Sub

    Private Async Sub ActionSharingLogin(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        _ShareLogin = oFE?.DataContext
        If _ShareLogin Is Nothing Then Return

        Await OneOrManyAsync(AddressOf MarkOnePicForLogin)

    End Sub

    Private Async Sub ActionSharingLoginUnMark(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        _ShareLogin = oFE?.DataContext
        If _ShareLogin Is Nothing Then Return

        Await OneOrManyAsync(AddressOf UnMarkOnePicForLogin)

    End Sub

    Private Async Function MarkOnePicForLogin(oPic As OnePic) As Task
        oPic.AddCloudPublished("L:" & _ShareLogin.login.ToString, "")
    End Function

    Private Async Function UnMarkOnePicForLogin(oPic As OnePic) As Task
        oPic.RemoveCloudPublished("L:" & _ShareLogin.login.ToString)
    End Function

#End Region

#Region "submenu servers"

    Public Shared Sub WypelnMenuServers(oMenuItem As MenuItem, oEventHandler As RoutedEventHandler)
        oMenuItem.Items.Clear()

        For Each oLogin As Vblib.ShareServer In Application.GetShareServers

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
            Dim ret As String = Await oPic.RunPipeline(_ShareSrvr.uploadProcessing, Application.gPostProcesory)
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

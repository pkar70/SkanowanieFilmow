


Public NotInheritable Class PicMenuShareUpload
    Inherits PicMenuBase

    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If _wasApplied Then Return

        MyBase.OnApplyTemplate()

        If Not InitEnableDisable("Share upload", True) Then Return

        WypelnMenu(Me, AddressOf ActionSharingUpload)

        _wasApplied = True
    End Sub

    Public Shared Sub WypelnMenu(oMenuItem As MenuItem, oEventHandler As RoutedEventHandler)
        oMenuItem.Items.Clear()
        ' _UImenuOnClick = oEventHandler

        For Each oLogin As Vblib.ShareServer In Application.GetShareServers.GetList

            Dim oNew As New MenuItem
            oNew.Header = oLogin.displayName
            oNew.DataContext = oLogin

            AddHandler oNew.Click, oEventHandler

            oMenuItem.Items.Add(oNew)
        Next

        ' w odróżnieniu od innych - tu mamy wygaszanie
        oMenuItem.Visibility = If(oMenuItem.Items.Count > 0, Visibility.Visible, Visibility.Collapsed)

    End Sub

    Private _allErrs As String = ""
    Private _ShareSrvr As Vblib.ShareServer

    Private Async Sub ActionSharingUpload(sender As Object, e As RoutedEventArgs)

        Dim oFE As FrameworkElement = sender
        _ShareSrvr = oFE?.DataContext
        If _ShareSrvr Is Nothing Then Return

        Dim sRet As String = Await lib_sharingNetwork.httpKlient.TryConnect(_ShareSrvr)
        If Not sRet.StartsWith("OK") Then
            Vblib.DialogBox("Błąd podłączenia do serwera: " & sRet)
            Return
        End If

        sRet = Await lib_sharingNetwork.httpKlient.CanUpload(_ShareSrvr)
        If Not sRet.StartsWith("YES") Then
            Vblib.DialogBox("Upload jest niedostępny: " & sRet)
            Return
        End If

        Await OneOrManyAsync(AddressOf UploadOnePic)

        If _allErrs <> "" Then
            Vblib.ClipPut(_allErrs)
            Vblib.DialogBox(_allErrs)
        End If

    End Sub

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
                ret = Await lib_sharingNetwork.httpKlient.UploadPic(_ShareSrvr, oPic)
                _allErrs &= ret & vbCrLf
            End If

        End If

        oPic.ResetPipeline() ' zwolnienie streamów, readerów, i tak dalej
    End Function


End Class

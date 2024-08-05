'skopiowane z UserControlPostProcessPipeline, dlatego nazwy raczej tamtemu odpowiadają :)

Public Class UserControlAllowedPeers

    Public Property AllowDuplicates As Boolean = False

    ' https://stackoverflow.com/questions/18461660/wpf-user-control-bind-data-to-user-control-property

    Public Shared ReadOnly AllowedPeersProperty As DependencyProperty =
DependencyProperty.Register("AllowedPeers", GetType(String),
GetType(UserControlAllowedPeers), New FrameworkPropertyMetadata(String.Empty))

    Public Property AllowedPeers As String
        Get
            Return GetValue(AllowedPeersProperty)?.ToString()
        End Get
        Set
            SetValue(AllowedPeersProperty, Value)
        End Set
    End Property

    '    Public Property Pipeline As DependencyProperty

    Private Sub UserControl_Loaded(sender As Object, e As RoutedEventArgs)
        WypelnMenuPeers()
    End Sub

    Private Sub WypelnMenuPeers()
        uiMenuPostProcessors.Items.Clear()

        For Each oItem As Vblib.ShareLogin In vblib.GetShareLogins
            uiMenuPostProcessors.Items.Add(StworzMenuItemPeer(oItem))
        Next

    End Sub

    Private Function StworzMenuItemPeer(oEngine As Vblib.ShareLogin) As MenuItem
        Dim oNew As New MenuItem
        oNew.Header = oEngine.displayName
        oNew.Margin = New Thickness(2)
        oNew.DataContext = oEngine

        AddHandler oNew.Click, AddressOf DodajTenPeer

        Return oNew
    End Function

    Private Sub DodajTenPeer(sender As Object, e As RoutedEventArgs)
        Dim oMI As MenuItem = sender
        If oMI Is Nothing Then Return

        Dim peer As Vblib.ShareLogin = oMI.DataContext

        Dim sAllowedPeers As String = uiPostprocess.Text

        If Not AllowDuplicates Then
            If sAllowedPeers.Contains(peer.GetIdForSharing) Then Return
        End If

        sAllowedPeers &= peer.GetIdForSharing

        AllowedPeers = sAllowedPeers
    End Sub

    Private Sub uiAddPostproc_Click(sender As Object, e As RoutedEventArgs)
        uiAddPostprocPopup.IsOpen = Not uiAddPostprocPopup.IsOpen
    End Sub
End Class

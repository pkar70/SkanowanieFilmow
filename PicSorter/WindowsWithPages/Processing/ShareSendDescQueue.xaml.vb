Imports pkar.UI.Extensions
Imports pkar.DotNetExtensions
Imports Org.BouncyCastle.Crypto
Imports System.Windows.Automation

Public Class ShareSendDescQueue

    Private _lista As List(Of DisplayQueuePeer)

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        Me.ProgRingInit(True, False)
        RecalculateShowList()
    End Sub

    Private Sub RecalculateShowList()

        _lista = New List(Of DisplayQueuePeer)

        For Each oDesc As Vblib.ShareDescription In Application.GetShareDescriptionsOut

            Dim oItem As DisplayQueuePeer = _lista.Find(Function(x) oDesc.lastPeer.ContainsCI(x.peer.login.ToString))
            If oItem IsNot Nothing Then
                oItem.count += 1
            Else
                oItem = New DisplayQueuePeer
                oItem.count = 1

                oItem.peer = Application.GetShareServers.Find(Function(x) oDesc.lastPeer.ContainsCI(x.login.ToString))
                oItem.nazwa = oItem.peer?.displayName
                _lista.Add(oItem)
            End If
        Next

        uiLista.ItemsSource = _lista
    End Sub

    Private Async Sub uiGetThis_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oSrc As DisplayQueuePeer = oFE?.DataContext
        If oSrc Is Nothing Then Return

        Await ApplyOne(oSrc)

        RecalculateShowList()
    End Sub

    Private Async Sub uiGetAll_Click(sender As Object, e As RoutedEventArgs)

        Dim iSelected As Integer = _lista.Where(Function(x) x.enabled).Count

        uiProgBarEngines.Maximum = iSelected
        uiProgBarEngines.Value = 0
        uiProgBarEngines.Visibility = Visibility.Visible

        uiGetAll.IsEnabled = False

        For Each oSrc As DisplayQueuePeer In _lista
            uiProgBarEngines.Value += 1

            If Not oSrc.enabled Then Continue For

            Await ApplyOne(oSrc)
        Next

        uiProgBarEngines.Visibility = Visibility.Collapsed
        uiGetAll.IsEnabled = True

        RecalculateShowList()
    End Sub



    Private Async Function ApplyOne(oSrc As DisplayQueuePeer) As Task

        Me.ProgRingShow(True)

        Await lib14_httpClnt.httpKlient.UploadPicDescriptions(Application.GetShareDescriptionsOut, oSrc.peer)

        Me.ProgRingShow(False)

        Application.ShowWait(False)
    End Function

    Protected Class DisplayQueuePeer
        Public Property enabled As Boolean
        Public Property nazwa As String
        Public Property peer As Vblib.SharePeer
        Public Property count As Integer

    End Class

End Class


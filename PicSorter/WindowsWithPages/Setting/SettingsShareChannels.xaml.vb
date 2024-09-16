
Imports pkar.UI.Extensions

Class SettingsShareChannels

    Dim _lista As List(Of Vblib.ShareChannel)

    'Private _kwerendy As List(Of String) 'ObservableList(Of SearchQuery)

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        Me.InitDialogs

        _lista = Vblib.Globs.GetShareChannels.OrderBy(Function(x) x.nazwa).ToList
        uiLista.ItemsSource = _lista

        '_kwerendy = Vblib.GetQueries.OrderBy(Of String)(Function(x) x.nazwa).Select(Of String)(Function(x) x.nazwa).ToList
    End Sub

#Region "lista kanałów"

    Private Sub uiAddChannel_Click(sender As Object, e As RoutedEventArgs)
        Dim oNew As New Vblib.ShareChannel
        'oNew.nazwa = "channelProc " & Date.Now.ToString("yyyy.MM.dd")
        '_lista.Add(oNew)
        ShowToEdit(oNew)
    End Sub

    Private Sub ShowToEdit(oChannel As Vblib.ShareChannel)
        uiEditChannel.Visibility = Visibility.Visible
        uiEditChannel.DataContext = oChannel.Clone
    End Sub

    Private Sub uiEdit_Click(sender As Object, e As RoutedEventArgs)
        Dim oChannel As Vblib.ShareChannel = TryCast(sender, FrameworkElement)?.DataContext
        If oChannel Is Nothing Then Return
        ShowToEdit(oChannel)
    End Sub

    Private Async Sub uiDel_Click(sender As Object, e As RoutedEventArgs)
        Dim oChannel As Vblib.ShareChannel = TryCast(sender, FrameworkElement)?.DataContext
        If oChannel Is Nothing Then Return

        Dim sLogins As String = GetLoginyKorzystajace(oChannel)

        If sLogins <> "" Then
            Me.MsgBox("Nie można usunąć kanału, bo jest używany przez " & sLogins)
            Return
        End If


        If Not Await Me.DialogBoxYNAsync($"Usunąć channel {oChannel.nazwa}?") Then Return

        Vblib.Globs.GetShareChannels.Remove(oChannel)
        Page_Loaded(Nothing, Nothing)

    End Sub

    Private Sub uiFind_Click(sender As Object, e As RoutedEventArgs)
        Dim oChannel As Vblib.ShareChannel = TryCast(sender, FrameworkElement)?.DataContext
        If oChannel Is Nothing Then Return

        Dim sLogins As String = GetLoginyKorzystajace(oChannel)

        If sLogins = "" Then
            Me.MsgBox("Żaden login nie korzysta z tego kanału")
            Return
        End If

        Me.MsgBox("Loginy korzystające z tego kanału:" & vbCrLf & vbCrLf & sLogins & vbCrLf & vbCrLf & "(lista skopiowana do clipboard)")
        sLogins.SendToClipboard

    End Sub

    Private Function GetLoginyKorzystajace(oChannel As Vblib.ShareChannel) As String
        Dim sLogins As String = ""

        For Each oLogin As Vblib.ShareLogin In Vblib.Globs.GetShareLogins
            If oLogin.channels Is Nothing Then Continue For
            For Each channelProc As Vblib.ShareChannelProcess In oLogin.channels
                If channelProc.channelName = oChannel.nazwa Then
                    sLogins &= oLogin.displayName & vbCrLf
                    Exit For
                End If
            Next
        Next

        Return sLogins
    End Function
#End Region

#Region "edycja kanału"

    Private Async Sub uiOK_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oChannel As Vblib.ShareChannel = oFE?.DataContext
        If oChannel Is Nothing Then Return

        ' default name
        If String.IsNullOrWhiteSpace(oChannel.nazwa) Then oChannel.nazwa = "channel " & Date.Now.ToString("yyyy.MM.dd")

        Dim sName As String = Await Me.InputBoxAsync("Podaj nazwę kanału", oChannel.nazwa)
        If String.IsNullOrWhiteSpace(sName) Then Return

        For Each oChan As Vblib.ShareChannel In _lista
            If oChan.nazwa = sName Then
                If Not Await Me.DialogBoxYNAsync($"Kanał '{sName}' już istnieje, zastąpić?") Then Return

                _lista.Remove(oChan)
                Exit For
            End If
        Next
        oChannel.nazwa = sName
        ' tu mamy Clone oryginału, którego nie zmieniamy

        uiLista.ItemsSource = Nothing   ' żeby nie pokazywał w kółko tego samego

        With Vblib.Globs.GetShareChannels
            .Add(oChannel)
            .ReResolveQueries()
            .Save(True)
        End With

        uiEditChannel.Visibility = Visibility.Collapsed

        Page_Loaded(Nothing, Nothing)
    End Sub

    'Private Sub uiEditChannel_DataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs) Handles uiEditChannel.DataContextChanged
    '    ' konwersja z List(Of string) do string z vbcrlf?
    'End Sub

    Private Sub uiAddQuery_Click(sender As Object, e As RoutedEventArgs)

        ' wygeneruj menu
        WypelnMenuAddKwerenda()
        ' otwórz popup
        uiAddQueryPopup.IsOpen = Not uiAddQueryPopup.IsOpen
    End Sub

    Private Sub DodajToQuery(sender As Object, e As RoutedEventArgs)
        Dim oChannel As Vblib.ShareChannel = uiEditChannel.DataContext
        Dim oFE As FrameworkElement = sender
        Dim oQuery As Vblib.SearchQuery = oFE?.DataContext
        If oQuery Is Nothing Then Return

        If oChannel.queries Is Nothing Then oChannel.queries = New List(Of Vblib.ShareQueryProcess)

        Dim shQrProc As New Vblib.ShareQueryProcess
        shQrProc.query = oQuery
        shQrProc.queryName = oQuery.nazwa
        oChannel.queries.Add(shQrProc)

        uiListaKwerend.ItemsSource = Nothing
        uiListaKwerend.ItemsSource = oChannel.queries

    End Sub

    Private Sub WypelnMenuAddKwerenda()

        If uiMenuQueries.Items IsNot Nothing AndAlso uiMenuQueries.Items.Count > 0 Then Return

        ' uiMenuQueries.Items.Clear()
        For Each oQuery As Vblib.SearchQuery In Vblib.Globs.GetQueries.OrderBy(Of String)(Function(x) x.nazwa)
            Dim oNew As New MenuItem
            oNew.Header = oQuery.nazwa
            oNew.DataContext = oQuery
            oNew.ToolTip = oQuery.AsDymek

            AddHandler oNew.Click, AddressOf DodajToQuery

            uiMenuQueries.Items.Add(oNew)
        Next

    End Sub

    Private Sub uiDelQuery_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oQuery As Vblib.ShareQueryProcess = oFE?.DataContext
        If oQuery Is Nothing Then Return

        Dim oChannel As Vblib.ShareChannel = uiEditChannel.DataContext
        oChannel.queries.Remove(oQuery)

        uiListaKwerend.ItemsSource = Nothing
        uiListaKwerend.ItemsSource = oChannel.queries
    End Sub

    Private Sub uiComboQuery_Loaded(sender As Object, e As RoutedEventArgs)
        Dim oCB As ComboBox = sender
        If oCB Is Nothing Then Return

        oCB.ItemsSource = Vblib.Globs.GetQueries.OrderBy(Of String)(Function(x) x.nazwa).Select(Of String)(Function(x) x.nazwa).ToList

    End Sub
#End Region

End Class

Public Class KonwersjaStringListString
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.Convert

        Dim lista As List(Of String) = value
        If lista Is Nothing Then Return ""

        Dim ret As String = ""
        For Each line As String In lista
            If ret <> "" Then ret &= vbCrLf
            ret &= line
        Next

        Return ret
    End Function


    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.ConvertBack

        Dim multiline As String = value
        If multiline Is Nothing Then Return Nothing

        Dim ret As New List(Of String)
        For Each line As String In multiline.Split(vbLf)
            Dim tmp As String = line.Trim
            If String.IsNullOrEmpty(tmp) Then Continue For
            ret.Add(tmp)
        Next

        If ret.Count < 1 Then Return Nothing
        Return ret

    End Function
End Class

Imports System.Runtime.InteropServices.WindowsRuntime
Imports pkar
Imports Vblib

Class SettingsShareChannels

    Dim _lista As ObservableList(Of Vblib.ShareChannel)

    Private _kwerendy As List(Of String) 'ObservableList(Of SearchQuery)

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        _lista = Application.GetShareChannels.OrderBy(Function(x) x.nazwa)
        uiLista.ItemsSource = _lista

        _kwerendy = Application.GetQueries.OrderBy(Of String)(Function(x) x.nazwa).Select(Of String)(Function(x) x.nazwa).ToList
    End Sub

#Region "lista kanałów"

    Private Sub uiAddChannel_Click(sender As Object, e As RoutedEventArgs)
        Dim oNew As New ShareChannel
        'oNew.nazwa = "channelProc " & Date.Now.ToString("yyyy.MM.dd")
        '_lista.Add(oNew)
        ShowToEdit(oNew)
    End Sub

    Private Sub ShowToEdit(oChannel As ShareChannel)
        uiEditChannel.Visibility = Visibility.Visible
        uiEditChannel.DataContext = oChannel.Clone
    End Sub

    Private Sub uiEdit_Click(sender As Object, e As RoutedEventArgs)
        Dim oChannel As ShareChannel = TryCast(sender, FrameworkElement)?.DataContext
        If oChannel Is Nothing Then Return
        ShowToEdit(oChannel)
    End Sub

    Private Async Sub uiDel_Click(sender As Object, e As RoutedEventArgs)
        Dim oChannel As ShareChannel = TryCast(sender, FrameworkElement)?.DataContext
        If oChannel Is Nothing Then Return

        Dim sLogins As String = GetLoginyKorzystajace(oChannel)

        If sLogins <> "" Then
            Vblib.DialogBox("Nie można usunąć kanału, bo jest używany przez " & sLogins)
            Return
        End If


        If Not Await Vblib.DialogBoxYNAsync($"Usunąć channel {oChannel.nazwa}?") Then Return

        Application.GetShareChannels.Remove(oChannel)
        Page_Loaded(Nothing, Nothing)

    End Sub

    Private Sub uiFind_Click(sender As Object, e As RoutedEventArgs)
        Dim oChannel As ShareChannel = TryCast(sender, FrameworkElement)?.DataContext
        If oChannel Is Nothing Then Return

        Dim sLogins As String = GetLoginyKorzystajace(oChannel)

        If sLogins = "" Then
            Vblib.DialogBox("Żaden login nie korzysta z tego kanału")
            Return
        End If

        Vblib.DialogBox("Loginy korzystające z tego kanału:" & vbCrLf & vbCrLf & sLogins & vbCrLf & vbCrLf & "(lista skopiowana do clipboard)")
        Vblib.ClipPut(sLogins)

    End Sub

    Private Function GetLoginyKorzystajace(oChannel As ShareChannel) As String
        Dim sLogins As String = ""

        For Each oLogin As ShareLogin In Application.GetShareLogins.GetList
            If oLogin.channels Is Nothing Then Continue For
            For Each channelProc As ShareChannelProcess In oLogin.channels
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
        Dim oChannel As ShareChannel = oFE?.DataContext
        If oChannel Is Nothing Then Return

        ' default name
        If String.IsNullOrWhiteSpace(oChannel.nazwa) Then oChannel.nazwa = "channel " & Date.Now.ToString("yyyy.MM.dd")

        Dim sName As String = Await Vblib.DialogBoxInputAllDirectAsync("Podaj nazwę kanału", oChannel.nazwa)
        If String.IsNullOrWhiteSpace(sName) Then Return

        For Each oChan As ShareChannel In _lista
            If oChan.nazwa = sName Then
                If Not Await Vblib.DialogBoxYNAsync($"Kanał '{sName}' już istnieje, zastąpić?") Then Return

                _lista.Remove(oChan)
                Exit For
            End If
        Next
        oChannel.nazwa = sName
        ' tu mamy Clone oryginału, którego nie zmieniamy

        uiLista.ItemsSource = Nothing   ' żeby nie pokazywał w kółko tego samego

        With Application.GetShareChannels
            .GetList.Add(oChannel)
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
        Dim oFE As FrameworkElement = sender
        Dim oChannel As ShareChannel = oFE?.DataContext
        If oChannel Is Nothing Then Return

        Dim oNew As New ShareQueryProcess
        If oChannel.queries Is Nothing Then oChannel.queries = New List(Of ShareQueryProcess)
        oChannel.queries.Add(oNew)

        uiListaKwerend.ItemsSource = Nothing
        uiListaKwerend.ItemsSource = oChannel.queries

    End Sub

    Private Sub uiDelQuery_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oQuery As ShareQueryProcess = oFE?.DataContext
        If oQuery Is Nothing Then Return

        Dim oChannel As ShareChannel = uiEditChannel.DataContext
        oChannel.queries.Remove(oQuery)

        uiListaKwerend.ItemsSource = Nothing
        uiListaKwerend.ItemsSource = oChannel.queries
    End Sub

    Private Sub uiComboQuery_Loaded(sender As Object, e As RoutedEventArgs)
        Dim oCB As ComboBox = sender
        If oCB Is Nothing Then Return

        oCB.ItemsSource = _kwerendy

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

﻿

Imports Vblib

Class SettingsShareServers
    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        uiLista.ItemsSource = Vblib.GetShareServers.OrderBy(Function(x) x.displayName)
    End Sub

    Private Async Sub uiAddSrv_Click(sender As Object, e As RoutedEventArgs)
        Dim token As String = Await Vblib.DialogBoxInputAllDirectAsync("Podaj link serwera z email")
        If String.IsNullOrWhiteSpace(token) Then Return

        ' PicSort://89.78.232.50/a0371bf6-c16e-42e6-b87b-0ea0788e921f

        Dim oNew As ShareServer = ShareServer.CreateFromLink(token)
        If oNew.login = Guid.Empty Then
            Vblib.DialogBox(oNew.serverAddress)
            Return
        End If

        oNew.displayName = Await Vblib.DialogBoxInputAllDirectAsync("Pod jaką nazwą zapamiętać go?")
        If oNew.displayName = "" Then Return

        Vblib.GetShareServers.Add(oNew)
        Vblib.GetShareServers.Save(True)
        Page_Loaded(Nothing, Nothing)
    End Sub

    Private Async Sub uiTry_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oItem As ShareServer = oFE?.DataContext
        If oItem Is Nothing Then Return

        Dim sRet As String = Await lib14_httpClnt.httpKlient.TryConnect(oItem)

        Vblib.DialogBox(sRet)
    End Sub

    Private Sub uiEdit_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oItem As ShareServer = oFE?.DataContext
        If oItem Is Nothing Then Return

    End Sub

    Private Async Sub uiDel_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oItem As ShareServer = oFE?.DataContext
        If oItem Is Nothing Then Return

        If Not Await Vblib.DialogBoxYNAsync($"Na pewno usunąć serwer {oItem.displayName}?") Then Return

        Vblib.GetShareServers.Remove(oItem)

    End Sub

    Private Sub uiOK_Click(sender As Object, e As RoutedEventArgs)
        Vblib.GetShareServers.Save(True)
    End Sub
End Class

Public Class KonwersjaDatyCzasu
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.Convert
        Dim temp As DateTime = CType(value, DateTime)

        If temp.Year < 1000 Then Return "--"
        If temp.Year > 2100 Then Return "--"

        Return temp.ToString("yyyy-MM-dd")
    End Function


    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New NotImplementedException()
    End Function
End Class
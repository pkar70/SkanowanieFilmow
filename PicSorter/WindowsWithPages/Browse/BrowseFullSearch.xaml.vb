
Imports vb14 = Vblib.pkarlibmodule14

Public Class BrowseFullSearch

    Private _query As New Vblib.SearchQuery

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        uiKwerenda.DataContext = _query
    End Sub

    Private Sub uiSearchAdd_Click(sender As Object, e As RoutedEventArgs)
        AddRemove(False)
    End Sub

    Private Sub uiSearchRemove_Click(sender As Object, e As RoutedEventArgs)
        AddRemove(True)
    End Sub

    Private Async Sub AddRemove(usun As Boolean)

        _query = Await uiKwerenda.QueryValidityCheck
        If _query Is Nothing Then Return
        Dim parent As ProcessBrowse = Me.Owner

        Try
            parent.FilterSearchCallback(_query, usun)
            Return
        Catch ex As Exception

        End Try

        Await vb14.DialogBoxAsync("Zniknęło okno miniaturek, a więc okno kwerend nie ma sensu")
        Me.Close()
    End Sub


End Class

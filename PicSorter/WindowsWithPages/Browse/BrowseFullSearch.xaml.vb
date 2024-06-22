
Imports pkar.UI.Extensions

Public Class BrowseFullSearch

    Private _query As New Vblib.SearchQuery

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        uiKwerenda.DataContext = _query
    End Sub

    Private Sub uiSearchSet_Click(sender As Object, e As RoutedEventArgs)
        AddRemove(TypFilterCallbacka.Zaznacz)
    End Sub

    Private Sub uiSearchAdd_Click(sender As Object, e As RoutedEventArgs)
        AddRemove(TypFilterCallbacka.Doznacz)
    End Sub

    Private Sub uiSearchRemove_Click(sender As Object, e As RoutedEventArgs)
        AddRemove(TypFilterCallbacka.Odznacz)
    End Sub

    Private Async Sub AddRemove(typek As TypFilterCallbacka)

        _query = Await uiKwerenda.QueryValidityCheck
        If _query Is Nothing Then Return
        Dim parent As ProcessBrowse = Me.Owner

        Try
            parent.FilterSearchCallback(_query, typek)
            Return
        Catch ex As Exception

        End Try

        Await Me.MsgBoxAsync("Zniknęło okno miniaturek, a więc okno kwerend nie ma sensu")
        Me.Close()
    End Sub

    Public Enum TypFilterCallbacka
        Zaznacz
        Doznacz
        Odznacz
    End Enum

End Class



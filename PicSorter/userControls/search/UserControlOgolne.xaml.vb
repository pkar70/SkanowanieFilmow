
Imports Vblib

Public Class UserControlOgolne
    Private Sub uiCopyDateMinToMax(sender As Object, e As RoutedEventArgs)
        uiMaxDate.SelectedDate = uiMinDate.SelectedDate
        uiMaxDateCheck.IsChecked = uiMinDateCheck.IsChecked
    End Sub

    Private Sub uiMinDateCheck_Unchecked(sender As Object, e As RoutedEventArgs)
        uiMaxDate.SelectedDate = Nothing
    End Sub

    Private Sub uiMaxDateCheck_Unchecked(sender As Object, e As RoutedEventArgs)
        uiMinDate.SelectedDate = Nothing
    End Sub

    Private Sub UserControl_DataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs)

        Dim queryOgolne As Vblib.QueryOgolne = DataContext

        uiMinDate.SelectedDate = If(queryOgolne.MinDate.IsDateValid, queryOgolne.MinDate, Date.Now.AddDays(-1))
        uiMaxDate.SelectedDate = If(queryOgolne.MaxDate.IsDateValid, queryOgolne.MaxDate, Date.Now.AddDays(1))

    End Sub

    Private Sub uiTagsSelect_Click(sender As Object, e As RoutedEventArgs)
        Dim wnd As New BrowseKeywordsWindow(False)
        wnd.DataContext = Nothing
        wnd.ShowDialog()

        Dim lKeys As List(Of Vblib.OneKeyword) = wnd.GetListOfSelectedKeywords()
        Dim query As Vblib.QueryOgolne = DataContext
        For Each oKey As Vblib.OneKeyword In lKeys
            If Not query.Tags.Contains(oKey.sId) Then
                query.Tags &= " " & oKey.sId
            End If
        Next

    End Sub
End Class

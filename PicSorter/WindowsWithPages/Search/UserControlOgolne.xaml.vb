
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
End Class

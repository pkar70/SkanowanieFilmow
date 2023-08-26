Imports Vblib

Public Class UserControlTxtNum

    Public Property HasNumFld As Boolean = True

    Public Property DataDisplayName As String

    Private Sub UserControl_Loaded(sender As Object, e As RoutedEventArgs)

        Dim query As QueryTxtNum = DataContext
        If query Is Nothing Then Return

        uiNumFlds.Visibility = If(HasNumFld, Visibility.Visible, Visibility.Collapsed)
        uiCheckBox.Content = "Dołącz zdjęcia bez " & DataDisplayName
        uiCheckBox.ToolTip = "Zaznacz jeśli w wynikach wyszukiwania powinny się znaleźć także zdjęcia które nie mają " & DataDisplayName
    End Sub
End Class

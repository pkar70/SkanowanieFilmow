Public Class UserSortMode
    Inherits ComboBox

    Public Overrides Sub OnApplyTemplate()
        MyBase.OnApplyTemplate()

        Me.Items.Clear()
        AddSortMode("date", ThumbSortOrder.Data)
        AddSortMode("serno", ThumbSortOrder.Serno)
        AddSortMode("fname", ThumbSortOrder.Fname)

        Me.ToolTip = "sort thumbs by"
    End Sub

    Public Function GetRequestedSort() As ThumbSortOrder

        Dim oCBI As ComboBoxItem = TryCast(SelectedItem, ComboBoxItem)
        If oCBI Is Nothing Then Return ThumbSortOrder.Data

        Return oCBI.DataContext
    End Function

    Private Sub AddSortMode(tekst As String, mode As ThumbSortOrder)
        Dim defSort As Integer = Vblib.GetSettingsInt("uiThumbsSortMode") + 1

        Me.Items.Add(New ComboBoxItem() With {
                     .Content = tekst, .DataContext = mode,
                     .IsSelected = (defSort = mode)})
    End Sub

    Public Sub SetCurrentSortMode(mode As ThumbSortOrder)
        For Each tryb As ComboBoxItem In Me.Items
            tryb.IsSelected = (tryb.DataContext = mode)
        Next
    End Sub

End Class


Public Enum ThumbSortOrder
    Data
    Serno
    Fname
End Enum
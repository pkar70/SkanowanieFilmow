Imports Vblib
Imports pkar.DotNetExtensions


Public Class UserControlOgolneAdvanced
    Private Sub StackPanel_Loaded(sender As Object, e As RoutedEventArgs)
        uiComboSource.Items.Clear()

        uiComboSource.Items.Add("")

        For Each oSource In Application.GetSourcesList.GetList
            uiComboSource.Items.Add(oSource.SourceName)
        Next

    End Sub

    Private Sub uiComboSource_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        Dim query As QueryOgolneAdvanced = DataContext
        query.Source = TryCast(uiComboSource.SelectedValue, String)?.ToLowerInvariant
    End Sub

    Private Sub UserControl_DataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs)

        Dim query As QueryOgolneAdvanced = DataContext
        If String.IsNullOrWhiteSpace(query.Source) Then Return

        For Each oItem As ComboBoxItem In uiComboSource.Items
            If TryCast(oItem.Content, String)?.EqualsCI(query.Source) Then uiComboSource.SelectedItem = oItem
        Next

    End Sub
End Class

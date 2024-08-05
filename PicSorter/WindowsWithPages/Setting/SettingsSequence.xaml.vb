
Imports Vblib

Class SettingsSequence
    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)

        uiListaSteps.SetItems(Vblib.SequenceCheckers.
                              OrderBy(Of Integer)(Function(x) x.StageNo).
                              Select(Of String)(Function(x) x.Nazwa).ToArray)

        uiListaAutotags.SetItems(Vblib.gAutoTagery.Select(Of String)(Function(x) x.Nazwa).ToArray)
    End Sub

    Private Sub uiOk_Click(sender As Object, e As RoutedEventArgs)
        uiListaSteps.SetSettingsString()
        uiListaAutotags.SetSettingsString()
        Globs.StageReadRequir() ' ustawienie stage.IsRequired

        Me.NavigationService.GoBack()
    End Sub
End Class

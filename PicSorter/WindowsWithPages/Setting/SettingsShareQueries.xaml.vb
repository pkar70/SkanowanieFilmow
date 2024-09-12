

Class SettingsShareQueries
    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()


        If uiKwerenda Is Nothing Then
            Task.Delay(500)
        End If

        uiKwerenda.DataContext = New Vblib.SearchQuery
    End Sub
End Class

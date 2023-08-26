Imports Vblib

Public Class UserControlGeo

    Private Sub uiGetGeo_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New EnterGeoTag
        If Not oWnd.ShowDialog Then Return

        Dim dataCont As QueryGeo = DataContext
        If dataCont Is Nothing Then Return

        dataCont.Location = oWnd.GetGeoPos
        uiLatLon.Text = $"szer. {dataCont.Location.StringLat(3)}, dług. {dataCont.Location.StringLon(3)}"

        Dim radiusKm As Integer = If(oWnd.IsZgrubne, 20, 5)
        uiGeoRadius.Text = radiusKm

    End Sub

    Private Sub uiGeoRadius_TextChanged(sender As Object, e As TextChangedEventArgs)
        Dim dataCont As QueryGeo = DataContext
        If dataCont Is Nothing Then Return

        Dim radiusStr As String = uiGeoRadius.Text
        If String.IsNullOrWhiteSpace(radiusStr) Then radiusStr = "5"
        dataCont.Location.Radius = radiusStr * 1000
    End Sub
End Class

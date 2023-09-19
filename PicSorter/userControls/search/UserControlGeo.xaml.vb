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

    Private bInChange As Boolean = False

    Private Sub uiGeoRadius_TextChanged(sender As Object, e As TextChangedEventArgs)

        If bInChange Then Return
        bInChange = True

        Dim dataCont As QueryGeo = DataContext
        If dataCont Is Nothing Then Return

        Dim radiusStr As String = uiGeoRadius.Text
        If String.IsNullOrWhiteSpace(radiusStr) Then radiusStr = "5"
        dataCont.Location.Radius = radiusStr * 1000

        bInChange = False

    End Sub

    Private Sub UserControl_DataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs)
        ' przepisanie z DataContext do UI tego co nie jest bindowane

        If bInChange Then Return
        bInChange = True

        Dim dataCont As QueryGeo = DataContext
        If dataCont Is Nothing Then Return

        uiLatLon.Text = $"szer. {dataCont.Location.StringLat(3)}, dług. {dataCont.Location.StringLon(3)}"
        uiGeoRadius.Text = dataCont.Location.Radius \ 1000  ' integer tylko nas interesuje

        bInChange = False

    End Sub
End Class

Imports Vblib
Imports pkar
Imports pkar.UI.Extensions
Imports System.Globalization

Public Class UserControlGeo

    Private Sub uiGetGeo_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New EnterGeoTag
        If Not oWnd.ShowDialog Then Return

        Dim dataCont As QueryGeo = DataContext
        If dataCont Is Nothing Then Return

        Dim radiusKm As Double = If(oWnd.IsZgrubne, 20, 0.1)
        dataCont.Location = New BasicGeoposWithRadius(oWnd.GetGeoPos, radiusKm * 1000)
        uiLatLon.Text = $"szer. {dataCont.Location.StringLat(3)}, dług. {dataCont.Location.StringLon(3)}"

        uiGeoRadius.Text = radiusKm

    End Sub

    Private bInChange As Boolean = False

    Private Sub uiGeoRadius_TextChanged(sender As Object, e As TextChangedEventArgs)

        If bInChange Then Return
        bInChange = True

        Dim dataCont As QueryGeo = DataContext
        If dataCont Is Nothing Then Return

        If dataCont.Location IsNot Nothing Then
            Dim radiusStr As String = uiGeoRadius.Text
            If String.IsNullOrWhiteSpace(radiusStr) Then radiusStr = "0"
            dataCont.Location.Radius = radiusStr * 1000
        End If

        bInChange = False

    End Sub

    Private Sub UserControl_DataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs)
        ' przepisanie z DataContext do UI tego co nie jest bindowane

        If bInChange Then Return
        bInChange = True

        Dim dataCont As QueryGeo = DataContext
        If dataCont Is Nothing Then Return

        If dataCont.Location IsNot Nothing Then
            uiLatLon.Text = $"szer. {dataCont.Location.StringLat(3)}, dług. {dataCont.Location.StringLon(3)}"
            uiGeoRadius.Text = dataCont.Location.Radius \ 1000  ' integer tylko nas interesuje
        End If

        bInChange = False

    End Sub

    Public Overrides Sub OnApplyTemplate()
        MyBase.OnApplyTemplate()

        UserControl_DataContextChanged(Nothing, Nothing)
    End Sub
End Class

#If False Then
Public Class KonwersjaMetryKilometry
    Implements IValueConverter

    ' metry na km
    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
        Dim metry As Double = CType(value, Double)
        Return (metry \ 1000).ToString
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
        Dim tekstowo As String = CType(value, String)
        Dim km As Double = 1
        If Not Double.TryParse(tekstowo, km) Then Return 1
        Return km
    End Function
End Class
#End If
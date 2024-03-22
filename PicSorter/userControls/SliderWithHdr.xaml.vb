Imports pkar.UI.Configs
Imports pkar.DotNetExtensions

Public Class SliderWithHdr

    Public Property Minimum As Integer
    Public Property Maximum As Integer
    Public Property Value As Integer
    Public Property Header As String
    Public Property Sufix As String

    Public Sub SetSliderValue(newValue As Integer)
        uiSlajder.Value = Value
        uiSlajder_ValueChanged(Nothing, Nothing)
    End Sub

    Public Sub SetSettingsInt()
        Vblib.SetSettingsInt(Name, Value)
        Value = uiSlajder.Value
    End Sub

    Public Sub GetSettingsInt()
        Value = Vblib.GetSettingsInt(Name).Between(Minimum, Maximum)
        uiSlajder.Value = Value
        uiSlajder_ValueChanged(Nothing, Nothing)
    End Sub


    Private Sub Grid_Loaded(sender As Object, e As RoutedEventArgs)
        uiHeader.Text = Header
        uiSlajder.Minimum = Minimum
        uiSlajder.Maximum = Maximum
        uiSlajder.Value = Value.Between(Minimum, Maximum)
    End Sub

    Private Sub uiSlajder_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double))
        Value = uiSlajder.Value
        uiTxtValue.Text = Value & " " & Sufix
    End Sub
End Class

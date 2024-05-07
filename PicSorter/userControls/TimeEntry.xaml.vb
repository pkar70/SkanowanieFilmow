Imports pkar.DotNetExtensions

Public Class TimeEntry

    Private Property _UseSeconds As Boolean = True

    Public Property UseSeconds As Boolean
        Get
            Return _UseSeconds
        End Get
        Set(value As Boolean)
            _UseSeconds = value
            StackPanel_Loaded(Nothing, Nothing)
        End Set
    End Property

    Public Property Time As TimeSpan
        Get
            Return New TimeSpan(uiHour.Text, uiMin.Text, uiSec.Text)
        End Get
        Set(value As TimeSpan)
            uiHour.Text = value.Hours.Abs.ToString("00")
            uiMin.Text = value.Minutes.Abs.ToString("00")
            uiSec.Text = value.Seconds.abs.ToString("00")
        End Set
    End Property


    Private Sub uiHour_PreviewTextInput(sender As Object, e As TextCompositionEventArgs)
        If e.Text.Contains(":") Then
            e.Handled = True
            uiMin.Focus()
            uiMin.SelectAll()
            Return
        End If

        If Not ValidInt(e.Text, 23) Then e.Handled = True
    End Sub

    Private Function ValidInt(text As String, max As Integer) As Boolean
        Dim iTemp As Integer
        If Not Integer.TryParse(text, iTemp) Then Return False
        If iTemp > max Then Return False
        Return True
    End Function

    Private Sub uiMin_PreviewTextInput(sender As Object, e As TextCompositionEventArgs)
        If e.Text.Contains(":") Then
            e.Handled = True
            uiSec.Focus()
            uiSec.SelectAll()
            Return
        End If

        If Not ValidInt(e.Text, 59) Then e.Handled = True
    End Sub

    Private Sub uiSec_PreviewTextInput(sender As Object, e As TextCompositionEventArgs)
        If Not ValidInt(e.Text, 59) Then e.Handled = True
    End Sub

    Private Sub StackPanel_Loaded(sender As Object, e As RoutedEventArgs)
        Dim vis As Visibility = Visibility.Collapsed
        If _UseSeconds Then vis = Visibility.Visible
        uiSepMinSec.Visibility = vis
        uiSec.Visibility = vis
        uiHour.Focus()
    End Sub

End Class

Imports pkar.DotNetExtensions

Public Class UserDateRange

    Private _inChange As Boolean

    Public Property MinDate As Date
        Get
            Return uiDateMin.SelectedDate
        End Get
        Set(value As Date)
            uiDateMin.SelectedDate = value
        End Set
    End Property

    Public Property MaxDate As Date
        Get
            Return uiDateMax.SelectedDate
        End Get
        Set(value As Date)
            uiDateMax.SelectedDate = value
        End Set
    End Property

    Public Property RangeAsText As String
        Get
            Return uiDateRange.Text
        End Get
        Set(value As String)
            uiDateRange.Text = value
        End Set
    End Property

    Public Property UseMin As Boolean
        Get
            Return uiUseMin.IsChecked
        End Get
        Set(value As Boolean)
            uiUseMin.IsChecked = value
        End Set
    End Property

    Public Property UseMax As Boolean
        Get
            Return uiUseMax.IsChecked
        End Get
        Set(value As Boolean)
            uiUseMax.IsChecked = value
        End Set
    End Property

    Private Sub uiDateRange_TextChanged(sender As Object, e As TextChangedEventArgs)
        If _inChange Then Return
        _inChange = True

        Try

            Dim rok, mies, dzien As Integer
            Dim zakres As String = uiDateRange.Text

            If zakres.Length > 3 AndAlso Integer.TryParse(zakres.Substring(0, 4), rok) Then
                uiDateMin.SelectedDate = New Date(rok, 1, 1)
                uiDateMax.SelectedDate = New Date(rok, 12, 31, 23, 59, 59)

                If zakres.Length > 6 AndAlso Integer.TryParse(zakres.Substring(5, 2), mies) Then
                    uiDateMin.SelectedDate = New Date(rok, mies, 1)
                    If mies = 12 Then
                        uiDateMax.SelectedDate = New Date(rok + 1, 1, 1).AddSeconds(-2)
                    Else
                        uiDateMax.SelectedDate = New Date(rok, mies + 1, 1).AddSeconds(-2)
                    End If

                    If zakres.Length > 9 AndAlso Integer.TryParse(zakres.Substring(8, 2), dzien) Then
                        uiDateMin.SelectedDate = New Date(rok, mies, dzien)
                        uiDateMax.SelectedDate = New Date(rok, mies, dzien, 23, 59, 59)
                    End If

                End If

            ElseIf zakres.Length > 2 AndAlso Integer.TryParse(zakres.Substring(0, 3), rok) Then
                ' dekada
                uiDateMin.SelectedDate = New Date(rok * 10, 1, 1)
                uiDateMax.SelectedDate = New Date(rok * 10 + 9, 12, 31, 23, 59, 59)


            End If

        Catch ex As Exception

        End Try

        _inChange = False
    End Sub

    Private Sub uiDate_SelectedDateChanged(sender As Object, e As SelectionChangedEventArgs)
        If _inChange Then Return
        _inChange = True

        uiDateRange.Text = ""
        If uiDateMin.SelectedDate.HasValue AndAlso uiDateMax.SelectedDate.HasValue Then
            Dim dmin As Date = uiDateMin.SelectedDate.Value
            Dim dmax As Date = uiDateMax.SelectedDate.Value
            If dmin.Year = dmax.Year Then
                uiDateRange.Text = dmin.Year

                If dmin.Month = dmax.Month Then
                    uiDateRange.Text &= "." & dmin.Month.ToString("00")

                    If dmin.Day = dmax.Day Then
                        uiDateRange.Text &= "." & dmin.Day.ToString("00")
                    End If

                End If

            End If
        End If

        _inChange = False
    End Sub


    Private Sub UserControl_Loaded(sender As Object, e As RoutedEventArgs)
        uiDateMin.DisplayDateStart = New Date(1800, 1, 1)
        uiDateMax.DisplayDateEnd = Date.Now.AddHours(5)
        uiDateRange.Focus()
    End Sub
End Class

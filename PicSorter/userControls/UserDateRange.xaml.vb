﻿Public Class UserDateRange

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

        Dim rok, mies, dzien As Integer
        Dim zakres As String = uiDateRange.Text
        If zakres.Length > 3 AndAlso Integer.TryParse(zakres.Substring(0, 4), rok) Then
            uiDateMin.SelectedDate = New Date(rok, 1, 1)
            uiDateMax.SelectedDate = New Date(rok, 12, 31)

            If zakres.Length > 6 AndAlso Integer.TryParse(zakres.Substring(5, 2), mies) Then
                uiDateMin.SelectedDate = New Date(rok, mies, 1)
                uiDateMax.SelectedDate = New Date(rok, mies + 1, 1).AddDays(-1)

                If zakres.Length > 9 AndAlso Integer.TryParse(zakres.Substring(8, 2), dzien) Then
                    uiDateMin.SelectedDate = New Date(rok, mies, dzien)
                    uiDateMax.SelectedDate = New Date(rok, mies, dzien)
                End If

            End If

        End If


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
    End Sub
End Class
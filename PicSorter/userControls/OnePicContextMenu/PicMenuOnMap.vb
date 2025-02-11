﻿
Imports MediaDevices
Imports pkar
Imports pkar.UI.Extensions
Imports Vblib

Public NotInheritable Class PicMenuOnMap
    Inherits PicMenuBase

    Protected Overrides Property _minAktualne As SequenceStages = SequenceStages.Dates


    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If Not String.IsNullOrWhiteSpace(Me.Header) Then Return

        MyBase.OnApplyTemplate()

        If Not InitEnableDisable("On map", "Wywołanie mapy ze wskazaniem miejsca wykonania zdjęcia", True) Then Return

        If UseSelectedItems Then Return

        Me.Items.Clear()
        Dim secmaps As New MenuItem With {.Header = "other maps"}

        For Each oDictMapa As SettingsMapsy.JednaMapa In SettingsMapsy.GetListaMap.OrderBy(Of String)(Function(x) x.nazwa)
            Try
                Dim oNew As New MenuItem
                oNew.Header = oDictMapa.nazwa
                oNew.DataContext = oDictMapa.link
                AddHandler oNew.Click, AddressOf uiOnMap_Click

                If oDictMapa.isMain Then
                    Me.Items.Add(oNew)
                Else
                    secmaps.Items.Add(oNew)
                End If

            Catch ex As Exception

            End Try
        Next

        Me.Items.Add(secmaps)

        DodajMenuFlicker(Me)
    End Sub

    Public Overrides Sub MenuOtwieramy()
        MyBase.MenuOtwieramy()

        Me.IsEnabled = False
        If UseSelectedItems Then Return

        Me.IsEnabled = GetFromDataContext()?.sumOfGeo IsNot Nothing

    End Sub

    Private Sub uiOnMap_Click(sender As Object, e As RoutedEventArgs)
        Dim oGps As BasicGeoposWithRadius = GetFromDataContext.GetGeoTag
        If oGps Is Nothing Then Return

        Dim oFE As FrameworkElement = sender
        Dim mapsvc As String = oFE?.DataContext
        Dim zoom As Integer = 18
        Dim oMI As MenuItem = TryCast(sender, MenuItem)
        If oGps.Radius > 500 Then zoom = 16
        Dim sUri As Uri = oGps.ToUri(oMI.Header, zoom)

        'Dim sUri As New Uri("https://www.openstreetmap.org/#map=16/" & oGps.Latitude & "/" & oGps.Longitude)
        sUri.OpenBrowser
    End Sub

#Region "flicker"

    Private Sub DodajMenuFlicker(uiMenu As MenuItem)

        Dim oMenuFlicker As New MenuItem
        oMenuFlicker.Header = "Flickr"

        Dim oNew As New MenuItem
        oNew.Header = "phototime"
        AddHandler oNew.Click, AddressOf oFlickerFotoTime
        oMenuFlicker.Items.Add(oNew)

        oNew = New MenuItem
        oNew.Header = "same day"
        AddHandler oNew.Click, AddressOf oFlickerSameDay
        oMenuFlicker.Items.Add(oNew)

        oNew = New MenuItem
        oNew.Header = "same month"
        AddHandler oNew.Click, AddressOf oFlickerSameMonth
        oMenuFlicker.Items.Add(oNew)

        oNew = New MenuItem
        oNew.Header = "same year"
        AddHandler oNew.Click, AddressOf oFlickerSameYear
        oMenuFlicker.Items.Add(oNew)

        oNew = New MenuItem
        oNew.Header = "anytime"
        AddHandler oNew.Click, AddressOf oFlickerAnyTime
        oMenuFlicker.Items.Add(oNew)

        uiMenu.Items.Add(oMenuFlicker)
    End Sub

    Private Sub oFlickerFotoTime(sender As Object, e As RoutedEventArgs)

        Dim minDate As String = GetFromDataContext.GetMinDate.ToString("yyyy-MM-dd")
        Dim maxDate As String = GetFromDataContext.GetMaxDate.ToString("yyyy-MM-dd")
        UseFlickerLink(minDate, maxDate)
    End Sub

    Private Sub oFlickerSameDay(sender As Object, e As RoutedEventArgs)
        Dim minDate As String = GetFromDataContext.GetMostProbablyDate.ToString("yyyy-MM-dd")
        UseFlickerLink(minDate, minDate)
    End Sub

    Private Sub oFlickerSameMonth(sender As Object, e As RoutedEventArgs)
        Dim minDate As Date = GetFromDataContext.GetMostProbablyDate
        minDate = minDate.AddDays(-minDate.Day + 1)
        Dim maxDate As Date = minDate.AddMonths(1).AddDays(-1)
        UseFlickerLink(minDate.ToString("yyyy-MM-dd"), maxDate.ToString("yyyy-MM-dd"))
    End Sub

    Private Sub oFlickerSameYear(sender As Object, e As RoutedEventArgs)
        Dim minDate As Integer = GetFromDataContext.GetMostProbablyDate.Year
        UseFlickerLink(minDate.ToString & "-01-01", minDate.ToString & "-12-31")
    End Sub

    Private Sub oFlickerAnyTime(sender As Object, e As RoutedEventArgs)
        UseFlickerLink("1800-01-01", Date.Now.ToString("yyyy-MM-dd"))
    End Sub

    Private Sub UseFlickerLink(dateMin As String, dateMax As String)
        Dim oGps As BasicGeopos = GetFromDataContext.GetGeoTag
        If oGps Is Nothing Then Return

        Dim sUri As String = oGps.FormatLink("https://www.flickr.com/map?&fLat=%lat&fLon=%lon&zl=14&")
        sUri &= $"min_taken_date={dateMin}%252000%253A00%253A00&max_taken_date={dateMax}%252023%253A59%253A59"

        ' https://www.flickr.com/map?&fLat=50.0439&fLon=19.9484&zl=10&min_upload_date=2023-01-01%252000%253A00%253A00&max_upload_date=2023-06-16%252000%253A00%253A00
        Dim oUri As New Uri(sUri)
        oUri.OpenBrowser
    End Sub

#End Region

End Class

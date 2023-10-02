Imports System.Windows.Controls.Primitives

Public Class UserControlPolaNum0123
    Public Property FieldsList As String

    Private Sub uiFieldList_Click(sender As Object, e As RoutedEventArgs)
        ' kliknięcie na dowolnym buttonie - po nazwie do sprawdzenia który

        If DataContext Is Nothing Then Return

        Dim uiButt As Button = sender
        If uiButt Is Nothing Then Return

        ' szukamy czy już taki jest, jeśli tak, to tylko przełączamy jego widoczność
        Dim basename As String = uiButt.Name
        Dim oMenu As Menu = uiGrid.FindName(basename & "_Menu")
        If oMenu.Items.Count < 1 Then
            Dim listapol As List(Of String) = GetListaPol()
            If listapol Is Nothing Then Return

            For Each pole As String In listapol
                Dim oNew As New MenuItem With {.Header = pole, .DataContext = basename}
                AddHandler oNew.Click, AddressOf MenuItemClick
                oMenu.Items.Add(oNew)
            Next
        End If

        Dim oPop As Popup = uiGrid.FindName(basename & "_Popup")
        oPop.IsOpen = Not oPop.IsOpen

    End Sub

    Private Function GetListaPol() As List(Of String)
        Select Case FieldsList.ToLowerInvariant
            Case "azure"
                Return Nothing ' Azure nie ma danych numerycznych
            Case "viscros"
                Return New List(Of String) From {"tempmax", "tempmin", "feelslikemax", "feelslikemin", "precipprob", "precipcover", "sunriseEpoch", "sunsetEpoch", "moonphase", "temp", "feelslike", "humidity", "dew", "precip", "snow", "snowdepth", "windgust", "windspeed", "winddir", "pressure", "visibility", "cloudcover", "solarradiation", "solarenergy", "uvindex", "sunhour"}
            Case "opad"
                Return New List(Of String) From {"SumaDobowaOpadowMM", "WysokPokrywySnieznejCM", "WysokSwiezoSpadlegoSnieguCM"}
            Case "klimat"
                Return New List(Of String) From {"TempMax", "TempMin", "TempAvg", "TempMinGrunt", "SumaOpadowMM", "WysokPokrywySnieznejCM", "HigroAvg", "WindSpeedAvgMS", "ZachmurzenieOktantyAvg"}
            Case "synop"
                Return New List(Of String) From {"TempMax", "TempMin", "TempAvg", "TempMinGrunt", "SumaOpadowMM", "WysokPokrywySnieznejCM", "RownowaznikWodnySnieguMMCM", "HrsUslonecznienie", "HrsDeszcz", "HrsSnieg", "HrsDeszczZeSniegiem", "HrsGrad", "HrsMgla", "HrsZamglenie", "HrsSadz", "HrsGololedz", "HrsZamiecNiska", "HrsZamiecWysoka", "HrsZmetnienie", "HrsWiatrOd10MS", "HrsWiatrOd15MS", "HrsBurza", "HrsRosa", "HrsSzron", "PokrywaSniezna", "Blyskawica", "IzotermaDolnaCM", "IzotermaGornaCm", "AktynometriaJCM2", "ZachmurzenieOktantyAvg", "WindSpeedAvgMS", "CisnParyWodnejAvgHPa", "HigroAvg", "CisnStacjaAvgHPa", "CisnMorzeAvgHPa", "OpadDzienMM", "OpadNocMM"}
        End Select

        Return Nothing  ' nie znam listy pól
    End Function


    Private Sub MenuItemClick(sender As Object, e As RoutedEventArgs)
        ' do pola o nazwie DataContext wpisz Header

        Dim oMI As MenuItem = TryCast(sender, MenuItem)
        Dim basename As String = oMI?.DataContext
        If basename Is Nothing Then Return

        Dim oTBox As TextBox = uiGrid.FindName(basename & "_Name")
        If oTBox Is Nothing Then Return

        oTBox.Text = oMI.Header

        Dim oPop As Popup = uiGrid.FindName(basename & "_Popup")
        oPop.IsOpen = False

    End Sub

End Class

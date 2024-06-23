Imports Vblib
Imports pkar
Imports pkar.DotNetExtensions

Public Class EnterGeoTag
    Private Sub uiLatLon_TextChanged(sender As Object, e As TextChangedEventArgs)
        Dim oTB As TextBox = sender
        If TryFromLink(oTB.Text) Then Return
        CheckEnableOk()
    End Sub

    Private Sub CheckEnableOk()
        uiOK.IsEnabled = False
        If uiLatitude.Text.Length < 3 Then Return
        If uiLongitude.Text.Length < 3 Then Return
        uiOK.IsEnabled = True
    End Sub

    Private Function TryFromLink(sLink As String) As Boolean
        If Not sLink.StartsWithCI("http") Then Return False

        Dim oPos As BasicGeopos = BasicGeopos.GetFromLink(sLink)
        If oPos.IsEmpty Then Return False

        uiLatitude.Text = oPos.Latitude
        uiLongitude.Text = oPos.Longitude
        uiOK.IsEnabled = True
        Return True
    End Function


    Private Sub uiOk_Click(sender As Object, e As RoutedEventArgs)
        Me.DialogResult = True
    End Sub

    Public Function GetGeoPos() As BasicGeopos
        Return New BasicGeopos(uiLatitude.Text, uiLongitude.Text)
    End Function

    Public Function IsZgrubne() As Boolean
        Return uiZgrubne.IsChecked
    End Function

    Private Sub uiUsePOI_Click(sender As Object, e As RoutedEventArgs)
        Dim oFe As FrameworkElement = sender
        Dim oPOI As OSMnominatim = oFe?.DataContext

        If oPOI Is Nothing Then Return

        uiLatitude.Text = oPOI.lat
        uiLongitude.Text = oPOI.lon

        uiOK.IsDefault = True
        uiSearch.IsDefault = False

    End Sub

    Private Async Sub uiSearch_Click(sender As Object, e As RoutedEventArgs)
        Dim sQuery As String = uiPOIname.Text
        If sQuery.Length < 5 Then
            DialogBox("Tekst musi mieć przynajmniej 5 znaków!")
            Return
        End If

        Me.Height = 400
        Me.Width = 300
        Await POIfill(sQuery)
    End Sub

    'z przypomnijTu
    Private Async Function POIfill(sSearchQuery As String) As Task

        If Not OperatingSystem.IsWindows Then Return
        If Not OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240) Then Return


        ' https://operations.osmfoundation.org/policies/nominatim/ (1 search/sec, UserAgent na serio)
        ' zrob listę, do każdego jako DataContext dodaj Windows.Devices.Geolocation.BasicGeoposition ze wspolrzednymi

        ' System.Net.WebUtility.UrlEncode daje " " -> "+", a OSM wymaga "%20", czyli tak jak daje Uri.EscapeUriString 
        ' Dim sUrl As String = "https://nominatim.openstreetmap.org/search?format=jsonv2&q=" & System.Net.WebUtility.UrlEncode(sSearchQuery)
        Dim sUrl As String = "https://nominatim.openstreetmap.org/search?format=jsonv2&q=" & Uri.EscapeUriString(sSearchQuery)

        Dim moHttp As New Windows.Web.Http.HttpClient
        moHttp.DefaultRequestHeaders.UserAgent.TryParseAdd("PrzypomnijTu " & GetAppVers())
        moHttp.DefaultRequestHeaders.Accept.Add(New Windows.Web.Http.Headers.HttpMediaTypeWithQualityHeaderValue("application/json"))

        Dim sError = ""
        Dim oResp As Windows.Web.Http.HttpResponseMessage = Nothing

        Try
            oResp = Await moHttp.GetAsync(New Uri(sUrl))
        Catch ex As Exception
            sError = ex.Message
        End Try
        If sError <> "" Then
            sError = "error " & sError & ": chyba app nie ma uprawnień do Internet"
            DialogBox(sError)
            Return
        End If

        If Not oResp.IsSuccessStatusCode Then
            DialogBox("ERROR: cannot get answer from nominatim.openstreetmap.org")
            Return
        End If

        Dim sResp As String = ""
        Try
            sResp = Await oResp.Content.ReadAsStringAsync
        Catch ex As Exception
            sError = ex.Message
        End Try

        If sError <> "" Then
            sError = "error " & sError & " at ReadAsStringAsync"
            DialogBox(sError)
            Return
        End If

        Dim oList As List(Of OSMnominatim)
        oList = Newtonsoft.Json.JsonConvert.DeserializeObject(sResp, GetType(List(Of OSMnominatim)))
        uiLista.ItemsSource = oList

    End Function

    Public Class OSMnominatim
        Public Property lat As String
        Public Property lon As String
        Public Property display_name As String
    End Class

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        uiLatitude.Focus()
    End Sub

    Private Sub uiPOIname_TextChanged(sender As Object, e As TextChangedEventArgs) Handles uiPOIname.TextChanged
        If uiPOIname.Text.Length < 1 Then
            uiOK.IsDefault = True
            uiSearch.IsDefault = False
        Else
            uiOK.IsDefault = False
            uiSearch.IsDefault = True
        End If
    End Sub
End Class

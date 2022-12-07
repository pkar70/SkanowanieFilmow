Imports Vblib

Public Class EnterGeoTag
    Private Sub uiLatitude_TextChanged(sender As Object, e As TextChangedEventArgs)
        If Not uiLatitude.Text.StartsWith("http") Then Return

        Dim oPos As MyBasicGeoposition = SettingsMapsy.Link2Geo(uiLatitude.Text)
        If oPos.IsEmpty Then Return

        uiLatitude.Text = oPos.Latitude
        uiLongitude.Text = oPos.Longitude
    End Sub

    Private Sub uiOk_Click(sender As Object, e As RoutedEventArgs)
        Me.DialogResult = True
    End Sub

    Public Function GetGeoPos() As Vblib.MyBasicGeoposition
        Return New Vblib.MyBasicGeoposition(uiLatitude.Text, uiLongitude.Text)
    End Function

    Private Sub uiUsePOI_Click(sender As Object, e As RoutedEventArgs)
        Dim oFe As FrameworkElement = sender
        Dim oPOI As OSMnominatim = oFe?.DataContext

        If oPOI Is Nothing Then Return

        uiLatitude.Text = oPOI.lat
        uiLongitude.Text = oPOI.lon

    End Sub

    Private Sub uiSearch_Click(sender As Object, e As RoutedEventArgs)
        Dim sQuery As String = uiPOIname.Text
        If sQuery.Length < 5 Then
            DialogBox("Tekst musi mieć przynajmniej 5 znaków!")
            Return
        End If

        Me.Height = 400
        Me.Width = 300
        POIfill(sQuery)
    End Sub

    'z przypomnijTu
    Private Async Function POIfill(sSearchQuery As String) As Task
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

End Class

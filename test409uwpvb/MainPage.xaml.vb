' The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>
Public NotInheritable Class MainPage
    Inherits Page


    Private Const _defaultHttpAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36 Edg/126.0.0.0"

    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        uiMsg.Text = "starting..."

        Dim oHandler As New System.Net.Http.HttpClientHandler()
        oHandler.CookieContainer = New System.Net.CookieContainer()

        Dim _oHttp As New System.Net.Http.HttpClient(oHandler)
        _oHttp.DefaultRequestHeaders.UserAgent.TryParseAdd(_defaultHttpAgent)
        _oHttp.DefaultRequestHeaders.Accept.Add(New System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"))
        _oHttp.DefaultRequestHeaders.AcceptLanguage.Add(New System.Net.Http.Headers.StringWithQualityHeaderValue("en-US"))
        _oHttp.DefaultRequestHeaders.AcceptLanguage.Add(New System.Net.Http.Headers.StringWithQualityHeaderValue("en"))
        '//' _oHttp.DefaultRequestHeaders.AcceptEncoding.Add(New Net.Http.Headers.StringWithQualityHeaderValue("gzip"));// ' Accept - Encoding: gzip, deflate

        '//'_oHttp.DefaultRequestHeaders.AcceptEncoding.Add(New Net.Http.Headers.StringWithQualityHeaderValue("deflate"))
        '//'_oHttp.DefaultRequestHeaders.Connection.Add("Keep-alive")

        Dim oResp As System.Net.Http.HttpResponseMessage 'oResp;//= As Net.Http.HttpResponseMessage

        '            //' przygotuj pContent, będzie przy redirect używany ponownie
        oResp = Await _oHttp.GetAsync("http://www.skyscrapercity.com/login")

        If oResp.IsSuccessStatusCode Then
            uiMsg.Text = "Success!"
        Else
            uiMsg.Text = "Error code: " & oResp.StatusCode
        End If

        Dim iHttp As New Windows.Web.Http.HttpClient()
        Dim iRestp = Await iHttp.GetAsync(New Uri("http://www.skyscrapercity.com/login"))

        If iRestp.IsSuccessStatusCode Then
            uiMsg.Text &= "Second: OK"
        Else
            uiMsg.Text &= "second error" & oResp.StatusCode
        End If



    End Sub
End Class

Imports System.Reflection.Metadata

Class MainWindow

    Private Const _defaultHttpAgent As String = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36 Edg/126.0.0.0"

    Private Async Sub Window_Loaded(sender As Object, e As RoutedEventArgs)


        Dim oHandler As New Net.Http.HttpClientHandler With
                {
                .CookieContainer = New Net.CookieContainer
                }
        ' Dim _oHttp As New Net.Http.HttpClient()

        Dim _oHttp As New Net.Http.HttpClient(oHandler)
        _oHttp.DefaultRequestHeaders.UserAgent.TryParseAdd(_defaultHttpAgent)
        _oHttp.DefaultRequestHeaders.Accept.Add(New Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"))
        _oHttp.DefaultRequestHeaders.AcceptLanguage.Add(New Net.Http.Headers.StringWithQualityHeaderValue("en-US"))
        _oHttp.DefaultRequestHeaders.AcceptLanguage.Add(New Net.Http.Headers.StringWithQualityHeaderValue("en"))
        ' _oHttp.DefaultRequestHeaders.AcceptEncoding.Add(New Net.Http.Headers.StringWithQualityHeaderValue("gzip")) ' Accept-Encoding: gzip, deflate

        '_oHttp.DefaultRequestHeaders.AcceptEncoding.Add(New Net.Http.Headers.StringWithQualityHeaderValue("deflate"))
        '_oHttp.DefaultRequestHeaders.Connection.Add("Keep-alive")

        Dim oResp As Net.Http.HttpResponseMessage

        ' przygotuj pContent, będzie przy redirect używany ponownie
        oResp = Await _oHttp.GetAsync("https://www.skyscrapercity.com/login")

        If oResp.IsSuccessStatusCode Then
            MsgBox("Success!")
        Else
            MsgBox("Error code: " & oResp.StatusCode)
        End If

    End Sub
End Class

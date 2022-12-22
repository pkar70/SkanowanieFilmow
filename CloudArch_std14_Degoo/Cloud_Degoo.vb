
Imports System.Net
Imports System.Net.Http
Imports System.Text
Imports MetadataExtractor
Imports Microsoft.Rest.Azure
Imports Newtonsoft
Imports Vblib
Imports vb14 = Vblib.pkarlibmodule14

' https://github.com/bernd-wechner/Degoo
' https://github.com/levnikort/degoo-api-js


Public Class Cloud_Degoo
    Inherits Vblib.CloudArchive

    Public Const PROVIDERNAME As String = "Degoo"

    Public Overrides Property sProvider As String = PROVIDERNAME
    Private _DataDir As String

    Public Overrides Async Function SendFileMain(oPic As Vblib.OnePic) As Task(Of String)
        ' *TODO*

    End Function

    Public Overrides Function CreateNew(oConfig As Vblib.CloudConfig, oPostProcs As Vblib.PostProcBase(), sDataDir As String) As Vblib.AnyStorage
        If oConfig.sProvider <> sProvider Then Return Nothing

        Dim oNew As New Cloud_Degoo
        oNew.konfiguracja = oConfig
        oNew._PostProcs = oPostProcs
        oNew._DataDir = sDataDir
        Return oNew
    End Function

    Public Overrides Async Function GetMBfreeSpace() As Task(Of Integer)
        Return Integer.MaxValue ' no limits
    End Function

    Public Overrides Async Function SendFiles(oPicki As List(Of Vblib.OnePic), oNextPic As JedenWiecejPlik) As Task(Of String)
        ' *TODO* na razie i tak nie bêdzie wykorzystywane, podobnie jak w LocalStorage
        Throw New NotImplementedException()
    End Function

    Public Overrides Async Function GetFile(oPic As Vblib.OnePic) As Task(Of String)
        Dim sLink As String = Await GetShareLink(oPic)
        If sLink = "" Then Return "ERROR: nie mam zapisanego ID pliku"

        ' *TODO* na razie i tak nie bêdzie wykorzystywane, podobnie jak w LocalStorage
        ' *TODO* raczej bedzie konieczny LOGIN
        Throw New NotImplementedException()
    End Function


    Public Overrides Async Function GetRemoteTagsMain(oPic As Vblib.OnePic) As Task(Of String)
        Dim sLink As String = Await GetShareLink(oPic)
        If sLink = "" Then Return "ERROR: nie mam zapisanego ID pliku"

        ' If mInstaApi Is Nothing Then Return "ERROR: przed GetRemoteTags musi byæ LOGIN"

        ' *TODO* na razie i tak nie bêdzie wykorzystywane, podobnie jak w LocalStorage
        Throw New NotImplementedException()
    End Function

    Public Overrides Async Function Delete(oPic As Vblib.OnePic) As Task(Of String)
        Dim sLink As String = Await GetShareLink(oPic)
        If sLink = "" Then Return "ERROR: nie mam zapisanego ID pliku"

        ' If mInstaApi Is Nothing Then Return "ERROR: przed Delete musi byæ LOGIN"

        ' *TODO* na razie i tak nie bêdzie wykorzystywane, podobnie jak w LocalStorage
        Throw New NotImplementedException()
    End Function

    Public Overrides Async Function GetShareLink(oPic As Vblib.OnePic) As Task(Of String)
        Dim sId As String = oPic.GetCloudPublishedId(konfiguracja.nazwa)
        If sId = "" Then Return ""

        Return "https://www.instagram.com/p/" & sId & "/"
    End Function

    Public Overrides Async Function Login() As Task(Of String)
        Return Await EnsureLogin()
    End Function

#Disable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously

    Public Overrides Async Function VerifyFileExist(oPic As Vblib.OnePic) As Task(Of String)
        Dim sLink As String = Await GetShareLink(oPic)
        If sLink = "" Then Return "ERROR: nie mam zapisanego ID pliku"

        Dim sPage As String = Await Vblib.HttpPageAsync(sLink)
        If sPage.Contains("<title>Instagram</title>") Then Return "NO FILE"
        ' gdy jest, to <title>XXXXXX on Instagram: &quot;DESCRIPTION&quot;</title>
        Return ""

    End Function

    Public Overrides Async Function VerifyFile(oPic As Vblib.OnePic, oCopyFromArchive As Vblib.LocalStorage) As Task(Of String)
        Dim sRet As String = Await VerifyFileExist(oPic)
        If sRet <> "NO FILE" Then Return sRet

        ' If mInstaApi Is Nothing Then Return "ERROR: przed VerifyFile:Resend musi byæ LOGIN"

        ' *TODO* na razie i tak nie bêdzie wykorzystywane, podobnie jak w LocalStorage
        Throw New NotImplementedException()
    End Function

    Public Overrides Async Function GetShareLink(oOneDir As Vblib.OneDir) As Task(Of String)
        Return ""
    End Function

    Public Overrides Async Function Logout() As Task(Of String)
        Return ""
    End Function

#Enable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously

#Region "bezpoœredni dostêp do Degoo"

    Private Async Function EnsureLogin() As Task(Of String)
        ' jesteœmy zalogowani
        If Not String.IsNullOrWhiteSpace(_token) Then Return ""

        If String.IsNullOrWhiteSpace(konfiguracja.sUsername) Then Return "ERROR: use username for App ID"
        If String.IsNullOrWhiteSpace(konfiguracja.sPswd) Then Return "ERROR: use password for App Secret"

        ' Return Await Login_Bernd(konfiguracja.sUsername, konfiguracja.sPswd)
        Return Await Login_WebWrap(konfiguracja.sUsername, konfiguracja.sPswd)

    End Function


    Private _token As String

#If LEVNIKORT Then

    ' https://github.com/levnikort/degoo-api-js/blob/master/degoo.js


    Private Const _apiToken As String = "da2-vs6twz5vnjdavpqndtbzg3prra"
    Private Const _apiUrl As String = "https://production-appsync.degoo.com/graphql"
    Private Const _registerUrl As String = "https://rest-api.degoo.com/register"

    Private Async Function auth(email As String, password As String) As Task
        If Not String.IsNullOrWhiteSpace(_token) Then Return

        Dim sPostData As String = "{""CountryCode"": ""PL"", ""LanguageCode"": ""pl-PL"",""Source"": ""Web App"","
        sPostData &= $"Password: {konfiguracja.sPswd}, Username: {konfiguracja.sUsername}"
        sPostData &= "}"

        Dim result = Await axios.post(registerUrl, sPostData, 

, this.config.requestOptions);

            _token = result.data.Token
            this.config.rootPath = result.data.Redirect.replace('/my-files/', '');

    End Function
#Else
    ' https://github.com/bernd-wechner/Degoo/issues/31

    Private _oHttp As Net.Http.HttpClient
    Private Const _defaultHttpAgent As String = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/88.0.4321.0 Safari/537.36 Edg/88.0.702.0"
    Private Const _AgentDegoo As String = "Degoo-client/0.3"

    Private _reqToken As String

    Private Async Function GetPage(sUrl As String, Optional sPostData As String = "") As Task(Of String)
        If _oHttp Is Nothing Then

            Dim oHandler As New HttpClientHandler With
                {
                .CookieContainer = New CookieContainer
                }

            _oHttp = New Http.HttpClient(oHandler)
            _oHttp.DefaultRequestHeaders.UserAgent.TryParseAdd(_defaultHttpAgent)
            _oHttp.DefaultRequestHeaders.Accept.Add(New Http.Headers.MediaTypeWithQualityHeaderValue("*/*"))
            _oHttp.DefaultRequestHeaders.AcceptLanguage.Add(New Http.Headers.StringWithQualityHeaderValue("en-US"))
            _oHttp.DefaultRequestHeaders.AcceptLanguage.Add(New Http.Headers.StringWithQualityHeaderValue("en"))
            _oHttp.DefaultRequestHeaders.AcceptEncoding.Add(New Http.Headers.StringWithQualityHeaderValue("gzip")) ' Accept-Encoding: gzip, deflate
            _oHttp.DefaultRequestHeaders.AcceptEncoding.Add(New Http.Headers.StringWithQualityHeaderValue("deflate"))
            _oHttp.DefaultRequestHeaders.Connection.Add("Keep-alive")
        End If

        Dim oResp As Net.Http.HttpResponseMessage

        ' przygotuj pContent, bêdzie przy redirect u¿ywany ponownie
        Dim pContent As Net.Http.StringContent = Nothing    ' ¿eby nie krzycza³ ¿e u¿ywam nieinicjalizowanego
        If sPostData <> "" Then pContent = New Net.Http.StringContent(sPostData, Text.Encoding.UTF8, "application/x-www-form-urlencoded")

        Try
            ' ISSUE: reference to a compiler-generated method
            If sPostData <> "" Then
                oResp = Await _oHttp.PostAsync(sUrl, pContent)
            Else
                oResp = Await _oHttp.GetAsync(sUrl)
            End If

#Disable Warning CA1031 ' Do not catch general exception types
        Catch ex As Exception
#Enable Warning CA1031 ' Do not catch general exception types
            vb14.DumpMessage($"ERROR @HttpPageAsync get/post {sUrl}: {ex.Message}")
            pContent?.Dispose()
            Return ""
        End Try

        If Not oResp.IsSuccessStatusCode Then
            vb14.DumpMessage($"Error code: {oResp.StatusCode}. {oResp.ReasonPhrase}")
            pContent?.Dispose()
            Return ""
        End If

        pContent?.Dispose()

        Dim sPage As String

        Try
            sPage = Await oResp.Content.ReadAsStringAsync()
#Disable Warning CA1031 ' Do not catch general exception types
        Catch ex As Exception
#Enable Warning CA1031 ' Do not catch general exception types
            vb14.DumpMessage("ERROR @HttpPageAsync ReadAsync: " & ex.Message)
            Return ""
        End Try

        Return sPage


    End Function

    Private Async Function Login_WebWrap(email As String, pass As String) As Task(Of String)

        Dim sPage As String = Await GetPage("https://degoo.com/me/login") ' st¹d wraca natychmiast, z b³êdem "Too many requests"
        If sPage = "" Then Return "ERROR prelogin page"

        Dim oHtmlDoc As New HtmlAgilityPack.HtmlDocument
        oHtmlDoc.LoadHtml(sPage)

        ' <form action="/me/login" data-focus="false" id="form-login-eb11c767-f981-4189-8b17-9a241951358e" method="post">
        ' <input name="__RequestVerificationToken" type="hidden" value="2CIrb9jgC45rf1l3t-4c0DtQcFxgwZ5u8hG6UXJ1_JcNcARt6OKBQdjK2LPjkx8fVdBut_HRTRDBP7uruLv7PjqsKI-loG2wpuV-05y0R_U1">
        ' <input id="returnUrl" name="returnUrl" type="hidden" value=""><input id="source" name="source" type="hidden" value=""><input id="premiumID" name="premiumID" type="hidden" value="">
        ' <p><label for="Email">Email</label>
        ' <input class="" id="Email" maxlength="100" name="Email" placeholder="" required="required" tabindex="1" type="email" value=""></p>
        ' <p><label for="Password">Password</label><input class="" id="Password" maxlength="127" minlength="6" name="Password" placeholder="" required="required" tabindex="2" type="password"></p>
        ' <p><button id="submit-btn-eb11c767-f981-4189-8b17-9a241951358e" class="button-turquoise button-fill" type="submit" data-loading-text="Logging in...">Log in</button></p>
        ' <p class="hr color-main"><small>or</small></p><div class="push-center"><a id="oauth-link-eb11c767-f981-4189-8b17-9a241951358e" class="g-signin" href="/me/googlelogin?registerIfNotExists=False" tabindex="3"></a>
        ' </div><p class="push-center" style="margin-top: 3rem;"><a href="/">No account? Sign up!</a></p><p class="push-center"><a href="/forgotpassword" data-target="#forgot-password-modal" data-toggle="modal" data-remote="false">Forgot password?</a></p></form>


        Dim sToken As String = oHtmlDoc.DocumentNode.SelectSingleNode("//input[@name='__RequestVerificationToken']")?.GetAttributeValue("value", "")
        If sToken Is Nothing Then Return "ERROR prelogin page, reqtoken"

        Dim sPostData As String = $"Email={email.Replace("@", "%40")}&Password={pass}&__RequestVerificationToken={sToken}"
        sPage = Await vb14.HttpPageAsync("https://degoo.com/me/login", sPostData)



    End Function

    Private Async Function Login_Bernd(email As String, pass As String) As Task(Of String)


        vb14.HttpPageSetAgent(_AgentDegoo)
        Dim sPage As String = Await vb14.HttpPageAsync("https://degoo.com")

        Dim sPostData As String = $"Email={email.Replace("@", "%40")}&Password={pass}"

        sPage = Await vb14.HttpPageAsync("https://degoo.com/me/login", sPostData)


        Dim sLoginUrl As String = "https://rest-api.degoo.com/login"
        Dim sBody As String = $"""Username"":""{email}"",""Password"":""{pass}"",""GenerateToken"":true"
        sBody = "{" & sBody & "}"

        If _oHttp IsNot Nothing Then _oHttp.Dispose()
        _oHttp = New Net.Http.HttpClient()
        _oHttp.DefaultRequestHeaders.UserAgent.TryParseAdd(_AgentDegoo)
        _oHttp.DefaultRequestHeaders.Accept.Add(New Http.Headers.MediaTypeWithQualityHeaderValue("*/*"))
        _oHttp.DefaultRequestHeaders.AcceptLanguage.Add(New Http.Headers.StringWithQualityHeaderValue("en-US"))
        _oHttp.DefaultRequestHeaders.AcceptLanguage.Add(New Http.Headers.StringWithQualityHeaderValue("en"))
        _oHttp.DefaultRequestHeaders.AcceptEncoding.Add(New Http.Headers.StringWithQualityHeaderValue("gzip")) ' Accept-Encoding: gzip, deflate
        _oHttp.DefaultRequestHeaders.AcceptEncoding.Add(New Http.Headers.StringWithQualityHeaderValue("deflate"))
        _oHttp.DefaultRequestHeaders.Add("Origin", "https://app.degoo.com")
        _oHttp.DefaultRequestHeaders.Add("x-api-key", "da2-vs6twz5vnjdavpqndtbzg3prra")
        '      Content-Type: application/ Json
        '      Origin: https : //app.degoo.com
        'Content-Length:  61
        Dim pContent As New Net.Http.StringContent(sBody, Text.Encoding.UTF8, "application/json")

        Dim oResp As Net.Http.HttpResponseMessage = Await _oHttp.PostAsync(New Uri(sLoginUrl), pContent)

        sPage = Await oResp.Content.ReadAsStringAsync()


    End Function


#End If

#End Region
End Class

'authority: degoo.com
'method : POST
'path :  /me/login
'scheme : https
'accept: text/ html, application / xhtml + xml, application / xml;q=0.9, image / webp, image / apng,*/*;q=0.8, application / signed - exchange;v=b3;q=0.9
'accept-encoding: gzip, deflate, br
'accept-language: en-US, en;q=0.9, pl;q=0.8
'cache-control: max-age = 0
'content-length:  210
'content-type: application/ x - www - form - urlencoded
'cookie: AcceptCookies = YO4XUGTif2dfoSL7rE0_upfrFlDqzyAsQz3mcTigWU - ciCAUJO - GkxRhcxdw8h62axSpMCQ6ysaYFyShEamIWA; ulc=MxeNpxGoAmQZbyn6c6Q8AatkQ91Vfx1f5CdCTGYVoQGx17Qbv4cAxSSMzeZqhdt6a91VhtRjrhZICkZ_aViaVQ; GDPRConsentTime=lp38kheU7lXBGI2Z_hW29hUrfheBiQvr6OT9EVJ-0vC6x9pXMFXnEtcg7y96sbCZdEBjC0SBBA3IEOXSIOnDYg; lang=xlgxKS5T7xyqyp-y-BBZG7p1TrK8yWrCLKmrfsv8YaN67VmZbOYEv5LIs_OGj62ibc9U35vV1HeoO0m0hqeX7w; uniqueDeviceID=320E534F0E91e6704e17e36feed42bc5; _gid=GA1.2.366000379.1671133361; _ga=GA1.1.297181382.1669298600; _ga_668MSHBNLB=GS1.1.1671192772.13.1.1671193246.60.0.0; pid=; __RequestVerificationToken=mLCWp95Xw46xFpr7cOSSEWFCHiR6Wg9XYphjvL0s1Djag4mD6zkFJx2wym9BEbpoyqbgdqMqfJixPMfty9syX4RRL6xf2r-2v5PvNH3E19E1; invite=
'dnt: 1
'origin: https : //degoo.com
'referer: https : //degoo.com/
'sec-ch - ua:  "Not?A_Brand";v="8", "Chromium";v="108", "Microsoft Edge";v="108"
'sec-ch - ua - mobile: ? 0
'sec-ch - ua - platform:  "Windows"
'sec-fetch - dest: document
'sec-fetch - mode: navigate
'sec-fetch - site: same-origin
'sec-fetch - user: ? 1
'upgrade-insecure - requests:  1
'user-agent: Mozilla/ 5.0(Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, Like Gecko) Chrome/108.0.0.0 Safari/537.36 Edg/108.0.1462.4
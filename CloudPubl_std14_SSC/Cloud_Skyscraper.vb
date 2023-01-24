
Imports Vblib
Imports vb14 = Vblib.pkarlibmodule14


' additInfo - gdzie maj¹ trafiaæ, jesli /threads/ to jest to ucinane, jesli /unread to tez.
' skyscrapercity.com/threads/prl-i-reszta-œwiata.800434
' albo: skyscrapercity.com/threads/800434

Public Class Cloud_Skyscraper
    Inherits Vblib.CloudPublish

    Public Const PROVIDERNAME As String = "SkyScraperCity"
    Public Overrides Property sProvider As String = PROVIDERNAME
    Private _DataDir As String

    Public Overrides Async Function SendFileMain(oPic As Vblib.OnePic) As Task(Of String)
        Dim lista As New List(Of Vblib.OnePic)
        lista.Add(oPic)
        Return Await SendFileSSC(lista, Nothing)
    End Function


    Public Overrides Function VerifyFileExist(oPic As Vblib.OnePic) As Task(Of String)
        Throw New NotImplementedException()
    End Function

    Public Overrides Function VerifyFile(oPic As Vblib.OnePic, oCopyFromArchive As Vblib.LocalStorage) As Task(Of String)
        Throw New NotImplementedException()
    End Function

    Public Overrides Async Function SendFilesMain(oPicki As List(Of Vblib.OnePic), oNextPic As JedenWiecejPlik) As Task(Of String)
        Return Await SendFileSSC(oPicki, oNextPic)
    End Function

    Public Overrides Function GetFile(oPic As Vblib.OnePic) As Task(Of String)
        Throw New NotImplementedException()
    End Function

    Public Overrides Async Function Login() As Task(Of String)
        Return Await EnsureLogin()
    End Function

#Disable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
    Public Overrides Async Function GetMBfreeSpace() As Task(Of Integer)
        Return Integer.MaxValue
    End Function

    Protected Overrides Async Function GetRemoteTagsMain(oPic As Vblib.OnePic) As Task(Of String)
        Return "SkyScraperCity nie obs³uguje RemoteTags"
    End Function

    Public Overrides Async Function Delete(oPic As Vblib.OnePic) As Task(Of String)
        Return "St¹d siê nie da, otwórz post w WWW i wtedy spróbuj"
    End Function

    Public Overrides Async Function GetShareLink(oPic As Vblib.OnePic) As Task(Of String)
        Return "https://www.skyscrapercity.com/threads/" & oPic.GetCloudPublishedId(konfiguracja.nazwa)
    End Function

#Enable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
    Public Overrides Function GetShareLink(oOneDir As Vblib.OneDir) As Task(Of String)
        Throw New NotImplementedException()
    End Function

    Public Overrides Function Logout() As Task(Of String)
        Throw New NotImplementedException()
    End Function

    Public Overrides Function CreateNew(oConfig As Vblib.CloudConfig, oPostProcs() As Vblib.PostProcBase, sDataDir As String) As Vblib.AnyStorage
        If oConfig.sProvider <> sProvider Then Return Nothing

        Dim oNew As New Cloud_Skyscraper
        oNew.konfiguracja = oConfig
        oNew._PostProcs = oPostProcs
        oNew._DataDir = sDataDir
        Return oNew
    End Function

    Private _token As String

    Private Async Function EnsureLogin() As Task(Of String)
        ' jesteœmy zalogowani
        If _oHttp IsNot Nothing Then Return ""

        If String.IsNullOrWhiteSpace(konfiguracja.sUsername) Then Return "ERROR: username cannot be null"
        If String.IsNullOrWhiteSpace(konfiguracja.sPswd) Then Return "ERROR: password cannot be null"

        ' Return Await Login_Bernd(konfiguracja.sUsername, konfiguracja.sPswd)
        Return Await Login_Main(konfiguracja.sUsername, konfiguracja.sPswd)

    End Function

    Private Async Function Login_Main(sUsername As String, sPswd As String) As Task(Of String)

        Dim sUri As String = "https://www.skyscrapercity.com/login"
        Dim sPage As String = Await GetPageAsync(sUri)
        If sPage = "" Then Return "ERROR first page"

        If Not sPage.Contains("_xfRedirect") Then Return "ERROR - no _xfRedirect on first page"

        Dim oDoc As New HtmlAgilityPack.HtmlDocument
        oDoc.LoadHtml(sPage)

        Dim xfRedir As String = oDoc.DocumentNode.SelectSingleNode("//input[@name='_xfRedirect']")?.GetAttributeValue("value", "")
        ' i powinniœmy wzi¹æ drugie wyst¹pienie, ale chyba jest to samo
        Dim xfToken As String = oDoc.DocumentNode.SelectSingleNode("//input[@name='_xfToken']")?.GetAttributeValue("value", "")

        sUri = "https://www.skyscrapercity.com/login/login"
        Dim sPost As String = $"login={konfiguracja.sUsername}&password={konfiguracja.sPswd}&remember=1&_xfRedirect={xfRedir}&_xfToken={xfToken}"

        Await Task.Delay(500)
        sPage = Await GetPageAsync(sUri, sPost)
        If sPage = "" Then Return "ERROR second page"

        Return ""
        'sPage = Await GetPageAsync("https://www.skyscrapercity.com/watched/")

    End Function


    ' w³asne, nie z VbLib, bo ma byæ niezale¿ne
    Private _oHttp As Net.Http.HttpClient
    Private Const _defaultHttpAgent As String = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/88.0.4321.0 Safari/537.36 Edg/88.0.702.0"
    Private Async Function GetPageAsync(sUrl As String, Optional sPostData As String = "") As Task(Of String)
        If _oHttp Is Nothing Then

            Dim oHandler As New Net.Http.HttpClientHandler With
                {
                .CookieContainer = New Net.CookieContainer
                }

            _oHttp = New Net.Http.HttpClient(oHandler)
            _oHttp.DefaultRequestHeaders.UserAgent.TryParseAdd(_defaultHttpAgent)
            _oHttp.DefaultRequestHeaders.Accept.Add(New Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"))
            _oHttp.DefaultRequestHeaders.AcceptLanguage.Add(New Net.Http.Headers.StringWithQualityHeaderValue("en-US"))
            _oHttp.DefaultRequestHeaders.AcceptLanguage.Add(New Net.Http.Headers.StringWithQualityHeaderValue("en"))
            '_oHttp.DefaultRequestHeaders.AcceptEncoding.Add(New Net.Http.Headers.StringWithQualityHeaderValue("gzip")) ' Accept-Encoding: gzip, deflate
            '_oHttp.DefaultRequestHeaders.AcceptEncoding.Add(New Net.Http.Headers.StringWithQualityHeaderValue("deflate"))
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

    Private Function Link2ThreadId(sLink As String) As String
        ' https://www.skyscrapercity.com/threads/krak%C3%B3w-archiwalia.1925740
        ' https://www.skyscrapercity.com/forums/testing.88/

        Dim iInd As Integer = sLink.LastIndexOf(".")
        If iInd < 0 Then Return ""
        Dim sRet As String = sLink.Substring(iInd + 1)
        sRet = sRet.Replace("/", "")

        Return sRet
    End Function

    Private Async Function SendFileSSC(picki As List(Of Vblib.OnePic), oNextPic As JedenWiecejPlik) As Task(Of String)
        Dim sRet As String = Await EnsureLogin()
        If sRet <> "" Then Return sRet

        ' step 1: wyci¹gniêcie numeru forum
        Dim sForum As String = Link2ThreadId(konfiguracja.additInfo)
        If sForum = "" Then Return "ERROR cannot understand additInfo"

        ' step 2: œci¹gniêcie pierwszej strony (dla hash z formatki dodawania postu
        Dim sPage As String = Await GetPageAsync("https://www.skyscrapercity.com/threads/" & sForum)
        If sPage = "" Then Return "ERROR getting thread main page"
        Dim oDoc As New HtmlAgilityPack.HtmlDocument
        oDoc.LoadHtml(sPage)

        Dim sHash As String = oDoc.DocumentNode.SelectSingleNode("//input[@name='attachment_hash']").GetAttributeValue("value", "")
        Dim sHashCombined As String = oDoc.DocumentNode.SelectSingleNode("//input[@name='attachment_hash_combined']")?.GetAttributeValue("value", "")
        Dim sLastDate As String = oDoc.DocumentNode.SelectSingleNode("//input[@name='last_date']")?.GetAttributeValue("value", "")
        Dim xfToken As String = oDoc.DocumentNode.SelectSingleNode("//input[@name='_xfToken']")?.GetAttributeValue("value", "")
        Dim sParentId As String = oDoc.DocumentNode.SelectSingleNode("//input[@name='parent_id']")?.GetAttributeValue("value", "")


        Dim sPostBody As String = ""
        Dim oResp As Net.Http.HttpResponseMessage

        For Each oPic As Vblib.OnePic In picki

            ' step 3: wys³anie obrazka
            oPic._PipelineOutput.Seek(0, IO.SeekOrigin.Begin)
            Dim oPicContent As New Net.Http.StreamContent(oPic._PipelineOutput)
            ' ta linijka wymaga mapki mimetypów, Nuget - ale on wymaga .Net Std 2.0
            oPicContent.Headers.ContentType =
                New Net.Http.Headers.MediaTypeHeaderValue(
                MimeTypes.MimeTypeMap.GetMimeType(IO.Path.GetExtension(oPic.InBufferPathName)))

            Dim sUri As String = $"https://www.skyscrapercity.com/attachments/upload?type=post&context[thread_id]={sForum}&hash={sHash}"
            Dim pContent As New Net.Http.MultipartFormDataContent From {
                {New Net.Http.StringContent(xfToken), "_xfToken"},
                {New Net.Http.StringContent("json"), "_xfResponseType"},
                {New Net.Http.StringContent("1"), "_xfWithData"},
                {oPicContent, "upload", oPic.sSuggestedFilename}
            }

            'Content-Disposition: form-data; name=^\^"upload^\^"; filename=^\^"average-adult-weight-by-year.png^\^"^
            'Content-Type: image/png ^


            Try
                oResp = Await _oHttp.PostAsync(sUri, pContent)
#Disable Warning CA1031 ' Do not catch general exception types
            Catch ex As Exception
#Enable Warning CA1031 ' Do not catch general exception types
                pContent?.Dispose()
                Return "ERROR (catch) sending picture"
            End Try

            If Not oResp.IsSuccessStatusCode Then
                pContent?.Dispose()
                Return $"ERROR sending pic, code: {oResp.StatusCode}. {oResp.ReasonPhrase}"
            End If

            pContent?.Dispose()

            sPage = Await oResp.Content.ReadAsStringAsync()
            If sPage = "" Then Return "ERROR reading response after sending pic"
            ' przetworzenie JSONa, wyci¹gam z niego to co potrzebujê

            vb14.DumpMessage(sPage)

            Dim oJsonPicRet As PicUploadReturn = Newtonsoft.Json.JsonConvert.DeserializeObject(sPage, GetType(PicUploadReturn))
            If oJsonPicRet.status.ToLower <> "ok" Then
                Return "ERROR sending pic status not OK: " & oJsonPicRet.message
            End If


            sPostBody &= $"<p>{oPic.GetDescriptionForCloud}</p><p><img src='{oJsonPicRet.link}' style='width: auto;' class='fr-fic fr-dii' data-attachment='full:{oJsonPicRet.attachment.attachment_id}'/></p>"
            If oNextPic IsNot Nothing Then oNextPic()
        Next

        ' step 3: wys³anie postu

        sPostBody &= "<p>Post published by PicSorter app</p>"

        Dim pContentPost As New Net.Http.MultipartFormDataContent From {
            {New Net.Http.StringContent(sPostBody, Text.Encoding.UTF8), "message_html"},
            {New Net.Http.StringContent(sHash, Text.Encoding.UTF8), "attachment_hash"},
            {New Net.Http.StringContent(sHashCombined, Text.Encoding.UTF8), "attachment_hash_combined"},
            {New Net.Http.StringContent("", Text.Encoding.UTF8), "comment_type"},
            {New Net.Http.StringContent(sLastDate, Text.Encoding.UTF8), "last_date"},
            {New Net.Http.StringContent(sLastDate, Text.Encoding.UTF8), "last_known_date"},
            {New Net.Http.StringContent(sParentId, Text.Encoding.UTF8), "parent_id"},
            {New Net.Http.StringContent(sParentId, Text.Encoding.UTF8), "parent_ids"},
            {New Net.Http.StringContent("", Text.Encoding.UTF8), "guestReplyMethod"},
            {New Net.Http.StringContent(xfToken, Text.Encoding.UTF8), "_xfToken"},
            {New Net.Http.StringContent($"/threads/{sForum}", Text.Encoding.UTF8), "_xfRequestUri"},
            {New Net.Http.StringContent("1", Text.Encoding.UTF8), "_xfWithData"},
            {New Net.Http.StringContent(xfToken, Text.Encoding.UTF8), "_xfToken"},
            {New Net.Http.StringContent("json", Text.Encoding.UTF8), "_xfResponseType"}
        }

        ' tak, xfToken jest dwa razy.
        Dim sUriPost As String = $"https://www.skyscrapercity.com/threads/{sForum}/add-reply"
        Try
            oResp = Await _oHttp.PostAsync(sUriPost, pContentPost)
#Disable Warning CA1031 ' Do not catch general exception types
        Catch ex As Exception
#Enable Warning CA1031 ' Do not catch general exception types
            pContentPost?.Dispose()
            Return "ERROR (catch) sending post"
        End Try

        If Not oResp.IsSuccessStatusCode Then
            pContentPost?.Dispose()
            Return $"ERROR sending post, code: {oResp.StatusCode}. {oResp.ReasonPhrase}"
        End If

        pContentPost?.Dispose()

        sPage = Await oResp.Content.ReadAsStringAsync()
        If sPage = "" Then Return "ERROR reading response after sending post"
        ' przetworzenie JSONa, wyci¹gam z niego to co potrzebujê

        vb14.DumpMessage(sPage)

        Dim oJsonPostRet As PostUploadReturn = Newtonsoft.Json.JsonConvert.DeserializeObject(sPage, GetType(PostUploadReturn))
        If oJsonPostRet.status.ToLower <> "ok" Then
            Return "ERROR sending ppost status not OK: " & oJsonPostRet.status
        End If

        ' dodanie info o publikacji
        Dim sPostLink As String = oJsonPostRet.html.content.ToLowerInvariant
        '' *TODO* tylko w czasie uruchamiania, ¿eby mo¿na by³o sprawdziæ na ile to jest poprawny id...bo chyba jednak nie ten co trzeba
        'Dim sTemp As String = sPostLink
        'Dim iInd1 As Integer = sTemp.IndexOf("post-")
        'While iInd1 > 0
        '    Debug.WriteLine(sTemp.Substring(iInd1, 20))
        '    sTemp = sTemp.Substring(iInd1 + 5)
        '    iInd1 = sTemp.IndexOf("post-")
        'End While

        'Dim iInd As Integer = sPostLink.IndexOf("data-content")
        Dim iInd As Integer = sPostLink.LastIndexOf("anchorTarget")
        iInd = sPostLink.IndexOf("post-", iInd)
        sPostLink = sPostLink.Substring(iInd)
        iInd = sPostLink.IndexOf("""")
        sPostLink = sPostLink.Substring(0, iInd)
        sPostLink = $"{sForum}/{sPostLink}"




        ' do wszystkich - ale przecie¿ dopiero jak ca³y post wyszed³, a nie wczeœniej
        For Each oPic As Vblib.OnePic In picki
            oPic.AddCloudPublished(konfiguracja.nazwa, sPostLink)
        Next

        Return ""


    End Function



    Public Class PostUploadReturn
        Public Property status As String
        Public Property html As PostUploadReturnHtml
        'Public Property lastDate As Integer
        'Public Property visitor As Visitor
        'Public Property job As Job
    End Class

    Public Class PostUploadReturnHtml
        Public Property content As String
        'Public Property css() As String
        'Public Property js() As String
    End Class

    'Public Class Job
    '    Public Property manual As Object
    '    Public Property autoBlocking As Object
    '    Public Property autoBlockingMessage As Object
    '    Public Property _auto As Boolean
    'End Class
    Public Class PicUploadReturn
        Public Property status As String
        Public Property message As String
        Public Property redirect As String
        Public Property attachment As Attachment
        Public Property link As String
        'Public Property visitor As Visitor
    End Class

    Public Class Attachment
        Public Property attachment_id As Integer
        'Public Property filename As String
        'Public Property file_size As Integer
        'Public Property thumbnail_url As String
        'Public Property is_video As Boolean
        'Public Property video_url As Object
        'Public Property link As String
    End Class

    'Public Class Visitor
    '    Public Property conversations_unread As String
    '    Public Property alerts_unread As String
    '    Public Property total_unread As String
    'End Class

End Class

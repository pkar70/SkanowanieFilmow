Imports System.Threading
Imports pkar.UI.Configs.Extensions
Imports pkar.UI.Extensions
Imports pkar.DotNetExtensions
Imports Vblib

Partial Class SettingsPublishOptions
    Private Sub uiOK_Click(sender As Object, e As RoutedEventArgs)
        uiPublishUseAzure.SetSettingsBool
        uiPublishAddMaplink.SetSettingsBool
        uiPublishShowSerno.SetSettingsBool
        uiDefaultCopyr.SetSettingsString
        uiPublishUseDate.SetSettingsBool
        uiPublishUseLinks.SetSettingsBool

    End Sub

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        uiPublishUseAzure.GetSettingsBool
        uiPublishAddMaplink.GetSettingsBool
        uiPublishShowSerno.GetSettingsBool
        uiDefaultCopyr.GetSettingsString
        uiPublishUseDate.GetSettingsBool
        uiPublishUseLinks.GetSettingsBool
    End Sub

    Private Async Sub uiGetFacebookToken1_Click(sender As Object, e As RoutedEventArgs)
        ' https://developers.facebook.com/docs/facebook-login/for-devices#tech
        ' https://developers.facebook.com/apps/664684258557778/fb-login/settings/

        Dim sUri As String = "https://graph.facebook.com/v2.6/device/login"
        Dim sData As String = $"access_token={FB_APP_ID}|{FB_CLNT_TOKEN}
scope=public_profile,user_posts"

        sUri = $"https://graph.facebook.com/v2.6/device/login?access_token={FB_APP_ID}|{FB_CLNT_TOKEN}" ' &scope=public_profile"
        sData = ""

        ' gdy dam inny appid: Error validating application. Cannot get application info due to a system error.",
        ' niezależnie od scope: (#3) Application does not have the capability to make this API call.
        Dim sPage As String = Await Vblib.HttpPageAsync(sUri, sData, True)

        Dim respLoginCode As FB_GetLoginCodeResponse
        Try
            respLoginCode = Newtonsoft.Json.JsonConvert.DeserializeObject(sPage, GetType(FB_GetLoginCodeResponse))
        Catch ex As Exception
        End Try

        If String.IsNullOrWhiteSpace(respLoginCode.user_code) Then
            Me.MsgBox("unrecognized response from Facebook before code: " & vbCrLf & sPage)
            Return
        End If


        respLoginCode.user_code.SendToClipboard
        Me.MsgBox("Do Clipboard wpisałem kod który masz wpisać do przeglądarki")

        Dim oUri As New Uri("https://facebook.com/device")
        oUri.OpenBrowser

        sUri = "https://graph.facebook.com/v2.6/device/login_status"
        sData = $"access_token={FB_APP_ID}|{FB_CLNT_TOKEN}
       code={respLoginCode.code}"

        Dim respToken As New FB_GetTokenResponse

        For iLp = 0 To respLoginCode.expires_in / respLoginCode.interval
            Await Task.Delay(TimeSpan.FromSeconds(respLoginCode.interval))

            sPage = Await Vblib.HttpPageAsync(sUri, sData)

            Try
                respLoginCode = Newtonsoft.Json.JsonConvert.DeserializeObject(sPage, GetType(FB_GetTokenResponse))
            Catch ex As Exception
                'Me.MsgBox("unrecognized response from Facebook after code: " & vbCrLf & sPage)
                'Return
            End Try

            ' respToken = ...
            If respLoginCode.expires_in > 0 Then Exit For
        Next

        If respToken.expires_in < 1 Then
            Me.MsgBox("Nieudane logowanie - nie dałeś zgody?")
            uiFacebookClientToken.Text = ""
            uiFacebookClientTokenValidTo.Text = ""
            Return
        End If

        sUri = $"https://graph.facebook.com/v2.3/me?fields=name&access_token=respToken.access_token"
        sPage = Await Vblib.HttpPageAsync(sUri)
        Dim respMe As FB_GetLoginNameResponse =
            Newtonsoft.Json.JsonConvert.DeserializeObject(sPage, GetType(FB_GetLoginNameResponse))

        uiFacebookClientToken.Text = respToken.access_token
        uiFacebookClientTokenValidTo.Text = Date.Now.AddSeconds(respToken.expires_in).ToExifString

        Me.MsgBox($"Udało się! Według Facebook jesteś {respMe.name}")

    End Sub

    Private Async Sub uiGetFacebookToken_Click(sender As Object, e As RoutedEventArgs)
        ' https://developers.facebook.com/docs/facebook-login/guides/advanced/manual-flow

        'Dim sUri As String = "https://www.facebook.com/v20.0/dialog/oauth?"
        Dim sUri As String = "https://www.facebook.com/dialog/oauth?"
        sUri &= $"client_id={FB_APP_ID}"
        ' sUri &= $"&redirect_uri=http://www.facebook.com/connect/login_success.html"
        sUri &= $"&redirect_uri=https://{Await SettingsShareLogins.GetCurrentMeAsWeb}:{Globs.APP_HTTPS_PORT}/fromFB"
        sUri &= "&response_type=token"

        Dim state As String = Guid.NewGuid.ToString.Substring(0, 8)
        sUri &= "&state=" & state

        Dim oUri As New Uri(sUri)
        oUri.OpenBrowser


    End Sub

    Private Async Sub uiGetInstagramToken_Click(sender As Object, e As RoutedEventArgs)
        ' https://developers.facebook.com/docs/facebook-login/guides/advanced/manual-flow

        Dim sUri As String = "https://api.instagram.com/oauth/authorize?"
        sUri &= $"client_id={IG_APP_ID}"
        sUri &= $"&redirect_uri=https://{Await SettingsShareLogins.GetCurrentMeAsWeb}:{Globs.APP_HTTPS_PORT}/fromIG"
        sUri &= "&scope=user_profile,user_media"
        sUri &= "&response_type=code"

        Me.MsgBox(sUri)

        Dim oUri As New Uri(sUri)
        oUri.OpenBrowser

    End Sub


    Private Class FB_GetLoginNameResponse
        Public Property name As String
    End Class

    Private Class FB_GetTokenResponse
        Public Property access_token As String
        ' in seconds
        Public Property expires_in As Integer
    End Class

    Private Class FB_GetLoginCodeResponse
            Public Property code As String
            Public Property user_code As String ' to na ekrnaie pokazać, ludź ma to wpisać w logowaniu
            Public Property verification_uri As String ' "https://www.facebook.com/device",
            Public Property expires_in As Integer ' sekund
            Public Property interval As Integer ' ": 5 ' co ile sekund sprawdzać czy poszło
        End Class
    End Class

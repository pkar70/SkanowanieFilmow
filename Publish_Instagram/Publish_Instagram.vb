Imports System.IO
Imports System.Net
Imports System.Net.Mail
Imports InstagramApiSharp
Imports InstagramApiSharp.API
Imports InstagramApiSharp.Classes.Models
Imports Vblib
Imports vb14 = Vblib.pkarlibmodule14


Public Class Publish_Instagram
    Inherits Vblib.CloudPublish

    Public Const PROVIDERNAME As String = "Instagram"

    Public Overrides Property sProvider As String = PROVIDERNAME

    Private _DataDir As String

    Private Function GetSessionFilePathName() As String
        Return IO.Path.Combine(_DataDir, $"instaSessionState.{konfiguracja.nazwa}.bin")
    End Function

    Public Overrides Async Function Login() As Task(Of String)
        If Await InstaNugetLoginAsync() Then Return ""
        Return "ERROR: incorrect login"
    End Function

    Public Overrides Async Function SendFileMain(oPic As Vblib.OnePic) As Task(Of String)
        If Not Await EnsureLoggedIn() Then Return "ERROR: przed SendFile musi byæ LOGIN"

        ' If String.IsNullOrEmpty(sZmienneZnaczenie) Then Return "ERROR: Publish_AdHoc, folderForFiles is not set"

        ' jesteœmy po pipeline, które jest "piêtro wy¿ej"

        ' wyœlij - 
        oPic._PipelineOutput.Seek(0, SeekOrigin.Begin)
        Dim sRet As String = Await InstaSendPic(oPic)
        Return sRet
        ' w innych Publish: uzupelnij info w oPic o publishingu
        ' oPic.AddCloudPublished(konfiguracja.nazwa, "")

    End Function

#Disable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
    Public Overrides Async Function GetMBfreeSpace() As Task(Of Integer)
        Return Integer.MaxValue ' no limits
    End Function
#Enable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously

    Public Overrides Function CreateNew(oConfig As Vblib.CloudConfig, oPostProcs As Vblib.PostProcBase(), sDataDir As String) As Vblib.AnyStorage
        If oConfig.sProvider <> sProvider Then Return Nothing

        Dim oNew As New Publish_Instagram
        oNew.konfiguracja = oConfig
        oNew._PostProcs = oPostProcs
        oNew._DataDir = sDataDir
        Return oNew
    End Function



    Public Overrides Async Function SendFilesMain(oPicki As List(Of Vblib.OnePic), oNextPic As JedenWiecejPlik) As Task(Of String)

        ' tu jest proste - zwyk³e wywo³anie SendFile dla kolejnych
        For Each oPicek As Vblib.OnePic In oPicki
            Dim sRet As String = Await SendFileMain(oPicek)
            If sRet <> "" Then Return $"When sending {oPicek.sSuggestedFilename}: " & sRet
            oNextPic()
        Next

        Return ""
    End Function

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

        If mInstaApi Is Nothing Then Return "ERROR: przed VerifyFile:Resend musi byæ LOGIN"

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


    Protected Overrides Async Function GetRemoteTagsMain(oPic As Vblib.OnePic) As Task(Of String)
        Dim sId As String = oPic.GetCloudPublishedId(konfiguracja.nazwa)
        If sId = "" Then Return "ERROR: nie mam zapisanego ID pliku"

        If Not Await EnsureLoggedIn() Then Return "ERROR: przed Delete musi byæ LOGIN"

        Dim iInd As Integer = sId.IndexOf("|")
        Dim sIdek As String = sId.Substring(iInd + 1)

        Dim oRet = Await mInstaApi.MediaProcessor.GetMediaByIdAsync(sIdek)
        If Not oRet.Succeeded Then Return "ERROR: " & oRet.Info.Message

        Dim sKeywordInDesc As String = "CLOUD:" & konfiguracja.nazwa

        If konfiguracja.processLikes Then
            If oRet.Value.LikesCount > 0 Then
                Dim sLikes As String = $"Likes: {oRet.Value.LikesCount}"
                sLikes &= " ("
                Dim iGuard As Integer = 10
                For Each oLike As InstaUserShort In oRet.Value.Likers
                    sLikes = sLikes & oLike.UserName & ","
                    iGuard -= 1
                    If iGuard < 0 Then
                        sLikes &= "..."
                        Exit For
                    End If
                Next
                sLikes &= ")"

                oPic.AddDescription(New Vblib.OneDescription(sLikes, sKeywordInDesc))
            End If
        End If

        If oRet.Value.PreviewComments IsNot Nothing Then
            For Each oComment As InstaComment In oRet.Value.PreviewComments
                ' tylko "nieswoje"
                If oComment.User.UserName <> konfiguracja.sUsername Then
                    ' *TODO* pomijamy reply, tylko komentarze
                    Dim sData As String = oComment.CreatedAt.ToString("yyyy.MM.dd HH:mm")
                    Dim sComm As String = $"{oComment.Text} ({oComment.User.UserName})"
                    oPic.TryAddDescription(New Vblib.OneDescription(sData, sComm, sKeywordInDesc))
                End If
            Next
        End If

        Return ""
    End Function

    Public Overrides Async Function Delete(oPic As Vblib.OnePic) As Task(Of String)
        Dim sId As String = oPic.GetCloudPublishedId(konfiguracja.nazwa)
        If sId = "" Then Return "ERROR: nie mam zapisanego ID pliku"

        If Not Await EnsureLoggedIn() Then Return "ERROR: przed Delete musi byæ LOGIN"

        Dim iInd As Integer = sId.IndexOf("|")
        Dim sIdek As String = sId.Substring(iInd + 1)
        Dim oRet = Await mInstaApi.MediaProcessor.DeleteMediaAsync(sIdek, InstaMediaType.Image)
        If Not oRet.Succeeded Then Return "ERROR: " & oRet.Info.Message

        oPic.RemoveCloudPublished(konfiguracja.nazwa)

        Return ""

    End Function

    Public Overrides Async Function GetShareLink(oPic As Vblib.OnePic) As Task(Of String)
        Dim sId As String = oPic.GetCloudPublishedId(konfiguracja.nazwa)
        If sId = "" Then Return ""
        Dim iInd As Integer = sId.IndexOf("|")
        Return "https://www.instagram.com/p/" & sId.Substring(0, iInd) & "/"
    End Function

    Public Overrides Async Function GetShareLink(oOneDir As Vblib.OneDir) As Task(Of String)
        Return ""
    End Function

    Public Overrides Async Function Logout() As Task(Of String)
        Return ""
    End Function

#Enable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously

    Private Async Function EnsureLoggedIn() As Task(Of Boolean)
        If mInstaApi IsNot Nothing Then Return True
        Await Login()
        If mInstaApi IsNot Nothing Then Return True
        Return False
    End Function


#Region "ramtinak/InstagramApiSharp"

    Private mInstaApi As InstagramApiSharp.API.IInstaApi = Nothing

    Private Async Function InstaNugetLoginAsync() As Task(Of Boolean)
        If mInstaApi IsNot Nothing Then Return True

        Dim mUserLogin As InstagramApiSharp.Classes.UserSessionData = New InstagramApiSharp.Classes.UserSessionData
        mUserLogin.UserName = konfiguracja.sUsername
        mUserLogin.Password = konfiguracja.sPswd

        If mUserLogin.Password = "" Then
            Await vb14.DialogBoxAsync("No password set!")
            Return False
        End If

        Dim _instaApiBuilder As InstagramApiSharp.API.Builder.IInstaApiBuilder =
            InstagramApiSharp.API.Builder.InstaApiBuilder.CreateBuilder().SetUser(mUserLogin)
        mInstaApi = _instaApiBuilder.Build()

        ' 2021.09.29 bo jak nie ma ustalonego, to jest random!
        ' https://github.com/ramtinak/InstagramApiSharp/blob/master/src/InstagramApiSharp/API/Builder/InstaApiBuilder.cs
        ' if (_device == null) _device = AndroidDeviceGenerator.GetRandomAndroidDevice();
        Dim oAndroid As InstagramApiSharp.Classes.Android.DeviceInfo.AndroidDevice =
            InstagramApiSharp.Classes.Android.DeviceInfo.AndroidDeviceGenerator.GetByName(
                InstagramApiSharp.Classes.Android.DeviceInfo.AndroidDevices.GALAXY5)
        mInstaApi.SetDevice(oAndroid)

        Dim sSessionFile As String = GetSessionFilePathName()

        If IO.File.Exists(sSessionFile) Then
            '2021.09.26 ogranicznik czasu logowania
            If IO.File.GetCreationTime(sSessionFile).AddHours(24) < DateTime.Now Then
                IO.File.Delete(sSessionFile)
            Else
                mInstaApi.LoadStateDataFromString(IO.File.ReadAllText(sSessionFile))
            End If
        End If

        If Not mInstaApi.IsUserAuthenticated Then

            '// Call this function before calling LoginAsync
            vb14.DumpMessage("SendRequestsBeforeLoginAsync...")
            Await mInstaApi.SendRequestsBeforeLoginAsync()
            '// wait 5 seconds
            Await Task.Delay(5000)

            vb14.DumpMessage("LoginAsync...")
            Dim logInResult = Await mInstaApi.LoginAsync()
            If Not logInResult.Succeeded Then

                If logInResult.Value <> InstagramApiSharp.Classes.InstaLoginResult.ChallengeRequired Then
                    Await vb14.DialogBoxAsync("Cannot login! reason:  " & logInResult.Value.ToString)
                    mInstaApi = Nothing
                    Return False
                End If

                ' czyli nie udane zalogowanie, i chc¹ challenge
                Dim bEmail As Boolean = Await vb14.DialogBoxYNAsync("Challenge musi byæ, TAK: email, NIE: SMS")
                Dim challMethods = Await mInstaApi.GetChallengeRequireVerifyMethodAsync()
                If Not challMethods.Succeeded Then
                    Await vb14.DialogBoxAsync("Cannot get challenge methods! reason: " & challMethods.Value.ToString)
                    mInstaApi = Nothing
                    Return False
                End If

                If bEmail Then
                    Dim emailChall = Await mInstaApi.RequestVerifyCodeToEmailForChallengeRequireAsync()
                    If Not emailChall.Succeeded Then
                        Await vb14.DialogBoxAsync("Cannot get email challenge! reason: " & emailChall.Value.ToString)
                        mInstaApi = Nothing
                        Return False
                    End If

                Else
                    Dim smsChall = Await mInstaApi.RequestVerifyCodeToSMSForChallengeRequireAsync()
                    If Not smsChall.Succeeded Then
                        Await vb14.DialogBoxAsync("Cannot get SMS challenge! reason: " & smsChall.Value.ToString)
                        mInstaApi = Nothing
                        Return False
                    End If
                End If

                Dim smsCode As String = Await vb14.DialogBoxInputDirectAsync("Podaj kod:")
                If smsCode = "" Then
                    Await vb14.DialogBoxAsync("No to nie bedzie loginu (bez challenge ani rusz)")
                    mInstaApi = Nothing
                    Return False
                End If

                Dim challResp = Await mInstaApi.VerifyCodeForChallengeRequireAsync(smsCode)
                If Not challResp.Succeeded Then
                    Await vb14.DialogBoxAsync("Challenge error! reason: " & challResp.Value.ToString)
                    mInstaApi = Nothing
                    Return False
                End If


            End If
        End If

        Dim sNewState As String = mInstaApi.GetStateDataAsString()
        IO.File.WriteAllText(sSessionFile, sNewState)

        Return True
    End Function

    Private Async Function InstaSendPic(oPic As Vblib.OnePic) As Task(Of String)
        Dim obrazekTam As New InstaImageUpload()
        Dim iPicSize As Integer = oPic._PipelineOutput.Length
        obrazekTam.ImageBytes = New Byte(iPicSize - 1) {}
        If Await oPic._PipelineOutput.ReadAsync(obrazekTam.ImageBytes, 0, iPicSize) < iPicSize Then
            Return "ERROR: cannot read picture bytes"
        End If

        Dim sCaption As String = oPic.GetDescriptionForCloud
        Dim oRet = Await mInstaApi.MediaProcessor.UploadPhotoAsync(obrazekTam, sCaption)
        If Not oRet.Succeeded Then Return "ERROR: " & oRet.Info.Message

        oPic.AddCloudPublished(konfiguracja.nazwa, oRet.Value.Code & "|" & oRet.Value.Pk)

        ' https://github.com/ramtinak/InstagramApiSharp/blob/master/src/InstagramApiSharp/API/Processors/MediaProcessor.cs
        Return ""
    End Function

#End Region

End Class


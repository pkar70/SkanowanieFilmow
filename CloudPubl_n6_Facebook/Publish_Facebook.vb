Imports System.IO
Imports vb14 = Vblib.pkarlibmodule14
Imports Vblib
Imports Facebook

Public Class Publish_Facebook_Post
    Inherits Publish_Facebook

    Public Const PROVIDERNAME As String = "Facebook post"

    Public Overrides Property sProvider As String = PROVIDERNAME

    Public Overrides Async Function SendFileMain(oPic As Vblib.OnePic) As Task(Of String)
        vb14.DumpCurrMethod()

        Return Await SendFileMainFB(oPic, "")

    End Function

    Public Overrides Function CreateNew(oConfig As Vblib.CloudConfig, oPostProcs As Vblib.PostProcBase(), sDataDir As String) As Vblib.AnyStorage
        If oConfig.sProvider <> sProvider Then Return Nothing

        Dim oNew As New Publish_Facebook_Post
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


End Class

Public Class Publish_Facebook_Album
    Inherits Publish_Facebook

    Public Const PROVIDERNAME As String = "Facebook Album"

    Public Overrides Property sProvider As String = PROVIDERNAME

    Public Overrides Async Function SendFileMain(oPic As Vblib.OnePic) As Task(Of String)

        'Dim sAlbumName As String = konfiguracja.additInfo
        'If String.IsNullOrWhiteSpace(sAlbumName) Then sAlbumName = oPic.TargetDir

        'Dim albumId As String = Await mMordkaApi.GetOrCreateAlbum(sAlbumName)
        'If albumId.StartsWith("ERROR") Then Return albumId

        '' sRet to numer albumu
        'Dim iPicSize As Integer = oPic._PipelineOutput.Length
        'Dim ImageBytes As Byte() = New Byte(iPicSize - 1) {}
        'oPic._PipelineOutput.Seek(0, SeekOrigin.Begin)
        'If Await oPic._PipelineOutput.ReadAsync(ImageBytes, 0, iPicSize) < iPicSize Then
        '    Return "ERROR: cannot read picture bytes"
        'End If

        'Dim sCaption As String = oPic.GetDescriptionForCloud
        'Dim sRet As String = Await mMordkaApi.MediaProcessor.UploadPhotoToAlbumAsync(albumId, sCaption, oPic.sSuggestedFilename, ImageBytes)
        'If sRet.StartsWith("ERROR") Then Return sRet

        'oPic.AddCloudPublished(konfiguracja.nazwa, $"media/set/?set=a.{albumId}")

        'Return ""

        ' Return Await SendFileMainFB(oPic, oPic.TargetDir)

    End Function

    Public Overrides Function CreateNew(oConfig As Vblib.CloudConfig, oPostProcs As Vblib.PostProcBase(), sDataDir As String) As Vblib.AnyStorage
        If oConfig.sProvider <> sProvider Then Return Nothing

        Dim oNew As New Publish_Facebook_Album
        oNew.konfiguracja = oConfig
        oNew._PostProcs = oPostProcs
        oNew._DataDir = sDataDir

        Return oNew
    End Function

    Private Async Function CreateAlbum(sAlbumName As String) As Task(Of String)
        'Dim sRet As String = Await mMordkaApi.GetOrCreateAlbum(sAlbumName)
        'If sRet.StartsWith("ERROR") Then
        '    ' to jest error
        'Else
        '    ' to jest ID albumu
        'End If
    End Function

    Public Overrides Async Function SendFilesMain(oPicki As List(Of Vblib.OnePic), oNextPic As JedenWiecejPlik) As Task(Of String)

        'Dim sAlbumName As String = konfiguracja.additInfo
        'If Not String.IsNullOrWhiteSpace(sAlbumName) Then
        '    Dim albumId As String = Await mMordkaApi.GetOrCreateAlbum(sAlbumName)
        '    If albumId.StartsWith("ERROR") Then Return albumId

        '    ' *TODO* do tego albumu wyœlikj pliki - ju¿ s¹ po pipeline
        'Else
        '    ' skoro mog¹ byæ ró¿ne foldery za ka¿dym razem, rób to zupe³nie pojedynczo
        '    For Each oPicek As Vblib.OnePic In oPicki
        '        Dim sRet As String = Await SendFileMain(oPicek)
        '        If sRet <> "" Then Return $"When sending {oPicek.sSuggestedFilename}: " & sRet
        '        oNextPic()
        '    Next

        '    Return ""
        'End If

        'Return "ERROR unexpected place in code"
    End Function

End Class

Partial Public MustInherit Class Publish_Facebook
    Inherits Vblib.CloudPublish

    Private _token As String    ' wa¿ny 60 dni, ale raczej tak d³ugo nikt nie bêdize mia³ otwartego PicSorta
    Protected _DataDir As String

    Private Async Function EnsureLoggedIn() As Task(Of Boolean)
        vb14.DumpCurrMethod()

        'If mMordkaApi IsNot Nothing Then Return True
        'Await Login()
        'If mMordkaApi IsNot Nothing Then Return True
        'Return False
    End Function

    Protected Async Function SendFileMainFB(oPic As Vblib.OnePic, sAlbumName As String) As Task(Of String)
        vb14.DumpCurrMethod()

        If Not Await EnsureLoggedIn() Then Return "ERROR: przed SendFile musi byæ LOGIN"

        ' jesteœmy po pipeline, które jest "piêtro wy¿ej"

        oPic._PipelineOutput.Seek(0, SeekOrigin.Begin)
        Dim sRet As String = Await MordkaSendPic(oPic, sAlbumName)
        Return sRet
    End Function

#Disable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
    Public Overrides Async Function GetMBfreeSpace() As Task(Of Integer)
        Return Integer.MaxValue ' no limits
    End Function

    Public Overrides Async Function GetFile(oPic As Vblib.OnePic) As Task(Of String)
        Dim sLink As String = Await GetShareLink(oPic)
        If sLink = "" Then Return "ERROR: nie mam zapisanego ID pliku"

        ' *TODO* na razie i tak nie bêdzie wykorzystywane, podobnie jak w LocalStorage
        ' *TODO* raczej bedzie konieczny LOGIN
        Throw New NotImplementedException()
    End Function


    Protected Overrides Async Function GetRemoteTagsMain(oPic As Vblib.OnePic) As Task(Of String)
        Dim sLink As String = Await GetShareLink(oPic)
        If sLink = "" Then Return "ERROR: nie mam zapisanego ID pliku"

        ' If mMordkaApi Is Nothing Then Return "ERROR: przed GetRemoteTags musi byæ LOGIN"

        ' *TODO* na razie i tak nie bêdzie wykorzystywane, podobnie jak w LocalStorage
        Throw New NotImplementedException()
    End Function

    Public Overrides Async Function Delete(oPic As Vblib.OnePic) As Task(Of String)
        Dim sLink As String = Await GetShareLink(oPic)
        If sLink = "" Then Return "ERROR: nie mam zapisanego ID pliku"

        ' If mMordkaApi Is Nothing Then Return "ERROR: przed Delete musi byæ LOGIN"

        ' *TODO* na razie i tak nie bêdzie wykorzystywane, podobnie jak w LocalStorage
        Throw New NotImplementedException()
    End Function

    Public Overrides Async Function GetShareLink(oPic As Vblib.OnePic) As Task(Of String)
        Dim sId As String = oPic.GetCloudPublishedId(konfiguracja.nazwa)
        If sId = "" Then Return ""

        Return "https://www.facebook.com/" & sId & "/"
    End Function
#Enable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously

    Public Overrides Async Function Login() As Task(Of String)
        If Await FacebookLoginAsync() Then Return ""
        Return "ERROR: incorrect login"
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

        ' If mMordkaApi Is Nothing Then Return "ERROR: przed VerifyFile:Resend musi byæ LOGIN"

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

    Private Function GetSessionFilePathName() As String
        Return IO.Path.Combine(_DataDir, $"mordkowiecSessionState.{konfiguracja.nazwa}.bin")
    End Function


#Region "ramtinak/FacebookApiSharp"
    ' https://dotnetthoughts.net/how-to-upload-image-on-facebook-using-graph-api-and-c/

    'Protected mMordkaApi As API.IFacebookApi = Nothing


    Private Async Function FacebookLoginAsync() As Task(Of Boolean)
        '        vb14.DumpCurrMethod()

        '        If mMordkaApi IsNot Nothing Then Return True

        '        ' https://developers.facebook.com/docs/pages/access-tokens

        '        If String.IsNullOrWhiteSpace(konfiguracja.sUsername) Then
        '            Await vb14.DialogBoxAsync("ERROR: use username for App ID")
        '            Return False
        '        End If
        '        If String.IsNullOrWhiteSpace(konfiguracja.sPswd) Then
        '            Await vb14.DialogBoxAsync("ERROR: use password for App Secret")
        '            Return False
        '        End If

        '        Dim mUserLogin As Classes.UserSessionData = New Classes.UserSessionData
        '        mUserLogin.User = konfiguracja.sUsername
        '        mUserLogin.Password = konfiguracja.sPswd

        '        Dim sSessionFile As String = GetSessionFilePathName()

        '        Dim _instaApiBuilder As API.Builder.IFacebookApiBuilder =
        '            API.Builder.FacebookApiBuilder.CreateBuilder.SetUser(mUserLogin)
        '        _instaApiBuilder = _instaApiBuilder.UseLogger(New Logger.DebugLogger(Logger.LogLevel.None))
        '        _instaApiBuilder = _instaApiBuilder.SetRequestDelay(Classes.RequestDelay.FromSeconds(0, 1))
        '        _instaApiBuilder = _instaApiBuilder.SetSessionHandler(New Classes.SessionHandlers.FileSessionHandler With {.FilePath = sSessionFile})

        '        mMordkaApi = _instaApiBuilder.Build()


        '        mMordkaApi.SimCountry = "pl" ' API.FacebookApi.NetworkCountry // lower case < us => united states
        '        mMordkaApi.ClientCountryCode = "PL" ' most be upper Case <US =>  united states
        '        mMordkaApi.AppLocale = "pl_PL" ' If you want en_US , no need To Set these

        '        '// load old session
        '        If IO.File.Exists(sSessionFile) Then
        '            '2021.09.26 ogranicznik czasu logowania
        '            If IO.File.GetCreationTime(sSessionFile).AddHours(4 * 24) < DateTime.Now Then
        '                IO.File.Delete(sSessionFile)
        '            Else
        '                mMordkaApi.SessionHandler?.Load()
        '            End If
        '        End If

        '        If Not mMordkaApi.IsUserAuthenticated Then '// If we weren't logged in
        '            Await mMordkaApi.SendLoginFlowsAsync()

        '            Dim loginResult = Await mMordkaApi.LoginAsync()
        '            If Not loginResult.Succeeded Then '// logged In
        '#Disable Warning BC40000 ' Type or member is obsolete
        '                Select Case loginResult.Value
        '                    Case Enums.FacebookLoginResult.WrongUserOrPassword
        '                        Await vb14.DialogBoxAsync("Wrong Credentials (user or password is wrong)")
        '                        Return False
        '                    Case Enums.FacebookLoginResult.RenewPwdEncKeyPkg
        '                        Await vb14.DialogBoxAsync("Press login button again")
        '                        Return False
        '                    Case Enums.FacebookLoginResult.SMScodeRequired
        '                        Dim sCode As String = Await vb14.DialogBoxInputAllDirectAsync("Enter SMS code")
        '                        If sCode = "" Then Return False

        '                        loginResult = Await mMordkaApi.LoginSMScodeAsync(sCode)
        '                        If Not loginResult.Succeeded Then '// logged In
        '                            Await vb14.DialogBoxAsync("niby by³ SMS, ale dalej Ÿle")
        '                            Return ""
        '                        End If
        '                    Case Else
        '                        Await vb14.DialogBoxAsync("Login error: " & loginResult.Value)
        '#Enable Warning BC40000 ' Type or member is obsolete
        '                        Return False
        '                End Select

        '            End If

        '            '        //// library will saves session automatically, so no need to do this:
        '            '        //FacebookApi.SessionHandler?.Save();
        '            '        Connected();
        '            '        // after we logged in, we need to sends some requests
        '            Await mMordkaApi.SendAfterLoginFlowsAsync()
        '        Else

        '            '{
        '            '    Connected();
        '            '}
        '        End If


        '        ' 2021.09.29 bo jak nie ma ustalonego, to jest random!
        '        ' https://github.com/ramtinak/InstagramApiSharp/blob/master/src/InstagramApiSharp/API/Builder/InstaApiBuilder.cs
        '        ' if (_device == null) _device = AndroidDeviceGenerator.GetRandomAndroidDevice();
        '        'Dim oAndroid As Classes.Android.DeviceInfo.AndroidDevice =
        '        '    Classes.Android.DeviceInfo.AndroidDeviceGenerator.GetByName(
        '        '        Classes.Android.DeviceInfo.AndroidDevices.GALAXY5)
        '        'mMordkaApi.SetDevice(oAndroid)

        '        Return True
    End Function

    Private Async Function MordkaSendPic(oPic As Vblib.OnePic, sAlbumName As String) As Task(Of String)

        'Dim iPicSize As Integer = oPic._PipelineOutput.Length
        'Dim ImageBytes As Byte() = New Byte(iPicSize - 1) {}
        'If Await oPic._PipelineOutput.ReadAsync(ImageBytes, 0, iPicSize) < iPicSize Then
        '    Return "ERROR: cannot read picture bytes"
        'End If

        'Dim sCaption As String = oPic.GetDescriptionForCloud
        'Dim oRet = Await mMordkaApi.MediaProcessor.UploadPhotoAsync(sCaption, ImageBytes, False) ' ostatnie: disable comments
        'If Not oRet.Succeeded Then Return "ERROR: " & oRet.Info.Message

        'Dim sLink As String = oRet.Value.Story.Tracking
        '' Tracking = "{\"top_level_post_id\":\"6144616548904797\",\"content_owner_id_new\":\"100000695378362\",
        '' \"photo_id\":\"6144615472238238\",\"story_location\":9,\"story_attachment_style\":\"photo\",\"ent_attachement_type\":\"MediaAttachment\",\"actrs\":\"100000695378362\",...
        'Dim iInd As Integer = sLink.IndexOf("photo_id")
        'sLink = sLink.Substring(iInd + "photo_id"":""")
        'iInd = sLink.IndexOf("""")
        'sLink = sLink.Substring(0, iInd)

        'oPic.AddCloudPublished(konfiguracja.nazwa, "photo/?fbid=" & sLink)

        'Return ""
    End Function



#End Region
End Class


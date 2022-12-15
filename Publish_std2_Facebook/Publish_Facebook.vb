Imports System.IO
Imports System.Text
Imports vb14 = Vblib.pkarlibmodule14
Imports FacebookApiSharp
Imports System.Runtime.InteropServices.ComTypes


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

End Class

Public Class Publish_Facebook_Album
    Inherits Publish_Facebook

    Public Const PROVIDERNAME As String = "Facebook Album"

    Public Overrides Property sProvider As String = PROVIDERNAME

    Public Overrides Async Function SendFileMain(oPic As Vblib.OnePic) As Task(Of String)

        Return Await SendFileMainFB(oPic, oPic.TargetDir)

    End Function

    Public Overrides Function CreateNew(oConfig As Vblib.CloudConfig, oPostProcs As Vblib.PostProcBase(), sDataDir As String) As Vblib.AnyStorage
        If oConfig.sProvider <> sProvider Then Return Nothing

        Dim oNew As New Publish_Facebook_Album
        oNew.konfiguracja = oConfig
        oNew._PostProcs = oPostProcs
        oNew._DataDir = sDataDir

        Return oNew
    End Function

End Class

Public MustInherit Class Publish_Facebook
    Inherits Vblib.CloudPublish

    Private _token As String    ' wa�ny 60 dni, ale raczej tak d�ugo nikt nie b�dize mia� otwartego PicSorta
    Protected _DataDir As String

    Private Async Function EnsureLoggedIn() As Task(Of Boolean)
        vb14.DumpCurrMethod()

        If mInstaApi IsNot Nothing Then Return True
        Await Login()
        If mInstaApi IsNot Nothing Then Return True
        Return False
    End Function

    Protected Async Function SendFileMainFB(oPic As Vblib.OnePic, sAlbumName As String) As Task(Of String)
        vb14.DumpCurrMethod()

        If Not Await EnsureLoggedIn() Then Return "ERROR: przed SendFile musi by� LOGIN"

        ' jeste�my po pipeline, kt�re jest "pi�tro wy�ej"

        oPic._PipelineOutput.Seek(0, SeekOrigin.Begin)
        Dim sRet As String = Await InstaSendPic(oPic)
        Return sRet
    End Function

#Disable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
    Public Overrides Async Function GetMBfreeSpace() As Task(Of Integer)
        Return Integer.MaxValue ' no limits
    End Function

    Public Overrides Async Function SendFiles(oPicki As List(Of Vblib.OnePic)) As Task(Of String)
        ' *TODO* na razie i tak nie b�dzie wykorzystywane, podobnie jak w LocalStorage
        Throw New NotImplementedException()
    End Function

    Public Overrides Async Function GetFile(oPic As Vblib.OnePic) As Task(Of String)
        Dim sLink As String = Await GetShareLink(oPic)
        If sLink = "" Then Return "ERROR: nie mam zapisanego ID pliku"

        ' *TODO* na razie i tak nie b�dzie wykorzystywane, podobnie jak w LocalStorage
        ' *TODO* raczej bedzie konieczny LOGIN
        Throw New NotImplementedException()
    End Function


    Protected Overrides Async Function GetRemoteTagsMain(oPic As Vblib.OnePic) As Task(Of String)
        Dim sLink As String = Await GetShareLink(oPic)
        If sLink = "" Then Return "ERROR: nie mam zapisanego ID pliku"

        ' If mInstaApi Is Nothing Then Return "ERROR: przed GetRemoteTags musi by� LOGIN"

        ' *TODO* na razie i tak nie b�dzie wykorzystywane, podobnie jak w LocalStorage
        Throw New NotImplementedException()
    End Function

    Public Overrides Async Function Delete(oPic As Vblib.OnePic) As Task(Of String)
        Dim sLink As String = Await GetShareLink(oPic)
        If sLink = "" Then Return "ERROR: nie mam zapisanego ID pliku"

        ' If mInstaApi Is Nothing Then Return "ERROR: przed Delete musi by� LOGIN"

        ' *TODO* na razie i tak nie b�dzie wykorzystywane, podobnie jak w LocalStorage
        Throw New NotImplementedException()
    End Function

    Public Overrides Async Function GetShareLink(oPic As Vblib.OnePic) As Task(Of String)
        Dim sId As String = oPic.GetCloudPublishedId(konfiguracja.nazwa)
        If sId = "" Then Return ""

        Return "https://www.instagram.com/p/" & sId & "/"
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

        ' If mInstaApi Is Nothing Then Return "ERROR: przed VerifyFile:Resend musi by� LOGIN"

        ' *TODO* na razie i tak nie b�dzie wykorzystywane, podobnie jak w LocalStorage
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

    Private mInstaApi As API.IFacebookApi = Nothing


    Private Async Function FacebookLoginAsync() As Task(Of Boolean)
        vb14.DumpCurrMethod()

        If mInstaApi IsNot Nothing Then Return True

        ' https://developers.facebook.com/docs/pages/access-tokens

        If String.IsNullOrWhiteSpace(konfiguracja.sUsername) Then
            Await vb14.DialogBoxAsync("ERROR: use username for App ID")
            Return False
        End If
        If String.IsNullOrWhiteSpace(konfiguracja.sPswd) Then
            Await vb14.DialogBoxAsync("ERROR: use password for App Secret")
            Return False
        End If

        Dim mUserLogin As Classes.UserSessionData = New Classes.UserSessionData
        mUserLogin.User = konfiguracja.sUsername
        mUserLogin.Password = konfiguracja.sPswd

        Dim sSessionFile As String = GetSessionFilePathName()

        Dim _instaApiBuilder As API.Builder.IFacebookApiBuilder =
            API.Builder.FacebookApiBuilder.CreateBuilder.SetUser(mUserLogin)
        _instaApiBuilder = _instaApiBuilder.UseLogger(New Logger.DebugLogger(Logger.LogLevel.None))
        _instaApiBuilder = _instaApiBuilder.SetRequestDelay(Classes.RequestDelay.FromSeconds(0, 1))
        _instaApiBuilder = _instaApiBuilder.SetSessionHandler(New Classes.SessionHandlers.FileSessionHandler With {.FilePath = sSessionFile})

        mInstaApi = _instaApiBuilder.Build()


        mInstaApi.SimCountry = "pl" ' API.FacebookApi.NetworkCountry // lower case < us => united states
        mInstaApi.ClientCountryCode = "PL" ' most be upper Case <US =>  united states
        mInstaApi.AppLocale = "pl_PL" ' If you want en_US , no need To Set these

        '// load old session
        If IO.File.Exists(sSessionFile) Then
            '2021.09.26 ogranicznik czasu logowania
            If IO.File.GetCreationTime(sSessionFile).AddHours(24) < DateTime.Now Then
                IO.File.Delete(sSessionFile)
            Else
                mInstaApi.SessionHandler?.Load()
            End If
        End If

        If Not mInstaApi.IsUserAuthenticated Then '// If we weren't logged in
            Await mInstaApi.SendLoginFlowsAsync()

            Dim loginResult = Await mInstaApi.LoginAsync()
            If loginResult.Succeeded Then '// logged In
                '        //// library will saves session automatically, so no need to do this:
                '        //FacebookApi.SessionHandler?.Save();
                '        Connected();
                '        // after we logged in, we need to sends some requests
                Await mInstaApi.SendAfterLoginFlowsAsync()
            Else
                Select Case loginResult.Value
                    Case Enums.FacebookLoginResult.WrongUserOrPassword
                        Await vb14.DialogBoxAsync("Wrong Credentials (user or password is wrong)")
                        Return False
                    Case Enums.FacebookLoginResult.RenewPwdEncKeyPkg
                        Await vb14.DialogBoxAsync("Press login button again")
                        Return False
                    Case Enums.FacebookLoginResult.SMScodeRequired
                        Dim sCode As String = Await vb14.DialogBoxInputAllDirectAsync("Enter SMS code")
                        If sCode = "" Then Return False
                        ' *TODO* zr�b co trzeba
                    Case Else
                        Await vb14.DialogBoxAsync("Login error: " & loginResult.Value)
                        Return False
                End Select

            End If
        Else
            '{
            '    Connected();
            '}
        End If


        ' 2021.09.29 bo jak nie ma ustalonego, to jest random!
        ' https://github.com/ramtinak/InstagramApiSharp/blob/master/src/InstagramApiSharp/API/Builder/InstaApiBuilder.cs
        ' if (_device == null) _device = AndroidDeviceGenerator.GetRandomAndroidDevice();
        'Dim oAndroid As Classes.Android.DeviceInfo.AndroidDevice =
        '    Classes.Android.DeviceInfo.AndroidDeviceGenerator.GetByName(
        '        Classes.Android.DeviceInfo.AndroidDevices.GALAXY5)
        'mInstaApi.SetDevice(oAndroid)

        Return True
    End Function

    Private Async Function InstaSendPic(oPic As Vblib.OnePic) As Task(Of String)

        Dim iPicSize As Integer = oPic._PipelineOutput.Length
        Dim ImageBytes As Byte() = New Byte(iPicSize - 1) {}
        If Await oPic._PipelineOutput.ReadAsync(ImageBytes, 0, iPicSize) < iPicSize Then
            Return "ERROR: cannot read picture bytes"
        End If

        Dim oExif As Vblib.ExifTag = oPic.FlattenExifs(konfiguracja.defaultExif)

        Dim sCaption As String = oExif.UserComment  ' caption z oPic
        Dim oRet = Await mInstaApi.MediaProcessor.UploadPhotoAsync(sCaption, ImageBytes, False) ' ostatnie: disable comments
        If Not oRet.Succeeded Then Return "ERROR: " & oRet.Info.Message

        oPic.AddCloudPublished(konfiguracja.nazwa, oRet.Value.Story.Id)




        ' https://github.com/ramtinak/InstagramApiSharp/blob/master/src/InstagramApiSharp/API/Processors/MediaProcessor.cs
        Return ""
    End Function


#End Region
End Class


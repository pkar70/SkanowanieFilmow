Imports System.IO
Imports InstagramApiSharp
Imports InstagramApiSharp.API
Imports InstagramApiSharp.Classes.Models
Imports vb14 = Vblib.pkarlibmodule14


Public Class Publish_Instagram
    Inherits Vblib.CloudPublish

    Public Const PROVIDERNAME As String = "Instagram"

    Public Overrides Property sProvider As String = PROVIDERNAME


    Private Function GetSessionFilePathName() As String
        Return IO.Path.Combine(IO.Path.GetTempPath, "instaSessionState.bin")
    End Function

    Public Overrides Async Function Login() As Task(Of String)
        If Await InstaNugetLoginAsync() Then Return ""
        Return "ERROR: incorrect login"
    End Function

    Public Overrides Async Function SendFileMain(oPic As Vblib.OnePic) As Task(Of String)
        If mInstaApi Is Nothing Then Return "ERROR: przed SendFile musi byæ LOGIN"

        ' If String.IsNullOrEmpty(sZmienneZnaczenie) Then Return "ERROR: Publish_AdHoc, folderForFiles is not set"

        ' jesteœmy po pipeline, które jest "piêtro wy¿ej"

        ' wyœlij - 
        oPic._PipelineOutput.Seek(0, SeekOrigin.Begin)
        Dim sRet As String = Await InstaSendPic(oPic)
        Return sRet
        ' w innych Publish: uzupelnij info w oPic o publishingu
        ' oPic.AddCloudPublished(konfiguracja.nazwa, "")

    End Function

    Public Overrides Async Function SendFiles(oPicki As List(Of Vblib.OnePic)) As Task(Of String)
        ' *TODO* na razie i tak nie bêdzie wykorzystywane, podobnie jak w LocalStorage
        Throw New NotImplementedException()
    End Function

    Public Overrides Async Function GetMBfreeSpace() As Task(Of Integer)
        Return Integer.MaxValue ' no limits
    End Function

    Public Overrides Function CreateNew(oConfig As Vblib.CloudConfig, oPostProcs As Vblib.PostProcBase()) As Vblib.AnyStorage
        If oConfig.sProvider <> sProvider Then Return Nothing

        Dim oNew As New Publish_Instagram
        oNew.konfiguracja = oConfig
        oNew._PostProcs = oPostProcs

        Return oNew
    End Function



#Disable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
    Public Overrides Async Function VerifyFileExist(oPic As Vblib.OnePic) As Task(Of String)
        Return "Should not be run for Ad-Hoc"
    End Function

    Public Overrides Async Function VerifyFile(oPic As Vblib.OnePic, oCopyFromArchive As Vblib.LocalStorage) As Task(Of String)
        Return "Should not be run for Ad-Hoc"
    End Function

    Public Overrides Async Function GetFile(oPic As Vblib.OnePic) As Task(Of String)
        Return "Should not be run for Ad-Hoc"
    End Function


    Public Overrides Async Function GetRemoteTags(oPic As Vblib.OnePic) As Task(Of String)
        Return "Should not be run for Ad-Hoc"
    End Function

    Public Overrides Async Function Delete(oPic As Vblib.OnePic) As Task(Of String)
        Return "Should not be run for Ad-Hoc"
    End Function

    Public Overrides Async Function GetShareLink(oPic As Vblib.OnePic) As Task(Of String)
        Return "Should not be run for Ad-Hoc"
    End Function

    Public Overrides Async Function GetShareLink(oOneDir As Vblib.OneDir) As Task(Of String)
        Return "Should not be run for Ad-Hoc"
    End Function

    Public Overrides Async Function Logout() As Task(Of String)
        Return ""
    End Function

#Enable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously


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

        Dim oExif As Vblib.ExifTag = oPic.FlattenExifs(konfiguracja.defaultExif)

        Dim sCaption As String = oExif.UserComment  ' caption z oPic
        Dim oRet = Await mInstaApi.MediaProcessor.UploadPhotoAsync(obrazekTam, sCaption)
        If Not oRet.Succeeded Then Return "ERROR: " & oRet.Info.Message

        oPic.AddCloudPublished(konfiguracja.nazwa, oRet.Value.Code)

        ' https://github.com/ramtinak/InstagramApiSharp/blob/master/src/InstagramApiSharp/API/Processors/MediaProcessor.cs
        Return ""
    End Function


#If False Then
    Public Async Function InstaNugetGetUserDataAsync(sChannel As String) As Task(Of JSONinstaUser)
        If mInstaApi Is Nothing Then Return Nothing
        If Not mInstaApi.IsUserAuthenticated Then Return Nothing

        Try
            Dim userInfo = Await mInstaApi.UserProcessor.GetUserInfoByUsernameAsync(sChannel)
            If Not userInfo.Succeeded Then Return Nothing
            Dim oNew As JSONinstaUser = New JSONinstaUser
            oNew.biography = userInfo.Value.Biography
            oNew.full_name = userInfo.Value.FullName
            oNew.id = userInfo.Value.Pk
            Return oNew
        Catch ex As Exception
            Return Nothing
        End Try

    End Function

    Public Async Function InstaNugetFollowAsync(iUserId As Long, bMsg As Boolean) As Task(Of Boolean)
        If mInstaApi Is Nothing Then Return False
        If Not mInstaApi.IsUserAuthenticated Then Return False

        Try
            Dim oRes = Await mInstaApi.UserProcessor.FollowUserAsync(iUserId)
            If oRes.Succeeded Then Return True

            If bMsg Then Await DialogBoxAsync("Error trying to Follow")

        Catch ex As Exception
        End Try
        Return False
    End Function

    Public Function InstaNugetGetCurrentUserId() As Long
        If mInstaApi Is Nothing Then Return 0
        If Not mInstaApi.IsUserAuthenticated Then Return 0

        If mInstaApi.GetLoggedUser.LoggedInUser.Pk > 0 Then Return mInstaApi.GetLoggedUser.LoggedInUser.Pk

        'Dim oCurrUsers = Await mInstaApi.GetLoggedUser.LoggedInUser  ' .GetCurrentUserAsync()
        'If Not oCurrUsers.Succeeded Then Return 0

        'Return oCurrUsers.Value.Pk
        Return 0
    End Function

    'Public Async Function InstaNugetGetFollowingi(oTBoxSetText As UItBoxSetText) As Task(Of List(Of LocalChannel))
    '    Dim oRet As List(Of LocalChannel) = New List(Of LocalChannel)

    '    Dim iUserId As Long = GetSettingsLong("instagramUserId")
    '    If iUserId = 0 Then
    '        iUserId = InstaNugetGetCurrentUserId()
    '        If iUserId = 0 Then
    '            If oTBoxSetText IsNot Nothing Then Await DialogBoxAsync("ERROR: nie moge dostac sie do currentUserId")
    '            Return Nothing
    '        End If
    '        SetSettingsLong("instagramUserId", iUserId)
    '    End If

    '    Dim oPaging As InstagramApiSharp.PaginationParameters = InstagramApiSharp.PaginationParameters.MaxPagesToLoad(20)   ' przy 10 jest chyba OK, ale warto miec rezerwê
    '    Dim sSearchQuery As String = ""
    '    Dim oRes = Await mInstaApi.UserProcessor.GetUserFollowingByIdAsync(iUserId, oPaging, sSearchQuery)
    '    If Not oRes.Succeeded Then Return Nothing

    '    If oRes.Value Is Nothing Then Return Nothing

    '    ' 2021.12.14 - jedno pytanie tylko...
    '    Dim bDodajFollow As Boolean = False

    '    For Each oFoll As InstagramApiSharp.Classes.Models.InstaUserShort In oRes.Value
    '        Dim bMam As Boolean = False
    '        For Each oItem As LocalChannel In _kanaly
    '            If oItem.sChannel.ToLower = oFoll.UserName.ToLower Then
    '                If oItem.iUserId < 10 Then oItem.iUserId = oFoll.Pk
    '                oRet.Add(oItem)
    '                bMam = True
    '            End If
    '        Next
    '        If Not bMam Then
    '            If oTBoxSetText Is Nothing Then Continue For

    '            If Not bDodajFollow Then
    '                If Not Await DialogBoxYNAsync("Following '" & oFoll.UserName & "' - nie ma kana³u, dodaæ?") Then
    '                    Continue For
    '                End If
    '                bDodajFollow = True
    '            End If
    '            Dim sAdded As String = Await TryAddChannelAsync(oFoll.UserName, oTBoxSetText)
    '            If Not sAdded.StartsWith("OK") Then Continue For
    '        End If
    '    Next

    '    Return oRet

    'End Function

    'Public Delegate Sub UIProgRingShow(bVisible As Boolean, dMax As Double)
    'Public Delegate Sub UIProgRingMaxVal(iMax As Integer)
    'Public Delegate Sub UIProgRingInc()

    'Public Async Function InstaNugetRefreshAll(oTBoxSetText As UItBoxSetText,
    '              oProgRingShow As UIProgRingShow, oProgRingMaxVal As UIProgRingMaxVal, oProgRingInc As UIProgRingInc) As Task(Of Integer)
    '    'Dim bMsg As Boolean = False
    '    'If oTB IsNot Nothing Then bMsg = True

    '    oProgRingShow(True, 0)
    '    Dim bLoadOk As Boolean = LoadChannels()
    '    oProgRingShow(False, 0)

    '    If Not bLoadOk Then
    '        If oTBoxSetText IsNot Nothing Then Await DialogBoxAsync("Empty channel list")
    '        Return False
    '    End If

    '    ' sprawdzamy tylko followingi - omijaj¹c w ten sposób mechanizm blokowania kana³ów z aplikacji, jako ¿e teraz to jest rozsynchronizowane
    '    oProgRingShow(True, 0)
    '    If Not Await InstaNugetLoginAsync(oTBoxSetText) Then Return -1

    '    Dim oFollowingi As List(Of LocalChannel) = Await InstaNugetGetFollowingi(oTBoxSetText)
    '    oProgRingShow(False, 0)
    '    If oFollowingi Is Nothing Then Return -1

    '    ' ponizsza czesc bedzie wspolna dla RefreshAll (wedle Following), oraz periodycznego (wedle InstaNugetGetRecentActivity)
    '    Dim bChannelsDirty As Boolean = False
    '    Dim lsToastErrors As List(Of String) = New List(Of String)
    '    Dim lsToastNews As List(Of String) = New List(Of String)
    '    Dim iNewsCount As Integer = 0

    '    Dim iErrCntToStop As Integer = 10

    '    oProgRingShow(True, oFollowingi.Count)
    '    oProgRingMaxVal(oFollowingi.Count)   ' bo to zagnie¿d¿one jest w PRShow, czyli sam z siebie nie zmieni Max

    '    For Each oChannel As LocalChannel In From c In oFollowingi Order By c.sChannel
    '        oProgRingInc()

    '        If oTBoxSetText IsNot Nothing Then oTBoxSetText(oChannel.sChannel)

    '        oProgRingShow(True, 0)
    '        Dim iRet As Integer = Await InstaNugetCheckNewsFromUserAsync(oChannel, oTBoxSetText)   ' w serii, wiec bez czekania na klikanie b³êdów
    '        oProgRingShow(False, 0)

    '        Await Task.Delay(3000)  ' tak samo jak w wersji anonymous, jednak czekamy troche, nawet wiêcej (3 nie 2 sek) - i tak idziemy tylko po follow, nieistniej¹ce s¹ usuniête automatycznie
    '        If iRet < 0 Then
    '            oChannel.iNewCnt = -1
    '            If oChannel.sFirstError = "" Then oChannel.sFirstError = DateTime.Now.ToString("dd MM yyyy")
    '            lsToastErrors.Add(oChannel.sChannel)
    '            iErrCntToStop -= 1
    '            If iErrCntToStop < 0 Then
    '                ' skoro tak duzo bledow pod rz¹d, to pewnie nie ma sensu nic dalej sciagac
    '                ClipPut(msLastHtmlPage)
    '                If oTBoxSetText IsNot Nothing Then Await DialogBoxAsync("za duzo b³êdów pod rz¹d, poddajê siê; w ClipBoard ostatni HTML")
    '                Exit For
    '            End If
    '            bChannelsDirty = True
    '        ElseIf iRet > 0 Then
    '            iErrCntToStop = 10
    '            oChannel.sFirstError = ""  ' kana³ ju¿ nie daje b³êdów
    '            lsToastNews.Add(oChannel.sChannel & " (" & iRet & ")")
    '            oChannel.iNewCnt = iRet
    '            iNewsCount += iRet
    '            bChannelsDirty = True
    '        End If
    '        ' zero: nothing - nic nowego, ale te¿ bez b³êdu
    '        ' ewentualnie kiedyœ sprawdzanie, ¿e dawno nic nie by³o
    '    Next

    '    ' te dwie rzeczy by³y na koncu, ale wtedy czasem nie zapisuje (jak wylatuje) - wiec zrobmy to teraz, jakby crash byl ponizej (a nie w samym sciaganiu)
    '    SetSettingsString("lastRun", DateTime.Now.ToString("yyyy.MM.dd HH:mm"))
    '    If bChannelsDirty Then SaveChannels()

    '    oProgRingShow(False, 0)

    '    Await PrzygotujPokazInfo(lsToastErrors, lsToastNews, (oTBoxSetText IsNot Nothing))
    '    ' przeniesione przed przygotowanie komunikatu
    '    'SetSettingsString("lastRun", DateTime.Now.ToString("yyyy.MM.dd HH:mm"))

    '    If Not bChannelsDirty Then Return False

    '    Return True

    'End Function

    '' wersja 2, teoretycznie szybsza: foreach(user in getRecentActivity) do checkUserActivity()
    '' wersja 3, podobna do powyzej: foreach(activity in getRecentActivity) do sciagnijObrazek
    '' ale co jak jest kilka obrazkow po kolei w jednym? to jest ten story?
    '' ile wpisów jest na page? 12, jak przy user (ze nigdy wiecej nie pokazalo obrazkow niz 12?)
    '' to mo¿e byæ na timer, np. godzinny
    'Public Async Function InstaNugetGetRecentActivity() As Task
    '    If mInstaApi Is Nothing Then Return
    '    If Not mInstaApi.IsUserAuthenticated Then Return

    '    Dim oPaging As InstagramApiSharp.PaginationParameters = InstagramApiSharp.PaginationParameters.MaxPagesToLoad(10)
    '    Dim oRes = Await mInstaApi.UserProcessor.GetFollowingRecentActivityFeedAsync(oPaging)

    '    ' *TODO*

    'End Function


    'Public Async Function InstaNugetCheckNewsFromUserAsync(oChannel As LocalChannel, oTBoxSetText As UItBoxSetText) As Task(Of Integer)
    '    Try

    '        ' folder for pictures
    '        Dim sFold As String = Await GetChannelDir(oChannel, (oTBoxSetText IsNot Nothing))
    '        If sFold = "" Then Return -1

    '        Dim bRet As Boolean
    '        bRet = Await InstaNugetLoginAsync(oTBoxSetText)
    '        If Not bRet Then Return Nothing

    '        ' mozna id wzi¹æ z oItem
    '        If oChannel.iUserId < 10 Then
    '            Dim userInfo = Await mInstaApi.UserProcessor.GetUserInfoByUsernameAsync(oChannel.sChannel)
    '            If Not userInfo.Succeeded Then Return Nothing
    '            oChannel.iUserId = userInfo.Value.Pk
    '            ' kana³y s¹ do póŸniejszego ZAPISU!! choæby dlatego ¿e lastId sie zmienia
    '        End If

    '        Dim userFullInfo = Await mInstaApi.UserProcessor.GetFullUserInfoAsync(oChannel.iUserId)
    '        If Not userFullInfo.Succeeded Then Return Nothing

    '        ' przetworzenie listy z FEED
    '        Dim oWpisy As List(Of InstagramApiSharp.Classes.Models.InstaMedia) = userFullInfo.Value?.Feed?.Items
    '        If oWpisy Is Nothing Then
    '            If oTBoxSetText IsNot Nothing Then Await DialogBoxAsync("Bad List(Of InstaMedia)" & vbCrLf & oChannel.sChannel)
    '            Return -1
    '        End If

    '        ' list of pictures (with data)
    '        Dim oPicList As List(Of LocalPictureData) = LoadPictData(sFold)
    '        Dim bPicListDirty As Boolean = False

    '        Dim sLastGuid As String = oChannel.sLastId
    '        Dim bFirst As Boolean = True
    '        Dim iNewPicCount As Integer = 0

    '        For Each oMedia As InstagramApiSharp.Classes.Models.InstaMedia In oWpisy

    '            If oMedia.Images Is Nothing Then Continue For
    '            If oMedia.Images.Count < 1 Then Continue For

    '            Dim oPic = oMedia.Images.ElementAt(0) ' teoretycznie pierwszy jest najwiekszy, ale...
    '            For Each oPicLoop In oMedia.Images
    '                If oPicLoop.Height > oPic.Height Then oPic = oPicLoop
    '            Next

    '            If oMedia.Pk = sLastGuid Then Exit For
    '            If bFirst Then
    '                oChannel.sLastId = oMedia.Pk
    '                bFirst = False
    '            End If

    '            Dim oNew As LocalPictureData = New LocalPictureData
    '            oNew.iTimestamp = oMedia.TakenAtUnix
    '            If oMedia.Location IsNot Nothing Then oNew.sPlace = oMedia.Location?.Name
    '            ' oNew.sCaptionAccessib = oItem.accessibility_caption ' tego pola nie ma?
    '            oNew.sData = DateTime.Now.ToString("yyyy-MM-dd") ' data wczytania obrazka, nie data obrazka!
    '            If oMedia.Caption IsNot Nothing Then oNew.sCaption = oMedia.Caption.Text
    '            'If oItem?.edge_media_to_caption?.edges IsNot Nothing Then
    '            '    For Each oCapt As JSONinstaNodeCaption In oItem.edge_media_to_caption.edges
    '            '        oNew.sCaption = oNew.sCaption & oCapt.node.text & vbCrLf
    '            '    Next
    '            'End If

    '            oNew.sFileName = Await DownloadPictureAsync(sFold, oPic.Uri)
    '            If oNew.sFileName = "" Then
    '                If oTBoxSetText IsNot Nothing Then Await DialogBoxAsync("Cannot download picture from channel" & vbCrLf & oChannel.sChannel)
    '            Else
    '                ' tylko gdy dodany zosta³ jakiœ obrazek
    '                bPicListDirty = True
    '                iNewPicCount += 1
    '                oPicList.Insert(0, oNew)

    '                '' aktualizacja listy nowoœci
    '                'Dim oNewPicData As LocalNewPicture = New LocalNewPicture
    '                'oNewPicData.oPicture = oNew
    '                'oNewPicData.oChannel = oChannel
    '                'App._gNowosci.Add(oNewPicData)

    '            End If
    '        Next

    '        If Not bPicListDirty Then Return 0

    '        Await SavePictData(oChannel, oPicList, (oTBoxSetText IsNot Nothing))
    '        'Dim sTxt As String = Newtonsoft.Json.JsonConvert.SerializeObject(From c In oPicList Select c Distinct, Newtonsoft.Json.Formatting.Indented)
    '        'Await oFold.WriteAllTextToFileAsync("pictureData.json", sTxt, Windows.Storage.CreationCollisionOption.ReplaceExisting)


    '        Return iNewPicCount

    '    Catch ex As Exception
    '        CrashMessageAdd("GetInstagramFeedFromJSON", ex, True)
    '    End Try

    '    Return -1   ' signal error


    'End Function
#End If

#End Region

End Class


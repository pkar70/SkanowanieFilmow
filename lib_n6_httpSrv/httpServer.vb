Imports System.Collections.Specialized
Imports System.IO
Imports System.Net
Imports System.Runtime.CompilerServices
Imports pkar
Imports pkar.DotNetExtensions

Imports Vblib

Public Class ServerWrapper

    Private _host As HttpListener
    Private Shared _loginy As pkar.BaseList(Of Vblib.ShareLogin)
    Private Shared _databases As Vblib.DatabaseInterface
    Private Shared _lastAccess As Vblib.ShareLoginData
    Private Shared _buffer As Vblib.IBufor
    Private Shared _shareDescIn As pkar.BaseList(Of Vblib.ShareDescription)
    Private Shared _shareDescOut As pkar.BaseList(Of Vblib.ShareDescription)
    Private Shared _postProcs As Vblib.PostProcBase()
    Private Shared _dataFolder As String

    ''' <summary>
    ''' Kiedy ostatnio coœ siê komunikowa³o
    ''' </summary>
    Public Shared _lastNetAccess As New LastNetAccess

    Public Sub New(databases As Vblib.DatabaseInterface)
        _loginy = Vblib.GetShareLogins
        _databases = databases
        _lastAccess = Vblib.gLastLoginSharing
        _buffer = Vblib.GetBuffer
        _shareDescIn = Vblib.GetShareDescriptionsIn
        _shareDescOut = Vblib.GetShareDescriptionsOut
        _postProcs = Vblib.gPostProcesory
        _dataFolder = Vblib.GetDataFolder
    End Sub

    Public Function IsRunning() As Boolean
        If _host Is Nothing Then Return False
        Return _host.IsListening
    End Function


#Region "start/stop"

    Public Sub StartSvc()
        DumpCurrMethod()

        If _host Is Nothing Then Task.Run(Sub() InitService())
    End Sub

    ' uwaga: port tak¿e w PicMenuSearchWebByPic
    Private Const baseUri As String = "http://*:20563/"

    Public Sub TryAddAcl()
        Dim cmdline As String = $"http add urlacl url={baseUri} user={Environment.UserDomainName}\{Environment.UserName}"

        Dim psi As New ProcessStartInfo("netsh", cmdline)
        psi.Verb = "runas"
        psi.CreateNoWindow = True
        psi.WindowStyle = ProcessWindowStyle.Hidden
        psi.UseShellExecute = True

        Process.Start(psi).WaitForExit()

    End Sub

    Private Function TryStart() As Boolean
        _host = New HttpListener
        _host.Prefixes.Add(baseUri)

        Try
            _host.Start()
            Return True
        Catch ex As Exception
            If ex.HResult <> &H80004005 Then Return False
        End Try

        TryAddAcl()

        ' bez tych linijek - _host.Start wylatuje na disposed object
        _host = New HttpListener
        _host.Prefixes.Add(baseUri)

        Try
            _host.Start()
            Return True
        Catch ex As Exception
            Return False
        End Try

    End Function

    Private _stopping As Boolean = False

    Private Async Sub InitService()
        DumpCurrMethod()

        If Not TryStart() Then Return
        ' netsh http add urlacl url=http://+:20563/ user=

        _stopping = False

        Do Until _stopping
            Try
                If _host Is Nothing Then Exit Do

                ' ten call blokuje do pierwszego wywo³ania przez klienta
                Dim context As HttpListenerContext = _host?.GetContext()
                ' a tak reaguje na EXIT:
                ' The I/O operation has been aborted because of either a thread exit or an application request.'

                Dim request As HttpListenerRequest = context?.Request
                If request Is Nothing Then Exit Do   ' takie zabezpieczenie to tylko u³atwienie gdy jest pod debuggerem podczas wy³¹czania programu

                _lastNetAccess.Zapisz("??", "??")

                Vblib.DumpMessage("Mam request: " & request.RawUrl) ' on jest typu: /canupload?guid=xxx&clientHost=Hxxxx

                Dim response As HttpListenerResponse = context.Response
                Dim responseString As String = Await MainWork(request, response)

                If responseString <> "NOTXTRESPONSE" Then
                    Dim buffer As Byte() = System.Text.Encoding.UTF8.GetBytes(responseString)
                    response.ContentLength64 = buffer.Length
                    Using output As System.IO.Stream = response.OutputStream
                        output.Write(buffer, 0, buffer.Length)
                        'You must close the output stream.
                        output.Close()
                    End Using
                End If

            Catch ex As Exception

            End Try
        Loop

    End Sub



    Public Sub StopSvc()
        DumpCurrMethod()

        If _host Is Nothing Then Return
        _stopping = True
        _host.Stop()
        _host.Close()
        _host = Nothing
    End Sub
#End Region

    Public Function GetLogDir() As String
        Dim folder As String = IO.Path.Combine(_dataFolder, "HttpLog")
        If Not IO.Directory.Exists(folder) Then IO.Directory.CreateDirectory(folder)
        Return folder
    End Function

    Private Sub AppendLog(oLogin As Vblib.ShareLogin, msg As String)
        If Not Vblib.GetSettingsBool("uiHttpLog") Then Return
        Dim currFile As String = IO.Path.Combine(GetLogDir, Date.Now.ToString("yyyy-MM") & ".log")
        Dim linia As String = Date.Now.ToExifString & " "
        If oLogin IsNot Nothing Then linia &= oLogin.displayName.Replace(" ", "_") & " "
        linia &= msg & vbCrLf
        linia = linia.Depolit
        ' *TODO* niezbyt to efektywne, bo w kó³ko zapisuje log, bez buforowania - ale na razie rzadko to robimy :)
        IO.File.AppendAllText(currFile, linia)
    End Sub

#Region "real work"

    Private Const PROTO_VERS As String = "1.1"

    ''' <summary>
    ''' returns string = response for remote command
    ''' </summary>
    Private Async Function MainWork(request As HttpListenerRequest, response As HttpListenerResponse) As Task(Of String)

        Dim command As String = request.Url.AbsolutePath

        ' bez logowania jest wysy³ka do celów "BING search by pic"
        If command.StartsWith("/bufpic/") Then
            _lastNetAccess.Zapisz("(websearch)", "bufpic")
            Return Await SendMarkedPicDataFromBuff(Nothing, command.Replace("/bufpic/", ""), response)
        End If

        Dim clientAddress As IPAddress = request.LocalEndPoint.Address
        Dim queryString As NameValueCollection = request.QueryString

        _lastAccess.remoteHostName = ""
        _lastAccess.kiedy = Date.Now
        _lastAccess.IPaddr = clientAddress.ToString

        Dim tmp As String = queryString.Item("guid")
        Dim loginGuid As Guid
        If String.IsNullOrWhiteSpace(tmp) Then Return "Who you are?"
        Try
            loginGuid = New Guid(tmp)
        Catch ex As Exception
            Return "Who you really are?"
        End Try

        tmp = queryString.Item("clientHost")
        _lastAccess.remoteHostName = tmp

        Dim oLogin As Vblib.ShareLogin = ResolveLogin(loginGuid, clientAddress, tmp)
        If oLogin Is Nothing Then Return "Sorry Winnetou"

        ' /JakasKomenda -> jakaskomenda
        command = command.Substring(1).ToLowerInvariant
        _lastNetAccess.Zapisz(oLogin.displayName, command)

        Select Case command

            ' done 2023.09.22

            Case "trylogin"
                ' input: TryLogin, guid, clientHost
                ' return: OK lub error (ju¿ wczeœniej, przed Select Case)
                Return "OK"
            Case "ver" ' wersja protokó³
                ' input: Ver, guid, clientHost
                ' return: nr wersji protokolu lub error (ju¿ wczeœniej, przed Select Case)
                Return PROTO_VERS


                ' done 2023.09.24
                ' Aœka wysy³a zdjêcie do mnie

            Case "canupload"
                ' input: CanUpload, guid, clientHost
                ' return: YES, TEMPLOCK (gdy chwilowo wstrzymane uploady), NO, lub error (ju¿ wczeœniej, przed Select Case)
                If Not CanUpload(oLogin) Then Return "NO" ' w konfiguracji kana³u
                If Vblib.GetSettingsBool("uiUploadBlocked") Then Return "TEMPLOCK" ' chwilowa blokada
                Return "YES"
            Case "putpicmeta"
                ' input: PutPicMeta, guid, clientHost; POST: JSON z OnePic
                ' return: OK picguid, NOWAY (kana³ nie ma zgody na upload), TEMPLOCK (chwilowo wstrzymane), BADDATA (b³¹d wczytywania JSON) lub error (ju¿ wczeœniej, przed Select Case)
                Return PrzyjmijPlikMeta(oLogin, request)
            Case "putpicdata"
                ' input: PutPicMeta, guid, clientHost, picguid; POST: file data
                ' return: OK, NOWAY (kana³ nie ma zgody na upload), TEMPLOCK (chwilowo wstrzymane), BADPICGUID (nie ma takiego OnePic w buforze), BADDATA (b³¹d wczytywania JSON) lub error (ju¿ wczeœniej, przed Select Case)
                Return Await PrzyjmijPlikData(oLogin, request)


                ' done: 2023.09.26
                ' Aœka wysy³a do mnie opis do zdjêcia ode mnie

            Case "uploadpicdesc"
                ' input: UploadPicDesc, guid, clientHost, picid; POST: JSON z OneDescription
                ' return: OK, BADDATA (b³¹d wczytywania JSON) lub error (ju¿ wczeœniej, przed Select Case)
                Return GotDescription(oLogin, queryString.Item("serno"), request)


                ' *TODO* done: 2023.09.29
                ' Aœka pyta mnie o opisy do jej zdjêæ

            Case "querypicdescqueue"
                ' input: guid, clientHost
                ' return: JSON z dumpem wszystkich ShareDescription z kolejki
                Return SendDescriptionQueue(oLogin)
            Case "confirmpicdescqueue"
                ' input: guid, clientHost, lastPicId
                ' return: JSON z dumpem wszystkich z kolejki
                Return ConfirmDescriptionQueue(oLogin, queryString.Item("picids"))

            Case "currentpiclistforme"
                ' input: guid, clientHost
                ' return: JSON z dumpem wszystkich z kolejki
                Return SendMarkedPicsListFromBuff(oLogin)
            Case "currentpicdata"
                ' input: guid, clientHost, fname = InBuffer
                Return Await SendMarkedPicDataFromBuff(oLogin, queryString.Item("fname"), response)

            Case "getnewpicslist"
                Return GetNewPicsList(oLogin, queryString.Item("sinceId"))
            Case "GetPic"
                Return "Not yet"
                'Case "putpic"
                '    Return PrzyjmijPlik(oLogin, request)

            Case "purgegetstatus"
                Return If(oLogin.maintainPurge, "YES maintains purge file", "NOT using purge file")
            Case "purgegetlist"
                Dim purgeFile As String = IO.Path.Combine(_dataFolder, $"purge.{oLogin.login.ToString}.txt")
                If Not IO.File.Exists(purgeFile) Then Return ""
                Dim purgeEntries As String = IO.File.ReadAllText(purgeFile)
                Return purgeEntries
            Case "purgeresetlist"
                Dim purgeFile As String = IO.Path.Combine(_dataFolder, $"purge.{oLogin.login.ToString}.txt")
                If Not IO.File.Exists(purgeFile) Then Return "OK"

                Dim clearToDate As String = queryString.Item("ackTill")
                Dim purgeEntries As String() = IO.File.ReadAllLines(purgeFile)
                Dim iPurged As Integer = 0

                ' Date.Now.ToString("yyyyMMdd.HHmm")
                IO.File.Delete(purgeFile & ".bak")
                IO.File.Move(purgeFile, purgeFile & ".bak")
                For Each linia As String In purgeEntries
                    If linia > clearToDate Then
                        IO.File.AppendAllText(purgeFile, linia & vbCrLf)
                    Else
                        iPurged += 1
                    End If
                Next
                Return $"OK, purged {iPurged} entries out of {purgeEntries.Count}"


            Case "webbuf"
                If Not Vblib.GetSettingsBool("uiAsWebServer") Then Return "Web interface is disabled"
                AppendLog(oLogin, "webbuf")
                Return WebPageForLogin(oLogin, RequestAllowsPL(request))
            Case "webthumb"
                If Not Vblib.GetSettingsBool("uiAsWebServer") Then Return "Web interface is disabled"
                Await WyslijFotke(oLogin, queryString.Item("serno"), response, False)
                Return "NOTXTRESPONSE"
            Case "webpic"
                If Not Vblib.GetSettingsBool("uiAsWebServer") Then Return "Web interface is disabled"
                AppendLog(oLogin, $"webpic #{queryString.Item("serno")}")
                Await WyslijFotke(oLogin, queryString.Item("serno"), response, True)
                Return "NOTXTRESPONSE"
            Case Else
                Return "PROTOERROR, here is " & PROTO_VERS
        End Select

    End Function


    Private Function RequestAllowsPL(request As HttpListenerRequest) As Boolean
        If request.UserLanguages Is Nothing Then Return True
        For Each lang In request.UserLanguages
            If lang.StartsWithCI("pl") Then Return True
        Next

        Return False
    End Function


#Region "Jako strony WWW"
    Private Function WebPageForLogin(oLogin As ShareLogin, usePL As Boolean) As String
        Dim sTitle As String = $"Current pictures for {oLogin.login.ToString}"
        Dim sHdr1 As String = If(usePL, "Bie¿¹ce zdjêcia dla Ciebie", "Current pictures for you")

        Dim sPage As String = $"<html><head><title>{sTitle}</title><meta charset='utf-8' /></head>" & vbCrLf & $"<body><h1>{sHdr1}</h1>"

        sPage &= "<table>"

        Dim byloCos As Boolean = False

        Dim guard As Integer = Vblib.GetSettingsInt("uiWebBuffPicLimit")
        For Each oPic As Vblib.OnePic In _buffer.GetList.Where(Function(x) Not x.sharingLockSharing AndAlso x.PeerIsForLogin(oLogin)).Take(guard)
            sPage &= "<tr>"
            sPage &= $"<td><a href='webpic?guid={oLogin.login}&serno={oPic.serno}'><img src='webthumb?guid={oLogin.login}&serno={oPic.serno}'></a>"
            sPage &= "<td>"

            If Vblib.GetSettingsBool("uiAsWebPrintFilename") Then
                sPage &= oPic.sSuggestedFilename & "<br />"
            End If

            If Vblib.GetSettingsBool("uiAsWebPrintSerno") Then
                sPage &= "#serno: " & oPic.serno & "<br />"
            End If

            Try
                If Vblib.GetSettingsBool("uiAsWebPrintDates") Then
                    sPage &= $"Date: {oPic.GetMinDate.ToExifString} .. {oPic.GetMaxDate.ToExifString}" & "<br />"
                End If
            Catch ex As Exception

            End Try

            If Vblib.GetSettingsBool("uiAsWebPrintKwd") Then
                sPage &= oPic.sumOfKwds & "<br />"
            End If

            If Vblib.GetSettingsBool("uiAsWebPrintDescr") Then
                sPage &= oPic.sumOfDescr & "<br />"
            End If

            If Vblib.GetSettingsBool("uiAsWebPrintGeo") Then
                Dim geo As pkar.BasicGeoposWithRadius = oPic.GetGeoTag
                If geo IsNot Nothing Then
                    sPage &= $"<a href='{geo.ToOSMLink}'>mapa</a>"
                End If
            End If

            sPage &= "</tr>" & vbCrLf
            byloCos = True
            guard -= 1
            If guard < 0 Then Exit For
        Next

        sPage &= "</table>"

        If guard < 0 Then
            sPage &= If(usePL, "<p>(zdjêæ jest wiêcej, ale ogranicza mnie limit)</p>", "<p>(more pictures available, but web interface is limited )</p>")
        End If

        If Not byloCos Then
            sPage &= If(usePL, "<p>W buforze nie ma zdjêæ pasuj¹cych do Twojego loginu</p>", "<p>No picture in buffer for you</p>")
        End If

        sPage &= "<hr/><p><small>Page generated by <a href=''>PicSort</a></small></p>"
        sPage &= "</body></html>"

        Return sPage

    End Function

    Private Async Function WyslijFotke(oLogin As ShareLogin, serno As String, response As HttpListenerResponse, fullPic As Boolean) As Task

        Dim oPic As Vblib.OnePic = _buffer.GetList.FirstOrDefault(Function(x) x.serno = serno)
        If oPic Is Nothing Then Return
        If oPic.sharingLockSharing Then Return
        If Not oPic.PeerIsForLogin(oLogin) Then Return

        If fullPic Then
            Await PutPipelinedPicToResponse(oPic, oLogin, response)
            Return
        End If

        ' tylko thumba wysy³amy
        Dim fname As String = oPic.InBufferPathName & ".PicSortThumb.jpg"
        If Not File.Exists(fname) Then Return

        Try
            Dim strumyk As FileStream = File.OpenRead(fname)
            response.ContentLength64 = strumyk.Length
            Using output As System.IO.Stream = response.OutputStream
                Await strumyk.CopyToAsync(output)
                'You must close the output stream.
                output.Close()
            End Using
        Catch ex As Exception
            ' bo CANCEL po tamtej stronie mo¿e byæ, i wtedy tu "The specified network name is no longer available."
        End Try
    End Function
#End Region

#Region "odsy³anie zdjêæ z bufora"
    Private Function SendMarkedPicsListFromBuff(oLogin As ShareLogin) As String

        Dim sRet As String = ""
        For Each oPic As Vblib.OnePic In _buffer.GetList.Where(Function(x) Not x.sharingLockSharing AndAlso x.PeerIsForLogin(oLogin))
            If sRet <> "" Then sRet &= ","
            ' usuwamy informacje które tam sie nie powinny znaleŸæ
            sRet &= oPic.StrippedForSharing.DumpAsJSON
        Next

        ' If sRet = "" Then Return "No pics marked for your login"
        Return "[" & sRet & "]"

    End Function

    Private Async Function SendMarkedPicDataFromBuff(oLogin As ShareLogin, fname As String, response As HttpListenerResponse) As Task(Of String)
        fname = fname.Replace("%20", " ") ' byæ mo¿e te¿ trzeba wiêcej 
        Dim oPic As Vblib.OnePic = _buffer.GetList.Find(Function(x) x.InBufferPathName.EndsWithCI(fname))
        If oPic Is Nothing Then Return "ERROR: no such pic"

        If oLogin IsNot Nothing Then
            If oPic.sharingLockSharing Then Return "ERROR: file is excluded from sharing"
            If Not oPic.IsCloudPublishMentioned("L:" & oLogin.login.ToString) Then Return "ERROR: pic not marked"
        Else
            ' spróbuj znaleŸæ oLogin defaultowy do search by pic, ewentualnie przyjmij z empty pipeline
            oLogin = _loginy.Find(Function(x) x.displayName = "ForPicSearch")
            If oLogin Is Nothing Then
                oLogin = New ShareLogin With {.processing = ""}
            End If
        End If

        Return Await PutPipelinedPicToResponse(oPic, oLogin, response)
        'oPic.ResetPipeline()
        'Dim ret As String = Await oPic.RunPipeline(oLogin.processing, _postProcs, False)
        'If ret <> "" Then Return "ERROR: " & ret
        'oPic._PipelineOutput.Seek(0, SeekOrigin.Begin)

        'response.ContentLength64 = oPic._PipelineOutput.Length
        'response.ContentType = "image/jpeg"
        ''response.OutputStream.Seek(0, SeekOrigin.Begin) ' ten stream nie ma seek
        'oPic._PipelineOutput.CopyTo(response.OutputStream)
        ''You must close the output stream.
        'response.OutputStream.Close()

        'oPic.ResetPipeline() ' zwolnienie streamów, readerów, i tak dalej

        '' ¿e response ju¿ jest, binarny, wiêc nie wysy³amy tekstu
        'Return "NOTXTRESPONSE"
    End Function

    Private Async Function PutPipelinedPicToResponse(oPic As Vblib.OnePic, oLogin As ShareLogin, response As HttpListenerResponse) As Task(Of String)
        oPic.ResetPipeline()
        Dim ret As String = Await oPic.RunPipeline(oLogin.processing, _postProcs, False)
        If ret <> "" Then Return "ERROR: " & ret
        oPic._PipelineOutput.Seek(0, SeekOrigin.Begin)

        response.ContentLength64 = oPic._PipelineOutput.Length
        response.ContentType = "image/jpeg"
        'response.OutputStream.Seek(0, SeekOrigin.Begin) ' ten stream nie ma seek
        oPic._PipelineOutput.CopyTo(response.OutputStream)
        'You must close the output stream.
        response.OutputStream.Close()

        oPic.ResetPipeline() ' zwolnienie streamów, readerów, i tak dalej

        ' ¿e response ju¿ jest, binarny, wiêc nie wysy³amy tekstu
        Return "NOTXTRESPONSE"
    End Function


#End Region


#Region "odsy³anie skolejkowanych komentarzy"

    ' obs³uga kolejki ShareDescOut

    ''' <summary>
    ''' Odes³anie Listy OneDescription dla podanego loginu z ShareDescOut (parameter New())
    ''' </summary>
    Private Function SendDescriptionQueue(oLogin As ShareLogin) As String

        ' szukamy tych dla podanego GUID (obojêtnie czy login czy server)
        Dim peerGuid As String = oLogin.login.ToString
        Dim ret As String = ""
        For Each oDesc As Vblib.ShareDescription In _shareDescOut.Where(Function(x) x.descr.PeerGuid.EndsWithCI(peerGuid))
            If ret <> "" Then ret &= ","
            ret &= oDesc.DumpAsJSON(True)
        Next

        Return "[" & ret & "]"
    End Function

    ''' <summary>
    ''' Potwierdzenie przyjêcia komentarzy a¿ do lastpicid - mo¿na je skasowaæ z kolejki OUT (jakbym akurat jednoczeœnie opisywa³)
    ''' </summary>
    Private Function ConfirmDescriptionQueue(oLogin As ShareLogin, picids As String) As String
        ' szukamy tych dla podanego GUID (obojêtnie czy login czy server)
        Dim peerGuid As String = oLogin.login.ToString

        Dim aIds As String() = picids.Split("-")

        Dim iCnt As Integer = 0

        For Each id As String In aIds
            Dim iId As Integer
            If Not Integer.TryParse(id, iId) Then Continue For

            Do
                Dim oItem As Vblib.ShareDescription = _shareDescOut.Find(Function(x) x.serno = iId)
                If oItem Is Nothing Then Exit Do
                iCnt += 1
                _shareDescOut.Remove(oItem)
            Loop

        Next

        Return $"OK, deleted {iCnt} items from queue"
    End Function
#End Region

#Region "przyjmowanie opisu do zdjêcia od nas wziêtego"
    ''' <summary>
    ''' Przyjêcie ShareDescription, przetworzenie IDs i zapisanie do listy ShareDescIn (parametr New())
    ''' </summary>
    Private Function GotDescription(oLogin As ShareLogin, serno As Integer, request As HttpListenerRequest) As String
        Vblib.DumpCurrMethod()

        Dim json As String = ReadReqAsString(request)

        Dim oDesc As Vblib.ShareDescription
        Try
            oDesc = Newtonsoft.Json.JsonConvert.DeserializeObject(json, GetType(Vblib.ShareDescription))
        Catch ex As Exception
            Return "BADDATA"
        End Try

        oDesc.descr.PeerGuid &= ";L:" & oLogin.login.ToString & ":" & oDesc.serno
        oDesc.serno = serno

        _shareDescIn.Add(oDesc)
        _shareDescIn.Save(True)

        Vblib.DumpMessage("Got description for " & oDesc.serno)

        Return "OK"

    End Function
#End Region

#Region "incoming pictures (uploaded)"
    Private _nowePicki As New pkar.BaseList(Of Vblib.OnePic)("dummyfolder")   ' dopóki nie bêdzie load, albo save, getdate, itp., plik nie zostanie utworzony

    ''' <summary>
    ''' przyjêcie do bufora zdalnego OnePic (metadata only)
    ''' </summary>
    ''' <returns>tekst b³êdu, lub OK wraz z tutejszym ID pliku - uploader ma to potem wykorzystaæ</returns>
    Private Function PrzyjmijPlikMeta(oLogin As Vblib.ShareLogin, request As HttpListenerRequest) As String
        Vblib.DumpCurrMethod()

        If Not CanUpload(oLogin) Then Return "NOWAY"
        If Vblib.GetSettingsBool("uiUploadBlocked") Then Return "TEMPLOCK"

        Dim json As String = ReadReqAsString(request)

        Dim oPic As Vblib.OnePic
        Try
            oPic = _nowePicki.LoadItem(json)
        Catch ex As Exception
            Return "BADDATA"
        End Try

        ' teraz nadaj w³asny ID - GUID najlepiej, by by³o niepowtarzalne - albo, po prostu, pozycja na liœcie
        oPic.sharingFromGuid &= $"L:{oLogin.login.ToString}:{oPic.serno};" ' aktualny jest na koñcu
        oPic.serno = 0
        oPic.InBufferPathName = Guid.NewGuid.ToString
        _nowePicki.Add(oPic)

        Vblib.DumpMessage("Got metadata for " & oPic.sSuggestedFilename)

        Return "OK " & oPic.InBufferPathName

    End Function


    ''' <summary>
    ''' przyjêcie pliku, i razem z wczeœniej wstawionym OnePic, zapisanie go w IBuffer - parametrze w New()
    ''' </summary>
    ''' <returns>tekst b³êdu, lub OK wraz z tutejszym ID pliku - uploader ma to potem wykorzystaæ</returns>
    Private Async Function PrzyjmijPlikData(oLogin As Vblib.ShareLogin, request As HttpListenerRequest) As Task(Of String)
        If Not CanUpload(oLogin) Then Return "NOWAY"
        If Vblib.GetSettingsBool("uiUploadBlocked") Then Return "TEMPLOCK"

        Dim uploadGuid As String = request.QueryString.Item("picguid")
        If String.IsNullOrEmpty(uploadGuid) Then Return "NOPICGUID"
        Dim oPic As Vblib.OnePic = _nowePicki.Find(Function(x) x.InBufferPathName = uploadGuid)
        If oPic Is Nothing Then Return "BADPICGUID"

        ' ale przeciez AddFile dba o jednolistoœæ nazw :)
        'If bufor.GetList.Find(Function(x) x.sSuggestedFilename = oDesc.sSuggestedFilename) IsNot Nothing Then
        '    Dim tmpname As String = = oLogin.login.ToString.Substring(0, 8) & "-" & oDesc.sSuggestedFilename
        '    If bufor.GetList.Find(Function(x) x.sSuggestedFilename = oDesc.sSuggestedFilename) Is Nothing Then
        '        oDesc.sSuggestedFilename = tmpname
        '    Else
        '        tmpname = oLogin.login.ToString & "." & Date.Now.ToString("yyyyMMdd") & "-" & oDesc.sSuggestedFilename
        '        If bufor.GetList.Find(Function(x) x.sSuggestedFilename = oDesc.sSuggestedFilename) Is Nothing Then
        '            oDesc.sSuggestedFilename = tmpname
        '        Else
        '            Return "TOOMANYSAMEFILES"
        '        End If
        '    End If
        'End If

        Try
            Dim klon As Vblib.OnePic = oPic.Clone
            klon.oContent = request.InputStream
            Await _buffer.AddFile(klon)   ' ten oDesc jest zmieniony, o InBufferPathname, wiêc trzeba zrobiæ clone by pomin¹æ te zmiany
            _buffer.SaveData()
        Catch ex As Exception
            Return "BADDATA"
        End Try

        Vblib.DumpMessage("Got filedata for " & oPic.sSuggestedFilename)

        ' skoro siê uda³o, to mo¿emy skasowaæ OnePic z listy tymczasowej
        _nowePicki.Remove(oPic)

        Return "OK"

    End Function

#End Region

#Region "tools"
    Private Function ReadReqAsString(request As HttpListenerRequest) As String
        Using rdr As New StreamReader(request.InputStream)
            Return rdr.ReadToEnd
        End Using
    End Function


    ''' <summary>
    ''' nie tylko znalezienie Login, ale tak¿e kontrola security.
    ''' </summary>
    ''' <returns>NULL gdy nie ma usera, zablokowany, z³y adres...</returns>
    Private Function ResolveLogin(loginGuid As Guid, IPaddr As IPAddress, clntName As String) As Vblib.ShareLogin
        For Each oLogin As Vblib.ShareLogin In _loginy
            If oLogin.login = loginGuid Then

                If Not oLogin.enabled Then Return Nothing

                If Not String.IsNullOrEmpty(oLogin.allowedLogin.remoteHostName) Then
                    If Not oLogin.allowedLogin.remoteHostName.EqualsCI(clntName) Then Return Nothing
                End If
                ' *TODO* sprawdzenie adresu

                If Not String.IsNullOrWhiteSpace(clntName) Then
                    oLogin.lastLogin.remoteHostName = clntName.ToUpperInvariant
                End If

                oLogin.lastLogin.kiedy = Date.Now
                    oLogin.lastLogin.IPaddr = IPaddr.ToString

                    Return oLogin
                End If
        Next

        Return Nothing
    End Function

    Private Function GetNewPicsList(oLogin As Vblib.ShareLogin, sinceId As String) As String

        If Not _databases.IsLoaded Then
            'Return "TRYAGAIN"
            _databases.Load() ' to mog³oby pójœæ w oddzielnym thread
        End If

        Dim lista As List(Of Vblib.OnePic) = _databases.Search(oLogin, sinceId)
        If lista Is Nothing Then Return "FAIL"

        Dim ret As String = ""
        For Each oPic As Vblib.OnePic In lista
            If ret <> "" Then ret &= ","
            ret &= oPic.StrippedForSharing.DumpAsJSON(True)
        Next

        Return "[" & ret & "]"

    End Function
#End Region

    Public Function GetPic(loginGuid As Guid, picId As String) As Byte()

        Throw New NotImplementedException()
    End Function

    Public Function UploadPicDescription(loginGuid As Guid, picId As String, picData As String) As Boolean

        Throw New NotImplementedException()
    End Function

    Private Function CanUpload(oLogin As Vblib.ShareLogin) As Boolean
        Return oLogin.allowUpload
    End Function

    Public Function PutPic(loginGuid As Guid, picMetadata As String, picBytes() As Byte) As Boolean

        Throw New NotImplementedException()
    End Function
#End Region


End Class

Public Class LastNetAccess
    Private Property kiedy As Date
    Private Property kto As String
    Private Property cmd As String

    Public Sub Zapisz(kto As String, cmd As String)
        kiedy = Date.Now
        Me.kto = kto
        Me.cmd = cmd
    End Sub

    Public Function GetString() As String
        Dim datediff As TimeSpan = Date.Now - kiedy
        If datediff.TotalDays > 365 Then
            Return "No recent logins"
        Else
            Return $"Last request: {kto}:{cmd} @{(Date.Now - kiedy).ToStringDHMS} ago"
        End If

    End Function
End Class
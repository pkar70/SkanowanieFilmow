Imports System.Collections.Specialized
Imports System.IO
Imports System.Net
Imports System.Runtime.CompilerServices
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
    ''' Kiedy ostatnio co� si� komunikowa�o
    ''' </summary>
    Public Shared _lastNetAccess As Date

    Public Sub New(loginy As pkar.BaseList(Of Vblib.ShareLogin), databases As Vblib.DatabaseInterface, lastAccess As Vblib.ShareLoginData, buffer As Vblib.IBufor, shareDescIn As pkar.BaseList(Of Vblib.ShareDescription), shareDescOut As pkar.BaseList(Of Vblib.ShareDescription), postProcs As Vblib.PostProcBase(), dataFolder As String)
        _loginy = loginy
        _databases = databases
        _lastAccess = lastAccess
        _buffer = buffer
        _shareDescIn = shareDescIn
        _shareDescOut = shareDescOut
        _postProcs = postProcs
        _dataFolder = dataFolder
    End Sub

    Public Sub StartSvc()
        DumpCurrMethod()

        If _host Is Nothing Then Task.Run(Sub() InitService())
    End Sub

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

                ' ten call blokuje do pierwszego wywo�ania przez klienta
                Dim context As HttpListenerContext = _host?.GetContext()
                ' a tak reaguje na EXIT:
                ' The I/O operation has been aborted because of either a thread exit or an application request.'

                Dim request As HttpListenerRequest = context?.Request
                If request Is Nothing Then Exit Do   ' takie zabezpieczenie to tylko u�atwienie gdy jest pod debuggerem podczas wy��czania programu


                _lastNetAccess = Date.Now
                Vblib.DumpMessage("Mam request: " & request.RawUrl) ' on jest typu: /canupload?guid=xxx&clientHost=Hxxxx

                Dim response As HttpListenerResponse = context.Response
                Dim responseString As String = Await MainWork(request, response)

                If responseString <> "NOTXTRESPONSE" Then
                    Dim buffer As Byte() = System.Text.Encoding.UTF8.GetBytes(responseString)
                    response.ContentLength64 = buffer.Length
                    Dim output As System.IO.Stream = response.OutputStream
                    output.Write(buffer, 0, buffer.Length)
                    'You must close the output stream.
                    output.Close()
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

#Region "real work"

    Private Const PROTO_VERS As String = "1.0"

    ''' <summary>
    ''' returns string = response for remote command
    ''' </summary>
    Private Async Function MainWork(request As HttpListenerRequest, response As HttpListenerResponse) As Task(Of String)

        Dim command As String = request.Url.AbsolutePath
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

        Select Case command

            ' done 2023.09.22

            Case "trylogin"
                ' input: TryLogin, guid, clientHost
                ' return: OK lub error (ju� wcze�niej, przed Select Case)
                Return "OK"
            Case "ver" ' wersja protok�
                ' input: Ver, guid, clientHost
                ' return: nr wersji protokolu lub error (ju� wcze�niej, przed Select Case)
                Return PROTO_VERS


                ' done 2023.09.24
                ' A�ka wysy�a zdj�cie do mnie

            Case "canupload"
                ' input: CanUpload, guid, clientHost
                ' return: YES, TEMPLOCK (gdy chwilowo wstrzymane uploady), NO, lub error (ju� wcze�niej, przed Select Case)
                If Not CanUpload(oLogin) Then Return "NO" ' w konfiguracji kana�u
                If Vblib.GetSettingsBool("uiUploadBlocked") Then Return "TEMPLOCK" ' chwilowa blokada
                Return "YES"
            Case "putpicmeta"
                ' input: PutPicMeta, guid, clientHost; POST: JSON z OnePic
                ' return: OK picguid, NOWAY (kana� nie ma zgody na upload), TEMPLOCK (chwilowo wstrzymane), BADDATA (b��d wczytywania JSON) lub error (ju� wcze�niej, przed Select Case)
                Return PrzyjmijPlikMeta(oLogin, request)
            Case "putpicdata"
                ' input: PutPicMeta, guid, clientHost, picguid; POST: file data
                ' return: OK, NOWAY (kana� nie ma zgody na upload), TEMPLOCK (chwilowo wstrzymane), BADPICGUID (nie ma takiego OnePic w buforze), BADDATA (b��d wczytywania JSON) lub error (ju� wcze�niej, przed Select Case)
                Return Await PrzyjmijPlikData(oLogin, request)


                ' done: 2023.09.26
                ' A�ka wysy�a do mnie opis do zdj�cia ode mnie

            Case "uploadpicdesc"
                ' input: UploadPicDesc, guid, clientHost, picid; POST: JSON z OneDescription
                ' return: OK, BADDATA (b��d wczytywania JSON) lub error (ju� wcze�niej, przed Select Case)
                Return GotDescription(oLogin, queryString.Item("picid"), request)


                ' *TODO* done: 2023.09.29
                ' A�ka pyta mnie o opisy do jej zdj��

            Case "querypicdescqueue"
                ' input: guid, clientHost
                ' return: JSON z dumpem wszystkich ShareDescription z kolejki
                Return SendDescriptionQueue(oLogin)
            Case "confirmpicdescqueue"
                ' input: guid, clientHost, lastPicId
                ' return: JSON z dumpem wszystkich z kolejki
                Return ConfirmDescriptionQueue(oLogin, queryString.Item("lastpicid"))

            Case "currentpiclistforme"
                ' input: guid, clientHost
                ' return: JSON z dumpem wszystkich z kolejki
                Return SendMarkedPicsListFromBuff(oLogin)
            Case "currentpicdata"
                ' input: guid, clientHost, fname = InBuffer
                ' return: JSON z dumpem wszystkich z kolejki
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

            Case Else
                Return "PROTOERROR, here is " & PROTO_VERS
        End Select

    End Function

#Region "odsy�anie zdj�� z bufora"
    Private Function SendMarkedPicsListFromBuff(oLogin As ShareLogin) As String

        Dim sRet As String = ""
        For Each oPic As Vblib.OnePic In _buffer.GetList.Where(Function(x) Not x.sharingLockSharing AndAlso x.IsCloudPublishMentioned("L:" & oLogin.login.ToString))
            If sRet <> "" Then sRet &= ","
            sRet &= oPic.DumpAsJSON
        Next

        ' If sRet = "" Then Return "No pics marked for your login"
        Return "[" & sRet & "]"

    End Function

    Private Async Function SendMarkedPicDataFromBuff(oLogin As ShareLogin, fname As String, response As HttpListenerResponse) As Task(Of String)

        Dim oPic As Vblib.OnePic = _buffer.GetList.Find(Function(x) x.InBufferPathName = fname)
        If oPic Is Nothing Then Return "ERROR: no such pic"
        If oPic.sharingLockSharing Then Return "ERROR: file is excluded from sharing"
        If Not oPic.IsCloudPublishMentioned("L:" & oLogin.login.ToString) Then Return "ERROR: pic not marked"

        oPic.ResetPipeline()
        Dim ret As String = Await oPic.RunPipeline(oLogin.processing, _postProcs, False)
        If ret <> "" Then Return "ERROR: " & ret

        response.ContentLength64 = oPic.oContent.Length
        response.ContentType = "image/jpeg"
        oPic.oContent.Seek(0, SeekOrigin.Begin)
        response.OutputStream.Seek(0, SeekOrigin.Begin)
        oPic.oContent.CopyTo(response.OutputStream)
        'You must close the output stream.
        response.OutputStream.Close()

        oPic.ResetPipeline() ' zwolnienie stream�w, reader�w, i tak dalej

        ' �e response ju� jest, binarny, wi�c nie wysy�amy tekstu
        Return "NOTXTRESPONSE"
    End Function

#End Region


#Region "odsy�anie skolejkowanych komentarzy"

    ' obs�uga kolejki ShareDescOut

    ''' <summary>
    ''' Odes�anie Listy OneDescription dla podanego loginu z ShareDescOut (parameter New())
    ''' </summary>
    Private Function SendDescriptionQueue(oLogin As ShareLogin) As String

        ' szukamy tych dla podanego GUID (oboj�tnie czy login czy server)
        Dim peerGuid As String = oLogin.login.ToString
        Dim ret As String = ""
        For Each oDesc As Vblib.ShareDescription In _shareDescOut.Where(Function(x) x.descr.PeerGuid.EndsWithCI(peerGuid))
            If ret <> "" Then ret &= ","
            ret &= oDesc.DumpAsJSON(True)
        Next

        Return "[" & ret & "]"
    End Function

    ''' <summary>
    ''' Potwierdzenie przyj�cia komentarzy a� do lastpicid - mo�na je skasowa� z kolejki OUT (jakbym akurat jednocze�nie opisywa�)
    ''' </summary>
    Private Function ConfirmDescriptionQueue(oLogin As ShareLogin, lastpicid As String) As String
        ' szukamy tych dla podanego GUID (oboj�tnie czy login czy server)
        Dim peerGuid As String = oLogin.login.ToString

        Dim iCnt As Integer = 0
        Do
            Dim oItem As Vblib.ShareDescription = _shareDescOut.Find(Function(x) x.descr.PeerGuid.EndsWithCI(peerGuid))
            If oItem Is Nothing Then Exit Do
            iCnt += 1
            _shareDescOut.Remove(oItem)
            If oItem.picid = lastpicid Then Exit Do
        Loop

        Return $"OK, deleted {iCnt} items from queue"
    End Function
#End Region

#Region "przyjmowanie opisu do zdj�cia od nas wzi�tego"
    ''' <summary>
    ''' Przyj�cie OneDescription, opakowanie go w ShareDescription i zapisanie do listy ShareDescIn (parametr New())
    ''' </summary>
    Private Function GotDescription(oLogin As ShareLogin, picid As String, request As HttpListenerRequest) As String
        Vblib.DumpCurrMethod()

        Dim json As String = ReadReqAsString(request)

        Dim oDesc As Vblib.OneDescription
        Try
            oDesc = Newtonsoft.Json.JsonConvert.DeserializeObject(json, GetType(Vblib.OneDescription))
        Catch ex As Exception
            Return "BADDATA"
        End Try

        oDesc.PeerGuid = "L:" & oLogin.login.ToString

        Dim oNew As New Vblib.ShareDescription
        oNew.descr = oDesc
        oNew.picid = request.QueryString.Item("picid")

        _shareDescIn.Add(oNew)
        _shareDescIn.Save(True)

        Vblib.DumpMessage("Got description for " & oNew.picid)

        Return "OK"

    End Function
#End Region

#Region "incoming pictures (uploaded)"
    Private _nowePicki As New pkar.BaseList(Of Vblib.OnePic)("dummyfolder")   ' dop�ki nie b�dzie load, albo save, getdate, itp., plik nie zostanie utworzony

    ''' <summary>
    ''' przyj�cie do bufora zdalnego OnePic (metadata only)
    ''' </summary>
    ''' <returns>tekst b��du, lub OK wraz z tutejszym ID pliku - uploader ma to potem wykorzysta�</returns>
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

        ' teraz nadaj w�asny ID - GUID najlepiej, by by�o niepowtarzalne - albo, po prostu, pozycja na li�cie
        oPic.sharingFromGuid &= $"L:{oLogin.login.ToString};" ' aktualny jest na ko�cu
        oPic.InBufferPathName = Guid.NewGuid.ToString
        _nowePicki.Add(oPic)

        Vblib.DumpMessage("Got metadata for " & oPic.sSuggestedFilename)

        Return "OK " & oPic.InBufferPathName

    End Function


    ''' <summary>
    ''' przyj�cie pliku, i razem z wcze�niej wstawionym OnePic, zapisanie go w IBuffer - parametrze w New()
    ''' </summary>
    ''' <returns>tekst b��du, lub OK wraz z tutejszym ID pliku - uploader ma to potem wykorzysta�</returns>
    Private Async Function PrzyjmijPlikData(oLogin As Vblib.ShareLogin, request As HttpListenerRequest) As Task(Of String)
        If Not CanUpload(oLogin) Then Return "NOWAY"
        If Vblib.GetSettingsBool("uiUploadBlocked") Then Return "TEMPLOCK"

        Dim uploadGuid As String = request.QueryString.Item("picguid")
        If String.IsNullOrEmpty(uploadGuid) Then Return "NOPICGUID"
        Dim oPic As Vblib.OnePic = _nowePicki.Find(Function(x) x.InBufferPathName = uploadGuid)
        If oPic Is Nothing Then Return "BADPICGUID"

        ' ale przeciez AddFile dba o jednolisto�� nazw :)
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
            Await _buffer.AddFile(klon)   ' ten oDesc jest zmieniony, o InBufferPathname, wi�c trzeba zrobi� clone by pomin�� te zmiany
            _buffer.SaveData()
        Catch ex As Exception
            Return "BADDATA"
        End Try

        Vblib.DumpMessage("Got filedata for " & oPic.sSuggestedFilename)

        ' skoro si� uda�o, to mo�emy skasowa� OnePic z listy tymczasowej
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
    ''' nie tylko znalezienie Login, ale tak�e kontrola security.
    ''' </summary>
    ''' <returns>NULL gdy nie ma usera, zablokowany, z�y adres...</returns>
    Private Function ResolveLogin(loginGuid As Guid, IPaddr As IPAddress, clntName As String) As Vblib.ShareLogin
        For Each oLogin As Vblib.ShareLogin In _loginy
            If oLogin.login = loginGuid Then

                If Not oLogin.enabled Then Return Nothing

                If Not String.IsNullOrEmpty(oLogin.allowedLogin.remoteHostName) Then
                    If Not oLogin.allowedLogin.remoteHostName.EqualsCI(clntName) Then Return Nothing
                End If
                ' *TODO* sprawdzenie adresu

                oLogin.lastLogin.remoteHostName = clntName.ToUpperInvariant
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
            _databases.Load() ' to mog�oby p�j�� w oddzielnym thread
        End If

        Dim lista As List(Of Vblib.OnePic) = _databases.Search(oLogin, sinceId)
        If lista Is Nothing Then Return "FAIL"

        Dim ret As String = ""
        For Each oPic As Vblib.OnePic In lista
            If ret <> "" Then ret &= ","
            ret &= oPic.DumpAsJSON(True)
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

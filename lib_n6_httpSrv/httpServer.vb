Imports System.Collections.Specialized
Imports System.IO
Imports System.Net
Imports System.Net.Http
Imports System.Text.RegularExpressions
Imports Microsoft.Rest.TransientFaultHandling
Imports Vblib

Public Class ServerWrapper

    Private _host As HttpListener
    Private Shared _loginy As pkar.BaseList(Of Vblib.ShareLogin)
    Private Shared _databases As Vblib.DatabaseInterface
    Private Shared _lastAccess As Vblib.ShareLoginData
    Private Shared _buffer As Vblib.IBufor
    Private Shared _shareDesc As pkar.BaseList(Of Vblib.OneDescription)

    Public Sub New(loginy As pkar.BaseList(Of Vblib.ShareLogin), databases As Vblib.DatabaseInterface, lastAccess As Vblib.ShareLoginData, buffer As Vblib.IBufor, shareDesc As pkar.BaseList(Of Vblib.OneDescription))
        _loginy = loginy
        _databases = databases
        _lastAccess = lastAccess
        _buffer = buffer
        _shareDesc = shareDesc
    End Sub

    Public Sub StartSvc()
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

    Private Async Sub InitService()

        If Not TryStart() Then Return
        ' netsh http add urlacl url=http://+:20563/ user=

        Do
            Try
                If _host Is Nothing Then Exit Do

                ' ten call blokuje do pierwszego wywo³ania przez klienta
                Dim context As HttpListenerContext = _host?.GetContext()
                ' a tak reaguje na EXIT:
                ' The I/O operation has been aborted because of either a thread exit or an application request.'

                Dim request As HttpListenerRequest = context?.Request
                If request Is Nothing Then Exit Do   ' takie zabezpieczenie to tylko u³atwienie gdy jest pod debuggerem podczas wy³¹czania programu

                Vblib.DumpMessage("Mam request: " & request.RawUrl) ' on jest typu: /canupload?guid=xxx&clientHost=Hxxxx

                Dim responseString As String = Await MainWork(request)

                Dim response As HttpListenerResponse = context.Response
                Dim buffer As Byte() = System.Text.Encoding.UTF8.GetBytes(responseString)
                response.ContentLength64 = buffer.Length
                Dim output As System.IO.Stream = response.OutputStream
                output.Write(buffer, 0, buffer.Length)
                'You must close the output stream.
                output.Close()
            Catch ex As Exception

            End Try
        Loop

    End Sub



    Public Sub StopSvc()
        If _host Is Nothing Then Return
        _host.Stop()
        _host.Close()
        _host = Nothing
    End Sub

#Region "real work"

    Private Const PROTO_VERS As String = "1.0"

    Private Async Function MainWork(request As HttpListenerRequest) As Task(Of String)

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
                ' return: OK lub error (ju¿ wczeœniej, przed Select Case)
                Return "OK"
            Case "ver" ' wersja protokó³
                ' input: Ver, guid, clientHost
                ' return: nr wersji protokolu lub error (ju¿ wczeœniej, przed Select Case)
                Return PROTO_VERS

                ' done 2023.09.24

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

            Case "uploadpicdesc"
                ' input: UploadPicDesc, guid, clientHost, picid; POST: JSON z OneDescription
                ' return: OK, BADDATA (b³¹d wczytywania JSON) lub error (ju¿ wczeœniej, przed Select Case)
                Return GotDescription(oLogin, queryString.Item("picid"), request)

            Case "getnewpicslist"
                Return GetNewPicsList(oLogin, queryString.Item("sinceId"))
            Case "GetPic"
                Return "Not yet"
            Case "UploadPicDescription"
                Return "Not yet"
                'Case "putpic"
                '    Return PrzyjmijPlik(oLogin, request)
            Case Else
                Return "PROTOERROR, here is " & PROTO_VERS
        End Select

    End Function

    Private Function GotDescription(oLogin As ShareLogin, picid As String, request As HttpListenerRequest) As String
        Vblib.DumpCurrMethod()

        Dim json As String = ReadReqAsString(request)

        Dim oDesc As Vblib.OneDescription
        Try
            oDesc = _shareDesc.LoadItem(json)
        Catch ex As Exception
            Return "BADDATA"
        End Try

        oDesc.ShareLoginGuid = oLogin.login.ToString

        Dim oNew As New Vblib.ShareDescription
        oNew.descr = oDesc
        oNew.picid = request.QueryString.Item("picid")

        _shareDesc.Add(oDesc)
        _shareDesc.Save(True)

        Vblib.DumpMessage("Got description for " & oNew.picid)

        Return "OK"

    End Function

#Region "incoming pictures (uploaded)"
    Private _nowePicki As New pkar.BaseList(Of Vblib.OnePic)("dummy")   ' dopóki nie bêdzie load, albo save, getdate, itp., plik nie zostanie utworzony

    ''' <summary>
    ''' przyjêcie do w³asnego bufora oDesc
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
        oPic.sharingFromChannel &= oLogin.login.ToString & ";" ' aktualny jest na koñcu
        oPic.InBufferPathName = Guid.NewGuid.ToString
        _nowePicki.Add(oPic)

        Vblib.DumpMessage("Got metadata for " & oPic.sSuggestedFilename)

        Return "OK " & oPic.InBufferPathName

    End Function


    ''' <summary>
    ''' przyjêcie pliku, korzystaj¹c z bufora oDesc, i zapisanie go w odpowiednim miejscu
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
    Private Function ReadReqAsString(request As HttpListenerRequest) As String
        Using rdr As New StreamReader(request.InputStream)
            Return rdr.ReadToEnd
        End Using
    End Function

#If TRYMULTIPART Then
    ' https://stackoverflow.com/questions/8466703/httplistener-and-file-upload

    Private Shared Function GetBoundary(contType As String)
        Return "--" & contType.Split(";")(1).Split("=")(1)
    End Function

    Private Shared Function IndexOf(buffer As Byte(), len As Int32, boundaryBytes As Byte()) As Integer
        For i As Integer = 0 To len - boundaryBytes.Length
            Dim match As Boolean = True
            For j As Integer = 0 To boundaryBytes.Length - 1
                match = buffer(i + j) = boundaryBytes(j)
                If Not match Then Exit For
            Next
            If match Then Return i
        Next
        Return -1
    End Function


    Private Function PrzyjmijPlik(oLogin As ShareLogin, request As HttpListenerRequest) As String
        If Not CanUpload(oLogin) Then Return "NOWAY"
        If Vblib.GetSettingsBool("uiUploadBlocked") Then Return "TEMPLOCK"

        ' Encoding enc, String boundary, Stream input
        ' context.Request.ContentEncoding, GetBoundary(context.Request.ContentType), context.Request.InputStream

        Dim enc As Text.Encoding = request.ContentEncoding
        Dim boundary As String = GetBoundary(request.ContentType)
        Dim input As IO.Stream = request.InputStream

        Dim boundaryBytes As Byte() = enc.GetBytes(boundary)
        Dim boundaryLen As Integer = boundaryBytes.Length

        Using output As New IO.FileStream("data", IO.FileMode.Create, IO.FileAccess.Write)

            Dim buffer As Byte() = New Byte(1024) {}
            Dim len As Integer = input.Read(buffer, 0, 1024)
            Dim startPos As Integer = -1

            '// Find start boundary
            While True

                If len = 0 Then Throw New Exception("Start Boundaray Not Found")

                startPos = IndexOf(buffer, len, boundaryBytes)
                If startPos >= 0 Then
                    Exit While
                Else
                    Array.Copy(buffer, len - boundaryLen, buffer, 0, boundaryLen)
                    len = input.Read(buffer, boundaryLen, 1024 - boundaryLen)
                End If
            End While

            '// Skip four lines (Boundary, Content-Disposition, Content-Type, And a blank)
            For i As Integer = 0 To 3
                While True

                    If len = 0 Then Throw New Exception("Preamble not Found.")

                    startPos = Array.IndexOf(buffer, enc.GetBytes("\n")(0), startPos)
                    If startPos >= 0 Then
                        startPos += 1
                        Exit While
                    End If
                    len = input.Read(buffer, 0, 1024)
                End While
            Next

            Array.Copy(buffer, startPos, buffer, 0, len - startPos)
            len -= startPos

            While (True)
                Dim endPos As Int32 = IndexOf(buffer, len, boundaryBytes)
                If endPos >= 0 Then

                    If endPos > 0 Then output.Write(buffer, 0, endPos - 2)
                    Exit While
                ElseIf len <= boundaryLen Then
                    Throw New Exception("End Boundaray Not Found")
                Else
                    output.Write(buffer, 0, len - boundaryLen)
                    Array.Copy(buffer, len - boundaryLen, buffer, 0, boundaryLen)
                    len = input.Read(buffer, boundaryLen, 1024 - boundaryLen) + boundaryLen
                End If
            End While
        End Using


        Return "OK"

    End Function
#End If

    ''' <summary>
    ''' nie tylko znalezienie Login, ale tak¿e kontrola security.
    ''' </summary>
    ''' <returns>NULL gdy nie ma usera, zablokowany, z³y adres...</returns>
    Private Function ResolveLogin(loginGuid As Guid, IPaddr As IPAddress, clntName As String) As Vblib.ShareLogin
        For Each oLogin As Vblib.ShareLogin In _loginy.GetList
            If oLogin.login = loginGuid Then

                If Not oLogin.enabled Then Return Nothing

                If Not String.IsNullOrEmpty(oLogin.allowedLogin.remoteHostName) Then
                    If oLogin.allowedLogin.remoteHostName.ToLowerInvariant <> clntName.ToLowerInvariant Then Return Nothing
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
            _databases.Load() ' to mog³oby pójœæ w oddzielnym thread
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

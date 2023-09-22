Imports System.Collections.Specialized
Imports System.Net

Public Class ServerWrapper

    Private _host As HttpListener
    Private Shared _loginy As pkar.BaseList(Of Vblib.ShareLogin)
    Private Shared _databases As Vblib.DatabaseInterface
    Private Shared _lastAccess As Vblib.ShareLoginData

    Public Sub New(loginy As pkar.BaseList(Of Vblib.ShareLogin), databases As Vblib.DatabaseInterface, lastAccess As Vblib.ShareLoginData)
        _loginy = loginy
        _databases = databases
        _lastAccess = lastAccess
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

                Dim responseString As String = Await MainWork(request.Url.AbsolutePath, request.LocalEndPoint.Address, request.QueryString)

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

    Private Async Function MainWork(command As String, clientAddress As IPAddress, queryString As NameValueCollection) As Task(Of String)

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
            Case "trylogin"
                Return "OK"
            Case "getnewpicslist"
                Return GetNewPicsList(oLogin, queryString.Item("sinceId"))
            Case "GetPic"
                Return "Not yet"
            Case "UploadPicDescription"
                Return "Not yet"
            Case "canupload"
                Return If(CanUpload(oLogin), "YES", "NO")
            Case "PutPic"
                Return "Not yet"
            Case "ver" ' wersja protokó³
                Return PROTO_VERS
            Case Else
                Return "PROTOERROR, here is " & PROTO_VERS
        End Select

    End Function

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

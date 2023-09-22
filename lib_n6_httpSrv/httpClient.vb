


Imports System.Net.Http
Imports Microsoft.Rest

Public Class httpKlient

    Public Shared Async Function TryConnect(oServer As Vblib.ShareServer) As Task(Of String)
        ' połączenie Version oraz CanUpload
        If Not EnsureClient() Then Return "FAIL"

        Dim ret As String = ""
        Try
            Dim retVer As String = Await _clientQuick.GetStringAsync(GetUri(oServer, "ver"))
            Dim retAllow As String = Await _clientQuick.GetStringAsync(GetUri(oServer, "canupload"))

            ret = $"OK, server version {retVer}, CanUpload={retAllow}"

        Catch ex As Exception
            ret = "ERROR"
        End Try

        _inuse = False  ' =true jest w EnsureClient
        Return ret

    End Function

    Private Shared _clientQuick As HttpClient   ' 2 sekundy timeout
    Private Shared _clientSlow As HttpClient    ' 60 sekund timeout
    Private Shared _inuse As Boolean

    Private Shared Function EnsureClient() As Boolean
        If _inuse Then Return False
        If _clientQuick Is Nothing Or _clientSlow Is Nothing Then
            _clientQuick = New HttpClient
            _clientQuick.DefaultRequestHeaders.UserAgent.Clear()
            _clientQuick.DefaultRequestHeaders.UserAgent.ParseAdd("PicSort")
            _clientQuick.Timeout = TimeSpan.FromSeconds(2)

            _clientSlow = New HttpClient
            _clientSlow.DefaultRequestHeaders.UserAgent.Clear()
            _clientSlow.DefaultRequestHeaders.UserAgent.ParseAdd("PicSort")
            _clientSlow.Timeout = TimeSpan.FromSeconds(60)
        End If

        _inuse = True
        Return True
    End Function

    Private Shared Function GetUri(oServer As Vblib.ShareServer, command As String) As Uri
        ' dla: trylogin, CanUpload, ver
        ' reszta ma dodatkowe parametry
        ' http://server:20563/cmd?guid=xxxx&clientHost=xxx

        Dim sUri As String = $"http://{oServer.serverAddress}:20563/{command}?guid={oServer.login}&clientHost={Environment.MachineName}"
        Vblib.DumpMessage("Uri: " & sUri)
        Return New Uri(sUri)

    End Function



    'Case "trylogin"
    'Return "OK"
    'Case "getnewpicslist"
    'Return GetNewPicsList(oLogin, queryString.Item("sinceId"))
    'Case "GetPic"
    'Return "Not yet"
    'Case "UploadPicDescription"
    'Return "Not yet"
    'Case "CanUpload"
    'Return If(CanUpload(oLogin), "YES", "NO")
    'Case "PutPic"
    'Return "Not yet"
    'Case "ver" ' wersja protokół
    'Return PROTO_VERS

End Class

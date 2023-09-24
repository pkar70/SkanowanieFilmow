


Imports System.IO
Imports System.Net.Http
Imports System.Net.Http.Json
Imports Microsoft
Imports Microsoft.Rest
Imports Microsoft.SqlServer
Imports Vblib

Public Class httpKlient

#Region "handshaking"

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

    Public Shared Async Function CanUpload(oServer As Vblib.ShareServer) As Task(Of String)
        ' połączenie Version oraz CanUpload
        If Not EnsureClient() Then Return "FAIL"

        Dim ret As String = ""
        Try
            ret = Await _clientQuick.GetStringAsync(GetUri(oServer, "canupload"))
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

    Private Shared Function GetUri(oServer As Vblib.ShareServer, command As String, Optional addit As String = "") As Uri
        ' dla: trylogin, CanUpload, ver
        ' reszta ma dodatkowe parametry
        ' http://server:20563/cmd?guid=xxxx&clientHost=xxx

        Dim sUri As String = $"http://{oServer.serverAddress}:20563/{command}?guid={oServer.login}&clientHost={Environment.MachineName}"
        If addit <> "" Then
            If Not addit.StartsWith("&") Then sUri &= "&"
            sUri &= addit
        End If

        Vblib.DumpMessage("Uri: " & sUri)
        Return New Uri(sUri)

    End Function

#End Region

#Region "uploading pics"

    Public Shared Async Function UploadPic(oServer As Vblib.ShareServer, oPic As Vblib.OnePic) As Task(Of String)
        ' tak się zdarzyć nie powinno, bo pipeline powinien być uruchomiony "poziom wyżej"
        If oPic._PipelineOutput Is Nothing AndAlso oPic._PipelineInput Is Nothing Then Return "NO INPUT FILE"

        If Not EnsureClient() Then Return "FAIL EnsureClient"

        Try ' dla Finally

            Try
                If "YES" <> Await _clientQuick.GetStringAsync(GetUri(oServer, "canupload")) Then Return "CANNOT"
            Catch ex As Exception
                Return "FAIL GetCanUpload"
            End Try

            'Dim contentJson As JsonContent = JsonContent.Create(oPic, GetType(Vblib.OnePic))
            Dim tempPic As OnePic = oPic.Clone ' żeby zmiana LOCK nie spowodowała zmiany w oryginale
            tempPic.sharingLockSharing = oServer.lockForwarding
            Dim contentJson As New StringContent(tempPic.DumpAsJSON)   ' nie chce mi się uczyć kolejnego JSONa

            ' wczytanie jako memstream, bo tylko on ma ToArray
            Dim contentPic As ByteArrayContent
            Using memStream As New MemoryStream
                If oPic._PipelineOutput IsNot Nothing Then
                    oPic._PipelineOutput.CopyTo(memStream)
                Else
                    ' ale jak nie ma Out, to musi być IN, bo to już jest sprawdzone na wejściu do funkcji
                    oPic._PipelineInput.CopyTo(memStream)
                End If
                contentPic = New ByteArrayContent(memStream.ToArray)
            End Using

#If USEMULTIPART Then

            Dim content As New MultipartContent From {contentJson, contentPic}

            Try
                Dim resp = Await _clientSlow.PutAsync(GetUri(oServer, "PutPic"), content)
                If resp.IsSuccessStatusCode Then Return "OK"
                Return "FAIL notOK"
            Catch ex As Exception
                Return "FAIL Sending"
            End Try
#Else
            Dim resp As HttpResponseMessage
            Try
                resp = Await _clientQuick.PutAsync(GetUri(oServer, "putpicmeta"), contentJson)
                If Not resp.IsSuccessStatusCode Then Return "FAIL meta notOK"
            Catch ex As Exception
                Return "FAIL Sending meta"
            End Try

            ' extract GUID obrazka - czyli response.string; format: OK GUID
            Dim respStr As String = Await resp.Content.ReadAsStringAsync
            If Not respStr.StartsWith("OK ") Then Return "FAIL meta " & respStr

            Dim newPicGuid As String = respStr.Substring(3)
            Dim newPicUri As Uri = GetUri(oServer, "putpicdata", "picguid=" & newPicGuid)

            Try
                resp = Await _clientSlow.PutAsync(newPicUri, contentPic)
                If Not resp.IsSuccessStatusCode Then Return "FAIL data notOK"
            Catch ex As Exception
                Return "FAIL Sending data"
            End Try

#End If
        Finally
            _inuse = False  ' =true jest w EnsureClient
        End Try

        Return "OK"



    End Function

#End Region

#Region "uploading descriptions"

    ''' <summary>
    ''' Upload oDesc jako description dla oPic (konieczny oPic.PicGuid)
    ''' </summary>
    ''' <returns>OK lub error</returns>
    Public Shared Async Function UploadDesc(oServer As Vblib.ShareServer, oPic As Vblib.OnePic, oDesc As Vblib.OneDescription) As Task(Of String)
        If Not EnsureClient() Then Return "FAIL EnsureClient"

        Try ' dla Finally

            Dim picid As String = oPic.PicGuid
            ' musi być PicGuid jako identyfikator do czego dopisać trzeba description
            If String.IsNullOrWhiteSpace(picid) Then Return "FAIL no picguid given!"

            Dim contentJson As New StringContent(oDesc.DumpAsJSON)

            Dim resp As HttpResponseMessage
            Try
                resp = Await _clientQuick.PutAsync(GetUri(oServer, "uploadpicdesc"), contentJson)
                If Not resp.IsSuccessStatusCode Then Return "FAIL meta notOK"
            Catch ex As Exception
                Return "FAIL Sending meta"
            End Try

        Finally
            _inuse = False  ' =true jest w EnsureClient
        End Try

        Return "OK"

    End Function

#End Region

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

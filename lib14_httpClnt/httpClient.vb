
Imports System.IO
Imports System.Net.Http
Imports System.Text
Imports pkar
Imports Vblib

Public Class httpKlient

    Public Shared _machineName As String

#Region "handshaking"

    ''' <summary>
    ''' próba podłączenia
    ''' </summary>
    ''' <returns>FAIL, ERROR..., OK...</returns>
    Public Shared Async Function TryConnect(oServer As Vblib.ShareServer) As Task(Of String)
        ' połączenie Version oraz CanUpload
        If Not EnsureClient() Then Return "FAIL"

        Dim ret As String = ""
        Try
            Dim retVer As String = Await _clientQuick.GetStringAsync(GetUri(oServer, "ver"))
            Dim retAllow As String = Await _clientQuick.GetStringAsync(GetUri(oServer, "canupload"))

            ret = $"OK, server version {retVer}, CanUpload={retAllow}"

        Catch ex As Exception
            ret = "ERROR " & ex.Message
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

    ''' <summary>
    ''' stworzenie _client* jeśli ich nie ma, włącza _inUse!
    ''' </summary>
    ''' <returns>FALSE: _inUse jest TRUE, więc nie można kontynuować</returns>
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

    ''' <summary>
    ''' Stwórz URI (z wbudowanym GUID)
    ''' </summary>
    ''' <param name="oServer">z tego ważny jest tylko .serverAddress i .login (guid)</param>
    ''' <param name="command">komenda</param>
    ''' <param name="addit">dodatkowe parametry, bez &amp; ani pytajnika</param>
    Private Shared Function GetUri(oServer As Vblib.ShareServer, command As String, Optional addit As String = "") As Uri
        ' dla: trylogin, CanUpload, ver
        ' reszta ma dodatkowe parametry
        ' http://server:20563/cmd?guid=xxxx&clientHost=xxx

        Dim sUri As String = $"http://{oServer.serverAddress}:{Globs.APP_HTTP_PORT}/{command}?guid={oServer.login}&clientHost={_machineName}"
        If addit <> "" Then
            If addit.NotStartsWith("&") Then sUri &= "&"
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
            If respStr.NotStartsWith("OK ") Then Return "FAIL meta " & respStr

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
    ''' wyślij wszystkie komentarze z kolejki do podanego serwera (dla loginu nie da się wysłać :), po wysłaniu każdego descr usuwa go z kolejki
    ''' </summary>
    Public Shared Async Function UploadPicDescriptions(lista As BaseList(Of Vblib.ShareDescription), peer As ShareServer) As Task(Of Boolean)
        If lista Is Nothing Then Return True    ' nie ma nic do wysłania



        Do
            Dim jeden As ShareDescription = lista.First(Function(x) x.descr.PeerGuid = peer.login.ToString)
            If jeden Is Nothing Then Exit Do

            Dim ret As String = Await UploadDesc(peer, jeden)
            If ret <> "OK" Then Exit Do

            lista.Remove(jeden)
        Loop

        lista.Save(True)

        Return True
    End Function



    ''' <summary>
    ''' Upload oDesc jako description dla oPic (konieczny oPic.PicGuid)
    ''' </summary>
    ''' <returns>OK lub error</returns>
    Public Shared Async Function UploadDesc(oServer As ShareServer, oDesc As ShareDescription) As Task(Of String)
        If Not EnsureClient() Then Return "FAIL EnsureClient"

        Try ' dla Finally - zwolnienie zasobów

            Dim sernotmp As String = oDesc.lastPeer.Replace(";", "")
            Dim iInd As Integer = sernotmp.IndexOf(":")
            If iInd < 1 Then Return "source ser# required!"
            Dim serno As Integer = sernotmp.Substring(iInd + 1)

            Dim contentJson As New StringContent(oDesc.DumpAsJSON)

            Dim resp As HttpResponseMessage
            Try
                resp = Await _clientQuick.PutAsync(GetUri(oServer, "uploadpicdesc", "serno=" & serno), contentJson)
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

    ''' <summary>
    ''' pobiera Descriptions z serwera i wkleja je do własnej kolejki
    ''' </summary>
    ''' <param name="oServer">z jakiego serwera wczytać dane</param>
    ''' <param name="descrIn">gdzie wrzucić komentarze</param>
    ''' <returns>-1: error, 0..: liczba wczytanych komentarzy</returns>
    Public Shared Async Function GetDescripts(oServer As Vblib.ShareServer, descrIn As BaseList(Of Vblib.ShareDescription)) As Task(Of Integer)
        Dim JsonList As String = Await _clientSlow.GetStringAsync(GetUri(oServer, "querypicdescqueue"))
        If JsonList Is Nothing Then Return -1
        If JsonList = "[]" Then Return 0

        Dim listaTemp As New pkar.BaseList(Of Vblib.ShareDescription)("dummyfolder")
        If Not listaTemp.Import(JsonList) Then Return -2    ' nie da się zdeserializować

        Dim iCnt As Integer = 0
        Dim allIds As String = ""
        For Each oItem As Vblib.ShareDescription In listaTemp
            If oItem Is Nothing Then Continue For
            allIds &= "-" & oItem.serno

            ' przetworzenie na 'tutejsze' metadata (wskazanie serwera skąd przyszło) - na wejściu to identyfikator loginu (klienta) do którego ma być wysłany
            oItem.descr.PeerGuid = "S:" & oServer.login.ToString & ":" & oItem.serno

            descrIn.Add(oItem)
            iCnt += 1
        Next
        descrIn.Save(True)

        Dim retval As String = Await _clientQuick.GetStringAsync(GetUri(oServer, "confirmpicdescqueue", $"picids={allIds}"))
        ' powinno być OK, ale i tak nie mamy co zrobić z błędem, bo już dodaliśmy do własnej listy :)

        Return iCnt
    End Function



#Region "ściąganie plików z bufora serwera"
    Public Shared Async Function GetPicListBuffer(oServer As ShareServer) As Task(Of BaseList(Of Vblib.OnePic))
        Dim listaTemp As New pkar.BaseList(Of Vblib.OnePic)("dummyfolder")


        Dim JsonList As String = Await _clientSlow.GetStringAsync(GetUri(oServer, "currentpiclistforme"))
        If JsonList Is Nothing Then Return Nothing
        If JsonList = "[]" Then Return listaTemp

        If Not listaTemp.Import(JsonList) Then Return Nothing    ' nie da się zdeserializować

        Return listaTemp

    End Function

    ''' <summary>
    ''' wczytaj plik z serwera, używając InBufferPathName, daje stream
    ''' </summary>
    Public Shared Async Function GetPicDataFromBuff(oServer As ShareServer, fname As String) As Task(Of IO.Stream)
        Return Await _clientSlow.GetStreamAsync(GetUri(oServer, "currentpicdata", "fname=" & fname))
    End Function

#End Region

#Region "mappedSource"

    Private Shared _MS_ShareServer As ShareServer

    Public Shared Sub MS_SetServer(linek As String)
        _MS_ShareServer = New ShareServer

        ' $"http://{adres}:{APP_HTTP_PORT}/?guid={_item.mappedGuid}"
        Dim iInd As Integer = linek.IndexOf(":")
        If iInd < 2 Then Return
        linek = linek.Substring(iInd + 3) ' $"{adres}:{APP_HTTP_PORT}/?guid={_item.mappedGuid}"

        iInd = linek.IndexOf(":")
        If iInd < 2 Then Return
        _MS_ShareServer.serverAddress = linek.Substring(0, iInd)
        linek = linek.Substring(iInd + 1) ' $"{APP_HTTP_PORT}/?guid={_item.mappedGuid}"

        ' że port jest na sztywno, i nie ma go co podawać?
        'iInd = linek.IndexOf("/")
        'If iInd < 2 Then Return
        'Integer.TryParse(linek.Substring(0, iInd), _MS_ShareSe)

        iInd = linek.IndexOf("=")
        If iInd < 2 Then Return
        _MS_ShareServer.login = New Guid(linek.Substring(iInd + 1))

    End Sub

    Public Shared Async Function MS_TryConnect() As Task(Of String)
        Return Await TryConnect(_MS_ShareServer)
    End Function

    Public Shared Async Function MS_GetLastImport() As Task(Of String)
        If Not EnsureClient() Then Return "FAIL EnsureClient"

        Dim ret As String = Await _clientQuick.GetStringAsync(GetUri(_MS_ShareServer, "getlastupload"))
        _inuse = False ' =true jest w EnsureClient
        Return ret
    End Function

    Public Shared Async Function MS_sendPic(fname As String, strumyk As MemoryStream) As Task(Of String)
        'If Not EnsureClient() Then Return "FAIL EnsureClient"

        Try
            Dim ret As String = Await MS_AnnouncePic(fname)
            If ret.StartsWith("DONT WANT") Then Return ret
            If ret.NotStartsWith("OK") Then Return ret

            ret = Await MS_sendPicData(strumyk)
            Return ret

        Finally
            _inuse = False  ' =true jest w EnsureClient w Announce

        End Try

    End Function

    Public Shared Async Function MS_SignalEndOfPics() As Task(Of String)
        Return Await _clientQuick.GetStringAsync(GetUri(_MS_ShareServer, "endofpics"))
    End Function

    Public Shared Async Function MS_GetPurgeList() As Task(Of String)
        Return Await _clientSlow.GetStringAsync(GetUri(_MS_ShareServer, "purgegetlist"))
    End Function

    Public Shared Async Function MS_ClearPurgeList() As Task(Of String)
        Return Await _clientSlow.GetStringAsync(GetUri(_MS_ShareServer, "purgeclearlist"))
    End Function

    Public Shared Async Function MS_GetPurgeDelay() As Task(Of Integer)
        Dim kiedy As String = Await _clientSlow.GetStringAsync(GetUri(_MS_ShareServer, "purgegetdelay"))
        If kiedy.NotStartsWith("OK") Then Return -1
        Return kiedy.ParseInt(-1)
    End Function

    Private Shared Async Function MS_AnnouncePic(filename As String) As Task(Of String)
        If Not EnsureClient() Then Return "FAIL EnsureClient"
        Return Await _clientQuick.GetStringAsync(GetUri(_MS_ShareServer, "expectfilename", "fname=" & filename))
    End Function


    Private Shared Async Function MS_sendPicData(strumykMem As MemoryStream) As Task(Of String)

        'Try ' dla Finally - zwolnienie zasobów
        Dim contentPic As ByteArrayContent
            contentPic = New ByteArrayContent(strumykMem.ToArray)

            Dim resp As HttpResponseMessage

            Dim newPicUri As Uri = GetUri(_MS_ShareServer, "putpicdata")

            Try
                resp = Await _clientSlow.PutAsync(newPicUri, contentPic)
                If Not resp.IsSuccessStatusCode Then Return "FAIL data notOK"
            Catch ex As Exception
                Return "FAIL Sending data"
            End Try

            Return "OK"

        'Finally
        '    _inuse = False  ' =true jest w EnsureClient
        'End Try

    End Function


#End Region


#Region "plik purge"

    ''' <summary>
    ''' check if server maintains purge file for this client
    ''' </summary>
    ''' <returns>YES... NO... FAIL ERROR</returns>
    Public Shared Async Function PurgeIsMaintained(oServer As Vblib.ShareServer) As Task(Of String)
        If Not EnsureClient() Then Return "FAIL"

        Dim ret As String = ""
        Try
            ret = Await _clientQuick.GetStringAsync(GetUri(oServer, "purgegetstatus"))
        Catch ex As Exception
            ret = "ERROR"
        End Try

        _inuse = False  ' =true jest w EnsureClient
        Return ret

    End Function

    ''' <summary>
    ''' gets contens of purge file from server
    ''' </summary>
    ''' <returns>FAIL ERROR filecontens</returns>
    Public Shared Async Function PurgeGetList(oServer As Vblib.ShareServer) As Task(Of String)
        If Not EnsureClient() Then Return "FAIL"

        Dim ret As String = ""
        Try
            ret = Await _clientSlow.GetStringAsync(GetUri(oServer, "purgegetlist"))
        Catch ex As Exception
            ret = "ERROR"
        End Try

        _inuse = False  ' =true jest w EnsureClient
        Return ret

    End Function

    ''' <summary>
    ''' wysłanie do serwera komendy usuwania z pliku purge tego co już tutaj usuwaliśmy
    ''' </summary>
    ''' <param name="oTillDate">do tej daty włącznie jest do usunięcia</param>
    ''' <returns>OK, purged {iPurged} entries out of {purgeEntries.Count}</returns>
    Public Shared Async Function PurgeIsMaintained(oServer As Vblib.ShareServer, oTillDate As Date) As Task(Of String)
        ' $"OK, purged {iPurged} entries out of {purgeEntries.Count}"
        If Not EnsureClient() Then Return "FAIL"

        Dim ret As String = ""
        Try
            Dim limitDate As String = oTillDate.ToString("yyyyMMdd.HHmm")
            ret = Await _clientQuick.GetStringAsync(GetUri(oServer, "purgeresetlist", limitDate))
        Catch ex As Exception
            ret = "ERROR"
        End Try

        _inuse = False  ' =true jest w EnsureClient
        Return ret

    End Function

#End Region

End Class

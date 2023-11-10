
' albo w OnePic, albo w onepic.History, albo w onepic.Description - z jakiego kanału zdjęcie pochodzi
' zarówno "gwoli informacji", jak i by było wiadomo jak zwrócić komentarz

Imports System.Net
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports pkar

#Region "struktury"

' pokazywane w Browser.Pic.ContextMenu:Channels i lista channels które obejmują to zdjęcie - wtedy można dać disable, czyli dopisać do wyjątków kanału
Public Class ShareChannel
    Inherits BaseStruct

    Public Property nazwa As String

    ''' <summary>
    ''' lista kwerend używanych jako OR włączające zdjęcia do kanału, każda z processingiem
    ''' </summary>
    ''' 
    Public Property queries As List(Of ShareQueryProcess)

    ''' <summary>
    ''' lista PicGuid wyłączanych (mimo że pasują do powyższych filtrów)
    ''' </summary>
    Public Property exclusions As List(Of String)

    ''' <summary>
    ''' lista kroków do processing, jak zdjęcie miałoby być obrobione - składane z query.processing 
    ''' </summary>
    Public Property processing As String

End Class

Public Class ShareQueryProcess

    Public Property queryName As String
    <JsonIgnore>
    Public Property query As SearchQuery

    Public Property processing As String
End Class


Public MustInherit Class SharePeer
    Inherits BaseStruct

    Public Property login As Guid ' tym się INNY loguje
    Public Property displayName As String ' widać go jako...
End Class

Public Class ShareLogin
    Inherits SharePeer

    Public Property enabled As Boolean

    Public Property channels As List(Of ShareChannelProcess) ' widzi kanały i dla każdego może być processing

    '<JsonIgnore>
    'Public Property channels As List(Of ShareChannel) ' może widzieć kanały...

    'Public Property channelNames As String ' na dysku są tylko nazwy

    ''' <summary>
    ''' lista PicGuid wyłączanych (mimo że pasują do powyższych filtrów)
    ''' </summary>
    Public Property exclusions As List(Of String)

    Public Property processing As String ' składane do query.processing i channel.processing

    Public Property allowUpload As Boolean ' czy może ten ktoś robić upload

    Public Property allowedLogin As New ShareLoginData
    Public Property lastLogin As New ShareLoginData
    Public Property maintainPurge As Boolean

End Class

Public Class ShareChannelProcess

    Public Property channelName As String
    <JsonIgnore>
    Public Property channel As ShareChannel

    Public Property processing As String
End Class


Public Class ShareLoginData
    Inherits BaseStruct
    Public Property kiedy As Date ' kiedy ostatnio się logował / not used
    Public Property remoteHostName As String ' z jakiego hosta (zgłaszany przez jego PicSort) / tylko taki może

    ' musi być ="", bo inaczej kontrolka IPaddress ma problem
    Public Property IPaddr As String = ""   ' adres / tylko taki może (wtedy z netmask piętro wyżej)
    Public Property netmask As String = ""   ' IPNetwork w asp.net   

    ' albo specjalna klasa adres/maska ' https://github.com/jsakamoto/ipaddressrange/
    ' 'usercontrol: https://github.com/mariugul/IPUserControls
End Class

Public Class ShareServer
    Inherits SharePeer

    Public Property serverAddress As String ' adres PicSort z którym mam się łączyć
    Public Property filters As List(Of SearchQuery) ' mogę filtrować po swojej stronie
    Public Property uploadProcessing As String ' jeśli uploaduję, to mogę coś zmieniać
    Public Property lockForwarding As Boolean ' i mogę zablokować dalsze udostępnianie

    Public Property lastCheck As Date ' kiedy ostatnio sprawdzałem
    Public Property lastId As Integer ' jaki ID ostatnio widziałem (serno)

    ''' <summary>
    ''' z podanego linku robi obiekt, gdy błąd: onew.login = NULL, serveradd = komunikat błędu (od ERROR)
    ''' </summary>
    ''' <returns>NULL jeśli błąd</returns>
    Public Shared Function CreateFromLink(link As String) As ShareServer

        Dim oNew As New ShareServer
        oNew.login = Guid.Empty

        If Not link.StartsWithCI("PicSort://") Then
            oNew.serverAddress = "ERROR link jest niepoprawny..."
            Return oNew
        End If

        link = link.Substring("PicSort://".Length)    ' ucinamy początek
        Dim aToken As String() = link.Split("/")
        If aToken.Length <> 2 Then
            oNew.serverAddress = "ERROR link jest jednak niepoprawny..."
            Return oNew
        End If

        Try
            oNew.login = New Guid(aToken(1))
        Catch ex As Exception
            oNew.serverAddress = "ERROR GUID jest zły"
            oNew.login = Guid.Empty
        End Try

        oNew.serverAddress = aToken(0)

        Return oNew
    End Function

End Class


Public Class ShareDescription
    Inherits BaseStruct

    ''' <summary>
    ''' id zdjęcia którego dotyczy komentarz (picguid, lub suggestedfilename z prefiksem ':')
    ''' </summary>
    Public Property picid As String
    ''' <summary>
    ''' normalny opis, w nim pole peerGuid
    ''' </summary>
    Public Property descr As Vblib.OneDescription

    ''' <summary>
    ''' w tym ustawienie oNew.descr.PeerGuid (na prefiksowany L/S)
    ''' </summary>
    Public Shared Function GetForPic(picek As Vblib.OnePic, descr As String) As ShareDescription
        Dim oNew As New Vblib.ShareDescription
        oNew.descr = New Vblib.OneDescription(descr, "")
        oNew.descr.PeerGuid = picek.GetLastShareGuid
        oNew.picid = picek.PicGuid
        If String.IsNullOrWhiteSpace(oNew.picid) Then oNew.picid = ":" & picek.sSuggestedFilename
        Return oNew
    End Function
End Class


#End Region



#Region "listy"
Public Class ShareChannelsList
    Inherits BaseList(Of Vblib.ShareChannel)

    Private _queries As BaseList(Of SearchQuery)

    Public Sub New(sFolder As String, queriesList As BaseList(Of SearchQuery))
        MyBase.New(sFolder, "channels.json")
        _queries = queriesList
    End Sub

    Public Overrides Function Load() As Boolean
        Dim ret As Boolean = MyBase.Load()
        If Not ret Then Return False

        ReResolveQueries()
        Return True
    End Function

    Public Sub ReResolveQueries()
        'If _list Is Nothing Then Return
        If Count < 1 Then Return

        For Each oItem As Vblib.ShareChannel In Me
            If oItem.queries Is Nothing Then Continue For
            For Each queryData As Vblib.ShareQueryProcess In oItem.queries

                For Each query As Vblib.SearchQuery In _queries
                    If query.nazwa = queryData.queryName Then
                        queryData.query = query
                        Exit For
                    End If
                Next
            Next
        Next

    End Sub


End Class

Public Class ShareLoginsList
    Inherits BaseList(Of Vblib.ShareLogin)

    Dim _channelsList As ShareChannelsList

    Public Sub New(sFolder As String, channels As ShareChannelsList)
        MyBase.New(sFolder, "logins.json")
        _channelsList = channels
    End Sub

    Public Overrides Function Load() As Boolean
        Dim ret As Boolean = MyBase.Load()
        If Not ret Then Return False

        ReResolveChannels()
        Return True
    End Function

    Public Sub ReResolveChannels()
        'If _list Is Nothing Then Return
        If Count < 1 Then Return

        For Each oItem As Vblib.ShareLogin In Me
            If oItem.channels Is Nothing Then Continue For

            For Each channelProc As ShareChannelProcess In oItem.channels
                If String.IsNullOrWhiteSpace(channelProc.channelName.Trim) Then Continue For ' dwa ;; pod rząd

                '' zjakiegoś powodu się zwielokratnia, więc tu ucinam dublety
                'Dim sNewChanName As String = $";{channelName};"
                'If sChanNames.Contains(sNewChanName) Then Continue For
                'sChanNames &= sNewChanName

                For Each channel As Vblib.ShareChannel In _channelsList
                    If channel.nazwa = channelProc.channelName Then ' channelName.Trim Then
                        'If oItem.channels Is Nothing Then oItem.channels = New List(Of ShareChannel)
                        'oItem.channels.Add(channel)
                        channelProc.channel = channel
                        Exit For
                    End If
                Next

            Next
        Next

    End Sub

    Public Function FindByGuid(loginGuid As String) As ShareLogin
        Return MyBase.Find(Function(x) x.login.ToString = loginGuid)
    End Function
End Class



Public Class ShareServerList
    Inherits BaseList(Of ShareServer)

    Public Sub New(folder As String)
        MyBase.New(folder, "servers.json")
    End Sub

    Public Function FindByGuid(loginGuid As String) As ShareServer
        Return MyBase.Find(Function(x) x.login.ToString = loginGuid)
    End Function

End Class

Public Class ShareDescInList
    Inherits BaseList(Of ShareDescription)

    Public Sub New(folder As String)
        MyBase.New(folder, "shareIncoming.json")
    End Sub

    Public Function FindForPic(picek As Vblib.OnePic) As List(Of ShareDescription)
        Dim ret As List(Of ShareDescription)

        ' jeśli picek ma guid, to z niego skorzystamy najpierw
        If Not String.IsNullOrWhiteSpace(picek.PicGuid) Then
            ret = MyBase.Where(Function(x) x.picid = picek.PicGuid)
            If ret IsNot Nothing AndAlso ret.Count > 0 Then Return ret
        End If

        ' próbujemy z dwukropkiem - nazwą pliku
        Dim temp As String = ":" & IO.Path.GetFileName(picek.InBufferPathName)
        ret = MyBase.Where(Function(x) x.picid = temp)
        If ret IsNot Nothing AndAlso ret.Count > 0 Then Return ret

        Return Nothing
    End Function

End Class

#End Region
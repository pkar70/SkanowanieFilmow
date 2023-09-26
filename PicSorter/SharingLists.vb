

Imports pkar
Imports Vblib

' nie może być w lib, bo potrzebuje Application.GetQueries.GetList

'Public Class ShareChannelsList
'    Inherits BaseList(Of Vblib.ShareChannel)

'    Public Sub New(sFolder As String)
'        MyBase.New(sFolder, "channels.json")
'    End Sub

'    Public Overrides Function Load() As Boolean
'        Dim ret As Boolean = MyBase.Load()
'        If Not ret Then Return False

'        ReResolveQueries()
'        Return True
'    End Function

'    Public Sub ReResolveQueries()
'        If _list Is Nothing Then Return
'        If _list.Count < 1 Then Return

'        For Each oItem As Vblib.ShareChannel In _list
'            If oItem.queries Is Nothing Then Continue For
'            For Each queryData As Vblib.ShareQueryProcess In oItem.queries

'                For Each query As Vblib.SearchQuery In Application.GetQueries.GetList
'                    If query.nazwa = queryData.queryName Then
'                        queryData.query = query
'                        Exit For
'                    End If
'                Next
'            Next
'        Next

'    End Sub


'End Class


'Public Class ShareLoginsList
'    Inherits BaseList(Of Vblib.ShareLogin)

'    Public Sub New(sFolder As String)
'        MyBase.New(sFolder, "logins.json")
'    End Sub

'    Public Overrides Function Load() As Boolean
'        Dim ret As Boolean = MyBase.Load()
'        If Not ret Then Return False

'        ReResolveChannels()
'        Return True
'    End Function

'    'Public Overrides Function Save(Optional bIgnoreNulls As Boolean = False) As Boolean

'    '    For Each oLogin As ShareLogin In _list
'    '        If oLogin Is Nothing Then Continue For

'    '        Dim channelNames As String = ""
'    '        If oLogin.channels IsNot Nothing Then
'    '            For Each oChannel As ShareChannel In oLogin.channels
'    '                If oChannel Is Nothing Then Continue For
'    '                If channelNames <> "" Then channelNames &= ";"
'    '                channelNames &= oChannel.nazwa
'    '            Next
'    '        End If
'    '        oLogin.channelNames = channelNames
'    '    Next

'    '    Return MyBase.Save(bIgnoreNulls)
'    'End Function


'    Public Sub ReResolveChannels()
'        If _list Is Nothing Then Return
'        If _list.Count < 1 Then Return

'        For Each oItem As Vblib.ShareLogin In _list
'            If oItem.channels Is Nothing Then Continue For
'            'oItem.channels?.Clear()

'            'If String.IsNullOrWhiteSpace(oItem.channelNames) Then Continue For

'            'Dim aChanns As String() = oItem.channelNames.Split(";")

'            'Dim sChanNames As String = ""

'            'For Each channelName As String In aChanns
'            For Each channelProc As ShareChannelProcess In oItem.channels
'                If String.IsNullOrWhiteSpace(channelProc.channelName.Trim) Then Continue For ' dwa ;; pod rząd

'                '' zjakiegoś powodu się zwielokratnia, więc tu ucinam dublety
'                'Dim sNewChanName As String = $";{channelName};"
'                'If sChanNames.Contains(sNewChanName) Then Continue For
'                'sChanNames &= sNewChanName

'                For Each channel As Vblib.ShareChannel In Application.GetShareChannels.GetList
'                    If channel.nazwa = channelProc.channelName Then ' channelName.Trim Then
'                        'If oItem.channels Is Nothing Then oItem.channels = New List(Of ShareChannel)
'                        'oItem.channels.Add(channel)
'                        channelProc.channel = channel
'                        Exit For
'                    End If
'                Next

'            Next
'        Next

'    End Sub

'    Public Function FindByLogin(loginGuid As String) As ShareLogin
'        Return MyBase.Find(Function(x) x.login.ToString = loginGuid)
'    End Function
'End Class



Imports pkar
Imports Vblib

' nie może być w lib, bo potrzebuje Application.GetQueries.GetList

Public Class ShareChannelsList
    Inherits BaseList(Of Vblib.ShareChannel)

    Public Sub New(sFolder As String)
        MyBase.New(sFolder, "channels.json")
    End Sub

    Public Overrides Function Load() As Boolean
        Dim ret As Boolean = MyBase.Load()
        If Not ret Then Return False

        ReResolveQueries()
        Return True
    End Function

    Public Sub ReResolveQueries()
        If _list Is Nothing Then Return
        If _list.Count < 1 Then Return

        For Each oItem As Vblib.ShareChannel In _list
            For Each queryData As Vblib.ShareQueryProcess In oItem.queries

                For Each query As Vblib.SearchQuery In Application.GetQueries.GetList
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

    Public Sub New(sFolder As String)
        MyBase.New(sFolder, "logins.json")
    End Sub

    Public Overrides Function Load() As Boolean
        Dim ret As Boolean = MyBase.Load()
        If Not ret Then Return False

        ReResolveChannels()
        Return True
    End Function

    Public Overrides Function Save(Optional bIgnoreNulls As Boolean = False) As Boolean

        For Each oLogin As ShareLogin In _list
            If oLogin Is Nothing Then Continue For

            Dim channelNames As String = ""
            If oLogin.channels IsNot Nothing Then
                For Each oChannel As ShareChannel In oLogin.channels
                    If oChannel Is Nothing Then Continue For
                    If channelNames <> "" Then channelNames &= ";"
                    channelNames &= oChannel.nazwa
                Next
            End If
            oLogin.channelNames = channelNames
        Next

        Return MyBase.Save(bIgnoreNulls)
    End Function


    Public Sub ReResolveChannels()
        If _list Is Nothing Then Return
        If _list.Count < 1 Then Return

        For Each oItem As Vblib.ShareLogin In _list

            If String.IsNullOrWhiteSpace(oItem.channelNames) Then Continue For

            Dim aChanns As String() = oItem.channelNames.Split(";")

            For Each channelName As String In aChanns
                If String.IsNullOrWhiteSpace(channelName.Trim) Then Continue For ' dwa ;; pod rząd
                For Each channel As Vblib.ShareChannel In Application.GetShareChannels.GetList
                    If channel.nazwa = channelName.Trim Then
                        If oItem.channels Is Nothing Then oItem.channels = New List(Of ShareChannel)
                        oItem.channels.Add(channel)
                        Exit For
                    End If
                Next

            Next
        Next

    End Sub


End Class

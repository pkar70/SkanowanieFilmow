

Imports pkar

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

    Public Sub ReResolveChannels()
        If _list Is Nothing Then Return
        If _list.Count < 1 Then Return

        For Each oItem As Vblib.ShareLogin In _list
            For Each channel As Vblib.ShareChannel In oItem.channels

                ' *TODO* tylko pilnować trzeba żeby nie było cross-wywołań
                'For Each query As Vblib.SearchQuery In Application.GetQueries.GetList
                '    If query.nazwa = queryData.queryName Then
                '        queryData.query = query
                '        Exit For
                '    End If
                'Next
            Next
        Next

    End Sub


End Class

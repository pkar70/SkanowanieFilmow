
' w 2.0, bo PicSourceImplement


Imports System.Security.Cryptography

Public Class PicSourceList
    Inherits Vblib.MojaLista(Of PicSourceImplement)

    Private _dataFolder As String

    Public Sub New(sFolder As String)
        MyBase.New(sFolder, "sources.json")
        _dataFolder = sFolder
    End Sub

    ''' <summary>
    ''' konieczne dla poprawnego działania Purge (inaczej byłoby wspólne - sourcename jest puste przy Load)
    ''' używa katalogu który był parametrem dla New
    ''' </summary>
    Public Sub InitDataDirectory()
        For Each oSrc As PicSourceImplement In _lista
            oSrc.InitDataDirectory(_dataFolder)
        Next
    End Sub

    Public Sub AddToPurgeList(sSource As String, sId As String)
        For Each oSrc As PicSourceImplement In _lista
            If oSrc.SourceName = sSource Then
                oSrc.AddToPurgeList(sId)
            End If
        Next
    End Sub

End Class

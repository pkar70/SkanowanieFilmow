
' w 2.0, bo PicSourceImplement

Public Class PicSourceList
    Inherits pkar.BaseList(Of PicSourceImplement)

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
        Me.ForEach(Sub(x) x.InitDataDirectory(_dataFolder))
    End Sub

    Public Sub AddToPurgeList(sSource As String, sId As String)
        Find(Function(x) x.SourceName = sSource)?.AddToPurgeList(sId)
    End Sub

    Protected Overrides Sub InsertDefaultContent()

        ' wzięte z Settings.Sources
        ' AdHoc nie potrzebuje folderu na plik PURGE, bo nie purgujemy
        Dim oNewSrc As Vblib.PicSourceBase = New PicSourceImplement(Vblib.PicSourceType.AdHOC, "")
        oNewSrc.SourceName = "AdHoc"
        oNewSrc.enabled = False
        oNewSrc.defaultExif = New Vblib.ExifTag(Vblib.ExifSource.SourceDefault)

        Add(oNewSrc)

    End Sub

End Class

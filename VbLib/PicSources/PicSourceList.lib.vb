Public Class PicSourceList
    Inherits MojaLista(Of PicSourceBase)

    Public Sub New(sFolder As String)
        MyBase.New(sFolder, "sources.json")
    End Sub

End Class

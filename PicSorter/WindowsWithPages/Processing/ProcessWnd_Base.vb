Imports pkar.UI.Extensions

Public Class ProcessWnd_Base
    Inherits Window

    Protected Function GetBuffer() As Vblib.BufferSortowania
        Dim procPic As ProcessPic = GetOwner()
        Return procPic?.GetCurrentBuffer
    End Function

    Protected Function IsDefaultBuff() As Boolean
        Dim procPic As ProcessPic = GetOwner()
        If procPic Is Nothing Then Return False
        Return procPic.IsDefaultBuff
    End Function

    Protected Function GetOwner() As ProcessPic
        Dim procPic As ProcessPic = TryCast(Me.Owner, ProcessPic)
        If procPic Is Nothing Then
            Me.MsgBox("Nie ma ownera, lub nie jest to ProcessPic, nie mam skąd wziąć bufora!")
            Return Nothing
        End If

        Return procPic
    End Function
End Class

Imports pkar.UI.Extensions
Imports Vblib

Public Class AddLink

    Public Property linek As New OneLink

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()
        ' Add any initialization after the InitializeComponent() call.
        DataContext = linek

    End Sub

    Private Sub uiOk_Click(sender As Object, e As RoutedEventArgs)

        If linek.opis.Length < 3 Then
            MsgBox("Za krótki opis...")
            Return
        End If

        Me.DialogResult = True
        Me.Close()

    End Sub

    Private Sub Window_KeyUp(sender As Object, e As KeyEventArgs)
        If e.IsRepeat Then Return
        If e.Key <> Key.Escape Then Return
        Me.DialogResult = False
        Me.Close()
    End Sub

End Class

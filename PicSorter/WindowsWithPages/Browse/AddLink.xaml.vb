Imports pkar.UI.Extensions
Imports Vblib
Imports pkar.DotNetExtensions

Public Class AddLink

    Public Property linek As New OneLink

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()
        ' Add any initialization after the InitializeComponent() call.
        DataContext = linek

    End Sub

    Private Sub uiOk_Click(sender As Object, e As RoutedEventArgs)

        uiOpis.Focus()    ' jak się wkleja link, to ładnie tworzy opis, ale jakby nie robi refresh zmiennych?

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

    Private Sub uiLink_TextChanged(sender As Object, e As TextChangedEventArgs)
        Dim link As String = uiLink.Text
        If link = "" Then Return

        If link.StartsWith("#") Then
            uiLink.Text = "pic" & link
            Return
        End If

        If Not link.ContainsCI("wikipedia") Then Return
        If uiOpis.Text <> "" Then Return

        uiOpis.Text = "wiki"

        Dim iInd As Integer = link.IndexOf("wikipedia")
        If link.AsSpan(iInd - 1, 1) = "." Then

            link = link.Substring(0, iInd - 1)
            iInd = link.LastIndexOf("/")

            uiOpis.Text &= $" ({link.Substring(iInd + 1)})"
        End If

        uiOpis.Focus() ' ominięcie "za krótki"
        uiLink.Focus()
    End Sub

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        uiLink.Focus()
    End Sub
End Class

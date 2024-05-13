Public Class FilterKeywords
    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        ' WypelnMenuKeywords()
        _lastAdd = Date.Now
    End Sub

    'Private Sub WypelnMenuKeywords()

    '    Dim count As Integer = AddDescription.WypelnMenuKeywords(uiMenuKeywords, AddressOf DodajTenKeyword)

    '    If count < 1 Then
    '        uiAdd.IsEnabled = False
    '        uiAdd.ToolTip = "(nie ma zdefiniowanych słów kluczowych)"
    '    Else
    '        uiAdd.IsEnabled = True
    '        uiAdd.ToolTip = "Dodaj słowa kluczowe"
    '    End If

    'End Sub

    Private _lastAdd As Date

    'Private Sub DodajTenKeyword(sender As Object, e As RoutedEventArgs)
    '    uiAddPopup.IsOpen = False

    '    If _lastAdd.AddSeconds(0.5) > Date.Now Then Return
    '    _lastAdd = Date.Now

    '    Dim oMI As MenuItem = sender
    '    Dim oKeyword As Vblib.OneKeyword = oMI?.DataContext
    '    If oKeyword Is Nothing Then Return

    '    uiKeywords.Text = (uiKeywords.Text & " " & oKeyword.sId).Trim & " "

    'End Sub

    'Private Sub uiAdd_Click(sender As Object, e As RoutedEventArgs)
    '    uiAddPopup.IsOpen = Not uiAddPopup.IsOpen
    'End Sub

    Private Sub Button_Click(sender As Object, e As RoutedEventArgs)
        Me.Close()
    End Sub

    Public Function GetKwerenda() As String
        Return uiKeywords.uiSlowka.Text
    End Function
End Class

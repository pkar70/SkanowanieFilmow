

Public Class UserControlListaChecks
    Inherits StretchedListView

    Public Sub SetItems(listaNazw As String())
        ItemsSource = Nothing

        Dim _lista As New List(Of CheckBox)
        Dim zaznaczone As String = Vblib.GetSettingsString(Name)

        For Each nazwa As String In listaNazw
            _lista.Add(New CheckBox With
                       {.Content = nazwa.Replace("_", "__"),
                       .IsChecked = zaznaczone.Contains(nazwa & "|")})
        Next

        ItemsSource = _lista

    End Sub

    Public Function GetChecked() As String
        Dim ret As String = ""
        For Each oItem As CheckBox In ItemsSource
            If oItem.IsChecked Then ret &= oItem.Content.ToString.Replace("__", "_") & "|"
        Next

        Return ret
    End Function

    Public Function Count() As Integer
        Dim ret As Integer = 0
        For Each oItem As CheckBox In ItemsSource
            If oItem.IsChecked Then ret += 1
        Next

        Return ret
    End Function


    Public Sub SetSettingsString()
        Vblib.SetSettingsString(Name, GetChecked)
    End Sub

End Class

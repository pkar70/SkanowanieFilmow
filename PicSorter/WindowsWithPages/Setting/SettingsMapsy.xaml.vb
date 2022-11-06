
Imports vb14 = Vblib.pkarlibmodule14


Class SettingsMapsy


    Public Shared Sub WypelnMenuMapami(oMenuItem As MenuItem, oEvent As RoutedEventHandler)
        initLista()

        oMenuItem.Items.Clear()

        For Each oEngine As Vblib.JednaMapa In _lista.GetList
            Dim oNew As New MenuItem
            oNew.Header = oEngine.nazwa
            oNew.DataContext = oEngine
            AddHandler oNew.Click, oEvent
            oMenuItem.Items.Add(oNew)
        Next

        oMenuItem.IsEnabled = (oMenuItem.Items.Count > 0)

    End Sub

    Private Shared _lista As Vblib.Mapy

    Private Shared Sub initLista()
        If _lista IsNot Nothing Then Return

        _lista = New Vblib.Mapy(Application.GetDataFolder)
        _lista.Load()
    End Sub


    Private Sub uiOk_Click(sender As Object, e As RoutedEventArgs)
        _lista.Save()
    End Sub

    Private Function LinkToName(sLink As String) As String
        ' *TODO* wczytaj nazwę mapy

        Dim sNazwa As String = sLink
        Dim iInd As Integer = sNazwa.IndexOf("://")
        sNazwa = sNazwa.Substring(iInd + 3)
        iInd = sNazwa.IndexOf(":")
        sNazwa = sNazwa.Substring(0, iInd)
        sNazwa = sNazwa.Replace(".pl", "")
        sNazwa = sNazwa.Replace(".com", "")
        sNazwa = sNazwa.Replace(".org", "")
        sNazwa = sNazwa.Replace("www.", "")
        sNazwa = sNazwa.Replace("mapa.", "")

        Return sNazwa
    End Function

    Private Async Sub uiAddMapa_Click(sender As Object, e As RoutedEventArgs)

        Dim sLink As String = Await vb14.DialogBoxInputAllDirectAsync("Podaj link do nowej mapy" & vbCrLf & "(użyj %lat i %lon)")
        If sLink = "" Then Return

        If Not sLink.Contains("%lat") Or Not sLink.Contains("%lon") Then
            vb14.DialogBox("Link powinien zawierac %lat i %long")
            Return
        End If

        Dim oNew As New Vblib.JednaMapa(LinkToName(sLink), sLink)
        _lista.Add(oNew)

        uiLista.ItemsSource = _lista

        ' bez Save - to będzie na [OK]

    End Sub

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        initLista()
        uiLista.ItemsSource = _lista.GetList
    End Sub
End Class

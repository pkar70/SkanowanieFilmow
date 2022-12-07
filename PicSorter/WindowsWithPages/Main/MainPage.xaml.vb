
Imports vb14 = Vblib.pkarlibmodule14

Class MainPage
    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)

        If App.GetDataFolder(False) = "" Then
            vb14.DialogBox("Nie ma ustawień, konieczne Settings")
            vb14.SetSettingsInt("uiMaxThumbs", CalculateMaxThumbCount)
            uiSettings_Click(Nothing, Nothing)
        End If

        ' guzik Retrieve wyłączany jak nie ma zdefiniowanych sources
        uiRetrieve.IsEnabled = IO.File.Exists(App.GetDataFile("", "sources.json", False))

        ' *TODO* guziki pozostałe wyłączane jak nie ma LocalStorage

        Dim count As Integer = Application.GetBuffer.Count
        uiProcess.Content = $"Process ({count})"

        'Vblib.PicSourceBase.UseTagsFromFilename(Application.GetBuffer.GetList, Application.GetKeywords.ToFlatList)
        'Application.GetBuffer.SaveData()
    End Sub


    Private Function CalculateMaxThumbCount() As Integer
        ' policz ile zdjęć można mieć
        ' zakładam jeden thumb to 500 KiB (tak zapisałem kiedyś w userGuide)

        Dim iGB As Integer = vblib46ram.GetGBram
        If iGB > 8 Then Return 1000 ' dla >8 GB, 1000 obrazków (500 MB)
        If iGB > 4 Then Return 500 ' dla >4 GB, 500 obrazków (256 MB)

        Return 100

    End Function

    Private Sub uiSettings_Click(sender As Object, e As RoutedEventArgs)
        uiSettings.IsEnabled = False
        Dim oWnd As New SettingsWindow
        oWnd.ShowDialog()
        uiSettings.IsEnabled = True
    End Sub

    Private Sub uiProcess_Click(sender As Object, e As RoutedEventArgs)
        uiProcess.IsEnabled = False
        Dim oWnd As New ProcessPic
        oWnd.Show()
        uiProcess.IsEnabled = True
    End Sub

    Private Sub uiRetrieve_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New ProcessDownload
        oWnd.Show()
    End Sub
End Class

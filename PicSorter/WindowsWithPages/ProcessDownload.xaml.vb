
' ściąganie ze źródeł

Imports vb14 = Vblib.pkarlibmodule14

Public Class ProcessDownload
    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        uiLista.ItemsSource = Application.GetSourcesList.GetList
    End Sub

    Private Async Sub uiGetThis_Click(sender As Object, e As RoutedEventArgs)
        ' to konkretne, więc bardziej szczegółowo
        Dim oFE As FrameworkElement = sender
        Dim oSrc As Vblib.PicSourceBase = oFE?.DataContext
        If oSrc Is Nothing Then Return

        If oSrc.Typ = Vblib.PicSourceType.AdHOC Then
            ' troche bardziej skomplikowane, zeby w oSrc.Path był ostatni naprawdę użyty katalog
            Dim dirToGet As String = SettingsGlobal.FolderBrowser(oSrc.Path)
            If dirToGet = "" Then Return
            oSrc.Path = dirToGet
        End If

        If oSrc.currentExif Is Nothing Then oSrc.currentExif = oSrc.defaultExif.Clone
        Dim oWnd As New EditExifTag(oSrc.currentExif, oSrc.SourceName & " (chwilowe)", EditExifTagScope.LimitedToSourceDir, False)
        oWnd.ShowDialog()

        Await RetrieveFilesFromSource(oSrc)

        Dim iToPurge As Integer = oSrc.Purge(False)
        If iToPurge > 0 Then
            If Await vb14.DialogBoxYNAsync($"Zrobić purge? ({iToPurge} plików)") Then oSrc.Purge(True)
        End If

    End Sub

    Private Async Sub uiGetAll_Click(sender As Object, e As RoutedEventArgs)
        ' uproszczona wersja

        For Each oSrc As Vblib.PicSourceBase In Application.GetSourcesList.GetList
            If Not oSrc.enabled Then Continue For

            Await RetrieveFilesFromSource(oSrc)
            oSrc.Purge(True)
        Next

    End Sub

    Private Async Function RetrieveFilesFromSource(oSrc As Vblib.PicSourceBase) As Task

        Dim iCount As Integer = oSrc.ReadDirectory
        If iCount < 1 Then Return

        uiProgBar.Maximum = iCount
        uiProgBar.Value = 0
        uiProgBar.Visibility = Visibility.Visible

        Dim oSrcFile As Vblib.OnePic = oSrc.GetFirst
        If oSrcFile Is Nothing Then Return
        Await Application.GetBuffer.AddFile(oSrcFile)

        Do
            oSrcFile = oSrc.GetNext
            If oSrcFile Is Nothing Then Exit Do
            Await Application.GetBuffer.AddFile(oSrcFile)
            uiProgBar.Value += 1
        Loop

        Application.GetBuffer.SaveData()
        oSrc.lastDownload = Date.Now
        uiProgBar.Visibility = Visibility.Collapsed

    End Function

End Class

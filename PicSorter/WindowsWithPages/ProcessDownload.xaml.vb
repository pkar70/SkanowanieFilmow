
' ściąganie ze źródeł

Imports Vblib
Imports vb14 = Vblib.pkarlibmodule14
Imports pkar.DotNetExtensions
'Imports Org.BouncyCastle.Crypto
'Imports Org.BouncyCastle.Crypto

Public Class ProcessDownload
    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        vb14.DumpCurrMethod()
        uiLista.ItemsSource = Application.GetSourcesList.GetList

        CheckDiskFree()
    End Sub

    Private Function CheckDiskFree() As Boolean
        Dim buffer As String = vb14.GetSettingsString("uiFolderBuffer")
        If buffer <> "" Then

            Dim oDrives = IO.DriveInfo.GetDrives()
            For Each oDrive As IO.DriveInfo In oDrives
                If buffer.ToLowerInvariant.StartsWith(oDrive.Name.ToLowerInvariant) Then
                    ' limit: 100 MB
                    If oDrive.AvailableFreeSpace > 100 * 1000 * 1000 Then Return True
                    Exit For
                End If
            Next
        End If

        vb14.DialogBox("Za mało miejsca na dysku z buforem!")
        Return False
    End Function

    Private Async Sub uiGetThis_Click(sender As Object, e As RoutedEventArgs)
        vb14.DumpCurrMethod()

        If Not CheckDiskFree() Then Return

        ' to konkretne, więc bardziej szczegółowo
        Dim oFE As FrameworkElement = sender
        Dim oSrc As Vblib.PicSourceBase = oFE?.DataContext
        If oSrc Is Nothing Then Return

        If oSrc.Typ = Vblib.PicSourceType.AdHOC Then
            ' troche bardziej skomplikowane, zeby w oSrc.Path był ostatni naprawdę użyty katalog
            Dim dirToGet As String = SettingsGlobal.FolderBrowser(oSrc.Path, "Select source folder")
            If dirToGet = "" Then Return
            oSrc.Path = dirToGet

            oSrc.VolLabel = GetVolLabelForPath(dirToGet)

        End If

        If oSrc.currentExif Is Nothing Then oSrc.currentExif = oSrc.defaultExif.Clone
        Dim oWnd As New EditExifTag(oSrc.currentExif, oSrc.SourceName & " (chwilowe)", EditExifTagScope.LimitedToSourceDir, False)
        oWnd.ShowDialog()

        Application.ShowWait(True)
        Dim iCount As Integer = Await RetrieveFilesFromSource(oSrc)
        Application.ShowWait(False)

        Dim iToPurge As Integer = oSrc.Purge(False)
        If iToPurge > 0 Then
            If Await vb14.DialogBoxYNAsync($"Zrobić purge? ({iToPurge} plików)") Then oSrc.Purge(True)
        End If

        If iCount > 0 Then
            vb14.DialogBox($"Done ({iCount} new files).")
        Else
            vb14.DialogBox($"Done.")
        End If

    End Sub

    Private Shared Function GetVolLabelForPath(dirToGet As String) As String
        vb14.DumpCurrMethod()

        Dim oDrives = IO.DriveInfo.GetDrives()
        For Each oDrive As IO.DriveInfo In oDrives
            If oDrive.IsReady Then
                If dirToGet.StartsWithOrdinal(oDrive.RootDirectory.FullName) Then
                    Return oDrive.VolumeLabel & " (" & oDrive.RootDirectory.FullName & ")"
                End If
            End If
        Next

        Return ""
    End Function

    Private Async Sub uiGetAll_Click(sender As Object, e As RoutedEventArgs)
        vb14.DumpCurrMethod()

        ' sprawdzam czy cokolwiek jest zaznaczone
        If Not Application.GetSourcesList.GetList.Any(Function(x) x.enabled) Then
            Await vb14.DialogBoxAsync("Ale nic nie zaznaczyłeś...")
            Return
        End If

        If Not CheckDiskFree() Then Return

        ' uproszczona wersja

        If Not Await vb14.DialogBoxYNAsync("Ściągnąć ze wszystkich zaznaczonych źródeł?") Then Return

        For Each oSrc As Vblib.PicSourceBase In Application.GetSourcesList.GetList
            If Not oSrc.enabled Then Continue For

            Application.ShowWait(True)
            Await RetrieveFilesFromSource(oSrc)
            Application.ShowWait(False)
            oSrc.Purge(True)
        Next

        vb14.DialogBox("Done.")

    End Sub

    Private Async Function RetrieveFilesFromSource(oSrc As Vblib.PicSourceBase) As Task(Of Integer)
        vb14.DumpCurrMethod(oSrc.VolLabel)

        Dim iCount As Integer = oSrc.ReadDirectory(Application.GetKeywords.ToFlatList)
        'Await vb14.DialogBoxAsync($"read {iCount} files")
        vb14.DumpMessage($"Read {iCount} files")

        If iCount < 1 Then Return 0

        uiProgBar.Maximum = iCount
        uiProgBar.Value = 0
        uiProgBar.Visibility = Visibility.Visible

        Dim oSrcFile As Vblib.OnePic = oSrc.GetFirst
        If oSrcFile Is Nothing Then Return 0

        iCount = 1

        Do
            ' obsługa WP_20221119_10_39_05_Rich.jpg.thumb
            ' raczej nie będzie wtedy JPGa pełnego, więc ignorujemy dokładniejsze testowanie
            If oSrcFile.sSuggestedFilename.EndsWithOrdinal(".thumb") Then
                oSrcFile.sSuggestedFilename = oSrcFile.sSuggestedFilename.Replace(".thumb", "")
            End If

            ' false gdy np. pod tą samą nazwą jest ten sam plik z tą samą zawartością; lub gdy dodanie daty nie pozwala 'unikalnąć' nazwy
            Await Application.GetBuffer.AddFile(oSrcFile)
            oSrcFile = oSrc.GetNext
            If oSrcFile Is Nothing Then Exit Do
            uiProgBar.Value += 1
            iCount += 1
        Loop

        Sequence.ResetPoRetrieve()

        Application.GetBuffer.SaveData()
        oSrc.lastDownload = Date.Now
        Application.GetSourcesList.Save()   ' zmieniona data

        uiProgBar.Value = 0
        uiProgBar.Visibility = Visibility.Collapsed

        Return iCount
    End Function

End Class

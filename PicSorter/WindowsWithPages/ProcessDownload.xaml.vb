
' ściąganie ze źródeł

Imports Vblib
Imports vb14 = Vblib.pkarlibmodule14
Imports pkar.DotNetExtensions
Imports lib_sharingNetwork
Imports pkar
Imports System.Drawing
'Imports Org.BouncyCastle.Crypto
'Imports Org.BouncyCastle.Crypto

Public Class ProcessDownload
    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        vb14.DumpCurrMethod()

        Dim listka As New List(Of lib_PicSource.PicSourceImplement)
        Application.GetSourcesList.ForEach(Sub(x) listka.Add(x))

        If Application.GetShareServers.Count > 0 Then
            ' dodajemy do tego jeszcze peer-serwery
            For Each oSrv As Vblib.ShareServer In Application.GetShareServers
                Dim oNew As New lib_PicSource.PicSourceImplement(PicSourceType.PeerSrv, Nothing)
                oNew.SourceName = oSrv.displayName
                oNew.Path = oSrv.login.ToString
                oNew.enabled = False
                listka.Add(oNew)
            Next
        End If


        uiLista.ItemsSource = listka

        CheckDiskFree()
    End Sub

    Private Function CheckDiskFree() As Boolean
        Dim buffer As String = vb14.GetSettingsString("uiFolderBuffer")
        If buffer <> "" Then

            Dim oDrives = IO.DriveInfo.GetDrives()
            For Each oDrive As IO.DriveInfo In oDrives
                If buffer.StartsWithCIAI(oDrive.Name) Then
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

        If oSrc.Typ <> Vblib.PicSourceType.PeerSrv Then
            If oSrc.currentExif Is Nothing Then oSrc.currentExif = oSrc.defaultExif.Clone
            Dim oWnd As New EditExifTag(oSrc.currentExif, oSrc.SourceName & " (chwilowe)", EditExifTagScope.LimitedToSourceDir, False)
            oWnd.ShowDialog()
        End If


        Dim iCount As Integer
        Application.ShowWait(True)
        iCount = Await RetrieveFilesFromSource(oSrc)
        Application.ShowWait(False)
        If iCount < 0 Then
            vb14.DialogBox("Błąd wczytywania")
            Return
        End If

        Dim iToPurge As Integer = 0
        If oSrc.Typ <> Vblib.PicSourceType.PeerSrv Then iToPurge = oSrc.Purge(False)

        If iToPurge > 0 Then
            If Await vb14.DialogBoxYNAsync($"Wczytałem {iCount} nowości; czy mam zrobić purge? ({iToPurge} plików)") Then
                oSrc.Purge(True)
                vb14.DialogBox($"Done ({iCount} new files).")
            End If
        Else
            If iCount > 0 Then
                vb14.DialogBox($"Done ({iCount} new files).")
            Else
                vb14.DialogBox($"Done - no new files.")
            End If
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
        If Not Application.GetSourcesList.Any(Function(x) x.enabled) Then
            Await vb14.DialogBoxAsync("Ale nic nie zaznaczyłeś...")
            Return
        End If

        If Not CheckDiskFree() Then Return

        ' uproszczona wersja

        If Not Await vb14.DialogBoxYNAsync("Ściągnąć ze wszystkich zaznaczonych źródeł?") Then Return

        For Each oSrc As Vblib.PicSourceBase In Application.GetSourcesList.Where(Function(x) x.enabled)
            Application.ShowWait(True)
            Await RetrieveFilesFromSource(oSrc)
            Application.ShowWait(False)
            oSrc.Purge(True)
        Next

        vb14.DialogBox("Done.")

    End Sub

    Private Async Function RetrieveFilesFromSource(oSrc As Vblib.PicSourceBase) As Task(Of Integer)
        vb14.DumpCurrMethod(oSrc.VolLabel)

        If oSrc.Typ = PicSourceType.PeerSrv Then Return Await RetrieveFromPeer(oSrc)

        Dim iCount As Integer = oSrc.ReadDirectory(Application.GetKeywords.ToFlatList)
        'Await vb14.DialogBoxAsync($"read {iCount} files")
        vb14.DumpMessage($"Read {iCount} files")

        If iCount < 1 Then Return iCount

        uiProgBar.Maximum = iCount
        uiProgBar.Value = 0
        uiProgBar.Visibility = Visibility.Visible

        Dim oSrcFile As Vblib.OnePic = oSrc.GetFirst
        If oSrcFile Is Nothing Then Return 0

        iCount = 1

        Do
            ' obsługa WP_20221119_10_39_05_Rich.jpg.thumb
            ' raczej nie będzie wtedy JPGa pełnego, więc ignorujemy dokładniejsze testowanie
            If IO.Path.GetExtension(oSrcFile.sSuggestedFilename).EqualsCI(".thumb") Then
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

    Private Async Function RetrieveFromPeer(oSrc As PicSourceBase) As Task(Of Integer)
        ' ściągnij przez sieć

        Dim oPeer As Vblib.ShareServer = Application.GetShareServers.FindByGuid(oSrc.Path)

        Dim ret As String = Await httpKlient.TryConnect(oPeer)
        If Not ret.StartsWith("OK") Then
            Vblib.DialogBox($"Cannot connect to {oSrc.SourceName}" & vbCrLf & ret)
            Return -1
        End If

        Dim lista As BaseList(Of Vblib.OnePic) = Await httpKlient.GetPicListBuffer(oPeer)
        If lista Is Nothing Then Return -2
        If lista.Count < 1 Then Return 0

        uiProgBar.Maximum = lista.Count
        uiProgBar.Value = 0
        uiProgBar.Visibility = Visibility.Visible

        Dim iCount As Integer = 1

        For Each oPicek As Vblib.OnePic In lista
            ' kontrola czy pliku już przypadkiem nie ma (wedle suggested filename) - wtedy go pomijamy
            If Application.GetBuffer.GetList.First(Function(x) x.sSuggestedFilename = oPicek.sSuggestedFilename) IsNot Nothing Then Continue For

            oPicek.oContent = Await httpKlient.GetPicDataFromBuff(oPeer, oPicek.InBufferPathName)
            If oPicek.oContent IsNot Nothing Then
                Await Application.GetBuffer.AddFile(oPicek)
                uiProgBar.Value += 1
                iCount += 1
                ' co 10 plików zapisuje dane, na wypadek awarii/zamknięcia app żeby coś było
                If iCount Mod 10 = 0 Then Application.GetBuffer.SaveData()
            End If
        Next

        Sequence.ResetPoRetrieve()

        uiProgBar.Value = 0
        uiProgBar.Visibility = Visibility.Collapsed

        Application.GetBuffer.SaveData()

        Return iCount

    End Function
End Class

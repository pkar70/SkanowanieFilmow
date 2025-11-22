
' ściąganie ze źródeł

Imports Vblib
Imports vb14 = Vblib.pkarlibmodule14
Imports pkar.DotNetExtensions
Imports pkar    ' dla baselist
Imports pkar.UI.Extensions


Public Class ProcessDownload
    ' Inherits ProcessWnd_Base


    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        Log.DumpCurrMethod()
        vb14.DumpCurrMethod()
        Me.InitDialogs
        Me.ProgRingInit(True, True)

        Dim listka As New List(Of lib_PicSource.PicSourceImplement)
        Application.GetSourcesList.ForEach(Sub(x) listka.Add(x))

        If vblib.GetShareServers.Count > 0 Then
            ' dodajemy do tego jeszcze peer-serwery
            For Each oSrv As Vblib.ShareServer In vblib.GetShareServers
                Dim oNew As New lib_PicSource.PicSourceImplement(PicSourceType.PeerSrv, Nothing)
                oNew.SourceName = "peer: " & oSrv.displayName
                oNew.Path = oSrv.login.ToString
                oNew.enabled = False
                listka.Add(oNew)
            Next
        End If


        uiLista.ItemsSource = listka

        CheckDiskFree() ' tylko ostrzeżenie - można zrobić miejsce przed uruchamianiem Source
    End Sub

    ''' <summary>
    ''' Sprawdza czy dysk z buforem ma 100 MB wolnego. Jeśli nie: message
    ''' </summary>
    ''' <returns>TRUE gdy jest 100 MB, else FALSE</returns>
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

        Me.MsgBox("Za mało miejsca na dysku z buforem!")
        Return False
    End Function

    Private Async Sub uiGetThis_Click(sender As Object, e As RoutedEventArgs)
        vb14.DumpCurrMethod()

        If Not CheckDiskFree() Then Return

        ' to konkretne, więc bardziej szczegółowo
        Dim oFE As FrameworkElement = sender
        Dim oSrc As Vblib.PicSourceBase = oFE?.DataContext
        If oSrc Is Nothing Then Return

        If oSrc.Typ = Vblib.PicSourceType.AdHOC OrElse oSrc.Typ = Vblib.PicSourceType.Reel Then
            ' troche bardziej skomplikowane, zeby w oSrc.Path był ostatni naprawdę użyty katalog
            Dim dirToGet As String = SettingsGlobal.FolderBrowser(oSrc.Path, "Select source folder")
            If dirToGet = "" Then Return

            If dirToGet.StartsWith("\\") Then
                If Not Await Me.DialogBoxYNAsync("Czy naprawdę ma byc katalog sieciowy?") Then
                    Return
                End If
            End If


                oSrc.Path = dirToGet

            oSrc.VolLabel = GetVolLabelForPath(dirToGet)

        End If


        If oSrc.Typ <> Vblib.PicSourceType.PeerSrv Then
            If oSrc.currentExif Is Nothing Then
                oSrc.currentExif = oSrc.defaultExif.Clone
            Else
                oSrc.currentExif = oSrc.currentExif.Clone
            End If

            If oSrc.Typ <> PicSourceType.Inet Then
                Dim oWnd As New EditExifTag(oSrc.currentExif, oSrc.SourceName & " (chwilowe)", EditExifTagScope.LimitedToSourceDir, False)
                oWnd.ShowDialog()
                If Not oWnd.DialogResult Then Return
            End If

        End If


        Dim iCount As Integer

        iCount = Await RetrieveFilesFromSource(oSrc)
        If iCount < 0 Then
            Me.MsgBox("Błąd wczytywania")
            Return
        End If

        Await Purguj(True, iCount, oSrc)
        If iCount > 0 Then
            Me.MsgBox($"Done ({iCount} new files).")
        Else
            Me.MsgBox($"Done - no new files.")
        End If

        SprawdzCzyKonczySieSerNo(vb14.GetSettingsInt("lastSerNo"))

    End Sub

    Private Sub SprawdzCzyKonczySieSerNo(serno As Integer)

        Dim iMaxCyfr As Integer = GetSettingsInt("uiSerNoDigits")
        Dim iMaxSerno As Integer = Math.Pow(10, iMaxCyfr)
        If serno * 10 < iMaxSerno Then Return

        ' czyli mamy na pewno nie zero na pierwszym miejscu
        If serno * 1.1 < iMaxSerno Then
            If GetSettingsInt("askedSerNo10") >= iMaxCyfr Then Return

            SetSettingsInt("askedSerNo10", iMaxCyfr)
            Me.MsgBox("Wykorzystujesz już wszystkie cyfry serno, może zwiększ ich liczbę?")
            Return
        End If

        ' mamy już 90 % wykorzystane!
        If GetSettingsInt("askedSerNo90") >= iMaxCyfr Then Return
        SetSettingsInt("askedSerNo90", iMaxCyfr)
        Me.MsgBox("Wykorzystujesz już ponad 90 % puli serno, może zwiększ liczbę cyfr?")

    End Sub

    ''' <summary>
    ''' oSrc.Purge wrapper z obsługą ProgRing
    ''' </summary>
    ''' <param name="bAsk">TRUE gdy pytać o usuwanie, FALSE gdy usuwa bez pytania (GetAll)</param>
    ''' <param name="iCount">Licza plików wczytanych (do wyświetlenia pytania)</param>
    ''' <param name="oSrc">Gdzie robić purge</param>
    ''' <returns></returns>
    Private Async Function Purguj(bAsk As Boolean, iCount As Integer, oSrc As PicSourceBase) As Task
        ' dla Peer nie usuwamy, bo nie mamy jak
        If oSrc.Typ = Vblib.PicSourceType.PeerSrv Then Return

        Dim iToPurge As Integer = oSrc.Purge(False, Nothing)
        If iToPurge = 0 Then Return

        If bAsk And Not Await Me.DialogBoxYNAsync($"Wczytałem {iCount} nowości; czy mam zrobić purge? ({iToPurge} plików)") Then
            Return
        End If

        Me.ProgRingSetMax(iToPurge)
        Me.ProgRingSetVal(0)
        Me.ProgRingShow(True)
        oSrc.Purge(True, Sub() Me.ProgRingInc)
        Me.ProgRingShow(False)
    End Function

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
            Await Me.MsgBoxAsync("Ale nic nie zaznaczyłeś...")
            Return
        End If

        If Not CheckDiskFree() Then Return

        ' uproszczona wersja

        If Not Await Me.DialogBoxYNAsync("Ściągnąć ze wszystkich zaznaczonych źródeł?") Then Return

        For Each oSrc As Vblib.PicSourceBase In Application.GetSourcesList.Where(Function(x) x.enabled)
            Await RetrieveFilesFromSource(oSrc)
            Await Purguj(False, 0, oSrc)
        Next

        Me.MsgBox("Done.")

        SprawdzCzyKonczySieSerNo(vb14.GetSettingsInt("lastSerNo"))


    End Sub

    Private Async Function RetrieveFilesFromSource(oSrc As Vblib.PicSourceBase) As Task(Of Integer)

        vb14.DumpCurrMethod(oSrc.SourceName & ": " & oSrc.VolLabel)

        If ProcessPic.GetBuffer(Me) Is Nothing Then
            ' nie ma bufora, więc nie ma jak ściągać
            Me.MsgBox("Nie znam bufora, nie mogę importować!")
            Return 0
        End If

        Me.ProgRingSetMax(100) 'obojętnie ile, byle teraz nie pokazać paska :)
        Me.ProgRingSetVal(0)
        Me.ProgRingShow(True)

        Dim retval As Integer = 0

        Select Case oSrc.Typ
            Case PicSourceType.Inet
                If ProcessPic.IsDefaultBuff(Me) Then
                    Dim oWnd As New ProcessDownloadInternet(oSrc)
                    oWnd.Owner = Me.Owner
                    Me.Hide()
                    If Not oWnd.ShowDialog() Then
                        retval = 0
                    Else
                        retval = oWnd.Counter
                    End If
                    Me.Show()
                Else
                    Me.MsgBox("Nie umiem do nie-default bufora")
                End If
            Case PicSourceType.PeerSrv
                retval = Await RetrieveFromPeer(oSrc)
            Case PicSourceType.Reel
                Await Me.MsgBoxAsync("Najlepiej z debuggerem, bo coś jest nie tak w renames/target/reel")
                retval = Await RetrieveFromReel(oSrc)
            Case Else
                retval = Await RetrieveFromDisk(oSrc)
        End Select

        Await RunAutoExif()

        'If ProcessPic.IsDefaultBuff(Me) Then SequenceHelper.ResetPoRetrieve()
        ProcessPic.GetBuffer(Me).SetStagesSettings("")

        Me.ProgRingSetText("Saving metadata...")

        ProcessPic.GetBuffer(Me).SaveData()
        If retval > 0 Then

            oSrc.lastDownload = Date.Now

            If oSrc.Typ = PicSourceType.Inet Then
                Dim dataret As String = Await Me.InputBoxAsync("Jaką datę ostatniego pobrania ustawić?", oSrc.lastDownload.ToExifString)
                oSrc.lastDownload = dataret.ParseExifDate(oSrc.lastDownload)
            End If

            Application.GetSourcesList.Save()   ' zmieniona data
        End If

        Me.ProgRingShow(False)

        Return retval
    End Function

    Private Async Function RunAutoExif() As Task
        Me.ProgRingSetText("Reading EXIFs...")
        Me.ProgRingSetVal(0)

        Await ProcessPic.GetBuffer(Me).RunAutoExif

    End Function

    Private Async Function RetrieveFromDisk(oSrc As PicSourceBase) As Task(Of Integer)
        Me.ProgRingSetText($"Dir {oSrc.SourceName}")

        Dim iCount As Integer = oSrc.ReadDirectory()
        'Await vb14.DialogBoxAsync($"read {iCount} files")
        vb14.DumpMessage($"Read {iCount} files")

        If iCount < 1 Then
            Me.ProgRingShow(False)
            Return iCount
        End If

        Me.ProgRingSetMax(iCount)
        Me.ProgRingSetText($"Import from {oSrc.SourceName}")

        Dim oSrcFile As Vblib.OnePic = oSrc.GetFirst
        If oSrcFile Is Nothing Then Return 0

        iCount = 1

        Do
            ' obsługa WP_20221119_10_39_05_Rich.jpg.thumb
            ' raczej nie będzie wtedy JPGa pełnego, więc ignorujemy dokładniejsze testowanie
            If IO.Path.GetExtension(oSrcFile.sSuggestedFilename).EqualsCI(".thumb") Then
                oSrcFile.sSuggestedFilename = oSrcFile.sSuggestedFilename.Replace(".thumb", "")
            End If

            If Not String.IsNullOrWhiteSpace(oSrc.defaultKwds) Then
                ' mogłoby być i bez IFa, bo potrafi się zachować :)
                oSrcFile.ReplaceOrAddExif(vblib.GetKeywords.CreateManualTagFromKwds(oSrc.defaultKwds))
            End If

            ' false gdy np. pod tą samą nazwą jest ten sam plik z tą samą zawartością; lub gdy dodanie daty nie pozwala 'unikalnąć' nazwy
            Await ProcessPic.GetBuffer(Me).AddFile(oSrcFile)
            oSrcFile = oSrc.GetNext
            If oSrcFile Is Nothing Then Exit Do
            Me.ProgRingInc
            iCount += 1
        Loop

        Return iCount
    End Function

    Private Async Function RetrieveFromReel(oSrc As PicSourceBase) As Task(Of Integer)
        Me.ProgRingSetText($"Dir {oSrc.SourceName}")

        Dim iCount As Integer = oSrc.ReadDirectory()
        'Await vb14.DialogBoxAsync($"read {iCount} files")
        vb14.DumpMessage($"Read {iCount} files")

        If iCount < 1 Then
            Me.ProgRingShow(False)
            Return 0
        End If

        Dim wndRenameFolders As New ReelRenames(oSrc.GetInternalDirList, oSrc.Path)
        If Not wndRenameFolders.ShowDialog() Then
            Me.ProgRingShow(False)
            Return 0
        End If


        Me.ProgRingSetMax(iCount)
        Me.ProgRingSetText($"Import from {oSrc.SourceName}")

        Dim oSrcFile As Vblib.OnePic = oSrc.GetFirst
        If oSrcFile Is Nothing Then Return 0

        iCount = 1

        Do
            ' obsługa WP_20221119_10_39_05_Rich.jpg.thumb
            ' raczej nie będzie wtedy JPGa pełnego, więc ignorujemy dokładniejsze testowanie
            If IO.Path.GetExtension(oSrcFile.sSuggestedFilename).EqualsCI(".thumb") Then
                oSrcFile.sSuggestedFilename = oSrcFile.sSuggestedFilename.Replace(".thumb", "")
            End If

            If Not String.IsNullOrWhiteSpace(oSrc.defaultKwds) Then
                ' mogłoby być i bez IFa, bo potrafi się zachować :)
                oSrcFile.ReplaceOrAddExif(vblib.GetKeywords.CreateManualTagFromKwds(oSrc.defaultKwds))
            End If

            ' wszystkie zmiany wynikające z rename
            wndRenameFolders.RenamesInOnePic(oSrcFile)

            ' false gdy np. pod tą samą nazwą jest ten sam plik z tą samą zawartością; lub gdy dodanie daty nie pozwala 'unikalnąć' nazwy
            Await ProcessPic.GetBuffer(Me).AddFile(oSrcFile)
            oSrcFile = oSrc.GetNext
            If oSrcFile Is Nothing Then Exit Do
            Me.ProgRingInc
            iCount += 1
        Loop

        Return iCount
    End Function

    Private Async Function RetrieveFromPeer(oSrc As PicSourceBase) As Task(Of Integer)
        ' ściągnij przez sieć

        Dim oPeer As Vblib.ShareServer = vblib.GetShareServers.FindByGuid(oSrc.Path)

        Dim ret As String = Await lib14_httpClnt.httpKlient.TryConnect(oPeer)
        If Not ret.StartsWith("OK") Then
            Me.MsgBox($"Cannot connect to {oSrc.SourceName}" & vbCrLf & ret)
            Return -1
        End If

        Me.ProgRingSetText($"Dir {oSrc.SourceName}...")

        Dim lista As BaseList(Of Vblib.OnePic) = Await lib14_httpClnt.httpKlient.GetPicListBuffer(oPeer)
        If lista Is Nothing Then Me.ProgRingShow(False)

        If lista.Count < 1 Then Return 0

        Me.ProgRingSetMax(lista.Count)
        Me.ProgRingSetText($"Import from {oSrc.SourceName}")

        Dim iCount As Integer = 1

        For Each oPicek As Vblib.OnePic In lista
            ' kontrola czy pliku już przypadkiem nie ma (wedle suggested filename) - wtedy go pomijamy
            If ProcessPic.GetBuffer(Me).GetList.First(Function(x) x.sSuggestedFilename = oPicek.sSuggestedFilename) IsNot Nothing Then Continue For

            oPicek.oContent = Await lib14_httpClnt.httpKlient.GetPicDataFromBuff(oPeer, oPicek.InBufferPathName)
            If oPicek.oContent IsNot Nothing Then
                oPicek.sharingFromGuid &= $";L:{oPeer.login}:{oPicek.serno}"
                oPicek.serno = 0 ' muszę nadać swój

                If Not String.IsNullOrWhiteSpace(oSrc.defaultKwds) Then
                    ' mogłoby być i bez IFa, bo potrafi się zachować :)
                    oPicek.ReplaceOrAddExif(vblib.GetKeywords.CreateManualTagFromKwds(oSrc.defaultKwds))
                End If


                Await ProcessPic.GetBuffer(Me).AddFile(oPicek)
                Await Me.ProgRingInc
                iCount += 1
                ' co 10 plików zapisuje dane, na wypadek awarii/zamknięcia app żeby coś było
                If iCount Mod 10 = 0 Then ProcessPic.GetBuffer(Me).SaveData()
            End If
        Next

        Return iCount

    End Function
End Class


Public Class KonwersjaDateTime
    Inherits ValueConverterOneWaySimple

    Protected Overrides Function Convert(value As Object) As Object
        Dim datka As DateTime = CType(value, DateTime)
        If Not datka.IsDateValid Then Return "??"

        Return "last run: " & datka.ToExifString
    End Function
End Class

' ściąganie ze źródeł

Imports Vblib
Imports vb14 = Vblib.pkarlibmodule14
Imports pkar.DotNetExtensions
Imports lib_sharingNetwork
Imports pkar
Imports System.Drawing
Imports pkar.UI.Extensions
Imports System.ComponentModel.Design

Public Class ProcessDownload
    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        vb14.DumpCurrMethod()
        Me.InitDialogs
        Me.ProgRingInit(True, True)

        Dim listka As New List(Of lib_PicSource.PicSourceImplement)
        Application.GetSourcesList.ForEach(Sub(x) listka.Add(x))

        If Application.GetShareServers.Count > 0 Then
            ' dodajemy do tego jeszcze peer-serwery
            For Each oSrv As Vblib.ShareServer In Application.GetShareServers
                Dim oNew As New lib_PicSource.PicSourceImplement(PicSourceType.PeerSrv, Nothing)
                oNew.SourceName = "peer: " & oSrv.displayName
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

        If oSrc.Typ = Vblib.PicSourceType.AdHOC Then
            ' troche bardziej skomplikowane, zeby w oSrc.Path był ostatni naprawdę użyty katalog
            Dim dirToGet As String = SettingsGlobal.FolderBrowser(oSrc.Path, "Select source folder")
            If dirToGet = "" Then Return
            oSrc.Path = dirToGet

            oSrc.VolLabel = GetVolLabelForPath(dirToGet)

        End If

        If oSrc.Typ <> Vblib.PicSourceType.PeerSrv Then
            If oSrc.currentExif Is Nothing Then
                oSrc.currentExif = oSrc.defaultExif.Clone
            Else
                oSrc.currentExif = oSrc.currentExif.Clone
            End If
            Dim oWnd As New EditExifTag(oSrc.currentExif, oSrc.SourceName & " (chwilowe)", EditExifTagScope.LimitedToSourceDir, False)
            oWnd.ShowDialog()
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

    End Sub

    Private Async Function RetrieveFilesFromSource(oSrc As Vblib.PicSourceBase) As Task(Of Integer)
        vb14.DumpCurrMethod(oSrc.VolLabel)

        If oSrc.Typ = PicSourceType.PeerSrv Then Return Await RetrieveFromPeer(oSrc)

        Me.ProgRingSetMax(100) 'obojętnie ile, byle teraz nie pokazać paska :)
        Me.ProgRingSetVal(0)
        Me.ProgRingShow(True)
        Me.ProgRingSetText($"Dir {oSrc.SourceName}")

        Dim iCount As Integer = oSrc.ReadDirectory(Application.GetKeywords.ToFlatList)
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

            ' false gdy np. pod tą samą nazwą jest ten sam plik z tą samą zawartością; lub gdy dodanie daty nie pozwala 'unikalnąć' nazwy
            Await Application.GetBuffer.AddFile(oSrcFile)
            oSrcFile = oSrc.GetNext
            If oSrcFile Is Nothing Then Exit Do
            Me.ProgRingInc
            iCount += 1
        Loop

        Await RunAutoExif()

        SequenceHelper.ResetPoRetrieve()

        Me.ProgRingSetText("Saving metadata...")

        Application.GetBuffer.SaveData()
        oSrc.lastDownload = Date.Now
        Application.GetSourcesList.Save()   ' zmieniona data

        Me.ProgRingShow(False)

        Return iCount
    End Function

    Private Async Function RunAutoExif() As Task
        Me.ProgRingSetText("Reading EXIFs...")
        Me.ProgRingSetVal(0)

        Dim oEngine As AutotaggerBase = Application.gAutoTagery.Where(Function(x) x.Nazwa = Vblib.ExifSource.FileExif).ElementAt(0)
        ' się nie powinno zdarzyć, no ale cóż...
        If oEngine Is Nothing Then Return

        Dim iSerNo As Integer = vb14.GetSettingsInt("lastSerNo")

        For Each oItem As Vblib.OnePic In Application.GetBuffer.GetList

            ' najpierw Serial Number zrobimy - obojętnie co dalej...
            If oItem.serno < 1 Then
                iSerNo += 1
                oItem.serno = iSerNo
            End If

            If Not IO.File.Exists(oItem.InBufferPathName) Then Continue For   ' zabezpieczenie przed samoznikaniem

            ' niby nie ma prawa być, chyba że to Peer
            If oItem.GetExifOfType(oEngine.Nazwa) Is Nothing Then
                Try
                    Dim oExif As Vblib.ExifTag = Await oEngine.GetForFile(oItem)
                    If oExif IsNot Nothing Then
                        oItem.ReplaceOrAddExif(oExif)
                        oItem.TagsChanged = True
                    End If
                    'Await Task.Delay(1) ' na wszelki wypadek, żeby był czas na przerysowanie progbar, nawet jak tworzenie EXIFa jest empty
                Catch ex As Exception
                    ' zabezpieczenie, żeby mi na pewno nie zrobił crash przy nadawaniu serno!
                End Try
            End If
            Me.ProgRingInc
        Next

        vb14.SetSettingsInt("lastSerNo", iSerNo)
    End Function

    Private Async Function RetrieveFromPeer(oSrc As PicSourceBase) As Task(Of Integer)
        ' ściągnij przez sieć

        Me.ProgRingSetMax(100) 'obojętnie ile, byle teraz nie pokazać paska :)
        Me.ProgRingSetVal(0)
        Me.ProgRingShow(True)
        Me.ProgRingSetText($"Connecting to {oSrc.SourceName}..")


        Dim oPeer As Vblib.ShareServer = Application.GetShareServers.FindByGuid(oSrc.Path)

        Dim ret As String = Await lib14_httpClnt.httpKlient.TryConnect(oPeer)
        If Not ret.StartsWith("OK") Then
            Me.MsgBox($"Cannot connect to {oSrc.SourceName}" & vbCrLf & ret)
            Me.ProgRingShow(False)
            Return -1
        End If

        Me.ProgRingSetText($"Dir {oSrc.SourceName}...")

        Dim lista As BaseList(Of Vblib.OnePic) = Await lib14_httpClnt.httpKlient.GetPicListBuffer(oPeer)
        If lista Is Nothing Then
            Me.ProgRingShow(False)
            Return -2
        End If
        If lista.Count < 1 Then
            Me.ProgRingShow(False)
            Return 0
        End If

        Me.ProgRingSetMax(lista.Count)
        Me.ProgRingSetText($"Import from {oSrc.SourceName}")

        Dim iCount As Integer = 1

        For Each oPicek As Vblib.OnePic In lista
            ' kontrola czy pliku już przypadkiem nie ma (wedle suggested filename) - wtedy go pomijamy
            If Application.GetBuffer.GetList.First(Function(x) x.sSuggestedFilename = oPicek.sSuggestedFilename) IsNot Nothing Then Continue For

            oPicek.oContent = Await lib14_httpClnt.httpKlient.GetPicDataFromBuff(oPeer, oPicek.InBufferPathName)
            If oPicek.oContent IsNot Nothing Then
                oPicek.sharingFromGuid &= $";L:{oPeer.login}:{oPicek.serno}"
                oPicek.serno = 0 ' muszę nadać swój
                Await Application.GetBuffer.AddFile(oPicek)
                Await Me.ProgRingInc
                iCount += 1
                ' co 10 plików zapisuje dane, na wypadek awarii/zamknięcia app żeby coś było
                If iCount Mod 10 = 0 Then Application.GetBuffer.SaveData()
            End If
        Next

        Await RunAutoExif()

        Me.ProgRingSetText("Saving metadata...")

        SequenceHelper.ResetPoRetrieve()

        Application.GetBuffer.SaveData()

        Me.ProgRingShow(False)

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
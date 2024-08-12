

Imports Vblib
Imports vb14 = Vblib.pkarlibmodule14
Imports pkar
Imports pkar.DotNetExtensions
Imports System.IO
Imports pkar.UI.Extensions
Imports System.ComponentModel   ' dla PropertyChangedEventHandler i podobnych
Imports System.Linq
Imports Windows.UI.Notifications
Imports PicSorterNS.BrowseFullSearch
Imports Newtonsoft.Json
Imports System.Windows.Controls.Primitives
'Imports System.Windows.Forms
'Imports Windows.ApplicationModel.Background
'Imports Microsoft.EntityFrameworkCore.Internal

Public Class ProcessBrowse

    ' Private Const THUMBS_LIMIT As Integer = 9999

    Private _thumbsy As New System.Collections.ObjectModel.ObservableCollection(Of ThumbPicek)
    Private _iMaxRun As Integer  ' po wczytaniu: liczba miniaturek, później: max ciąg zdjęć
    Private _redrawPending As Boolean = False
    Private _oBufor As Vblib.IBufor
    'Private _inArchive As Boolean  ' to będzie wyłączać różne funkcjonalności
    Private _title As String
    Private _memSizeKb As Integer

    ' Private _MetadataWindow As ShowExifs

#Region "called on init"

    ''' <summary>
    ''' przeglądarka na liście plików BUFOR, w pełnej wersji bądź ograniczonej do view (czyli gdy już na archiwum a nie na bufurze wejściowym)
    ''' </summary>
    ''' <param name="bufor"></param>
    ''' <param name="onlyBrowse"></param>
    Public Sub New(bufor As Vblib.IBufor, title As String)
        vb14.DumpCurrMethod()

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        _oBufor = bufor
        '_inArchive = onlyBrowse

        _title = title
    End Sub

    Private Sub MenuActionReadOnly()
        Dim oVis As Visibility = If(_oBufor.GetIsReadonly, Visibility.Collapsed, Visibility.Visible)
        ' jedynie dla menu w Action to zadziała, menu context obrazka - nie jest dostępne
        uiDeleteSelected.Visibility = oVis
        uiMenuAutotags.Visibility = oVis
        uiDescribeSelected.Visibility = oVis
        uiGeotagSelected.Visibility = oVis
        'uiMenuDateRefit.Visibility = oVis
        uiBatchProcessors.Visibility = oVis
        uiActionTargetDir.Visibility = oVis
        uiDeleteThumbsSelected.Visibility = oVis
    End Sub

    Private Shared Function GetMegabytes() As Long
        'Return GC.GetTotalAllocatedBytes(False) / 1024 / 1024
        Return Process.GetCurrentProcess.WorkingSet64 / 1024 / 1024
    End Function

    Private Async Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        vb14.DumpCurrMethod()
        Me.ProgRingInit(True, False)

        Me.ProgRingShow(True)
        'Application.ShowWait(True)

        ' przenoszę na początek, żeby nie wczytywać tysiąca obrazków które już są do usunięcia
        Await EwentualneKasowanieArchived()

        Dim initMem As Integer = GetMegabytes() ' Windows.System.MemoryManager.AppMemoryUsage

        Await Bufor2Thumbsy()   ' w tym obsługa znikniętych
        SizeMe()

        _memSizeKb = GetMegabytes() - initMem + 1

        Await SortujThumbsy()   ' proszę posortować - robimy to tylko po zmianach dat!
        RefreshMiniaturki(True)
        'PokazThumbsy() ' tylko test, czy observable zadziała

        'WypelnMenuFilterSharing()
        'WypelnMenuActionSharing()

        ' zaczynamy od wygaszonego stereopack - dla archiwum, oraz gdy nie znamy SPM
        uiStereoPack.Visibility = Visibility.Collapsed

        If _oBufor.GetIsReadonly Then
            ' działamy na archiwum

            uiFilterNoTarget.Visibility = Visibility.Collapsed
            uiFilterNoDescr.Visibility = Visibility.Collapsed

            uiSplit.IsEnabled = False

            uiOknaTargetDir.Visibility = Visibility.Collapsed
            MenuActionReadOnly()
        Else
            ' działamy na buforze - wszystkie akcje dozwolone
            uiSplit.IsEnabled = True

            ' menu dodatkowych okien
            uiOknaTargetDir.Visibility = Visibility.Visible

            'WypelnMenuBatchProcess(uiBatchProcessors, AddressOf PostProcessRun)
            'WypelnMenuAutotagerami(uiMenuAutotags, AddressOf AutoTagRun)

            uiFilterNoTarget.Visibility = Visibility.Visible
            MenuActionReadOnly()

            ' to ma sens tylko wtedy gdy wiadomo jak wywołać SPM
            If Not String.IsNullOrEmpty(vb14.GetSettingsString("uiStereoSPMPath")) Then
                uiStereoPack.Visibility = Visibility.Visible
            End If

            Await EwentualneKasowanieBak()
            'Await EwentualneKasowanieArchived()
        End If


        Me.ProgRingShow(False)

        If Vblib.GetShareDescriptionsIn.Count > 0 Then
            If Await Me.DialogBoxYNAsync("Są nadesłane opisy, włączyć teraz odpowiedni filtr?") Then
                uiFilterRemoteDesc_Click(Nothing, Nothing)
            End If
        End If

        UstawMenuStages(uiFilterStageMenuExact, 0)
        UstawMenuStages(uiFilterStageMenuBelow, -1)
        UstawMenuStages(uiFilterStageMenuNot, -99)

        'Application.ShowWait(False)

        'DescriptionToDescription
    End Sub

    Private Sub UstawMenuStages(gdzie As MenuItem, tryb As Integer)

        gdzie.Items.Clear()

        For Each stage As SequenceStageBase In Globs.SequenceCheckers.OrderBy(Of Integer)(Function(x) x.StageNo)
            Dim oNew As New MenuItem
            oNew.Header = stage.Nazwa
            oNew.CommandParameter = tryb
            oNew.DataContext = stage
            oNew.Icon = stage.Icon
            AddHandler oNew.Click, AddressOf uiFilterStage_Click
            gdzie.Items.Add(oNew)
        Next

    End Sub

    Public Function GetPicCount() As Integer
        Return _thumbsy.Count
    End Function

    ''' <summary>
    ''' przepisanie DESCRIPT.ION:UserComment do Pic.Descriptions
    ''' </summary>
    Private Sub DescriptionToDescription()
        For Each picek In _oBufor.GetList
            Debug.WriteLine("Picek " & picek.sSuggestedFilename)
            Dim oExif As Vblib.ExifTag = picek.GetExifOfType(Vblib.ExifSource.SourceDescriptIon)
            If oExif Is Nothing Then Continue For

            If String.IsNullOrEmpty(oExif.UserComment) Then Continue For

            If picek.GetSumOfDescriptionsText.ContainsCI(oExif.UserComment) Then Continue For

            picek.AddDescription(New Vblib.OneDescription(oExif.UserComment, ""))
        Next

        _oBufor.SaveData()
    End Sub


    ' to jest w związku z DEL w ShowBig
    Private Sub Window_GotFocus(sender As Object, e As RoutedEventArgs)
        If Not _redrawPending Then Return

        _redrawPending = False
        RefreshMiniaturki(True)
    End Sub

    Private Async Function EwentualneKasowanieBak() As Task

        Dim iDelay As Integer = vb14.GetSettingsInt("uiBakDelayDays")

        Dim iOutdated As Integer = _oBufor.BakDelete(iDelay, False)
        If iOutdated < 1 Then Return

        If Await Me.DialogBoxYNAsync($"Skasować stare pliki BAK? ({iOutdated})") Then Return

        _oBufor.BakDelete(iDelay, True)

    End Function

    Private _niekasujArchived As Boolean

    Private Async Function EwentualneKasowanieArchived() As Task
        If _oBufor.GetIsReadonly Then Return
        If _niekasujArchived Then Return

        Dim iArchCount As Integer = Application.GetArchivesList.Count
        Dim iCloudArchCount As Integer = Application.GetCloudArchives.GetList.Count

        If iArchCount + iCloudArchCount < 1 Then Return ' jeśli nie mamy żadnego zdefiniowanego, to nie kasujemy i tak

        Dim lista As New List(Of Vblib.OnePic)
        For Each oPic As Vblib.OnePic In _oBufor.GetList
            If oPic.NoPendingAction(iArchCount, iCloudArchCount) Then lista.Add(oPic)
        Next

        If lista.Count < 1 Then Return

        If Not Await Me.DialogBoxYNAsync($"Skasować pliki już w pełni zarchiwizowane? ({lista.Count})") Then
            ' i nie pytaj więcej :)
            _niekasujArchived = True
            Return
        End If

        For Each oPic As Vblib.OnePic In lista
            DeletePicture(oPic)
        Next

        SaveMetaData()

    End Function

    ''' <summary>
    ''' zmiana rozmiaru Window na prawie cały ekran
    ''' </summary>
    Private Sub SizeMe()
        vb14.DumpCurrMethod()

        Me.Width = SystemParameters.FullPrimaryScreenWidth * 0.9
        Me.Height = SystemParameters.FullPrimaryScreenHeight * 0.9
    End Sub


    Private Async Function WczytajIndeks() As Task(Of List(Of ThumbPicek))
        vb14.DumpCurrMethod()

        Dim lista As New List(Of ThumbPicek)

        Dim iMaxBok As Integer = GetMaxBok(_iMaxRun)

        Dim lDeleted As New List(Of Vblib.OnePic)

        For Each oPic As Vblib.OnePic In _oBufor.GetList
            If Not IO.File.Exists(oPic.InBufferPathName) Then
                ' zabezpieczenie przed samoznikaniem - nie ma, to kasujemy z listy naszych plikow
                lDeleted.Add(oPic)
                vb14.DumpMessage("Znikniety plik: " & oPic.InBufferPathName)
                Continue For
            End If

            Dim oThumb As New ThumbPicek(oPic, iMaxBok)

            oThumb.dateMin = oPic.GetMostProbablyDate
            uiProgBar.Value += 1
            lista.Add(oThumb)

        Next

        If lDeleted.Count < 1 Then Return lista

        If Await Me.DialogBoxYNAsync($"Niektóre pliki są zniknięte ({lDeleted.Count}), skopiować do clipboard ich listę??") Then
            Dim sNames As String = ""
            For Each oItem As Vblib.OnePic In lDeleted
                sNames = sNames & vbCrLf & oItem.sSuggestedFilename
            Next

            vb14.ClipPut(sNames)
        End If

        If Await Me.DialogBoxYNAsync($"Mam je usunąć z indeksu?") Then
            For Each oItem As Vblib.OnePic In lDeleted
                _oBufor.GetList.Remove(oItem)
            Next
            SaveMetaData()
        End If

        Return lista
    End Function

    Private Async Function DoczytajMiniaturki() As Task
        vb14.DumpCurrMethod()

        uiProgBar.Value = 0
        uiProgBar.Visibility = Visibility.Visible

        For Each oItem As ThumbPicek In _thumbsy
            Await oItem.ThumbWczytajLubStworz(_oBufor.GetIsReadonly) 'DoczytajMiniaturke(bCacheThumbs, oItem)
            Await Task.Delay(1) ' na potrzeby ProgressBara
            uiProgBar.Value += 1
        Next

    End Function


    ''' <summary>
    ''' przetworzenie danych Bufor na własną listę (thumbsów), sortowane wg thumbpic.datemin = onepic.mostprobably
    ''' </summary>
    Private Async Function Bufor2Thumbsy() As Task
        vb14.DumpCurrMethod()

        _iMaxRun = _oBufor.Count.Max(1)

        uiProgBar.Maximum = _iMaxRun
        uiProgBar.Visibility = Visibility.Visible

        _thumbsy.Clear()
        Dim iMax As Integer = vb14.GetSettingsInt("uiMaxThumbs")
        If iMax < 10 Then iMax = 100

        Dim lista As List(Of ThumbPicek) = Await WczytajIndeks()   ' tu ewentualne kasowanie jest znikniętych, to wymaga YNAsync

        If lista.Count > iMax Then
            Await Me.MsgBoxAsync($"Wczytuję miniaturki tylko {iMax} (z {lista.Count})")
        End If

        Dim nrkol As Integer = 1
        For Each oItem As ThumbPicek In From c In lista Order By c.dateMin Take iMax
            oItem.nrkol = nrkol
            oItem.maxnum = Math.Min(iMax, lista.Count)
            _thumbsy.Add(oItem)
            nrkol += 1
        Next
        lista.Clear()

        uiProgBar.Value = 0
        Await DoczytajMiniaturki()

        uiProgBar.Visibility = Visibility.Collapsed

    End Function

    Private Sub Window_Closing(sender As Object, e As System.ComponentModel.CancelEventArgs)
        uiPicList.ItemsSource = Nothing

        For Each oPicek As ThumbPicek In _thumbsy
            oPicek.oImageSrc = Nothing
        Next

        SaveMetaData(True)  '  po Describe, OCR, i tak dalej - lepiej zapisać nawet jak nie było zmian niż je zgubić

        GC.Collect()    ' usuwamy, bo dużo pamięci zwolniliśmy
    End Sub


#End Region

#Region "górny toolbox"

    ''' <summary>
    ''' zwraca tryb sortowania: 1 daty, 2 serno, 3 filename
    ''' </summary>
    ''' <returns></returns>
    Private Function GetCurrentSortMode() As Integer
        Dim oCBI As ComboBoxItem = TryCast(uiSortBy.SelectedItem, ComboBoxItem)
        If oCBI IsNot Nothing Then
            Return oCBI.DataContext
        End If

        Return 1
    End Function

    ''' <summary>
    ''' ustaw ItemsSource na thumbsy wedle daty - na start, i po zmianach dat - SLOW!
    ''' </summary>
    Private Async Function SortujThumbsy() As Task
        If uiPicList Is Nothing Then Return ' tak jest na początku, z uiSortBy_SelectionChanged
        uiPicList.ItemsSource = Nothing

        Dim sortmode As Integer = GetCurrentSortMode()

        Me.ProgRingShow(True)

        Await Task.Run(Sub()
                           Select Case sortmode
                               Case 2 ' serno
                                   _thumbsy = New ObjectModel.ObservableCollection(Of ThumbPicek)(From c In _thumbsy Where c.bVisible Order By c.oPic.serno)
                               Case 3 ' 3=filename
                                   _thumbsy = New ObjectModel.ObservableCollection(Of ThumbPicek)(From c In _thumbsy Where c.bVisible Order By c.oPic.sSuggestedFilename)
                               Case Else ' 1=date
                                   _thumbsy = New ObjectModel.ObservableCollection(Of ThumbPicek)(From c In _thumbsy Where c.bVisible Order By c.oPic.GetMostProbablyDate)
                           End Select
                       End Sub
            )

        uiPicList.ItemsSource = _thumbsy
        Me.ProgRingShow(False)

    End Function

    Private Async Sub uiSortBy_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        Await SortujThumbsy()
    End Sub

    Private Sub uiOpenHistoragam_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New HistogramWindow(_oBufor)
        oWnd.Show()
    End Sub
#End Region

#Region "menu actions"

    'Public Function WypelnMenuActionSharing() As Integer
    '    uiActionUploadMenu.Items.Clear()


    '    Dim iCnt As Integer = 0

    '    For Each oLogin As Vblib.ShareServer In Application.GetShareServers.GetList

    '        Dim oNew As New MenuItem
    '        oNew.Header = oLogin.displayName
    '        oNew.DataContext = oLogin

    '        AddHandler oNew.Click, AddressOf ActionSharingServer

    '        uiActionUploadMenu.Items.Add(oNew)
    '        iCnt += 1
    '    Next

    '    uiActionUploadMenu.Visibility = If(iCnt > 0, Visibility.Visible, Visibility.Collapsed)
    '    uiSeparatorActionUpload.Visibility = If(iCnt > 0, Visibility.Visible, Visibility.Collapsed)
    '    Return iCnt
    'End Function

    'Private Async Sub ActionSharingServer(sender As Object, e As RoutedEventArgs)
    '    uiActionsPopup.IsOpen = False

    '    Dim oFE As FrameworkElement = sender
    '    Dim oPicSortSrv As Vblib.ShareServer = oFE?.DataContext
    '    If oPicSortSrv Is Nothing Then Return

    '    Dim sRet As String = Await lib_sharingNetwork.httpKlient.TryConnect(oPicSortSrv)
    '    If Not sRet.StartsWith("OK") Then
    '        Vblib.DialogBox("Błąd podłączenia do serwera: " & sRet)
    '        Return
    '    End If

    '    sRet = Await lib_sharingNetwork.httpKlient.CanUpload(oPicSortSrv)
    '    If Not sRet.StartsWith("YES") Then
    '        Vblib.DialogBox("Upload jest niedostępny: " & sRet)
    '        Return
    '    End If

    '    uiProgBar.Value = 0
    '    uiProgBar.Maximum = uiPicList.SelectedItems.Count
    '    uiProgBar.Visibility = Visibility.Visible

    '    Dim allErrs As String = ""
    '    For Each oThumb As ThumbPicek In uiPicList.SelectedItems
    '        Dim oPic As Vblib.OnePic = oThumb.oPic

    '        If oPic.sharingLockSharing Then
    '            allErrs &= oPic.sSuggestedFilename & " is excluded from sharing, ignoring" & vbCrLf
    '        Else

    '            oPic.ResetPipeline()
    '            Dim ret As String = Await oPic.RunPipeline(oPicSortSrv.uploadProcessing, Application.gPostProcesory)
    '            If ret <> "" Then
    '                ' jakiś błąd
    '                allErrs &= ret & vbCrLf
    '            Else
    '                ' pipeline OK
    '                ret = Await lib_sharingNetwork.httpKlient.UploadPic(oPicSortSrv, oPic)
    '                allErrs &= ret & vbCrLf
    '            End If

    '        End If

    '        oPic.ResetPipeline() ' zwolnienie streamów, readerów, i tak dalej
    '        uiProgBar.Value += 1
    '    Next

    '    uiProgBar.Visibility = Visibility.Visible

    '    If allErrs <> "" Then
    '        Vblib.ClipPut(allErrs)
    '        Vblib.DialogBox(allErrs)
    '    End If

    'End Sub

    Private Shared Function GetDateBetween(oDate1 As Date, oDate2 As Date) As Date
        Dim minutes As Integer = Math.Abs((oDate1 - oDate2).TotalMinutes)
        If oDate1 < oDate2 Then Return oDate1.AddMinutes(minutes / 2)
        Return oDate2.AddMinutes(minutes / 2)
    End Function

    'Private Sub uiSetTargetDir_Click(sender As Object, e As RoutedEventArgs)
    '    uiActionsPopup.IsOpen = False

    '    If uiPicList.SelectedItems.Count < 1 Then Return

    '    Dim lSelected As New List(Of ThumbPicek)
    '    For Each oThumb As ThumbPicek In uiPicList.SelectedItems
    '        lSelected.Add(oThumb)
    '    Next

    '    Dim oWnd As New TargetDir(_thumbsy.ToList, lSelected)
    '    If Not oWnd.ShowDialog Then Return

    '    If _isTargetFilterApplied Then
    '        For Each oThumb As ThumbPicek In uiPicList.SelectedItems
    '            oThumb.opacity = _OpacityWygas
    '        Next
    '    End If

    '    ' pokaz na nowo obrazki
    '    ReDymkuj()
    '    RefreshMiniaturki(False)

    '    SaveMetaData()
    'End Sub

    Private Sub uiActionLock_Click(sender As Object, e As RoutedEventArgs)
        'uiActionsPopup.IsOpen = False

        If uiPicList.SelectedItems.Count < 1 Then Return

        For Each oItem As ThumbPicek In uiPicList.SelectedItems
            oItem.oPic.locked = True
        Next

    End Sub


    Private Sub uiActionSelectFilter_Click(sender As Object, e As RoutedEventArgs)
        'uiActionsPopup.IsOpen = False

        uiPicList.SelectedItems.Clear()

        For Each oItem As ThumbPicek In uiPicList.ItemsSource
            If oItem.opacity = 1 Then uiPicList.SelectedItems.Add(oItem)
        Next

    End Sub

#End Region

    Private Sub uiMetadataChanged(sender As Object, e As EventArgs)
        'uiActionsPopup.IsOpen = False
        SaveMetaData()
    End Sub

    Private Sub uiMetadataChangedResort(sender As Object, e As EventArgs)
        ' tu wskakuje po zmianie daty zdjęć...
        'uiActionsPopup.IsOpen = False

        ReDymkuj()

        ' jeśli sortowanie jest wg dat, to zaktualizuj
        If GetCurrentSortMode() = 1 Then SortujThumbsy()

        SaveMetaData()
    End Sub

    Public Function GetSelectedThumbs() As List(Of ThumbPicek)
        If uiPodpisCheckbox.IsChecked Then
            Return _thumbsy.Where(Function(x) x.IsChecked).ToList
        Else
            Dim ret As New List(Of ThumbPicek)
            For Each oItem As ThumbPicek In uiPicList.SelectedItems
                ret.Add(oItem)
            Next
            Return ret
        End If
    End Function

    Public Function GetAllThumbs() As List(Of ThumbPicek)
        Return _thumbsy.ToList
    End Function

    Private Sub uiTargetMetadataChanged(sender As Object, e As EventArgs)

        uiMetadataChangedDymkuj(Nothing, Nothing)

        ' teoretycznie się wygasi / zgasi samo z siebie (via Notify)
        If _isTargetFilterApplied Then
            ' mogliśmy zarówno skasować Target, jak i nadać
            For Each oItem As ThumbPicek In uiPicList.SelectedItems
                If String.IsNullOrWhiteSpace(oItem.TargetDir) Then
                    oItem.opacity = 1
                Else
                    oItem.opacity = _OpacityWygas
                End If
            Next
        End If
    End Sub
    Private Sub uiGeotagMetadataChanged(sender As Object, e As EventArgs)
        uiMetadataChangedDymkuj(Nothing, Nothing)
        ' tu trzeba wraz z reapply filter
        If _isGeoFilterApplied Then uiFilterNoGeo_Click(Nothing, Nothing)
    End Sub

    Private Sub uiMetadataChangedReparse(sender As Object, e As EventArgs)
        uiMetadataChangedDymkuj(Nothing, Nothing)
        SortujThumbsy()
        RefreshMiniaturki(True)
    End Sub

    Private Sub uiMetadataChangedDymkuj(sender As Object, e As EventArgs)
        'uiActionsPopup.IsOpen = False
        ReDymkuj()
        SaveMetaData()
    End Sub

    Private Sub uiMetadataChangedDescribe(sender As Object, e As EventArgs)
        'uiActionsPopup.IsOpen = False
        For Each oItem As ThumbPicek In uiPicList.SelectedItems
            oItem.ZrobDymek()
            oItem.NotifyPropChange("sumOfDescr")
        Next

        SaveMetaData()
    End Sub


    'Private Sub uiCopyOut_Click(sender As Object, e As System.Windows.RoutedEventArgs)
    '    uiActionsPopup.IsOpen = False

    '    Dim sFolder As String = SettingsGlobal.FolderBrowser("", "Gdzie skopiować pliki?")
    '    If sFolder = "" Then Return
    '    If Not IO.Directory.Exists(sFolder) Then Return

    '    Dim iErrCount As Integer = 0
    '    For Each oThumb As ThumbPicek In uiPicList.SelectedItems
    '        Try
    '            IO.File.Copy(oThumb.oPic.InBufferPathName, IO.Path.Combine(sFolder, oPic.oPic.sSuggestedFilename))
    '        Catch ex As Exception
    '            iErrCount += 1
    '        End Try
    '    Next

    '    If iErrCount < 1 Then Return

    '    vb14.DialogBox($"{iErrCount} errors while copying")

    'End Sub

    'Private Sub uiCopyClip_Click(sender As Object, e As System.Windows.RoutedEventArgs)
    '    uiActionsPopup.IsOpen = False

    '    Clipboard.Clear()
    '    Dim lista As New Specialized.StringCollection
    '    For Each oTB As ThumbPicek In uiPicList.SelectedItems
    '        lista.Add(oTB.oPic.InBufferPathName)
    '    Next

    '    Clipboard.SetFileDropList(lista)

    '    vb14.DialogBox("Files in Clipboard")

    'End Sub

    Private _lastMouseDownTime As Integer
    Private _lastMouseMovePosition As Point
    Private _dragDropCreated As Boolean

    Private Sub uiPicList_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles uiPicList.MouseDown
        ' nie wchodzi tu?
        MyBase.OnMouseDown(e)

        vb14.DumpCurrMethod()

        If Not e.LeftButton = MouseButtonState.Pressed Then Return
        _lastMouseDownTime = e.Timestamp
        vb14.DumpMessage($"lastMouseDownTime = {_lastMouseDownTime}")
    End Sub

    Private Sub uiPicList_MouseMove(sender As Object, e As MouseEventArgs) Handles uiPicList.MouseMove
        MyBase.OnMouseMove(e)

        If Not e.LeftButton = MouseButtonState.Pressed Then Return

        'If _dragDropCreated Then Return
        '_dragDropCreated = True

        'Dim diff As Integer = Math.Abs(e.Timestamp - _lastMouseDownTime)
        'If diff < 200 Then Return
        'If diff > 5000 Then
        '    ' reset danych
        '    _lastMouseDownTime = e.Timestamp
        '    _lastMouseMovePosition = e.GetPosition(uiPicList)
        '    Return
        'End If

        'Dim currPos As Point = e.GetPosition(uiPicList)
        'Dim odl As Integer = Math.Abs(currPos.X - _lastMouseMovePosition.X) + Math.Abs(currPos.Y - _lastMouseMovePosition.Y)

        'DumpMessage($"mouse time diff {diff} msec, odl {odl}")

        'If odl < 20 Then Return

        StartDragOut()
    End Sub

    Private Async Function StartDragOut() As Task
        vb14.DumpCurrMethod()

        '' sprawdzamy czy mamy odpowiedni Publisher do tego
        'Dim validPubl As New List(Of CloudPublish)
        'For Each oPubl As CloudPublish In Application.GetCloudPublishers.GetList
        '    If oPubl.sProvider.EqualsCI("DragOutNIEMA") Then
        '        validPubl.Add(oPubl)
        '    End If
        'Next

        ' tu przygotujemy listę plików do wysłania
        Dim lista As New List(Of String)

        'If validPubl.Count < 1 Then
        '    DumpMessage($"Nie ma żadnego publishera dla Drag&Drop - zwykłe pliki z bufora")
        '    ' jeśli nie mamy żadnego publishera, to najprościej - bez przetwarzania

        Dim useThumbs As Boolean = vb14.GetSettingsBool("uiDragOutThumbs")

        For Each oTB As ThumbPicek In uiPicList.SelectedItems
            If useThumbs AndAlso IO.File.Exists(oTB.ThumbGetFilename) Then
                lista.Add(oTB.ThumbGetFilename)
            Else
                lista.Add(oTB.oPic.InBufferPathName)
            End If
        Next
        'Else
        '    DumpMessage($"Znamy {validPubl.Count} publisherów dla Drag&Drop")

        '    If validPubl.Count > 1 Then
        '        ' *TODO* teraz do wyboru jakiś drag&drop
        '        Await vb14.DialogBoxAsync($"Widzę {validPubl.Count} publisherów, nie umiem jeszcze wyboru - użyję pierwszego")
        '    End If

        '    Dim processor As CloudPublish = validPubl.ElementAt(0)

        '    Application.TempDirPrepare(2)

        '    'Dim picki As List(Of ThumbPicek) = uiPicList.SelectedItems

        '    For Each oTB As ThumbPicek In uiPicList.SelectedItems
        '        Dim oPic As OnePic = oTB.oPic
        '        oPic.ResetPipeline()

        '        Dim sRet As String = oPic.CanRunPipeline(processor.konfiguracja.defaultPostprocess, Application.gPostProcesory)
        '        If sRet <> "" Then
        '            DumpMessage($"Nie da się pipeline dla pliku {oPic.sSuggestedFilename}")
        '            ' gdy nie można przetworzyć, to użyj pliku oryginalnego
        '            lista.Add(oTB.oPic.InBufferPathName)
        '        Else
        '            oPic.oOstatniExif = processor.konfiguracja.defaultExif
        '            sRet = Await oPic.RunPipeline(processor.konfiguracja.defaultPostprocess, Application.gPostProcesory)
        '            If sRet <> "" Then
        '                ' był błąd - użyj pliku oryginalnego
        '                DumpMessage($"Był błąd w pipeline dla pliku {oPic.sSuggestedFilename}")
        '                lista.Add(oTB.oPic.InBufferPathName)
        '            Else
        '                ' użyj pliku tymczasowego
        '                Dim tempfile As String = Application.TempDirCreateTempFilename
        '                Using writer As FileStream = IO.File.OpenWrite(tempfile)
        '                    oPic._PipelineOutput.Seek(0, SeekOrigin.Begin)
        '                    oPic._PipelineOutput.CopyTo(writer)
        '                End Using
        '                lista.Add(tempfile)
        '                DumpMessage($"Plik {oPic.sSuggestedFilename} przekonwertowany do {tempfile}")
        '            End If
        '        End If
        '    Next

        'End If

        vb14.DumpMessage($"mam liste {lista.Count} plików")
        If lista.Count < 1 Then Return

        Dim data As New DataObject
        data.SetData(DataFormats.FileDrop, lista.ToArray)

        ' Inititate the drag-and-drop operation.
        DragDrop.DoDragDrop(Me, data, DragDropEffects.Copy)

    End Function

    Private Sub uiGetFileSize_Click(sender As Object, e As System.Windows.RoutedEventArgs)
        'uiActionsPopup.IsOpen = False

        Dim iFileSize As Long = 0
        Dim iCnt As Integer = 0
        For Each oItem As ThumbPicek In uiPicList.SelectedItems
            iCnt += 1
            Try
                Dim oFI As New IO.FileInfo(oItem.oPic.InBufferPathName)
                iFileSize += oFI.Length
            Catch ex As Exception
            End Try
        Next

        Me.MsgBox($"{iFileSize.ToSIstringWithPrefix("B", False, True)} in {iCnt} file(s)")

    End Sub

    Private Sub uiSlideshow_Click(sender As Object, e As RoutedEventArgs)

        If uiPicList.SelectedItems.Count < 1 Then Return

        Dim oThumb As ThumbPicek = uiPicList.SelectedItems(0)

        Dim oWnd As New ShowBig(oThumb, _oBufor.GetIsReadonly, True)
        oWnd.Owner = Me
        oWnd.Show()

        Task.Delay(100) ' bo czasem focus wraca do Browser i chodzenie nie działa
        oWnd.Focus()

    End Sub

    Private Async Sub uiStereoPack_Click(sender As Object, e As RoutedEventArgs)

        If uiPicList.SelectedItems.Count > 2 Then
            Me.MsgBox($"Umiem zrobić stereoskopię tylko z dwu zdjęć, a zaznaczyłeś {uiPicList.SelectedItems.Count}")
            Return
        End If

        Dim pic0 As ThumbPicek = uiPicList.SelectedItems(0)
        Dim pic1 As ThumbPicek = Nothing

        If uiPicList.SelectedItems.Count > 1 Then
            pic1 = uiPicList.SelectedItems(1)

            If Not Await StereoTestDaty(pic0, pic1) Then Return ' Δtime
            If Not Await StereoTestGeo(pic0, pic1) Then Return ' Δgeo 

            If Not Await StereoTestExify(pic0, pic1) Then Return ' Δexifs (choć niektóre pomija)

        End If

        'proba stworzenia nowej nazwy - automat, a jak nie umie (nie WP_..) to zapytac
        Dim newName As String = Await StereoCreatePackName(pic0, pic1)
        If String.IsNullOrEmpty(newName) Then Return

        Dim stereopackfolder As String = IO.Path.Combine(IO.Path.GetTempPath, newName)

        ' false gdy katalog juz istnieje i brak zgody na skasowanie
        If Not Await StereoKopiujDoTemp(pic0, pic1, stereopackfolder) Then Return

        If Not Await StereoRunSpmOnPack(stereopackfolder, True) Then
            StereoRemoveFolder(stereopackfolder)
            Return
        End If

        ' nazwa pliku ZIP wewnątrz Buffer
        Dim packZipName As String = IO.Path.GetDirectoryName(pic0.oPic.InBufferPathName)
        packZipName = IO.Path.Combine(packZipName, newName) & ".stereo.zip"

        StereoFolderToZip(packZipName, stereopackfolder)
        StereoRemoveFolder(stereopackfolder)

        ' buffer.json.pic1 - skasowanie pliku, w JSON zmiana nazwy (InBuffer, sSuggested i chyba tyle), zmiana ikonki typu w thumbs
        pic0.oPic.DeleteAllTempFiles()
        IO.File.Delete(pic0.oPic.InBufferPathName)
        pic0.oPic.InBufferPathName = packZipName
        pic0.oPic.Archived = "" ' takiej wersji na pewno nie było zarchiwizowanej :)
        pic0.oPic.sSuggestedFilename = IO.Path.GetFileName(packZipName)
        pic0.oPic.SetDefaultFileTypeDiscriminator() ' ikonka przy picku

        If pic1 IsNot Nothing Then DeletePicekMain(pic1)   ' zmieni _Reapply, jeśli picek miał splita; ze wszystkąd usuwa

        ' *TODO* ewentualnie stereoautoalign, własny anagl z *aligned*, ustalenie R/L, i zrobienie pliku JPS - tak by do StereoViewer wysłac pliki aligned
    End Sub

    Public Shared Sub StereoRemoveFolder(stereopackfolder As String)
        Try
            IO.Directory.Delete(stereopackfolder, True)
        Catch ex As Exception
            ' katalog in use i inne tym podobne
        End Try
    End Sub

#Region "StereoSubki"
    Private Async Function StereoTestDaty(pic0 As ThumbPicek, pic1 As ThumbPicek) As Task(Of Boolean)

        ' Δtime 
        Dim time0 As Date = pic0.oPic.GetMostProbablyDate(True)
        Dim time1 As Date = pic1.oPic.GetMostProbablyDate(True)

        If Not time0.IsDateValid OrElse Not time1.IsDateValid Then Return True

        Dim timeDiff As TimeSpan = time1 - time0
        If Math.Abs((time1 - time0).TotalSeconds) <= vb14.GetSettingsInt("uiStereoMaxDiffSecs") Then
            Return True
        End If

        Return Await Me.DialogBoxYNAsync($"Zdjęcia zbyt odległe w czasie ({timeDiff.ToStringDHMS} sec), kontynuować?")

    End Function

    Private Async Function StereoTestGeo(pic0 As ThumbPicek, pic1 As ThumbPicek) As Task(Of Boolean)
        Dim geo0 As BasicGeopos = pic0.oPic.GetGeoTag
        Dim geo1 As BasicGeopos = pic1.oPic.GetGeoTag

        If geo0 Is Nothing OrElse geo1 Is Nothing Then Return True

        Dim meters As Integer = geo1.DistanceTo(geo0)
        If meters <= vb14.GetSettingsInt("uiStereoMaxDiffMeteres") Then Return True

        Return Await Me.DialogBoxYNAsync($"Zdjęcia zbyt odległe w przestrzeni ({meters} m), kontynuować?")

    End Function

    Private Async Function StereoTestExify(pic0 As ThumbPicek, pic1 As ThumbPicek) As Task(Of Boolean)
        ' sprawdzenie różnic w EXIFach
        Dim roznice As String = ""

        For Each oExif0 As Vblib.ExifTag In pic0.oPic.Exifs
            ' SOURCE_DEFAULT: autor, copyright
            ' SOURCE_FILEATTR: daty
            ' AUTO_EXIF: CameraModel, daty, geotag
            ' AUTO_GUID: oczywiste :)
            ' AUTO_FULLEXIF: jak EXIF, plus daty, obiektyw, naswietlanie i cała seria
            If "SOURCE_DEFAULT|SOURCE_FILEATTR|AUTO_EXIF|AUTO_FULLEXIF|AUTO_GUID".Contains(oExif0.ExifSource) Then Continue For
            Dim oExif1 As Vblib.ExifTag = pic1.oPic.GetExifOfType(oExif0.ExifSource)
            If oExif1 Is Nothing Then Continue For

            If oExif0.DumpAsJSON.EqualsCIAI(oExif1.DumpAsJSON) Then Continue For

            roznice &= ", " & oExif0.ExifSource
        Next

        For Each oExif1 As Vblib.ExifTag In pic1.oPic.Exifs
            If "SOURCE_DEFAULT|SOURCE_FILEATTR|AUTO_EXIF|AUTO_FULLEXIF|AUTO_GUID".Contains(oExif1.ExifSource) Then Continue For
            If roznice.ContainsCI(oExif1.ExifSource) Then Continue For

            Dim oExif0 As Vblib.ExifTag = pic0.oPic.GetExifOfType(oExif1.ExifSource)
            If oExif0 Is Nothing Then Continue For

            If oExif0.DumpAsJSON.EqualsCIAI(oExif1.DumpAsJSON) Then Continue For

            roznice &= ", " & oExif0.ExifSource
        Next

        If roznice = "" Then Return True

        Return Await Me.DialogBoxYNAsync($"Metadane różnią się w {roznice.Substring(2)}, kontynuować?")

    End Function

    ''' <summary>
    ''' zwraca utworzoną nazwę - ale bez extension
    ''' </summary>
    ''' <param name="pic1">Może być NULL</param>
    Private Async Function StereoCreatePackName(pic0 As ThumbPicek, pic1 As ThumbPicek) As Task(Of String)

        If pic1 IsNot Nothing Then
            'proba stworzenia nowej nazwy - automat, a jak nie umie (nie WP_..) to zapytac
            If pic0.oPic.sSuggestedFilename.StartsWith("WP_") AndAlso pic1.oPic.sSuggestedFilename.StartsWith("WP_") Then
                Return pic0.oPic.sSuggestedFilename.Substring(0, "wp_yyyymmdd_hh_mm_ss".Length)
            End If
        End If

        ' wersja side-by-side jest na pewno nie WP_ :) zapewne przystawka do Practica albo FBstaryKrakow
        Return Await Me.InputBoxAsync("Podaj nazwę paczki stereo", IO.Path.GetFileNameWithoutExtension(pic0.oPic.sSuggestedFilename))
    End Function

    ''' <summary>
    ''' utwórz TEMP folder i wkopiuj tam pliki z ThumbPicek oraz stwórz picsort.json
    ''' </summary>
    ''' <param name="pic1">Może być NULL</param>
    ''' <returns>FALSE gdy katalog już istnieje i nie ma zgody na skasowanie</returns>
    Private Async Function StereoKopiujDoTemp(pic0 As ThumbPicek, pic1 As ThumbPicek, stereopackfolder As String) As Task(Of Boolean)
        If IO.Directory.Exists(stereopackfolder) Then
            If Not Await Me.DialogBoxYNAsync($"Katalog '{stereopackfolder}' istnieje ({IO.Directory.GetLastWriteTime(stereopackfolder).ToExifString}), skasować?") Then Return False
            IO.Directory.Delete(stereopackfolder, True)
        End If

        IO.Directory.CreateDirectory(stereopackfolder)
        pic0.oPic.FileCopyToDir(stereopackfolder, True)
        pic1?.oPic.FileCopyToDir(stereopackfolder, True)

        Dim json As String = "[" & vbCrLf & pic0.oPic.DumpAsJSON & "," & vbCrLf
        If pic1 IsNot Nothing Then json &= pic1.oPic.DumpAsJSON & vbCrLf
        json &= "]"
        IO.File.WriteAllText(IO.Path.Combine(stereopackfolder, "picsort.json"), json)

        Return True
    End Function

    ''' <summary>
    '''  uruchom SPM na stereopackfolder, poczekaj na koniec, zapytaj o poprawność
    ''' </summary>
    ''' <param name="stereopackfolder">folder TEMP z plikami</param>
    ''' <param name="askIfOk">FALSE: zawsze zwróci FALSE, TRUE: po SPM zapyta o poprawność, i wtedy ret=False to error </param>
    ''' <returns>TRUE: wszystko OK, pliki do przepakowania</returns>
    Public Shared Async Function StereoRunSpmOnPack(stereopackfolder As String, askIfOk As Boolean) As Task(Of Boolean)

        Dim twoPics As String() = StereoGetTwoPics(stereopackfolder)
        If twoPics.Count > 2 Then Return False

        Dim spmpathname As String = vb14.GetSettingsString("uiStereoSPMPath")

        Dim anaglPathname As String = IO.Path.GetFileName(stereopackfolder) & ".stereo.jpg"
        anaglPathname = IO.Path.Combine(stereopackfolder, anaglPathname)
        vb14.ClipPut(anaglPathname)

        ' nie ma sensu ustalać StartFolder, bo SPM i tak to ignoruje
        Dim spmProcess = Process.Start(spmpathname, twoPics)
        If spmProcess Is Nothing Then Return False

        Await spmProcess.WaitForExitAsync()
        If Not askIfOk Then Return False

        If Not IO.File.Exists(anaglPathname) Then
            If Not Await vb14.DialogBoxYNAsync("Nie ma anaglifu, kontynuować?") Then Return False
        Else
            If Not Await vb14.DialogBoxYNAsync("Wszystko w porządku? kontynuować?") Then Return False
        End If

        Return True
    End Function

    Private Shared Function StereoGetTwoPics(stereopackfolder As String) As String()

        Dim retVal As New List(Of String)

        Dim metadane As New BaseList(Of Vblib.OnePic)(stereopackfolder, "picsort.json")
        If Not metadane.Load Then Return retVal.ToArray

        If metadane.Count < 2 Then Return retVal.ToArray

        retVal.Add(IO.Path.Combine(stereopackfolder, metadane.Item(0).GetInBuffName))
        retVal.Add(IO.Path.Combine(stereopackfolder, metadane.Item(1).GetInBuffName))
        Return retVal.ToArray

    End Function

    Public Shared Sub StereoFolderToZip(packZipName As String, stereopackfolder As String)
        ' ZIP(stereopackfolder\*) => buffer\newName & "stereo.zip"
        IO.File.Delete(packZipName)
        IO.Compression.ZipFile.CreateFromDirectory(stereopackfolder, packZipName)
    End Sub


#End Region

#If False Then
    Private Async Sub uiSaveContactSheetScroll_Click(sender As Object, e As RoutedEventArgs)

        Dim picker As New Microsoft.Win32.SaveFileDialog
        picker.DefaultExt = ".jpg"
        picker.Filter = "Obrazy (.jpg)|*.jpg"
        If Not picker.ShowDialog Then Return

        Dim szerok As Integer = uiPicList.ActualWidth
        Dim wysok As Integer = uiPicList.ActualHeight
        Dim scrVwr As ScrollViewer = GetDescendantByType(uiPicList, GetType(ScrollViewer))
        If scrVwr IsNot Nothing Then
            wysok = scrVwr.ExtentHeight
        End If

        Dim dpi As Integer = 96
        Dim bmp As New RenderTargetBitmap(szerok, wysok, dpi, dpi, PixelFormats.Pbgra32) 'Bgr24, Rgb24
        bmp.Render(scrVwr)

        ' zapisujemy
        Dim encoder As New JpegBitmapEncoder()
        'encoder.QualityLevel = vb14.GetSettingsInt("uiJpgQuality")  ' choć to raczej niepotrzebne, bo to tylko thumb
        encoder.Frames.Add(BitmapFrame.Create(bmp))

        Using fileStream = IO.File.Create(picker.FileName)
            encoder.Save(fileStream)
        End Using

    End Sub
#End If

    Private Sub uiSaveContactSheetItems_Click(sender As Object, e As RoutedEventArgs)

        Dim picker As New Microsoft.Win32.SaveFileDialog
        picker.DefaultExt = ".jpg"
        picker.Filter = "Obrazy (.jpg)|*.jpg"
        If Not picker.ShowDialog Then Return

        Dim szerok As Integer = uiPicList.ActualWidth
        Dim wysok As Integer = uiPicList.ActualHeight
        Dim scrVwr As FrameworkElement = GetDescendantByType(uiPicList, GetType(ItemsPresenter))
        If scrVwr IsNot Nothing Then
            'wysok = scrVwr.ExtentHeight
            wysok = scrVwr.RenderSize.Height
        End If

        Dim dpi As Integer = 96
        Dim bmp As New RenderTargetBitmap(szerok, wysok, dpi, dpi, PixelFormats.Pbgra32) 'Bgr24, Rgb24

        Dim drawVis As New DrawingVisual()
        Using drawCntx As DrawingContext = drawVis.RenderOpen()
            Dim pedzelek As New SolidColorBrush(Colors.White)
            drawCntx.DrawRectangle(pedzelek, Nothing, New Rect(New Point(), New Size(szerok, wysok)))
        End Using
        bmp.Render(drawVis)
        bmp.Render(scrVwr)

        'Dim sourceBrush As New VisualBrush(scrVwr)
        'Dim drawVis As New DrawingVisual()
        'Using drawCntx As DrawingContext = drawVis.RenderOpen()
        '    'drawCntx .PushTransform(New ScaleTransform(zoom, zoom))
        '    drawCntx.DrawRectangle(sourceBrush, Nothing, New Rect(New Point(0, 0), New Point(szerok, wysok)))
        'End Using

        'bmp.Render(drawVis)

        ' zapisujemy
        Dim encoder As New JpegBitmapEncoder()
        'encoder.QualityLevel = vb14.GetSettingsInt("uiJpgQuality")  ' choć to raczej niepotrzebne, bo to tylko thumb
        encoder.Frames.Add(BitmapFrame.Create(bmp))

        Using fileStream = IO.File.Create(picker.FileName)
            encoder.Save(fileStream)
        End Using

    End Sub


    Public Shared Function GetDescendantByType(element As Visual, szukanyTyp As Type) As Visual

        If element Is Nothing Then Return Nothing

        If element.GetType() = szukanyTyp Then Return element

        Dim foundElement As Visual = Nothing
        '          If element Is FrameworkElement Then
        '  (element as FrameworkElement).ApplyTemplate();
        'End If

        For iLp As Integer = 0 To VisualTreeHelper.GetChildrenCount(element) - 1
            Dim vsl As Visual = VisualTreeHelper.GetChild(element, iLp)
            foundElement = GetDescendantByType(vsl, szukanyTyp)
            If foundElement IsNot Nothing Then Return foundElement
        Next

        Return foundElement
    End Function


    Private Sub uiActionNewWndFltr_Click(sender As Object, e As RoutedEventArgs)
        uiActionSelectFilter_Click(Nothing, Nothing)
        uiActionNewWndSelection_Click(Nothing, Nothing)
    End Sub
    Private Sub uiActionNewWndSelection_Click(sender As Object, e As RoutedEventArgs)
        Dim lista As New Vblib.BufferFromQuery()

        For Each oPic As ThumbPicek In uiPicList.SelectedItems
            lista.AddFile(oPic.oPic)
        Next

        Dim oWnd As New ProcessBrowse(lista, "Selected")
        oWnd.Show()
    End Sub


#Region "ShowBig i callbacki z niego"
#Region "double click dla ShowBig"

    Private _DblClickLastPicek As String
    Private _DblClickLastDate As Date

    Private Sub uiImage_LeftClick(sender As Object, e As MouseButtonEventArgs)
        Dim oItem As FrameworkElement = sender
        Dim oPicek As ThumbPicek
        Try
            oPicek = oItem?.DataContext
        Catch ex As Exception
            Return  ' disconnected item na przykład
        End Try

        If oPicek Is Nothing Then Return

        If oPicek.oPic.InBufferPathName = _DblClickLastPicek Then
            If _DblClickLastDate.AddSeconds(1) > Date.Now Then
                uiShowBig_Click(sender, e)
            End If
        End If
        _DblClickLastPicek = oPicek.oPic.InBufferPathName
        _DblClickLastDate = Date.Now
    End Sub

#End Region


    Private Sub uiShowBig_Click(sender As Object, e As RoutedEventArgs)
        Dim oItem As FrameworkElement = sender
        Dim oPicek As ThumbPicek = oItem?.DataContext

        PokazNaDuzymMain(oPicek)
    End Sub

    Private Sub PokazNaDuzymMain(oPicek As ThumbPicek)
        If oPicek Is Nothing Then Return

        _redrawPending = False
        Dim oWnd As New ShowBig(oPicek, _oBufor.GetIsReadonly, False)
        oWnd.Owner = Me
        oWnd.Show()

        Task.Delay(100) ' bo czasem focus wraca do Browser i chodzenie nie działa
        oWnd.Focus()
    End Sub

    Public Function FromBig_Delete(oPic As ThumbPicek) As ThumbPicek
        ' skasować plik, zwróć następny
        Dim oNext As ThumbPicek = FromBig_Next(oPic, False, False)
        DeletePicture(oPic)
        SaveMetaData()
        ' PROBA - bo zdaje się samo się usuwa ładnie z miniaturek (via ObservableList), więc może nie trzeba przerysowywać na GotFocus
        '_redrawPending = True

        ' popraw licznik w tytule okna
        Me.Title = $"{_title} ({_thumbsy.Count} images, memsize {_memSizeKb} MiB)"

        Return oNext
    End Function

    Public Function FromBig_Next(oPic As ThumbPicek, iKierunek As Integer, binSlideShow As Boolean) As ThumbPicek

        Dim thumb As ThumbPicek

        If uiPicList.SelectedItems.Count > 1 Then
            thumb = FromBig_NextMain(oPic, iKierunek, uiPicList.SelectedItems, True)
        Else
            thumb = FromBig_NextMain(oPic, iKierunek, _thumbsy.ToList, False)
            If thumb IsNot Nothing Then uiPicList.SelectedItem = thumb
        End If

        If thumb Is Nothing Then Return thumb
        ' jeśli zapętlenie
        If thumb.oPic.sSuggestedFilename = oPic.oPic.sSuggestedFilename Then Return thumb

        If Not binSlideShow Then Return thumb
        ' czyli jesteśmy w slideshow 
        If vb14.GetSettingsBool("uiSlideShowAlsoX") Then Return thumb

        ' ok, slideshow i mamy pomijać Adulty
        If Not thumb.oPic.IsAdultInExifs Then Return thumb

        ' mamy adult pic a jego nie chcemy, to przeskakujemy do nastepnego
        Return FromBig_Next(thumb, iKierunek, binSlideShow)
    End Function


    Private Function FromBig_NextMain(oPic As ThumbPicek, iKierunek As Integer, lista As IList, retSame As Boolean) As ThumbPicek
        For iLP = 0 To lista.Count - 1
            Dim oItem As ThumbPicek = lista.Item(iLP)
            If oItem.oPic.InBufferPathName = oPic.oPic.InBufferPathName Then
                If iKierunek < 0 Then
                    If iLP = 0 Then
                        If retSame Then
                            System.Media.SystemSounds.Beep.Play()
                            Return oPic
                        Else
                            Return Nothing
                        End If
                    Else
                        Dim oPicRet As ThumbPicek = lista.Item(iLP - 1)
                        If iKierunek < -1 Then
                            ' -100 oznacza: na początek
                            oPicRet = lista.Item(0)
                        End If
                        'ShowKwdForPic(oPicRet)
                        RefreshOwnedWindows(oPicRet)
                        Return oPicRet
                    End If
                Else ' iKierunek > 0, czyli idziemy do przodu: +1, +100 = do końca
                    If iLP = lista.Count - 1 Then
                        If retSame Then
                            System.Media.SystemSounds.Beep.Play()
                            Return oPic
                        Else
                            Return Nothing
                        End If
                    Else
                        Dim oPicRet As ThumbPicek = lista.Item(iLP + 1)
                        If iKierunek > 1 Then
                            ' +100 oznacza: na koniec
                            oPicRet = lista.Item(lista.Count - 1)
                        Else
                            If oPicRet.oPic.InBufferPathName = oPic.oPic.InBufferPathName Then
                                Me.MsgBox($"Sprawdź, bo są dwa pliki z nazwą '{oPicRet.oPic.sSuggestedFilename}'")
                                oPicRet = lista.Item(iLP + 2)
                            End If
                        End If
                        'ShowKwdForPic(oPicRet)
                        RefreshOwnedWindows(oPicRet)
                        Return oPicRet
                    End If
                End If
            End If
        Next

        Return Nothing
    End Function


    Private _SaveMetaDataCounter As Integer
    Private _SaveMetaDataTimer As System.Windows.Threading.DispatcherTimer
    ''' <summary>
    ''' shortcut do zapisania JSON indeksu (buffer.json)
    ''' </summary>
    Public Sub SaveMetaData(Optional force As Boolean = False)

        If _SaveMetaDataTimer Is Nothing Then
            _SaveMetaDataTimer = New System.Windows.Threading.DispatcherTimer
            AddHandler _SaveMetaDataTimer.Tick, Sub() SaveMetaData(True)
            _SaveMetaDataTimer.IsEnabled = True
        End If

        _SaveMetaDataCounter += 1

        If Not force AndAlso _SaveMetaDataCounter < 10 Then
            _SaveMetaDataTimer.Interval = TimeSpan.FromSeconds(60)
            _SaveMetaDataTimer.Start()
            Return
        End If

        _SaveMetaDataTimer.Stop()

        _oBufor.SaveData()
        _SaveMetaDataCounter = 0
    End Sub

#End Region



#Region "delete"

    Private _ReapplyAutoSplit As Boolean = False

    ''' <summary>
    ''' usuń plik "ze wszystkąd"
    ''' </summary>
    Private Sub DeletePicture(oThumb As ThumbPicek)
        If oThumb Is Nothing Then Return

        oThumb.oImageSrc = Nothing  ' bez tego plik był zajęty, nie mógł go skasować

        If Not DeletePicture(oThumb.oPic) Then Return

        ' przesunięcie "dzielnika" *TODO* bezpośrednio na liscie
        If oThumb.splitBefore Then _ReapplyAutoSplit = True

        ' skasuj z tutejszej listy
        _thumbsy.Remove(oThumb)

    End Sub

    ''' <summary>
    ''' usuń plik "ze wszystkąd"
    ''' </summary>
    ''' <returns>FALSE gdy nieudane i proces kasowania trzeba przerwać</returns>
    Private Function DeletePicture(oPic As Vblib.OnePic) As Boolean
        If oPic Is Nothing Then Return False

        GC.Collect()    ' zabezpieczenie jakby tu był jeszcze otwarty plik jakiś

        ' usuń z bufora (z listy i z katalogu), ale nie zapisuj indeksu (jakby to była seria kasowania)
        If Not _oBufor.DeleteFile(oPic) Then Return False  ' nieudane skasowanie

        ' kasujemy różne miniaturki i tak dalej. 
        oPic.DeleteAllTempFiles()

        ' zapisz jako plik do kiedyś-tam usunięcia ze źródła - ale tylko jeśli to nasze źródło
        If String.IsNullOrWhiteSpace(oPic.sharingFromGuid) Then
            Application.GetSourcesList.AddToPurgeList(oPic.sSourceName, oPic.sInSourceID)
        Else
            ' możemy mieć Client ktory jest naszym telefonem, więc do niego trzeba byłoby mieć Purge
            Dim oLogin As Vblib.ShareLogin = TryCast(oPic.GetLastSharePeer(Nothing, Vblib.GetShareLogins), Vblib.ShareLogin)
            If oLogin IsNot Nothing AndAlso oLogin.maintainPurge Then
                Dim _purgeFile As String = IO.Path.Combine(Vblib.GetDataFolder, $"purge.{oLogin.login.ToString}.txt")
                IO.File.AppendAllText(_purgeFile, Date.Now.ToString("yyyyMMdd.HHmm") & vbTab & oPic.sInSourceID & vbCrLf)
            End If

        End If

        Return True
    End Function



    Public Sub DeleteByFilename(filepathname As String)
        For Each oPicek As ThumbPicek In _thumbsy
            If oPicek.oPic.InBufferPathName = filepathname Then
                DeletePicekMain(oPicek)
                Exit For
            End If
        Next
    End Sub

    Private Sub uiDelOne_Click(sender As Object, e As RoutedEventArgs)
        Dim oItem As FrameworkElement = sender
        Dim oPicek As ThumbPicek = oItem?.DataContext

        DeleteAskPicekMain(oPicek)
    End Sub

    Private Async Function DeleteAskPicekMain(oPicek As ThumbPicek) As Task
        If oPicek Is Nothing Then Return

        If Not vb14.GetSettingsBool("uiNoDelConfirm") Then
            If Not Await vb14.DialogBoxYNAsync($"Skasować zdjęcie ({oPicek.oPic.sSuggestedFilename})?") Then Return
        End If

        DeletePicekMain(oPicek)
    End Function


    ''' <summary>
    ''' usuwa plik "ze wszystkąd", zapisuje metadane oraz odnawia miniaturki
    ''' </summary>
    Private Sub DeletePicekMain(oPicek As ThumbPicek)
        If oPicek Is Nothing Then Return

        _ReapplyAutoSplit = False
        DeletePicture(oPicek)   ' zmieni _Reapply, jeśli picek miał splita

        SaveMetaData()

        ' pokaz na nowo obrazki
        If _ReapplyAutoSplit Then
            ' tu można byłoby "przesuwać" splita pomiędzy zdjęciami
            RefreshMiniaturki(_ReapplyAutoSplit)
        End If
    End Sub

    Private Async Sub uiDeleteSelected_Click(sender As Object, e As RoutedEventArgs)
        'uiActionsPopup.IsOpen = False

        ' delete selected
        If uiPicList.SelectedItems Is Nothing Then Return

        Dim lLista As New List(Of ThumbPicek)
        For Each oItem As ThumbPicek In uiPicList.SelectedItems
            lLista.Add(oItem)
        Next

        If Not vb14.GetSettingsBool("uiNoDelConfirm") Then
            If Not Await vb14.DialogBoxYNAsync($"Skasować zdjęcia? ({lLista.Count})") Then Return
        End If

        _ReapplyAutoSplit = False

        For Each oItem As ThumbPicek In lLista
            DeletePicture(oItem)
        Next

        SaveMetaData()    ' tylko raz, po całej serii kasowania

        ' pokaz na nowo obrazki
        RefreshMiniaturki(_ReapplyAutoSplit)
    End Sub

    Private Async Sub uiDeleteThumbsSelected_Click(sender As Object, e As RoutedEventArgs)
        'uiActionsPopup.IsOpen = False
        If uiPicList.SelectedItems Is Nothing Then Return

        'Dim bCacheThumbs As Boolean = vb14.GetSettingsBool("uiCacheThumbs")

        For Each oThumb As ThumbPicek In uiPicList.SelectedItems
            Await oThumb.ThumbWczytajLubStworz(_oBufor.GetIsReadonly, True)
            'Await DoczytajMiniaturke(bCacheThumbs, oThumb, True)
        Next

    End Sub


#End Region

    ''' <summary>
    ''' podaje maksymalny bok dla miniaturki, przy podanej liczbie zdjęć które mają się zmieścić na ekranie
    ''' </summary>
    ''' <param name="iCount"></param>
    ''' <returns></returns>
    Private Function GetMaxBok(iCount As Integer) As Integer
        Dim iPixeli As Integer = Me.ActualWidth * Me.ActualHeight * 0.8   ' na zaokrąglenia
        Dim iPixPerPic As Integer = iPixeli / iCount  ' pikseli² na obrazek
        Dim iMaxBok As Integer = Math.Min(400, Math.Sqrt(iPixPerPic))
        Return iMaxBok
    End Function

#Region "autosplit"

    Private Sub ApplyAutoSplitDaily()

        Dim sLastDate As String = "19700101"

        For Each oItem As ThumbPicek In _thumbsy
            Dim sCurrDate As String = oItem.dateMin.ToString("yyyyMMdd")
            If sCurrDate <> sLastDate Then
                sLastDate = sCurrDate
                oItem.splitBefore = SplitBeforeEnum.czas
            End If
        Next
    End Sub

    Private Sub ApplyAutoSplitHours(hours As Integer)

        Dim lastDate As New Date(1600, 1, 1) ' yyyyMMddHHmmss

        For Each oItem As ThumbPicek In _thumbsy
            If lastDate.IsDateValid Then
                Dim dDateDiff As TimeSpan = oItem.dateMin - lastDate
                If dDateDiff.TotalHours > hours Then
                    oItem.splitBefore = SplitBeforeEnum.czas
                    Dim temp As Long = dDateDiff.TotalSeconds
                    oItem.dymekSplit = $"Time diff: {temp.ToStringDHMS}"
                End If
            End If

            lastDate = oItem.dateMin
        Next
    End Sub

    Private Sub ApplyAutoSplitGeo(kiloms As Integer)

        Dim lastGeo As BasicGeopos = BasicGeopos.Empty ' (0, -150)    ' raczej tam nie będę, środek oceanu

        For Each oItem As ThumbPicek In _thumbsy
            Dim geoExif As BasicGeopos = oItem.oPic.GetGeoTag
            If geoExif IsNot Nothing Then
                If Not lastGeo.IsEmpty Then
                    Dim dOdl As Double = Math.Floor(lastGeo.DistanceTo(geoExif)) / 1000
                    If dOdl > kiloms Then
                        oItem.splitBefore = SplitBeforeEnum.geo
                        oItem.dymekSplit = $"Distance: {dOdl} km"
                    End If
                End If
                lastGeo = geoExif
            End If
        Next
    End Sub

    Private Function PoliczMaxRun() As Integer

        Dim iMax As Integer = 0
        Dim iCurr As Integer = 1

        For Each oItem As ThumbPicek In _thumbsy
            If oItem.splitBefore <> SplitBeforeEnum.none Then
                If iCurr > iMax Then iMax = iCurr
                iCurr = 1
            Else
                iCurr += 1
            End If
        Next

        ' no i ostatni ciąg, który przecież nie ma końca :)
        If iCurr > iMax Then iMax = iCurr


        Return iMax
    End Function

    Private Sub ApplyAutoSplit()

        ' najpierw usuwamy splittery
        For Each oItem As ThumbPicek In _thumbsy
            oItem.splitBefore = SplitBeforeEnum.none
            oItem.dymekSplit = Nothing
        Next

        ' split mniej ważny
        If vb14.GetSettingsBool("uiGeoGapOn", True) Then ApplyAutoSplitGeo(vb14.GetSettingsInt("uiGeoGapInt"))

        ' split ważniejszy
        If vb14.GetSettingsBool("uiDayChange") Then ApplyAutoSplitDaily()
        If vb14.GetSettingsBool("uiHourGapOn", True) Then ApplyAutoSplitHours(vb14.GetSettingsInt("uiHourGapInt"))

        _iMaxRun = PoliczMaxRun()

    End Sub
#End Region


    ''' <summary>
    ''' autosplit, skalowanie miniaturek - duże zmiany w oknie
    ''' </summary>
    ''' <param name="bReapplyAutoSplit">przelicz także autosplit</param>
    Private Sub RefreshMiniaturki(bReapplyAutoSplit As Boolean)
        If bReapplyAutoSplit Then ApplyAutoSplit()    ' zmienia _iMaxRun

        SkalujRozmiarMiniaturek() ' może używać _iMaxRun

        'SortujThumbsy()
        Me.Title = $"{_title} ({_thumbsy.Count} images, memsize {_memSizeKb} MiB)"

    End Sub

    ''' <summary>
    ''' odświeża dymki w SelectedItems - nie przerysowuje
    ''' </summary>
    Private Sub ReDymkuj()
        For Each oItem As ThumbPicek In uiPicList.SelectedItems
            oItem.ZrobDymek()
        Next
    End Sub

    ''' <summary>
    ''' ustawia thumb.iDuzoscH i thumb.widthPaskow - nie przerysowuje
    ''' </summary>
    Private Sub SkalujRozmiarMiniaturek()

        Dim iMaxBok As Integer

        Dim sRequest As String = TryCast(uiComboSize.SelectedValue, ComboBoxItem).Content

        Select Case sRequest.ToLowerInvariant
            Case "fit all"
                iMaxBok = GetMaxBok(_thumbsy.Count)
            Case "fit run"
                iMaxBok = GetMaxBok(_iMaxRun)
            Case Else
                iMaxBok = sRequest
        End Select

        For Each oItem In _thumbsy
            If oItem.oImageSrc Is Nothing Then Continue For

            If oItem.oImageSrc.Width > oItem.oImageSrc.Height Then
                oItem.iDuzoscH = iMaxBok * 0.66
            Else
                oItem.iDuzoscH = iMaxBok
            End If

            oItem.widthPaskow = (iMaxBok * 0.05).Between(3, 10)
        Next

    End Sub

    Private Shared _oFirstVisible As ThumbPicek = Nothing
    Private Shared _lastSizeOption As String

    Private Sub uiComboSize_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles uiComboSize.SelectionChanged
        If _thumbsy Is Nothing Then Return
        If _thumbsy.Count < 1 Then Return

        If uiPicList.Items Is Nothing Then Return
        If uiPicList.Items.CurrentPosition < 0 Then
            ' Me.MsgBox("uiPicList.Items.CurrentPosition < 0")
            Return
        End If

        ' ominięcie podwójnego OnSelectionChanged, które się łączyło z:
        ' System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'ListViewItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        Dim sRequest As String = TryCast(uiComboSize.SelectedValue, ComboBoxItem).Content
        If String.IsNullOrWhiteSpace(sRequest) Then Return

        If _lastSizeOption = sRequest Then Return
        _lastSizeOption = sRequest

        If uiPicList.SelectedItems IsNot Nothing AndAlso uiPicList.SelectedItems.Count > 0 Then
            _oFirstVisible = uiPicList.SelectedItems(0)
        End If

        SkalujRozmiarMiniaturek()

        If _oFirstVisible IsNot Nothing Then uiPicList.ScrollIntoView(_oFirstVisible)
    End Sub

    Private Sub Window_SizeChanged(sender As Object, e As SizeChangedEventArgs)
        uiComboSize_SelectionChanged(Nothing, Nothing)
    End Sub

    Private Sub uiPodpis_Click(sender As Object, e As RoutedEventArgs)
        uiPodpisWybor.IsOpen = Not uiPodpisWybor.IsOpen
    End Sub

    Private Sub uiPodpis_Checked(sender As Object, e As RoutedEventArgs)
        ' tylko wtedy gdy trzeba przeliczyć, czyli dla description oraz keywords
        uiPodpisWybor.IsOpen = False

        Dim oMI As MenuItem = sender
        If oMI Is Nothing Then Return

        Application.ShowWait(True)

        'Dim mode As String = oMI.Header
        'Select Case mode.ToLowerInvariant
        '    Case "keywords"
        '        'For Each oThumb In _thumbsy
        '        '    oThumb.AllKeywords = oThumb.oPic.GetAllKeywords
        '        'Next
        '    Case "description"
        '        ' to już jest zrobione w trakcie wczytywania
        '        'For Each oThumb In _thumbsy
        '        '    oThumb.SumOfDescriptionsText = oThumb.oPic.GetSumOfDescriptionsText
        '        'Next
        'End Select

        RefreshMiniaturki(False)
        Application.ShowWait(False)

    End Sub


    Private Sub uiPodpisDbl_Click(sender As Object, e As MouseButtonEventArgs) ' z click: RoutedEventArgs)
        uiPodpisWybor.IsOpen = False

        Dim oMI As MenuItem = sender
        If oMI Is Nothing Then Return

        For Each oMImenu As MenuItem In uiPodpisMenu.Items
            If oMI.Name <> oMImenu.Name Then oMImenu.IsChecked = False
        Next

    End Sub


    Private Sub uiSplitMode_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New AutoSplitWindow
        oWnd.WindowStartupLocation = WindowStartupLocation.Manual
        Dim oPoint As Point = uiComboSize.PointToScreen(New Point(0, 0))
        oWnd.Top = oPoint.Y + 10
        oWnd.Left = oPoint.X - 10
        oWnd.ShowDialog()

        If vb14.GetSettingsBool("uiGeoGapOn", True) Then
            Dim iCnt As Integer = 0
            Dim iTotal As Integer = 0
            For Each oItem As ThumbPicek In _thumbsy
                iTotal += 1
                If oItem.oPic.GetExifOfType(Vblib.ExifSource.FileExif) IsNot Nothing Then iCnt += 1
            Next

            If iCnt <> iTotal Then
                Me.MsgBox("Nie wszystkie pliki mają znane położenie - sugeruję AUTO_EXIF")
            End If

        End If


        RefreshMiniaturki(True)
    End Sub

#Region "filtry"


    Private _isGeoFilterApplied As Boolean = False
    Private _isTargetFilterApplied As Boolean = False
    Private _OpacityWygas As Double = 0.3

    Private Sub uiFilterAll_Click(sender As Object, e As RoutedEventArgs)
        'uiFilterPopup.IsOpen = False
        uiFilters.Content = "Filters"

        For Each oItem In _thumbsy
            oItem.opacity = 1
        Next

        _isGeoFilterApplied = False
        _isTargetFilterApplied = False

        '*PROBA* nieudana zakomentownia
        'RefreshMiniaturki(False)
    End Sub

    Private Sub uiFilterReverse_Click(sender As Object, e As RoutedEventArgs)
        'uiFilterPopup.IsOpen = False
        ' uiFilters.Content = "none" - nie zmieniamy typu, jest po prostu odwrotnie
        If uiFilters.Content.ToString.StartsWith("¬") Then
            uiFilters.Content = uiFilters.Content.ToString.Substring(1)
        Else
            uiFilters.Content = "¬ " & uiFilters.Content
        End If

        For Each oItem In _thumbsy
            oItem.opacity = If(oItem.opacity = 1, _OpacityWygas, 1)
        Next

        '*PROBA* nieudana zakomentownia
        'RefreshMiniaturki(False)
    End Sub

    Private Sub uiFilterNone_Click(sender As Object, e As RoutedEventArgs)
        'uiFilterPopup.IsOpen = False
        uiFilters.Content = "none"

        For Each oItem In _thumbsy
            oItem.opacity = _OpacityWygas
        Next

        _isGeoFilterApplied = False
        _isTargetFilterApplied = False

        '*PROBA* nieudana zakomentownia
        'RefreshMiniaturki(False)
    End Sub


    Private Sub uiFilterNoRealDate_Click(sender As Object, e As RoutedEventArgs)
        vb14.DumpCurrMethod()

        'uiFilterPopup.IsOpen = False
        uiFilters.Content = "no date"

        Dim bMamy As Boolean = False
        For Each oItem In _thumbsy
            If oItem.oPic.HasRealDate Then
                oItem.opacity = _OpacityWygas
            Else
                oItem.opacity = 1
                bMamy = True
            End If
        Next

        KoniecFiltrowania(bMamy, True)
    End Sub

    Private Sub uiFilterNoGeo_Click(sender As Object, e As RoutedEventArgs)
        vb14.DumpCurrMethod()

        'uiFilterPopup.IsOpen = False
        uiFilters.Content = "no geo"

        Dim bMamy As Boolean = False
        For Each oItem In _thumbsy
            If oItem.oPic.GetGeoTag IsNot Nothing Then
                oItem.opacity = _OpacityWygas
            Else
                oItem.opacity = 1
                bMamy = True
            End If
        Next

        If bMamy Then _isGeoFilterApplied = True

        KoniecFiltrowania(bMamy, sender IsNot Nothing)
    End Sub


    Private Sub uiFilterLocked_Click(sender As Object, e As RoutedEventArgs)
        uiFilterLocking(True)
    End Sub

    Private Sub uiFilterunLocked_Click(sender As Object, e As RoutedEventArgs)
        uiFilterLocking(False)
    End Sub

    Private Sub uiFilterStage_Click(sender As Object, e As RoutedEventArgs)
        'uiFilterPopup.IsOpen = False

        Dim oMI As MenuItem = TryCast(sender, MenuItem)
        Dim stage As SequenceStageBase = TryCast(oMI?.DataContext, SequenceStageBase)
        If stage Is Nothing Then Return
        Dim tryb As Integer = CType(oMI.CommandParameter, Integer) ' 0, -1

        Dim bMamy As Boolean = False

        Select Case tryb

            Case 0
                ' dokładny etap
                uiFilters.Content = "=" & stage.Nazwa
                For Each oItem In _thumbsy
                    If stage.Check(oItem.oPic) Then
                        oItem.opacity = 1
                        bMamy = True
                    Else
                        oItem.opacity = 0
                    End If
                Next

            Case -1
                ' zdjęcie nie dotarło do poziomu
                uiFilters.Content = "<" & stage.Nazwa
                For Each oItem In _thumbsy
                    If oItem.oPic.GetStage.StageNo < stage.StageNo Then
                        oItem.opacity = 1
                        bMamy = True
                    Else
                        oItem.opacity = 0
                    End If
                Next

            Case -99
                ' dokładny etap
                uiFilters.Content = "¬" & stage.Nazwa
                For Each oItem In _thumbsy
                    If Not stage.Check(oItem.oPic) Then
                        oItem.opacity = 1
                        bMamy = True
                    Else
                        oItem.opacity = 0
                    End If
                Next

        End Select

        ' gdy nie trafi na CASE, to bMamy będzie false i wyłączy filtrowanie
        KoniecFiltrowania(bMamy, sender IsNot Nothing)
    End Sub


    Private Sub uiFilterStageReady_Click(sender As Object, e As RoutedEventArgs)
        ' te które mają spełnione wszystkie required

        'uiFilterPopup.IsOpen = False
        uiFilters.Content = "ready"

        Dim requirsy As IEnumerable(Of SequenceStageBase) = Globs.SequenceCheckers.Where(Function(x) x.IsRequired)


        Dim bMamy As Boolean = False

        For Each oItem In _thumbsy
            oItem.opacity = 1

            For Each oStage In requirsy
                If Not oStage.Check(oItem.oPic) Then
                    oItem.opacity = _OpacityWygas
                    Exit For
                End If
            Next

            If oItem.opacity = 1 Then bMamy = True

        Next

        KoniecFiltrowania(bMamy, sender IsNot Nothing)

    End Sub




    Private Sub uiFilterLocking(locked As Boolean)
        vb14.DumpCurrMethod()

        'uiFilterPopup.IsOpen = False
        uiFilters.Content = If(locked, "🔒 locked", "unlocked")

        Dim bMamy As Boolean = False
        For Each oItem In _thumbsy
            If oItem.oPic.locked = locked Then
                oItem.opacity = 1
                bMamy = True
            Else
                oItem.opacity = _OpacityWygas
            End If
        Next

        KoniecFiltrowania(bMamy, True)
    End Sub


    Private Sub uiFilterDwaSek_Click(sender As Object, e As RoutedEventArgs)
        'uiFilterPopup.IsOpen = False
        uiFilters.Content = "dwa/sek"

        If _thumbsy.Count < 2 Then
            uiFilterAll_Click(sender, e)
            Return
        End If

        Dim bMamy As Boolean = False

        ' element 1
        If _thumbsy(0).oPic.GetMostProbablyDate = _thumbsy(1).oPic.GetMostProbablyDate Then
            _thumbsy(0).opacity = 1
            bMamy = True
        Else
            _thumbsy(0).opacity = _OpacityWygas
        End If

        ' element LAST
        If _thumbsy(_thumbsy.Count - 1).oPic.GetMostProbablyDate = _thumbsy(_thumbsy.Count - 2).oPic.GetMostProbablyDate Then
            _thumbsy(_thumbsy.Count - 1).opacity = 1
            bMamy = True
        Else
            _thumbsy(_thumbsy.Count - 1).opacity = _OpacityWygas
        End If

        ' oraz wszystkie pomiędzy

        For iLp As Integer = 1 To _thumbsy.Count - 2
            _thumbsy(iLp).opacity = _OpacityWygas

            If _thumbsy(iLp).oPic.GetMostProbablyDate = _thumbsy(iLp - 1).oPic.GetMostProbablyDate Then
                _thumbsy(iLp).opacity = 1
                bMamy = True
            End If

            If _thumbsy(iLp + 1).oPic.GetMostProbablyDate = _thumbsy(iLp).oPic.GetMostProbablyDate Then
                _thumbsy(iLp).opacity = 1
                bMamy = True
            End If

        Next

        KoniecFiltrowania(bMamy, True)
    End Sub

    Private Sub uiFilterNoAzure_Click(sender As Object, e As RoutedEventArgs)
        'uiFilterPopup.IsOpen = False
        uiFilters.Content = "no azure"

        _isGeoFilterApplied = False
        _isTargetFilterApplied = False

        Dim bMamy As Boolean = False

        For Each oItem As ThumbPicek In _thumbsy
            If oItem.oPic.fileTypeDiscriminator = "►" OrElse
                oItem.oPic.GetExifOfType("AUTO_AZURE") IsNot Nothing Then
                oItem.opacity = _OpacityWygas
            Else
                oItem.opacity = 1
                bMamy = True
            End If
        Next

        KoniecFiltrowania(bMamy, True)

    End Sub

    Private Async Sub uiFilterAzure_Click(sender As Object, e As RoutedEventArgs)
        ' uiFilterPopup.IsOpen = False

        Dim allAzure As Boolean = True
        For Each oItem In _thumbsy
            If oItem.oPic.GetExifOfType("AUTO_AZURE") Is Nothing Then
                allAzure = False
                Exit For
            End If
        Next
        If Not allAzure Then
            If Not Await vb14.DialogBoxYNAsync("Niektóre zdjęcia nie mają analizy Azure, kontynuować?") Then Return
        End If

        Dim oMI As MenuItem = sender
        uiFilters.Content = oMI.Header

        Dim bNot As Boolean = oMI.Header.ToString.StartsWithCI("no")
        Dim bPerson As Boolean = oMI.Header.ToString.ContainsCI("person") ' false: faces

        Dim bMamy As Boolean = False

        For Each oItem In _thumbsy
            oItem.opacity = 1   ' domyślnie: pokazujemy (także gdy nie ma Azure)
            Dim oAzure As Vblib.ExifTag = oItem.oPic.GetExifOfType("AUTO_AZURE")
            If oAzure IsNot Nothing Then
                If bPerson Then
                    ' czy są osoby
                    ' w tags, oraz w objects, albo po prostu w UserComment (gdzie jest dump)
                    If oAzure.UserComment.ContainsCI("person") Then
                        If bNot Then oItem.opacity = _OpacityWygas
                    Else
                        If Not bNot Then oItem.opacity = _OpacityWygas
                    End If
                Else
                    ' faces
                    If oAzure.AzureAnalysis?.Faces IsNot Nothing Then
                        If oAzure.AzureAnalysis?.Faces.lista.Count > 0 Then
                            If bNot Then oItem.opacity = _OpacityWygas
                        Else
                            If Not bNot Then oItem.opacity = _OpacityWygas
                        End If
                    Else
                        ' nie ma twarzy
                        If Not bNot Then oItem.opacity = _OpacityWygas
                    End If
                End If
            End If
            If oItem.opacity = 1 Then bMamy = True
        Next

        _isGeoFilterApplied = False
        _isTargetFilterApplied = False

        KoniecFiltrowania(bMamy, True)
    End Sub

    Private Sub uiFilterAzureAdult_Click(sender As Object, e As RoutedEventArgs)
        'uiFilterPopup.IsOpen = False
        uiFilters.Content = "adult"

        Dim bMamy As Boolean = False

        For Each oItem In _thumbsy
            oItem.opacity = _OpacityWygas   ' domyślnie: nie pokazujemy 
            Dim oAzure As Vblib.ExifTag = oItem.oPic.GetExifOfType("AUTO_AZURE")
            If oAzure IsNot Nothing Then
                If Not String.IsNullOrWhiteSpace(oAzure.AzureAnalysis.Wiekowe) Then
                    oItem.opacity = 1
                    bMamy = True
                End If
            End If
        Next

        _isGeoFilterApplied = False
        _isTargetFilterApplied = False

        KoniecFiltrowania(bMamy, True)
    End Sub

    Private Sub uiFilterAzureTag_Click(sender As Object, e As RoutedEventArgs)
        ShowFilterAzureTag("Tags")
    End Sub
    Private Sub uiFilterAzureObject_Click(sender As Object, e As RoutedEventArgs)
        ShowFilterAzureTag("Objects")
    End Sub
    Private Sub uiFilterAzureBrand_Click(sender As Object, e As RoutedEventArgs)
        ShowFilterAzureTag("Brands")
    End Sub
    Private Sub uiFilterAzureLandmarks_Click(sender As Object, e As RoutedEventArgs)
        ShowFilterAzureTag("Landmarks")
    End Sub
    Private Sub uiFilterAzureCategories_Click(sender As Object, e As RoutedEventArgs)
        ShowFilterAzureTag("Categories")
    End Sub
    Private Sub uiFilterAzureCelebrities_Click(sender As Object, e As RoutedEventArgs)
        ShowFilterAzureTag("Celebrities")
    End Sub

    Private Async Sub ShowFilterAzureTag(listaPropName As String)
        'uiFilterPopup.IsOpen = False

        Me.ProgRingShow(True)

        Dim tagi As List(Of String) = PokazStatystyke.WyciagnijListeMozliwych(listaPropName, _oBufor.GetList)

        Dim oTB As New TextBox With
            {
            .AcceptsReturn = True,
            .IsReadOnly = True,
            .HorizontalAlignment = HorizontalAlignment.Stretch,
            .Height = 250,
            .Text = String.Join(vbCrLf, tagi.OrderBy(Of String)(Function(x) x)),
            .VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            }

        Dim oSP As New StackPanel
        oSP.Children.Add(oTB)
        oSP.Children.Add(New Button With {.Content = " OK ", .IsDefault = True, .HorizontalAlignment = HorizontalAlignment.Center})

        Me.ProgRingShow(False)

        Dim oWnd As New Window With {.Content = oSP, .Width = 200, .Height = 290}
        oWnd.Show()

    End Sub

    Private Sub uiFilterNoDescr_Click(sender As Object, e As RoutedEventArgs)
        'uiFilterPopup.IsOpen = False
        uiFilters.Content = "no desc"

        Dim bMamy As Boolean = False

        For Each oItem As ThumbPicek In _thumbsy
            If String.IsNullOrWhiteSpace(oItem.oPic.GetSumOfDescriptionsText) Then
                oItem.opacity = 1
                bMamy = True
            Else
                oItem.opacity = _OpacityWygas
            End If
        Next

        _isGeoFilterApplied = False
        _isTargetFilterApplied = True
        KoniecFiltrowania(bMamy, True)

    End Sub

    Private Sub uiFilterNoTarget_Click(sender As Object, e As RoutedEventArgs)
        'uiFilterPopup.IsOpen = False
        uiFilters.Content = "no dir"

        _isGeoFilterApplied = False
        _isTargetFilterApplied = True

        Dim bMamy As Boolean = False

        For Each oItem As ThumbPicek In _thumbsy
            If String.IsNullOrWhiteSpace(oItem.oPic.TargetDir) Then
                oItem.opacity = 1
                bMamy = True
            Else
                oItem.opacity = _OpacityWygas
            End If
        Next

        KoniecFiltrowania(bMamy, sender IsNot Nothing)

    End Sub

    Private Sub uiFilterNAR_Click(sender As Object, e As RoutedEventArgs)
        'uiFilterPopup.IsOpen = False
        uiFilters.Content = "NARs"

        Dim bMamy As Boolean = False

        For Each oItem As ThumbPicek In _thumbsy
            If oItem.oPic.fileTypeDiscriminator = "*" Then
                oItem.opacity = 1
                bMamy = True
            Else
                oItem.opacity = _OpacityWygas
            End If
        Next

        _isGeoFilterApplied = False
        _isTargetFilterApplied = False
        KoniecFiltrowania(bMamy, True)
    End Sub


    Private Sub uiFilterKeywords_Click(sender As Object, e As RoutedEventArgs)
        'uiFilterPopup.IsOpen = False
        uiFilters.Content = "kwds"

        Dim oWnd As New FilterKeywords
        oWnd.ShowDialog()

        Dim sQuery As String = oWnd.GetKwerenda 'Await vb14.DialogBoxInputAllDirectAsync("Podaj kwerendę słów kluczowych")
        If String.IsNullOrWhiteSpace(sQuery) Then Return

        Dim aKwds As String() = sQuery.Split(" ")

        Dim bMamy As Boolean = False

        For Each thumb As ThumbPicek In _thumbsy
            If thumb.oPic.MatchesKeywords(aKwds) Then
                thumb.opacity = 1
                bMamy = True
            Else
                thumb.opacity = _OpacityWygas
            End If
        Next

        _isGeoFilterApplied = False
        _isTargetFilterApplied = False
        KoniecFiltrowania(bMamy, True)
    End Sub

    'Private Sub uiFilter_Click(sender As Object, e As RoutedEventArgs)
    '    uiFilterPopup.IsOpen = Not uiFilterPopup.IsOpen
    'End Sub

    ''' <summary>
    ''' jeśli były jakieś zaznaczone, to je pokaż; jeśli nie było nic - przywróć wszystkie
    ''' </summary>
    ''' <param name="bMamy">czy był jakiś zaznaczony</param>
    Private Sub KoniecFiltrowania(bMamy As Boolean, bScrollIntoView As Boolean)
        If Not bMamy Then
            Me.MsgBox("Nie ma takich zdjęć, wyłączam filtrowanie")
            uiFilterAll_Click(Nothing, Nothing)
        Else
            'RefreshMiniaturki(False)

            If bScrollIntoView Then
                ' scroll do pierwszego niewygaszonego
                For Each oThumb As ThumbPicek In uiPicList.ItemsSource
                    If oThumb.opacity > 0.9 Then 'czyli "bardzo pokazane", choć można byłoby =1; tak jednak może być kilka stopni ukrywania :)
                        uiPicList.ScrollIntoView(oThumb)
                        Return
                    End If
                Next
            End If
        End If
    End Sub

    'Private Shared _searchWnd As Window
    Private Sub uiFilterSearch_Click(sender As Object, e As RoutedEventArgs)
        ' uiFilterPopup.IsOpen = False
        uiFilters.Content = "query"

        ' jeśli takie mamy, to go aktywujemy
        For Each oWnd As Window In Me.OwnedWindows
            If oWnd.GetType = GetType(BrowseFullSearch) Then
                oWnd.Activate()
                Return
            End If
        Next

        ' a jak nie mamy, to tworzymy
        Dim _searchWnd As Window = New BrowseFullSearch
        _searchWnd.Owner = Me
        _searchWnd.Show()
    End Sub

    Public Sub FilterSearchCallback(query As Vblib.SearchQuery, typek As TypFilterCallbacka)

        Me.ProgRingShow(True)

        Select Case typek
            Case TypFilterCallbacka.Doznacz, TypFilterCallbacka.Odznacz
                Dim targetOpactity As Double = If(typek = TypFilterCallbacka.Odznacz, _OpacityWygas, 1)

                For Each thumb As ThumbPicek In _thumbsy.Where(Function(x) x.oPic.CheckIfMatchesQuery(query))
                    thumb.opacity = targetOpactity
                Next
            Case TypFilterCallbacka.Zaznacz
                For Each thumb As ThumbPicek In _thumbsy
                    thumb.opacity = If(thumb.oPic.CheckIfMatchesQuery(query), 1, _OpacityWygas)
                Next
        End Select

        Me.ProgRingShow(False)

        ' to jest z FullSearch, znaczy dodawanie/usuwanie zaznaczeń, więc "nie ma takich" jest bez sensu - dlatego pierwszy parametr jest TRUE
        KoniecFiltrowania(True, True)

    End Sub


    'Public Sub WypelnMenuFilterSharing()

    '    Dim iCnt As Integer = WypelnMenuFilterSharingChannels()
    '    iCnt += WypelnMenuFilterSharingLogins()

    '    If iCnt < 1 Then
    '        uiFilterSharing.Visibility = Visibility.Collapsed
    '    Else
    '        uiFilterSharing.Visibility = Visibility.Visible
    '    End If
    'End Sub

    Private Sub uiFilterSharing_SubmenuOpened(sender As Object, e As RoutedEventArgs)
        WypelnMenuFilterQuery()
        WypelnMenuFilterSharingChannels()
        WypelnMenuFilterSharingLogins()
    End Sub


    Private Sub WypelnMenuFilterQuery()
        uiFilterQuery.Items.Clear()

        Dim iCnt As Integer = 0

        For Each oQuery As Vblib.SearchQuery In From c In Vblib.GetQueries Order By c.nazwa

            'Dim oNew As New MenuItem With {.Header = oLogin.displayName, .DataContext = oLogin}
            'AddHandler oNew.Click, AddressOf FilterSharingLogin
            'uiFilterLogins.Items.Add(oNew)
            iCnt += 1

            Dim oNewMarked As New MenuItem With {.Header = oQuery.nazwa, .DataContext = oQuery}
            AddHandler oNewMarked.Click, AddressOf FilterSharingQueryMarked
            uiFilterQuery.Items.Add(oNewMarked)

        Next

        uiFilterQuery.IsEnabled = (iCnt > 0)
    End Sub


    Private Sub FilterSharingChannel(sender As Object, e As RoutedEventArgs)
        'uiFilterPopup.IsOpen = False
        uiFilters.Content = "channel"

        Dim oFE As FrameworkElement = sender
        Dim oChannel As Vblib.ShareChannel = oFE?.DataContext
        If oChannel Is Nothing Then Return

        If oChannel.queries Is Nothing Then
            Me.MsgBox("Ten channel nie ma żadnego query?")
            Return
        End If

        Dim bWas As Boolean = False
        For Each thumb As ThumbPicek In _thumbsy
            thumb.opacity = _OpacityWygas

            For Each query As Vblib.ShareQueryProcess In oChannel.queries

                If thumb.oPic.CheckIfMatchesQuery(query.query) Then
                    bWas = True
                    thumb.opacity = 1
                    Exit For
                End If
            Next
        Next

        KoniecFiltrowania(bWas, True)

    End Sub

    Private Sub uiFilterCudze_Click(sender As Object, e As RoutedEventArgs)
        'uiFilterPopup.IsOpen = False
        uiFilters.Content = "cudze"

        Dim bWas As Boolean = False
        For Each thumb As ThumbPicek In _thumbsy
            If String.IsNullOrWhiteSpace(thumb.oPic.sharingFromGuid) Then
                thumb.opacity = _OpacityWygas
            Else
                thumb.opacity = 1
                bWas = True
            End If
        Next

        KoniecFiltrowania(bWas, True)

    End Sub

    Private Sub uiFilterRemoteDesc_Click(sender As Object, e As RoutedEventArgs)
        'uiFilterPopup.IsOpen = False
        uiFilters.Content = "remdesc"

        Dim bWas As Boolean = False
        For Each thumb As ThumbPicek In _thumbsy
            If Vblib.GetShareDescriptionsIn.FindAllForPic(thumb.oPic) Is Nothing Then
                thumb.opacity = _OpacityWygas
            Else
                thumb.opacity = 1
                bWas = True
            End If
        Next

        KoniecFiltrowania(bWas, True)

    End Sub

    Private Sub FilterSharingQueryMarked(sender As Object, e As RoutedEventArgs)
        'uiFilterPopup.IsOpen = False
        uiFilters.Content = "query"

        Dim oFE As FrameworkElement = sender
        Dim oItem As Vblib.SearchQuery = oFE?.DataContext
        If oItem Is Nothing Then Return


        Dim bWas As Boolean = False
        For Each thumb As ThumbPicek In _thumbsy
            thumb.opacity = _OpacityWygas

            If thumb.oPic.CheckIfMatchesQuery(oItem) Then
                thumb.opacity = 1
                bWas = True
            End If
        Next

        KoniecFiltrowania(bWas, True)
    End Sub



    Private Sub FilterSharingLogin(sender As Object, e As RoutedEventArgs)
        'uiFilterPopup.IsOpen = False
        uiFilters.Content = "login"

        Dim oFE As FrameworkElement = sender
        Dim oLogin As Vblib.ShareLogin = oFE?.DataContext
        If oLogin?.channels Is Nothing Then Return


        Dim bWas As Boolean = False
        For Each thumb As ThumbPicek In _thumbsy
            thumb.opacity = _OpacityWygas

            For Each oChannel As Vblib.ShareChannelProcess In oLogin.channels
                For Each query As Vblib.ShareQueryProcess In oChannel.channel.queries

                    If thumb.oPic.CheckIfMatchesQuery(query.query) Then
                        thumb.opacity = 1
                        bWas = True
                        Exit For
                    End If
                Next

                If thumb.opacity = 1 Then Exit For
            Next

        Next

        KoniecFiltrowania(bWas, True)

    End Sub

    Private Sub FilterSharingLoginMarked(sender As Object, e As RoutedEventArgs)
        'uiFilterPopup.IsOpen = False
        uiFilters.Content = "marked"

        Dim oFE As FrameworkElement = sender
        Dim oLogin As Vblib.ShareLogin = oFE?.DataContext
        If oLogin?.channels Is Nothing Then Return

        Dim bWas As Boolean = False
        For Each thumb As ThumbPicek In _thumbsy
            thumb.opacity = _OpacityWygas
            If thumb.oPic.PeerIsForLogin(oLogin) Then
                thumb.opacity = 1
                bWas = True
            End If
        Next

        KoniecFiltrowania(bWas, True)
    End Sub

    Public Function WypelnMenuFilterSharingChannels() As Integer
        uiFilterChannels.Items.Clear()

        Dim iCnt As Integer = 0

        For Each oChannel As Vblib.ShareChannel In From c In Vblib.GetShareChannels Order By c.nazwa
            Dim oNew As New MenuItem
            oNew.Header = oChannel.nazwa
            oNew.DataContext = oChannel

            AddHandler oNew.Click, AddressOf FilterSharingChannel

            uiFilterChannels.Items.Add(oNew)
            iCnt += 1
        Next

        uiFilterChannels.IsEnabled = (iCnt > 0)

        Return iCnt
    End Function

    Public Function WypelnMenuFilterSharingLogins() As Integer
        uiFilterLoginsMarked.Items.Clear()

        Dim iCnt As Integer = 0

        For Each oLogin As Vblib.ShareLogin In From c In Vblib.GetShareLogins Order By c.displayName

            'Dim oNew As New MenuItem With {.Header = oLogin.displayName, .DataContext = oLogin}
            'AddHandler oNew.Click, AddressOf FilterSharingLogin
            'uiFilterLogins.Items.Add(oNew)
            iCnt += 1

            Dim oNewMarked As New MenuItem With {.Header = oLogin.displayName, .DataContext = oLogin}
            AddHandler oNewMarked.Click, AddressOf FilterSharingLoginMarked
            uiFilterLoginsMarked.Items.Add(oNewMarked)


        Next

        'uiFilterLogins.IsEnabled = (iCnt > 0)
        uiFilterLoginsMarked.IsEnabled = (iCnt > 0)

        Return iCnt
    End Function


#End Region

    Private Sub RefreshOwnedWindows(oThumb As ThumbPicek)
        vb14.DumpCurrMethod($"(picek={oThumb.oPic.sSuggestedFilename}")
        For Each oWnd As Window In Me.OwnedWindows
            vb14.DumpMessage($"changing DataContext in {oWnd.Title}")
            oWnd.DataContext = oThumb
        Next

    End Sub


    Private Sub uiPicList_SelChanged(sender As Object, e As SelectionChangedEventArgs)
        vb14.DumpCurrMethod()

        If uiPodpisCheckbox.IsChecked Then
            SetActionTitle(_checkedCount)
        Else
            Dim ile As Integer = uiPicList.SelectedItems.Count
            SetActionTitle(ile)
            If ile = 1 Then
                Dim oThumb As ThumbPicek = uiPicList.SelectedItems(0)
                RefreshOwnedWindows(oThumb)
            End If
        End If

        _dragDropCreated = False
    End Sub

    Private Sub SetActionTitle(ile As Integer)

        If ile < 1 Then
            'uiAction.IsEnabled = False
            uiAction.Content = " Action "
        Else
            uiAction.IsEnabled = True
            uiAction.Content = $" Action ({ile})"
        End If
    End Sub

#Region "menu autotaggers"
    Private Sub uiActionOpen_Click(sender As Object, e As RoutedEventArgs)
        uiActionsPopup.IsOpen = Not uiActionsPopup.IsOpen
        If uiActionsPopup.IsOpen Then UruchomMenuOpen(uiActionsMenu)
    End Sub

    Private Sub uiActionsContext_Opening(sender As Object, e As ContextMenuEventArgs)
        Dim img As Image = TryCast(sender, Image)
        Dim ctxmn As ContextMenu = TryCast(img.ContextMenu, ContextMenu)
        If ctxmn Is Nothing Then Return
        UruchomMenuOpen(ctxmn)
    End Sub


    Private Sub UruchomMenuOpen(meni As MenuBase)

        For Each oItem In meni.Items
            Dim pmb As PicMenuBase = TryCast(oItem, PicMenuBase)
            pmb?.MenuOtwieramy()
        Next
    End Sub


#End Region

#Region "Keywords window"

    Private Function GetKwdWnd() As Window
        For Each oWnd As Window In Me.OwnedWindows
            If oWnd.Name = "BrowseKeywords" Then Return oWnd
        Next
        Return Nothing
    End Function


    Public Sub ChangedKeywords(oExif As Vblib.ExifTag, oPic1 As ThumbPicek)
        ' callback z BrowseKeywordsWindow - do zaznaczonego (jednego bądź wielu)

        If uiPicList.SelectedItems.Count < 1 Then Return

        If uiPicList.SelectedItems.Count = 1 Then
            'Dim oThumb As ThumbPicek = uiPicList.SelectedItems(0)
            ' tylko jeden wyselekcjonowany - to uznaj że dobry oThumb przychodzi z Keyword
            ' bo przez PgUp/PgDown mogliśmy przejść do innego zdjęcia
            oPic1.oPic.ReplaceOrAddExif(oExif)
            oPic1.oPic.RemoveFromDescriptions(oExif.Keywords, Vblib.GetKeywords)
            oPic1.ZrobDymek()
            oPic1.NotifyPropChange("sumOfKwds")

        Else
            Dim aKwds As String() = oExif.Keywords.Replace("|", " ").Split(" ")
            Dim aOpisy As String() = oExif.UserComment.Split("|")

            For Each oPic As ThumbPicek In uiPicList.SelectedItems

                ' 1) jeśli mamy jakieś tagi, to nowe tylko dołączamy do tego (nie ma wtedy wyłączania tagów)
                Dim oCurrExif As Vblib.ExifTag = oPic.oPic.GetExifOfType(Vblib.ExifSource.ManualTag)
                If oCurrExif IsNot Nothing Then

                    For iLp = 0 To aKwds.Length - 1 ' było: .Count
                        Dim kwd As String = aKwds(iLp).TrimStart
                        If Not oCurrExif.Keywords.Contains(kwd) Then
                            oCurrExif.Keywords &= " " & kwd
                            If iLp < aOpisy.Length Then oCurrExif.UserComment &= "|" & aOpisy(iLp)
                        End If
                    Next

                    'oCurrExif.Keywords = oCurrExif.Keywords & " " & oExif.Keywords
                    'oCurrExif.UserComment = oCurrExif.UserComment & " | " & oExif.UserComment

                    oCurrExif.DateMax = oCurrExif.DateMax.Max(oExif.DateMax)
                    oCurrExif.DateMin = oCurrExif.DateMin.Min(oExif.DateMin)

                    ' wygrywa nowo dodany tag z geo (radiusa już tu nie widać)
                    If oExif.GeoTag IsNot Nothing Then
                        If Not oExif.GeoTag.IsEmpty Then
                            oCurrExif.GeoTag = oExif.GeoTag
                            oCurrExif.GeoName = oExif.GeoName

                            ' i skoro go mamy, to możemy skasować to z kopiowania GeoTag między zdjęciami
                            oPic.oPic.RemoveExifOfType(Vblib.ExifSource.ManualGeo)
                            ' a także te, które są zależne od GeoTag
                            oPic.oPic.RemoveExifOfType(Vblib.ExifSource.AutoOSM)
                            oPic.oPic.RemoveExifOfType(Vblib.ExifSource.AutoImgw)

                            If _isGeoFilterApplied Then oPic.opacity = _OpacityWygas

                        End If
                    End If
                Else
                    ' 2) a jeśli nie mamy, to po prostu dodajemy tag
                    oPic.oPic.ReplaceOrAddExif(oExif.Clone)
                End If

                oPic.oPic.RemoveFromDescriptions(oExif.Keywords, Vblib.GetKeywords)

                oPic.ZrobDymek()
                oPic.NotifyPropChange("sumOfKwds")
            Next

        End If

        ' PROBA czy bedzie OK przez re-ItemsSource
        ' RefreshMiniaturki(True)

        SaveMetaData()

    End Sub



#End Region
#Region "ExifTag window"

#Region "menu otwierania okien"

    Private Sub uiOkna_Click(sender As Object, e As RoutedEventArgs)
        uiOknaPopup.IsOpen = Not uiOknaPopup.IsOpen
    End Sub

    Private Sub OpenSubWindow(oWnd As Window)
        uiOknaPopup.IsOpen = False

        If uiPicList.SelectedItems.Count < 1 Then Return

        oWnd.Owner = Me
        oWnd.DataContext = uiPicList.SelectedItems.Item(0)
        oWnd.Show()

    End Sub

    Private Sub uiOknaShowExif_Click(sender As Object, e As RoutedEventArgs)
        OpenSubWindow(New ShowExifs(True))
    End Sub
    Private Sub uiOknaShowMetadata_Click(sender As Object, e As RoutedEventArgs)
        OpenSubWindow(New ShowExifs(False))
    End Sub

    Private Sub uiOknaEditKwds_Click(sender As Object, e As RoutedEventArgs)
        OpenSubWindow(New SimpleKeywords(_oBufor.GetIsReadonly))
    End Sub

    Private Sub uiOknaKwdsTree_Click(sender As Object, e As RoutedEventArgs)
        OpenSubWindow(New BrowseKeywordsWindow(_oBufor.GetIsReadonly))
    End Sub

    Private Sub uiOknaDescribe_Click(sender As Object, e As RoutedEventArgs)
        OpenSubWindow(New SimpleDescribe(_oBufor.GetIsReadonly))
    End Sub

    Private Sub uiOknaManualExif_Click(sender As Object, e As RoutedEventArgs)
        OpenSubWindow(New EditOneExif(Vblib.ExifSource.Flattened, _oBufor.GetIsReadonly))
    End Sub

    Private Sub uiOknaOCR_Click(sender As Object, e As RoutedEventArgs)
        OpenSubWindow(New SimpleOCR(_oBufor.GetIsReadonly))
    End Sub

    Private Sub uiOknaRemoteDesc_Click(sender As Object, e As RoutedEventArgs)
        OpenSubWindow(New RemoteDescr(_oBufor.GetIsReadonly))
    End Sub

    Private Sub uiOknaTargetDir_Click(sender As Object, e As RoutedEventArgs)
        OpenSubWindow(New SimpleTargetDir)
    End Sub

    Private Sub uiOknaExifProp_Click(sender As Object, e As RoutedEventArgs)
        OpenSubWindow(New SimpleExifProp)
    End Sub

    Private Sub uiOknaDatesSumm_Click(sender As Object, e As RoutedEventArgs)
        OpenSubWindow(New DatesSummary)
    End Sub


    Private Sub uiOknaManualAzureExif_Click(sender As Object, e As RoutedEventArgs)
        OpenSubWindow(New EditOneExif(Vblib.ExifSource.AutoAzure, _oBufor.GetIsReadonly))

        Dim b As New ThumbPicek(Nothing, 10)
        Dim c As Vblib.OnePic = b

    End Sub

#End Region


#End Region


    Public Class ThumbPicek
        Implements INotifyPropertyChanged


        Public Const THUMB_SUFIX As String = ".PicSortThumb.jpg"

        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

        Public Property oPic As Vblib.OnePic
        Public Property sDymek As String 'XAML dymekCount
        Public Property oImageSrc As BitmapImage = Nothing ' XAML image
        Public Property bVisible As Boolean = True
        Public Property dateMin As Date ' kopiowane z oThumb.Exifs(..)

        Public Property IsChecked As Boolean

        Private _splitBefore As Integer
        Public Property splitBefore As Integer
            Get
                Return _splitBefore
            End Get
            Set(value As Integer)
                Dim bChange As Boolean = value <> _splitBefore
                _splitBefore = value
                If bChange Then NotifyPropChange("splitBefore")
            End Set
        End Property

        Private _dymekSplit As String = ""
        Public Property dymekSplit
            Get
                Return _dymekSplit
            End Get
            Set(value)
                Dim bChange As Boolean = value <> _dymekSplit
                _dymekSplit = value
                If bChange Then NotifyPropChange("dymekSplit")
            End Set
        End Property

        Private _opacity As Double = 1 ' czyli normalnie pokazany
        Public Property opacity As Double
            Get
                Return _opacity
            End Get
            Set(value As Double)
                Dim bChange As Boolean = value <> _opacity
                _opacity = value
                If bChange Then NotifyPropChange("opacity")
            End Set
        End Property

        Private _iDuzoscH As Integer
        Public Property iDuzoscH As Integer ' XAML height
            Get
                Return _iDuzoscH
            End Get
            Set(value As Integer)
                Dim bChange As Boolean = value <> _iDuzoscH
                _iDuzoscH = value
                If bChange Then NotifyPropChange("iDuzoscH")
            End Set
        End Property

        Private _widthPaskow As Integer
        Public Property widthPaskow As Integer
            Get
                Return _widthPaskow
            End Get
            Set(value As Integer)
                Dim bChange As Boolean = value <> _widthPaskow
                _widthPaskow = value
                If bChange Then NotifyPropChange("widthPaskow")
            End Set
        End Property


        'Public Property podpis As String = ""
        'Public Property AllKeywords As String
        'Public Property SumOfDescriptionsText As String
        Public Property nrkol As Integer
        Public Property maxnum As Integer

        Public ReadOnly Property TargetDir As String
            ' proxy dla oPic, tak by działało Notify
            Get
                Return oPic.TargetDir
            End Get
        End Property

        Public ReadOnly Property sumOfKwds As String
            ' proxy dla oPic, tak by działało Notify
            Get
                Return oPic.sumOfKwds
            End Get
        End Property

        Public ReadOnly Property sumOfDescr As String
            ' proxy dla oPic, tak by działało Notify
            Get
                Return oPic.sumOfDescr
            End Get
        End Property


        Sub New(picek As Vblib.OnePic, iMaxBok As Integer)
            oPic = picek
            iDuzoscH = iMaxBok
            ZrobDymek()
        End Sub

        Public Function GetLastSharePeer() As Vblib.SharePeer
            Return oPic.GetLastSharePeer(Vblib.GetShareServers, Vblib.GetShareLogins)
        End Function

        Public Sub ZrobDymek()

            Dim newDymek = oPic.sSuggestedFilename

            ' line 0: jeśli przybywa "skądś"
            If Not String.IsNullOrWhiteSpace(oPic.sharingFromGuid) Then
                newDymek &= vbCrLf & GetLastSharePeer()?.displayName & "\" & oPic.sSourceName
            Else
                If oPic.sSourceName.EqualsCI("adhoc") Then newDymek = newDymek & vbCrLf & "Src: " & oPic.sSourceName
            End If

            ' dateMin to GetMostProbablyDate
            If oPic.HasRealDate Then
                newDymek = newDymek & vbCrLf & "Real date: " & oPic.GetMostProbablyDate.ToExifString
            Else
                newDymek = newDymek & vbCrLf & "Date range: " & oPic.GetMinDate.ToExifString & " .. " & oPic.GetMaxDate.ToExifString
                newDymek = newDymek & vbCrLf & "Mid date: " & oPic.GetMostProbablyDate.ToExifString
            End If

            Dim oExifTag As Vblib.ExifTag

            ' line 1: data
            'Dim oExifTag As Vblib.ExifTag = oPic.GetExifOfType(Vblib.ExifSource.FileExif)
            'If oExifTag IsNot Nothing Then
            '    newDymek = newDymek & vbCrLf & "Taken: " & oExifTag.DateTimeOriginal
            'Else
            '    oExifTag = oPic.GetExifOfType(Vblib.ExifSource.SourceFile)
            '    newDymek = newDymek & vbCrLf & "(file: " & oExifTag.DateMin.ToExifString & ")"
            'End If

            ' line 2: geoname / lat&lon
            Dim sGeo As String = ""
            For Each oExif As Vblib.ExifTag In oPic.Exifs
                If oExif.GeoName <> "" Then sGeo = sGeo & vbCrLf & oExif.GeoName
            Next
            sGeo = sGeo.Trim
            If sGeo = "" Then
                Dim oPos As BasicGeopos = oPic.GetGeoTag
                If oPos IsNot Nothing Then sGeo = $"[{oPos.StringLat}, {oPos.StringLon}]"
            End If
            If sGeo <> "" Then newDymek = newDymek & vbCrLf & sGeo

            ' line 3: Azure caption
            oExifTag = oPic.GetExifOfType(Vblib.ExifSource.AutoAzure)
            If oExifTag IsNot Nothing Then
                Dim sCaption As String = oExifTag.AzureAnalysis?.Captions?.GetList(0).ToDisplay
                newDymek = newDymek & vbCrLf & sCaption
            End If

            ' line 4: descriptions
            newDymek = newDymek & vbCrLf & "Descriptions: " & oPic.GetSumOfDescriptionsText & vbCrLf

            ' line 5: keywords
            newDymek = newDymek & "Keywords: " & oPic.sumOfKwds & vbCrLf

            ' line 6: targetdir
            If Not String.IsNullOrWhiteSpace(oPic.TargetDir) Then
                newDymek = newDymek & vbCrLf & "► " & oPic.TargetDir
            End If

            ' line 7: picid - właściwie tylko do picków z archiwum
            newDymek = newDymek & vbCrLf & oPic.FormattedSerNo

            If oPic.locked Then
                newDymek = newDymek & vbCrLf & "🔒 LOCKED"
            End If

            If newDymek <> sDymek Then
                sDymek = newDymek
                NotifyPropChange("sDymek")
            End If

        End Sub

        Public Shared Widening Operator CType(ByVal thumb As ThumbPicek) As Vblib.OnePic
            Return thumb.oPic
        End Operator

#Region "Thumb bitmapowy"

        Public Function ThumbGetFilename() As String
            Return oPic.InBufferPathName & THUMB_SUFIX
        End Function

        Public Shared Function ThumbGetFilename(pathname As String) As String
            Return pathname & THUMB_SUFIX
        End Function

        Public Sub ThumbDelete()
            IO.File.Delete(ThumbGetFilename)
            IO.File.Delete(ThumbGetFilename() & ".png")   ' pierwsza klatka filmu (pełny rozmiar)
        End Sub

        Private Async Function ThumbCreate(bCacheThumbs As Boolean) As Task(Of BitmapImage)
            ' async, bo robienie np. z filmu może potrwać

            Dim sExt As String = IO.Path.GetExtension(oPic.InBufferPathName).ToLowerInvariant

            If Vblib.OnePic.ExtsMovie.ContainsCI(sExt) Then
                Return Await ThumbCreateFromMovie(bCacheThumbs)
            End If

            Select Case sExt
                Case ".nar", ".zip"
                    Using strumyk As Stream = oPic.SinglePicFromMulti()
                        If strumyk Is Nothing Then Return Nothing
                        Dim bitmapa As New BitmapImage()
                        bitmapa.BeginInit()
                        bitmapa.DecodePixelHeight = 400
                        bitmapa.CacheOption = BitmapCacheOption.OnLoad ' Close na Stream uzyty do ładowania
                        bitmapa.StreamSource = strumyk
                        bitmapa.EndInit()
                        Return bitmapa
                    End Using
                Case ".jps"
                    Return ThumbCreateFromJPS(oPic.InBufferPathName)
                Case Else
                    Return ThumbCreateFromNormal(oPic.InBufferPathName)
            End Select

        End Function


        Private Function ThumbCreateFromJPS(sInputFile As String) As BitmapSource

            ' z temp (.png) należy stworzyć THUMB
            Dim bitmapa As New BitmapImage()
            bitmapa.BeginInit()
            bitmapa.DecodePixelHeight = 400
            bitmapa.CacheOption = BitmapCacheOption.OnLoad ' Close na Stream uzyty do ładowania
            bitmapa.UriSource = New Uri(sInputFile)
            bitmapa.EndInit()

            Dim croping As New Int32Rect(0, 0, bitmapa.Width / 2, bitmapa.Height)
            Dim cropped As New CroppedBitmap(bitmapa, croping)

            Return cropped

        End Function

        Public Shared Function ThumbCreateFromNormal(sInputFile As String) As BitmapImage

            Try
                ' z temp (.png) należy stworzyć THUMB
                Dim bitmapa As New BitmapImage()
                bitmapa.BeginInit()
                bitmapa.DecodePixelHeight = 400
                bitmapa.CacheOption = BitmapCacheOption.OnLoad ' Close na Stream uzyty do ładowania
                bitmapa.UriSource = New Uri(sInputFile)
                bitmapa.EndInit()

                Return bitmapa
            Catch ex As Exception

            End Try

            Return Nothing
        End Function

        Private Async Function ThumbCreateFromMovie(bCacheThumbs As Boolean) As Task(Of BitmapImage)

            Dim firstFrame As String = ThumbGetFilename() & ".png"

            If IO.File.Exists(firstFrame) Then
                Return ThumbCreateFromNormal(firstFrame)
            End If

            If bCacheThumbs Then
                ' możemy stworzyć plik pierwszej klatki
                If Not Await VblibStd2_mov2jpg.Mov2jpg.ExtractFirstFrame(oPic.InBufferPathName, firstFrame) Then
                    Return Nothing
                Else
                    Return ThumbCreateFromNormal(firstFrame)
                End If
            End If

            ' nie możemy tworzyć pliku - więc robimy to przez plik tymczasowy

            Dim tempFrame As String = Await VblibStd2_mov2jpg.Mov2jpg.ExtractFirstFrameToTemp(oPic.InBufferPathName)
            If String.IsNullOrEmpty(tempFrame) Then Return Nothing ' nieudane stworzenie ramki
            ' z temp (.png) należy stworzyć THUMB
            Dim bitmapa As BitmapImage = ThumbCreateFromNormal(tempFrame)
            IO.File.Delete(tempFrame)
            Return bitmapa

        End Function

        Public Async Function ThumbWczytajLubStworz(_inArchive As Boolean, Optional bRecreate As Boolean = False) As Task
            If bRecreate Then ThumbDelete()

            Dim bCacheThumbs As Boolean = vb14.GetSettingsBool("uiCacheThumbs")
            If _inArchive Then bCacheThumbs = False    ' w archiwum nie robimy tego!

            Dim bitmapa As BitmapImage = Await ThumbGet(bCacheThumbs)
            oImageSrc = bitmapa

        End Function

        Private Async Function ThumbGet(bCacheThumbs As Boolean) As Task(Of BitmapImage)

            Dim bitmapa As BitmapImage

            ' jeśli mamy zapisany THUMB, to go otwieramy i już
            If IO.File.Exists(ThumbGetFilename) Then
                bitmapa = New BitmapImage()
                bitmapa.BeginInit()
                bitmapa.CacheOption = BitmapCacheOption.OnLoad ' Close na Stream uzyty do ładowania

                bitmapa.UriSource = New Uri(ThumbGetFilename)
                Try
                    bitmapa.EndInit()
                    Return bitmapa
                Catch
                    Return Nothing
                End Try
            End If

            bitmapa = Await ThumbCreate(bCacheThumbs)
            If bitmapa Is Nothing Then Return ThumbPlaceholder()

            If Not bCacheThumbs Then Return bitmapa

            ' zapisujemy
            Dim encoder As New JpegBitmapEncoder()
            'encoder.QualityLevel = vb14.GetSettingsInt("uiJpgQuality")  ' choć to raczej niepotrzebne, bo to tylko thumb
            encoder.Frames.Add(BitmapFrame.Create(bitmapa))

            Using fileStream = IO.File.Create(ThumbGetFilename())
                encoder.Save(fileStream)
            End Using

            FileAttrHidden(ThumbGetFilename(), True)

            Return bitmapa

        End Function

        Private Function ThumbPlaceholder() As BitmapImage

            Dim sExt As String = IO.Path.GetExtension(oPic.InBufferPathName).ToLowerInvariant
            Dim placeholderThumb As String = vblib.GetDataFile("", $"placeholder{sExt}.jpg")
            If Not IO.File.Exists(placeholderThumb) Then
                Process_Signature.WatermarkCreate.StworzWatermarkFile(placeholderThumb, sExt, sExt)
                FileAttrHidden(placeholderThumb, True)
            End If

            Dim bitmapa As New BitmapImage()
            bitmapa.BeginInit()
            bitmapa.DecodePixelHeight = 400
            bitmapa.CacheOption = BitmapCacheOption.OnLoad ' Close na Stream uzyty do ładowania
            bitmapa.UriSource = New Uri(placeholderThumb)
            bitmapa.EndInit()

            Return bitmapa
        End Function

        '       PicToPublish(OnePic) As Stream - Do publikacji, wyciaganie zdjecia z nar itp.
        'PicToThumb(OnePic) as stream - dla thumba, wyciąganie klatki dla filmu...
        'PicToBig(OnePic) do pokazywania na ekranie w ShowBig (podstawowa wersja, bez 3 okien NARa itp)

        'OnePic.PicForBig As stream
        'OnePic.PicForThumb as stream
        'OnePic.PicForPublish(extsmask) as stream
        'OnePic.PicForArchive(extsmask) as strem 

        'OnePic.DelTemps
        'OnePic.DelThumb
#End Region
        Public Async Function PicForBig(Optional iRotation As Rotation = Rotation.Rotate0) As Task(Of BitmapImage)

            If Not IO.File.Exists(oPic.InBufferPathName) Then Return Nothing

            Dim sExt As String = IO.Path.GetExtension(oPic.InBufferPathName).ToLowerInvariant

            Try

                If OnePic.ExtsMovie.ContainsCI(sExt) Then
                    ' mamy filmik
                    Dim firstFrame As String = ThumbGetFilename() & ".png"

                    If IO.File.Exists(firstFrame) Then
                        Dim bitmapa As New BitmapImage()
                        bitmapa.BeginInit()
                        bitmapa.CacheOption = BitmapCacheOption.OnLoad ' Close na Stream uzyty do ładowania
                        bitmapa.UriSource = New Uri(firstFrame)
                        bitmapa.EndInit()

                        Return bitmapa
                    End If
                    ' jeśli nie mamy firstframe, to znaczy że nie wolno jej tworzyć - byłaby stworzona przez ProcessBrowse
                    Return Await ThumbCreateFromMovie(False)
                End If

                Select Case sExt
                    Case ".nar", ".zip"
                        Using strumyk As Stream = oPic.SinglePicFromMulti(Vblib.GetSettingsBool("uiStereoBigAnaglyph"))
                            If strumyk Is Nothing Then Return Nothing
                            Dim bitmapa As New BitmapImage()
                            bitmapa.BeginInit()
                            bitmapa.CacheOption = BitmapCacheOption.OnLoad ' Close na Stream uzyty do ładowania
                            bitmapa.StreamSource = strumyk
                            bitmapa.EndInit()

                            Return bitmapa
                        End Using

                        'Case ".jps"
                    Case Else
                        Dim bitmapa As New BitmapImage()
                        bitmapa.BeginInit()
                        bitmapa.CacheOption = BitmapCacheOption.OnLoad ' Close na Stream uzyty do ładowania
                        bitmapa.Rotation = iRotation
                        bitmapa.UriSource = New Uri(oPic.InBufferPathName)
                        bitmapa.EndInit()

                        Return bitmapa

                End Select

            Catch ex As Exception
                Return Nothing
            End Try


        End Function


        Public Sub NotifyPropChange(propertyName As String)
            ' ale do niektórych to onepic się zmienia, więc niby rekurencyjnie powinno być :)
            Dim evChProp As New PropertyChangedEventArgs(propertyName)
            RaiseEvent PropertyChanged(Me, evChProp)
        End Sub

    End Class

    Private Sub uiPicList_KeyUp(sender As Object, e As KeyEventArgs)
        If e.IsRepeat Then Return

        Dim oThumb As ThumbPicek = uiPicList.SelectedItem
        If oThumb Is Nothing Then Return

        Select Case e.Key
            Case Key.Enter
                PokazNaDuzymMain(oThumb)
            Case Key.Delete
                DeleteAskPicekMain(oThumb)
        End Select
    End Sub

    Private Sub uiAddLink_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New AddLink
        If Not oWnd.ShowDialog() Then Return

        For Each oThumb As ThumbPicek In uiPicList.SelectedItems
            oThumb.oPic.AddLink(oWnd.linek)
        Next

        SaveMetaData()

    End Sub

    Private Shared _GrayOrHide As Boolean

    Public Shared Function GetGrayOrHide() As Boolean
        Return _GrayOrHide
    End Function

    Private Sub uiGrayOrHide_Checked(sender As Object, e As RoutedEventArgs)
        _GrayOrHide = uiGrayOrHide.IsChecked

        Dim opac As Double = If(_GrayOrHide, 0.1, _OpacityWygas)

        For Each oItem In _thumbsy
            If oItem.opacity < 1 Then oItem.opacity = opac
            'oItem.NotifyPropChange("opacity")
        Next
    End Sub

    Private Sub uiActionSaveSelection_Click(sender As Object, e As RoutedEventArgs)

    End Sub

    Private _checkedCount As Integer
    Private Sub CheckBox_Checked(sender As Object, e As RoutedEventArgs)
        Dim oFE As CheckBox = TryCast(sender, CheckBox)
        If oFE Is Nothing Then Return

        _checkedCount += If(oFE.IsChecked, 1, -1)

        SetActionTitle(_checkedCount)
    End Sub

    Private Sub uiPodpisCheckbox_Checked(sender As Object, e As RoutedEventArgs) Handles uiPodpisCheckbox.Checked
        _checkedCount = _thumbsy.Where(Function(x) x.IsChecked).Count
        uiPodpisWybor.IsOpen = False
    End Sub

    Private Sub uiCheckUncheckAll_Click(sender As Object, e As RoutedEventArgs)
        For Each oItem As ThumbPicek In _thumbsy
            If Not oItem.IsChecked Then Continue For

            oItem.IsChecked = False
            oItem.NotifyPropChange("IsChecked")
        Next
    End Sub

    Private Sub uiCheckCheckAll_Click(sender As Object, e As RoutedEventArgs)
        For Each oItem As ThumbPicek In _thumbsy
            If oItem.IsChecked Then Continue For

            oItem.IsChecked = True
            oItem.NotifyPropChange("IsChecked")
        Next
    End Sub

    Private Sub uiCheckReverse_Click(sender As Object, e As RoutedEventArgs)
        For Each oItem As ThumbPicek In _thumbsy
            oItem.IsChecked = Not oItem.IsChecked
            oItem.NotifyPropChange("IsChecked")
        Next
    End Sub

End Class


Public Class KonwersjaDescrIgnoreNewLine
    Inherits ValueConverterOneWaySimple

    Protected Overrides Function Convert(value As Object) As Object
        Dim str As String = CType(value, String)
        If String.IsNullOrWhiteSpace(str) Then Return ""
        Return str.Replace(vbCrLf, " | ")
    End Function
End Class

Public Class KonwersjaGrayOrHide
    Inherits ValueConverterOneWaySimple

    Protected Overrides Function Convert(value As Object) As Object
        Dim opac As Double = CType(value, Double)
        If opac = 1 Then Return Visibility.Visible

        If ProcessBrowse.GetGrayOrHide() Then Return Visibility.Collapsed

        Return Visibility.Visible
    End Function
End Class

Public Class KonwersjaPasekKolor
    Inherits ValueConverterOneWaySimple

    Protected Overrides Function Convert(value As Object) As Object
        Dim temp As Integer = CType(value, Integer)

        If temp = SplitBeforeEnum.czas Then Return New SolidColorBrush(Colors.SkyBlue)
        If temp = SplitBeforeEnum.geo Then Return New SolidColorBrush(Colors.OrangeRed)

        ' i tak będzie niewidoczny, więc w sumie nie jest takie ważne, ale po co robić nowe obiekty
        Return Microsoft.Windows.Themes.ThemeColor.NormalColor
    End Function

End Class



Public Class KonwersjaPasekWysok
    Implements IMultiValueConverter

    Public Function Convert(values() As Object, targetType As Type, parameter As Object, culture As System.Globalization.CultureInfo) As Object Implements IMultiValueConverter.Convert

        Dim splitBefore As Integer = CType(values.ElementAt(0), Integer)
        Dim height As Integer = CType(values.ElementAt(1), Integer)

        Select Case splitBefore
            Case SplitBeforeEnum.geo
                Return height / 2.0
            Case Else
                Return height * 1.0
        End Select

    End Function


    Public Function ConvertBack(value As Object, targetTypes() As Type, parameter As Object, culture As System.Globalization.CultureInfo) As Object() Implements IMultiValueConverter.ConvertBack
        Throw New NotImplementedException()
    End Function
End Class



Public Class KonwersjaPasekVisibility
    Inherits ValueConverterOneWay

    Public Overrides Function Convert(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object
        Dim bTemp As Boolean = CType(value, Integer) > 0

        If parameter IsNot Nothing Then
            Dim sParam As String = CType(parameter, String)
            If sParam.EqualsCI("NEG") Then bTemp = Not bTemp
        End If
        If bTemp Then Return Visibility.Visible

        Return Visibility.Collapsed
    End Function


End Class

Public Class KonwersjaFileDiscrVisibility
    Inherits ValueConverterOneWaySimple

    Protected Overrides Function Convert(value As Object) As Object
        Dim bTemp As String = CType(value, String)
        If String.IsNullOrWhiteSpace(bTemp) Then Return Visibility.Collapsed

        Return Visibility.Visible
    End Function

End Class

Public Class KonwersjaSourcePath2Podpis
    Inherits ValueConverterOneWaySimple

    Protected Overrides Function Convert(value As Object) As Object
        Dim str As String = CType(value, String)
        str = IO.Path.GetDirectoryName(str) ' bez "\" na końcu
        Dim iInd As Integer = str.LastIndexOf(IO.Path.DirectorySeparatorChar)
        If iInd < 1 Then Return str
        iInd = str.LastIndexOf(IO.Path.DirectorySeparatorChar, iInd - 1)
        If iInd < 1 Then Return str
        Return str.Substring(iInd + 1)
    End Function
End Class


Public Enum SplitBeforeEnum
    none
    czas
    geo
End Enum

Interface BrowseSubWindow
    Sub ShowForPic(oPic As Vblib.OnePic)
End Interface
' pokazywanie zdjęć

' 1) prosty przegląd, zaraz po Download - żeby:
' a) skasować niepotrzebne, których nie ma sensu "autotagować"
' b) ułatwić autosortowanie (według dat)

' 2) pełniejszy przegląd, później

' FUNKCJONALNOŚCI:
' 2) crop
' 3) rotate - ze skasowaniem z EXIF informacji o obrocie
' 4) resize
' 5) shell open edit
' ** uwaga! zachować daty plików?

' toolbox: delete, crop, rotate, resize (może być automat jakiś)
' EXIF per oglądany obrazek, oraz per zaznaczone (EXIFSource: MANUAL & yyMMdd-HHmmss)


Imports System.Security.Policy
Imports Vblib
Imports vb14 = Vblib.pkarlibmodule14
Imports pkar
Imports pkar.DotNetExtensions
Imports System.Runtime.InteropServices.WindowsRuntime
Imports Org.BouncyCastle.Math.EC

Public Class ProcessBrowse

    ' Private Const THUMBS_LIMIT As Integer = 9999

    Private _thumbsy As New System.Collections.ObjectModel.ObservableCollection(Of ThumbPicek)
    Private _iMaxRun As Integer  ' po wczytaniu: liczba miniaturek, później: max ciąg zdjęć
    Private _redrawPending As Boolean = False
    Private _oBufor As Vblib.IBufor
    Private _inArchive As Boolean  ' to będzie wyłączać różne funkcjonalności
    Private _title As String

    ' Private _MetadataWindow As ShowExifs

    Public Const THUMB_SUFIX As String = ".PicSortThumb.jpg"

#Region "called on init"

    ''' <summary>
    ''' przeglądarka na liście plików BUFOR, w pełnej wersji bądź ograniczonej do view (czyli gdy już na archiwum a nie na bufurze wejściowym)
    ''' </summary>
    ''' <param name="bufor"></param>
    ''' <param name="onlyBrowse"></param>
    Public Sub New(bufor As Vblib.IBufor, onlyBrowse As Boolean, title As String)
        vb14.DumpCurrMethod()

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        _oBufor = bufor
        _inArchive = onlyBrowse

        _title = title
    End Sub

    Private Sub MenuActionReadOnly()
        Dim oVis As Visibility = If(_inArchive, Visibility.Collapsed, Visibility.Visible)

        uiDeleteSelected.Visibility = oVis
        uiMenuAutotags.Visibility = oVis
        uiDescribeSelected.Visibility = oVis
        uiMenuCopyGeoTag.Visibility = oVis
        uiMenuCreateGeoTag.Visibility = oVis
        uiMenuDateRefit.Visibility = oVis
        uiBatchProcessors.Visibility = oVis
        uiSetTargetDir.Visibility = oVis
        uiActionClearTargetDir.Visibility = oVis
        uiDeleteThumbsSelected.Visibility = oVis
    End Sub

    Private Async Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        vb14.DumpCurrMethod()

        Application.ShowWait(True)

        Await Bufor2Thumbsy()
        SizeMe()
        RefreshMiniaturki(True)

        WypelnMenuCloudPublish(Nothing, uiMenuPublish, AddressOf PublishRun)

        WypelnMenuFilterSharing

        If _inArchive Then
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

            WypelnMenuBatchProcess(uiBatchProcessors, AddressOf PostProcessRun)
            WypelnMenuAutotagerami(uiMenuAutotags, AddressOf AutoTagRun)

            uiFilterNoTarget.Visibility = Visibility.Visible
            MenuActionReadOnly()

            Await EwentualneKasowanieBak()
            Await EwentualneKasowanieArchived()
        End If

        Application.ShowWait(False)
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

        If Await vb14.DialogBoxYNAsync($"Skasować stare pliki BAK? ({iOutdated})") Then Return

        _oBufor.BakDelete(iDelay, True)

    End Function

    Private Async Function EwentualneKasowanieArchived() As Task
        If _inArchive Then Return

        Dim iArchCount As Integer = Application.GetArchivesList.Count
        Dim iCloudArchCount As Integer = Application.GetCloudArchives.GetList.Count

        If iArchCount + iCloudArchCount < 1 Then Return ' jeśli nie mamy żadnego zdefiniowanego, to nie kasujemy i tak

        Dim lista As New List(Of ThumbPicek)
        For Each oThumb As ThumbPicek In _thumbsy
            If oThumb.oPic.NoPendingAction(iArchCount, iCloudArchCount) Then lista.Add(oThumb)
        Next

        If lista.Count < 1 Then Return

        If Not Await vb14.DialogBoxYNAsync($"Skasować pliki już w pełni zarchiwizowane? ({lista.Count})") Then Return

        For Each oThumb As ThumbPicek In lista
            DeletePicture(oThumb)
        Next

        SaveMetaData()
        RefreshMiniaturki(True)

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

        For Each oItem As Vblib.OnePic In _oBufor.GetList
            If Not IO.File.Exists(oItem.InBufferPathName) Then
                ' zabezpieczenie przed samoznikaniem - nie ma, to kasujemy z listy naszych plikow
                lDeleted.Add(oItem)
                vb14.DumpMessage("Znikniety plik: " & oItem.InBufferPathName)
                Continue For
            End If

            Dim oNew As New ThumbPicek(oItem, iMaxBok)

            oNew.dateMin = oItem.GetMostProbablyDate
            uiProgBar.Value += 1
            lista.Add(oNew)

        Next

        If lDeleted.Count > 0 Then

            If Await Vblib.DialogBoxYNAsync($"Niektóre pliki są zniknięte ({lDeleted.Count}), usunąć je z indeksu?") Then

                For Each oItem As Vblib.OnePic In lDeleted
                    _oBufor.GetList.Remove(oItem)
                Next
                SaveMetaData()
            Else
                If Await Vblib.DialogBoxYNAsync($"Skopiować do clipboard ich listę??") Then
                    Dim sNames As String = ""
                    For Each oItem As Vblib.OnePic In lDeleted
                        sNames = sNames & vbCrLf & oItem.sSuggestedFilename
                    Next

                    vb14.ClipPut(sNames)
                End If
            End If
        End If
        Return lista
    End Function

    Private Async Function DoczytajMiniaturki() As Task
        vb14.DumpCurrMethod()

        uiProgBar.Value = 0
        uiProgBar.Visibility = Visibility.Visible

        Dim bCacheThumbs As Boolean = vb14.GetSettingsBool("uiCacheThumbs")
        If _inArchive Then bCacheThumbs = False    ' w archiwum nie robimy tego!

        For Each oItem As ThumbPicek In _thumbsy
            Await DoczytajMiniaturke(bCacheThumbs, oItem)
            uiProgBar.Value += 1
        Next

    End Function

    Private Shared Async Function DoczytajMiniaturke(bCacheThumbs As Boolean, oItem As ThumbPicek, Optional bRecreate As Boolean = False) As Task

        Dim miniaturkaPathname As String = oItem.oPic.InBufferPathName & THUMB_SUFIX

        ' wymuszone odtworzenie miniaturki
        If bRecreate Then IO.File.Delete(miniaturkaPathname)

        Dim bitmapa As BitmapImage = Await WczytajObrazek(oItem.oPic.InBufferPathName, 400, Rotation.Rotate0)
        oItem.oImageSrc = bitmapa

        If bCacheThumbs AndAlso Not IO.File.Exists(miniaturkaPathname) Then
            Dim encoder As New JpegBitmapEncoder()
            encoder.QualityLevel = vb14.GetSettingsInt("uiJpgQuality")  ' choć to raczej niepotrzebne, bo to tylko thumb
            encoder.Frames.Add(BitmapFrame.Create(bitmapa))

            Using fileStream = IO.File.Create(miniaturkaPathname)
                encoder.Save(fileStream)
            End Using

            FileAttrHidden(miniaturkaPathname, True)
        End If

    End Function

    ''' <summary>
    ''' przetworzenie danych Bufor na własną listę (thumbsów)
    ''' </summary>
    Private Async Function Bufor2Thumbsy() As Task
        vb14.DumpCurrMethod()

        _iMaxRun = _oBufor.Count

        uiProgBar.Maximum = _iMaxRun
        uiProgBar.Visibility = Visibility.Visible

        _thumbsy.Clear()
        Dim iMax As String = vb14.GetSettingsInt("uiMaxThumbs")
        If iMax < 10 Then iMax = 100

        Dim lista As List(Of ThumbPicek) = Await WczytajIndeks()   ' tu ewentualne kasowanie jest znikniętych, to wymaga YNAsync

        If lista.Count > iMax Then
            Await vb14.DialogBoxAsync($"Wczytuję miniaturki tylko {iMax} (z {lista.Count})")
        End If

        For Each oItem As ThumbPicek In From c In lista Order By c.dateMin Take iMax
            _thumbsy.Add(oItem)
        Next
        lista.Clear()

        uiProgBar.Value = 0
        Await DoczytajMiniaturki()

        uiProgBar.Visibility = Visibility.Collapsed

    End Function

    Private Sub Window_Closing(sender As Object, e As ComponentModel.CancelEventArgs)
        uiPicList.ItemsSource = Nothing

        For Each oPicek As ThumbPicek In _thumbsy
            oPicek.oImageSrc = Nothing
        Next

        SaveMetaData()  '  po Describe, OCR, i tak dalej - lepiej zapisać nawet jak nie było zmian niż je zgubić

        GC.Collect()    ' usuwamy, bo dużo pamięci zwolniliśmy
    End Sub


#End Region

#Region "górny toolbox"

    Private Sub PokazThumbsy()
        uiPicList.ItemsSource = Nothing
        uiPicList.ItemsSource = From c In _thumbsy Where c.bVisible Order By c.dateMin
        Me.Title = $"{_title} ({_thumbsy.Count} images)"
    End Sub

    Private Sub uiOpenHistoragam_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New HistogramWindow(_oBufor)
        oWnd.Show()
    End Sub
#End Region

#Region "Thumb ContexMenu"

    Private Sub uiCopyPath_Click(sender As Object, e As RoutedEventArgs)
        Dim oItem As FrameworkElement = sender
        Dim oPicek As ThumbPicek = oItem.DataContext
        vb14.ClipPut(oPicek.oPic.InBufferPathName)
    End Sub


#End Region
#Region "menu actions"
    Private Sub uiMenuCopyGeoTag_Click(sender As Object, e As RoutedEventArgs)
        uiActionsPopup.IsOpen = False

        If uiPicList.SelectedItems.Count < 2 Then
            vb14.DialogBox("Funkcja kopiowania GeoTag wymaga zaznaczenia przynajmniej dwu zdjęć")
            Return
        End If

        ' step 1: znajdź pierwszy geotag
        Dim oNewGeoTag As New Vblib.ExifTag(Vblib.ExifSource.ManualGeo)
        Dim oExifOSM As Vblib.ExifTag = Nothing
        Dim oExifImgw As Vblib.ExifTag = Nothing

        For Each oItem As ThumbPicek In uiPicList.SelectedItems
            Dim oGeo As BasicGeopos = oItem.oPic.GetGeoTag
            If oGeo Is Nothing Then Continue For
            oNewGeoTag.GeoTag = oGeo
            oExifOSM = oItem.oPic.GetExifOfType(Vblib.ExifSource.AutoOSM)
            oExifImgw = oItem.oPic.GetExifOfType(Vblib.ExifSource.AutoImgw)
        Next
        If oNewGeoTag.GeoTag Is Nothing OrElse oNewGeoTag.GeoTag.IsEmpty Then
            vb14.DialogBox("Nie mogę znaleźć zdjęcia z GeoTag")
            Return
        End If

        ' step 2: sprawdź czy wszystkie zaznaczone zdjęcia, jeśl mają geotagi, to z tych samych okolic
        Dim iMaxOdl As Integer = 0
        For Each oItem As ThumbPicek In uiPicList.SelectedItems
            Dim oCurrGeo As BasicGeopos = oItem.oPic.GetGeoTag
            If oCurrGeo IsNot Nothing Then iMaxOdl = Math.Max(iMaxOdl, oNewGeoTag.GeoTag.DistanceTo(oCurrGeo))
        Next

        If iMaxOdl > 1000 Then
            vb14.DialogBox($"Wybrane zdjęcia mają między sobą odległość {iMaxOdl} metrów")
            Return
        End If


        For Each oItem As ThumbPicek In uiPicList.SelectedItems

            oItem.oPic.ReplaceOrAddExif(oNewGeoTag)

            If oExifOSM Is Nothing Then
                oItem.oPic.RemoveExifOfType(Vblib.ExifSource.AutoOSM)
            Else
                oItem.oPic.ReplaceOrAddExif(oExifOSM)
            End If

            If oExifImgw Is Nothing Then
                oItem.oPic.RemoveExifOfType(Vblib.ExifSource.AutoImgw)
            Else
                oItem.oPic.ReplaceOrAddExif(oExifImgw)
            End If

            oItem.ZrobDymek()

            If _isGeoFilterApplied Then oItem.opacity = _OpacityWygas
        Next

        ' pokaz na nowo obrazki
        RefreshMiniaturki(True)
        SaveMetaData()
    End Sub

    Private Sub DodajManualGeoTag(oItem As ThumbPicek, oNewGeoTag As Vblib.ExifTag)
        oItem.oPic.ReplaceOrAddExif(oNewGeoTag)
        oItem.oPic.RemoveExifOfType(Vblib.ExifSource.AutoOSM)
        oItem.oPic.RemoveExifOfType(Vblib.ExifSource.AutoImgw)
        oItem.ZrobDymek()

        If _isGeoFilterApplied Then oItem.opacity = _OpacityWygas
    End Sub

    Private Sub uiMenuGeoTag2Clip_Click(sender As Object, e As RoutedEventArgs)
        uiActionsPopup.IsOpen = False

        Dim oItem As FrameworkElement = sender
        Dim oPicek As ThumbPicek = oItem.DataContext

        Dim oGeo As BasicGeopos = oPicek.oPic.GetGeoTag
        If oGeo Is Nothing Then
            vb14.DialogBoxResAsync("Zaznaczone zdjęcie nie ma GeoTag")
            Return
        End If

        vb14.ClipPut(oGeo.ToOSMLink(16))
        vb14.DialogBox("Link do OSM jest w Clipboard")

    End Sub


    Private Function GetDateBetween(oDate1 As Date, oDate2 As Date) As Date
        Dim minutes As Integer = Math.Abs((oDate1 - oDate2).TotalMinutes)
        If oDate1 < oDate2 Then Return oDate1.AddMinutes(minutes / 2)
        Return oDate2.AddMinutes(minutes / 2)
    End Function
    Private Sub uiMenuDateRefit_Click(sender As Object, e As RoutedEventArgs)
        uiActionsPopup.IsOpen = False

        If uiPicList.SelectedItems.Count <> 3 Then
            vb14.DialogBox("Date refit działa tylko przy trzech zaznaczonych zdjęciach")
            Return
        End If

        Dim oPic1 As ThumbPicek = uiPicList.SelectedItems(0)
        Dim oPic2 As ThumbPicek = uiPicList.SelectedItems(1)
        Dim oPic3 As ThumbPicek = uiPicList.SelectedItems(2)

        Dim date1 As Date = oPic1.oPic.GetMostProbablyDate
        Dim date2 As Date = oPic2.oPic.GetMostProbablyDate
        Dim date3 As Date = oPic3.oPic.GetMostProbablyDate

        If Math.Abs((date1 - date2).TotalHours) < 1 AndAlso Math.Abs((date2 - date3).TotalHours) < 1 AndAlso Math.Abs((date1 - date3).TotalHours) < 1 Then
            vb14.DialogBox("Za mała różnica czasu pomiędzy zdjęciami")
            Return
        End If

        Dim oNew As New Vblib.ExifTag(Vblib.ExifSource.ManualDate)

        If Math.Abs((date1 - date2).TotalHours) < 1 Then
            ' czyli date3 jest "za daleko"
            Dim dNew As Date = GetDateBetween(date1, date2)
            oNew.DateTimeOriginal = dNew.ToExifString
            oPic3.oPic.ReplaceOrAddExif(oNew)
            oPic3.dateMin = dNew
        ElseIf Math.Abs((date2 - date3).TotalHours) < 1 Then
            ' czyli date1 jest "za daleko"
            Dim dNew As Date = GetDateBetween(date2, date3)
            oNew.DateTimeOriginal = dNew.ToExifString
            oPic1.oPic.ReplaceOrAddExif(oNew)
            oPic1.dateMin = dNew
        ElseIf Math.Abs((date1 - date3).TotalHours) < 1 Then
            ' czyli date2 jest "za daleko"
            Dim dNew As Date = GetDateBetween(date1, date3)
            oNew.DateTimeOriginal = dNew.ToExifString
            oPic2.oPic.ReplaceOrAddExif(oNew)
            oPic2.dateMin = dNew
        Else
            vb14.DialogBox("Nie ma dwu zdjęć blisko siebie, nie mam jak policzyć średniej")
            Return
        End If


        SaveMetaData()
        RefreshMiniaturki(True)

    End Sub

    Private Sub uiMenuCreateGeoTag_Click(sender As Object, e As RoutedEventArgs)
        uiActionsPopup.IsOpen = False

        Dim oWnd As New EnterGeoTag
        If Not oWnd.ShowDialog Then Return

        Dim oNewGeoTag As New Vblib.ExifTag(Vblib.ExifSource.ManualGeo)
        oNewGeoTag.GeoTag = oWnd.GetGeoPos
        oNewGeoTag.GeoZgrubne = oWnd.IsZgrubne

        If uiPicList.SelectedItems.Count = 1 Then
            DodajManualGeoTag(uiPicList.SelectedItems.Item(0), oNewGeoTag)
        Else
            For Each oItem As ThumbPicek In uiPicList.SelectedItems
                If oItem.oPic.GetGeoTag Is Nothing Then DodajManualGeoTag(oItem, oNewGeoTag)
            Next
        End If

        ' pokaz na nowo obrazki
        RefreshMiniaturki(True)
        SaveMetaData()
    End Sub

    Private Sub uiSetTargetDir_Click(sender As Object, e As RoutedEventArgs)
        uiActionsPopup.IsOpen = False

        If uiPicList.SelectedItems.Count < 1 Then Return

        Dim lSelected As New List(Of ThumbPicek)
        For Each oItem As ThumbPicek In uiPicList.SelectedItems
            lSelected.Add(oItem)
        Next

        Dim oWnd As New TargetDir(_thumbsy.ToList, lSelected)
        If Not oWnd.ShowDialog Then Return

        If _isTargetFilterApplied Then
            For Each oItem As ThumbPicek In uiPicList.SelectedItems
                oItem.opacity = _OpacityWygas
            Next
        End If

        ' pokaz na nowo obrazki
        ReDymkuj()
        RefreshMiniaturki(False)

        SaveMetaData()
    End Sub

    Private Sub uiActionLock_Click(sender As Object, e As RoutedEventArgs)
        uiActionsPopup.IsOpen = False

        If uiPicList.SelectedItems.Count < 1 Then Return

        For Each oItem As ThumbPicek In uiPicList.SelectedItems
            oItem.oPic.locked = True
        Next

    End Sub

    Private Sub uiActionCopyTargetDir_Click(sender As Object, e As RoutedEventArgs)
        uiActionsPopup.IsOpen = False

        If uiPicList.SelectedItems.Count < 2 Then Return

        Dim sTarget As String = ""

        ' ustalenie katalogu, i sprawdzenie czy nie ma różnych
        For Each oItem As ThumbPicek In uiPicList.SelectedItems
            If sTarget = "" Then
                ' jeszcze nie było
                If Not String.IsNullOrWhiteSpace(oItem.oPic.TargetDir) Then sTarget = oItem.oPic.TargetDir
            Else
                If String.IsNullOrWhiteSpace(oItem.oPic.TargetDir) Then Continue For

                If sTarget <> oItem.oPic.TargetDir Then
                    vb14.DialogBox("Są ustalone różne TargetDir dla zaznaczonych plików, więc nic nie robię" & vbCrLf & sTarget & vbCrLf & oItem.oPic.TargetDir)
                    Return
                End If
            End If
        Next

        If String.IsNullOrWhiteSpace(sTarget) Then
            vb14.DialogBox("Nie znalazłem żadnego TargetDir")
            Return
        End If


        ' uzupełniamy tam gdzie nie ma ustalonego
        For Each oItem As ThumbPicek In uiPicList.SelectedItems
            If String.IsNullOrEmpty(oItem.oPic.TargetDir) Then
                oItem.oPic.TargetDir = sTarget
                If _isTargetFilterApplied Then oItem.opacity = _OpacityWygas
            End If
        Next

        ' pokaz na nowo obrazki
        ReDymkuj()
        RefreshMiniaturki(False)

        SaveMetaData()
    End Sub


    Private Sub uiActionClearTargetDir_Click(sender As Object, e As RoutedEventArgs)
        uiActionsPopup.IsOpen = False

        If uiPicList.SelectedItems.Count < 1 Then Return

        For Each oItem As ThumbPicek In uiPicList.SelectedItems
            oItem.oPic.TargetDir = Nothing
            If _isTargetFilterApplied Then oItem.opacity = 1
        Next

        ' pokaz na nowo obrazki
        ReDymkuj()
        RefreshMiniaturki(False)

        SaveMetaData()
    End Sub

    Private Sub uiActionSelectFilter_Click(sender As Object, e As RoutedEventArgs)
        uiActionsPopup.IsOpen = False

        uiPicList.SelectedItems.Clear()

        For Each oItem As ThumbPicek In uiPicList.ItemsSource
            If oItem.opacity = 1 Then uiPicList.SelectedItems.Add(oItem)
        Next

    End Sub

#End Region

#Region "Describe"

    Private Sub uiDescribeSelected_Click(sender As Object, e As RoutedEventArgs)
        uiActionsPopup.IsOpen = False
        Dim oWnd As New AddDescription(Nothing)
        If Not oWnd.ShowDialog Then Return

        Dim oDesc As Vblib.OneDescription = oWnd.GetDescription

        For Each oItem As ThumbPicek In uiPicList.SelectedItems
            oItem.oPic.AddDescription(oDesc)
        Next

        SaveMetaData()  ' bo zmieniono EXIF
    End Sub

    Private Sub uiDescribe_Click(sender As Object, e As RoutedEventArgs)
        Dim oItem As FrameworkElement = sender
        Dim oPicek As ThumbPicek = oItem.DataContext

        Dim oWnd As New AddDescription(oPicek.oPic)
        oWnd.Owner = Me
        If Not oWnd.ShowDialog Then Return

        Dim oDesc As Vblib.OneDescription = oWnd.GetDescription
        oPicek.oPic.AddDescription(oDesc)

        SaveMetaData()  ' bo zmieniono EXIF
    End Sub
#End Region

    Private Sub uiCopyOut_Click(sender As Object, e As System.Windows.RoutedEventArgs)
        uiActionsPopup.IsOpen = False

        Dim sFolder As String = SettingsGlobal.FolderBrowser("", "Gdzie skopiować pliki?")
        If sFolder = "" Then Return
        If Not IO.Directory.Exists(sFolder) Then Return

        Dim iErrCount As Integer = 0
        For Each oItem As ThumbPicek In uiPicList.SelectedItems
            Try
                IO.File.Copy(oItem.oPic.InBufferPathName, IO.Path.Combine(sFolder, oItem.oPic.sSuggestedFilename))
            Catch ex As Exception
                iErrCount += 1
            End Try
        Next

        If iErrCount < 1 Then Return

        vb14.DialogBox($"{iErrCount} errors while copying")

    End Sub

    Private Sub uiCopyClip_Click(sender As Object, e As System.Windows.RoutedEventArgs)
        uiActionsPopup.IsOpen = False

        Clipboard.Clear()
        Dim lista As New Specialized.StringCollection
        For Each oTB As ThumbPicek In uiPicList.SelectedItems
            lista.Add(oTB.oPic.InBufferPathName)
        Next

        Clipboard.SetFileDropList(lista)

        vb14.DialogBox("Files in Clipboard")

    End Sub

    Private Sub uiPicList_MouseMove(sender As Object, e As MouseEventArgs) Handles uiPicList.MouseMove
        MyBase.OnMouseMove(e)
        If e.LeftButton = MouseButtonState.Pressed Then

            Dim lista As New List(Of String)

            For Each oTB As ThumbPicek In uiPicList.SelectedItems
                lista.Add(oTB.oPic.InBufferPathName)
            Next

            If lista.Count < 1 Then Return

            Dim data As New DataObject
            data.SetData(DataFormats.FileDrop, lista.ToArray)

            ' Inititate the drag-and-drop operation.
            DragDrop.DoDragDrop(Me, data, DragDropEffects.Copy)

        End If
    End Sub

    Private Sub uiGetFileSize_Click(sender As Object, e As System.Windows.RoutedEventArgs)
        uiActionsPopup.IsOpen = False

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

        vb14.DialogBox($"{iFileSize.ToSIstringWithPrefix("B", False, True)} in {iCnt} file(s)")

    End Sub

    Private Sub uiSlideshow_Click(sender As Object, e As RoutedEventArgs)

        If uiPicList.SelectedItems.Count < 1 Then Return

        Dim oThumb As ThumbPicek = uiPicList.SelectedItems(0)

        Dim oWnd As New ShowBig(oThumb, _inArchive, True)
        oWnd.Owner = Me
        oWnd.Show()

        Task.Delay(100) ' bo czasem focus wraca do Browser i chodzenie nie działa
        oWnd.Focus()

    End Sub

    Private Sub uiShellExec_Click(sender As Object, e As RoutedEventArgs)
        Dim oItem As FrameworkElement = sender
        Dim oPicek As ThumbPicek = oItem?.DataContext

        Dim proc As New Process()
        proc.StartInfo.UseShellExecute = True
        proc.StartInfo.FileName = oPicek?.oPic?.InBufferPathName
        proc.Start()
    End Sub

    Private Sub uiGoWiki_Click(sender As Object, e As RoutedEventArgs)
        Dim oItem As FrameworkElement = sender
        Dim oPicek As ThumbPicek = oItem?.DataContext

        ShowBig.OpenWikiForMonth(oPicek.oPic)
    End Sub

    ''' <summary>
    ''' wczytaj ze skalowaniem do 400 na wiekszym boku
    ''' (SzukajPicka tu ma błąd, olbrzymie ilości pamięci zjada - bo nie ma skalowania)
    ''' </summary>
    ''' <param name="sPathName"></param>
    ''' <param name="iMaxSize">ograniczenie wielkości (skalowanie), 0: bez skalowania</param>
    ''' <param name="iRotation">obrót obrazka</param>
    ''' <returns></returns>
    Public Shared Async Function WczytajObrazek(sPathName As String, Optional iMaxSize As Integer = 0, Optional iRotation As Rotation = Rotation.Rotate0) As Task(Of BitmapImage)
        If Not IO.File.Exists(sPathName) Then Return Nothing
        Dim bitmap = New BitmapImage()
        bitmap.BeginInit()
        If iMaxSize > 0 Then bitmap.DecodePixelHeight = iMaxSize
        bitmap.CacheOption = BitmapCacheOption.OnLoad ' Close na Stream uzyty do ładowania
        bitmap.Rotation = iRotation

        Dim sExt As String = IO.Path.GetExtension(sPathName).ToLowerInvariant

        If iMaxSize <> 0 AndAlso IO.File.Exists(sPathName & THUMB_SUFIX) Then
            ' jak mamy Thumb, i chcemy thumba, to wczytaj thumb
            sPathName = sPathName & THUMB_SUFIX
        ElseIf IO.File.Exists(sPathName & THUMB_SUFIX & ".png") Then
            ' jeśli mamy PNG, to zapewne chodzi o kadr z filmu - bierzemy niezależnie od skalowania
            sPathName = sPathName & THUMB_SUFIX & ".png"
        Else
            If sExt = ".nar" Or OnePic.ExtsMovie.Contains(sExt) Then
                Dim sRet As String = Await MakeThumbFromFile(sPathName)
                If sRet <> "" Then sPathName = sRet
            End If
        End If

        bitmap.UriSource = New Uri(sPathName)
        Try
            bitmap.EndInit()
            Await Task.Delay(1) ' na potrzeby ProgressBara

            Return bitmap
        Catch ex As Exception
            ' nieudane wczytanie miniaturki - to zapewne błąd tworzenia miniaturki, można spróbować ją utworzyć jeszcze raz
            If sPathName.Contains(THUMB_SUFIX) Then IO.File.Delete(sPathName)
        End Try

        vb14.DumpMessage($"cannot initialize bitmap from {sExt}, using placeholder")


        ' się nie udało tak wprost, to pokazujemy inny obrazek - file extension
        Dim sPlaceholder As String = Application.GetDataFile("", $"placeholder{sExt}.jpg")
        If Not IO.File.Exists(sPlaceholder) Then
            Process_Signature.WatermarkCreate.StworzWatermarkFile(sPlaceholder, sExt, sExt)
            FileAttrHidden(sPlaceholder, True)
        End If

        bitmap = New BitmapImage()
        bitmap.BeginInit()
        If iMaxSize > 0 Then bitmap.DecodePixelHeight = iMaxSize
        bitmap.CacheOption = BitmapCacheOption.OnLoad ' Close na Stream uzyty do ładowania
        bitmap.Rotation = iRotation
        bitmap.UriSource = New Uri(sPlaceholder)
        bitmap.EndInit()
        Await Task.Delay(1) ' na potrzeby ProgressBara

        Return bitmap



    End Function

    ''' <summary>
    ''' stwórz coś do pokazywania (dla nieJPG), 
    ''' </summary>
    ''' <param name="sPathName">obrazek źródłowy</param>
    ''' <returns>filename do TempFile obrazka</returns>
    Private Shared Async Function MakeThumbFromFile(sPathName As String) As Task(Of String)
        ' THUMB_SUFIX

        Dim sExt As String = IO.Path.GetExtension(sPathName).ToLowerInvariant

        Dim sOutfilename As String = sPathName & THUMB_SUFIX
        ' *TODO* skalowanie tego?
        Select Case sExt
            Case ".nar"
                'Dim sTempFile As String = IO.Path.GetTempFileName
                Await Vblib.Auto_AzureTest.Nar2Jpg(sPathName, sOutfilename)
                Return sOutfilename
            Case OnePic.ExtsMovie
                Dim sOutFile As String = sOutfilename & ".png"
                If Not Await VblibStd2_mov2jpg.Mov2jpg.ExtractFirstFrame(sPathName, sOutFile) Then Return ""
                FileAttrHidden(sOutFile, True)
                Return sOutfilename & ".png"
            Case Else
                Return ""    ' nie umiem zrobić - nie wiem co to za plik
        End Select

        Return ""    ' raczej tu nie doszedł...

    End Function



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

        If oPicek Is Nothing Then Return

        _redrawPending = False
        Dim oWnd As New ShowBig(oPicek, _inArchive, False)
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
        _redrawPending = True
        Return oNext
    End Function

    Public Function FromBig_Next(oPic As ThumbPicek, bGoBack As Boolean, binSlideShow As Boolean) As ThumbPicek

        Dim thumb As ThumbPicek

        If uiPicList.SelectedItems.Count > 1 Then
            thumb = FromBig_NextMain(oPic, bGoBack, uiPicList.SelectedItems, True)
        Else
            thumb = FromBig_NextMain(oPic, bGoBack, _thumbsy.ToList, False)
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
        Return FromBig_Next(thumb, bGoBack, binSlideShow)
    End Function


    Private Function FromBig_NextMain(oPic As ThumbPicek, bGoBack As Boolean, lista As IList, retSame As Boolean) As ThumbPicek
        For iLP = 0 To lista.Count - 1
            Dim oItem As ThumbPicek = lista.Item(iLP)
            If oItem.oPic.InBufferPathName = oPic.oPic.InBufferPathName Then
                If bGoBack Then
                    If iLP = 0 Then
                        If retSame Then
                            System.Media.SystemSounds.Beep.Play()
                            Return oPic
                        Else
                            Return Nothing
                        End If
                    Else
                        Dim oPicRet As ThumbPicek = lista.Item(iLP - 1)
                        'ShowKwdForPic(oPicRet)
                        RefreshOwnedWindows(oPicRet)
                        Return oPicRet
                    End If
                Else
                    If iLP = lista.Count - 1 Then
                        If retSame Then
                            System.Media.SystemSounds.Beep.Play()
                            Return oPic
                        Else
                            Return Nothing
                        End If
                    Else
                        Dim oPicRet As ThumbPicek = lista.Item(iLP + 1)
                        'ShowKwdForPic(oPicRet)
                        RefreshOwnedWindows(oPicRet)
                        Return oPicRet
                    End If
                End If
            End If
        Next

        Return Nothing
    End Function

    ''' <summary>
    ''' shortcut do zapisania JSON indeksu 
    ''' </summary>
    Public Sub SaveMetaData()
        _oBufor.SaveData()
    End Sub

#End Region



#Region "delete"

    Private _ReapplyAutoSplit As Boolean = False

    ''' <summary>
    ''' usuń plik "ze wszystkąd"
    ''' </summary>
    ''' <param name="oPicek"></param>
    ''' <returns></returns>
    Private Sub DeletePicture(oPicek As ThumbPicek)
        If oPicek Is Nothing Then Return

        GC.Collect()    ' zabezpieczenie jakby tu był jeszcze otwarty plik jakiś

        ' usuń z bufora (z listy i z katalogu), ale nie zapisuj indeksu (jakby to była seria kasowania)
        If Not _oBufor.DeleteFile(oPicek.oPic) Then Return   ' nieudane skasowanie

        ' kasujemy różne miniaturki i tak dalej. Delete nie robi Exception jak pliku nie ma.
        IO.File.Delete(oPicek.oPic.InBufferPathName & THUMB_SUFIX)
        IO.File.Delete(oPicek.oPic.InBufferPathName & THUMB_SUFIX & ".png")

        ' zapisz jako plik do kiedyś-tam usunięcia ze źródła
        Application.GetSourcesList.AddToPurgeList(oPicek.oPic.sSourceName, oPicek.oPic.sInSourceID)

        ' przesunięcie "dzielnika" *TODO* bezpośrednio na liscie
        If oPicek.splitBefore Then _ReapplyAutoSplit = True

        ' skasuj z tutejszej listy
        _thumbsy.Remove(oPicek)

    End Sub

    Public Sub DeleteByFilename(filepathname As String)
        For Each oPicek As ThumbPicek In _thumbsy
            If oPicek.oPic.InBufferPathName = filepathname Then
                DeletePicekMain(oPicek)
                Exit For
            End If
        Next
    End Sub

    Private Async Sub uiDelOne_Click(sender As Object, e As RoutedEventArgs)
        Dim oItem As FrameworkElement = sender
        Dim oPicek As ThumbPicek = oItem?.DataContext

        If Not vb14.GetSettingsBool("uiNoDelConfirm") Then
            If Not Await vb14.DialogBoxYNAsync($"Skasować zdjęcie ({oPicek.oPic.sSuggestedFilename})?") Then Return
        End If

        DeletePicekMain(oPicek)
    End Sub

    Private Sub DeletePicekMain(oPicek As ThumbPicek)
        _ReapplyAutoSplit = False
        DeletePicture(oPicek)   ' zmieni _Reapply, jeśli picek miał splita

        SaveMetaData()

        ' pokaz na nowo obrazki
        RefreshMiniaturki(_ReapplyAutoSplit)
    End Sub

    Private Async Sub uiDeleteSelected_Click(sender As Object, e As RoutedEventArgs)
        uiActionsPopup.IsOpen = False

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
        uiActionsPopup.IsOpen = False
        If uiPicList.SelectedItems Is Nothing Then Return

        Dim bCacheThumbs As Boolean = vb14.GetSettingsBool("uiCacheThumbs")

        For Each oItem As ThumbPicek In uiPicList.SelectedItems
            Await DoczytajMiniaturke(bCacheThumbs, oItem, True)
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
    ''' przelicz i pokaż miniaturki
    ''' </summary>
    ''' <param name="bReapplyAutoSplit">przelicz także autosplit</param>
    Private Sub RefreshMiniaturki(bReapplyAutoSplit As Boolean)
        If bReapplyAutoSplit Then ApplyAutoSplit()    ' zmienia _iMaxRun

        SkalujRozmiarMiniaturek() ' może używać _iMaxRun
        PokazThumbsy()

    End Sub

    Private Sub ReDymkuj()
        For Each oItem As ThumbPicek In uiPicList.SelectedItems
            oItem.ZrobDymek()
        Next
    End Sub


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

    Private Sub uiComboSize_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles uiComboSize.SelectionChanged
        If _thumbsy Is Nothing Then Return
        If _thumbsy.Count < 1 Then Return

        RefreshMiniaturki(False)
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

        Dim mode As String = oMI.Header
        Select Case mode.ToLowerInvariant
            Case "keywords"
                For Each oThumb In _thumbsy
                    oThumb.AllKeywords = oThumb.oPic.GetAllKeywords
                Next
            Case "description"
                For Each oThumb In _thumbsy
                    oThumb.SumOfDescriptionsText = oThumb.oPic.GetSumOfDescriptionsText
                Next
        End Select

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


    Private Sub uiPodpisTo_Click(sender As Object, e As RoutedEventArgs)
        uiPodpisWybor.IsOpen = False

        Dim oMI As MenuItem = sender
        If oMI Is Nothing Then Return

        Dim mode As String = oMI.Header
        Select Case mode.ToLowerInvariant
            Case "filename"
                For Each oThumb In _thumbsy
                    oThumb.podpis = oThumb.oPic.sSuggestedFilename
                Next
            Case "keywords"
                For Each oThumb In _thumbsy
                    oThumb.podpis = oThumb.oPic.GetAllKeywords
                Next
            Case "description"
                For Each oThumb In _thumbsy
                    oThumb.podpis = oThumb.oPic.GetSumOfDescriptionsText
                Next
            Case "targetdir"
                For Each oThumb In _thumbsy
                    oThumb.podpis = oThumb.oPic.TargetDir
                Next

            Case Else ' w tym "none"
                For Each oThumb In _thumbsy
                    oThumb.podpis = ""
                Next

        End Select

        For Each oMI In uiPodpisMenu.Items
            oMI.IsChecked = (oMI.Header = mode)
        Next

        RefreshMiniaturki(False)
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
                vb14.DialogBox("Nie wszystkie pliki mają znane położenie - sugeruję AUTO_EXIF")
            End If

        End If


        RefreshMiniaturki(True)
    End Sub

#Region "filtry"
    ' na przyszłość, pewnie z jakichś automatów by to było
    'Private Sub WypelnMenuFiltrow()
    '    uiMenuFilters.Items.Clear()
    'End Sub

    Private _isGeoFilterApplied As Boolean = False
    Private _isTargetFilterApplied As Boolean = False
    Private _OpacityWygas As Double = 0.3

    Private Sub uiFilterAll_Click(sender As Object, e As RoutedEventArgs)
        uiFilterPopup.IsOpen = False
        uiFilters.Content = "Filters"

        For Each oItem In _thumbsy
            oItem.opacity = 1
        Next

        _isGeoFilterApplied = False
        _isTargetFilterApplied = False

        RefreshMiniaturki(False)
    End Sub

    Private Sub uiFilterNone_Click(sender As Object, e As RoutedEventArgs)
        uiFilterPopup.IsOpen = False
        uiFilters.Content = "none"

        For Each oItem In _thumbsy
            oItem.opacity = _OpacityWygas
        Next

        _isGeoFilterApplied = False
        _isTargetFilterApplied = False

        RefreshMiniaturki(False)
    End Sub

    Private Sub uiFilterNoGeo_Click(sender As Object, e As RoutedEventArgs)
        vb14.DumpCurrMethod()

        uiFilterPopup.IsOpen = False
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

        KoniecFiltrowania(bMamy)
    End Sub

    Private Sub uiFilterDwaSek_Click(sender As Object, e As RoutedEventArgs)
        uiFilterPopup.IsOpen = False
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

        KoniecFiltrowania(bMamy)
    End Sub

    Private Sub uiFilterNoAzure_Click(sender As Object, e As RoutedEventArgs)
        uiFilterPopup.IsOpen = False
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

        KoniecFiltrowania(bMamy)

    End Sub

    Private Async Sub uiFilterAzure_Click(sender As Object, e As RoutedEventArgs)
        uiFilterPopup.IsOpen = False

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

        Dim bNot As Boolean = oMI.Header.ToString.ToLowerInvariant.StartsWith("no")
        Dim bPerson As Boolean = oMI.Header.ToString.ToLowerInvariant.Contains("person") ' false: faces

        Dim bMamy As Boolean = False

        For Each oItem In _thumbsy
            oItem.opacity = 1   ' domyślnie: pokazujemy (także gdy nie ma Azure)
            Dim oAzure As Vblib.ExifTag = oItem.oPic.GetExifOfType("AUTO_AZURE")
            If oAzure IsNot Nothing Then
                If bPerson Then
                    ' czy są osoby
                    ' w tags, oraz w objects, albo po prostu w UserComment (gdzie jest dump)
                    If oAzure.UserComment.ToLowerInvariant.Contains("person") Then
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

        KoniecFiltrowania(bMamy)
    End Sub

    Private Sub uiFilterAzureAdult_Click(sender As Object, e As RoutedEventArgs)
        uiFilterPopup.IsOpen = False
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

        KoniecFiltrowania(bMamy)
    End Sub

    Private Sub uiFilterNoDescr_Click(sender As Object, e As RoutedEventArgs)
        uiFilterPopup.IsOpen = False
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
        KoniecFiltrowania(bMamy)

    End Sub

    Private Sub uiFilterNoTarget_Click(sender As Object, e As RoutedEventArgs)
        uiFilterPopup.IsOpen = False
        uiFilters.Content = "no dir"

        Dim bMamy As Boolean = False

        For Each oItem As ThumbPicek In _thumbsy
            If String.IsNullOrWhiteSpace(oItem.oPic.TargetDir) Then
                oItem.opacity = 1
                bMamy = True
            Else
                oItem.opacity = _OpacityWygas
            End If
        Next

        _isGeoFilterApplied = False
        _isTargetFilterApplied = True
        KoniecFiltrowania(bMamy)
    End Sub

    Private Sub uiFilterNAR_Click(sender As Object, e As RoutedEventArgs)
        uiFilterPopup.IsOpen = False
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
        KoniecFiltrowania(bMamy)
    End Sub


    Private Sub uiFilterKeywords_Click(sender As Object, e As RoutedEventArgs)
        uiFilterPopup.IsOpen = False
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
        KoniecFiltrowania(bMamy)
    End Sub

    Private Sub uiFilter_Click(sender As Object, e As RoutedEventArgs)
        uiFilterPopup.IsOpen = Not uiFilterPopup.IsOpen
    End Sub

    Private Sub KoniecFiltrowania(bMamy As Boolean)
        If Not bMamy Then
            vb14.DialogBox("Nie ma takich zdjęć, wyłączam filtrowanie")
            uiFilterAll_Click(Nothing, Nothing)
        Else
            RefreshMiniaturki(False)
        End If
    End Sub

    Private Shared _searchWnd As Window
    Private Sub uiFilterSearch_Click(sender As Object, e As RoutedEventArgs)
        uiFilterPopup.IsOpen = False
        uiFilters.Content = "query"


        If _searchWnd IsNot Nothing Then
            Try
                _searchWnd.Activate()
                Return
            Catch ex As Exception
                _searchWnd = Nothing
            End Try
        End If

        _searchWnd = New BrowseFullSearch
        _searchWnd.Owner = Me
        _searchWnd.Show()
    End Sub

    Public Function FilterSearchCallback(query As SearchQuery, usun As Boolean)

        For Each thumb As ThumbPicek In _thumbsy

            'If SearchWindow.CheckIfOnePicMatches(thumb.oPic, query) Then
            If thumb.oPic.CheckIfMatchesQuery(query) Then

                If usun Then
                    thumb.opacity = _OpacityWygas
                Else
                    thumb.opacity = 1
                End If
            End If
        Next

        RefreshMiniaturki(False)

        Return True
    End Function


    Public Sub WypelnMenuFilterSharing()

        uiFilterLogins.Items.Clear()

        Dim iCnt As Integer = WypelnMenuFilterSharingChannels
        iCnt += WypelnMenuFilterSharingLogins

        If iCnt < 1 Then
            uiFilterSharing.Visibility = Visibility.Collapsed
        Else
            uiFilterSharing.Visibility = Visibility.Visible
        End If
    End Sub


    Private Sub FilterSharingChannel(sender As Object, e As RoutedEventArgs)
        uiFilterPopup.IsOpen = False
        uiFilters.Content = "channel"

        Dim oFE As FrameworkElement = sender
        Dim oChannel As Vblib.ShareChannel = oFE?.DataContext
        If oChannel Is Nothing Then Return

        For Each thumb As ThumbPicek In _thumbsy
            thumb.opacity = _OpacityWygas

            For Each query As ShareQueryProcess In oChannel.queries

                If thumb.oPic.CheckIfMatchesQuery(query.query) Then
                    thumb.opacity = 1
                    Exit For
                End If
            Next
        Next

        RefreshMiniaturki(False)

    End Sub

    Private Sub FilterSharingLogin(sender As Object, e As RoutedEventArgs)
        uiFilterPopup.IsOpen = False
        uiFilters.Content = "login"

        Dim oFE As FrameworkElement = sender
        Dim oLogin As Vblib.ShareLogin = oFE?.DataContext
        If oLogin?.channels Is Nothing Then Return

        For Each thumb As ThumbPicek In _thumbsy
            thumb.opacity = _OpacityWygas

            For Each oChannel As ShareChannelProcess In oLogin.channels
                For Each query As ShareQueryProcess In oChannel.channel.queries

                    If thumb.oPic.CheckIfMatchesQuery(query.query) Then
                        thumb.opacity = 1
                        Exit For
                    End If
                Next

                If thumb.opacity = 1 Then Exit For
            Next

        Next

        RefreshMiniaturki(False)

    End Sub


    Public Function WypelnMenuFilterSharingChannels() As Integer
        uiFilterChannels.Items.Clear()

        Dim iCnt As Integer = 0

        For Each oChannel As Vblib.ShareChannel In Application.GetShareChannels.GetList
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
        uiFilterLogins.Items.Clear()

        Dim iCnt As Integer = 0

        For Each oLogin As Vblib.ShareLogin In Application.GetShareLogins.GetList
            Dim oNew As New MenuItem
            oNew.Header = oLogin.displayName
            oNew.DataContext = oLogin

            AddHandler oNew.Click, AddressOf FilterSharingLogin

            uiFilterLogins.Items.Add(oNew)
            iCnt += 1
        Next

        uiFilterLogins.IsEnabled = (iCnt > 0)

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
        Dim ile As Integer = uiPicList.SelectedItems.Count
        If ile < 1 Then
            uiAction.IsEnabled = False
            uiAction.Content = " Action "
        Else
            uiAction.IsEnabled = True
            uiAction.Content = $" Action ({ile})"
        End If

        If ile = 1 Then
            Dim oItem As ThumbPicek = uiPicList.SelectedItems(0)
            RefreshOwnedWindows(oItem)

            'ShowKwdForPic(oItem)
            'ShowExifForPic(oItem)
        End If

    End Sub


#Region "menu autotaggers"
    Private Sub uiActionOpen_Click(sender As Object, e As RoutedEventArgs)
        uiActionsPopup.IsOpen = Not uiActionsPopup.IsOpen
    End Sub

    'Private Shared _UImenuOnClick As RoutedEventHandler

    'Private Shared Sub MenuAutoTaggersRun(sender As Object, e As RoutedEventArgs)
    '    If _UImenuOnClick Is Nothing Then Return
    '    _UImenuOnClick(sender, e)
    'End Sub

    Public Shared Sub WypelnMenuAutotagerami(oMenuItem As MenuItem, oEventHandler As RoutedEventHandler)
        oMenuItem.Items.Clear()
        ' _UImenuOnClick = oEventHandler

        For Each oEngine As Vblib.AutotaggerBase In Application.gAutoTagery
            Dim oNew As New MenuItem
            oNew.Header = oEngine.Nazwa.Replace("_", "__")
            oNew.DataContext = oEngine
            AddHandler oNew.Click, oEventHandler
            oMenuItem.Items.Add(oNew)
        Next

        oMenuItem.IsEnabled = (oMenuItem.Items.Count > 0)

    End Sub

    Public Shared Sub WypelnMenuBatchProcess(oMenuItem As MenuItem, oEventHandler As RoutedEventHandler)
        oMenuItem.Items.Clear()
        ' _UImenuOnClick = oEventHandler

        For Each oEngine As Vblib.PostProcBase In Application.gPostProcesory
            Dim oNew As New MenuItem
            oNew.Header = oEngine.Nazwa.Replace("_", "__")
            oNew.DataContext = oEngine
            AddHandler oNew.Click, oEventHandler
            oMenuItem.Items.Add(oNew)
        Next

        oMenuItem.IsEnabled = (oMenuItem.Items.Count > 0)

    End Sub

    'Public Shared Sub WypelnMenuCloudArchive(oMenuItem As MenuItem, oEventHandler As RoutedEventHandler)
    '    oMenuItem.Items.Clear()
    '    ' _UImenuOnClick = oEventHandler

    '    For Each oEngine As Vblib.CloudArchive In Application.gCloudProviders.GetCloudArchiversList
    '        Dim oNew As New MenuItem
    '        oNew.Header = oEngine.konfiguracja.nazwa.Replace("_", "__")
    '        oNew.DataContext = oEngine
    '        AddHandler oNew.Click, oEventHandler
    '        oMenuItem.Items.Add(oNew)
    '    Next

    '    oMenuItem.IsEnabled = (oMenuItem.Items.Count > 0)

    'End Sub

    Private Shared Function NewMenuCloudOperation(sDisplay As String, oEngine As Object, oEventHandler As RoutedEventHandler) As MenuItem
        Dim oNew As New MenuItem
        oNew.Header = sDisplay.Replace("_", "__")
        oNew.DataContext = oEngine

        If oEventHandler IsNot Nothing Then AddHandler oNew.Click, oEventHandler

        Return oNew
    End Function

    Private Shared Function NewMenuCloudOperation(oEngine As Object) As MenuItem
        Return NewMenuCloudOperation(oEngine.konfiguracja.nazwa, oEngine, Nothing)
    End Function

    Public Shared Sub WypelnMenuCloudPublish(oPic As Vblib.OnePic, oMenuItem As MenuItem, oEventHandler As RoutedEventHandler)
        oMenuItem.Items.Clear()
        ' _UImenuOnClick = oEventHandler

        For Each oEngine As Vblib.CloudPublish In Application.GetCloudPublishers.GetList
            Dim oNew As MenuItem = NewMenuCloudOperation(oEngine)
            oNew.IsCheckable = False    ' aczkolwiek to jest default, więc pewnie nie będzie więcej miejsca od tego

            If oPic Is Nothing OrElse Not oPic.IsCloudPublishedIn(oEngine.konfiguracja.nazwa) Then
                AddHandler oNew.Click, oEventHandler
            Else
                oNew.Items.Add(NewMenuCloudOperation("Open", oEngine, oEventHandler))
                oNew.Items.Add(NewMenuCloudOperation("Share link", oEngine, oEventHandler))
                oNew.Items.Add(NewMenuCloudOperation("Get tags", oEngine, oEventHandler))
                oNew.Items.Add(New Separator)
                oNew.Items.Add(NewMenuCloudOperation("Delete", oEngine, oEventHandler))
            End If

            oMenuItem.Items.Add(oNew)

        Next

        oMenuItem.IsEnabled = (oMenuItem.Items.Count > 0)

    End Sub

    Public Shared Sub WypelnMenuCloudArchives(oPic As Vblib.OnePic, oMenuItem As MenuItem, oEventHandler As RoutedEventHandler)
        oMenuItem.Items.Clear()
        ' _UImenuOnClick = oEventHandler

        For Each oEngine As Vblib.CloudArchive In Application.GetCloudArchives.GetList
            If Not oPic.IsCloudArchivedIn(oEngine.konfiguracja.nazwa) Then Continue For

            Dim oNew As MenuItem = NewMenuCloudOperation(oEngine)

            oNew.Items.Add(NewMenuCloudOperation("Open", oEngine, oEventHandler))
            oNew.Items.Add(NewMenuCloudOperation("Share link", oEngine, oEventHandler))
            oNew.Items.Add(NewMenuCloudOperation("Get tags", oEngine, oEventHandler))

            oMenuItem.Items.Add(oNew)

        Next

        oMenuItem.IsEnabled = (oMenuItem.Items.Count > 0)

    End Sub

#End Region
    Private Async Sub AutoTagRun(sender As Object, e As RoutedEventArgs)
        uiActionsPopup.IsOpen = False

        Dim oFE As FrameworkElement = sender
        Dim oEngine As Vblib.AutotaggerBase = oFE?.DataContext
        If oEngine Is Nothing Then Return

        uiProgBar.Value = 0
        uiProgBar.Maximum = uiPicList.SelectedItems.Count
        uiProgBar.Visibility = Visibility.Visible

        Application.ShowWait(True)
        For Each oItem As ThumbPicek In uiPicList.SelectedItems
            uiProgBar.ToolTip = oItem.oPic.InBufferPathName
            If oItem.oPic.GetExifOfType(oEngine.Nazwa) Is Nothing Then
                Dim oExif As Vblib.ExifTag = Await oEngine.GetForFile(oItem.oPic)
                If oExif Is Nothing Then
                    If oEngine.Nazwa = ExifSource.AutoAzure Then Exit For
                Else
                    oItem.oPic.Exifs.Add(oExif)
                    oItem.oPic.TagsChanged = True
                    oItem.ZrobDymek()
                End If
                Await Task.Delay(3) ' na wszelki wypadek, żeby był czas na przerysowanie progbar, nawet jak tworzenie EXIFa jest empty
            End If
            uiProgBar.Value += 1
        Next
        uiProgBar.ToolTip = ""
        Application.ShowWait(False)

        uiProgBar.Visibility = Visibility.Collapsed

        SaveMetaData() ' bo zmieniono EXIF

        ' ale nie mamy pamiętane jaki jest aktualnie filtr
        'If oEngine.Nazwa = ExifSource.AutoAzure Then
        '    RefreshMiniaturki(False)
        'End If


    End Sub

    Private Async Sub PostProcessRun(sender As Object, e As RoutedEventArgs)
        uiActionsPopup.IsOpen = False

        Dim oFE As FrameworkElement = sender
        Dim oEngine As Vblib.PostProcBase = oFE?.DataContext
        If oEngine Is Nothing Then Return

        uiProgBar.Maximum = uiPicList.SelectedItems.Count
        uiProgBar.Visibility = Visibility.Visible

        Application.ShowWait(True)
        For Each oItem As ThumbPicek In uiPicList.SelectedItems
            Await oEngine.Apply(oItem.oPic, False, "")
            Await Task.Delay(1) ' na wszelki wypadek, żeby był czas na przerysowanie progbar, nawet jak tworzenie EXIFa jest empty
            uiProgBar.Value += 1
        Next
        Application.ShowWait(False)

        uiProgBar.Visibility = Visibility.Collapsed

        SaveMetaData()  ' bo zmieniono EXIF

    End Sub

    Private Async Sub GetRemoteTagsRun(sender As Object, e As RoutedEventArgs)
        uiActionsPopup.IsOpen = False

        Dim oFE As FrameworkElement = sender
        Dim oSrc As Vblib.CloudPublish = oFE?.DataContext
        If oSrc Is Nothing Then Return

        uiProgBar.Maximum = uiPicList.SelectedItems.Count
        uiProgBar.Visibility = Visibility.Visible

        Application.ShowWait(True)

        Dim sErr As String = Await oSrc.Login
        If sErr <> "" Then
            Await vb14.DialogBoxAsync(sErr)
            Application.ShowWait(False)
            Return
        End If

        For Each oItem As ThumbPicek In uiPicList.SelectedItems
            Await oSrc.GetRemoteTags(oItem.oPic)
        Next

        Application.ShowWait(False)

        uiProgBar.Visibility = Visibility.Collapsed

        SaveMetaData()  ' bo zmieniono dane plików

    End Sub

    Private Async Sub PublishRun(sender As Object, e As RoutedEventArgs)
        uiActionsPopup.IsOpen = False

        ' test na adultpice
        Dim iCnt As Integer = 0
        Dim sNames As String = ""
        For Each oItem As ThumbPicek In uiPicList.SelectedItems
            If oItem.oPic.IsAdultInExifs OrElse Application.GetKeywords.IsAdultInAnyKeyword(oItem.oPic.GetAllKeywords) Then
                iCnt += 1
                sNames = sNames & vbCrLf & oItem.oPic.sSuggestedFilename
            End If
        Next
        If iCnt > 0 Then
            Dim sMsg As String = "plików zawiera"
            If iCnt = 1 Then sMsg = "plik zawiera"
            If iCnt > 1 AndAlso iCnt < 5 Then sMsg = "pliki zawierają"

            If Not Await vb14.DialogBoxYNAsync($"{iCnt} {sMsg} ograniczenia wiekowe, kontynuować? ") Then
                vb14.ClipPut(sNames)
                vb14.DialogBox("Lista plików - w clipboard")
                Return
            End If
        End If


        Dim oFE As FrameworkElement = sender
        Dim oSrc As Vblib.CloudPublish = oFE?.DataContext
        If oSrc Is Nothing Then Return

        Dim bSendNow As Boolean = True

        If oSrc.sProvider = Vblib.Publish_AdHoc.PROVIDERNAME Then
            Dim sFolder As String = SettingsGlobal.FolderBrowser("", "Gdzie wysłać pliki?")
            If sFolder = "" Then Return
            oSrc.sZmienneZnaczenie = sFolder
        Else
            bSendNow = Await vb14.DialogBoxYNAsync("Wysłać teraz? Bo mogę tylko zaznaczyć do wysłania")
        End If

        If bSendNow Then

            Dim lista As New List(Of Vblib.OnePic)

            For Each oItem As ThumbPicek In uiPicList.SelectedItems
                lista.Add(oItem.oPic)
            Next

            uiProgBar.Maximum = lista.Count
            uiProgBar.Visibility = Visibility.Visible

            Application.ShowWait(True)
            Await PublishAllFilesTo(oSrc, lista)
            Application.ShowWait(False)

            uiProgBar.Visibility = Visibility.Collapsed
        Else
            For Each oItem As ThumbPicek In uiPicList.SelectedItems
                oItem.oPic.AddCloudPublished(oSrc.konfiguracja.nazwa, "")
            Next
        End If

        SaveMetaData()  ' bo zmieniono dane plików

    End Sub

    Private Async Function PublishAllFilesTo(oSrc As Vblib.CloudPublish, lista As List(Of Vblib.OnePic)) As Task
        Dim sErr As String = Await oSrc.Login
        If sErr <> "" Then
            Await vb14.DialogBoxAsync(sErr)
            Return
        End If

        ' to pozwala robić dwie publikacje po kolei
        For Each oFile As Vblib.OnePic In lista
            oFile.ResetPipeline()
        Next

        sErr = Await oSrc.SendFiles(lista, AddressOf ProgBarInc)
        If sErr <> "" Then Await vb14.DialogBoxAsync(sErr)
        ' Await oLogin.Logout()
    End Function

    Private Sub ProgBarInc()

        uiProgBar.Value += 1
    End Sub

    'Private Async Function PublishOnePicTo(oLogin As CloudPublish, bSendNow As Boolean, oItem As ThumbPicek) As Task
    '    If bSendNow Then
    '        Await oLogin.SendFile(oItem.oPic)
    '    Else
    '        oItem.oPic.AddCloudPublished(oLogin.konfiguracja.nazwa, "")
    '    End If
    '    Await Task.Delay(1) ' na wszelki wypadek, żeby był czas na przerysowanie progbar, nawet jak tworzenie EXIFa jest empty
    '    uiProgBar.Value += 1
    'End Function


#Region "Keywords window"

    Private Function GetKwdWnd() As Window
        For Each oWnd As Window In Me.OwnedWindows
            If oWnd.Name = "BrowseKeywords" Then Return oWnd
        Next
        Return Nothing
    End Function

    ' było jak jeszcze nie było otwierania wielu sub-okien (#, describe, exif...)
    'Private Sub uiKeywords_Click(sender As Object, e As RoutedEventArgs)

    '    ' step 1: sprawdzenie czy nie ma takiego już otwartego
    '    Dim oWnd As Window = GetKwdWnd()
    '    If oWnd IsNot Nothing Then
    '        oWnd.BringIntoView()
    '        Return
    '    End If

    '    ' step 2: pokaż takie okno
    '    oWnd = New BrowseKeywordsWindow()
    '    oWnd.Owner = Me
    '    oWnd.Name = "BrowseKeywords"
    '    oWnd.Show()

    '    ' step 3: pokaż w nim selected pic
    '    If uiPicList.SelectedItems.Count = 1 Then
    '        Dim oItem As ThumbPicek = uiPicList.SelectedItems(0)
    '        RefreshOwnedWindows(oItem)
    '        ' ShowKwdForPic(oItem)
    '    End If
    'End Sub

    'Private Sub ShowKwdForPic(oPic As ThumbPicek)
    '    Dim oWnd As BrowseKeywordsWindow = GetKwdWnd()
    '    If oWnd IsNot Nothing Then
    '        oWnd.InitForPic(oPic)
    '    End If
    'End Sub

    Public Sub ChangedKeywords(oExif As Vblib.ExifTag, oPic1 As ThumbPicek)
        ' callback z BrowseKeywordsWindow - do zaznaczonego (jednego bądź wielu)

        If uiPicList.SelectedItems.Count < 1 Then Return

        If uiPicList.SelectedItems.Count = 1 Then
            'Dim oThumb As ThumbPicek = uiPicList.SelectedItems(0)
            ' tylko jeden wyselekcjonowany - to uznaj że dobry oThumb przychodzi z Keyword
            ' bo przez PgUp/PgDown mogliśmy przejść do innego zdjęcia
            oPic1.oPic.ReplaceOrAddExif(oExif)
            oPic1.oPic.RemoveFromDescriptions(oExif.Keywords, Application.GetKeywords)
            oPic1.ZrobDymek()

        Else
            Dim aKwds As String() = oExif.Keywords.Replace("|", " ").Split(" ")
            Dim aOpisy As String() = oExif.UserComment.Split("|")

            For Each oPic As ThumbPicek In uiPicList.SelectedItems

                ' 1) jeśli mamy jakieś tagi, to nowe tylko dołączamy do tego (nie ma wtedy wyłączania tagów)
                Dim oCurrExif As Vblib.ExifTag = oPic.oPic.GetExifOfType(Vblib.ExifSource.ManualTag)
                If oCurrExif IsNot Nothing Then

                    For iLp = 0 To aKwds.Count - 1
                        Dim kwd As String = aKwds(iLp).TrimStart
                        If Not oCurrExif.Keywords.Contains(kwd) Then
                            oCurrExif.Keywords &= " " & kwd
                            If iLp < aOpisy.Count Then oCurrExif.UserComment &= "|" & aOpisy(iLp)
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
                    oPic.oPic.ReplaceOrAddExif(oExif)
                End If

                oPic.oPic.RemoveFromDescriptions(oExif.Keywords, Application.GetKeywords)

                oPic.ZrobDymek()

            Next

        End If

        RefreshMiniaturki(True)

        SaveMetaData()

    End Sub



#End Region
#Region "ExifTag window"

    'Private Const EXIFTAG_WINDOW As String = "BrowseExifs"

    'Private Function GetExifWnd() As Window
    '    For Each oWnd As Window In Me.OwnedWindows
    '        If oWnd.Name = EXIFTAG_WINDOW Then Return oWnd
    '    Next
    '    Return Nothing
    'End Function

    Private Sub uiShowExifs_Click(sender As Object, e As RoutedEventArgs)
        uiActionsPopup.IsOpen = False

        Dim oItem As FrameworkElement = sender
        Dim oPicek As ThumbPicek = oItem.DataContext

        Dim oWnd As New ShowExifs(False) '(oPicek.oPic)

        ' step 2: pokaż takie okno
        oWnd.Owner = Me
        'oWnd.Name = EXIFTAG_WINDOW
        oWnd.DataContext = oPicek
        oWnd.Show()

    End Sub

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
        OpenSubWindow(New SimpleKeywords(_inArchive))
    End Sub

    Private Sub uiOknaKwdsTree_Click(sender As Object, e As RoutedEventArgs)
        OpenSubWindow(New BrowseKeywordsWindow(_inArchive))
    End Sub

    Private Sub uiOknaDescribe_Click(sender As Object, e As RoutedEventArgs)
        OpenSubWindow(New SimpleDescribe(_inArchive))
    End Sub

    Private Sub uiOknaManualExif_Click(sender As Object, e As RoutedEventArgs)
        OpenSubWindow(New EditOneExif(Vblib.ExifSource.Flattened, _inArchive))
    End Sub

    Private Sub uiOknaOCR_Click(sender As Object, e As RoutedEventArgs)
        OpenSubWindow(New SimpleOCR(_inArchive))
    End Sub

    Private Sub uiOknaTargetDir_Click(sender As Object, e As RoutedEventArgs)
        OpenSubWindow(New SimpleTargetDir)
    End Sub

    Private Sub uiOknaManualAzureExif_Click(sender As Object, e As RoutedEventArgs)
        OpenSubWindow(New EditOneExif(Vblib.ExifSource.AutoAzure, _inArchive))
    End Sub

#End Region

    'Private Sub ShowExifForPic(oPic As ThumbPicek)
    '    Dim oWnd As ShowExifs = GetExifWnd()
    '    If oWnd IsNot Nothing Then
    '        oWnd.SetForPic(oPic.oPic)
    '    End If
    'End Sub


#End Region


    Public Class ThumbPicek
        Public Property oPic As Vblib.OnePic
        Public Property sDymek As String 'XAML dymekCount
        Public Property oImageSrc As BitmapImage = Nothing ' XAML image
        Public Property iDuzoscH As Integer ' XAML height
        Public Property bVisible As Boolean = True
        Public Property dateMin As Date ' kopiowane z oThumb.Exifs(..)
        Public Property splitBefore As Integer
        Public Property widthPaskow As Integer
        Public Property dymekSplit As String = ""
        Public Property opacity As Double = 1   ' czyli normalnie pokazany

        Public Property podpis As String = ""
        Public Property AllKeywords As String
        Public Property SumOfDescriptionsText As String

        Sub New(picek As Vblib.OnePic, iMaxBok As Integer)
            oPic = picek
            iDuzoscH = iMaxBok
            ZrobDymek()
        End Sub

        Public Sub ZrobDymek()
            sDymek = oPic.sSuggestedFilename
            If oPic.sSourceName.ToLower <> "adhoc" Then sDymek = sDymek & vbCrLf & "Src: " & oPic.sSourceName

            Dim oExifTag As Vblib.ExifTag = oPic.GetExifOfType(Vblib.ExifSource.FileExif)
            If oExifTag IsNot Nothing Then
                sDymek = sDymek & vbCrLf & "Taken: " & oExifTag.DateTimeOriginal
            Else
                oExifTag = oPic.GetExifOfType(Vblib.ExifSource.SourceFile)
                sDymek = sDymek & vbCrLf & "(file: " & oExifTag.DateMin.ToExifString & ")"
            End If

            Dim sGeo As String = ""
            For Each oExif As Vblib.ExifTag In oPic.Exifs
                If oExif.GeoName <> "" Then sGeo = sGeo & vbCrLf & oExif.GeoName
            Next
            sGeo = sGeo.Trim
            If sGeo = "" Then
                Dim oPos As BasicGeopos = oPic.GetGeoTag
                If oPos IsNot Nothing Then sGeo = $"[{oPos.StringLat}, {oPos.StringLon}]"
            End If
            If sGeo <> "" Then sDymek = sDymek & vbCrLf & sGeo

            oExifTag = oPic.GetExifOfType(Vblib.ExifSource.AutoAzure)
            If oExifTag IsNot Nothing Then
                Dim sCaption As String = oExifTag.AzureAnalysis?.Captions?.GetList(0).ToDisplay
                sDymek = sDymek & vbCrLf & sCaption
            End If

            sDymek = sDymek & vbCrLf & "Descriptions: " & oPic.GetSumOfDescriptionsText & vbCrLf
            sDymek = sDymek & "Keywords: " & oPic.GetAllKeywords & vbCrLf

            If Not String.IsNullOrWhiteSpace(oPic.TargetDir) Then
                sDymek = sDymek & vbCrLf & "► " & oPic.TargetDir
            End If

        End Sub

    End Class

End Class

Public Class KonwersjaPasekKolor
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.Convert
        Dim temp As Integer = CType(value, Integer)

        If temp = SplitBeforeEnum.czas Then Return New SolidColorBrush(Colors.SkyBlue)
        If temp = SplitBeforeEnum.geo Then Return New SolidColorBrush(Colors.OrangeRed)

        ' i tak będzie niewidoczny, więc w sumie nie jest takie ważne, ale po co robić nowe obiekty
        Return Microsoft.Windows.Themes.ThemeColor.NormalColor
    End Function


    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New NotImplementedException()
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
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.Convert
        Dim bTemp As Boolean = CType(value, Integer) > 0

        If parameter IsNot Nothing Then
            Dim sParam As String = CType(parameter, String)
            If sParam.ToUpperInvariant = "NEG" Then bTemp = Not bTemp
        End If
        If bTemp Then Return Visibility.Visible

        Return Visibility.Collapsed
    End Function


    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New NotImplementedException()
    End Function
End Class

Public Class KonwersjaFileDiscrVisibility
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.Convert
        Dim bTemp As String = CType(value, String)

        If String.IsNullOrWhiteSpace(bTemp) Then Return Visibility.Collapsed

        Return Visibility.Visible
    End Function


    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New NotImplementedException()
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
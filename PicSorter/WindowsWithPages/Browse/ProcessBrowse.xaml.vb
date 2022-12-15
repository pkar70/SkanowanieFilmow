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


Imports System.Collections.ObjectModel
Imports System.Data
Imports System.Globalization
Imports System.IO
Imports System.Net.WebRequestMethods
Imports System.Security.Policy
Imports System.Xml
Imports Microsoft.Windows.Themes
Imports Vblib
Imports Windows.Media.Ocr
Imports Windows.Web.Http.Headers
Imports vb14 = Vblib.pkarlibmodule14



Public Class ProcessBrowse

    ' Private Const THUMBS_LIMIT As Integer = 9999

    Private _thumbsy As New ObservableCollection(Of ThumbPicek)
    Private _iMaxRun As Integer  ' po wczytaniu: liczba miniaturek, później: max ciąg zdjęć
    Private _redrawPending As Boolean = False
    Private _oBufor As Vblib.Buffer
    ' Private _MetadataWindow As ShowExifs

#Region "called on init"

    Public Sub New(bufor As Vblib.Buffer)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        _oBufor = bufor
    End Sub



    Private Async Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        Application.ShowWait(True)

        Await Bufor2Thumbsy()
        SizeMe()
        RefreshMiniaturki(True)

        WypelnMenuAutotagerami(uiMenuAutotags, AddressOf AutoTagRun)
        WypelnMenuBatchProcess(uiBatchProcessors, AddressOf PostProcessRun)
        WypelnMenuCloudPublish(Nothing, uiMenuPublish, AddressOf PublishRun)

        Await EwentualneKasowanieBak()
        Await EwentualneKasowanieArchived()
        ' *TODO* Await EwentualneKasowanieArchived() -

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

        Dim iArchCount As Integer = Application.GetArchivesList.Count
        Dim iCloudArchCount As Integer = Application.GetCloudArchives.GetList.Count


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

            If Await Vblib.DialogBoxYNAsync($"Niektóre pliki są zniknięte ({lDeleted.Count}, usunąć je z indeksu?") Then

                For Each oItem As Vblib.OnePic In lDeleted
                    _oBufor.GetList.Remove(oItem)
                Next
                SaveMetaData()

            End If
        End If

        Return lista
    End Function

    Private Async Function DoczytajMiniaturki() As Task

        uiProgBar.Value = 0
        uiProgBar.Visibility = Visibility.Visible

        For Each oItem As ThumbPicek In _thumbsy

            '' *TODO* tymczasowe, by wyłapywać tylko to co chcemy
            'If Not oItem.oThumb.MatchesMasks("*.avi") Then Continue For

            oItem.oImageSrc = Await WczytajObrazek(oItem.oPic.InBufferPathName, 400, Rotation.Rotate0)
            uiProgBar.Value += 1

        Next

    End Function

    ''' <summary>
    ''' przetworzenie danych Bufor na własną listę (thumbsów)
    ''' </summary>
    Private Async Function Bufor2Thumbsy() As Task

        _iMaxRun = _oBufor.Count

        uiProgBar.Maximum = _iMaxRun

        _thumbsy.Clear()
        Dim iMax As String = vb14.GetSettingsInt("uiMaxThumbs")

        Dim lista As List(Of ThumbPicek) = Await WczytajIndeks()   ' tu ewentualne kasowanie jest znikniętych, to wymaga YNAsync
        For Each oItem As ThumbPicek In From c In lista Order By c.dateMin Take iMax
            _thumbsy.Add(oItem)
        Next
        lista.Clear()

        uiProgBar.Value = 0
        Await DoczytajMiniaturki()

    End Function

    Private Sub Window_Closing(sender As Object, e As ComponentModel.CancelEventArgs)
        uiPicList.ItemsSource = Nothing

        For Each oPicek As ThumbPicek In _thumbsy
            oPicek.oImageSrc = Nothing
        Next

        GC.Collect()    ' usuwamy, bo dużo pamięci zwolniliśmy
    End Sub


#End Region

#Region "górny toolbox"

    Private Sub PokazThumbsy()
        uiPicList.ItemsSource = Nothing
        uiPicList.ItemsSource = From c In _thumbsy Where c.bVisible Order By c.dateMin
        Me.Title = $"Browse ({_thumbsy.Count} images)"
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

#Region "menu actions"
    Private Sub uiMenuCopyGeoTag_Click(sender As Object, e As RoutedEventArgs)
        uiActionsPopup.IsOpen = False

        If uiPicList.SelectedItems.Count < 2 Then
            vb14.DialogBox("Funkcja kopiowania GeoTag wymaga zaznaczenia przynajmniej dwu zdjęć")
            Return
        End If

        ' step 1: znajdź pierwszy geotag
        Dim oNewGeoTag As New Vblib.ExifTag(ExifSource.ManualGeo)
        Dim oExifOSM As Vblib.ExifTag = Nothing
        Dim oExifImgw As Vblib.ExifTag = Nothing

        For Each oItem As ThumbPicek In uiPicList.SelectedItems
            Dim oGeo As MyBasicGeoposition = oItem.oPic.GetGeoTag
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
            Dim oCurrGeo As MyBasicGeoposition = oItem.oPic.GetGeoTag
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

    Private Sub uiMenuCreateGeoTag_Click(sender As Object, e As RoutedEventArgs)
        uiActionsPopup.IsOpen = False

        Dim oWnd As New EnterGeoTag
        If Not oWnd.ShowDialog Then Return

        Dim oNewGeoTag As New Vblib.ExifTag(ExifSource.ManualGeo)
        oNewGeoTag.GeoTag = oWnd.GetGeoPos

        For Each oItem As ThumbPicek In uiPicList.SelectedItems
            oItem.oPic.ReplaceOrAddExif(oNewGeoTag)
            oItem.ZrobDymek()

            If _isGeoFilterApplied Then oItem.opacity = _OpacityWygas
        Next

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

        ' pokaz na nowo obrazki
        RefreshMiniaturki(False)

        SaveMetaData()
    End Sub

#End Region

#End Region

#Region "Describe"

    Private Sub uiDescribeSelected_Click(sender As Object, e As RoutedEventArgs)
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
        If Not oWnd.ShowDialog Then Return

        Dim oDesc As Vblib.OneDescription = oWnd.GetDescription
        oPicek.oPic.AddDescription(oDesc)

        SaveMetaData()  ' bo zmieniono EXIF
    End Sub
#End Region


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

        ' *TODO* reakcja jakaś na inne typy niż JPG
        ' *TODO* dla NAR (Lumia950), MP4 (Lumia*), AVI (Fuji), MOV (iPhone) są specjalne obsługi
        bitmap.UriSource = New Uri(sPathName)
        bitmap.EndInit()
        Await Task.Delay(1) ' na potrzeby ProgressBara

        Return bitmap
    End Function



#Region "ShowBig i callbacki z niego"

#Region "double click dla ShowBig"

    Private _DblClickLastPicek As String
    Private _DblClickLastDate As Date

    Private Sub uiImage_LeftClick(sender As Object, e As MouseButtonEventArgs)
        Dim oItem As FrameworkElement = sender
        Dim oPicek As ThumbPicek = oItem?.DataContext

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
        Dim oWnd As New ShowBig(oPicek)
        oWnd.Owner = Me
        oWnd.Show()

        Task.Delay(100) ' bo czasem focus wraca do Browser i chodzenie nie działa
        oWnd.Focus()
    End Sub

    Public Function FromBig_Delete(oPic As ThumbPicek) As ThumbPicek
        ' skasować plik, zwróć następny
        Dim oNext As ThumbPicek = FromBig_Next(oPic, False)
        DeletePicture(oPic)
        SaveMetaData()
        _redrawPending = True
        Return oNext
    End Function

    Public Function FromBig_Next(oPic As ThumbPicek, bGoBack As Boolean) As ThumbPicek

        If uiPicList.SelectedItems.Count > 1 Then
            Return FromBig_NextMain(oPic, bGoBack, uiPicList.SelectedItems, True)
        Else
            Return FromBig_NextMain(oPic, bGoBack, _thumbsy.ToList, False)
        End If

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
                        ShowKwdForPic(oPicRet)
                        ShowExifForPic(oPicRet)
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
                        ShowKwdForPic(oPicRet)
                        ShowExifForPic(oPicRet)
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

        ' zapisz jako plik do kiedyś-tam usunięcia ze źródła
        Application.GetSourcesList.AddToPurgeList(oPicek.oPic.sSourceName, oPicek.oPic.sInSourceID)

        ' przesunięcie "dzielnika" *TODO* bezpośrednio na liscie
        If oPicek.splitBefore Then _ReapplyAutoSplit = True

        ' skasuj z tutejszej listy
        _thumbsy.Remove(oPicek)

    End Sub

    Private Async Sub uiDelOne_Click(sender As Object, e As RoutedEventArgs)
        Dim oItem As FrameworkElement = sender
        Dim oPicek As ThumbPicek = oItem?.DataContext

        If Not vb14.GetSettingsBool("uiNoDelConfirm") Then
            If Not Await vb14.DialogBoxYNAsync("Skasować zdjęcie?") Then Return
        End If

        _ReapplyAutoSplit = False
        DeletePicture(oPicek)

        SaveMetaData()

        ' pokaz na nowo obrazki
        RefreshMiniaturki(_ReapplyAutoSplit)
    End Sub

    Private Async Sub uiDeleteSelected_Click(sender As Object, e As RoutedEventArgs)
        ' delete selected
        If uiPicList.SelectedItems Is Nothing Then Return

        Dim lLista As New List(Of ThumbPicek)
        For Each oItem As ThumbPicek In uiPicList.SelectedItems
            lLista.Add(oItem)
        Next

        If Not vb14.GetSettingsBool("uiNoDelConfirm") Then
            If Not Await vb14.DialogBoxYNAsync($"Skasować zdjęcia? ({lLista.Count}") Then Return
        End If

        _ReapplyAutoSplit = False

        For Each oItem As ThumbPicek In lLista
            DeletePicture(oItem)
        Next

        SaveMetaData()    ' tylko raz, po całej serii kasowania

        ' pokaz na nowo obrazki
        RefreshMiniaturki(_ReapplyAutoSplit)
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

        Dim lastDate As New Date(1970, 1, 1) ' yyyyMMddHHmmss

        For Each oItem As ThumbPicek In _thumbsy
            If lastDate < oItem.dateMin Then oItem.splitBefore = SplitBeforeEnum.czas
            lastDate = oItem.dateMin.AddHours(hours)
        Next
    End Sub

    Private Sub ApplyAutoSplitGeo(kiloms As Integer)

        Dim lastGeo As Vblib.MyBasicGeoposition = MyBasicGeoposition.EmptyGeoPos ' (0, -150)    ' raczej tam nie będę, środek oceanu

        For Each oItem As ThumbPicek In _thumbsy
            Dim geoExif As Vblib.MyBasicGeoposition = oItem.oPic.GetGeoTag
            If geoExif IsNot Nothing Then
                If lastGeo.DistanceTo(geoExif) > kiloms Then oItem.splitBefore = SplitBeforeEnum.geo
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
    Private _OpacityWygas As Double = 0.3

    Private Sub uiFilterAll_Click(sender As Object, e As RoutedEventArgs)
        uiFilterPopup.IsOpen = False
        uiFilters.Content = "Filters"

        For Each oItem In _thumbsy
            oItem.opacity = 1
        Next

        _isGeoFilterApplied = False

        RefreshMiniaturki(False)
    End Sub
    Private Sub uiFilterNoGeo_Click(sender As Object, e As RoutedEventArgs)
        vb14.DumpCurrMethod()

        uiFilterPopup.IsOpen = False
        uiFilters.Content = "no geo"

        For Each oItem In _thumbsy
            If oItem.oPic.GetGeoTag IsNot Nothing Then
                oItem.opacity = _OpacityWygas
            Else
                oItem.opacity = 1
            End If
        Next

        _isGeoFilterApplied = True

        RefreshMiniaturki(False)
    End Sub

    Private Sub uiFilterNoAzure_Click(sender As Object, e As RoutedEventArgs)
        uiFilterPopup.IsOpen = False
        uiFilters.Content = "no azure"

        For Each oItem In _thumbsy
            If oItem.oPic.GetExifOfType("AUTO_AZURE") IsNot Nothing Then
                oItem.opacity = _OpacityWygas
            Else
                oItem.opacity = 1
            End If
        Next

        _isGeoFilterApplied = False

        RefreshMiniaturki(False)
    End Sub


    Private Sub uiFilterNoTarget_Click(sender As Object, e As RoutedEventArgs)
        uiFilterPopup.IsOpen = False
        uiFilters.Content = "no dir"

        For Each oItem In _thumbsy
            If String.IsNullOrWhiteSpace(oItem.oPic.TargetDir) Then
                oItem.opacity = 1
            Else
                oItem.opacity = _OpacityWygas
            End If
        Next

        _isGeoFilterApplied = False

        RefreshMiniaturki(False)
    End Sub

    Private Sub uiFilter_Click(sender As Object, e As RoutedEventArgs)
        uiFilterPopup.IsOpen = Not uiFilterPopup.IsOpen
    End Sub

#End Region

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
            ShowKwdForPic(oItem)
            ShowExifForPic(oItem)
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
        Dim oEngine As AutotaggerBase = oFE?.DataContext
        If oEngine Is Nothing Then Return

        uiProgBar.Value = 0
        uiProgBar.Maximum = uiPicList.SelectedItems.Count
        uiProgBar.Visibility = Visibility.Visible

        Application.ShowWait(True)
        For Each oItem As ThumbPicek In uiPicList.SelectedItems
            uiProgBar.ToolTip = oItem.oPic.InBufferPathName
            If oItem.oPic.GetExifOfType(oEngine.Nazwa) Is Nothing Then
                Dim oExif As Vblib.ExifTag = Await oEngine.GetForFile(oItem.oPic)
                If oExif IsNot Nothing Then
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
            Await oEngine.Apply(oItem.oPic)
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
            Dim oExif As Vblib.ExifTag = oItem.oPic.GetExifOfType(Vblib.ExifSource.AutoAzure)
            If String.IsNullOrWhiteSpace(oExif?.AzureAnalysis?.Wiekowe) Then Continue For
            iCnt += 1
            sNames = sNames & vbCrLf & oItem.oPic.sSuggestedFilename
        Next
        If iCnt > 0 Then
            Dim sMsg As String = "plików zawiera"
            If iCnt = 1 Then sMsg = "plik zawiera"
            If iCnt > 1 AndAlso iCnt < 5 Then sMsg = "pliki zawierają"

            If Not Await vb14.DialogBoxYNAsync($"{iCnt} {sMsg} ograniczenia wiekowe, kontynuować? ") Then
                vb14.ClipPut(sNames)
                DialogBox("Lista plików - w clipboard")
                Return
            End If
        End If


        Dim oFE As FrameworkElement = sender
        Dim oSrc As Vblib.CloudPublish = oFE?.DataContext
        If oSrc Is Nothing Then Return

        Dim bSendNow As Boolean = Await vb14.DialogBoxYNAsync("Wysłać teraz? Bo mogę tylko zaznaczyć do wysłania")

        If oSrc.sProvider = Publish_AdHoc.PROVIDERNAME Then
            Dim sFolder As String = SettingsGlobal.FolderBrowser("", "Gdzie wysłać pliki?")
            If sFolder = "" Then Return
            oSrc.sZmienneZnaczenie = sFolder
        End If


        uiProgBar.Maximum = uiPicList.SelectedItems.Count
        uiProgBar.Visibility = Visibility.Visible

        Application.ShowWait(True)
        Await PublishAllFilesTo(oSrc, bSendNow)
        Application.ShowWait(False)

        uiProgBar.Visibility = Visibility.Collapsed

        SaveMetaData()  ' bo zmieniono dane plików

    End Sub

    Private Async Function PublishAllFilesTo(oSrc As CloudPublish, bSendNow As Boolean) As Task
        Dim sErr As String = Await oSrc.Login
        If sErr <> "" Then
            Await vb14.DialogBoxAsync(sErr)
            Return
        End If

        For Each oItem As ThumbPicek In uiPicList.SelectedItems
            Await PublishOnePicTo(oSrc, bSendNow, oItem)
        Next

        ' Await oSrc.Logout()
    End Function

    Private Async Function PublishOnePicTo(oSrc As CloudPublish, bSendNow As Boolean, oItem As ThumbPicek) As Task
        If bSendNow Then
            Await oSrc.SendFile(oItem.oPic)
        Else
            oItem.oPic.AddCloudPublished(oSrc.konfiguracja.nazwa, "")
        End If
        Await Task.Delay(1) ' na wszelki wypadek, żeby był czas na przerysowanie progbar, nawet jak tworzenie EXIFa jest empty
        uiProgBar.Value += 1
    End Function

    Public Class ThumbPicek
        Public Property oPic As Vblib.OnePic
        Public Property sDymek As String 'XAML dymekCount
        Public Property oImageSrc As BitmapImage = Nothing ' XAML image
        Public Property iDuzoscH As Integer ' XAML height
        Public Property bVisible As Boolean = True
        Public Property dateMin As Date ' kopiowane z oThumb.Exifs(..)
        Public Property splitBefore As Integer
        Public Property widthPaskow As Integer
        Public Property opacity As Double = 1   ' czyli normalnie pokazany

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
                Dim oPos As Vblib.MyBasicGeoposition = oPic.GetGeoTag
                If oPos IsNot Nothing Then sGeo = $"[{oPos.StringLat}, {oPos.StringLon}]"
            End If
            If sGeo <> "" Then sDymek = sDymek & vbCrLf & sGeo

            oExifTag = oPic.GetExifOfType(Vblib.ExifSource.AutoAzure)
            If oExifTag IsNot Nothing Then
                Dim sCaption As String = oExifTag.AzureAnalysis?.Captions?.GetList(0).ToDisplay
                sDymek = sDymek & vbCrLf & sCaption
            End If

            If Not String.IsNullOrWhiteSpace(oPic.TargetDir) Then
                sDymek = sDymek & vbCrLf & "► " & oPic.TargetDir
            End If

        End Sub

    End Class

#Region "Keywords window"

    Private Function GetKwdWnd() As Window
        For Each oWnd As Window In Me.OwnedWindows
            If oWnd.Name = "BrowseKeywords" Then Return oWnd
        Next
        Return Nothing
    End Function

    Private Sub uiKeywords_Click(sender As Object, e As RoutedEventArgs)

        ' step 1: sprawdzenie czy nie ma takiego już otwartego
        Dim oWnd As Window = GetKwdWnd()
        If oWnd IsNot Nothing Then
            oWnd.BringIntoView()
            Return
        End If

        ' step 2: pokaż takie okno
        oWnd = New BrowseKeywordsWindow()
        oWnd.Owner = Me
        oWnd.Name = "BrowseKeywords"
        oWnd.Show()

        ' step 3: pokaż w nim selected pic
        If uiPicList.SelectedItems.Count = 1 Then
            Dim oItem As ThumbPicek = uiPicList.SelectedItems(0)
            ShowKwdForPic(oItem)
        End If
    End Sub

    Private Sub ShowKwdForPic(oPic As ThumbPicek)
        Dim oWnd As BrowseKeywordsWindow = GetKwdWnd()
        If oWnd IsNot Nothing Then
            oWnd.InitForPic(oPic)
        End If
    End Sub

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
            For Each oPic As ThumbPicek In uiPicList.SelectedItems

                ' 1) jeśli mamy jakieś tagi, to nowe tylko dołączamy do tego (nie ma wtedy wyłączania tagów)
                Dim oCurrExif As Vblib.ExifTag = oPic.oPic.GetExifOfType(Vblib.ExifSource.ManualTag)
                If oCurrExif IsNot Nothing Then
                    oCurrExif.Keywords = oCurrExif.Keywords & " " & oExif.Keywords
                    oCurrExif.UserComment = oCurrExif.UserComment & " | " & oExif.UserComment
                    oCurrExif.DateMax = oCurrExif.DateMax.DateMax(oExif.DateMax)
                    oCurrExif.DateMin = oCurrExif.DateMin.DateMin(oExif.DateMin)

                    ' wygrywa nowo dodany tag z geo (radiusa już tu nie widać)
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

    Private Const EXIFTAG_WINDOW As String = "BrowseExifs"

    Private Function GetExifWnd() As Window
        For Each oWnd As Window In Me.OwnedWindows
            If oWnd.Name = EXIFTAG_WINDOW Then Return oWnd
        Next
        Return Nothing
    End Function

    Private Sub uiShowExifs_Click(sender As Object, e As RoutedEventArgs)
        uiActionsPopup.IsOpen = False

        Dim oItem As FrameworkElement = sender
        Dim oPicek As ThumbPicek = oItem.DataContext

        Dim oWnd As New ShowExifs(oPicek.oPic)

        ' step 2: pokaż takie okno
        oWnd.Owner = Me
        oWnd.Name = EXIFTAG_WINDOW
        oWnd.Show()

    End Sub

    Private Sub ShowExifForPic(oPic As ThumbPicek)
        Dim oWnd As ShowExifs = GetExifWnd()
        If oWnd IsNot Nothing Then
            oWnd.SetForPic(oPic.oPic)
        End If
    End Sub


#End Region
End Class

Public Class KonwersjaPasekKolor
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.Convert
        Dim temp As Integer = CType(value, Integer)

        If temp = SplitBeforeEnum.czas Then Return New SolidColorBrush(Colors.SkyBlue)
        If temp = SplitBeforeEnum.geo Then Return New SolidColorBrush(Colors.OrangeRed)

        ' i tak będzie niewidoczny, więc w sumie nie jest takie ważne, ale po co robić nowe obiekty
        Return ThemeColor.NormalColor
    End Function


    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New NotImplementedException()
    End Function
End Class

Public Class KonwersjaPasekWysok
    Implements IMultiValueConverter

    Public Function Convert(values() As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IMultiValueConverter.Convert

        Dim splitBefore As Integer = CType(values.ElementAt(0), Integer)
        Dim height As Integer = CType(values.ElementAt(1), Integer)

        Select Case splitBefore
            Case SplitBeforeEnum.geo
                Return height / 2.0
            Case Else
                Return height * 1.0
        End Select

    End Function


    Public Function ConvertBack(value As Object, targetTypes() As Type, parameter As Object, culture As CultureInfo) As Object() Implements IMultiValueConverter.ConvertBack
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

Public Enum SplitBeforeEnum
    none
    czas
    geo
End Enum
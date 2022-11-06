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


Imports Vblib
Imports vb14 = Vblib.pkarlibmodule14



Public Class ProcessBrowse

    Private _thumbsy As New List(Of ThumbPicek)
    Private _iMaxRun As Integer  ' po wczytaniu: liczba miniaturek, później: max ciąg zdjęć

#Region "called on init"

    Private Async Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        Await Bufor2Thumbsy()
        SizeMe()
        RefreshMiniaturki(True)

        WypelnMenuAutotagerami(uiMenuAutotags, AddressOf AutoTagRun)

    End Sub


    ''' <summary>
    ''' zmiana rozmiaru Window na prawie cały ekran
    ''' </summary>
    Private Sub SizeMe()
        Me.Width = SystemParameters.FullPrimaryScreenWidth * 0.9
        Me.Height = SystemParameters.FullPrimaryScreenHeight * 0.9
    End Sub

    ''' <summary>
    ''' przetworzenie danych Bufor na własną listę (thumbsów)
    ''' </summary>
    Private Async Function Bufor2Thumbsy() As Task

        _iMaxRun = Application.GetBuffer.Count

        uiProgBar.Maximum = _iMaxRun
        uiProgBar.Value = 0
        uiProgBar.Visibility = Visibility.Visible

        Dim iMaxBok As Integer = GetMaxBok(_iMaxRun)

        ' Dim iLimit As Integer = 16

        For Each oItem As Vblib.OnePic In Application.GetBuffer.GetList
            If Not IO.File.Exists(oItem.InBufferPathName) Then Continue For   ' zabezpieczenie przed samoznikaniem

            Dim oNew As New ThumbPicek(oItem, iMaxBok)
            oNew.oImageSrc = Await SkalujObrazek(oItem.InBufferPathName, 400)

            oNew.dateMin = DataDoSortowania(oItem)
            uiProgBar.Value += 1
            _thumbsy.Add(oNew)

            'iLimit -= 1
            'If iLimit < 0 Then Exit For
        Next

        uiProgBar.Visibility = Visibility.Hidden

    End Function

    ''' <summary>
    ''' data do wsortowania obrazka - dateMin z sourcefile, jako że to najpewniejsza (ustawiana przy import pic)
    ''' </summary>
    ''' <param name="dlaZdjecia"></param>
    ''' <returns></returns>
    Private Function DataDoSortowania(dlaZdjecia As Vblib.OnePic) As Date
        Dim oExif As Vblib.ExifTag = dlaZdjecia.GetExifOfType(Vblib.ExifSource.SourceFile)
        ' jakby co, ale zakładam że jest ten ExifSource... data potrzebna do sortowania plików
        If oExif Is Nothing Then Return Date.Now

        Return oExif.DateMin
    End Function

    Private Sub Window_Closing(sender As Object, e As ComponentModel.CancelEventArgs)
        uiPicList.ItemsSource = Nothing

        For Each oPicek As ThumbPicek In _thumbsy
            oPicek.oImageSrc = Nothing
        Next

        GC.Collect()    ' usuwamy, bo dużo pamięci zwolniliśmy
    End Sub


#End Region

    Private Sub PokazThumbsy()
        uiPicList.ItemsSource = Nothing
        uiPicList.ItemsSource = From c In _thumbsy Where c.bVisible Order By c.dateMin
        Me.Title = $"Browse buffer ({_thumbsy.Count} images)"
    End Sub

    Private Sub uiOpenHistoragam_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New HistogramWindow
        oWnd.Show()
    End Sub

    Private Sub uiShowExifs_Click(sender As Object, e As RoutedEventArgs)
        Dim oItem As FrameworkElement = sender
        Dim oPicek As ThumbPicek = oItem.DataContext

        Dim oWnd As New ShowExifs(oPicek.oPic)
        oWnd.Show()
    End Sub

    Private Sub uiDescribeSelected_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New AddDescription(Nothing)
        If Not oWnd.ShowDialog Then Return

        Dim oDesc As Vblib.OneDescription = oWnd.GetDescription

        For Each oItem As ThumbPicek In uiPicList.SelectedItems
            oItem.oPic.AddDescription(oDesc)
        Next

        Application.GetBuffer.SaveData()  ' bo zmieniono EXIF
    End Sub

    Private Sub uiDescribe_Click(sender As Object, e As RoutedEventArgs)
        Dim oItem As FrameworkElement = sender
        Dim oPicek As ThumbPicek = oItem.DataContext

        Dim oWnd As New AddDescription(oPicek.oPic)
        If Not oWnd.ShowDialog Then Return

        Dim oDesc As Vblib.OneDescription = oWnd.GetDescription
        oPicek.oPic.AddDescription(oDesc)

        Application.GetBuffer.SaveData()  ' bo zmieniono EXIF
    End Sub


    ''' <summary>
    ''' wczytaj ze skalowaniem do 400 na wiekszym boku
    ''' (SzukajPicka tu ma błąd, olbrzymie ilości pamięci zjada - bo nie ma skalowania)
    ''' </summary>
    ''' <param name="sPathName"></param>
    ''' <returns></returns>
    Public Shared Async Function SkalujObrazek(sPathName As String, iMaxSize As Integer) As Task(Of BitmapImage)
        Dim bitmap = New BitmapImage()
        bitmap.BeginInit()
        If iMaxSize > 0 Then bitmap.DecodePixelHeight = iMaxSize
        bitmap.CacheOption = BitmapCacheOption.OnLoad ' Close na Stream uzyty do ładowania
        bitmap.UriSource = New Uri(sPathName)
        bitmap.EndInit()
        Await Task.Delay(1)

        Return bitmap
    End Function

    Private Sub uiCopyPath_Click(sender As Object, e As RoutedEventArgs)
        Dim oItem As FrameworkElement = sender
        Dim oPicek As ThumbPicek = oItem.DataContext
        vb14.ClipPut(oPicek.oPic.InBufferPathName)
    End Sub

    Private Sub uiShowBig_Click(sender As Object, e As RoutedEventArgs)
        Dim oItem As FrameworkElement = sender
        Dim oPicek As ThumbPicek = oItem?.DataContext

        If oPicek Is Nothing Then Return

        Dim oWnd As New ShowBig(oPicek)
        oWnd.Show()

    End Sub

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
        If Not Application.GetBuffer.DeleteFile(oPicek.oPic) Then Return   ' nieudane skasowanie

        ' zapisz jako plik do kiedyś-tam usunięcia ze źródła
        Application.GetSourcesList.AddToPurgeList(oPicek.oPic.sSourceName, oPicek.oPic.sInSourceID)

        ' przesunięcie "dzielnika" *TODO* bezpośrednio na liscie
        If oPicek.splitBefore Then _ReapplyAutoSplit = True

        ' skasuj z tutejszej listy
        _thumbsy.Remove(oPicek)

    End Sub

    Private Sub uiDelOne_Click(sender As Object, e As RoutedEventArgs)
        Dim oItem As FrameworkElement = sender
        Dim oPicek As ThumbPicek = oItem?.DataContext

        _ReapplyAutoSplit = False
        DeletePicture(oPicek)

        Application.GetBuffer.SaveData()

        ' pokaz na nowo obrazki
        RefreshMiniaturki(_ReapplyAutoSplit)
    End Sub

    Private Sub uiDeleteSelected_Click(sender As Object, e As RoutedEventArgs)
        ' delete selected
        If uiPicList.SelectedItems Is Nothing Then Return

        Dim lLista As New List(Of ThumbPicek)
        For Each oItem As ThumbPicek In uiPicList.SelectedItems
            lLista.Add(oItem)
        Next

        _ReapplyAutoSplit = False

        For Each oItem As ThumbPicek In lLista
            DeletePicture(oItem)
        Next

        Application.GetBuffer.SaveData()    ' tylko raz, po całej serii kasowania

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
                oItem.splitBefore = True
            End If
        Next
    End Sub

    Private Sub ApplyAutoSplitHours(hours As Integer)

        Dim lastDate As New Date(1970, 1, 1) ' yyyyMMddHHmmss

        For Each oItem As ThumbPicek In _thumbsy
            If lastDate < oItem.dateMin Then oItem.splitBefore = True
            lastDate = oItem.dateMin.AddHours(hours)
        Next
    End Sub

    Private Sub ApplyAutoSplitGeo(kiloms As Integer)

        Dim lastGeo As New Vblib.MyBasicGeoposition(0, -150)    ' raczej tam nie będę, środek oceanu

        For Each oItem As ThumbPicek In _thumbsy
            Dim geoExif As Vblib.MyBasicGeoposition = oItem.oPic.GetGeoTag
            If geoExif IsNot Nothing Then
                If lastGeo.DistanceTo(geoExif) > kiloms Then oItem.splitBefore = True
                lastGeo = geoExif
            End If
        Next
    End Sub

    Private Function PoliczMaxRun() As Integer

        Dim iMax As Integer = 0
        Dim iCurr As Integer = 1

        For Each oItem As ThumbPicek In _thumbsy
            If oItem.splitBefore Then
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

        If vb14.GetSettingsBool("uiDayChange") Then ApplyAutoSplitDaily()
        If vb14.GetSettingsBool("uiHourGapOn", True) Then ApplyAutoSplitHours(vb14.GetSettingsInt("uiHourGapInt", 18))
        If vb14.GetSettingsBool("uiGeoGapOn", True) Then ApplyAutoSplitGeo(vb14.GetSettingsInt("uiGeoGapInt", 20))

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

        RefreshMiniaturki(True)
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

#End Region
    Private Async Sub AutoTagRun(sender As Object, e As RoutedEventArgs)
        uiActionsPopup.IsOpen = False

        Dim oFE As FrameworkElement = sender
        Dim oEngine As AutotaggerBase = oFE?.DataContext
        If oEngine Is Nothing Then Return

        uiProgBar.Maximum = uiPicList.SelectedItems.Count
        uiProgBar.Visibility = Visibility.Visible

        For Each oItem As ThumbPicek In uiPicList.SelectedItems
            If oItem.oPic.GetExifOfType(oEngine.Nazwa) Is Nothing Then
                oItem.oPic.Exifs.Add(Await oEngine.GetForFile(oItem.oPic))
                oItem.oPic.TagsChanged = True

                Await Task.Delay(3) ' na wszelki wypadek, żeby był czas na przerysowanie progbar, nawet jak tworzenie EXIFa jest empty
            End If
            uiProgBar.Value += 1
        Next

        uiProgBar.Visibility = Visibility.Collapsed

        Application.GetBuffer.SaveData()  ' bo zmieniono EXIF

    End Sub


    Public Class ThumbPicek
        Public Property oPic As Vblib.OnePic
        Public Property sDymek As String 'XAML dymek
        Public Property oImageSrc As BitmapImage = Nothing ' XAML image
        Public Property iDuzoscH As Integer ' XAML height
        Public Property bVisible As Boolean = True
        Public Property dateMin As Date ' kopiowane z oPic.Exifs(..)
        Public Property splitBefore As Boolean
        Public Property widthPaskow As Integer

        Sub New(picek As Vblib.OnePic, iMaxBok As Integer)
            oPic = picek
            sDymek = oPic.sSuggestedFilename
            iDuzoscH = iMaxBok
        End Sub

    End Class


End Class


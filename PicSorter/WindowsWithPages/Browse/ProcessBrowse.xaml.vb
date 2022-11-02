' pokazywanie zdjęć

' 1) prosty przegląd, zaraz po Download - żeby:
' a) skasować niepotrzebne, których nie ma sensu "autotagować"
' b) ułatwić autosortowanie (według dat)

' 2) pełniejszy przegląd, później

' FUNKCJONALNOŚCI:
' 1) kasowanie - odwołanie do Buffer, który dopisuje do Purge i kasuje z listy
' 2) crop
' 3) rotate - ze skasowaniem z EXIF informacji o obrocie
' 4) resize
' 5) shell open edit
' ** uwaga! zachować daty plików?

' 'histogram' zdjęć versus czas, DateMin / DateMax, count per day
' gallery
' big pic
' toolbox: delete, crop, rotate, resize (może być automat jakiś)
' EXIF per oglądany obrazek, oraz per zaznaczone (EXIFSource: MANUAL & yyMMdd-HHmmss)


Imports System.Security.Policy
Imports PicSorterNS.ProcessBrowse
Imports vb14 = Vblib.pkarlibmodule14



Public Class ProcessBrowse

    Private _thumbsy As New List(Of ThumbPicek)

    Private Async Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        Await Bufor2Thumbsy()
        SizeMe()
        PokazThumbsy()

    End Sub

    ''' <summary>
    ''' zmiana rozmiaru Windows na prawie cały ekran
    ''' </summary>
    Private Sub SizeMe()
        Me.Width = System.Windows.SystemParameters.FullPrimaryScreenWidth * 0.9
        Me.Height = System.Windows.SystemParameters.FullPrimaryScreenHeight * 0.9
    End Sub

    Private Sub PokazThumbsy()
        uiPicList.ItemsSource = Nothing
        uiPicList.ItemsSource = _thumbsy
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
    ''' <summary>
    ''' przetworzenie danych Bufor na własną listę (thumbsów)
    ''' </summary>
    Private Async Function Bufor2Thumbsy() As Task

        uiProgBar.Maximum = Application.GetBuffer.Count
        uiProgBar.Value = 0
        uiProgBar.Visibility = Visibility.Visible

        Dim iMaxBok As Integer = GetMaxBok(uiProgBar.Maximum)

        ' Dim iLimit As Integer = 16

        For Each oItem As Vblib.OnePic In Application.GetBuffer.GetList
            If Not IO.File.Exists(oItem.InBufferPathName) Then Continue For   ' zabezpieczenie przed samoznikaniem

            Dim oNew As New ThumbPicek(oItem, iMaxBok)
            oNew.oImageSrc = Await SkalujObrazek(oItem.InBufferPathName, 400)
            uiProgBar.Value += 1
            _thumbsy.Add(oNew)

            'iLimit -= 1
            'If iLimit < 0 Then Exit For
        Next

        uiProgBar.Visibility = Visibility.Hidden

    End Function

    ''' <summary>
    ''' wczytaj ze skalowaniem do 400 na wiekszym boku
    ''' (SzukajPicka tu ma błąd, olbrzymie ilości pamięci zjada - bo nie ma skalowania)
    ''' </summary>
    ''' <param name="sPathName"></param>
    ''' <returns></returns>
    Private Async Function SkalujObrazek(sPathName As String, iMaxSize As Integer) As Task(Of BitmapImage)
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

    Private Async Sub uiShowBig_Click(sender As Object, e As RoutedEventArgs)
        Dim oItem As FrameworkElement = sender
        Dim oPicek As ThumbPicek = oItem?.DataContext

        If oPicek Is Nothing Then Return

        Dim oImage As New Image
        oImage.Source = Await SkalujObrazek(oPicek.oPic.InBufferPathName, 0)

        Dim oStack As StackPanel = New StackPanel
        oStack.Children.Add(oImage)
        'oStack.Children.Add(oButt)

        Dim oWin As Window = New Window
        oWin.Content = oStack
        oWin.Title = oPicek.sDymek
        oWin.Show()

        'Dim oWnd As New ShowBig(oPicek)
        'oWnd.Show()

    End Sub

#Region "delete"
    Private Sub DeletePicture(oPicek As ThumbPicek)
        If oPicek Is Nothing Then Return

        GC.Collect()    ' zabezpieczenie jakby tu był jeszcze otwarty plik jakiś

        ' usuń z bufora (z listy i z katalogu), ale nie zapisuj indeksu (jakby to była seria kasowania)
        If Not Application.GetBuffer.DeleteFile(oPicek.oPic) Then Return    ' nieudane skasowanie

        ' zapisz jako plik do kiedyś-tam usunięcia ze źródła
        Application.GetSourcesList.AddToPurgeList(oPicek.oPic.sSourceName, oPicek.oPic.sInSourceID)

        ' skasuj z tutejszej listy
        _thumbsy.Remove(oPicek)

    End Sub

    Private Sub uiDelOne_Click(sender As Object, e As RoutedEventArgs)
        Dim oItem As FrameworkElement = sender
        Dim oPicek As ThumbPicek = oItem?.DataContext
        DeletePicture(oPicek)

        Application.GetBuffer.SaveData()

        ' pokaz na nowo obrazki
        SkalujRozmiarMiniaturek()
        PokazThumbsy()
    End Sub

    Private Sub uiDeleteSelected_Click(sender As Object, e As RoutedEventArgs)
        ' delete selected
        If uiPicList.SelectedItems Is Nothing Then Return

        Dim lLista As New List(Of ThumbPicek)
        For Each oItem As ThumbPicek In uiPicList.SelectedItems
            lLista.Add(oItem)
        Next

        For Each oItem As ThumbPicek In lLista
            DeletePicture(oItem)
        Next

        Application.GetBuffer.SaveData()    ' tylko raz, po całej serii kasowania

        ' pokaz na nowo obrazki
        SkalujRozmiarMiniaturek()
        PokazThumbsy()
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
        Dim iMaxBok As Integer = Math.Sqrt(iPixPerPic)
        Return iMaxBok
    End Function

    Private Sub SkalujRozmiarMiniaturek()

        Dim iMaxBok As Integer = GetMaxBok(_thumbsy.Count)

        Dim sRequest As String = TryCast(uiComboSize.SelectedValue, ComboBoxItem).Content
        If sRequest <> "fit" Then iMaxBok = sRequest

        For Each oItem In _thumbsy
            If oItem.oImageSrc.Width > oItem.oImageSrc.Height Then
                oItem.iDuzoscH = iMaxBok * 0.66
            Else
                oItem.iDuzoscH = iMaxBok
            End If
        Next

    End Sub

    Private Sub uiComboSize_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles uiComboSize.SelectionChanged
        If _thumbsy Is Nothing Then Return
        If _thumbsy.Count < 1 Then Return

        SkalujRozmiarMiniaturek()
        ' uaktualnij 
        PokazThumbsy()

    End Sub
    Private Sub Window_SizeChanged(sender As Object, e As SizeChangedEventArgs)
        uiComboSize_SelectionChanged(Nothing, Nothing)
    End Sub

    Private Sub Window_Closing(sender As Object, e As ComponentModel.CancelEventArgs)
        uiPicList.ItemsSource = Nothing

        For Each oPicek As ThumbPicek In _thumbsy
            oPicek.oImageSrc = Nothing
        Next

        GC.Collect()    ' usuwamy, bo dużo pamięci zwolniliśmy
    End Sub


    Public Class ThumbPicek
        Public Property oPic As Vblib.OnePic
        Public Property sDymek As String 'XAML dymek
        Public Property oImageSrc As BitmapImage = Nothing ' XAML image
        Public Property iDuzoscH As Integer ' XAML height

        Sub New(picek As Vblib.OnePic, iMaxBok As Integer)
            oPic = picek
            sDymek = oPic.sSuggestedFilename
            iDuzoscH = iMaxBok
        End Sub

    End Class

End Class

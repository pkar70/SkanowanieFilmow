

'Imports System.IO
'Imports System.Runtime.InteropServices.WindowsRuntime
'Imports System.Runtime.Serialization
'Imports System.Runtime.Serialization.Formatters.Binary
'Imports System.Security.Policy
'Imports System.Windows.Controls.Primitives
'Imports System.Windows.Input.Manipulations
'Imports FFMpegCore.Enums
'Imports MetadataExtractor.Formats
'Imports Org.BouncyCastle.Crmf
'Imports Org.BouncyCastle.Crypto.Engines
Imports pkar
'Imports Vblib
'Imports Windows.Storage.Streams
'Imports Windows.UI.Core
Imports vb14 = Vblib.pkarlibmodule14
Imports pkar.UI.Extensions
Imports Microsoft.EntityFrameworkCore.Diagnostics
Imports Windows.Media.Devices
Imports Vblib

Public Class SearchWindow

    Private Shared _fullArchive As BaseList(Of Vblib.OnePic) ' pełny plik archiwum, do wyszukiwania
    Private _inputList As IEnumerable(Of Vblib.OnePic) ' aktualnie używany na wejściu
    Private _queryResults As IEnumerable(Of Vblib.OnePic) ' wynik szukania
    'Private _geoTag As BasicGeopos
    Private _initialCount As Integer

    Private _query As Vblib.SearchQuery

    ''' <summary>
    ''' Okno wyszukiwania (do zadawania pytań)
    ''' </summary>
    ''' <param name="lista">NULL albo już wstępnie ograniczona lista (np. przy szukaniu w nowym oknie w rezultatach)</param>
    ''' <param name="query">NULL, albo już zdefiniowane query</param>
    Public Sub New(Optional lista As List(Of Vblib.OnePic) = Nothing, Optional query As Vblib.SearchQuery = Nothing)
        ' This call is required by the designer.
        InitializeComponent()

        _inputList = Nothing
        _query = If(query, New Vblib.SearchQuery)

    End Sub

    Private Async Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        'WypelnComboSourceNames()
        Me.InitDialogs
        Me.ProgRingInit(True, False)

        If _inputList Is Nothing Then
            Await ReadWholeArchive()

            'Dim dtstart = Date.Now
            'For Each oPic As Vblib.OnePic In Application.gDbase.GetFirstLoaded.GetAll
            '    oPic.sumOfDescr = oPic.GetSumOfDescriptionsText
            'Next
            'Dim dtstop = Date.Now
            'Await Me.MsgBoxAsync("Przeliczenie descriptions: " & (dtstop - dtstart).Milliseconds) = 5

            'dtstart = Date.Now
            'For Each oPic As Vblib.OnePic In Application.gDbase.GetFirstLoaded.GetAll
            '    oPic.sumOfDescr = oPic.GetAllKeywords
            'Next
            'dtstop = Date.Now
            'Await Me.MsgBoxAsync("Przeliczenie kwds: " & (dtstop - dtstart).Milliseconds) = 276



            _inputList = Nothing
        End If

        uiResultsCount.Text = $"(no query, total {_initialCount} items)"

        uiKwerenda.DataContext = _query
        'AddHandler uiKwerenda.Szukajmy, AddressOf uiSearch_Click

    End Sub


    Private Async Function ReadWholeArchive() As Task

        _initialCount = Application.gDbase.Count
        If Application.gDbase.IsLoaded Then Return

        Me.ProgRingSetText("loading database...")
        Me.ProgRingShow(True)

        Await Task.Run(Sub() Application.gDbase.Load())
        'Application.gDbase.Load()
        _initialCount = Application.gDbase.Count

        Me.ProgRingShow(False)
        ' potem: new ProcessBrowse.New(bufor As Vblib.IBufor, onlyBrowse As Boolean)

        If _initialCount < 1 Then
            'vb14.DialogBox("Coś dziwnego, bo jakoby pusty indeks był?")
            ProbaWczytaniaJSON()  ' to pokaże komunikat błędu z wczytywania JSONa
        End If

    End Function

    Private Sub ProbaWczytaniaJSON()
        Dim lista As List(Of Vblib.OnePic)
        Dim sTxt = IO.File.ReadAllText(Application.GetDataFile("", "archIndexFull.json"))
        sTxt &= "]"
        Dim sErr As String = ""
        Try
            lista = Newtonsoft.Json.JsonConvert.DeserializeObject(sTxt, GetType(ObservableList(Of Vblib.OnePic)))
        Catch ex As Exception
            sErr = ex.Message
        End Try

        If sErr <> "" Then Me.MsgBox("Coś dziwnego, bo jakoby pusty indeks był?" & vbCrLf & sErr)
        Debug.WriteLine(sErr)
    End Sub


    ' Query 1:
    ' „pokaż zdjęcia, na których jestem ja i babcia Z, nie ma babci S;
    ' zdjęcie jest zrobione na zewnątrz,
    ' jest jakaś woda (typu jezioro, rzeka),
    ' widać samochód, ale nie ma roweru,
    ' i jest dużo żółtego (kwiaty);
    ' wykonane między 2010 a 2015 rokiem,
    ' latem,
    ' i w Suchej Beskidzkiej,
    ' gdy temperatura nie przekraczała 18 °C,
    ' między godziną 10:15 a 12:30,
    ' wiał dość silny wiatr północny,
    ' blisko pełni Księżyca”.
    '
    ' Query 2:
    ' „znajdź zdjęcia z parady samochodów zabytkowych, na których to paradach byłem z A, ale bez B”


    Private Async Function Szukaj(lista As IEnumerable(Of Vblib.OnePic), query As Vblib.SearchQuery) As Task(Of Integer)

        _queryResults = New List(Of Vblib.OnePic)
        Dim iCount As Integer = 0

        Me.ProgRingShow(True)
        Me.ProgRingSetText("searching...")
        Await Task.Delay(100)

        'For Each oPicek As Vblib.OnePic In lista
        '    If Not CheckIfOnePicMatches(oPicek, query) Then Continue For

        '    _queryResults.Add(oPicek)
        '    iCount += 1
        'Next

        ' konwersja subtagów - ale to jest skomplikowane, i niekoniecznie tak zadziała, odkładam na później do przemyślenia
        'query.ogolne.AllSubTags = new ....Clear()
        'If Not String.IsNullOrWhiteSpace(query.ogolne.Tags) Then
        '    For Each sKwd As String In query.ogolne.Tags.Split(" ")
        '        If Not sKwd.StartsWithCS("!") Then
        '            query.ogolne.AllSubTags.Add(Application.GetKeywords.GetAllChilds(sKwd))
        '        End If
        '    Next
        'End If

        Dim startTime As Date = Date.Now

        If lista Is Nothing Then
            ' po pełnym
            Await Task.Run(Sub() _queryResults = Application.gDbase.Search(query))
        Else
            ' po już ograniczonym
            Await Task.Run(Sub() _queryResults = lista.Where(Function(x) x.CheckIfMatchesQuery(query)))
        End If
        Me.ProgRingShow(False)

        Dim stopTime As Date = Date.Now

        ' w 21 tysiącach, -JA to niecałe 3 ms, 'gdziekolwiek' podobnie, ale czas jest bardzo długi PO tym msgbox do wyświetlenia!
        'Await Me.MsgBoxAsync("Szukanie zajęło ms: " & (stopTime - startTime).TotalMilliseconds)

        Return _queryResults.ToList.Count
        'Return iCount
    End Function

    Private Async Sub uiSearch_Click(sender As Object, e As RoutedEventArgs)

        ' clickcli
        _query = Await uiKwerenda.QueryValidityCheck
        If _query Is Nothing Then Return



        ' przeniesienie z UI do _query - większość się zrobi samo, ale daty - nie
        Dim iCount As Integer
        'If _inputList Is Nothing Then
        '    iCount = Szukaj(_fullArchive.GetList, _query)
        'Else
        Dim dtStart As Date = Date.Now
        iCount = Await Szukaj(_inputList, _query)
        'End If
        Dim dtEnd As Date = Date.Now

        Dim msec As String = $"[{(dtEnd - dtStart).TotalSeconds.ToString("F1")} sec]"

        If iCount < 1 Then
            uiLista.ItemsSource = Nothing
            uiListaKatalogow.ItemsSource = Nothing
            uiResultsCount.Text = $"Nic nie znalazłem (w {_initialCount}) {msec}."
            Return
        End If

        uiResultsCount.Text = $"Found {iCount} items (from {_initialCount}) {msec}."

        Dim startTime As Date = Date.Now

        ' najpierw lista folderów - znacznie krótsza niż plików (zakładam) i zawsze do pokazania
        Dim listaNazwFolderow As New List(Of String)

        For Each oPicek As Vblib.OnePic In _queryResults
            listaNazwFolderow.Add(oPicek.TargetDir)
        Next

        Dim stopTime As Date = Date.Now
        'Await Me.MsgBoxAsync("Wyciagniecie lista katalogów took ms: " & (stopTime - startTime).TotalMilliseconds)

        If listaNazwFolderow.Count > 0 Then

            Dim listaFolderow As New List(Of Vblib.OneDir)
            For Each nazwa As String In From c In listaNazwFolderow Order By c Distinct
                Dim oFolder As Vblib.OneDir = Application.GetDirTree.GetDirFromTargetDir(nazwa)
                If oFolder Is Nothing Then
                    oFolder = New Vblib.OneDir
                    oFolder.fullPath = nazwa
                    'Public Property sId As String
                    ''Public Property sDisplayName As String
                    'Public Property notes As String: nie ma
                    'Public Property denyPublish As Boolean
                    'Public Property SubItems As List(Of OneDir)
                    'Public Property sParentId As String
                End If
                listaFolderow.Add(oFolder)
            Next

            'stopTime = Date.Now
            'Await Me.MsgBoxAsync("GetDirFromTargetDir took ms: " & (stopTime - startTime).TotalMilliseconds)
            'startTime = Date.Now

            listaFolderow.Sort(
                Function(x As Vblib.OneDir, y As Vblib.OneDir)
                    Return x.fullPath.CompareTo(y.fullPath)
                End Function)
            'stopTime = Date.Now
            'Await Me.MsgBoxAsync("sort took ms: " & (stopTime - startTime).TotalMilliseconds)

            startTime = Date.Now
            uiListaKatalogow.ItemsSource = listaFolderow
            stopTime = Date.Now
            'Await Me.MsgBoxAsync("wstawienie do UI dirs took ms: " & (stopTime - startTime).TotalMilliseconds)
        Else
            ' kasujemy ewentualny poprzedni
            uiListaKatalogow.ItemsSource = Nothing
        End If


        'stopTime = Date.Now

        ' w 21 tysiącach, szukanie -JA to niecałe 3 ms, 'gdziekolwiek' podobnie,
        ' zaś konwersja katalogów: 4.5 sekundy! - a, ale dopiero wtedy pewnie szuka :)
        ' Await Me.MsgBoxAsync("Konwersja katalogów took ms: " & (stopTime - startTime).TotalMilliseconds)

        If iCount > 1000 Then
            If Not Await Me.DialogBoxYNAsync($"{iCount} to dużo elementów, pokazać listę mimo to?") Then Return
        End If

        startTime = Date.Now

        ' pokazanie rezultatów
        uiLista.ItemsSource = _queryResults 'From c In _queryResults

        stopTime = Date.Now
        'Await Me.MsgBoxAsync("wstawienie do UI files took ms: " & (stopTime - startTime).TotalMilliseconds)


    End Sub


    ''' <summary>
    ''' przeniesienie danych z UI do struktury Query - to, co samo się nie przenosi
    ''' </summary>


    Private Sub uiSubSearch_Click(sender As Object, e As RoutedEventArgs)
        Me.msgBox("jeszcze nie umiem")
    End Sub

    Private Sub uiGoMiniaturki_Click(sender As Object, e As RoutedEventArgs)

        If Not _queryResults.Any Then Return

        Dim lista As New Vblib.BufferFromQuery()

        ' = _queryResults

        For Each oPic As Vblib.OnePic In uiLista.SelectedItems

            For Each oArch As lib_PicSource.LocalStorageMiddle In Application.GetArchivesList
                'vb14.DumpMessage($"trying archive {oArch.StorageName}")
                Dim sRealPath As String = oArch.GetRealPath(oPic.TargetDir, oPic.sSuggestedFilename)
                vb14.DumpMessage($"real path of index file: {sRealPath}")
                If Not String.IsNullOrWhiteSpace(sRealPath) Then
                    Dim oPicNew As Vblib.OnePic = oPic.Clone
                    oPic.InBufferPathName = sRealPath
                    lista.AddFile(oPic)
                    Exit For
                End If
            Next
        Next

        Dim oWnd As New ProcessBrowse(lista, True, "Found")
        oWnd.Show()
        Return


    End Sub

    Private Sub uiOpenBig_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oPic As Vblib.OnePic = oFE.DataContext
        If oPic Is Nothing Then Return

        If String.IsNullOrWhiteSpace(oPic.TargetDir) Then Return

        For Each oArch As lib_PicSource.LocalStorageMiddle In Application.GetArchivesList
            'vb14.DumpMessage($"trying archive {oArch.StorageName}")
            Dim sRealPath As String = oArch.GetRealPath(oPic.TargetDir, oPic.sSuggestedFilename)
            vb14.DumpMessage($"real path of index file: {sRealPath}")
            If Not String.IsNullOrWhiteSpace(sRealPath) Then
                Dim oPicNew As Vblib.OnePic = oPic.Clone
                oPic.InBufferPathName = sRealPath
                Dim oWnd As New ShowBig(oPic, True, False)
                oWnd.Show()
                Return
            End If
        Next
        vb14.DialogBox("nie mogę znaleźć pliku w żadnym archiwum")

    End Sub

    Private Sub uiOpenExif_Click(sender As Object, e As RoutedEventArgs)
        Dim oItem As FrameworkElement = sender
        Dim oPicek As Vblib.OnePic = oItem.DataContext

        Dim oWnd As New ShowExifs(False) '(oPicek.oDir)

        ' możemy potem w nim robić zmiany...
        oWnd.Owner = Me
        oWnd.DataContext = oPicek
        oWnd.Show()

    End Sub

    Private Sub RefreshOwnedWindows(oPic As Vblib.OnePic)
        For Each oWnd As Window In Me.OwnedWindows
            oWnd.DataContext = oPic
        Next
    End Sub

    Private Sub uiLista_SelChanged(sender As Object, e As SelectionChangedEventArgs)
        Dim ile As Integer = uiLista.SelectedItems.Count

        If ile = 1 Then
            Dim oItem As Vblib.OnePic = uiLista.SelectedItems(0)
            RefreshOwnedWindows(oItem)
        End If

    End Sub


    Private Sub uiOpenFolder_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oPic As Vblib.OnePic = oFE.DataContext
        If oPic Is Nothing Then Return

        If String.IsNullOrWhiteSpace(oPic.TargetDir) Then Return
        SettingsDirTree.OpenFolderInPicBrowser(oPic.TargetDir)
    End Sub

    Private Sub uiFoldersOpenFolder_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oDir As Vblib.OneDir = oFE.DataContext
        If oDir Is Nothing Then Return

        ' otwórz folder - ale z listy folderów
        SettingsDirTree.OpenFolderInPicBrowser(oDir.fullPath)
    End Sub
End Class


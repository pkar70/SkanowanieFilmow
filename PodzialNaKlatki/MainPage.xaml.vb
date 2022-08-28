
'program do zamiany paska na klatki
'1) wyrownywanie?

'2) zaznaczenie punktow charakterystycznych
'a) prawo-górny róg klatki
'b) lewo-dół rog klatki
'c) jesli bez wyrownania, to prawo-dolny róg - do wyznaczenia wielkosci klatki z ukosem (inaczej wystarczyłyby RT/LB)
'd) prawo-górny róg klatki 2 - do powtarzania

'moze prawy klik i wybor z menu ktory To róg?
' L/ R T/B klatka2
'obrazek, dodanie pasków/kresek wzdłuż górnej krawedzi, dolnej, kresek pionowych miedzy klatkami

'dolne menu
'a) wybór góry obrazka (klatki) - pamietany
'b) mirror pionowy, poziomy (on/off) - pamietany

'SaveAs - do wskazanego katalogu, ustawianie filename prefix, cyfr numeru, numer poczatkowy - pamietany, tak ze kolejne uruchomienie po prostu kontynuuje

'ewentualnie zrobienie z tego od razu filmiku?

' 1) "zaznacz LEWĄ krawędź na górze i na dole paska" »» zrobi sie obrót, i będzie pionowo
' 2) "zaznacz lewy górny róg najwyższej klatki"  
' 3) "zaznacz prawy dolny róg najwyższej klatki" 
' 4) "zaznacz lewy górny róg drugiej klatki"  

' ALE OBRÓT jest tylko co 90°, a nie dowolny niestety. Więc obracanie proszę w DigitalImaging :)

' dzielenie - nie do plików, tylko do czegoś :) tak by pokazywac na ekranie
' mozna nawet zrobic mini-filmik
' pokazac parametry, ktore mozna zmienic i stworzyc nowe obrazki (np. jak sie nie trafi dobrze w punkt przy wskazywaniu)
' parametry moga byc pamietane pomiedzy sesjami (bo skoro bedzie seria skanow tego samego filmu, to wielkosc klatki itp. jest bez zmian, tylko jeden punkt wystarczy wskazywac - początek)
' skoro uklad jest zawsze pionowy, to w poziomie mozna podzielic na dwie czesci, i inaczej obsluzyc ustawianie punktow/zbieranie danych

Public NotInheritable Class MainPage
    Inherits Page

    Private moFile As Windows.Storage.StorageFile = Nothing
    Private moBitmap As BitmapImage = Nothing
    Private moDataStream As MemoryStream = Nothing
    'Private moBitmap As Windows.Graphics.Imaging.SoftwareBitmap = Nothing 'BitmapImage = Nothing
    Private mlKlatki As ObservableCollection(Of JednaKlatka) = New ObservableCollection(Of JednaKlatka)
    Private miStep As Integer = 0

#Region "kolejne kroki"
    Private Sub ShowStepMsg()
        Select Case miStep
            Case 0
                uiMsgText.Text = "zaznacz LEWĄ krawędź na GÓRZE paska"
            Case 1
                uiMsgText.Text = "zaznacz LEWĄ krawędź na DOLE paska"
            Case 2
                uiMsgText.Text = "zaznacz lewy górny róg najwyższej klatki"
            Case 3
                uiMsgText.Text = "zaznacz prawy dolny róg najwyższej klatki"
            Case 4
                uiMsgText.Text = "zaznacz lewy górny róg drugiej klatki"
            Case 5
                uiMsgText.Text = "chyba dzielimy"
            Case Else
                uiMsgText.Text = "a nie wiem co teraz?"
        End Select
    End Sub

    Private Sub NextStep(Optional bReset As Boolean = False)
        If bReset Then
            miStep = 2
        Else
            miStep = miStep + 1
            If miStep > 5 Then miStep = 0
        End If
        ShowStepMsg()
    End Sub

#End Region
#Region "wczytywanie wieloklatkowego obrazka"

    Private Async Sub uiOpen_Click(sender As Object, e As RoutedEventArgs)
        uiOpen.Visibility = Visibility.Collapsed
        uiMainPicScroll.Visibility = Visibility.Visible

        moFile = Await RunFilePicker()
        If moFile Is Nothing Then Return
        Await WczytajPicek(moFile)

        Await SetFullPicImageSource()

        NextStep(True)
    End Sub

    Private Async Function RunFilePicker() As Task(Of Windows.Storage.StorageFile)
        Dim picker = New Windows.Storage.Pickers.FileOpenPicker()
        picker.FileTypeFilter.Add(".jpg")
        picker.FileTypeFilter.Add(".tif")

        Return Await picker.PickSingleFileAsync()
    End Function

    Private Async Function WczytajPicek(oFile As Windows.Storage.StorageFile) As Task
        Dim oStream As Stream = Await oFile.OpenStreamForReadAsync

        ' zawisa na tym setsource
        If oStream Is Nothing Then
            DialogBox("FAIL WczytajPicek, oStream is NULL")
            Return
        End If

        ' wczytaj od razu do bitmapy
        'Await moBitmap.SetSourceAsync(oStream.AsRandomAccessStream)
        'oStream.Dispose()

        ' skopiuj do moDataStream
        moDataStream = New MemoryStream(oStream.Length)
        oStream.CopyTo(moDataStream)

        ' wersja do SoftwareBitmap
        ''https://docs.microsoft.com/en-us/windows/uwp/audio-video-camera/imaging
        'Using oStream As Windows.Storage.Streams.IRandomAccessStream = Await oFile.OpenAsync(Windows.Storage.FileAccessMode.Read)

        '    '// Create the decoder from the stream
        '    Dim oDecoder As Windows.Graphics.Imaging.BitmapDecoder =
        '    Await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(oStream)

        '    ' // Get the SoftwareBitmap representation of the file
        '    moBitmap = Await oDecoder.GetSoftwareBitmapAsync()
        'End Using

    End Function

    Private Async Function SetFullPicImageSource() As Task
        If moDataStream Is Nothing Then
            DialogBox("FAIL SetFullPicImageSource, moDataStream is NULL")
            Return
        End If

        moDataStream.Position = 0
        moBitmap = New BitmapImage
        moBitmap.SetSource(moDataStream.AsRandomAccessStream)

        uiFullPicture.Source = moBitmap
        uiFullPicture.Stretch = Stretch.Uniform

        'If moBitmap.BitmapPixelFormat <> Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8 Or
        'moBitmap.BitmapAlphaMode = Windows.Graphics.Imaging.BitmapAlphaMode.Straight Then

        '    moBitmap = Windows.Graphics.Imaging.SoftwareBitmap.Convert(
        '                moBitmap,
        '                Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,
        '                Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied)

        'End If

        'Dim oSoftBitmapSrc = New SoftwareBitmapSource()
        'Await oSoftBitmapSrc.SetBitmapAsync(moBitmap)

        ''// Set the source of the Image control
        'uiFullPicture.Source = oSoftBitmapSrc
    End Function
#End Region

#Region "góra klatek"

    Private Sub PokazGore()

        Dim oBrushEmpty = New SolidColorBrush(Windows.UI.Colors.LightBlue)
        Dim oBrushTop = New SolidColorBrush(Windows.UI.Colors.Red) ' zwykły Blue jest za ciemny, gubi sie na czarnym

        uiTopOnTop.Fill = oBrushEmpty
        uiTopOnBottom.Fill = oBrushEmpty
        uiTopOnLeft.Fill = oBrushEmpty
        uiTopOnRight.Fill = oBrushEmpty

        Select Case GetSettingsString("TopOn", "top").ToLower
            Case "top"
                uiTopOnTop.Fill = oBrushTop
            Case "bottom"
                uiTopOnBottom.Fill = oBrushTop
            Case "left"
                uiTopOnLeft.Fill = oBrushTop
            Case "right"
                uiTopOnRight.Fill = oBrushTop
        End Select

    End Sub

    Private Sub uiSetTop_Tapped(sender As Object, e As TappedRoutedEventArgs)
        Dim oEll As FrameworkElement = TryCast(sender, FrameworkElement)

        Dim sName As String = oEll.Name.Replace("uiTopOn", "")
        SetSettingsString("TopOn", sName.ToLower)
        PokazGore()

    End Sub

#End Region

#Region "wskazywanie rogów"
    Private moLastPoint As Point = Nothing  ' ostatni RightClick na obrazku
    Private moPointLT As Point = Nothing
    Private moPointRB As Point = Nothing
    Private moPointLT2 As Point = Nothing

#If False Then

    Private Sub uiSetPointLT_Click(sender As Object, e As RoutedEventArgs)
        DebugOut("uiSetPointLT_Click")
        moPointLT = moLastPoint
    End Sub

    Private Sub uiSetPointLT2_Click(sender As Object, e As RoutedEventArgs)
        DebugOut("uiSetPointLT2_Click")
        moPointLT2 = moLastPoint
    End Sub

    Private Sub uiSetPointLB_Click(sender As Object, e As RoutedEventArgs)
        DebugOut("uiSetPointLB_Click")
        moPointLB = moLastPoint
    End Sub

    Private Sub uiSetPointRT_Click(sender As Object, e As RoutedEventArgs)
        DebugOut("uiSetPointRT_Click")
        moPointRT = moLastPoint
    End Sub
#End If

    Private Async Sub uiSetStep_Click(sender As Object, e As RoutedEventArgs)
        ' z menu, którym jest OK - zatwierdzenie z RightTapped (jakby co, mozna poprawic!)
        Select Case miStep
            'Case 0
            '    ' uiMsgText.Text = "zaznacz LEWĄ krawędź na GÓRZE paska"
            '    moPointLT = moLastPoint
            'Case 1
            '    ' uiMsgText.Text = "zaznacz LEWĄ krawędź na DOLE paska"
            '    moPointLT2 = moLastPoint
            '    Await ObracanieGlownegoObrazka()
            Case 2
                ' uiMsgText.Text = "zaznacz lewy górny róg najwyższej klatki"
                moPointLT = moLastPoint
                uiTLleft.Value = CInt(moPointLT.X * GetScale())
                uiTLtop.Value = CInt(moPointLT.Y * GetScale())
            Case 3
                'uiMsgText.Text = "zaznacz prawy dolny róg najwyższej klatki"
                moPointRB = moLastPoint
                uiKwidth.Value = CInt(Math.Abs(moPointRB.X - moPointLT.X) * GetScale())
                uiKheight.Value = CInt(Math.Abs(moPointRB.Y - moPointLT.Y) * GetScale())
            Case 4
                'uiMsgText.Text = "zaznacz lewy górny róg drugiej klatki"
                moPointLT2 = moLastPoint
                uiKstep.Value = CInt(Math.Abs(moPointLT2.Y - moPointLT.Y) * GetScale())
                Await PodzielNaKlatki()
            Case Else
                uiMsgText.Text = "a nie wiem co teraz?"
        End Select


        NextStep()
    End Sub


    Private Sub uiMainPic_RightTapped(sender As Object, e As RightTappedRoutedEventArgs)
        moLastPoint = e.GetPosition(uiFullPicture)
        DebugOut("uiMainPic_RightTapped, x=" & moLastPoint.X & ", y=" & moLastPoint.Y)
    End Sub
#End Region


    Private Async Function ObracanieGlownegoObrazka() As Task
        ' ma do dyspozycji:
        '' uiMsgText.Text = "zaznacz LEWĄ krawędź na GÓRZE paska"
        'moPointLT = moLastPoint
        '' uiMsgText.Text = "zaznacz LEWĄ krawędź na DOLE paska"
        'moPointLT2 = moLastPoint
        DialogBox("teraz obracam, ale tylko na niby")

        Dim decoder As Windows.Graphics.Imaging.BitmapDecoder =
                Await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(moDataStream)

        'Dim destinationStream As Windows.Storage.Streams.InMemoryRandomAccessStream = New Windows.Storage.Streams.InMemoryRandomAccessStream()

        'Dim transform As Windows.Graphics.Imaging.BitmapTransform =
        '        New Windows.Graphics.Imaging.BitmapTransform With {.ScaledWidth = newWidth, .ScaledHeight = newHeight}

        Dim oStreamOut As New MemoryStream ' Windows.Storage.Streams.InMemoryRandomAccessStream

        Dim encoder As Windows.Graphics.Imaging.BitmapEncoder =
                Await Windows.Graphics.Imaging.BitmapEncoder.CreateForTranscodingAsync(oStreamOut, decoder)
        'BitmapTransform w tej kolejnosci robi: scale, flip, rotation, crop
        'encoder.BitmapTransform.ScaledHeight = newHeight
        'encoder.BitmapTransform.ScaledWidth = newWidth
        ' encoder.BitmapTransform.Rotation - tylko co 90 stopni! nie o dowolny kąt!

        Await encoder.FlushAsync()
        Await oStreamOut.FlushAsync

        ' podmiana strumieni
        moDataStream = oStreamOut

        ' i pokazanie bieżącego
        Await SetFullPicImageSource()

    End Function


    'Private Sub uiMainPic_Tapped(sender As Object, e As RoutedEventArgs)
    '    Dim oResize As Stretch = uiFullPicture.Stretch
    '    Select Case oResize
    '        Case Stretch.Uniform
    '            uiFullPicture.Stretch = Stretch.None
    '        Case Stretch.None
    '            uiFullPicture.Stretch = Stretch.Uniform
    '    End Select

    'End Sub

    Private Sub WczytajPoprzednieWielkosci()

        uiTLleft.Value = GetSettingsInt("uiTLleft")
        uiTLtop.Value = GetSettingsInt("uiTLtop")

        uiKwidth.Value = GetSettingsInt("uiKwidth", 10)
        uiKheight.Value = GetSettingsInt("uiKheight", 10)

        uiKstep.Value = GetSettingsInt("uiuiKstep", 10)

        uiFilePrefix.Text = GetSettingsString("filePrefix", "fr")
        uiCurrFrame.Value = GetSettingsInt("uiCurrFrame", 1)

    End Sub

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        PokazGore()
        WczytajPoprzednieWielkosci()
    End Sub

    Private Async Function MyCamerasTimerZmniejszanie(iMaxSize As Integer, oFileInput As Windows.Storage.StorageFile) As Task(Of Boolean)

        ' MyCameras, Timer
        ' H:\Home\PIOTR\VStudio\_Vs2017\MyCameras\MyCameras>notepad CameraTimer.xaml.vb

        Dim decoder As Windows.Graphics.Imaging.BitmapDecoder =
                Await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync((Await oFileInput.OpenStreamForReadAsync).AsRandomAccessStream)
        Dim origWidth As Integer = decoder.OrientedPixelWidth
        Dim origHeight As Integer = decoder.OrientedPixelHeight

        Dim ratioX As Double = iMaxSize / CDbl(origWidth)
        Dim ratioY As Double = iMaxSize / CDbl(origHeight)
        Dim ratio As Double = Math.Min(ratioX, ratioY)
        Dim newHeight As Integer = origHeight * ratio
        Dim newWidth As Integer = origWidth * ratio

        Dim destinationStream As Windows.Storage.Streams.InMemoryRandomAccessStream = New Windows.Storage.Streams.InMemoryRandomAccessStream()

        Dim transform As Windows.Graphics.Imaging.BitmapTransform =
                New Windows.Graphics.Imaging.BitmapTransform With {.ScaledWidth = newWidth, .ScaledHeight = newHeight}

        Dim oFold As Windows.Storage.StorageFolder = Windows.Storage.ApplicationData.Current.RoamingFolder
        If oFold Is Nothing Then
            DialogBox("FAIL: cannot open Roaming folder?")
            Return False
        End If
        oFold = Await oFold.CreateFolderAsync("timerPics", Windows.Storage.CreationCollisionOption.OpenIfExists)
        If oFold Is Nothing Then
            DialogBox("FAIL: cannot open/create timerPics folder?")
            Return False
        End If
        'If oFold.TryGetItemAsync(oFileInput.Name) IsNot Nothing Then
        '    Await pkar.DialogBox("File with this name already exist, try another")
        '    Return False
        'End If

        Dim oFileOut As Windows.Storage.StorageFile = Await oFold.CreateFileAsync(oFileInput.Name, Windows.Storage.CreationCollisionOption.FailIfExists)
        Dim fileStream As Windows.Storage.Streams.IRandomAccessStream =
                Await oFileOut.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite)
        Dim encoder As Windows.Graphics.Imaging.BitmapEncoder =
                Await Windows.Graphics.Imaging.BitmapEncoder.CreateForTranscodingAsync(fileStream, decoder)
        'BitmapTransform w tej kolejnosci robi: scale, flip, rotation, crop
        encoder.BitmapTransform.ScaledHeight = newHeight
        encoder.BitmapTransform.ScaledWidth = newWidth

        Await encoder.FlushAsync()
        Await fileStream.FlushAsync
        fileStream.Dispose()
    End Function

    Private Function GetScale() As Double
        ' moBitmap.PixelHeight = 14040, czyli faktycznie rozmiar bitmapy
        ' uiFullPicture.ActualHeight ≈ 5300, i wedle tego są podawane moPointLT2 !

        Return moBitmap.PixelHeight / uiFullPicture.ActualHeight
    End Function

#Region "dzielenie na klatki"


    Private Async Function PodzielNaKlatki() As Task
        ' wejście:
        '   moBitmap (Bitmap)
        ' uiMsgText.Text = "zaznacz lewy górny róg najwyższej klatki"
        'moPointLT = moLastPoint
        ''uiMsgText.Text = "zaznacz prawy dolny róg najwyższej klatki"
        'moPointRB = moLastPoint
        ''uiMsgText.Text = "zaznacz lewy górny róg drugiej klatki"
        'moPointLT2 = moLastPoint
        ' UWAGA NA SKALOWANIE!

        ' wyjscie: 
        '   mlKlatki
        mlKlatki.Clear()

        Dim decoder As Windows.Graphics.Imaging.BitmapDecoder =
                Await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(moDataStream.AsRandomAccessStream)

        Dim dScale As Double = GetScale()

        Dim iCurrY As Integer = uiTLtop.Value
        Dim iCurrX As Integer = uiTLleft.Value
        Dim iKlatkaWys As Integer = uiKheight.Value
        Dim iKlatkaSzer As Integer = uiKwidth.Value
        Dim iYStep As Integer = uiKstep.Value

        Dim iLicznik As Integer = uiCurrFrame.Value

        While iCurrY + iKlatkaWys < moBitmap.PixelHeight

            Dim oKlatka As New JednaKlatka
            oKlatka.sData = iLicznik.ToString("000")

            oKlatka.oOutStream = New Windows.Storage.Streams.InMemoryRandomAccessStream
            Dim encoder As Windows.Graphics.Imaging.BitmapEncoder =
                Await Windows.Graphics.Imaging.BitmapEncoder.CreateForTranscodingAsync(oKlatka.oOutStream, decoder)
            'BitmapTransform w tej kolejnosci robi: scale, flip, rotation, crop

            Dim oBounds As New Windows.Graphics.Imaging.BitmapBounds
            oBounds.Height = iKlatkaWys
            oBounds.Width = iKlatkaSzer
            oBounds.X = iCurrX
            oBounds.Y = iCurrY

            encoder.BitmapTransform.Bounds = oBounds

            Await encoder.FlushAsync()
            Await oKlatka.oOutStream.FlushAsync

            ' teraz ze stream do bitmap
            oKlatka.oOutStream.Seek(0)
            oKlatka.oImageSrc = New BitmapImage
            oKlatka.oImageSrc.SetSource(oKlatka.oOutStream)

            mlKlatki.Add(oKlatka)

            iLicznik += 1
            iCurrY += iYStep
        End While

        DialogBox("Stworzylem " & iLicznik - uiCurrFrame.Value & " ramek")
        'uiPicList.ItemsSource = Nothing
        uiPicList.ItemsSource = mlKlatki


    End Function

    Private Sub SprobujPoliczycKlatki()
        ' do testowania - czy rzezcywiscie dobrze policzy
        Dim y0k1 As Integer = moPointLT.Y
        Dim y0k2 As Integer = moPointLT2.Y

        Dim yMax As Integer = moBitmap.PixelHeight  ' 14040, czyli faktycznie rozmiar bitmapy
        yMax = uiFullPicture.ActualHeight   ' ale to jest lepsze, bo to jest 5k z czymś, czyli wedle wspolrzednych obrazka
        Dim yTemp As Integer = yMax - y0k1  ' rzeczywisty rozmiar skanu (bez marginesu górnego)
        ' ale z tego wychodzi rozmiar klatki ~300 px, a to jest nieprawda
        '--PKAR---:    uiMainPic_RightTapped, x = 682.591796875, y = 666.374694824219
        '--PKAR---:    uiSetPointLT_Click()
        '--PKAR---:    uiMainPic_RightTapped, x = 666.374694824219, y = 990.716369628906
        '--PKAR---:    uiSetPointLT2_Click()
        '--PKAR---:    uiMainPic_RightTapped, x = 492.409637451172, y = 5356.52685546875
        '--PKAR---:    uiSetPointLB_Click() - to poszlo na sam dół (ostatniej klatki)

        Dim ySize As Integer = y0k2 - y0k1  ' rozmiar od klatki do nastepnej klatki (czyli z przerwą międzyklatkową)

        DialogBox("Chyba jest klatek: " & yTemp \ ySize)

    End Sub

    Private Sub ZapiszWartosci()

        SetSettingsInt("uiTLleft", uiTLleft.Value)
        SetSettingsInt("uiTLtop", uiTLtop.Value)

        SetSettingsInt("uiKwidth", uiKwidth.Value)
        SetSettingsInt("uiKheight", uiKheight.Value)

        SetSettingsInt("uiuiKstep", uiKstep.Value)

        SetSettingsString("filePrefix", uiFilePrefix.Text)
        SetSettingsInt("uiCurrFrame", uiCurrFrame.Value)

    End Sub

    Private Async Sub uiSplit_Click(sender As Object, e As RoutedEventArgs)
        Await PodzielNaKlatki()
        ZapiszWartosci()
        uiSave.IsEnabled = True
        'PodzielNaKlatki()
        'uiPicList.ItemsSource = mlKlatki
    End Sub
#End Region

    Private Async Function PokazPickerKatalogu() As Task(Of Windows.Storage.StorageFolder)
        Dim picker = New Windows.Storage.Pickers.FolderPicker
        picker.FileTypeFilter.Add(".jpg")
        picker.FileTypeFilter.Add(".tif")

        Return Await picker.PickSingleFolderAsync

    End Function

    Private Async Sub uiSave_Click(sender As Object, e As RoutedEventArgs)
        If mlKlatki Is Nothing Then Return  ' zablokowane normalnie przez uiSplit
        If mlKlatki.Count < 1 Then
            DialogBox("ERROR: zero klatek? Nie mam nic do zapisania...")
            Return
        End If

        Dim oFold As Windows.Storage.StorageFolder = Await PokazPickerKatalogu()
        If oFold Is Nothing Then Return

        For Each oKlatka As JednaKlatka In mlKlatki
            Dim sFilename As String = uiFilePrefix.Text & oKlatka.sData & ".jpg"
            If Await oFold.FileExistsAsync(sFilename) Then
                DialogBox("Plik już istnieje!" & vbCrLf & sFilename)
                Return
            End If

            Dim oFile As Windows.Storage.StorageFile =
                Await oFold.CreateFileAsync(sFilename, Windows.Storage.CreationCollisionOption.FailIfExists)

            Dim fileStream As Windows.Storage.Streams.IRandomAccessStream =
                Await oFile.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite)
            Dim oRdrStream = oKlatka.oOutStream.AsStreamForRead
            oRdrStream.Seek(0, SeekOrigin.Begin)
            oRdrStream.CopyTo(fileStream.AsStreamForWrite)

            Await fileStream.FlushAsync
            fileStream.Dispose()


        Next

    End Sub
End Class



Public Class JednaKlatka
    Public Property oImageSrc As BitmapImage
    Public Property sData As String
    Public Property oOutStream As Windows.Storage.Streams.InMemoryRandomAccessStream
End Class



Imports Windows.Graphics.Imaging
Imports vb14 = Vblib.pkarlibmodule14

Public Class ShowBig

    Private _picek As ProcessBrowse.ThumbPicek

    Public Sub New(oPicek As ProcessBrowse.ThumbPicek)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        _picek = oPicek
    End Sub

    Private _bitmap As BitmapImage

    ' skopiowane z InstaMonitor
    Private Sub ZmianaRozmiaruImg()

        If _bitmap Is Nothing Then Return

        If uiFullPicture.Stretch = Stretch.None Then
            uiFullPicture.Width = _bitmap.PixelWidth
            uiFullPicture.Height = _bitmap.PixelHeight
        Else

            If _bitmap.PixelWidth < uiMainPicScroll.ViewportWidth And
               _bitmap.PixelHeight < uiMainPicScroll.ViewportHeight Then
                uiFullPicture.Width = _bitmap.PixelWidth
                uiFullPicture.Height = _bitmap.PixelHeight
            Else
                Dim dScaleX As Double = _bitmap.PixelWidth / uiMainPicScroll.ViewportWidth
                Dim dScaleY As Double = _bitmap.PixelHeight / uiMainPicScroll.ViewportHeight

                Dim dDesiredScale As Double = Math.Max(dScaleX, dScaleY)
                uiFullPicture.Width = _bitmap.PixelWidth / dDesiredScale
                uiFullPicture.Height = _bitmap.PixelHeight / dDesiredScale

            End If

        End If
    End Sub

    Private Function DetermineOrientation(oPic As Vblib.OnePic) As Rotation
        Dim oExif As Vblib.ExifTag

        oExif = oPic.GetExifOfType(Vblib.ExifSource.ManualRotate)
        If oExif Is Nothing Then oExif = oPic.GetExifOfType(Vblib.ExifSource.FileExif)

        If oExif Is Nothing Then Return Rotation.Rotate0

        Return OrientationToRotation(oExif.Orientation)

    End Function

    Private Async Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        Me.Title = _picek.sDymek.Replace(vbCrLf, " ")

        Dim iObrot As Rotation = DetermineOrientation(_picek.oPic)

        _bitmap = Await ProcessBrowse.WczytajObrazek(_picek.oPic.InBufferPathName, 0, iObrot)

        ' tylko JPG może być edytowany
        uiEditModes.IsEnabled = _picek.oPic.InBufferPathName.ToLowerInvariant.EndsWith("jpg")

        uiSave.IsEnabled = False
        uiRevert.IsEnabled = False
        If IO.File.Exists(_picek.oPic.InBufferPathName & ".bak") Then uiRevert.IsEnabled = True

        uiFullPicture.Source = _bitmap
        UpdateClipRegion()

        ' skalowanie okna
        Dim scrWidth As Double = SystemParameters.FullPrimaryScreenWidth * 0.9
        Dim scrHeight As Double = SystemParameters.FullPrimaryScreenHeight * 0.9

        Dim dScaleX As Double = _bitmap.PixelWidth / scrWidth
        Dim dScaleY As Double = _bitmap.PixelHeight / scrHeight
        Dim dDesiredScale As Double = Math.Max(dScaleX, dScaleY)

        If scrHeight > _bitmap.PixelHeight AndAlso scrWidth > _bitmap.PixelWidth Then
            Me.Height = _bitmap.PixelHeight + 60
            Me.Width = _bitmap.PixelWidth + 10
        Else
            If iObrot = Rotation.Rotate90 OrElse iObrot = Rotation.Rotate270 Then
                Me.Height = _bitmap.PixelHeight / dDesiredScale ' scrHeight
                Me.Width = _bitmap.PixelWidth / dDesiredScale ' scrWidth
            Else
                Me.Height = scrHeight + 30
                Me.Width = scrWidth + 10
            End If
            uiFullPicture.Width = _bitmap.PixelWidth / dDesiredScale
            uiFullPicture.Height = _bitmap.PixelHeight / dDesiredScale
        End If


        ProcessBrowse.WypelnMenuAutotagerami(uiMenuTaggers, AddressOf ApplyTagger)

        OnOffMap()
        SettingsMapsy.WypelnMenuMapami(uiOnMap, AddressOf uiOnMap_Click)

        ' MenuAutoTaggerow()

    End Sub

    Private Function OrientationToRotation(v As Vblib.OrientationEnum?) As Rotation
        If Not v.HasValue Then Return Rotation.Rotate0

        Select Case v.Value
            Case Vblib.OrientationEnum.topLeft
                Return Rotation.Rotate0
            Case Vblib.OrientationEnum.bottomRight
                Return Rotation.Rotate180
            Case Vblib.OrientationEnum.rightTop
                Return Rotation.Rotate90
            Case Vblib.OrientationEnum.leftBottom
                Return Rotation.Rotate270
                'Case Vblib.OrientationEnum.rightBottom = 7
                'Case Vblib.OrientationEnum.topRight = 2
                'Case Vblib.OrientationEnum.bottomLeft = 4
                'Case Vblib.OrientationEnum.leftTop = 5
        End Select

        Return Rotation.Rotate0
    End Function

    Private Sub OnOffMap()
        Dim oGps As Vblib.MyBasicGeoposition = _picek.oPic.GetGeoTag
        uiOnMap.IsEnabled = (oGps IsNot Nothing)

    End Sub

    'Private Sub MenuAutoTaggerow()

    '    uiMenuTaggers.Items.Clear()

    '    For Each oEngine As Vblib.AutotaggerBase In Application.gAutoTagery
    '        Dim oNew As New MenuItem
    '        oNew.Header = oEngine.Nazwa.Replace("_", "__")
    '        oNew.DataContext = oEngine
    '        AddHandler oNew.Click, AddressOf ApplyTagger
    '        uiMenuTaggers.Items.Add(oNew)
    '    Next

    '    If uiMenuTaggers.Items.Count > 0 Then
    '        uiMenuTaggers.IsEnabled = True
    '    Else
    '        uiMenuTaggers.IsEnabled = False
    '    End If


    'End Sub

    Private Async Sub ApplyTagger(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oSrc As Vblib.AutotaggerBase = oFE?.DataContext
        If oSrc Is Nothing Then Return

        Dim oExif As Vblib.ExifTag = Await oSrc.GetForFile(_picek.oPic)
        If oExif IsNot Nothing Then
            _picek.oPic.Exifs.Add(oExif)
            _picek.oPic.TagsChanged = True
        End If
        Application.GetBuffer.SaveData()  ' bo zmieniono EXIF

        OnOffMap()    ' bo moze juz bedzie mozna to pokazać
    End Sub

    'Private Sub uiFullPicture_MouseRightButtonDown(sender As Object, e As MouseButtonEventArgs) Handles uiFullPicture.MouseRightButtonDown
    '    uiFlyout.IsOpen = True
    'End Sub

    Private Sub Window_SizeChanged(sender As Object, e As SizeChangedEventArgs)
        ZmianaRozmiaruImg()
    End Sub

    Private Sub uiResizePic_Click(sender As Object, e As MouseButtonEventArgs)
        Dim oResize As Stretch = uiFullPicture.Stretch
        Select Case oResize
            Case Stretch.Uniform
                uiFullPicture.Stretch = Stretch.None
            Case Stretch.None
                uiFullPicture.Stretch = Stretch.Uniform
        End Select

        ZmianaRozmiaruImg()
    End Sub

    Private Sub uiCopyPath_Click(sender As Object, e As RoutedEventArgs)
        'uiFlyout.IsOpen = False
        vb14.ClipPut(_picek.oPic.InBufferPathName)
    End Sub

    Private Sub uiShowExifs_Click(sender As Object, e As RoutedEventArgs)
        'uiFlyout.IsOpen = False
        Dim oWnd As New ShowExifs(_picek.oPic)
        oWnd.Show()
    End Sub

    Private Sub uiOnMap_Click(sender As Object, e As RoutedEventArgs)
        Dim oGps As Vblib.MyBasicGeoposition = _picek.oPic.GetGeoTag
        If oGps Is Nothing Then Return

        Dim oFE As FrameworkElement = sender
        Dim oMapa As Vblib.JednaMapa = oFE?.DataContext

        Dim sUri As Uri = oMapa.UriForGeo(oGps)

        'Dim sUri As New Uri("https://www.openstreetmap.org/#map=16/" & oGps.Latitude & "/" & oGps.Longitude)
        sUri.OpenBrowser
    End Sub

    Private Sub uiDescribe_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New AddDescription(_picek.oPic)
        If Not oWnd.ShowDialog Then Return

        Dim oDesc As Vblib.OneDescription = oWnd.GetDescription
        _picek.oPic.AddDescription(oDesc)
    End Sub

    Private Sub ChangePicture(bGoBack As Boolean)
        Dim oBrowserWnd As ProcessBrowse = Me.Owner
        If oBrowserWnd Is Nothing Then Return

        _picek = oBrowserWnd.FromBig_Next(_picek, bGoBack)
        If _picek Is Nothing Then
            Me.Close()  ' koniec obrazków
        Else
            Window_Loaded(Nothing, Nothing)
        End If

    End Sub

    Private Sub Window_KeyUp(sender As Object, e As KeyEventArgs)
        Select Case e.Key
            Case Key.Space, Key.PageDown
                ChangePicture(False)
            Case Key.PageUp
                ChangePicture(True)
            Case Key.Delete
                uiDelPic_Click(Nothing, Nothing)

            'Case Key.Enter
            '    ' full screen / mały screen
            Case Key.Escape
                Me.Close()
            Case Else
                System.Media.SystemSounds.Beep.Play()
        End Select

    End Sub

    Private Async Sub uiDelPic_Click(sender As Object, e As RoutedEventArgs)
        If Not vb14.GetSettingsBool("uiNoDelConfirm") Then
            If Not Await vb14.DialogBoxYNAsync("Skasować zdjęcie?") Then Return
        End If

        Dim oBrowserWnd As ProcessBrowse = Me.Parent
        If oBrowserWnd Is Nothing Then Return

        _picek = oBrowserWnd.FromBig_Delete(_picek)
        If _picek Is Nothing Then
            Me.Close()  ' koniec obrazków
        Else
            Window_Loaded(Nothing, Nothing)
        End If
    End Sub

#Region "picture edits"

    Private Enum EditModeEnum
        none
        crop
        resize
        rotate
    End Enum

    Private _editMode As EditModeEnum = EditModeEnum.none

    Private Async Function SprawdzCzyJestEdycja(editMode As EditModeEnum) As Task(Of Boolean)
        If _editMode = EditModeEnum.none Then Return True

        If _editMode = editMode Then Return True ' jeśli włączamy to samo, to może być

        Dim bSave As Boolean = Await vb14.DialogBoxYNAsync("Jest już edycja, zapisać zmiany?")

        If bSave Then Await SaveChanges()

        _editMode = editMode

        Return True
    End Function

    Private Function RotateToDegree(iRot As Rotation) As Integer
        Select Case iRot
            Case Rotation.Rotate0
                Return 0
            Case Rotation.Rotate90
                Return 90
            Case Rotation.Rotate180
                Return 180
            Case Rotation.Rotate270
                Return 270
        End Select
        ' tego nie powinno być, bo nie ma innej mozliwosci
        Return 0
    End Function

    Private Function DegreeToBitmapRotate(iKat As Integer) As BitmapRotation
        Select Case iKat
            Case 0
                Return BitmapRotation.None
            Case 90
                Return BitmapRotation.Clockwise90Degrees
            Case 180
                Return BitmapRotation.Clockwise180Degrees
            Case 270
                Return BitmapRotation.Clockwise270Degrees
        End Select
        ' tego nie powinno być, bo nie ma innej mozliwosci
        Return BitmapRotation.None
    End Function


    Public Function KatToOrientationEnum(iKat) As Vblib.OrientationEnum
        If iKat > 359 Then iKat -= 360
        Select Case iKat
            Case 0
                Return Vblib.OrientationEnum.topLeft
            Case 90
                Return Vblib.OrientationEnum.rightTop
            Case 180
                Return Vblib.OrientationEnum.bottomRight
            Case 270
                Return Vblib.OrientationEnum.leftBottom
        End Select
        ' tego nie powinno być, bo nie ma innej mozliwosci
        Return Vblib.OrientationEnum.topLeft
    End Function

    Private Async Function SaveChanges() As Task

        Dim transf As New BitmapTransform

        Select Case _editMode
            Case EditModeEnum.none
                Return
            Case EditModeEnum.crop
                Dim oRect As Rect = ConvertSlidersToPicRect()
                Dim bnds As New BitmapBounds
                bnds.X = oRect.X
                bnds.Y = oRect.Y
                bnds.Width = oRect.Width
                bnds.Height = oRect.Height
                transf.Bounds = bnds
            Case EditModeEnum.resize
                ' *TODO* resize
            Case EditModeEnum.rotate

                If Not uiRotateUp.IsChecked Then

                    Dim preRotation As Rotation = DetermineOrientation(_picek.oPic)
                    Dim iKat As Integer = RotateToDegree(preRotation)

                    If uiRotateRight.IsChecked Then iKat += 270
                    If uiRotateDown.IsChecked Then iKat += 180
                    If uiRotateLeft.IsChecked Then iKat += 90

                    Dim oExif As New Vblib.ExifTag(Vblib.ExifSource.ManualRotate)
                    oExif.Orientation = KatToOrientationEnum(iKat)
                    _picek.oPic.ReplaceOrAddExif(oExif)

                End If

                transf = Nothing ' tego nie robimy zapisem

        End Select

        If transf IsNot Nothing Then Await ZapiszZmianyObrazka(transf)

        _editMode = EditModeEnum.none
        ShowHideEditControls(_editMode)
    End Function

#Region "crop"

    Private Sub ShowHideCropSliders(bShow As Boolean)
        Dim visib As Visibility = If(bShow, Visibility.Visible, Visibility.Collapsed)

        uiCropUp.Visibility = visib
        uiCropDown.Visibility = visib
        uiCropLeft.Visibility = visib
        uiCropRight.Visibility = visib

        If Not bShow Then Return

        If uiCropUp.Value = 0 Then uiCropUp.Value = 0.1
        If uiCropDown.Value = 0 Then uiCropDown.Value = 0.9
        If uiCropLeft.Value = 0 Then uiCropLeft.Value = 0.1
        If uiCropRight.Value = 0 Then uiCropRight.Value = 0.9

        UpdateClipRegion()
    End Sub

    Private Sub ShowHideRotateBoxes(bShow As Boolean)
        Dim visib As Visibility = If(bShow, Visibility.Visible, Visibility.Collapsed)

        uiRotateUp.Visibility = visib
        uiRotateDown.Visibility = visib
        uiRotateLeft.Visibility = visib
        uiRotateRight.Visibility = visib

    End Sub

    Public Sub ShowHideSizeSlider(bShow As Boolean)
        Dim visib As Visibility = If(bShow, Visibility.Visible, Visibility.Collapsed)

        uiSizingDown.Visibility = visib

        ' *TODO* inital skalowanie

    End Sub

    Private Sub ShowHideEditControls(iMode As EditModeEnum)
        ShowHideCropSliders(iMode = EditModeEnum.crop)
        ShowHideRotateBoxes(iMode = EditModeEnum.rotate)
        ShowHideSizeSlider(iMode = EditModeEnum.resize)
    End Sub

    Private Function ConvertSlidersToPicRect() As Rect
        Dim oRect As New Rect
        oRect.X = uiFullPicture.Width * (Math.Min(uiCropUp.Value, uiCropDown.Value))
        oRect.Width = uiFullPicture.Width * (Math.Abs(uiCropDown.Value - uiCropUp.Value))

        oRect.Y = uiFullPicture.Height * (Math.Min(uiCropLeft.Value, uiCropRight.Value))
        oRect.Height = uiFullPicture.Height * (Math.Abs(uiCropRight.Value - uiCropLeft.Value))

        Return oRect
    End Function

    Private Sub UpdateClipRegion()

        If Double.IsNaN(uiFullPicture.Width) OrElse Double.IsNaN(uiFullPicture.Height) Then
            uiFullPicture.Clip = Nothing
            Return
        End If

        Dim oRect As Rect = ConvertSlidersToPicRect()
        Dim rectGeom As New RectangleGeometry(oRect)
        uiFullPicture.Clip = rectGeom

    End Sub
    Private Async Sub uiCrop_Click(sender As Object, e As RoutedEventArgs)

        If Not Await SprawdzCzyJestEdycja(EditModeEnum.crop) Then Return

        If uiCropUp.Visibility = Visibility.Visible Then
            ShowHideEditControls(EditModeEnum.none)
            Await SaveChanges()

            uiFullPicture.Clip = Nothing
            Return
        End If

        ShowHideEditControls(EditModeEnum.crop)
        UpdateClipRegion()
    End Sub
    Private Sub uiCropUp_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double))
        UpdateClipRegion()
    End Sub

#End Region
    Private Async Function ZapiszZmianyObrazka(bmpTrans As BitmapTransform) As Task(Of Boolean)
        If Not Await vb14.DialogBoxYNAsync("Zapisać zmiany?") Then Return False

        Dim orgFileName As String = _picek.oPic.InBufferPathName
        Dim bakFileName As String = _picek.oPic.InBufferPathName & ".bak"

        If Not IO.File.Exists(bakFileName) Then
            IO.File.Move(orgFileName, bakFileName)
            IO.File.SetCreationTime(bakFileName, Date.Now)
        End If

        ' *TODO* konwersja bitmapy
        '' save oBmp jako JPG, ewentualne ustawianie jakości wedle settings
        'Dim oFrame As BitmapFrame() = BitmapFrame
        'Dim mSoftBot As SoftwareBitmap()
        'mSoftBot
        'Dim oEnc As New JpegBitmapEncoder
        'oEnc.QualityLevel = vb14.GetSettingsInt("uiJpgQuality", 80)
        'Dim osft As SoftwareBitmap BitmapFrame
        'oEnc.Frames.Add(BitmapFrame.Create(Image))
        'oEnc.Save(Stream)

        ' kopiuj metadatane z bak do org
        Dim oExifLib As New CompactExifLib.ExifData(bakFileName)
        oExifLib.Save(orgFileName)

        Return True
    End Function


    Private Async Sub uiResize_Click(sender As Object, e As RoutedEventArgs)
        If Not Await SprawdzCzyJestEdycja(EditModeEnum.resize) Then Return

        ShowHideEditControls(EditModeEnum.resize)
        ' *TODO* zapytanie o rozmiar, albo coś tego typu

        '    Public void ResizeImage(String sImageFile, Decimal dWidth, Decimal dHeight, String sOutputFile)
        '{
        '    Image oImg = Bitmap.FromFile(sImageFile);
        '    Bitmap oBMP = New Bitmap(Decimal.ToInt16(dWidth), Decimal.ToInt16(dHeight));

        '    Graphics g = Graphics.FromImage(oBMP);
        '    g.PageUnit = pgUnits;
        '    g.SmoothingMode = psMode;
        '    g.InterpolationMode = piMode;
        '    g.PixelOffsetMode = ppOffsetMode;

        '    g.DrawImage(oImg, 0, 0, Decimal.ToInt16(dWidth), Decimal.ToInt16(dHeight));

        '    ImageCodecInfo oEncoder = GetEncoder();
        '    EncoderParameters oENC = New EncoderParameters(1);

        '    oENC.Param[0] = New EncoderParameter(System.Drawing.Imaging.Encoder.Quality, plEncoderQuality);

        '    oImg.Dispose();

        '    oBMP.Save(sOutputFile, oEncoder, oENC);
        '    g.Dispose();

        '}
    End Sub
    Private Sub uiSizing_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double))
        ' *TODO* pokaż zmianę
    End Sub

    Private Async Sub uiRotate_Click(sender As Object, e As RoutedEventArgs)
        If Not Await SprawdzCzyJestEdycja(EditModeEnum.rotate) Then Return

        ShowHideEditControls(EditModeEnum.rotate)

        uiRotateUp.IsChecked = True
        uiRotateDown.IsChecked = False
        uiRotateLeft.IsChecked = False
        uiRotateRight.IsChecked = False

    End Sub

    Private Async Sub uiSave_Click(sender As Object, e As RoutedEventArgs)
        Await SaveChanges()
        ShowHideEditControls(EditModeEnum.none)
    End Sub

    Private Sub uiRevert_Click(sender As Object, e As RoutedEventArgs)

        Dim sJpgFilename As String = _picek.oPic.InBufferPathName
        Dim sBakFileName As String = sJpgFilename & ".bak"

        If Not IO.File.Exists(sBakFileName) Then
            vb14.DialogBox("Nie istnieje plik backup?" & vbCrLf & $"({sBakFileName})")
            Return
        End If

        IO.File.Delete(sJpgFilename)
        IO.File.Move(sBakFileName, sJpgFilename)
        IO.File.SetCreationTime(sJpgFilename, IO.File.GetLastWriteTime(sJpgFilename))

        ' no i przerysowujemy wszystko
        Window_Loaded(Nothing, Nothing)

    End Sub

#End Region

End Class

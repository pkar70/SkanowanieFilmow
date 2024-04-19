

Imports System.IO   ' for AsRandomAccess
Imports wingraph = Windows.Graphics.Imaging
Imports winstreams = Windows.Storage.Streams
Imports vb14 = Vblib.pkarlibmodule14
Imports Windows.Storage.Streams
Imports CompactExifLib
Imports Vblib
Imports pkar
'Imports Windows.UI.Xaml.Controls
Imports MediaDevices
Imports pkar.DotNetExtensions
Imports pkar.UI.Extensions

Public Class ShowBig

    Private _picek As ProcessBrowse.ThumbPicek
    Private _inArchive As Boolean
    Private _inSlideShow As Boolean
    Private _timer As New System.Windows.Threading.DispatcherTimer

    Public Sub New(oPicek As ProcessBrowse.ThumbPicek, bInArchive As Boolean, bSlideShow As Boolean)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        _picek = oPicek
        _inArchive = bInArchive
        _inSlideShow = bSlideShow
        DataContext = _picek.oPic

        AddHandler _timer.Tick, AddressOf Timer_Ticked
    End Sub

    Public Sub New(oPicek As Vblib.OnePic, bInArchive As Boolean, bSlideShow As Boolean)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        _picek = New ProcessBrowse.ThumbPicek(oPicek, 0)
        _inArchive = bInArchive
        _inSlideShow = bSlideShow
        DataContext = _picek.oPic

        AddHandler _timer.Tick, AddressOf Timer_Ticked
    End Sub

    Private _bitmap As BitmapImage

    Private _inScaling As Boolean

    ' skopiowane z InstaMonitor
    Private Sub ZmianaRozmiaruImg()

        If _bitmap Is Nothing Then Return

        If _inScaling Then Return
        _inScaling = True

        Dim szer As Double = Math.Max(200, _bitmap.PixelWidth)
        Dim wysok As Double = Math.Max(200, _bitmap.PixelHeight)

        If uiFullPicture.Stretch = Stretch.None Then
            uiFullPicture.Width = szer
            uiFullPicture.Height = wysok
        Else

            If szer < uiMainPicScroll.ViewportWidth And
               wysok < uiMainPicScroll.ViewportHeight Then
                uiFullPicture.Width = szer
                uiFullPicture.Height = wysok
            Else
                Dim dScaleX As Double = szer / uiMainPicScroll.ViewportWidth
                Dim dScaleY As Double = wysok / uiMainPicScroll.ViewportHeight

                Dim dDesiredScale As Double = Math.Max(dScaleX, dScaleY)
                uiFullPicture.Width = szer / dDesiredScale
                uiFullPicture.Height = wysok / dDesiredScale

            End If

        End If

        _inScaling = False
    End Sub

    Private Shared Function DetermineOrientation(oPic As Vblib.OnePic) As Rotation
        Dim oExif As Vblib.ExifTag

        'oExif = oPic.GetExifOfType(Vblib.ExifSource.ManualRotate)
        'If oExif Is Nothing Then
        oExif = oPic.GetExifOfType(Vblib.ExifSource.FileExif)

        If oExif Is Nothing Then Return Rotation.Rotate0

        Return OrientationToRotation(oExif.Orientation)

    End Function

    Private Async Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        Me.Title = _picek.oPic.InBufferPathName
        Me.InitDialogs
        uiEditCrop.IsEnabled = True ' po zmianie zdjęcia włącza Crop (bywa zablokowany po FullSize)

        Dim iObrot As Rotation = DetermineOrientation(_picek.oPic)

        ' *TODO* reakcja jakaś na inne typy niż JPG
        ' *TODO* dla NAR (Lumia950), MP4 (Lumia*), AVI (Fuji), MOV (iPhone) są specjalne obsługi

        '_bitmap = Await ProcessBrowse.WczytajObrazek(_picek.oPic.InBufferPathName, 0, iObrot)
        _bitmap = Await _picek.PicForBig(iObrot)
        If _bitmap Is Nothing Then Return

        Me.Title = _picek.oPic.InBufferPathName &
            $" ({_bitmap.Width.ToString("F0")}×{_bitmap.Height.ToString("F0")})" &
            $" [{_picek.nrkol}/{_picek.maxnum}]"

        UpdateClipRegion() ' tym razem, gdyż editmode=none, likwidacja crop

        ' tylko JPG może być edytowany
        uiEditModes.IsEnabled = IO.Path.GetExtension(_picek.oPic.InBufferPathName).EqualsCI(".jpg")

        uiSave.IsEnabled = False
        uiRevert.IsEnabled = False
        If IO.File.Exists(_picek.oPic.InBufferPathName & ".bak") Then uiRevert.IsEnabled = True

        ' skalowanie okna
        ZmienRozmiarOkna(iObrot)
        uiFullPicture.Source = _bitmap

        ' UpdateClipRegion()
        uiFullPicture.ToolTip = _picek.sDymek & vbCrLf & $"Size: {_bitmap.PixelWidth}×{_bitmap.PixelHeight}"


        If _inArchive Then
            uiBatchProcessors.Visibility = Visibility.Collapsed
            uiEditModes.Visibility = Visibility.Collapsed
            uiDelete.Visibility = Visibility.Collapsed
            uiGeotag.Visibility = Visibility.Collapsed
            uiDatetag.Visibility = Visibility.Collapsed
        Else
            'ProcessBrowse.WypelnMenuBatchProcess(uiBatchProcessors, AddressOf ApplyBatchProcess)
            uiBatchProcessors.Visibility = Visibility.Visible
            uiEditModes.Visibility = Visibility.Visible
            uiDelete.Visibility = Visibility.Visible
            uiGeotag.Visibility = Visibility.Visible
            uiDatetag.Visibility = Visibility.Visible
        End If

        'ProcessBrowse.WypelnMenuAutotagerami(uiMenuTaggers, AddressOf ApplyTagger)
        'ProcessBrowse.WypelnMenuCloudPublish(_picek.oPic, uiMenuPublish, AddressOf ApplyPublish)
        'ProcessBrowse.WypelnMenuCloudArchives(_picek.oPic, uiMenuCloudArch, AddressOf ApplyCloudArch)

        If String.IsNullOrEmpty(_picek.oPic.fileTypeDiscriminator) Then
            uiIkonkaTypu.Visibility = Visibility.Collapsed
        Else
            uiIkonkaTypu.Visibility = Visibility.Visible
            uiIkonkaTypu.Content = _picek.oPic.fileTypeDiscriminator

            If _picek.oPic.fileTypeDiscriminator = "✋" Then
                uiFullPicture.ContextMenu = Nothing
            End If
        End If

        TimerOnOff()

        ' MenuAutoTaggerow()

    End Sub

    Private Sub ZmienRozmiarOkna(iObrot As Rotation)
        DumpCurrMethod($"(iObrot={iObrot})")

        Dim maxPic As Double = vb14.GetSettingsInt("uiBigPicSize", 90) / 100.0

        Dim scrWidth As Double = SystemParameters.FullPrimaryScreenWidth * maxPic
        Dim scrHeight As Double = SystemParameters.FullPrimaryScreenHeight * maxPic

        Dim MARGIN_X As Integer = 40
        Dim MARGIN_Y As Integer = 80

        iObrot = Rotation.Rotate0 ' jednak to pomijamy - bo obracamy wczytując

        Dim imgSize As Size
        If iObrot = Rotation.Rotate90 OrElse iObrot = Rotation.Rotate270 Then
            imgSize = New Size(_bitmap.PixelHeight, _bitmap.PixelWidth)
        Else
            imgSize = New Size(_bitmap.PixelWidth, _bitmap.PixelHeight)
        End If

        If scrHeight > imgSize.Height AndAlso scrWidth > imgSize.Width Then
            ' Screen size: 1152, 873.9; bitmap size: 600, 450, so scale: 0.5208333333333334
            DumpMessage($"Screen bigger than picture ({imgSize.Width}, {imgSize.Height}), no scaling - full picture")

            ' gdy nie zmieniam uiFullPicture ani Me: OK
            ' gdy nie zmieniam Me: OK (tylko duza ramka wokol)
            Me.Height = Math.Max(200, imgSize.Height + MARGIN_Y)
            Me.Width = Math.Max(200, imgSize.Width + MARGIN_X)
            uiFullPicture.Width = imgSize.Width
            uiFullPicture.Height = imgSize.Height

            ' niby powinno być: none
            uiFullPicture.Stretch = Stretch.Uniform

        Else
            Dim dScaleX As Double = imgSize.Width / scrWidth
            Dim dScaleY As Double = imgSize.Height / scrHeight
            Dim dDesiredScale As Double = Math.Max(dScaleX, dScaleY)

            Dim scaledImg As New Size(imgSize.Width / dDesiredScale, imgSize.Height / dDesiredScale)

            DumpMessage($"Scaling {dDesiredScale} to image: {scaledImg.Width}, {scaledImg.Height})")

            Me.Height = scaledImg.Height + MARGIN_Y
            Me.Width = scaledImg.Width + MARGIN_X
            uiFullPicture.Width = scaledImg.Width
            uiFullPicture.Height = scaledImg.Height

            uiFullPicture.Stretch = Stretch.Uniform
        End If


    End Sub

    Private Sub TimerOnOff()
        If _inSlideShow Then
            uiSlideshow.Header = "Stop slideshow"
            _timer.Interval = TimeSpan.FromSeconds(vb14.GetSettingsInt("uiSlideShowSeconds"))
            _timer.Start()
        Else
            _timer.Stop()
            uiSlideshow.Header = "Start slideshow"
        End If
    End Sub

    Private Sub Timer_Ticked(sender As Object, e As EventArgs)
        ChangePicture(1, False)
    End Sub



    Private Shared Function OrientationToRotation(v As Vblib.OrientationEnum?) As Rotation
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


    Private _inWinResize As Boolean

    Private Sub Window_SizeChanged(sender As Object, e As SizeChangedEventArgs)
        If _inWinResize Then Return
        _inWinResize = True
        ZmianaRozmiaruImg()
        _inWinResize = False
    End Sub

    Private Sub uiResizePic_Click(sender As Object, e As MouseButtonEventArgs)
        Dim oResize As Stretch = uiFullPicture.Stretch
        DumpCurrMethod($"(switching from {oResize.ToString})")

        Select Case oResize
            Case Stretch.Uniform
                uiFullPicture.Stretch = Stretch.None
                Me.Width -= 1
                uiEditCrop.IsEnabled = False
                ' *TODO* nie widać całości zdjęcia! ale dotyczy chyba tylko zdjęć z auto-obracaniem
            Case Stretch.None
                uiFullPicture.Stretch = Stretch.Uniform
                Me.Width += 1
                ZmianaRozmiaruImg() ' bez tego nie wraca - prościej, choć może dałoby się inaczej: https://stackoverflow.com/questions/31146750/alternate-between-stretch-uniform-and-stretching-none-for-viewbox-with-scrol
                uiEditCrop.IsEnabled = True
        End Select

    End Sub

    Private Sub uiMetadataChanged(sender As Object, e As EventArgs)
        SaveMetaData()
    End Sub

    Private Sub uiPictureChanged(sender As Object, e As EventArgs)
        Window_Loaded(sender, Nothing)
    End Sub


    'Private Sub uiDescribe_Click(sender As Object, e As RoutedEventArgs)
    '    Dim oWnd As New AddDescription(_picek.oPic)
    '    If Not oWnd.ShowDialog Then Return

    '    Dim oDesc As Vblib.OneDescription = oWnd.GetDescription
    '    _picek.oPic.AddDescription(oDesc)
    '    SaveMetaData()
    'End Sub

    Private Sub SaveMetaData()
        Dim oBrowserWnd As ProcessBrowse = Me.Owner
        If oBrowserWnd Is Nothing Then Return

        oBrowserWnd.SaveMetaData()
    End Sub

    Private Sub ChangePicture(iKierunek As Integer, bShifty As Boolean)
        Dim oBrowserWnd As ProcessBrowse = Me.Owner
        If oBrowserWnd Is Nothing Then Return

        Dim picek As ProcessBrowse.ThumbPicek = oBrowserWnd.FromBig_Next(_picek, iKierunek, _inSlideShow)
        If picek Is Nothing Then
            Me.Close()  ' koniec obrazków
        Else

            If bShifty Then
                ' nowe okno
                Dim oWnd As New ShowBig(picek, _inArchive, _inSlideShow)
                oWnd.Owner = Me.Owner
                oWnd.Show()
                oWnd.Focus()
            Else
                _MojeDataContextChange = True
                ' to okno
                _picek = picek
                DataContext = _picek.oPic
                Window_Loaded(Nothing, Nothing)
                _MojeDataContextChange = False
            End If

        End If

    End Sub

    Private Sub Window_KeyUp(sender As Object, e As KeyEventArgs)

        ' lepiej nie kasować z repeat klawisza
        If e.IsRepeat Then
            System.Media.SystemSounds.Beep.Play()
            Return
        End If

        Dim bShifty As Boolean = Keyboard.IsKeyDown(Key.RightShift) Or Keyboard.IsKeyDown(Key.LeftShift)
        Dim bCntr As Boolean = Keyboard.IsKeyDown(Key.RightCtrl) Or Keyboard.IsKeyDown(Key.LeftCtrl)

        Select Case e.Key
            Case Key.Space
                ChangePicture(1, bShifty)
            Case Key.PageUp
                ChangePicture(-1, bShifty)
            Case Key.PageDown
                ChangePicture(1, bShifty)
            Case Key.Home
                ChangePicture(-100, bShifty)
            Case Key.End
                ChangePicture(100, bShifty)
            Case Key.Delete
                uiDelPic_Click(Nothing, Nothing)
            'Case Key.D
            '    If bCntr Then uiDescribe - tylko co dalej? nie RaiseEvent, nie .CLick
            'Case Key.Enter
            '    ' full screen / mały screen
            Case Key.Escape
                Me.Close()
            Case Key.D
                If Not Keyboard.IsKeyDown(Key.LeftAlt) Then Return
                ' nie ma bezpośrednio Describe tu uruchamianego, jakoś przez userControl to zrobić
                Dim oWnd As New AddDescription(_picek.oPic)
                If Not oWnd.ShowDialog Then Return
                Dim oDesc As Vblib.OneDescription = oWnd.GetDescription
                _picek.oPic.AddDescription(oDesc)
            Case Else
                System.Media.SystemSounds.Beep.Play()
        End Select

    End Sub

    ' jako zwykły menuitem, bo jest też wywoływany klawiszowo
    Private Async Sub uiDelPic_Click(sender As Object, e As RoutedEventArgs)
        If Not vb14.GetSettingsBool("uiNoDelConfirm") Then
            If Not Await Me.DialogBoxYNAsync($"Skasować zdjęcie ({_picek.oPic.sSuggestedFilename})?") Then Return
        End If

        Dim oBrowserWnd As ProcessBrowse = Me.Owner
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
        flip
    End Enum

    Private _editMode As EditModeEnum = EditModeEnum.none

    Private Async Function SprawdzCzyJestEdycja(editMode As EditModeEnum) As Task
        If _editMode <> EditModeEnum.none Then

            If _editMode <> editMode Then ' jeśli włączamy to samo, to nie ma powodu pytać o zapis

                Dim bSave As Boolean = Await Me.DialogBoxYNAsync("Jest już edycja, zapisać zmiany?")

                If bSave Then Await SaveChanges()
            End If

        End If

        _editMode = editMode

        Return
    End Function

    'Private Shared Function RotateToDegree(iRot As Rotation) As Integer
    '    Select Case iRot
    '        Case Rotation.Rotate0
    '            Return 0
    '        Case Rotation.Rotate90
    '            Return 90
    '        Case Rotation.Rotate180
    '            Return 180
    '        Case Rotation.Rotate270
    '            Return 270
    '    End Select
    '    ' tego nie powinno być, bo nie ma innej mozliwosci
    '    Return 0
    'End Function

    'Private Shared Function DegreeToBitmapRotate(iKat As Integer) As wingraph.BitmapRotation
    '    Select Case iKat
    '        Case 0
    '            Return wingraph.BitmapRotation.None
    '        Case 90
    '            Return wingraph.BitmapRotation.Clockwise90Degrees
    '        Case 180
    '            Return wingraph.BitmapRotation.Clockwise180Degrees
    '        Case 270
    '            Return wingraph.BitmapRotation.Clockwise270Degrees
    '    End Select
    '    ' tego nie powinno być, bo nie ma innej mozliwosci
    '    Return wingraph.BitmapRotation.None
    'End Function


    'Public Shared Function KatToOrientationEnum(iKat As Integer) As Vblib.OrientationEnum
    '    If iKat > 359 Then iKat -= 360
    '    Select Case iKat
    '        Case 0
    '            Return Vblib.OrientationEnum.topLeft
    '        Case 90
    '            Return Vblib.OrientationEnum.rightTop
    '        Case 180
    '            Return Vblib.OrientationEnum.bottomRight
    '        Case 270
    '            Return Vblib.OrientationEnum.leftBottom
    '    End Select
    '    ' tego nie powinno być, bo nie ma innej mozliwosci
    '    Return Vblib.OrientationEnum.topLeft
    'End Function

    Private Async Function SaveChanges() As Task
        If Not OperatingSystem.IsWindows Then Return
        If Not OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240) Then Return

        Dim transf As New wingraph.BitmapTransform
        Dim sHistory As String = ""

        Select Case _editMode
            Case EditModeEnum.none
                Return
            Case EditModeEnum.crop
                ' jesli jest rotate, to moze byc odwrocenie width/height
                Dim oRect As Rect = ConvertSlidersToPicRect(_bitmap.PixelWidth, _bitmap.PixelHeight)
                Dim bnds As New wingraph.BitmapBounds
                bnds.X = oRect.X
                bnds.Y = oRect.Y
                bnds.Width = oRect.Width
                bnds.Height = oRect.Height
                transf.Bounds = bnds

                sHistory = "Cropped to " &
                 $"({Math.Min(uiCropUp.Value, uiCropDown.Value).ToString("F2")} .. {Math.Max(uiCropUp.Value, uiCropDown.Value).ToString("F2")} × " &
                 $"({Math.Min(uiCropLeft.Value, uiCropRight.Value).ToString("F2")} .. {Math.Max(uiCropLeft.Value, uiCropRight.Value).ToString("F2")})"

            Case EditModeEnum.resize
                ' *TODO* resize
            Case EditModeEnum.rotate

                If uiRotateUp.IsChecked Then
                    transf = Nothing ' czyli bez zmian, ale trzeba zakończyć
                Else
                    sHistory = "Rotated "
                    If uiRotateRight.IsChecked Then
                        sHistory += "270"
                        transf.Rotation = wingraph.BitmapRotation.Clockwise270Degrees
                    End If
                    If uiRotateDown.IsChecked Then
                        sHistory += "180"
                        transf.Rotation = wingraph.BitmapRotation.Clockwise180Degrees
                    End If
                    If uiRotateLeft.IsChecked Then
                        sHistory += "90"
                        transf.Rotation = wingraph.BitmapRotation.Clockwise90Degrees
                    End If

                    sHistory &= " degrees"
                End If

            Case EditModeEnum.flip
                sHistory = "Flipped horizontally"
                transf.Flip = Windows.Graphics.Imaging.BitmapFlip.Horizontal
        End Select

        If transf IsNot Nothing Then
            _picek.oPic.AddEditHistory(sHistory)

            Dim oExif As Vblib.ExifTag = _picek.oPic.GetExifOfType(Vblib.ExifSource.FileExif)
            If oExif IsNot Nothing Then
                ' reset orientation, bo loadbitmap i tak obraca
                oExif.Orientation = Vblib.OrientationEnum.topLeft
            End If


            SaveMetaData()

            Await ZapiszZmianyObrazka(transf) ' w tym wczytanie obrazka (miniaturki) na nowo

        End If

        _editMode = EditModeEnum.none
        ShowHideEditControls(EditModeEnum.none)
    End Function

#Region "crop"

    Private Sub ShowHideCropSliders(bShow As Boolean)
        Dim visib As Visibility = If(bShow, Visibility.Visible, Visibility.Collapsed)

        uiCropUp.Visibility = visib
        uiCropDown.Visibility = visib
        uiCropLeft.Visibility = visib
        uiCropRight.Visibility = visib

        If Not bShow Then
            uiFullPicture.Clip = Nothing
            Return
        End If

        If uiCropUp.Value = 0 Then uiCropUp.Value = 0.1
        If uiCropDown.Value = 0 Then uiCropDown.Value = 0.9
        If uiCropLeft.Value = 0 Then uiCropLeft.Value = 0.1
        If uiCropRight.Value = 0 Then uiCropRight.Value = 0.9

        UpdateClipRegion()
    End Sub


    Private Sub ShowHideEditControls(iMode As EditModeEnum)
        ShowHideCropSliders(iMode = EditModeEnum.crop)
        ShowHideRotateBoxes(iMode = EditModeEnum.rotate)
        'ShowHideSizeSlider(iMode = EditModeEnum.resize)

        uiSave.IsEnabled = (iMode <> EditModeEnum.none)
    End Sub

    Private Function ConvertSlidersToPicRect(dWidth As Double, dHeight As Double) As Rect
        Dim oRect As New Rect
        oRect.X = dWidth * (Math.Min(uiCropUp.Value, uiCropDown.Value))
        oRect.Width = dWidth * (Math.Abs(uiCropDown.Value - uiCropUp.Value))

        oRect.Y = dHeight * (Math.Min(uiCropLeft.Value, uiCropRight.Value))
        oRect.Height = dHeight * (Math.Abs(uiCropRight.Value - uiCropLeft.Value))

        Return oRect
    End Function

    Private Sub UpdateClipRegion()

        If Not _editMode = EditModeEnum.crop OrElse Double.IsNaN(uiFullPicture.Width) OrElse Double.IsNaN(uiFullPicture.Height) Then
            uiFullPicture.Clip = Nothing
            Return
        End If

        Dim oRect As Rect = ConvertSlidersToPicRect(uiFullPicture.Width, uiFullPicture.Height)
        Dim rectGeom As New RectangleGeometry(oRect)
        uiFullPicture.Clip = rectGeom

    End Sub
    Private Async Sub uiCrop_Click(sender As Object, e As RoutedEventArgs)

        Await SprawdzCzyJestEdycja(EditModeEnum.crop)

        If uiCropUp.Visibility = Visibility.Visible Then
            ShowHideEditControls(EditModeEnum.none)
            Await SaveChanges()

            uiFullPicture.Clip = Nothing
            Return
        End If

        ShowHideEditControls(EditModeEnum.crop)
        UpdateClipRegion()
    End Sub
    Private Sub uiCrop_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double))
        UpdateClipRegion()
    End Sub

#End Region


    Private Async Function ZapiszZmianyObrazka(bmpTrans As wingraph.BitmapTransform) As Task(Of Boolean)
        If Not OperatingSystem.IsWindows Then Return False
        If Not OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240) Then Return False

        DumpCurrMethod($"(Transform: rotation={bmpTrans.Rotation.ToString}, crop={bmpTrans.Bounds.X}+{bmpTrans.Bounds.Width}, {bmpTrans.Bounds.Y}+ {bmpTrans.Bounds.Height})")


        ' *TODO* (może) do Settings:Misc, [] ask for confirm after EditSave (przy Crop, i ew. Rotate)
        ' If Not Await vb14.DialogBoxYNAsync("Zapisać zmiany?") Then Return False

        _picek.oPic.InitEdit(False)

        Using oStream As New InMemoryRandomAccessStream

            Dim oEncoder As wingraph.BitmapEncoder = Await Process_AutoRotate.GetJpgEncoderAsync(oStream)

            ' kopiujemy informacje o tym co jest do zrobienia - bo nie można po prostu podmienić
            oEncoder.BitmapTransform.Bounds = bmpTrans.Bounds
            oEncoder.BitmapTransform.Rotation = bmpTrans.Rotation
            oEncoder.BitmapTransform.ScaledHeight = bmpTrans.ScaledHeight ' na razie to jest nieużywane
            oEncoder.BitmapTransform.ScaledWidth = bmpTrans.ScaledWidth ' na razie to jest nieużywane
            oEncoder.BitmapTransform.Flip = bmpTrans.Flip

            Try

                oEncoder.SetSoftwareBitmap(Await Process_AutoRotate.LoadSoftBitmapAsync(_picek.oPic))

                ' gdy to robię na zwyklym AsRandomAccessStream to się wiesza
                Await oEncoder.FlushAsync()

                Process_AutoRotate.SaveSoftBitmap(oStream, _picek.oPic)
                'Process_AutoRotate.SaveSoftBitmap(oStream, _picek.oPic.sFilenameEditDst, _picek.oPic.sFilenameEditSrc)

                _picek.oPic.EndEdit(True, True)

            Catch ex As Exception
                Me.MsgBox("Błąd zapisu zmian:" & vbCrLf & ex.Message)
            End Try


        End Using

        IO.File.Delete(_picek.ThumbGetFilename) ' no exception if not found

        '_picek.oImageSrc = Await ProcessBrowse.WczytajObrazek(_picek.oPic.InBufferPathName, 400, Rotation.Rotate0)
        Await _picek.ThumbWczytajLubStworz(_inArchive)

        Return True
    End Function

#If RESIZE_HERE Then
    Private Async Sub uiResize_Click(sender As Object, e As RoutedEventArgs)
        If Not Await SprawdzCzyJestEdycja(EditModeEnum.resize) Then Return

        ShowHideEditControls(EditModeEnum.resize)

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
    'Private Sub uiSizing_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double))
    '    ' *TODO* pokaż zmianę
    'End Sub

        'Public Sub ShowHideSizeSlider(bShow As Boolean)
    '    Dim visib As Visibility = If(bShow, Visibility.Visible, Visibility.Collapsed)

    '    'uiSizingDown.Visibility = visib

    'End Sub


#End If

#Region "rotate"
    Private Sub ShowHideRotateBoxes(bShow As Boolean)
        Dim visib As Visibility = If(bShow, Visibility.Visible, Visibility.Collapsed)

        uiRotateUp.Visibility = visib
        uiRotateDown.Visibility = visib
        uiRotateLeft.Visibility = visib
        uiRotateRight.Visibility = visib

    End Sub

    Private _inRotateInit As Boolean = False

    Private Async Sub uiRotate_Click(sender As Object, e As RoutedEventArgs)

        _inRotateInit = True
        Await SprawdzCzyJestEdycja(EditModeEnum.rotate)

        ShowHideEditControls(EditModeEnum.rotate)

        uiRotateUp.IsChecked = True
        uiRotateDown.IsChecked = False
        uiRotateLeft.IsChecked = False
        uiRotateRight.IsChecked = False

        _inRotateInit = False
    End Sub

    Private Async Function uiRotateMenu2UI(bLeft As Boolean, bDown As Boolean, bRight As Boolean) As Task
        _inRotateInit = True
        Await SprawdzCzyJestEdycja(EditModeEnum.rotate)

        uiRotateRight.IsChecked = bRight
        uiRotateDown.IsChecked = bDown
        uiRotateLeft.IsChecked = bLeft

        _inRotateInit = False

        uiRotate_Checked(Nothing, Nothing)
    End Function

    Private Async Sub uiRotateRight_Click(sender As Object, e As RoutedEventArgs)
        Await uiRotateMenu2UI(False, False, True)
    End Sub
    Private Async Sub uiRotateDown_Click(sender As Object, e As RoutedEventArgs)
        Await uiRotateMenu2UI(False, True, False)
    End Sub
    Private Async Sub uiRotateLeft_Click(sender As Object, e As RoutedEventArgs)
        Await uiRotateMenu2UI(True, False, False)
    End Sub

    Private Sub uiRotate_Checked(sender As Object, e As RoutedEventArgs)

        If _editMode <> EditModeEnum.rotate Then Return
        If _inRotateInit Then Return
        ' równoważne Save - wybór góry zdjęcia automatycznie kończy operację, nie trzeba SAVE
        ' *TODO* (może) do Settings:Misc, [] autosave after manual rotate

        uiSave_Click(sender, e)
    End Sub

#End Region


    Private Async Sub uiSave_Click(sender As Object, e As RoutedEventArgs)
        Await SaveChanges()

        Window_Loaded(Nothing, Nothing)

        ' ShowHideEditControls(EditModeEnum.none)
    End Sub

    Private Sub uiRevert_Click(sender As Object, e As RoutedEventArgs)

        Dim sJpgFilename As String = _picek.oPic.InBufferPathName
        Dim sBakFileName As String = sJpgFilename & ".bak"

        If Not IO.File.Exists(sBakFileName) Then
            Me.MsgBox("Nie istnieje plik backup?" & vbCrLf & $"({sBakFileName})")
            Return
        End If

        IO.File.Delete(sJpgFilename)
        IO.File.Move(sBakFileName, sJpgFilename)
        FileAttrHidden(sJpgFilename, False)
        IO.File.SetCreationTime(sJpgFilename, IO.File.GetLastWriteTime(sJpgFilename))

        ' no i przerysowujemy wszystko
        Window_Loaded(Nothing, Nothing)

    End Sub

    Private Async Sub uiIkonkaTypu_Click(sender As Object, e As RoutedEventArgs)
        ' kliknięcie na typie, znaczy "*" lub ">"

        Select Case uiIkonkaTypu.Content
            Case "*" ' plik NAR
                ' otwórz trzy okienka? (po jednym na każdy JPG w NAR, z zablokowaniem DELETE itp.)? Ale mógłby być "wybierator", że wybieramy jeden JPG i zamieniamy buffer.OnePic(NAR) na (JPG)
                ' jest _picek.oPic.InBufferPathName
                ' z niego for each plik w ZIP
                ' open 

                Using oArchive = IO.Compression.ZipFile.OpenRead(_picek.oPic.InBufferPathName)

                    For Each oInArch As IO.Compression.ZipArchiveEntry In oArchive.Entries
                        If Not IO.Path.GetExtension(oInArch.Name).EqualsCI(".jpg") Then Continue For
                        ' mamy JPGa (a nie XML na przykład)

                        Dim sJpgFileName As String = IO.Path.Combine(IO.Path.GetTempPath, oInArch.Name)
                        File.Delete(sJpgFileName)
                        Using oWrite As Stream = IO.File.Create(sJpgFileName)
                            Await oInArch.Open.CopyToAsync(oWrite)
                            Await oWrite.FlushAsync()
                        End Using

                        Dim newPic As New OnePic("NAR extract", _picek.oPic.InBufferPathName, oInArch.Name)
                        newPic.InBufferPathName = sJpgFileName
                        newPic.sSuggestedFilename = oInArch.Name
                        newPic.Exifs = _picek.oPic.Exifs
                        newPic.fileTypeDiscriminator = "✋"

                        Dim newWind As New ShowBig(newPic, True, False)
                        newWind.Owner = Me.Owner
                        newWind.Show()
                    Next
                End Using
                Me.Close()

            Case "►" ' filmik
                uiMovie.Source = New Uri(_picek.oPic.InBufferPathName)
                uiMovie.Visibility = Visibility.Visible
                uiFullPicture.Visibility = Visibility.Collapsed
                uiIkonkaTypu.Content = "■"
                uiMovie.Play()
            Case "■"
                uiMovie.Visibility = Visibility.Collapsed
                uiFullPicture.Visibility = Visibility.Visible
                uiIkonkaTypu.Visibility = Visibility.Visible
                uiIkonkaTypu.Content = "►"
                uiMovie.Stop()
            Case "✋"
                If Not Await Me.DialogBoxYNAsync($"Czy podmienić JPG na {_picek.oPic.sSuggestedFilename}?") Then Return

                Dim targetJPG As String = _picek.oPic.sInSourceID ' ustawiony powyżej, otwierając NAR, na path/filename.nar
                targetJPG = Path.ChangeExtension(targetJPG, "jpg")
                Dim sourceJPG As String = _picek.oPic.InBufferPathName

                If File.Exists(targetJPG & ".bak") Then
                    If Not Await Me.DialogBoxYNAsync("Były edycje pliku JPG, kontynuować?") Then Return
                    Vblib.DumpMessage($"usuwam stary BAK")
                    File.Delete(targetJPG & ".bak")
                End If

                If File.Exists(targetJPG) Then
                    Vblib.DumpMessage($"targetJPG {targetJPG} juz istnieje, usuwam")
                    File.Delete(targetJPG)
                End If

                If File.Exists(ProcessBrowse.ThumbPicek.ThumbGetFilename(targetJPG)) Then
                    Vblib.DumpMessage($"usuwam stary thumb")
                    File.Delete(ProcessBrowse.ThumbPicek.ThumbGetFilename(targetJPG))
                End If

                File.Move(sourceJPG, targetJPG)

                If Await Me.DialogBoxYNAsync("Czy skasować źródłowy NAR?") Then
                    Dim oBrowserWnd As ProcessBrowse = Me.Owner
                    If oBrowserWnd Is Nothing Then Return
                    oBrowserWnd.DeleteByFilename(_picek.oPic.sInSourceID)
                End If

                Me.Close()
            Case "⧉"
                Dim zipname As String = IO.Path.GetFileNameWithoutExtension(_picek.oPic.InBufferPathName)
                zipname = zipname.Replace(".stereo", "") ' usuwamy .stereo.jpg

                Dim stereopackfolder As String = IO.Path.Combine(IO.Path.GetTempPath, zipname)

                ' rozpakuj do TEMPa
                If IO.Directory.Exists(stereopackfolder) Then
                    If Not Await Me.DialogBoxYNAsync($"Katalog '{stereopackfolder}' istnieje ({IO.Directory.GetLastWriteTime(stereopackfolder).ToExifString}), skasować?") Then Return
                    IO.Directory.Delete(stereopackfolder, True)
                End If

                IO.Directory.CreateDirectory(stereopackfolder)
                IO.Compression.ZipFile.ExtractToDirectory(_picek.oPic.InBufferPathName, stereopackfolder)

                ' rename anaglyph na stereo.jpg
                Dim poprzedniAnaglyph As String = IO.Path.Combine(stereopackfolder, "anaglyph.jpg")
                If IO.File.Exists(poprzedniAnaglyph) Then
                    IO.File.Move(poprzedniAnaglyph, IO.Path.Combine(stereopackfolder, zipname) & ".stereo.jpg")
                End If

                ' explorer (ewentualnie)
                'Dim cmdline As String = $"explorer.exe /select ""{stereopackfolder}"""
                Process.Start("explorer.exe", stereopackfolder)

                ' runSPM
                If Not Await ProcessBrowse.StereoRunSpmOnPack(stereopackfolder, True) Then
                    ProcessBrowse.StereoRemoveFolder(stereopackfolder)
                    Return
                End If

                ' *TODO* sprawdz daty, czy sie zmienily - NIE: koniec (ewentualnie)

                ' spakuj na nowo
                Dim packZipName As String = _picek.oPic.InBufferPathName
                ProcessBrowse.StereoFolderToZip(packZipName, stereopackfolder)
                ProcessBrowse.StereoRemoveFolder(stereopackfolder)

        End Select

        Return
    End Sub

    Private Sub uiMovie_Ended(sender As Object, e As RoutedEventArgs)
        uiMovie.Visibility = Visibility.Collapsed
        uiFullPicture.Visibility = Visibility.Visible
        uiIkonkaTypu.Visibility = Visibility.Visible
        uiIkonkaTypu.Content = "►"
    End Sub

    Private Sub uiSlideshow_Click(sender As Object, e As RoutedEventArgs)
        Me.MsgBox("jeszcze nie umiem stąd zrobić")
    End Sub

    Private Async Sub uiFlipHoriz_Click(sender As Object, e As RoutedEventArgs)
        Await SprawdzCzyJestEdycja(EditModeEnum.flip)
        If _editMode <> EditModeEnum.flip Then Return

        uiSave_Click(sender, e)

    End Sub

    Private _MojeDataContextChange As Boolean

    Private Sub Window_DataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs)
        If _editMode <> EditModeEnum.none Then Return
        If DataContext Is Nothing Then Return
        If _bitmap Is Nothing Then Return   ' jeszcze przed inicjalizacją pierwszego - obsługujemy jak dotychczas
        If _MojeDataContextChange Then Return

        If DataContext.GetType Is GetType(ProcessBrowse.ThumbPicek) Then
            _picek = DataContext
        Else
            _picek = New ProcessBrowse.ThumbPicek(DataContext, 0)
        End If

        Window_Loaded(Nothing, Nothing)
    End Sub


#End Region

End Class



Imports System.IO   ' for AsRandomAccess
'Imports System.Text
'Imports Windows.Foundation.Collections
Imports wingraph = Windows.Graphics.Imaging
'Imports Windows.Security
Imports winstreams = Windows.Storage.Streams
Imports vb14 = Vblib.pkarlibmodule14
Imports Windows.Storage.Streams
Imports CompactExifLib
Imports Vblib
Imports pkar
Imports Windows.UI.Xaml.Controls

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

        AddHandler _timer.Tick, AddressOf Timer_Ticked
    End Sub

    Public Sub New(oPicek As Vblib.OnePic, bInArchive As Boolean, bSlideShow As Boolean)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        _picek = New ProcessBrowse.ThumbPicek(oPicek, 0)
        _inArchive = bInArchive
        _inSlideShow = bSlideShow

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

        Dim iObrot As Rotation = DetermineOrientation(_picek.oPic)

        ' *TODO* reakcja jakaś na inne typy niż JPG
        ' *TODO* dla NAR (Lumia950), MP4 (Lumia*), AVI (Fuji), MOV (iPhone) są specjalne obsługi

        _bitmap = Await ProcessBrowse.WczytajObrazek(_picek.oPic.InBufferPathName, 0, iObrot)
        If _bitmap Is Nothing Then Return

        Me.Title = _picek.oPic.InBufferPathName & $" ({_bitmap.Width.ToString("F0")}×{_bitmap.Height.ToString("F0")})"

        UpdateClipRegion() ' tym razem, gdyż editmode=none, likwidacja crop

        ' tylko JPG może być edytowany
        uiEditModes.IsEnabled = _picek.oPic.InBufferPathName.ToLowerInvariant.EndsWith("jpg")

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
            uiShowExif.Visibility = Visibility.Collapsed
        Else
            ProcessBrowse.WypelnMenuBatchProcess(uiBatchProcessors, AddressOf ApplyBatchProcess)
            uiBatchProcessors.Visibility = Visibility.Visible
            uiEditModes.Visibility = Visibility.Visible
            uiDelete.Visibility = Visibility.Visible
            uiShowExif.Visibility = Visibility.Visible
        End If

        ProcessBrowse.WypelnMenuAutotagerami(uiMenuTaggers, AddressOf ApplyTagger)
        ProcessBrowse.WypelnMenuCloudPublish(_picek.oPic, uiMenuPublish, AddressOf ApplyPublish)
        ProcessBrowse.WypelnMenuCloudArchives(_picek.oPic, uiMenuCloudArch, AddressOf ApplyCloudArch)

        OnOffMap()
        SettingsMapsy.WypelnMenuMapami(uiOnMap, AddressOf uiOnMap_Click)

        If String.IsNullOrEmpty(_picek.oPic.fileTypeDiscriminator) Then
            uiIkonkaTypu.Visibility = Visibility.Collapsed
        Else
            uiIkonkaTypu.Visibility = Visibility.Visible
            uiIkonkaTypu.Content = _picek.oPic.fileTypeDiscriminator
        End If

        TimerOnOff()

        ' MenuAutoTaggerow()

    End Sub

    Private Sub ZmienRozmiarOkna(iObrot As Rotation)
        DumpCurrMethod($"(iObrot={iObrot})")

        Dim scrWidth As Double = SystemParameters.FullPrimaryScreenWidth * 0.9
        Dim scrHeight As Double = SystemParameters.FullPrimaryScreenHeight * 0.9

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
        ChangePicture(False, False)
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

    Private Sub OnOffMap()
        Dim oGps As BasicGeopos = _picek.oPic.GetGeoTag
        uiOnMap.IsEnabled = (oGps IsNot Nothing)

    End Sub



    Private Async Sub ApplyTagger(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oSrc As Vblib.AutotaggerBase = oFE?.DataContext
        If oSrc Is Nothing Then Return

        Application.ShowWait(True)
        Dim oExif As Vblib.ExifTag = Await oSrc.GetForFile(_picek.oPic)
        Application.ShowWait(False)
        If oExif IsNot Nothing Then
            _picek.oPic.Exifs.Add(oExif)
            _picek.oPic.TagsChanged = True
        End If
        SaveMetaData()  ' bo zmieniono EXIF

        OnOffMap()    ' bo moze juz bedzie mozna to pokazać
    End Sub

    Private Async Sub ApplyBatchProcess(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oSrc As Vblib.PostProcBase = oFE?.DataContext
        If oSrc Is Nothing Then Return

        Application.ShowWait(True)
        Await oSrc.Apply(_picek.oPic, False, "")
        Application.ShowWait(False)

    End Sub

    Private Async Sub ApplyPublish(sender As Object, e As RoutedEventArgs)
        Dim oFE As MenuItem = sender
        Dim oCloud As Vblib.CloudPublish = oFE?.DataContext
        If oCloud Is Nothing Then Return

        Select Case oFE.Header.ToString.ToLowerInvariant
            Case "open"
                Dim sLink As String = Await oCloud.GetShareLink(_picek.oPic)
                If sLink.ToLowerInvariant.StartsWithOrdinal("http") Then
                    pkar.OpenBrowser(sLink)
                Else
                    If sLink = "" Then sLink = "ERROR getting sharing link"
                    vb14.DialogBox(sLink)   ' error message
                End If
            Case "delete"
                Await oCloud.Delete(_picek.oPic)
            Case "get tags"
                Await oCloud.GetRemoteTags(_picek.oPic)
            Case "share link"
                Dim sLink As String = Await oCloud.GetShareLink(_picek.oPic)
                If sLink.ToLowerInvariant.StartsWithOrdinal("http") Then
                    vb14.ClipPut(sLink)
                    vb14.DialogBox("Link in ClipBoard")
                Else
                    If sLink = "" Then sLink = "ERROR getting sharing link"
                    vb14.DialogBox(sLink)   ' error message
                End If
            Case Else
                If oCloud.sProvider = Publish_AdHoc.PROVIDERNAME Then
                    Dim sFolder As String = SettingsGlobal.FolderBrowser("", "Gdzie wysłać pliki?")
                    If sFolder = "" Then Return
                    oCloud.sZmienneZnaczenie = sFolder
                End If

                Application.ShowWait(True)
                Dim sErr As String = Await oCloud.Login
                If sErr <> "" Then
                    Await vb14.DialogBoxAsync(sErr)
                    Application.ShowWait(False)
                    Return
                End If

                Dim sRet As String = Await oCloud.SendFile(_picek.oPic)
                Application.ShowWait(False)
                If sRet <> "" Then Await vb14.DialogBoxAsync(sRet)

        End Select

        ProcessBrowse.WypelnMenuCloudPublish(_picek.oPic, uiMenuPublish, AddressOf ApplyPublish)

        SaveMetaData()  ' bo zmieniono info o publishingu
    End Sub


    Private Async Sub ApplyCloudArch(sender As Object, e As RoutedEventArgs)
        Dim oFE As MenuItem = sender
        Dim oCloud As Vblib.CloudArchive = oFE?.DataContext
        If oCloud Is Nothing Then Return

        Select Case oFE.Header.ToString.ToLowerInvariant
            Case "open"
                Dim sLink As String = Await oCloud.GetShareLink(_picek.oPic)
                If sLink.ToLowerInvariant.StartsWithOrdinal("http") Then
                    pkar.OpenBrowser(sLink)
                Else
                    If sLink = "" Then sLink = "ERROR getting sharing link"
                    vb14.DialogBox(sLink)   ' error message
                End If
            Case "get tags"
                Await oCloud.GetRemoteTags(_picek.oPic)
                SaveMetaData()  ' bo zmieniono info o publishingu
            Case "share link"
                Dim sLink As String = Await oCloud.GetShareLink(_picek.oPic)
                If sLink.ToLowerInvariant.StartsWithOrdinal("http") Then
                    vb14.ClipPut(sLink)
                    vb14.DialogBox("Link in ClipBoard")
                Else
                    If sLink = "" Then sLink = "ERROR getting sharing link"
                    vb14.DialogBox(sLink)   ' error message
                End If
        End Select

    End Sub
    'Private Sub uiFullPicture_MouseRightButtonDown(sender As Object, e As MouseButtonEventArgs) Handles uiFullPicture.MouseRightButtonDown
    '    uiFlyout.IsOpen = True
    'End Sub

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
                ' to juz pokazuje scrollbary, ale jeszcze im sie wydaje ze nie ma po co przewijac, trzeba zrobic resize okna
                uiMainPicScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Visible
                uiMainPicScroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible
            Case Stretch.None
                uiFullPicture.Stretch = Stretch.Uniform
                uiMainPicScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                uiMainPicScroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
        End Select

        'ZmianaRozmiaruImg()
    End Sub

    Private Sub uiCopyPath_Click(sender As Object, e As RoutedEventArgs)
        'uiFlyout.IsOpen = False
        vb14.ClipPut(_picek.oPic.InBufferPathName)
    End Sub

    Private Sub uiShowExif_Click(sender As Object, e As RoutedEventArgs)
        'uiFlyout.IsOpen = False
        Dim oWnd As New ShowExifs(True) '(_picek.oPic)
        oWnd.DataContext = _picek
        oWnd.Show()
    End Sub

    Private Sub uiShowMetadata_Click(sender As Object, e As RoutedEventArgs)
        'uiFlyout.IsOpen = False
        Dim oWnd As New ShowExifs(False) '(_picek.oPic)
        oWnd.DataContext = _picek
        oWnd.Show()
    End Sub

    Private Sub uiOnMap_Click(sender As Object, e As RoutedEventArgs)
        Dim oGps As BasicGeopos = _picek.oPic.GetGeoTag
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
        SaveMetaData()
    End Sub

    Private Sub SaveMetaData()
        Dim oBrowserWnd As ProcessBrowse = Me.Owner
        If oBrowserWnd Is Nothing Then Return

        oBrowserWnd.SaveMetaData()
    End Sub

    Private Sub ChangePicture(bGoBack As Boolean, bShifty As Boolean)
        Dim oBrowserWnd As ProcessBrowse = Me.Owner
        If oBrowserWnd Is Nothing Then Return

        Dim picek As ProcessBrowse.ThumbPicek = oBrowserWnd.FromBig_Next(_picek, bGoBack, _inSlideShow)
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
                ' to okno
                _picek = picek
                Window_Loaded(Nothing, Nothing)
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

        Select Case e.Key
            Case Key.Space, Key.PageDown
                ChangePicture(False, bShifty)
            Case Key.PageUp
                ChangePicture(True, bShifty)
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
            If Not Await vb14.DialogBoxYNAsync($"Skasować zdjęcie ({_picek.oPic.sSuggestedFilename})?") Then Return
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

                Dim bSave As Boolean = Await vb14.DialogBoxYNAsync("Jest już edycja, zapisać zmiany?")

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
                vb14.DialogBox("Błąd zapisu zmian")
            End Try


        End Using

        IO.File.Delete(_picek.oPic.InBufferPathName & ProcessBrowse.THUMB_SUFIX) ' no exception if not found

        _picek.oImageSrc = Await ProcessBrowse.WczytajObrazek(_picek.oPic.InBufferPathName, 400, Rotation.Rotate0)

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
            vb14.DialogBox("Nie istnieje plik backup?" & vbCrLf & $"({sBakFileName})")
            Return
        End If

        IO.File.Delete(sJpgFilename)
        IO.File.Move(sBakFileName, sJpgFilename)
        IO.File.SetCreationTime(sJpgFilename, IO.File.GetLastWriteTime(sJpgFilename))

        ' no i przerysowujemy wszystko
        Window_Loaded(Nothing, Nothing)

    End Sub

    Private Sub uiIkonkaTypu_Click(sender As Object, e As RoutedEventArgs)
        ' kliknięcie na typie, znaczy "*" lub ">"

        Select Case uiIkonkaTypu.Content
            Case "*" ' plik NAR
                ' otwórz trzy okienka? (po jednym na każdy JPG w NAR, z zablokowaniem DELETE itp.)? Ale mógłby być "wybierator", że wybieramy jeden JPG i zamieniamy buffer.OnePic(NAR) na (JPG)
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
        End Select

        Return
    End Sub

    Private Sub uiMovie_Ended(sender As Object, e As RoutedEventArgs)
        uiMovie.Visibility = Visibility.Collapsed
        uiFullPicture.Visibility = Visibility.Visible
        uiIkonkaTypu.Visibility = Visibility.Visible
        uiIkonkaTypu.Content = "►"
    End Sub

    Private Sub uiGoWiki_Click(sender As Object, e As RoutedEventArgs)
        OpenWikiForMonth(_picek.oPic)
    End Sub

    Public Shared Sub OpenWikiForMonth(oPic As Vblib.OnePic)
        Dim data As Date = oPic.GetMostProbablyDate

        ' https://en.wikipedia.org/wiki/January_1970
        Dim sLink As String = data.ToString("MMMM_yyyy", System.Globalization.CultureInfo.InvariantCulture)
        sLink = "https://en.wikipedia.org/wiki/" & sLink

        pkar.OpenBrowser(sLink)

    End Sub

    Private Sub uiSlideshow_Click(sender As Object, e As RoutedEventArgs)
        vb14.DialogBox("jeszcze nie umiem stąd zrobić")
    End Sub

    Private Async Sub uiFlipHoriz_Click(sender As Object, e As RoutedEventArgs)
        Await SprawdzCzyJestEdycja(EditModeEnum.flip)
        If _editMode <> EditModeEnum.flip Then Return

        uiSave_Click(sender, e)

    End Sub


#End Region

End Class

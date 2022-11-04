
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

    Private Async Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        Me.Title = _picek.sDymek
        _bitmap = Await ProcessBrowse.SkalujObrazek(_picek.oPic.InBufferPathName, 0)
        uiFullPicture.Source = _bitmap

        ' skalowanie okna
        Dim scrWidth As Double = SystemParameters.FullPrimaryScreenWidth * 0.9
        Dim scrHeight As Double = SystemParameters.FullPrimaryScreenHeight * 0.9

        If scrHeight > _bitmap.PixelHeight AndAlso scrWidth > _bitmap.PixelWidth Then
            Me.Height = _bitmap.PixelHeight + 60
            Me.Width = _bitmap.PixelWidth + 10
        Else
            Me.Height = scrHeight
            Me.Width = scrWidth
            Dim dScaleX As Double = _bitmap.PixelWidth / scrWidth
            Dim dScaleY As Double = _bitmap.PixelHeight / scrHeight
            Dim dDesiredScale As Double = Math.Max(dScaleX, dScaleY)
            uiFullPicture.Width = _bitmap.PixelWidth / dDesiredScale
            uiFullPicture.Height = _bitmap.PixelHeight / dDesiredScale
        End If

        ProcessBrowse.WypelnMenuAutotagerami(uiMenuTaggers, AddressOf ApplyTagger)

        OnOffMap()

        ' MenuAutoTaggerow()

    End Sub

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

        _picek.oPic.Exifs.Add(Await oSrc.GetForFile(_picek.oPic))
        _picek.oPic.TagsChanged = True
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

        Dim sUri As New Uri("https://www.openstreetmap.org/#map=16/" & oGps.Latitude & "/" & oGps.Longitude)
        sUri.OpenBrowser
    End Sub

    Private Sub uiDescribe_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New AddDescription(_picek.oPic)
        If Not oWnd.ShowDialog Then Return

        Dim oDesc As Vblib.OneDescription = oWnd.GetDescription
        _picek.oPic.AddDescription(oDesc)
    End Sub
End Class

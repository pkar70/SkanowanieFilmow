
Imports pkar

Public Class PicMenuGeotag
    Inherits PicMenuBase

    Private Shared _clip As BasicGeoposWithRadius
    Private _itemPaste As New MenuItem

    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If _wasApplied Then Return

        MyBase.OnApplyTemplate()

        If Not InitEnableDisable("Geotags") Then Return

        Me.Items.Clear()

        Dim oNew As New MenuItem
        oNew.Header = "Create Geotag"
        AddHandler oNew.Click, AddressOf uiCreateGeotag_Click
        Me.Items.Add(oNew)


        oNew = New MenuItem
            oNew.Header = "Copy Geotag"
        oNew.IsEnabled = Not UseSelectedItems   ' COPY nie ma sensu na grupie
        AddHandler oNew.Click, AddressOf uiGeotagToClip_Click
            Me.Items.Add(oNew)

        _itemPaste.Header = "Paste Geotag"
        _itemPaste.IsEnabled = _clip IsNot Nothing
        AddHandler _itemPaste.Click, AddressOf uiGeotagPaste_Click
        Me.Items.Add(_itemPaste)

        oNew = New MenuItem
        oNew.Header = "Reset geotag"
        AddHandler oNew.Click, AddressOf uiGeotagClear_Click
        oNew.IsEnabled = _picek.GetExifOfType(Vblib.ExifSource.ManualGeo) IsNot Nothing
        Me.Items.Add(oNew)


        _wasApplied = True
    End Sub

    Private Sub GeotagClear(oPic As Vblib.OnePic)
        oPic.RemoveExifOfType(Vblib.ExifSource.ManualGeo)
        oPic.RemoveExifOfType(Vblib.ExifSource.AutoOSM)
        oPic.RemoveExifOfType(Vblib.ExifSource.AutoImgw)
    End Sub

    Private Sub uiGeotagClear_Click(sender As Object, e As RoutedEventArgs)
        OneOrMany(AddressOf GeotagClear)
        EventRaise(Me)
    End Sub

    Private Sub uiGeotagToClip_Click(sender As Object, e As RoutedEventArgs)

        Dim oGeo As BasicGeoposWithRadius = _picek.GetGeoTag
        If oGeo Is Nothing Then
            Vblib.DialogBox("Zaznaczone zdjęcie nie ma GeoTag")
            Return
        End If

        _clip = oGeo
        _itemPaste.IsEnabled = True

    End Sub

    Private Sub GeotagSet(oPic As Vblib.OnePic)
        _picek.ReplaceOrAddExif(_exifGeoToPaste)
        _picek.RemoveExifOfType(Vblib.ExifSource.AutoOSM)
        _picek.RemoveExifOfType(Vblib.ExifSource.AutoImgw)
    End Sub

    ' dla Paste oraz Create, żeby wyliczać go tylko raz
    Private _exifGeoToPaste As Vblib.ExifTag

    Private Sub uiGeotagPaste_Click(sender As Object, e As RoutedEventArgs)
        _exifGeoToPaste = New Vblib.ExifTag(Vblib.ExifSource.ManualGeo)
        _exifGeoToPaste.GeoTag = _clip
        _exifGeoToPaste.GeoZgrubne = _clip.Radius > 100  ' Exif zawiera BasicGeotag, bez radiusa, więc tutaj trzeba konwersję zrobić

        OneOrMany(AddressOf GeotagSet)
        EventRaise(Me)
    End Sub


    Private Sub uiCreateGeotag_Click(sender As Object, e As RoutedEventArgs)

        Dim oWnd As New EnterGeoTag
        If Not oWnd.ShowDialog Then Return

        _exifGeoToPaste = New Vblib.ExifTag(Vblib.ExifSource.ManualGeo)
        _exifGeoToPaste.GeoTag = oWnd.GetGeoPos
        _exifGeoToPaste.GeoZgrubne = oWnd.IsZgrubne

        OneOrMany(AddressOf GeotagSet)
        EventRaise(Me)

    End Sub

End Class


'Imports PicSorterNS.ProcessBrowse
Imports pkar

Public NotInheritable Class PicMenuGeotag
    Inherits PicMenuBase

    Public Overrides Property ChangeMetadata As Boolean = True

    Private Shared _clip As BasicGeoposWithRadius
    Private Shared _itemReset As MenuItem
    Private Shared _miCopy As MenuItem
    Private Shared _miPaste As MenuItem
    Private Shared _miCreate As MenuItem
    Private Shared _miMakeSame As MenuItem

    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If Not String.IsNullOrWhiteSpace(Me.Header) Then Return

        MyBase.OnApplyTemplate()

        If Not InitEnableDisable("Geotags", "Operacje na danych geograficznych", True) Then Return

        Me.Items.Clear()

        AddMenuItem("Create Geotag", "Tworzenie znacznika z lokalizacją", AddressOf uiCreateGeotag_Click)

        AddMenuItem("Make same", "Skopiowanie geografii pomiędzy zdjęciami", AddressOf uiGeotagMakeSame_Click, UseSelectedItems)

        _miCopy = AddMenuItem("Copy Geotag", "Skopiuj dane geograficzne ze wskazanego zdjęcia do lokalnego schowka ", AddressOf CopyCalled)
        _miPaste = AddMenuItem("Paste Geotag", "ustaw dane geograficzne zdjęć wedle lokalnego schowka", AddressOf uiGeotagPaste_Click, False)

        _itemReset = AddMenuItem("Reset Geotag", "Wyczyść dopisane przez program dane geograficzne zdjęcia (ManualGeo, OSM, IMGW)", AddressOf uiGeotagClear_Click)

        MenuOtwieramy()
    End Sub

    Public Overrides Sub MenuOtwieramy()
        MyBase.MenuOtwieramy()

        If _miCopy Is Nothing OrElse _miCreate Is Nothing Then Return
        If _itemReset Is Nothing OrElse _miMakeSame Is Nothing Then Return

        _miCopy.IsEnabled = Not UseSelectedItems AndAlso GetFromDataContext()?.sumOfGeo IsNot Nothing
        _itemReset.IsEnabled = UseSelectedItems OrElse GetFromDataContext()?.GetExifOfType(Vblib.ExifSource.ManualGeo) IsNot Nothing

        _miPaste.IsEnabled = _clip IsNot Nothing

        _miMakeSame.IsEnabled = UseSelectedItems AndAlso GetSelectedItems.Count > 1

        If Not CheckNieMaBlokerow() Then
            _miPaste.IsEnabled = False
            _itemReset.IsEnabled = False
            _miMakeSame.IsEnabled = False
            _miCreate.IsEnabled = False
        End If
    End Sub


    Private Sub uiGeotagMakeSame_Click(sender As Object, e As RoutedEventArgs)
        ' tu wejdzie tylko przy UseSelectedItems

        If GetSelectedItems.Count < 2 Then
            Vblib.DialogBox("Funkcja kopiowania GeoTag wymaga zaznaczenia przynajmniej dwu zdjęć")
            Return
        End If

        ' step 1: znajdź pierwszy geotag
        Dim oNewGeoTag As New Vblib.ExifTag(Vblib.ExifSource.ManualGeo)
        Dim oExifOSM As Vblib.ExifTag = Nothing
        Dim oExifImgw As Vblib.ExifTag = Nothing

        For Each oItem As ProcessBrowse.ThumbPicek In GetSelectedItems()
            Dim oGeo As BasicGeopos = oItem.oPic.GetGeoTag
            If oGeo Is Nothing Then Continue For
            oNewGeoTag.GeoTag = oGeo
            oExifOSM = oItem.oPic.GetExifOfType(Vblib.ExifSource.AutoOSM)
            oExifImgw = oItem.oPic.GetExifOfType(Vblib.ExifSource.AutoImgw)
        Next
        If oNewGeoTag.GeoTag Is Nothing OrElse oNewGeoTag.GeoTag.IsEmpty Then
            Vblib.DialogBox("Nie mogę znaleźć zdjęcia z GeoTag wśród zaznaczonych")
            Return
        End If

        ' step 2: sprawdź czy wszystkie zaznaczone zdjęcia, jeśl mają geotagi, to z tych samych okolic
        Dim iMaxOdl As Integer = 0
        For Each oItem As ProcessBrowse.ThumbPicek In GetSelectedItems()
            Dim oCurrGeo As BasicGeopos = oItem.oPic.GetGeoTag
            If oCurrGeo IsNot Nothing Then iMaxOdl = Math.Max(iMaxOdl, oNewGeoTag.GeoTag.DistanceTo(oCurrGeo))
        Next

        If iMaxOdl > 1000 Then
            Vblib.DialogBox($"Wybrane zdjęcia mają między sobą odległość {iMaxOdl} metrów")
            Return
        End If

        For Each oItem As ProcessBrowse.ThumbPicek In GetSelectedItems()

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

        Next

        EventRaise(Me)

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

    Private Sub CopyCalled(sender As Object, e As RoutedEventArgs)

        Dim oGeo As BasicGeoposWithRadius = GetFromDataContext.GetGeoTag
        If oGeo Is Nothing Then
            Vblib.DialogBox("Zaznaczone zdjęcie nie ma GeoTag")
            Return
        End If

        _clip = oGeo
        _miPaste.IsEnabled = True
    End Sub

    Private Sub GeotagSet(oPic As Vblib.OnePic)
        If UseSelectedItems Then
            If oPic.GetGeoTag IsNot Nothing Then Return
        End If

        oPic.ReplaceOrAddExif(_exifGeoToPaste)
        oPic.RemoveExifOfType(Vblib.ExifSource.AutoOSM)
        oPic.RemoveExifOfType(Vblib.ExifSource.AutoImgw)
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

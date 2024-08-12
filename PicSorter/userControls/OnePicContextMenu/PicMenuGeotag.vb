
'Imports PicSorterNS.ProcessBrowse
Imports pkar

Public NotInheritable Class PicMenuGeotag
    Inherits PicMenuBase

    Private Shared _clip As BasicGeoposWithRadius
    Private _itemReset As MenuItem

    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If _wasApplied Then Return

        MyBase.OnApplyTemplate()

        If Not InitEnableDisable("Geotags", "Operacje na danych geograficznych", True) Then Return

        Me.Items.Clear()

        Me.Items.Add(NewMenuItem("Create Geotag", "Tworzenie znacznika z lokalizacją", AddressOf uiCreateGeotag_Click))
        Me.Items.Add(NewMenuItem("Make same", "Skopiowanie geografii pomiędzy zdjęciami", AddressOf uiGeotagMakeSame_Click, UseSelectedItems))

        AddCopyMenu("Copy Geotag", "Skopiuj dane geograficzne ze wskazanego zdjęcia do lokalnego schowka ")
        AddPasteMenu("Paste Geotag", "ustaw dane geograficzne zdjęć wedle lokalnego schowka")

        _itemReset = NewMenuItem("Reset Geotag", "Wyczyść dopisane przez program dane geograficzne zdjęcia (ManualGeo, OSM, IMGW)", AddressOf uiGeotagClear_Click)
        Me.Items.Add(_itemReset)

        _wasApplied = True
    End Sub

    Public Overrides Sub MenuOtwieramy()
        MyBase.MenuOtwieramy()
        _miCopy.IsEnabled = Not UseSelectedItems AndAlso GetFromDataContext.sumOfGeo IsNot Nothing
        _itemReset.IsEnabled = UseSelectedItems OrElse GetFromDataContext.GetExifOfType(Vblib.ExifSource.ManualGeo) IsNot Nothing
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

    Protected Overrides Sub CopyCalled()
        MyBase.CopyCalled()

        Dim oGeo As BasicGeoposWithRadius = GetFromDataContext.GetGeoTag
        If oGeo Is Nothing Then
            Vblib.DialogBox("Zaznaczone zdjęcie nie ma GeoTag")
            Return
        End If

        _clip = oGeo
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

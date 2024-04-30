

Imports pkar

Public NotInheritable Class PicMenuDeleteTemps
    Inherits PicMenuBase

    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If _wasApplied Then Return

        MyBase.OnApplyTemplate()

        If Not InitEnableDisable("Delete temps", "Usuwanie plików tymczasowych związanych ze zdjęciem") Then Return

        AddHandler Me.Click, AddressOf ActionClick

        _wasApplied = True
    End Sub

    Private Sub ActionClick(sender As Object, e As RoutedEventArgs)
        OneOrMany(Sub(x) x.DeleteAllTempFiles())
    End Sub

End Class


#If False Then
Imports PicSorterNS.ProcessBrowse
Imports Vblib

Public NotInheritable Class PicMenuDeleteOwn
    Inherits PicMenuBase

    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If _wasApplied Then Return

        MyBase.OnApplyTemplate()

        If Not InitEnableDisable("Delete file") Then Return

        Me.Items.Clear()

        Me.Items.Add(NewMenuItem("Picture", AddressOf uiDelPic_Click))

        If UseSelectedItems Then
            Me.Items.Add(NewMenuItem("Thumb", AddressOf uiDelThumb_Click))
        End If

        Me.Items.Add(NewMenuItem("Backup", AddressOf uiDelBack_Click))

        _wasApplied = True
    End Sub

    Private Sub uiDelBack_Click(sender As Object, e As RoutedEventArgs)
        Throw New NotImplementedException()
    End Sub

    Private Async Sub uiDelThumb_Click(sender As Object, e As RoutedEventArgs)

        Dim bCacheThumbs As Boolean = Vblib.GetSettingsBool("uiCacheThumbs")

        ' nie można OneOrMany, bo tam operacje są na OnePic, a tu na Thumb trzeba
        For Each oItem As ThumbPicek In GetSelectedItems()
            Await ProcessBrowse.DoczytajMiniaturke(bCacheThumbs, oItem, True)
        Next
    End Sub

    Private Async Sub uiDelPic_Click(sender As Object, e As RoutedEventArgs)

        If Not Vblib.GetSettingsBool("uiNoDelConfirm") Then
            If UseSelectedItems Then
                If Not Await Vblib.DialogBoxYNAsync($"Skasować zdjęcia? ({GetSelectedItems.Count})") Then Return
            Else
                If Not Await Vblib.DialogBoxYNAsync($"Skasować zdjęcie ({_picek.sSuggestedFilename})?") Then Return
            End If
        End If

        Dim lLista As New List(Of ThumbPicek)
        For Each oItem As ThumbPicek In GetSelectedItems()
            lLista.Add(oItem)
        Next

        DeletePicekMain(oPicek)

    End Sub
    Private Sub DeletePicekMain(oPicek As ThumbPicek)
        _ReapplyAutoSplit = False
        DeletePicture(oPicek)   ' zmieni _Reapply, jeśli picek miał splita

        SaveMetaData()

        ' pokaz na nowo obrazki
        RefreshMiniaturki(_ReapplyAutoSplit)
    End Sub

    Private Sub DeletePicture(oPicek As ThumbPicek)
        If oPicek Is Nothing Then Return

        GC.Collect()    ' zabezpieczenie jakby tu był jeszcze otwarty plik jakiś

        ' usuń z bufora (z listy i z katalogu), ale nie zapisuj indeksu (jakby to była seria kasowania)
        If Not _oBufor.DeleteFile(oPicek.oPic) Then Return   ' nieudane skasowanie

        ' kasujemy różne miniaturki i tak dalej. Delete nie robi Exception jak pliku nie ma.
        IO.File.Delete(oPicek.oPic.InBufferPathName & THUMB_SUFIX)
        IO.File.Delete(oPicek.oPic.InBufferPathName & THUMB_SUFIX & ".png")

        ' zapisz jako plik do kiedyś-tam usunięcia ze źródła
        Application.GetSourcesList.AddToPurgeList(oPicek.oPic.sSourceName, oPicek.oPic.sInSourceID)

        ' przesunięcie "dzielnika" *TODO* bezpośrednio na liscie
        If oPicek.splitBefore Then _ReapplyAutoSplit = True

        ' skasuj z tutejszej listy
        _thumbsy.Remove(oPicek)

    End Sub


    Private Async Sub uiDeleteSelected_Click(sender As Object, e As RoutedEventArgs)
        uiActionsPopup.IsOpen = False

        ' delete selected
        If uiPicList.SelectedItems Is Nothing Then Return

        Dim lLista As New List(Of ThumbPicek)
        For Each oItem As ThumbPicek In uiPicList.SelectedItems
            lLista.Add(oItem)
        Next

        If Not vb14.GetSettingsBool("uiNoDelConfirm") Then
            If Not Await vb14.DialogBoxYNAsync($"Skasować zdjęcia? ({lLista.Count})") Then Return
        End If

        _ReapplyAutoSplit = False

        For Each oItem As ThumbPicek In lLista
            DeletePicture(oItem)
        Next

        SaveMetaData()    ' tylko raz, po całej serii kasowania

        ' pokaz na nowo obrazki
        RefreshMiniaturki(_ReapplyAutoSplit)
    End Sub

End Class

#End If
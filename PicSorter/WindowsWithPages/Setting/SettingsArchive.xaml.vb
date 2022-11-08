

Imports vb14 = Vblib.pkarlibmodule14

' pierwszy był Sources, to jest jego przeróbka

Class SettingsArchive

    Private _item As Vblib.LocalStorage

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        ShowSourcesList()
    End Sub
    Private Sub ShowSourcesList()
        uiLista.ItemsSource = Application.GetArchivesList().GetList
    End Sub

    Private Sub uiAddSource_Click(sender As Object, e As RoutedEventArgs)
        Dim oNewSrc As New Vblib.LocalStorage
        oNewSrc.StorageName = "(" & DateTime.Now.ToString("yy-MM-dd") & ")"
        oNewSrc.enabled = False
        Application.GetArchivesList.Add(oNewSrc)
        PokazDoEdycji(oNewSrc)
    End Sub

    Private Sub uiEdit_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oItem As Vblib.LocalStorage = oFE?.DataContext

        PokazDoEdycji(oItem)
    End Sub

    Private Async Sub uiDel_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oItem As Vblib.LocalStorage = oFE?.DataContext

        If Not Await vb14.DialogBoxYNAsync("Na pewno usunąć, a nie tylko zablokować?") Then Return

        Application.GetArchivesList().Remove(oItem)

    End Sub

    Private Sub uiOk_Click(sender As Object, e As RoutedEventArgs)
        Application.GetArchivesList().Save()
        Me.NavigationService.GoBack()
    End Sub


    Private Sub ComboVolLabels(sCurrentVolLabel As String)

        If String.IsNullOrWhiteSpace(sCurrentVolLabel) Then sCurrentVolLabel = "#####"  ' taka nie wystąpi

        Dim iInd As Integer = sCurrentVolLabel.IndexOf("(")
        If iInd > 1 Then sCurrentVolLabel = sCurrentVolLabel.Substring(0, iInd - 1)
        sCurrentVolLabel = sCurrentVolLabel.ToLowerInvariant

        uiSrcVolume.Items.Clear()
        iInd = -1   ' no vollabel to select

        ' dyski
        Dim oDrives = IO.DriveInfo.GetDrives()
        For Each oDrive As IO.DriveInfo In oDrives
            If oDrive.IsReady Then
                iInd = uiSrcVolume.Items.Add(oDrive.VolumeLabel & " (" & oDrive.Name & ")")
                If oDrive.VolumeLabel.ToLowerInvariant = sCurrentVolLabel Then
                    uiSrcVolume.SelectedIndex = iInd
                End If
            End If
        Next

    End Sub

    Private Sub PokazDoEdycji(oItem As Vblib.LocalStorage)
        uiOK.IsEnabled = False
        uiEditSource.Visibility = Visibility.Visible
        _item = oItem

        uiSrcName.Text = _item.StorageName

        ComboVolLabels(_item.VolLabel)

        uiSrcPath.Text = _item.Path

        uiSrcInclude.Text = _item.includeMask
        uiSrcExclude.Text = _item.excludeMask

        uiSrcLastSave.Text = "-"
        If _item.lastSave > New Date(2000, 1, 1) Then uiSrcLastSave.Text = _item.lastSave.ToString("yyyy-MM-dd HH:mm")

        uiTree0Dekada.IsChecked = _item.tree0Dekada
        uiTree1Rok.IsChecked = _item.tree1Rok
        uiTree2Miesiac.IsChecked = _item.tree2Miesiac
        uiTree3Dzien.IsChecked = _item.tree3Dzien
        uiTree3DzienWeekDay.IsChecked = _item.tree3DzienWeekDay
        uiTree4geo.IsChecked = _item.tree4Geo

        uiSrcSaveToExif.IsChecked = _item.saveToExif
        uiSrcJSONinside.IsChecked = _item.jsonInDir

    End Sub

    Private Sub uiSrcBrowse_Click(sender As Object, e As RoutedEventArgs)

        Dim sVolLabel As String = uiSrcVolume.SelectedValue
        If String.IsNullOrWhiteSpace(sVolLabel) Then Return
        Dim iInd As Integer = sVolLabel.IndexOf("(")
        If iInd > 0 Then sVolLabel = sVolLabel.Substring(0, iInd - 1).Trim

        Dim sPath As String = uiSrcPath.Text
        If String.IsNullOrWhiteSpace(sPath) Then sPath = "" ' nie chcemy NULLa

        sPath = VbLib20.PicSourceImplement.GetConvertedPathForVol_Folder(sVolLabel, sPath)
        SettingsGlobal.FolderBrowser(uiSrcPath, sPath)

    End Sub

    Private Sub uiEditOk_Click(sender As Object, e As RoutedEventArgs)
        uiOK.IsEnabled = True
        uiEditSource.Visibility = Visibility.Hidden

        ' nazwa nie moze sie pokryć
        If uiSrcName.Name <> _item.StorageName Then
            For Each oItem In Application.GetSourcesList().GetList
                If oItem.SourceName.ToLowerInvariant = uiSrcName.Name.ToLowerInvariant Then
                    vb14.DialogBox("Archiwum o takiej nazwie już istnieje")
                    Return
                End If
            Next
        End If

        _item.StorageName = uiSrcName.Text
        _item.VolLabel = uiSrcVolume.SelectedValue
        _item.Path = uiSrcPath.Text

        _item.includeMask = uiSrcInclude.Text
        _item.excludeMask = uiSrcExclude.Text

        _item.tree0Dekada = uiTree0Dekada.IsChecked
        _item.tree1Rok = uiTree1Rok.IsChecked
        _item.tree2Miesiac = uiTree2Miesiac.IsChecked
        _item.tree3Dzien = uiTree3Dzien.IsChecked
        _item.tree3DzienWeekDay = uiTree3DzienWeekDay.IsChecked
        _item.tree4Geo = uiTree4geo.IsChecked

        _item.saveToExif = uiSrcSaveToExif.IsChecked
        _item.jsonInDir = uiSrcJSONinside.IsChecked

    End Sub





End Class

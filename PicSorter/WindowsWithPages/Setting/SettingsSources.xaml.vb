
' edycja listy źródeł oraz ich parametrów
' *TODO* defaultPublish być może dodać

Imports vb14 = Vblib.pkarlibmodule14


Class SettingsSources

    Private _item As Vblib.PicSourceBase

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        ShowSourcesList()
        WypelnMenuTypyZrodel()
    End Sub

    Private Sub uiAddSource_Click(sender As Object, e As RoutedEventArgs)
        uiAddSourcePopup.IsOpen = Not uiAddSourcePopup.IsOpen
    End Sub

    Private Sub uiEdit_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oItem As Vblib.PicSourceBase = oFE?.DataContext

        PokazDoEdycji(oItem)
    End Sub

    Private Async Sub uiDel_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oItem As Vblib.PicSourceBase = oFE?.DataContext

        If Not Await vb14.DialogBoxYNAsync("Na pewno usunąć, a nie tylko zablokować?") Then Return

        Application.GetSourcesList().Remove(oItem)
        ShowSourcesList()
    End Sub

    Private Sub uiOk_Click(sender As Object, e As RoutedEventArgs)
        Application.GetSourcesList().Save()
        Me.NavigationService.GoBack()
    End Sub

    Private Sub ShowSourcesList()
        uiLista.ItemsSource = Nothing
        uiLista.ItemsSource = Application.GetSourcesList().GetList
    End Sub


    Private Function StworzMenuItemTypu(sTyp As String)
        Dim oNew As New MenuItem
        oNew.Header = " " & sTyp & " "
        oNew.Margin = New Thickness(2)

        AddHandler oNew.Click, AddressOf DodajTenTypZrodla

        Return oNew
    End Function

    Private Sub WypelnMenuTypyZrodel()
        uiMenuSourcesTypes.Items.Clear()

        uiMenuSourcesTypes.Items.Add(StworzMenuItemTypu("FOLDER"))
        uiMenuSourcesTypes.Items.Add(StworzMenuItemTypu("MTP"))

        ' ADHOC może być dodany tylko raz
        For Each oSource As Vblib.PicSourceBase In Application.GetSourcesList().GetList
            If oSource.Typ = Vblib.PicSourceType.AdHOC Then Return
        Next

        uiMenuSourcesTypes.Items.Add(StworzMenuItemTypu("ADHOC"))

    End Sub
    Private Sub DodajTenTypZrodla(sender As Object, e As RoutedEventArgs)
        uiAddSourcePopup.IsOpen = False

        Dim oNewSrc As Vblib.PicSourceBase
        Dim oMI As MenuItem = sender
        Select Case oMI.Header.ToString.Trim.ToLowerInvariant
            Case "folder"
                oNewSrc = New VbLib20.PicSourceImplement(Vblib.PicSourceType.FOLDER, Application.GetDataFolder)
            Case "mtp"
                oNewSrc = New VbLib20.PicSourceImplement(Vblib.PicSourceType.MTP, Application.GetDataFolder)
            Case "adhoc"
                oNewSrc = New VbLib20.PicSourceImplement(Vblib.PicSourceType.AdHOC, Application.GetDataFolder)
            Case Else
                Return
        End Select

        If oNewSrc.Typ = Vblib.PicSourceType.AdHOC Then
            oNewSrc.SourceName = "AdHoc"
        Else
            oNewSrc.SourceName = "(" & DateTime.Now.ToString("yy-MM-dd") & ")"
        End If

        oNewSrc.enabled = False
        oNewSrc.defaultExif = New Vblib.ExifTag(Vblib.ExifSource.SourceDefault)

        Application.GetSourcesList().Add(oNewSrc)
        PokazDoEdycji(oNewSrc)

    End Sub

    Private Sub ComboVolLabels(iSrcType As Vblib.PicSourceType, sCurrentVolLabel As String)

        If String.IsNullOrWhiteSpace(sCurrentVolLabel) Then sCurrentVolLabel = "#####"  ' taka nie wystąpi

        'vb14.DialogBox($"ComboVolLabels (...,{sCurrentVolLabel})")

        Dim iInd As Integer = sCurrentVolLabel.IndexOf("(")
        If iInd > 1 Then sCurrentVolLabel = sCurrentVolLabel.Substring(0, iInd - 1)
        sCurrentVolLabel = sCurrentVolLabel.ToLowerInvariant

        uiSrcBrowse.IsEnabled = True

        uiSrcVolume.Items.Clear()
        iInd = -1   ' no vollabel to select

        If iSrcType = Vblib.PicSourceType.MTP Then
            ' telefony

            Dim lLista As List(Of String) = MediaDevicesLib.Helper.GetDevicesList
            If lLista.Count = 0 And sCurrentVolLabel <> "#####" Then
                iInd = uiSrcVolume.Items.Add(sCurrentVolLabel)
                uiSrcVolume.SelectedIndex = iInd

                uiSrcVolume.IsEnabled = False
                uiSrcBrowse.IsEnabled = False
            Else
                For Each sDevice As String In lLista
                    iInd = uiSrcVolume.Items.Add(sDevice)
                    If sDevice.ToLower.StartsWith(sCurrentVolLabel & " (") Then
                        uiSrcVolume.SelectedIndex = iInd
                    End If
                Next
            End If
        Else
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

        End If

    End Sub

    Private Sub PokazDoEdycji(oItem As Vblib.PicSourceBase)
        uiOK.IsEnabled = False
        uiEditSource.Visibility = Visibility.Visible
        _item = oItem

        uiSrcType.Text = _item.Typ.ToString
        uiSrcName.Text = _item.SourceName

        ComboVolLabels(oItem.Typ, _item.VolLabel)

        uiSrcPath.Text = _item.Path

        uiSrcRecursive.IsChecked = _item.Recursive

        If oItem.Typ = Vblib.PicSourceType.AdHOC Then
            uiSrcPurge.IsEnabled = False
            uiSrcPurge.Text = "---"
        Else
            uiSrcPurge.IsEnabled = True
            uiSrcPurge.Text = _item.sourcePurgeDelay.TotalDays
        End If

        '<!--Public Property defaultPublish As List(Of String)   ' lista IDs-->

        uiSrcInclude.Text = _item.includeMask
        uiSrcExclude.Text = _item.excludeMask

        uiSrcLastDownload.Text = "-"
        If _item.lastDownload > New Date(2000, 1, 1) Then uiSrcLastDownload.Text = _item.lastDownload.ToString("yyyy-MM-dd HH:mm")

        ' te są bez sensu dla ADHOC, więc ukrywamy
        If _item.Typ = Vblib.PicSourceType.AdHoc Then
            uiSrcVolume.IsEnabled = False
            uiSrcPath.IsEnabled = False
            uiSrcBrowse.IsEnabled = False
        Else
            uiSrcVolume.IsEnabled = True
            uiSrcPath.IsEnabled = True
            uiSrcBrowse.IsEnabled = True
        End If

    End Sub

    Private Sub uiSrcBrowse_Click(sender As Object, e As RoutedEventArgs)

        Dim sVolLabel As String = uiSrcVolume.SelectedValue
        If String.IsNullOrWhiteSpace(sVolLabel) Then Return
        Dim iInd As Integer = sVolLabel.IndexOf("(")
        If iInd > 0 Then sVolLabel = sVolLabel.Substring(0, iInd - 1).Trim

        Dim sPath As String = uiSrcPath.Text
        If String.IsNullOrWhiteSpace(sPath) Then sPath = "" ' nie chcemy NULLa

        Select Case _item.Typ
            Case Vblib.PicSourceType.Folder
                sPath = VbLib20.PicSourceImplement.GetConvertedPathForVol_Folder(sVolLabel, sPath)
                SettingsGlobal.FolderBrowser(uiSrcPath, sPath, "Wskaz folder na archiwum")
            Case Vblib.PicSourceType.MTP
                Dim oWnd As New BrowseMtpDevice(sVolLabel, sPath)
                oWnd.ShowDialog()
                uiSrcPath.Text = oWnd.currentPath
            Case Vblib.PicSourceType.AdHOC
                Return  ' nie ma browser dla AdHoc
        End Select

    End Sub

    Private Sub uiEditExif_Click(sender As Object, e As RoutedEventArgs)
        uiOpenExif.IsEnabled = False
        Dim oWnd As New EditExifTag(_item.defaultExif, uiSrcName.Text, EditExifTagScope.LimitedToSourceDir, False)
        oWnd.ShowDialog()
        uiOpenExif.IsEnabled = True
    End Sub

    Private Sub uiEditOk_Click(sender As Object, e As RoutedEventArgs)
        uiOK.IsEnabled = True
        uiEditSource.Visibility = Visibility.Hidden

        ' nazwa nie moze sie pokryć
        If uiSrcName.Text <> _item.SourceName Then
            For Each oItem In Application.GetSourcesList().GetList
                If oItem.SourceName.ToLowerInvariant = uiSrcName.Text.ToLowerInvariant Then
                    vb14.DialogBox("Źródło o takiej nazwie już istnieje")
                    Return
                End If
            Next
        End If

        Dim dPurgeDelay As Double
        If _item.Typ = Vblib.PicSourceType.AdHOC Then
            dPurgeDelay = 9999
        Else
            Try
                dPurgeDelay = uiSrcPurge.Text
            Catch ex As Exception
                vb14.DialogBox("Niepoprawna liczba (purge delay)")
                Return
            End Try
        End If

        ' disabled: gdy edycja MTP, którego nie ma podpiętego
        If uiSrcVolume.IsEnabled Then
            If _item.Typ <> Vblib.PicSourceType.AdHOC Then
                ' przy AdHoc to jest null
                _item.VolLabel = uiSrcVolume.SelectedValue
                If _item.VolLabel.Length < 2 Then
                    vb14.DialogBox("Błędny vollabel")
                    Return
                End If
            End If
        End If

        _item.SourceName = uiSrcName.Text
        _item.Path = uiSrcPath.Text
        _item.Recursive = uiSrcRecursive.IsChecked

        _item.sourcePurgeDelay = TimeSpan.FromDays(dPurgeDelay)

        '<!--Public Property defaultPublish As List(Of String)   ' lista IDs-->

        _item.includeMask = uiSrcInclude.Text
        _item.excludeMask = uiSrcExclude.Text


        ShowSourcesList()
    End Sub





End Class

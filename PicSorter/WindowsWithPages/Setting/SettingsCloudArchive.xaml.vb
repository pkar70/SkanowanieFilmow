
Imports vb14 = Vblib.pkarlibmodule14
Imports Vblib.Extensions
Imports pkar.DotNetExtensions

Class SettingsCloudArchive

    ' pierwszy był Sources, to jest jego przeróbka

    Private _item As Vblib.CloudConfig


    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        ShowSourcesList()
        WypelnMenuTypyZrodel()
    End Sub

    Private Sub uiAddSource_Click(sender As Object, e As RoutedEventArgs)
        uiAddSourcePopup.IsOpen = Not uiAddSourcePopup.IsOpen
    End Sub

    Private Sub uiEdit_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oItem As Vblib.CloudArchive = oFE?.DataContext

        PokazDoEdycji(oItem.konfiguracja)
    End Sub

    Private Async Sub uiDel_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oItem As Vblib.CloudArchive = oFE?.DataContext

        If Not Await vb14.DialogBoxYNAsync("Na pewno usunąć, a nie tylko zablokować?") Then Return

        Application.GetCloudArchives.Remove(oItem)
        ShowSourcesList()
    End Sub

    Private Sub uiOk_Click(sender As Object, e As RoutedEventArgs)
        Application.GetCloudArchives().Save()
        Me.NavigationService.GoBack()
    End Sub

    Private Sub ShowSourcesList()
        uiLista.ItemsSource = Nothing
        uiLista.ItemsSource = Application.GetCloudArchives.GetList
    End Sub


    Private Function StworzMenuItemTypu(oEngine As Vblib.CloudArchive) As MenuItem
        Dim oNew As New MenuItem
        oNew.Header = oEngine.sProvider
        oNew.Margin = New Thickness(2)
        oNew.DataContext = oEngine

        AddHandler oNew.Click, AddressOf DodajTenTypZrodla

        Return oNew
    End Function

    Private Sub WypelnMenuTypyZrodel()
        uiMenuCloudProviders.Items.Clear()

        For Each oItem As Vblib.CloudArchive In Application.GetCloudArchives.GetProvidersList
            uiMenuCloudProviders.Items.Add(StworzMenuItemTypu(oItem))
        Next

    End Sub


    Private Sub DodajTenTypZrodla(sender As Object, e As RoutedEventArgs)
        uiAddSourcePopup.IsOpen = False

        Dim oFE As FrameworkElement = sender
        Dim oEngine As Vblib.CloudArchive = oFE?.DataContext
        If oEngine Is Nothing Then Return

        _item = New Vblib.CloudConfig
        _item.sProvider = oEngine.sProvider
        _item.eTyp = oEngine.eTyp

        _item.nazwa = "(" & DateTime.Now.ToString("yy-MM-dd") & ")"

        _item.enabled = False
        _item.defaultExif = New Vblib.ExifTag(Vblib.ExifSource.CloudPublish)

        _item.processLikes = uiProcessLikes.IsChecked

        Application.GetCloudArchives.Add(_item)

        PokazDoEdycji(_item)

    End Sub

    Private Sub PokazDoEdycji(oItem As Vblib.CloudConfig)
        uiOK.IsEnabled = False
        uiEditSource.Visibility = Visibility.Visible
        _item = oItem

        uiSrcType.Text = _item.sProvider
        uiSrcName.Text = _item.nazwa
        uiPostprocessUC.Pipeline = _item.defaultPostprocess

        uiProcessLikes.IsEnabled = True

        uiSrcPassword.Text = _item.sPswd
        uiSrcUsername.Text = _item.sUsername
        uiSrcAdditInfo.Text = _item.additInfo

        'afterTagChangeBehaviour As AfterChangeBehaviour = AfterChangeBehaviour.ignore
        'afterPicChangeBehaviour As AfterChangeBehaviour = AfterChangeBehaviour.ignore

        uiSrcInclude.Text = _item.includeMask
        uiSrcExclude.Text = _item.excludeMask

        uiProcessLikes.IsChecked = _item.processLikes

        uiSrcLastSave.Text = "-"
        If _item.lastSave.IsDateValid Then uiSrcLastSave.Text = _item.lastSave.ToString("yyyy-MM-dd HH:mm")

    End Sub

    Private Sub uiEditExif_Click(sender As Object, e As RoutedEventArgs)
        uiOpenExif.IsEnabled = False
        Dim oWnd As New EditExifTag(_item.defaultExif, uiSrcName.Text, EditExifTagScope.LimitedToCloudPublish, False)
        oWnd.ShowDialog()
        uiOpenExif.IsEnabled = True
    End Sub

    Private Sub uiEditOk_Click(sender As Object, e As RoutedEventArgs)
        uiOK.IsEnabled = True
        uiEditSource.Visibility = Visibility.Hidden

        ' nazwa nie moze sie pokryć
        If uiSrcName.Text <> _item.nazwa Then
            For Each oItem In Application.GetCloudArchives.GetList
                If oItem.konfiguracja.nazwa.EqualsCIAI(uiSrcName.Text) Then
                    vb14.DialogBox("Już istnieje miejsce archiwizacji z taką nazwą")
                    Return
                End If
            Next
        End If


        _item.nazwa = uiSrcName.Text
        _item.defaultPostprocess = uiPostprocessUC.Pipeline

        _item.sUsername = uiSrcUsername.Text
        _item.sPswd = uiSrcPassword.Text


        'afterTagChangeBehaviour As AfterChangeBehaviour = AfterChangeBehaviour.ignore
        'afterPicChangeBehaviour As AfterChangeBehaviour = AfterChangeBehaviour.ignore

        '<!--Public Property defaultPublish As List(Of String)   ' lista IDs-->

        _item.includeMask = uiSrcInclude.Text
        _item.excludeMask = uiSrcExclude.Text

        _item.additInfo = uiSrcAdditInfo.Text

        ShowSourcesList()
    End Sub

End Class


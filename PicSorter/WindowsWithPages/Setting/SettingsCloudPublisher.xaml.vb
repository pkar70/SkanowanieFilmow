
Imports Vblib
Imports vb14 = Vblib.pkarlibmodule14
Imports pkar.DotNetExtensions
Imports pkar.UI.Extensions

Class SettingsCloudPublisher

    ' pierwszy był Sources, to jest jego przeróbka

    Private _item As Vblib.CloudConfig


    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        Me.InitDialogs

        ShowSourcesList()
        WypelnMenuTypyZrodel()
        'WypelnMenuPeers
    End Sub

    Private Sub uiAddSource_Click(sender As Object, e As RoutedEventArgs)
        uiAddSourcePopup.IsOpen = Not uiAddSourcePopup.IsOpen
    End Sub

    'Private Sub uiAddPostproc_Click(sender As Object, e As RoutedEventArgs)
    '    uiAddPostprocPopup.IsOpen = Not uiAddPostprocPopup.IsOpen
    'End Sub

    Private Sub uiEdit_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oItem As Vblib.CloudPublish = oFE?.DataContext

        PokazDoEdycji(oItem.konfiguracja)
    End Sub

    Private Async Sub uiDel_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oItem As Vblib.CloudPublish = oFE?.DataContext

        If Not Await Me.DialogBoxYNAsync("Na pewno usunąć, a nie tylko zablokować?") Then Return

        Application.GetCloudPublishers.Remove(oItem)
        ShowSourcesList()
    End Sub

    Private Sub uiOk_Click(sender As Object, e As RoutedEventArgs)
        Application.GetCloudPublishers().Save()
        Me.NavigationService.GoBack()
    End Sub

    Private Sub ShowSourcesList()
        uiLista.ItemsSource = Nothing
        uiLista.ItemsSource = Application.GetCloudPublishers.GetList
    End Sub


    Private Function StworzMenuItemTypu(oEngine As CloudPublish) As MenuItem
        Dim oNew As New MenuItem
        oNew.Header = oEngine.sProvider
        oNew.Margin = New Thickness(2)
        oNew.DataContext = oEngine

        AddHandler oNew.Click, AddressOf DodajTenTypZrodla

        Return oNew
    End Function

    Private Sub WypelnMenuTypyZrodel()
        uiMenuCloudProviders.Items.Clear()

        For Each oItem As CloudPublish In Application.GetCloudPublishers.GetProvidersList
            uiMenuCloudProviders.Items.Add(StworzMenuItemTypu(oItem))
        Next

    End Sub

#If BEZUSERCONTROLPOSTPROC Then

    Private Function StworzMenuItemPostProcesora(oEngine As Vblib.PostProcBase) As MenuItem
        Dim oNew As New MenuItem
        oNew.Header = oEngine.Nazwa
        oNew.Margin = New Thickness(2)
        oNew.DataContext = oEngine

        AddHandler oNew.Click, AddressOf DodajTenPostproc

        Return oNew
    End Function

    Private Sub DodajTenPostproc(sender As Object, e As RoutedEventArgs)
        Dim oMI As MenuItem = sender
        If oMI Is Nothing Then Return

        If uiPostprocess.Text <> "" Then uiPostprocess.Text &= ";"
        uiPostprocess.Text &= oMI.Header
    End Sub

    Private Sub WypelnMenuPostprocesory()
        uiMenuPostProcessors.Items.Clear()

        For Each oItem As Vblib.PostProcBase In Application.gPostProcesory
            uiMenuPostProcessors.Items.Add(StworzMenuItemPostProcesora(oItem))
        Next

    End Sub

#End If


    Private Sub DodajTenTypZrodla(sender As Object, e As RoutedEventArgs)
        uiAddSourcePopup.IsOpen = False

        Dim oFE As FrameworkElement = sender
        Dim oEngine As CloudPublish = oFE?.DataContext
        If oEngine Is Nothing Then Return

        _item = New CloudConfig
        _item.sProvider = oEngine.sProvider
        _item.eTyp = oEngine.eTyp

        _item.nazwa = "(" & DateTime.Now.ToString("yy-MM-dd") & ")"

        _item.enabled = False
        _item.defaultExif = New Vblib.ExifTag(Vblib.ExifSource.CloudPublish)

        _item.processLikes = uiProcessLikes.IsChecked

        Application.GetCloudPublishers.Add(_item)

        PokazDoEdycji(_item)

    End Sub

    Private Sub PokazDoEdycji(oItem As Vblib.CloudConfig)
        uiOK.IsEnabled = False
        uiEditSource.Visibility = Visibility.Visible
        _item = oItem

        uiSrcType.Text = _item.sProvider
        uiSrcName.Text = _item.nazwa
        uiPostprocessUC.Pipeline = _item.defaultPostprocess

        If oItem.sProvider = Publish_AdHoc.PROVIDERNAME Then
            uiSrcPurge.IsEnabled = False
            uiSrcPurge.Text = "---"
            uiSrcPassword.IsEnabled = False
            uiSrcUsername.IsEnabled = False
            uiProcessLikes.IsEnabled = False
        Else
            uiSrcPurge.IsEnabled = True
            uiSrcPurge.Text = _item.deleteAfterDays
            uiSrcPassword.IsEnabled = True
            uiSrcUsername.IsEnabled = True
            uiProcessLikes.IsEnabled = True
        End If

        uiSrcPassword.Text = _item.sPswd
        uiSrcUsername.Text = _item.sUsername
        uiSrcAdditInfo.Text = _item.additInfo

        'afterTagChangeBehaviour As AfterChangeBehaviour = AfterChangeBehaviour.ignore
        'afterPicChangeBehaviour As AfterChangeBehaviour = AfterChangeBehaviour.ignore

        uiSrcInclude.Text = _item.includeMask
        uiSrcExclude.Text = _item.excludeMask


        uiStereoAnaglyph.IsChecked = _item.stereoAnaglyph
        uiProcessLikes.IsChecked = _item.processLikes

        uiSrcLastSave.Text = "-"
        If _item.lastSave.IsDateValid Then uiSrcLastSave.Text = _item.lastSave.ToString("yyyy-MM-dd HH:mm")


        If _item.MetaOptions Is Nothing Then
            _item.MetaOptions = Vblib.PublishMetadataOptions.GetDefault
        End If
        uiMetaOptions.DataContext = _item.MetaOptions
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
            For Each oItem In Application.GetCloudPublishers.GetList
                If oItem.konfiguracja.nazwa.EqualsCIAI(uiSrcName.Text) Then
                    Me.MsgBox("Już istnieje miejsce publikowania z taką nazwą")
                    Return
                End If
            Next
        End If

        Dim dPurgeDelay As Double
        If _item.sProvider = Publish_AdHoc.PROVIDERNAME Then
            dPurgeDelay = 9999
        Else
            Try
                dPurgeDelay = uiSrcPurge.Text
            Catch ex As Exception
                Me.MsgBox("Niepoprawna liczba (purge delay)")
                Return
            End Try
        End If
        _item.deleteAfterDays = dPurgeDelay

        _item.nazwa = uiSrcName.Text
        _item.defaultPostprocess = uiPostprocessUC.Pipeline

        _item.sUsername = uiSrcUsername.Text
        _item.sPswd = uiSrcPassword.Text
        _item.additInfo = uiSrcAdditInfo.Text

        'afterTagChangeBehaviour As AfterChangeBehaviour = AfterChangeBehaviour.ignore
        'afterPicChangeBehaviour As AfterChangeBehaviour = AfterChangeBehaviour.ignore

        '<!--Public Property defaultPublish As List(Of String)   ' lista IDs-->

        _item.includeMask = uiSrcInclude.Text
        _item.excludeMask = uiSrcExclude.Text
        _item.stereoAnaglyph = uiStereoAnaglyph.IsChecked

        _item.MetaOptions = uiMetaOptions.DataContext

        ShowSourcesList()
    End Sub


End Class

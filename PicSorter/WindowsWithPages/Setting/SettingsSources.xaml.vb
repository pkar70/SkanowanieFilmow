
' edycja listy źródeł oraz ich parametrów
'Public MustOverride ReadOnly Property Type As String  ' MTP, folder, AdHoc
'Public MustOverride ReadOnly Property Name As String  ' c:\xxxx, MTP\Lumia435, MTP\Lumia650 - per instance
'Public Property Path As String  ' znaczenie zmienne w zależności od Type
'Public Property Recursive As Boolean
'Public Property sourceRemoveDelay As TimeSpan
'Public Property defaultTags As ExifTag
'Public Property defaultPublish As List(Of String)   ' lista IDs
'Public Property include As List(Of String)  ' maski regexp
'Public Property exclude As List(Of String)  ' maski regexp
'Public Property lastDownload As DateTime
'Public Property enabled As Boolean = True

' lista: ENABLED TYPE NAME [edit] [delete]

Class SettingsSources

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        ShowData()
    End Sub

    Private Sub uiOpenExif_Click(sender As Object, e As RoutedEventArgs)
        ' *TODO* tylko tymczasowo, do testowania
        uiOpenExif.IsEnabled = False
        Dim oWnd As New EditExifTag(New Vblib.ExifTag("SOURCE_DIR"), "testsource", EditExifTagScope.LimitedToSourceDir, False)
        oWnd.ShowDialog()
        uiOpenExif.IsEnabled = True
    End Sub

    Private Sub uiAddSource_Click(sender As Object, e As RoutedEventArgs)
        ' *TODO* menu wedle istniejących typów
    End Sub

    Private Sub uiEdit_Click(sender As Object, e As RoutedEventArgs)
        ' *TODO* pokaż do zmiany dane wybranego PicSource
    End Sub

    Private Sub uiDel_Click(sender As Object, e As RoutedEventArgs)
        ' *TODO* usuń PicSource
    End Sub

    Private Sub uiOk_Click(sender As Object, e As RoutedEventArgs)
        Application.GetSourcesList().Save()
    End Sub

    Private Sub ShowData()
        uiLista.ItemsSource = Application.GetSourcesList().GetList
    End Sub

End Class

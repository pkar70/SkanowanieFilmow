
Imports vb14 = Vblib.pkarlibmodule14


Class SettingsKeywords
    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        PrzeliczIpokaz(True)
    End Sub

    Private Sub uiOk_Click(sender As Object, e As RoutedEventArgs)
        Application.GetKeywords.Save(True)
        Me.NavigationService.GoBack()
    End Sub

    Private Sub uiEditKeyword_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oItem As Vblib.OneKeyword = oFE?.DataContext

        If oItem Is Nothing Then Return

        If oItem.sTagId.Length = 1 Then
            vb14.DialogBox("Głównego węzła nie wolno edytować")
            Return
        End If

        Item2Edit(oItem, False)

    End Sub

    Private Sub uiAddSubItem_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oItem As Vblib.OneKeyword = oFE?.DataContext

        If oItem Is Nothing Then Return

        If oItem.SubItems Is Nothing Then oItem.SubItems = New List(Of Vblib.OneKeyword)

        Dim oNew As New Vblib.OneKeyword
        oNew.sTagId = oItem.sTagId

        oItem.SubItems.Add(oNew)
        Item2Edit(oNew, True)
    End Sub

    Private _editingItem As Vblib.OneKeyword
    Private _addMode As Boolean

    Private Sub Item2Edit(oItem As Vblib.OneKeyword, bAdd As Boolean)
        uiAddEdit.Visibility = Visibility.Visible
        _editingItem = oItem
        _addMode = bAdd

        If bAdd Then
            uiId.IsReadOnly = False
            uiDisplayName.IsReadOnly = False
        Else
            uiId.IsReadOnly = True
            uiDisplayName.IsReadOnly = True
        End If

        uiId.Text = oItem.sTagId
        uiDisplayName.Text = oItem.sDisplayName

        If oItem.minDate.Year > 1000 And oItem.minDate.Year < 2100 Then uiMinDate.SelectedDate = oItem.minDate
        If oItem.maxDate.Year > 1000 And oItem.maxDate.Year < 2100 Then uiMaxDate.SelectedDate = oItem.maxDate

        If oItem.oGeo IsNot Nothing Then
            uiLatitude.Text = oItem.oGeo.Latitude
            uiLongitude.Text = oItem.oGeo.Longitude
            uiRadius.Text = oItem.iGeoRadius
        End If

        uiDefPublish.Text = oItem.defaultPublish
        uiDenyPublish.Text = oItem.denyPublish

        uiNotes.Text = oItem.notes

    End Sub

    Private Sub uiAddEditDone_Click(sender As Object, e As RoutedEventArgs)

        If _addMode Then
            For Each oItem As Vblib.OneKeyword In Application.GetKeywords.GetList
                If oItem.sTagId = uiDisplayName.Text Then
                    vb14.DialogBox("Taka nazwa już istnieje, wybierz inną")
                    Return
                End If
            Next
            _editingItem.sDisplayName = uiDisplayName.Text
            _editingItem.sTagId = uiId.Text
        End If

        If uiMinDate.SelectedDate.HasValue Then _editingItem.minDate = uiMinDate.SelectedDate.Value
        If uiMaxDate.SelectedDate.HasValue Then _editingItem.maxDate = uiMaxDate.SelectedDate.Value

        If uiLatitude.Text.Length > 0 AndAlso uiLongitude.Text.Length > 0 AndAlso uiRadius.Text.Length > 0 Then
            Try
                _editingItem.oGeo = New Vblib.MyBasicGeoposition(uiLatitude.Text, uiLongitude.Text)
            Catch ex As Exception
                vb14.DialogBox("Błędne współrzędne geograficzne")
                Return
            End Try
            _editingItem.iGeoRadius = uiRadius.Text
        End If

        _editingItem.defaultPublish = uiDefPublish.Text
        _editingItem.denyPublish = uiDenyPublish.Text
        _editingItem.notes = uiNotes.Text


        uiAddEdit.Visibility = Visibility.Collapsed
        PrzeliczIpokaz(True)

    End Sub

    Private Sub PrzeliczIpokaz(bPrzelicz As Boolean)
        If bPrzelicz Then Application.GetKeywords.CalculateMinMaxDateTree()
        uiTreeView.ItemsSource = Nothing
        uiTreeView.ItemsSource = Application.GetKeywords.GetList()
    End Sub

    Private Sub uiExportItem_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oItem As Vblib.OneKeyword = oFE?.DataContext

        If oItem Is Nothing Then Return

        Dim sTxt As String = oItem.DumpAsJSON
        vb14.ClipPut(sTxt)
        vb14.DialogBox("Eksport jest w Clipboard")
    End Sub

    Private Async Sub uiImportSubItem_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oItem As Vblib.OneKeyword = oFE?.DataContext

        If oItem Is Nothing Then Return

        If Not Await vb14.DialogBoxYNAsync("Czy w Clipboard jest tekst do zaimportowania?") Then Return

        Dim sTxt As String = Clipboard.GetText
        Try
            Dim oNode As Vblib.OneKeyword = Newtonsoft.Json.JsonConvert.DeserializeObject(sTxt, GetType(Vblib.OneKeyword))
            If oItem.SubItems Is Nothing Then oItem.SubItems = New List(Of Vblib.OneKeyword)
            oItem.SubItems.Add(oNode)
            PrzeliczIpokaz(True)
        Catch ex As Exception

        End Try
    End Sub
End Class

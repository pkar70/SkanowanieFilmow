
Imports pkar
Imports Vblib
Imports vb14 = Vblib.pkarlibmodule14
Imports pkar.UI.Extensions


Class SettingsKeywords
    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        Me.InitDialogs

        PrzeliczIpokaz(True, Nothing)
    End Sub

    Private Sub uiOk_Click(sender As Object, e As RoutedEventArgs)
        Application.GetKeywords.Save(True)
        'Me.NavigationService.GoBack()
        Me.Close()
    End Sub

    Private Sub uiEditKeyword_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oItem As Vblib.OneKeyword = oFE?.DataContext
        If oItem Is Nothing Then Return

        If oItem.sId.Length = 1 Then
            Me.MsgBox("Głównego węzła nie wolno edytować")
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
        oNew.sId = oItem.sId
        oNew.denyPublish = oItem.denyPublish    ' domyślnie schodzi zakaz w dół

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

        uiId.Text = oItem.sId
        uiDisplayName.Text = oItem.sDisplayName

        If oItem.minDate.Year > 1000 And oItem.minDate.Year < 2100 Then uiMinDate.SelectedDate = oItem.minDate
        If oItem.maxDate.Year > 1000 And oItem.maxDate.Year < 2100 Then uiMaxDate.SelectedDate = oItem.maxDate

        If oItem.oGeo IsNot Nothing Then
            uiLatitude.Text = oItem.oGeo.Latitude
            uiLongitude.Text = oItem.oGeo.Longitude
            uiRadius.Text = oItem.iGeoRadius
        Else
            uiLatitude.Text = ""
            uiLongitude.Text = ""
            uiRadius.Text = ""
        End If

        uiOwnDir.Items.Clear()
        FillDirCombo(uiOwnDir, oItem.ownDir, False)

        'uiDefPublish.Text = oItem.defaultPublish
        uiDenyPublish.IsChecked = oItem.denyPublish

        uiNotes.Text = oItem.notes

    End Sub

    Private Shared Sub WypelnComboRecursive(uiCombo As ComboBox, oItem As Vblib.OneDir, sIndent As String, currDir As String, bNoDates As Boolean)

        ' jeśli natrafiliśmy na datowy katalog, a mamy takich nie pokazywać, to nie pokazujemy całego drzewka
        If bNoDates AndAlso oItem.IsFromDate Then Return

        Dim oNew As New ComboBoxItem
        oNew.Content = sIndent & oItem.ToComboDisplayName
        oNew.DataContext = oItem
        Dim iInd As Integer = uiCombo.Items.Add(oNew)
        If oItem.sId = currDir Then uiCombo.SelectedIndex = iInd

        If oItem.SubItems IsNot Nothing Then
            For Each oSubItem As Vblib.OneDir In oItem.SubItems
                'If oSubItem.SubItems Is Nothing Then Continue For
                'If oSubItem.SubItems.Count < 1 Then Continue For

                WypelnComboRecursive(uiCombo, oSubItem, sIndent & "  ", currDir, bNoDates)
            Next
        End If

    End Sub

    Public Shared Sub FillDirCombo(uiCombo As ComboBox, currDir As String, bNoDates As Boolean)
        Application.GetDirTree.ForEach(Sub(x) WypelnComboRecursive(uiCombo, x, "", currDir, bNoDates))
    End Sub


    Private Async Sub uiAddEditDone_Click(sender As Object, e As RoutedEventArgs)

        If _addMode Then
            For Each oItem As Vblib.OneKeyword In Application.GetKeywords
                If oItem.sId = uiDisplayName.Text Then
                    Me.MsgBox("Taka nazwa już istnieje, wybierz inną")
                    Return
                End If
            Next
            _editingItem.sDisplayName = uiDisplayName.Text
            _editingItem.sId = uiId.Text
        End If

        If uiMinDate.SelectedDate.HasValue Then _editingItem.minDate = uiMinDate.SelectedDate.Value
        If uiMaxDate.SelectedDate.HasValue Then _editingItem.maxDate = uiMaxDate.SelectedDate.Value

        If uiLatitude.Text.Length > 0 AndAlso uiLongitude.Text.Length > 0 AndAlso uiRadius.Text.Length > 0 Then
            Try
                _editingItem.oGeo = New BasicGeopos(uiLatitude.Text, uiLongitude.Text)
            Catch ex As Exception
                Me.MsgBox("Błędne współrzędne geograficzne")
                Return
            End Try
            _editingItem.iGeoRadius = uiRadius.Text
        End If

        If uiOwnDir.SelectedIndex < 0 Then
            _editingItem.ownDir = Nothing
        Else
            Dim oCBI As ComboBoxItem = uiOwnDir.SelectedItem
            Dim oDir As OneDir = oCBI.DataContext
            _editingItem.ownDir = oDir.sId
        End If

        '_editingItem.defaultPublish = uiDefPublish.Text
        If _editingItem.denyPublish <> uiDenyPublish.IsChecked AndAlso _editingItem.SubItems IsNot Nothing AndAlso _editingItem.SubItems.Count > 0 Then
            _editingItem.denyPublish = uiDenyPublish.IsChecked

            If Await Me.DialogBoxYNAsync("Zmiana zakazu publikacji, propagować w subkeys?") Then
                For Each oItem As OneKeyword In _editingItem.ToFlatList
                    oItem.denyPublish = _editingItem.denyPublish
                Next
            End If
        End If
        _editingItem.notes = uiNotes.Text


        uiAddEdit.Visibility = Visibility.Collapsed


        PrzeliczIpokaz(True, _editingItem)

    End Sub

    Private Sub PrzeliczIpokaz(bPrzelicz As Boolean, itemToShow As Vblib.OneKeyword)
        If bPrzelicz Then Application.GetKeywords.CalculateMinMaxDateTree()
        uiTreeView.ItemsSource = Nothing
        uiTreeView.ItemsSource = Application.GetKeywords

        If itemToShow Is Nothing Then Return

        Dim stck As FrameworkElement = ProcessBrowse.GetDescendantByType(uiTreeView, GetType(StackPanel))
        If stck Is Nothing Then Return

        For iLp As Integer = 0 To VisualTreeHelper.GetChildrenCount(stck) - 1
            Dim vsl As TreeViewItem = VisualTreeHelper.GetChild(stck, iLp)
            Dim oItem As Vblib.OneKeyword = TryCast(vsl?.DataContext, Vblib.OneKeyword)
            If oItem Is Nothing Then Continue For

            If oItem.sId.StartsWith(itemToShow.sId.Substring(0, 1)) Then
                ' vsl.ExpandSubtree() - to rozwija wszystko
                vsl.IsExpanded = True
                Exit For
            End If
        Next

    End Sub

    Private Sub uiExportItem_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oItem As Vblib.OneKeyword = oFE?.DataContext

        If oItem Is Nothing Then Return

        oItem.DumpAsJSON.SendToClipboard
        Me.MsgBox("Eksport jest w Clipboard")
    End Sub

    Private Async Sub uiImportSubItem_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oItem As Vblib.OneKeyword = oFE?.DataContext

        If oItem Is Nothing Then Return

        If Not Await Me.DialogBoxYNAsync("Czy w Clipboard jest tekst do zaimportowania?") Then Return

        Dim sTxt As String = Clipboard.GetText
        Try
            Dim oNode As Vblib.OneKeyword = Newtonsoft.Json.JsonConvert.DeserializeObject(sTxt, GetType(Vblib.OneKeyword))
            If oItem.SubItems Is Nothing Then oItem.SubItems = New List(Of Vblib.OneKeyword)
            oItem.SubItems.Add(oNode)
            PrzeliczIpokaz(True, oItem)
        Catch ex As Exception

        End Try
    End Sub

    ' teraz to jest w okienku EnterGeoTag
    'Private Sub uiLatitude_TextChanged(sender As Object, e As TextChangedEventArgs) Handles uiLatitude.TextChanged
    '    ' https://www.openstreetmap.org/way/830020459#map=18/50.01990/19.97866
    '    If Not uiLatitude.Text.StartsWith("http") Then Return

    '    Dim oPos As pkar.BasicGeopos = SettingsMapsy.Link2Geo(uiLatitude.Text)
    '    If oPos.IsEmpty Then Return

    '    SetGeo(oPos, 100)
    'End Sub

    Private Sub SetGeo(oPos As BasicGeopos, iRadius As Integer)
        If oPos.IsEmpty Then Return

        _editingItem.oGeo = oPos
        uiLatitude.Text = oPos.Latitude
        uiLongitude.Text = oPos.Longitude
        uiRadius.Text = iRadius
    End Sub

    Private Sub uiOpenGeo_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New EnterGeoTag
        If Not oWnd.ShowDialog Then Return

        Dim oGeo As BasicGeopos = oWnd.GetGeoPos
        Dim iRadius As Integer = If(oWnd.IsZgrubne, 20000, 100)
        SetGeo(oGeo, iRadius)

    End Sub

    Private Async Sub uiDeleteItem_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oItem As Vblib.OneKeyword = oFE?.DataContext

        If oItem Is Nothing Then Return

        If oItem.SubItems IsNot Nothing AndAlso oItem.SubItems.Count > 0 Then
            If Not Await Me.DialogBoxYNAsync("To słowo zawiera pod-słowa, skasować całe drzewko?") Then Return
        End If

        ' we flat, sprawdzić występowanie słów kluczowych w buffor/archive
        Dim bHasKeys As Boolean = False
        For Each oKey As OneKeyword In oItem.ToFlatList
            For Each oPic As Vblib.OnePic In Application.GetBuffer.GetList
                If oPic.HasKeyword(oKey) Then
                    bHasKeys = True
                    Exit For
                End If
            Next
            If bHasKeys Then Exit For
        Next

        ' kolejne pytanie - lub w ogóle zakaz gdy użyte
        If bHasKeys Then
            If Not Await Me.DialogBoxYNAsync("Keyword jest używany, na pewno usunąć?") Then Return
        End If

        Dim tatus As Vblib.OneKeyword = Application.GetKeywords.GetParentOf(oItem)

        ' kasowanie
        Application.GetKeywords.Remove(oItem)

        PrzeliczIpokaz(True, tatus)

    End Sub
End Class

Public Class KonwersjaGeo
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.Convert
        Dim temp As BasicGeopos = CType(value, BasicGeopos)

        If temp Is Nothing Then Return ""
        If temp.IsEmpty Then Return ""

        Return AutotaggerBase.IconGeo

    End Function


    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New NotImplementedException()
    End Function
End Class

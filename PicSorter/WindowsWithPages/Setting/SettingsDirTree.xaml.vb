Imports Vblib
Imports vb14 = Vblib.pkarlibmodule14

Public Class SettingsDirTree

    ' skopiowane i przerobione z Keywords, więc trochę pozostałości nazewniczych stamtąd

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        RefreshList()
    End Sub

    Private Sub uiOk_Click(sender As Object, e As RoutedEventArgs)
        Application.GetDirTree.Save(True)
        Me.Close()
    End Sub

    Private Sub uiEditKeyword_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oItem As Vblib.OneDir = oFE?.DataContext

        If oItem Is Nothing Then Return

        If oItem.IsRoot Then
            vb14.DialogBox("Głównego węzła nie wolno edytować")
            Return
        End If

        Item2Edit(oItem, False)

    End Sub

    Private Sub uiAddSubItem_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oItem As Vblib.OneDir = oFE?.DataContext

        If oItem Is Nothing Then Return

        If oItem.SubItems Is Nothing Then oItem.SubItems = New List(Of Vblib.OneDir)

        Dim oNew As New Vblib.OneDir
        oNew.denyPublish = oItem.denyPublish    ' domyślnie schodzi zakaz w dół
        oNew.sParentId = oItem.sId

        oItem.SubItems.Add(oNew)
        Item2Edit(oNew, True)
    End Sub

    Private _editingItem As Vblib.OneDir
    Private _addMode As Boolean

    Private Sub Item2Edit(oItem As Vblib.OneDir, bAdd As Boolean)
        uiAddEdit.Visibility = Visibility.Visible
        _editingItem = oItem
        _addMode = bAdd

        If bAdd Then
            uiId.IsReadOnly = False
            'uiDisplayName.IsReadOnly = False
        Else
            uiId.IsReadOnly = True
            'uiDisplayName.IsReadOnly = True
        End If

        uiId.Text = oItem.sId
        'uiDisplayName.Text = oItem.notes

        uiDenyPublish.IsChecked = oItem.denyPublish

        uiNotes.Text = oItem.notes

    End Sub

    Private Async Sub uiAddEditDone_Click(sender As Object, e As RoutedEventArgs)

        If _addMode Then
            For Each oItem As Vblib.OneDir In Application.GetDirTree.GetList
                If oItem.sId = uiId.Text Then
                    vb14.DialogBox("Taka nazwa już istnieje, wybierz inną")
                    Return
                End If
            Next
            _editingItem.notes = uiNotes.Text
            _editingItem.sId = uiId.Text
        End If

        '_editingItem.defaultPublish = uiDefPublish.Text
        If _editingItem.denyPublish <> uiDenyPublish.IsChecked AndAlso _editingItem.SubItems IsNot Nothing AndAlso _editingItem.SubItems.Count > 0 Then
            _editingItem.denyPublish = uiDenyPublish.IsChecked

            If Await vb14.DialogBoxYNAsync("Zmiana zakazu publikacji, propagować w subkeys?") Then
                For Each oItem As OneDir In _editingItem.ToFlatList
                    oItem.denyPublish = _editingItem.denyPublish
                Next
            End If
        End If
        _editingItem.notes = uiNotes.Text


        uiAddEdit.Visibility = Visibility.Collapsed
        RefreshList()

    End Sub

    Private Sub RefreshList()
        uiTreeView.ItemsSource = Nothing
        uiTreeView.ItemsSource = Application.GetDirTree.GetList()
    End Sub

    Private Sub uiExportItem_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oItem As Vblib.OneDir = oFE?.DataContext

        If oItem Is Nothing Then Return

        Dim sTxt As String = oItem.DumpAsJSON
        vb14.ClipPut(sTxt)
        vb14.DialogBox("Eksport jest w Clipboard")
    End Sub

    Private Async Sub uiImportSubItem_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oItem As Vblib.OneDir = oFE?.DataContext

        If oItem Is Nothing Then Return

        If Not Await vb14.DialogBoxYNAsync("Czy w Clipboard jest tekst do zaimportowania?") Then Return

        Dim sTxt As String = Clipboard.GetText
        Try
            Dim oNode As Vblib.OneDir = Newtonsoft.Json.JsonConvert.DeserializeObject(sTxt, GetType(Vblib.OneKeyword))
            If oItem.SubItems Is Nothing Then oItem.SubItems = New List(Of Vblib.OneDir)
            oItem.SubItems.Add(oNode)
            RefreshList()
        Catch ex As Exception

        End Try
    End Sub

    Private Async Sub uiDeleteItem_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oItem As Vblib.OneDir = oFE?.DataContext

        If oItem Is Nothing Then Return

        If oItem.SubItems IsNot Nothing AndAlso oItem.SubItems.Count > 0 Then
            If Not Await vb14.DialogBoxYNAsync("Ten katalog zawiera pod-katalogi, skasować całe drzewko?") Then Return
        End If

        ' nie trzeba we flat wszystkich katalogów, bo wystarczy prefix
        Dim bHasKeys As Boolean = False
        For Each oPic As Vblib.OnePic In Application.GetBuffer.GetList
            If oPic.TargetDir Is Nothing Then Continue For
            If oPic.TargetDir.StartsWith(oItem.sId) Then
                bHasKeys = True
                Exit For
            End If
        Next

        ' kolejne pytanie - lub w ogóle zakaz gdy użyte
        If bHasKeys Then
            If Not Await vb14.DialogBoxYNAsync("Katalog jest używany, na pewno usunąć z listy?") Then Return
        End If

        ' kasowanie
        Application.GetDirTree.Remove(oItem)
    End Sub


    Private Sub uiScanFolder_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oItem As Vblib.OneDir = oFE?.DataContext

        If oItem Is Nothing Then Return

        Dim sFolder As String = SettingsGlobal.FolderBrowser("", "Wskaż drzewko katalogów")
        If sFolder = "" Then Return

        Application.ShowWait(True)
        ' wczytanie istniejących folderów, tree, jako podkatalogi do item
        Application.GetDirTree.AddSubfolderTree(oItem, sFolder)
        Application.ShowWait(False)

        RefreshList()
    End Sub


End Class


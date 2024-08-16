Imports System.Globalization
Imports System.Xaml.Schema
Imports Vblib
Imports vb14 = Vblib.pkarlibmodule14
Imports pkar.DotNetExtensions
Imports System.Security.Policy
Imports System.ComponentModel

Public Class SettingsDirTree

    Public Shared _EditMode As Boolean

    ''' <summary>
    ''' FALSE gdy jako browse w archiwum, TRUE gdy edytor tagów
    ''' </summary>
    ''' <param name="editMode"></param>
    Public Sub New(editMode As Boolean)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        _EditMode = editMode
    End Sub

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        RefreshList()
        uiGridQuery.Visibility = If(_EditMode, Visibility.Collapsed, Visibility.Visible)
    End Sub


    Private Sub uiOk_Click(sender As Object, e As RoutedEventArgs)
        vblib.GetDirTree.Save(True)
        ' CloudArchivesList.CopyToOneDrive("dirstree.json", "uiUseOneDrive") - teraz idzie automatem, bo jest ustawione w BaseList.MaintainCopy
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
        'oNew.sParentId = oItem.sId
        oNew.fullPath = oItem.fullPath    ' tylko tymczasowo! na OK trzeba zmienić path

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
            uiId.Focus()
            'uiDisplayName.IsReadOnly = False
        Else
            uiId.IsReadOnly = True
            uiNotes.Focus()
            'uiDisplayName.IsReadOnly = True
        End If

        uiId.Text = oItem.sId
        'uiDisplayName.Text = oItem.notes

        uiDenyPublish.IsChecked = oItem.denyPublish

        uiNotes.Text = oItem.notes

    End Sub

    Private Async Sub uiAddEditDone_Click(sender As Object, e As RoutedEventArgs)

        If _addMode Then
            If vblib.GetDirTree.IdExists(uiId.Text) Then
                vb14.DialogBox("Taka nazwa już istnieje, wybierz inną")
                Return
            End If
            _editingItem.notes = uiNotes.Text
            _editingItem.sId = uiId.Text.DropAccents
            If String.IsNullOrEmpty(_editingItem.fullPath) Then
                _editingItem.fullPath = _editingItem.sId
            Else
                _editingItem.fullPath = IO.Path.Combine(_editingItem.fullPath, _editingItem.sId)
            End If
        End If

        '_editingItem.defaultPublish = uiDefPublish.Text
        If _editingItem.denyPublish <> uiDenyPublish.IsChecked AndAlso _editingItem.SubItems IsNot Nothing AndAlso _editingItem.SubItems.Count > 0 Then
            _editingItem.denyPublish = uiDenyPublish.IsChecked

            If Await vb14.DialogBoxYNAsync("Zmiana zakazu publikacji, propagować w subkeys?") Then
                For Each oItem As Vblib.OneDir In _editingItem.ToFlatList
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
        uiTreeView.ItemsSource = vblib.GetDirTree
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

        ' nie trzeba we flat wszystkich katalogów, bo wystarczy prefix (tylko o co chodziło w tym zapisie?)
        Dim bHasKeys As Boolean = False
        For Each oPic As Vblib.OnePic In vblib.GetBuffer.GetList
            If oPic.TargetDir Is Nothing Then Continue For
            ' tu było StartsWith, ale przecież teraz mamy pełne ścieżki więc musimy Contains
            If oPic.TargetDir.Contains(oItem.sId) Then
                bHasKeys = True
                Exit For
            End If
        Next

        ' kolejne pytanie - lub w ogóle zakaz gdy użyte
        If bHasKeys Then
            If Not Await vb14.DialogBoxYNAsync("Katalog jest używany, na pewno usunąć z listy?") Then Return
        End If

        ' kasowanie
        vblib.GetDirTree.Remove(oItem)
        RefreshList()
    End Sub


    Private Sub uiScanFolder_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oItem As Vblib.OneDir = oFE?.DataContext
        If oItem Is Nothing Then Return

        Dim sFolder As String = SettingsGlobal.FolderBrowser("", "Wskaż drzewko katalogów")
        If sFolder = "" Then Return

        Application.ShowWait(True)
        ' wczytanie istniejących folderów, tree, jako podkatalogi do item
        vblib.GetDirTree.AddSubfolderTree(oItem, sFolder)
        Application.ShowWait(False)

        RefreshList()
    End Sub

    Private Sub uiOpenFolder_Click(sender As Object, e As RoutedEventArgs)
        vb14.DumpCurrMethod()

        Dim oFE As FrameworkElement = sender
        Dim oItem As Vblib.OneDir = oFE?.DataContext
        If oItem Is Nothing Then Return

        OpenFolderInPicBrowser(oItem.fullPath)

    End Sub

    Public Shared Sub OpenFolderInPicBrowser(sTargetDir As String)
        ' Dim sTargetDir As String = Application.GetDirTree.GetFullPath(sFolderId)

        For Each oArch As lib_PicSource.LocalStorageMiddle In Application.GetArchivesList
            vb14.DumpMessage($"trying archive {oArch.StorageName}")
            Dim sRealPath As String = oArch.GetRealPath(sTargetDir, Vblib.ArchiveIndex.FOLDER_INDEX_FILE)
            vb14.DumpMessage($"real path of index file: {sRealPath}")
            If Not String.IsNullOrWhiteSpace(sRealPath) Then

                Dim oBuffer As New Vblib.BufferFromQuery(sRealPath)

                Dim oWnd As New ProcessBrowse(oBuffer, sTargetDir)
                oWnd.Show()
                Return
            End If
        Next
        vb14.DialogBox("Wygląda na to że nie mam pliku indeksowego w tym katalogu w żadnym archiwum")

    End Sub

    Private Sub uiQuery_TextChanged(sender As Object, e As TextChangedEventArgs) Handles uiQuery.TextChanged

        Dim query As String = uiQuery.Text.ToLowerInvariant
        If query.Length < 3 Then
            uiTreeView.Visibility = Visibility.Visible
            uiLista.Visibility = Visibility.Collapsed
            Return
        End If
        uiTreeView.Visibility = Visibility.Collapsed
        uiLista.Visibility = Visibility.Visible

        ' mamy jakieś query wpisane, to szukamy wedle niego
        Dim lista As New List(Of Vblib.OneDir)
        For Each oFold In vblib.GetDirTree.ToFlatList
            If oFold.notes.ContainsCIAI(query) OrElse oFold.sId.ContainsCIAI(query) Then
                lista.Add(oFold)
            End If
        Next

        uiLista.ItemsSource = From c In lista Order By c.sId
    End Sub

    Private Sub uiAutoClear_Click(sender As Object, e As RoutedEventArgs)

        Dim doUsuniecia As New List(Of OneDir)

        Dim minPicDate As Date = vblib.GetBuffer.GetMinDate
        Dim maxPicDate As Date = vblib.GetBuffer.GetMaxDate

        vb14.DumpMessage($"Zakres dat zdjec: {minPicDate.ToExifString} do {maxPicDate.ToExifString}")

        For Each oFold In vblib.GetDirTree.ToFlatList
            If String.IsNullOrEmpty(oFold.fullPath) Then Continue For
            vb14.DumpMessage($"analyzing folder {oFold.fullPath}")

            Dim dataFolderu As Date = oFold.GetDate
            If Not dataFolderu.IsDateValid Then Continue For
            If minPicDate > dataFolderu Then Continue For
            If maxPicDate < dataFolderu Then Continue For
            vb14.DumpMessage("data folderu w zakresie dat bufora")

            For Each oPic As OnePic In vblib.GetBuffer.GetList.Where(Function(x) Not String.IsNullOrWhiteSpace(x.TargetDir))
                If oPic.TargetDir.StartsWithOrdinal(oFold.fullPath) Then
                    vb14.DumpMessage("  jest taki w aktualnym buforze, więc go pomijam")
                    Exit For
                End If
                'If oFold.fullPath.StartsWithOrdinal(oPic.TargetDir) Then Exit For

                ' sprawdzamy jego parenta
                Dim bFound As Boolean = False
                Dim parentPath As String = IO.Path.GetDirectoryName(oFold.fullPath)
                vb14.DumpMessage($"  nie ma w aktualnym buforze, więc sprawdzam parenta {parentPath}")
                For Each oPicParent As OnePic In vblib.GetBuffer.GetList
                    If oPicParent.TargetDir.StartsWithOrdinal(parentPath) Then
                        vb14.DumpMessage($"  i parent jest w buforze")
                        bFound = True
                        Exit For
                    End If
                Next

                If Not bFound Then Exit For

                doUsuniecia.Add(oFold)
                Debug.WriteLine($"Do usuniecia: {oFold.fullPath}")
            Next

        Next

        For Each oDir As OneDir In doUsuniecia
            'Application.GetDirTree.Remove(oDir)
        Next

        ' przeglądaj wszystkie katalogi rekurencyjnie
        ' jeśli oDir.Path jest jako StartsWith w buffor
        ' to jeśli oDir.Path
    End Sub
End Class

Public Class KonwersjaVisibilyFromGlobal
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
        Return If(SettingsDirTree._EditMode, Visibility.Visible, Visibility.Collapsed)
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New NotImplementedException()
    End Function
End Class

Public Class KonwersjaVisibilyFromNotGlobal
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
        Return If(Not SettingsDirTree._EditMode, Visibility.Visible, Visibility.Collapsed)
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New NotImplementedException()
    End Function
End Class

Public Class KonwersjaSortujSubitemyDir
    Implements IValueConverter

    Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
        If value Is Nothing Then Return Nothing
        Dim subdiry As IList = TryCast(value, IList)
        Dim oView As New ListCollectionView(subdiry)
        Dim sort As New SortDescription("sId", ListSortDirection.Ascending)
        oView.SortDescriptions.Add(sort)
        Return oView
    End Function

    Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New NotImplementedException()
    End Function
End Class
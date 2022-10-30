
Imports vb14 = Vblib.pkarlibmodule14

Public Class EditEntryHist

    Private _lista As Vblib.EntryHistory
    Private _title As String
    Private _addComment As String
    Private _defaultNew As String

    Public Sub New(sDataFolder As String, sFileTitle As String, sAddComment As String, sDefaultNew As String)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        _title = sFileTitle
        _addComment = sAddComment
        _defaultNew = sDefaultNew

        _lista = New Vblib.EntryHistory(sDataFolder, sFileTitle)
        PokazListe()
    End Sub

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        Title = _title
    End Sub

    Private Sub uiOK_Click(sender As Object, e As RoutedEventArgs)
        _lista?.Save()
        Me.Close()
    End Sub

    Private Sub PokazListe()
        Dim lLista As New List(Of StringToList)
        For Each item As String In _lista.GetList
            lLista.Add(New StringToList(item))
        Next

        uiLista.ItemsSource = lLista
    End Sub

    Private Async Sub uiAdd_Click(sender As Object, e As RoutedEventArgs)
        Dim sNewItem As String = Await vb14.DialogBoxInputAllDirectAsync(_addComment, _defaultNew)
        If String.IsNullOrWhiteSpace(sNewItem) Then Return
        If sNewItem = _defaultNew Then Return

        _lista.Add(sNewItem)
        PokazListe()
    End Sub

    Private Sub uiDel_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim sCo As String = TryCast(oFE?.DataContext, StringToList).itemText
        If String.IsNullOrWhiteSpace(sCo) Then Return

        _lista.Delete(sCo)
        PokazListe()
    End Sub
End Class

Public Class StringToList
    Public Property itemText As String
    Sub New(initText As String)
        itemText = initText
    End Sub
End Class
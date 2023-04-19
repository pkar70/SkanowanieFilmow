

' *TODO* odpowiednik drzewka wyboru katalogu, tak jak w wersji desktop - tree lub search, albo samo search

Imports pkar
Imports Vblib
Imports vb14 = Vblib.pkarlibmodule14


Public NotInheritable Class MainPage
    Inherits Page

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        uiQuery_TextChanged(Nothing, Nothing)
    End Sub

    Private Sub uiSettings_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.Navigate(GetType(Settings))
    End Sub


    Public Shared _DirItem As OneDir

    Private Sub uiOpenThisFolder_Click(sender As Object, e As RoutedEventArgs)
        vb14.DumpCurrMethod()

        Dim oFE As FrameworkElement = sender
        GoSlideShow(oFE)
    End Sub

    Private Sub uiOpenSelected_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = uiLista.SelectedItem
        GoSlideShow(oFE)
    End Sub

    Private Sub GoSlideShow(OFE As FrameworkElement)
        Dim oItem As Vblib.OneDir = OFE?.DataContext
        If oItem Is Nothing Then Return

        _DirItem = oItem
        Me.Navigate(GetType(Slideshow))
    End Sub

    Private Sub uiQuery_TextChanged(sender As Object, e As RoutedEventArgs)

        Dim query As String = uiQuery.Text.ToLowerInvariant
        If query.Length < 3 Then
            Dim flatlist = GetDirTree.ToFlatList
            uiLista.ItemsSource = flatlist
            Return
        End If

        ' mamy jakieś query wpisane, to szukamy wedle niego
        Dim lista As New List(Of Vblib.OneDir)
        For Each oFold In GetDirTree.ToFlatList
            If oFold.notes.ToLowerInvariant.Contains(query) OrElse
                    oFold.sId.ToLowerInvariant.Contains(query) Then
                lista.Add(oFold)
            End If
        Next

        uiLista.ItemsSource = lista
    End Sub

#Region "archives list"

    ' kopia z wersji desktop, z małymi zmianami

    Private Shared gCloudArchives As List(Of Vblib.CloudConfig)
    Public Const CLOUDARCH_FILENAME As String = "cloudArchives.json"

    Public Shared Function GetCloudArchives() As List(Of CloudConfig)
        If gCloudArchives Is Nothing Then
            Dim localCloudArchives = New BaseList(Of Vblib.CloudConfig)(App.GetDataFolder, CLOUDARCH_FILENAME)
            localCloudArchives.Load()

            gCloudArchives = New List(Of CloudConfig)
            ' tylko chomiki obslugujemy
            For Each oItem In localCloudArchives.GetList
                If oItem.sProvider = "Chomikuj" Then
                    gCloudArchives.Add(oItem)
                End If
            Next
        End If
        Return gCloudArchives
    End Function


#End Region

#Region "Dirtree"
    ' kopia z desktop.Application 
    Private Shared gDirtree As Vblib.DirsList

    Public Shared Function GetDirTree() As Vblib.DirsList
        If gDirtree Is Nothing Then
            gDirtree = New Vblib.DirsList(App.GetDataFolder)
            gDirtree.Load()
        End If
        Return gDirtree
    End Function



#End Region

End Class

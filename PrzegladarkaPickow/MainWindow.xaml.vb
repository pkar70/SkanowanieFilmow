Imports pkar.DotNetExtensions

Class MainWindow
    Private Sub uiBrowse_Click(sender As Object, e As RoutedEventArgs)
        Dim sPath As String = "C:\Users\pkar\Pictures\krakow\FBstaryKrakow"
        FolderBrowser(uiFolderPath, sPath, "Select folder with photos")

        uiRefresh_Click(Nothing, Nothing)
    End Sub

    Private Async Sub uiFilter_TextChanged(sender As Object, e As TextChangedEventArgs)
        If uiFilter.Text.Length < 5 Then Return

        Dim aWords As String() = uiFilter.Text.ToLower.Split(" ")

        Dim lista As New List(Of thumb)
        For Each oPic As thumb In _lista
            Dim bMatch As Boolean = True
            For Each word As String In aWords
                If Not oPic.filename.Contains(word) Then
                    bMatch = False
                    Exit For
                End If
            Next

            If Not bMatch Then Continue For

            If oPic.oImageSrc Is Nothing Then
                oPic.oImageSrc = Await WczytajObrazek(oPic.filepath, 300, Rotation.Rotate0)
            End If

            If oPic.oImageSrc IsNot Nothing Then lista.Add(oPic)
        Next

        If lista.Count < 1 Then
            MsgBox("Nie ma nic do pokazania!")
        Else
            uiPicList.ItemsSource = lista
        End If

    End Sub

    Private Sub uiShowPicInfo_Click(sender As Object, e As RoutedEventArgs)

    End Sub

    Private Sub uiShellExec_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oPic As thumb = oFE.DataContext

        Dim proc As New Process()
        proc.StartInfo.UseShellExecute = True
        proc.StartInfo.FileName = oPic.filepath
        proc.Start()
    End Sub

    Private _lista As List(Of thumb)

    Public Sub WczytajKatalog(path As String)
        _lista = New List(Of thumb)

        Dim lastDate As New Date(1970, 1, 1)
        Dim lastName As String = ""

        For Each plik In IO.Directory.EnumerateFiles(uiFolderPath.Text)
            If plik.EndsWithCI("descript.ion") Then Continue For

            Dim oPic As New thumb
            oPic.filename = IO.Path.GetFileNameWithoutExtension(plik).ToLowerInvariant
            oPic.filepath = plik
            ' *TODO* powiększanie sDymek na więcej informacji
            oPic.sDymek = IO.Path.GetFileNameWithoutExtension(plik)
            _lista.Add(oPic)

            If lastDate < IO.File.GetLastWriteTime(plik) Then
                lastDate = IO.File.GetLastWriteTime(plik)
                lastName = IO.Path.GetFileName(plik)
            End If
        Next

        uiNewestPicDate.Text = lastDate.ToString("yyyy.MM.dd HH:mm")
        uiNewestPicDate.ToolTip = lastName
    End Sub

    Public Shared Async Function WczytajObrazek(sPathName As String, Optional iMaxSize As Integer = 0, Optional iRotation As Rotation = Rotation.Rotate0) As Task(Of BitmapImage)
        If Not IO.File.Exists(sPathName) Then Return Nothing
        Dim bitmap = New BitmapImage()
        bitmap.BeginInit()
        If iMaxSize > 0 Then bitmap.DecodePixelHeight = iMaxSize
        bitmap.CacheOption = BitmapCacheOption.OnLoad ' Close na Stream uzyty do ładowania
        bitmap.Rotation = iRotation

        bitmap.UriSource = New Uri(sPathName)
        Try
            bitmap.EndInit()
            Await Task.Delay(1) ' na potrzeby ProgressBara

            Return bitmap
        Catch ex As Exception
            ' nieudane wczytanie miniaturki - to zapewne błąd tworzenia miniaturki, można spróbować ją utworzyć jeszcze raz
        End Try

        Return Nothing

    End Function


    Public Shared Sub FolderBrowser(oBox As TextBox, sDefaultDir As String, sTitle As String)

        Dim sDir As String
        If IO.Directory.Exists(oBox.Text) Then
            sDir = oBox.Text
        Else
            sDir = sDefaultDir
        End If

        sDir = FolderBrowser(sDir, sTitle)
        If String.IsNullOrWhiteSpace(sDir) Then Return

        oBox.Text = sDir
    End Sub
    Public Shared Function FolderBrowser(sDefaultDir As String, sTitle As String) As String
        Dim oPicker As New Microsoft.Win32.SaveFileDialog
        oPicker.FileName = "none" ' Default file name
        oPicker.Title = sTitle
        oPicker.CheckPathExists = True
        oPicker.InitialDirectory = sDefaultDir

        ' Show open file dialog box
        Dim result? As Boolean = oPicker.ShowDialog()

        ' Process open file dialog box results
        If result <> True Then Return ""

        Dim filename As String = oPicker.FileName
        Return IO.Path.GetDirectoryName(filename)
    End Function

    Private Sub uiRefresh_Click(sender As Object, e As RoutedEventArgs)
        WczytajKatalog(uiFolderPath.Text)
    End Sub
End Class

Public Class thumb
    Public Property oImageSrc As BitmapImage = Nothing
    Public Property filename As String
    Public Property filepath As String
    Public Property sDymek As String
End Class
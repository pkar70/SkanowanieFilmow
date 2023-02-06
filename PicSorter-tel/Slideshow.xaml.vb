Imports System.Threading
Imports vb14 = Vblib.pkarlibmodule14

Public NotInheritable Class Slideshow
    Inherits Page

    Private _currArchive As chomikowanie

    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)

        Me.ProgRingInit(True, False)

        _currArchive = GetCurrArchive()


        uiNext.IsEnabled = False
        uiPrev.IsEnabled = False

        If _currArchive Is Nothing Then Return

        If Not _currArchive.Login Then Return

        uiNext.IsEnabled = True
        uiPrev.IsEnabled = True

        Me.ProgRingShow(True)
        '_currArchive.SetDir(MainPage.GetDirTree.GetFullPath(MainPage._DirItem))
        _currArchive.SetDir(MainPage._DirItem.fullPath)

        Dim sUri As String = _currArchive.GetNextFile(False)

        If Not String.IsNullOrWhiteSpace(sUri) Then
            uiFullPicure.Source = New BitmapImage(New Uri(sUri))
        End If

        Me.ProgRingShow(False)


        ' MainPage._DirItem
    End Sub

    Private Sub uiPrev_Click(sender As Object, e As RoutedEventArgs)

    End Sub

    Private Sub uiNext_Click(sender As Object, e As RoutedEventArgs)

    End Sub


    Private Function GetCurrArchive() As chomikowanie

        For Each oItem As Vblib.CloudConfig In MainPage.GetCloudArchives
            If oItem.nazwa.ToLowerInvariant = Vblib.GetSettingsString("currentArchive").ToLowerInvariant Then
                Return New chomikowanie(oItem)
            End If
        Next

        Return Nothing
    End Function

End Class

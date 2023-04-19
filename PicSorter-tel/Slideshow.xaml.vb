
Imports Windows.Storage.Streams
Imports vb14 = Vblib.pkarlibmodule14

Public NotInheritable Class Slideshow
    Inherits Page

    Private _currArchive As chomikowanie

    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)

        Me.ProgRingInit(True, False)

        Me.ProgRingShow(True)

        If Not Await TryShowFirstPicture() Then
            vb14.DialogBox("Niestety, nie ma zdjęć w tym katalogu")
        End If

        Me.ProgRingShow(False)

    End Sub

    Private Async Function TryShowFirstPicture() As Task(Of Boolean)
        _currArchive = GetCurrArchive()

        uiNext.IsEnabled = False
        uiPrev.IsEnabled = False

        If _currArchive Is Nothing Then Return False

        If Not _currArchive.Login Then Return False

        uiNext.IsEnabled = True
        uiPrev.IsEnabled = True

        Await _currArchive.SetDir(MainPage._DirItem.fullPath)

        Return Await PokazObrazek(False)
    End Function

    Private Async Function PokazObrazek(bPrev As Boolean) As Task(Of Boolean)

        Me.ProgRingShow(True)

        Try
            Using fileStream As Stream = _currArchive.GetNextFile(bPrev, False)
                If fileStream Is Nothing Then
                    Return False
                Else
                    Dim randomStream As IRandomAccessStream = fileStream.AsRandomAccessStream
                    Dim bitmap As New BitmapImage
                    Await bitmap.SetSourceAsync(randomStream) ' .AsRandomAccessStream
                    uiFullPicure.Source = bitmap
                    Return True
                End If
            End Using
        Finally
            Me.ProgRingShow(False)
        End Try

    End Function


    Private Sub uiPrev_Click(sender As Object, e As RoutedEventArgs)
        PokazObrazek(True)
    End Sub

    Private Sub uiNext_Click(sender As Object, e As RoutedEventArgs)
        PokazObrazek(False)
    End Sub


    Private Function GetCurrArchive() As chomikowanie

        For Each oItem As Vblib.CloudConfig In MainPage.GetCloudArchives
            If oItem.nazwa.ToLowerInvariant = Vblib.GetSettingsString("currentArchive").ToLowerInvariant Then
                Return New chomikowanie(oItem)
            End If
        Next

        Return Nothing
    End Function

    Private Sub uiImage_Tapped(sender As Object, e As TappedRoutedEventArgs)
        ' jeśli pierwsze 10 %, to GoPrev
        ' jeśli ostatnie 10 %, to GoNext

        If e.GetPosition(uiFullPicure).X * 10 < uiFullPicure.ActualWidth Then
            PokazObrazek(True)
        Else
            If e.GetPosition(uiFullPicure).X > uiFullPicure.ActualWidth / 10 Then
                PokazObrazek(False)
            End If
        End If


    End Sub
End Class

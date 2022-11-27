

Class ProcessPic
    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        AktualizujGuziki()
    End Sub

    ''' <summary>
    ''' liczy ile jest w buforze zdjęć które nie trafiły do wszystkich archiwów
    ''' </summary>
    ''' <returns></returns>
    Private Function CountDoArchiwizacji() As Integer

        Dim currentArchs As New List(Of String)
        For Each oArch As Vblib.LocalStorage In Application.GetArchivesList.GetList
            If oArch.enabled Then currentArchs.Add(oArch.StorageName.ToLower)
        Next

        If currentArchs.Count < 1 Then Return 0

        Dim iCnt As Integer = 0
        For Each oPic As Vblib.OnePic In Application.GetBuffer.GetList
            If String.IsNullOrWhiteSpace(oPic.TargetDir) Then Continue For

            If oPic.Archived Is Nothing Then
                iCnt += 1
            Else

                Dim sArchiwa As String = oPic.Archived.ToLower

                For Each sArch As String In currentArchs
                    If Not sArchiwa.Contains(sArch) Then
                        iCnt += 1
                        Exit For
                    End If
                Next
            End If

        Next

        Return iCnt

    End Function

    Private Function CountDoCloudArchiwizacji() As Integer

        Dim currentArchs As New List(Of String)
        For Each oArch As Vblib.LocalStorage In Application.GetArchivesList.GetList
            If oArch.enabled Then currentArchs.Add(oArch.StorageName.ToLower)
        Next

        If currentArchs.Count < 1 Then Return 0

        Dim iCnt As Integer = 0
        For Each oPic As Vblib.OnePic In Application.GetBuffer.GetList
            If String.IsNullOrWhiteSpace(oPic.TargetDir) Then Continue For

            If oPic.Archived Is Nothing Then
                iCnt += 1
            Else

                Dim sArchiwa As String = oPic.Archived.ToLower

                For Each sArch As String In currentArchs
                    If Not sArchiwa.Contains(sArch) Then
                        iCnt += 1
                        Exit For
                    End If
                Next
            End If

        Next

        Return iCnt

    End Function

    Private Sub AktualizujGuziki()

        ' z licznika z bufora
        Dim counter As Integer = Application.GetBuffer.Count
        uiBrowse.Content = $"Browse ({counter})"
        uiAutotag.Content = $"Try autotag ({counter})"


        uiAutotag.IsEnabled = (counter > 0)
        uiBatchEdit.IsEnabled = (counter > 0)

        ' z licznika do archiwizacji
        counter = CountDoArchiwizacji()
        uiLocalArch.Content = $"Local arch ({counter})"
        uiLocalArch.IsEnabled = (counter > 0)

        ' z licznika do web archiwizacji
        counter = CountDoCloudArchiwizacji()
        uiCloudArch.Content = $"Cloud arch ({counter})"
        uiCloudArch.IsEnabled = (counter > 0)


        ' oraz bez licznika
        uiPublish.Content = $"Publish"
        uiPublish.IsEnabled = (counter > 0)

    End Sub


    Private Sub uiBrowse_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New ProcessBrowse(Application.GetBuffer)
        oWnd.ShowDialog()
        AktualizujGuziki()
    End Sub

    Private Sub uiAutotag_Click(sender As Object, e As RoutedEventArgs) Handles uiAutotag.Click
        Dim oWnd As New AutoTags
        oWnd.ShowDialog()
    End Sub

    Private Sub uiBatchEdit_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New BatchEdit
        oWnd.ShowDialog()
    End Sub
End Class

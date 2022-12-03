

Class ProcessPic
    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        AktualizujGuziki()
    End Sub

    Private Function CountDoCloudArchiwizacji() As Integer

        Dim currentArchs As New List(Of String)
        For Each oArch As Vblib.CloudArchive In Application.GetCloudArchives.GetList
            If oArch.konfiguracja.enabled Then currentArchs.Add(oArch.konfiguracja.nazwa.ToLower)
        Next

        If currentArchs.Count < 1 Then Return 0

        Dim iCnt As Integer = 0
        For Each oPic As Vblib.OnePic In Application.GetBuffer.GetList
            If String.IsNullOrWhiteSpace(oPic.TargetDir) Then Continue For

            If oPic.Archived Is Nothing Then
                iCnt += 1
            Else
                Dim sArchiwa As String = oPic.CloudArchived.ToLower
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

    Private Function CountDoPublishing() As Integer

        Dim iCnt As Integer = 0
        For Each oPic As Vblib.OnePic In Application.GetBuffer.GetList
            If String.IsNullOrWhiteSpace(oPic.TargetDir) Then Continue For  ' bo musimy wiedzieć gdzie wstawiać
            If oPic.Published Is Nothing Then Continue For   ' bo wysyłamy do Cloud tylko te, które każemy wysyłać, a nie każdy

            For Each oPubl In oPic.Published
                ' jeśli value jest nonempty, to znaczy że mamy identyfikator wpisany - czyli wysłany
                If String.IsNullOrWhiteSpace(oPubl.Value) Then iCnt += 1
            Next
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
        counter = LocalArchive.CountDoArchiwizacji()
        uiLocalArch.Content = $"Local arch ({counter})"
        uiLocalArch.IsEnabled = (counter > 0)

        ' z licznika do web archiwizacji
        counter = CountDoCloudArchiwizacji()
        uiCloudArch.Content = $"Cloud arch ({counter})"
        uiCloudArch.IsEnabled = (counter > 0)


        ' oraz bez licznika
        counter = CountDoPublishing()
        uiPublish.Content = $"Publish {counter}"
        uiPublish.IsEnabled = (counter > 0)

    End Sub


    Private Sub uiBrowse_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New ProcessBrowse(Application.GetBuffer)
        oWnd.ShowDialog()
        AktualizujGuziki()
    End Sub

    Private Sub uiAutotag_Click(sender As Object, e As RoutedEventArgs) Handles uiAutotag.Click
        Dim oWnd As New AutoTags
        oWnd.Show()
    End Sub

    Private Sub uiBatchEdit_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New BatchEdit
        oWnd.Show()
    End Sub

    Private Sub uiLocalArch_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New LocalArchive
        oWnd.Show()
    End Sub

    Private Sub uiCloudPublish_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New CloudPublishing
        oWnd.Show()
    End Sub
End Class

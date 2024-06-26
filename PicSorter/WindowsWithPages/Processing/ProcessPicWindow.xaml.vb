﻿
Imports vb14 = Vblib.pkarlibmodule14

Class ProcessPic

    ' rozmiar okna - zob. AktualizujGuzikiSharingu
    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        Vblib.DumpCurrMethod()
    End Sub

    Private Async Sub Window_Activated(sender As Object, e As EventArgs)
        Vblib.DumpCurrMethod()
        Vblib.DumpCurrMethod()
        AktualizujGuziki()

        Await MozeClearPustyBufor()
    End Sub

    Private Async Sub Window_GotFocus(sender As Object, e As RoutedEventArgs)
        Vblib.DumpCurrMethod()
        AktualizujGuziki()

        Await MozeClearPustyBufor()

    End Sub

    Private Async Function MozeClearPustyBufor() As Task

        ' czyli wróć jeśli coś jest do archiwizacji
        If uiLocalArch.IsEnabled Then Return

        ' wróć jeśli bufor jest pusty
        If Application.GetBuffer.Count < 1 Then Return

        ' i jeszcze prosty test: bo może nic do archiwizacji, jako że nic nie ma targetDir ustalonego
        ' krótki test, jakby następny nie był optymalizowany
        If String.IsNullOrWhiteSpace(Application.GetBuffer.GetList.ElementAt(0).TargetDir) Then Return

        ' lepszy test robimy: przeglądamy wszystkie TargetDiry
        If Application.GetBuffer.GetList.Any(Function(x) String.IsNullOrEmpty(x.TargetDir)) Then Return

        If Not Await vb14.DialogBoxYNAsync("Wszystkie pliki są w pełni zarchiwizowane, wyczyścić bufor?") Then Return

        ' skasowanie wszystkich plików z katalogu bufora
        Dim sFolder As String = vb14.GetSettingsString("uiFolderBuffer")
        If String.IsNullOrWhiteSpace(sFolder) Then Return

        Application.ShowWait(True)


        Dim pliki As String() = IO.Directory.GetFiles(sFolder)
        For Each plik As String In pliki
            IO.File.Delete(plik)
        Next

        ' skasowanie buffer.json, i wyzerowanie tego w pamięci
        Application.ResetBuffer()

        Application.ShowWait(False)

        AktualizujGuziki()

    End Function

    Public Shared Function CountDoCloudArchiwizacji() As Integer

        Dim currentArchs As New List(Of String)
        For Each oArch As Vblib.CloudArchive In Application.GetCloudArchives.GetList
            If oArch.konfiguracja.enabled Then currentArchs.Add(oArch.konfiguracja.nazwa.ToLowerInvariant)
        Next

        If currentArchs.Count < 1 Then Return 0

        Dim iCnt As Integer = 0
        For Each oPic As Vblib.OnePic In Application.GetBuffer.GetList
            If String.IsNullOrWhiteSpace(oPic.TargetDir) Then Continue For

            If oPic.CloudArchived Is Nothing Then
                iCnt += 1
            Else
                Dim sArchiwa As String = oPic.CloudArchived.ToLowerInvariant
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

    Private Shared Function CountDoPublishing() As Integer

        Dim iCnt As Integer = 0
        For Each oPic As Vblib.OnePic In Application.GetBuffer.GetList
            If String.IsNullOrWhiteSpace(oPic.TargetDir) Then Continue For  ' bo musimy wiedzieć gdzie wstawiać
            iCnt += oPic.CountPublishingWaiting
        Next

        Return iCnt

    End Function


    ''' <summary>
    ''' Aktualizuje liczbę zdjęć w guzikach, zmienia też guziki Sharingu
    ''' </summary>
    ''' <returns></returns>
    Private Sub AktualizujGuziki()

        AktualizujGuzikiSharingu()

        ' z licznika z bufora
        Dim counter As Integer = Application.GetBuffer.Count
        uiBrowse.Content = $"Buffer ({counter})"
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
        uiPublish.Content = $"Publish ({counter})"
        uiPublish.IsEnabled = (counter > 0)

    End Sub

    ''' <summary>
    ''' włącza/wyłącza guziki sharingu, odpowiednio zmieniając rozmiar okna
    ''' </summary>
    Private Sub AktualizujGuzikiSharingu()
        Dim wysok As Integer = 460

        'uiSharingRetrieve.Visibility = Visibility.Collapsed
        uiSharingDescrips.Visibility = Visibility.Collapsed

        'If Application.GetShareServers.Count > 0 Then
        '    uiSharingRetrieve.Visibility = Visibility.Visible
        '    wysok += 40
        'End If

        If Application.GetShareDescriptionsIn.Count + Application.GetShareDescriptionsOut.Count > 0 Then
            uiSharingDescrips.Content = $"Upload descrs ({Application.GetShareDescriptionsOut.Count})"
            uiSharingDescrips.Visibility = Visibility.Visible
            wysok += 40
        End If

        Me.Height = wysok

    End Sub

    Private Sub uiBrowse_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New ProcessBrowse(Application.GetBuffer, False, "Buffer")
        oWnd.Show()
        AktualizujGuziki()
    End Sub

    Private Sub uiAutotag_Click(sender As Object, e As RoutedEventArgs)
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

    Private Sub uiCloudArch_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New CloudArchiving
        oWnd.Show()
    End Sub

    Private Sub uiSequence_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New SequenceHelper
        oWnd.Show()
    End Sub

    Private Sub uiRetrieve_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New ProcessDownload
        oWnd.Show()
    End Sub

    Private Sub uiSharingDescrips_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New ShareSendDescQueue
        oWnd.Show()
    End Sub

    Private Sub uiSharingRetrieve_Click(sender As Object, e As RoutedEventArgs)

    End Sub

End Class

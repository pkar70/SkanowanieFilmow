﻿Imports vb14 = Vblib.pkarlibmodule14
Public Class CloudArchiving

    Private _lista As List(Of DisplayArchive)

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)

        uiWithTargetDir.Maximum = Application.GetBuffer.Count
        uiWithTargetDir.Value = CountWithTargetDir()
        uiWithTargetDir.ToolTip = uiWithTargetDir.Value & "/" & uiWithTargetDir.Maximum

        ShowArchivesList()

    End Sub

    Private Async Sub uiGetThis_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oSrc As DisplayArchive = oFE?.DataContext
        If oSrc Is Nothing Then Return

        Await ApplyOne(oSrc)

        Window_Loaded(Nothing, Nothing) ' odczytaj na nowo spisy
    End Sub

    Private Async Sub uiGetAll_Click(sender As Object, e As RoutedEventArgs)

        Dim iSelected As Integer = 0
        For Each oSrc As DisplayArchive In _lista
            If oSrc.enabled Then iSelected += 1
        Next

        uiProgBarEngines.Maximum = iSelected
        uiProgBarEngines.Value = 0
        uiProgBarEngines.Visibility = Visibility.Visible

        uiGetAll.IsEnabled = False

        For Each oSrc As DisplayArchive In _lista
            uiProgBarEngines.Value += 1

            If Not oSrc.enabled Then Continue For

            Await ApplyOne(oSrc)
        Next

        uiProgBarEngines.Visibility = Visibility.Collapsed
        uiGetAll.IsEnabled = True

        Window_Loaded(Nothing, Nothing)
    End Sub


    Public Shared Function CountWithTargetDir() As Integer
        Dim iCnt As Integer = 0
        For Each oPic As Vblib.OnePic In Application.GetBuffer.GetList
            If String.IsNullOrWhiteSpace(oPic.TargetDir) Then Continue For
            iCnt += 1
        Next
        Return iCnt
    End Function

    ''' <summary>
    ''' liczy ile jest w buforze zdjęć które nie trafiły do wszystkich archiwów
    ''' </summary>
    ''' <returns></returns>
    ''' 
    Public Shared Function CountDoArchiwizacji() As Integer

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
                    If Not oPic.IsCloudArchivedIn(sArch) Then
                        iCnt += 1
                        Exit For
                    End If
                Next
            End If

        Next

        Return iCnt

    End Function

    Private Shared Function CountArchived(sArchName As String) As Integer
        Dim iCnt As Integer = 0
        For Each oPic As Vblib.OnePic In Application.GetBuffer.GetList
            If oPic.IsCloudArchivedIn(sArchName) Then iCnt += 1
        Next
        Return iCnt

    End Function

    Private Sub ShowArchivesList()

        Dim iMax As Integer = Application.GetBuffer.Count

        _lista = New List(Of DisplayArchive)

        For Each oArch As Vblib.CloudArchive In Application.GetCloudArchives.GetList
            Dim oNew As New DisplayArchive
            oNew.engine = oArch
            oNew.enabled = oArch.konfiguracja.enabled
            oNew.nazwa = oArch.konfiguracja.nazwa
            oNew.dymekAbout = oArch.sProvider
            oNew.maxCount = iMax
            oNew.count = CountArchived(oNew.nazwa)
            oNew.dymekCount = oNew.count & "/" & iMax

            _lista.Add(oNew)
        Next

        uiLista.ItemsSource = _lista
    End Sub

    Private Async Function ApplyOne(oSrc As DisplayArchive) As Task

        If Not Await LocalArchive.CheckGuidy() Then Return

        Application.ShowWait(True)
        uiProgBarInEngine.Maximum = oSrc.maxCount
        uiProgBarInEngine.Value = 0
        uiProgBarInEngine.Visibility = Visibility.Visible

        Dim sIndexJson As String = ""
        Dim bDirTreeToSave As Boolean = False

        Dim sErr As String = ""

        For Each oPic As Vblib.OnePic In Application.GetBuffer.GetList
            Dim sErr1 As String = ""
            uiProgBarInEngine.Value += 1

            If Not IO.File.Exists(oPic.InBufferPathName) Then
                sErr1 = $"Cannot cloud archive {oPic.InBufferPathName} because file doesn't exist"
                Debug.WriteLine(sErr1)
                sErr &= sErr1 & vbCrLf
                Continue For   ' zabezpieczenie przed samoznikaniem
            End If
            If String.IsNullOrEmpty(oPic.TargetDir) Then
                sErr1 = $"Cannot cloud archive {oPic.InBufferPathName} because there is no targetDir"
                Debug.WriteLine(sErr1)
                'sErr &= sErr1 & vbCrLf
                Continue For
            End If

            If oPic.IsCloudArchivedIn(oSrc.nazwa) Then Continue For

            oPic.ResetPipeline()
            Try
                sErr1 = Await oSrc.engine.SendFile(oPic)
            Catch ex As Exception
                sErr1 = $"Cannot cloud archive {oPic.InBufferPathName} to {oPic.TargetDir} because of error {sErr}"
                sErr &= sErr1 & vbCrLf
                Exit For
            End Try

            If sErr1 <> "" Then
                sErr &= $"Cannot cloud archive {oPic.InBufferPathName} to {oPic.TargetDir} because of {sErr1}" & vbCrLf
                Continue For ' nieudane!
            End If

            If Not oPic.IsCloudArchivedIn(oSrc.nazwa) Then
                sErr1 = $"Cannot cloud archive {oPic.InBufferPathName} to {oPic.TargetDir} - unconfirmed archivization"
                sErr &= sErr1 & vbCrLf
                Continue For ' nieudane!
            End If

            Await Task.Delay(2) ' na wszelki wypadek, żeby był czas na przerysowanie progbar
        Next

        If sErr <> "" Then
            Await vb14.DialogBoxAsync("Encountered error(s):" & vbCrLf & sErr)
            vb14.ClipPut(sErr)
        End If

        uiProgBarInEngine.Visibility = Visibility.Collapsed

        Application.GetBuffer.SaveData()  ' bo prawdopodobnie zmiany są w oPic.Archived

        Application.ShowWait(False)
    End Function

    Public Class DisplayArchive
        Public Property enabled As Boolean
        Public Property nazwa As String
        Public Property engine As Vblib.CloudArchive
        Public Property maxCount As Integer
        Public Property count As Integer
        Public Property dymekCount As String
        Public Property dymekAbout As String

    End Class

End Class



Imports vb14 = Vblib.pkarlibmodule14
Imports pkar.DotNetExtensions
Imports pkar.UI.Extensions

Public Class LocalArchive
    'Inherits ProcessWnd_Base


    Private _lista As List(Of DisplayArchive)
    Private _withTargetDir As Integer

#Region "UI"

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        Me.InitDialogs
        Me.ProgRingInit(True, True)

        _withTargetDir = ProcessPic.CountWithTargetDir(Me)

        uiWithTargetDir.Maximum = ProcessPic.GetBuffer(Me).Count
        uiWithTargetDir.Value = _withTargetDir
        uiWithTargetDir.ToolTip = uiWithTargetDir.Value & "/" & uiWithTargetDir.Maximum

        ShowArchivesList()

        Dim iKiB As Integer = GetTotalSizeKiB()

        If iKiB > 0 Then
            uiTotalSize.Text = $"Total size: {(iKiB / 1024).Ceiling} MiB"
        Else
            uiTotalSize.Text = "(cannot calculate total size)"
        End If
    End Sub

    Private Async Sub uiCheckFree_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oSrc As DisplayArchive = oFE?.DataContext
        If oSrc Is Nothing Then Return

        If Not oSrc.engine.IsPresent Then
            Me.MsgBox($"Ale Archiwum '{oSrc.nazwa}' jest aktualnie niewidoczne!")
            Return
        End If

        Dim wolne As Integer = Await oSrc.engine.GetMBfreeSpace
        Me.MsgBox($"Wolne: {wolne.ToStringWithSpaces} MiB")

    End Sub
#End Region


#Region "przygotowanie danych do pokazania"
    Private Function GetTotalSizeKiB() As Integer
        Dim iSize As Integer = 0
        For Each oPic As Vblib.OnePic In ProcessPic.GetBuffer(Me).GetList
            If String.IsNullOrWhiteSpace(oPic.TargetDir) Then Continue For

            If Not IO.File.Exists(oPic.InBufferPathName) Then Continue For
            Try
                iSize += New IO.FileInfo(oPic.InBufferPathName).Length / 1024 + 1
            Catch ex As Exception
                Return -1
            End Try
        Next

        Return iSize
    End Function

#If False Then
    Public Function CountWithTargetDir() As Integer
        Dim iCnt As Integer = 0
        For Each oPic As Vblib.OnePic In ProcessPic.GetBuffer(Me).GetList
            If String.IsNullOrWhiteSpace(oPic.TargetDir) Then Continue For
            iCnt += 1
        Next
        Return iCnt
    End Function
#End If

    ''' <summary>
    ''' liczy ile jest w buforze zdjęć które nie trafiły do wszystkich archiwów
    ''' </summary>
    Public Function CountDoArchiwizacji() As Integer

        Dim currentArchs As New List(Of String)
        Application.GetArchivesList.ForEach(Sub(x) If x.enabled Then currentArchs.Add(x.StorageName.ToLowerInvariant))

        If currentArchs.Count < 1 Then Return 0

        Dim iCnt As Integer = 0
        For Each oPic As Vblib.OnePic In ProcessPic.GetBuffer(Me).GetList
            If String.IsNullOrWhiteSpace(oPic.TargetDir) Then Continue For

            If oPic.Archived Is Nothing Then
                iCnt += 1
            Else
                Dim sArchiwa As String = oPic.Archived.ToLowerInvariant
                For Each sArch As String In currentArchs
                    If Not oPic.IsArchivedIn(sArch) Then
                        iCnt += 1
                        Exit For
                    End If
                Next
            End If

        Next

        Return iCnt

    End Function

    Private Function CountArchived(sArchName As String) As Integer
        Dim iCnt As Integer = 0
        For Each oPic As Vblib.OnePic In ProcessPic.GetBuffer(Me).GetList
            If oPic.IsArchivedIn(sArchName) Then iCnt += 1
        Next
        Return iCnt

    End Function

    Private Sub ShowArchivesList()

        _lista = New List(Of DisplayArchive)

        For Each oArch As Vblib.LocalStorage In Application.GetArchivesList
            Dim oNew As New DisplayArchive
            oNew.engine = oArch
            oNew.enabled = oArch.enabled
            oNew.nazwa = oArch.StorageName
            oNew.dymekAbout = oArch.VolLabel
            oNew.maxCount = _withTargetDir
            oNew.count = CountArchived(oNew.nazwa)
            oNew.dymekCount = oNew.count & "/" & _withTargetDir
            If oNew.count = _withTargetDir Then
                oNew.dymekCount &= " (komplet)"
                oNew.allDone = False
            End If

            _lista.Add(oNew)
        Next

        uiLista.ItemsSource = _lista
    End Sub
#End Region

#Region "archiwizowanie"
#If False Then
    Public Async Function CheckSerNo() As Task(Of Boolean)

        For Each oPic As Vblib.OnePic In GetBuffer.GetList
            If String.IsNullOrEmpty(oPic.TargetDir) Then Continue For
            If oPic.serno < 1 Then
                Await vb14.MsgBoxAsync($"Są zdjęcia bez serno, nie mogę kontynuować! ({oPic.sSuggestedFilename}")
                Return False
            End If
        Next
        Return True
    End Function
#End If
    Private Async Sub uiGetThis_Click(sender As Object, e As RoutedEventArgs)
        vb14.DumpCurrMethod()

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
            If Not oSrc.engine.IsPresent Then Continue For

            Await ApplyOne(oSrc)
        Next

        uiProgBarEngines.Visibility = Visibility.Collapsed
        uiGetAll.IsEnabled = True

        Window_Loaded(Nothing, Nothing)
    End Sub


    Private Async Function ApplyOne(oSrc As DisplayArchive) As Task
        vb14.DumpCurrMethod()

        If Not Await ProcessPic.CheckSerNo(Me) Then Return

        'If Not Await Me.DialogBoxYNAsync("Czy juz poprawiles dopisywanie do archive?") Then Return

        If Not oSrc.engine.IsPresent Then
            Await Me.MsgBoxAsync($"Ale Archiwum '{oSrc.nazwa}' jest aktualnie niewidoczne!")
            Return
        End If

        Dim sIndexShortJson As String = ""
        Dim sIndexLongJson As String = ""

        Dim bDirTreeToSave As Boolean = False

        Dim sErr As String = ""

        Dim newlyArchived As New List(Of Vblib.OnePic)

        Me.ProgRingShow(True, False, oSrc.maxCount)
        Me.ProgRingSetText(oSrc.nazwa)
        Me.ProgRingSetVal(0)

        'Dim serNoLastArchived As Integer = vb14.GetSettingsInt("serNoLastArchived")
        'Dim serNoLastArchivedInit As Integer = serNoLastArchived

        For Each oPic As Vblib.OnePic In ProcessPic.GetBuffer(Me).GetList.Where(Function(x) Not String.IsNullOrEmpty(x.TargetDir))
            ' If String.IsNullOrEmpty(oPic.TargetDir) Then Continue For

            Await Me.ProgRingInc
            Dim sErr1 As String = ""
            If Not IO.File.Exists(oPic.InBufferPathName) Then
                sErr1 = $"Cannot archive {oPic.InBufferPathName} because file doesn't exist"
                Debug.WriteLine(sErr1)
                sErr &= sErr1 & vbCrLf
                Continue For   ' zabezpieczenie przed samoznikaniem
            End If
            If String.IsNullOrEmpty(oPic.TargetDir) Then
                sErr1 = $"Cannot archive {oPic.InBufferPathName} because targetDir is not set"
                Debug.WriteLine(sErr1)
                sErr &= sErr1 & vbCrLf
                Continue For
            End If

            If oPic.IsArchivedIn(oSrc.nazwa) Then Continue For

            'If oPic.serno < 1 Then
            '    ' bo może już być przydzielony z archiwizacji na inny dysk
            '    serNoLastArchived += 1
            '    oPic.serno = serNoLastArchived
            'End If

            sErr1 = Await oSrc.engine.SendFile(oPic)

            If sErr1 <> "" Then
                sErr &= $"Cannot archive {oPic.InBufferPathName} to {oPic.TargetDir} because of {sErr1}" & vbCrLf
                Continue For ' nieudane!
            End If

            If Not oPic.IsArchivedIn(oSrc.nazwa) Then
                sErr1 = $"Cannot archive {oPic.InBufferPathName} to {oPic.TargetDir} - unconfirmed save"
                Debug.WriteLine(sErr1)
                sErr &= sErr1 & vbCrLf

                Continue For ' nieudane!
            End If

            ' aktualizujemy DirList - to tylko ostateczność, bo powinno być wcześniej zrobione.
            ' If Application.GetDirTree.TryAddFolder(oPic.TargetDir, "") Then bDirTreeToSave = True

            ' zapisz jako plik do kiedyś-tam usunięcia ze źródła
            Application.GetSourcesList.AddToPurgeList(oPic.sSourceName, oPic.sInSourceID)

            ' zapisujemy do globalnego archiwum tylko raz, bez powtarzania przy zapisie do każdego LocalArch
            ' tu był błąd! bylo <1, ale to już jest po dopisywaniu; więc ma być +1
            If oPic.ArchivedCount = 1 Then
                ' *TODO* ewentualnie, dac tutaj oPic.Clone i usuniecie z IsCloudPublishMentioned "L:<loginguid>"
                newlyArchived.Add(oPic)
            End If
            '    If sIndexLongJson <> "" Then sIndexLongJson &= ","
            '    sIndexLongJson &= oPic.DumpAsJSON(True)

            '    If sIndexShortJson <> "" Then sIndexShortJson &= ","
            '    sIndexShortJson &= oPic.GetFlatOnePic.DumpAsJSON(True)
            'End If

            Await Task.Delay(2) ' na wszelki wypadek, żeby był czas na przerysowanie progbar
        Next

        ' bez zapisu jesli się nie zmieniło - niewielka, ale jednak oszczędność zapisywania na dysk
        'If serNoLastArchivedInit <> serNoLastArchived Then
        '    vb14.SetSettingsInt("serNoLastArchived", serNoLastArchived)
        'End If

        Me.ProgRingShow(False)

        If sErr <> "" Then
            Await Me.MsgBoxAsync("Encountered error(s):" & vbCrLf & sErr)
            sErr.SendToClipboard
        End If


        ProcessPic.GetBuffer(Me).SaveData()  ' bo prawdopodobnie zmiany są w oPic.Archived
        If bDirTreeToSave Then vblib.GetDirTree.Save(True)   ' bo jakies katalogi całkiem możliwe że dodane są; z ignorowaniem NULLi

        Application.gDbase.AddFiles(newlyArchived)

        'Application.GetArchIndex.AddToGlobalJsonIndex(sIndexShortJson, sIndexLongJson)    ' aktualizacja indeksu archiwalnego

    End Function
#End Region




    Public Class DisplayArchive
        Public Property enabled As Boolean
        Public Property nazwa As String
        Public Property engine As Vblib.LocalStorage
        Public Property maxCount As Integer
        Public Property count As Integer
        Public Property dymekCount As String
        Public Property dymekAbout As String

        Public Property allDone As Boolean = True
    End Class

End Class

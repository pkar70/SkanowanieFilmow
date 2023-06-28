
Imports vb14 = Vblib.pkarlibmodule14


Public Class LocalArchive

    Private _lista As List(Of DisplayArchive)

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)

        uiWithTargetDir.Maximum = Application.GetBuffer.Count
        uiWithTargetDir.Value = CountWithTargetDir()
        uiWithTargetDir.ToolTip = uiWithTargetDir.Value & "/" & uiWithTargetDir.Maximum

        ShowArchivesList()

    End Sub

    Public Shared Async Function CheckGuidy() As Task(Of Boolean)

        'If Not Application.GetBuffer.GetList.
        '    Any(Function(x) (Not String.IsNullOrEmpty(x.TargetDir)) And String.IsNullOrEmpty(x.PicGuid)) Then
        '    Return True
        'End If

        'Return Await vb14.DialogBoxYNAsync("Są zdjęcia bez GUID, kontynuować?")

        For Each oPic As Vblib.OnePic In Application.GetBuffer.GetList
            If String.IsNullOrEmpty(oPic.TargetDir) Then Continue For
            If String.IsNullOrEmpty(oPic.PicGuid) Then
                Return Await vb14.DialogBoxYNAsync($"Są zdjęcia bez GUID, kontynuować? ({oPic.sSuggestedFilename}")
            End If
        Next
        Return True
    End Function

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
    Public Shared Function CountDoArchiwizacji() As Integer

        Dim currentArchs As New List(Of String)
        For Each oArch As Vblib.LocalStorage In Application.GetArchivesList.GetList
            If oArch.enabled Then currentArchs.Add(oArch.StorageName.ToLowerInvariant)
        Next

        If currentArchs.Count < 1 Then Return 0

        Dim iCnt As Integer = 0
        For Each oPic As Vblib.OnePic In Application.GetBuffer.GetList
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

    Private Shared Function CountArchived(sArchName As String) As Integer
        Dim iCnt As Integer = 0
        For Each oPic As Vblib.OnePic In Application.GetBuffer.GetList
            If oPic.IsArchivedIn(sArchName) Then iCnt += 1
        Next
        Return iCnt

    End Function

    Private Sub ShowArchivesList()

        Dim iMax As Integer = Application.GetBuffer.Count

        _lista = New List(Of DisplayArchive)

        For Each oArch As Vblib.LocalStorage In Application.GetArchivesList.GetList
            Dim oNew As New DisplayArchive
            oNew.engine = oArch
            oNew.enabled = oArch.enabled
            oNew.nazwa = oArch.StorageName
            oNew.dymekAbout = oArch.VolLabel
            oNew.maxCount = iMax
            oNew.count = CountArchived(oNew.nazwa)
            oNew.dymekCount = oNew.count & "/" & iMax

            _lista.Add(oNew)
        Next

        uiLista.ItemsSource = _lista
    End Sub

    Private Async Function ApplyOne(oSrc As DisplayArchive) As Task
        vb14.DumpCurrMethod()

        If Not Await CheckGuidy() Then Return

        If Not Await vb14.DialogBoxYNAsync("Czy juz poprawiles dopisywanie do archive?") Then Return

        If Not oSrc.engine.IsPresent Then
            Await vb14.DialogBoxAsync($"Ale Archiwum '{oSrc.nazwa}' jest aktualnie niewidoczne!")
            Return
        End If

        uiProgBarInEngine.Maximum = oSrc.maxCount
        uiProgBarInEngine.Value = 0
        uiProgBarInEngine.Visibility = Visibility.Visible

        Dim sIndexShortJson As String = ""
        Dim sIndexLongJson As String = ""

        Dim bDirTreeToSave As Boolean = False

        Dim sErr As String = ""

        For Each oPic As Vblib.OnePic In Application.GetBuffer.GetList
            If String.IsNullOrEmpty(oPic.TargetDir) Then Continue For

            uiProgBarInEngine.Value += 1
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
                If sIndexLongJson <> "" Then sIndexLongJson &= ","
                sIndexLongJson &= oPic.DumpAsJSON(True)

                If sIndexShortJson <> "" Then sIndexShortJson &= ","
                sIndexShortJson &= oPic.GetFlatOnePic.DumpAsJSON(True)
            End If

            Await Task.Delay(2) ' na wszelki wypadek, żeby był czas na przerysowanie progbar
        Next

        If sErr <> "" Then
            Await vb14.DialogBoxAsync("Encountered error(s):" & vbCrLf & sErr)
            vb14.ClipPut(sErr)
        End If


        uiProgBarInEngine.Visibility = Visibility.Collapsed

        Application.GetBuffer.SaveData()  ' bo prawdopodobnie zmiany są w oPic.Archived
        If bDirTreeToSave Then Application.GetDirTree.Save(True)   ' bo jakies katalogi całkiem możliwe że dodane są; z ignorowaniem NULLi
        Application.GetArchIndex.AddToGlobalJsonIndex(sIndexShortJson, sIndexLongJson)    ' aktualizacja indeksu archiwalnego

    End Function

    Public Class DisplayArchive
        Public Property enabled As Boolean
        Public Property nazwa As String
        Public Property engine As Vblib.LocalStorage
        Public Property maxCount As Integer
        Public Property count As Integer
        Public Property dymekCount As String
        Public Property dymekAbout As String

    End Class

End Class

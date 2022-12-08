
Imports vb14 = Vblib.pkarlibmodule14


Public Class LocalArchive

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
    ''' <returns></returns>
    ''' 
    Public Shared Function CountDoArchiwizacji() As Integer

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

        If Not oSrc.engine.IsPresent Then
            Await vb14.DialogBoxAsync($"Ale Archiwum '{oSrc.nazwa}' jest aktualnie niewidoczne!")
            Return
        End If

        uiProgBarInEngine.Maximum = oSrc.maxCount
        uiProgBarInEngine.Value = 0
        uiProgBarInEngine.Visibility = Visibility.Visible

        Dim sIndexJson As String = ""
        Dim bDirTreeToSave As Boolean = False

        For Each oPic As Vblib.OnePic In Application.GetBuffer.GetList
            uiProgBarInEngine.Value += 1

            If Not IO.File.Exists(oPic.InBufferPathName) Then Continue For   ' zabezpieczenie przed samoznikaniem
            If String.IsNullOrEmpty(oPic.TargetDir) Then Continue For

            If oPic.IsArchivedIn(oSrc.nazwa) Then Continue For

            Await oSrc.engine.SendFile(oPic)
            If Not oPic.IsArchivedIn(oSrc.nazwa) Then Continue For ' nieudane!

            ' aktualizujemy DirList - to tylko ostateczność, bo powinno być wcześniej zrobione
            If Application.GetDirTree.TryAddFolder(oPic.TargetDir, "") Then bDirTreeToSave = True

            ' zapisz jako plik do kiedyś-tam usunięcia ze źródła
            Application.GetSourcesList.AddToPurgeList(oPic.sSourceName, oPic.sInSourceID)

            If sIndexJson <> "" Then sIndexJson &= ","
            sIndexJson &= oPic.DumpAsJSON

            Await Task.Delay(2) ' na wszelki wypadek, żeby był czas na przerysowanie progbar
        Next

        uiProgBarInEngine.Visibility = Visibility.Collapsed

        Application.GetBuffer.SaveData()  ' bo prawdopodobnie zmiany są w oPic.Archived
        If bDirTreeToSave Then Application.GetDirTree.Save(True)   ' bo jakies katalogi całkiem możliwe że dodane są; z ignorowaniem NULLi
        Application.AddToGlobalJsonIndex(sIndexJson)    ' aktualizacja indeksu archiwalnego

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

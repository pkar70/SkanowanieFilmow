Imports vb14 = Vblib.pkarlibmodule14

Public Class CloudPublishing


    Private _lista As List(Of DisplayPublish)

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)

        uiWithTargetDir.Maximum = Application.GetBuffer.Count
        uiWithTargetDir.Value = LocalArchive.CountWithTargetDir()
        uiWithTargetDir.ToolTip = uiWithTargetDir.Value & "/" & uiWithTargetDir.Maximum

        ShowArchivesList()

    End Sub

    Private Async Sub uiGetThis_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oSrc As DisplayPublish = oFE?.DataContext
        If oSrc Is Nothing Then Return

        Await ApplyOne(oSrc)

        Window_Loaded(Nothing, Nothing) ' odczytaj na nowo spisy
    End Sub

    Private Async Sub uiGetAll_Click(sender As Object, e As RoutedEventArgs)

        Dim iSelected As Integer = 0
        For Each oSrc As DisplayPublish In _lista
            If oSrc.enabled Then iSelected += 1
        Next

        uiProgBarEngines.Maximum = iSelected
        uiProgBarEngines.Value = 0
        uiProgBarEngines.Visibility = Visibility.Visible

        uiGetAll.IsEnabled = False

        For Each oSrc As DisplayPublish In _lista
            uiProgBarEngines.Value += 1

            If Not oSrc.enabled Then Continue For

            Await ApplyOne(oSrc)
        Next

        uiProgBarEngines.Visibility = Visibility.Collapsed
        uiGetAll.IsEnabled = True

        Window_Loaded(Nothing, Nothing)
    End Sub



    Private Sub CountPublished(oPublisher As DisplayPublish)
        Dim iCnt As Integer = 0
        Dim iMax As Integer = 0
        Dim sDir As String = ""

        For Each oPic As Vblib.OnePic In Application.GetBuffer.GetList
            If Not oPic.IsCloudPublishMentioned(oPublisher.nazwa) Then Continue For

            iMax += 1

            If oPic.IsCloudPublishedIn(oPublisher.nazwa) Then iCnt += 1
        Next

        oPublisher.maxCount = iMax
        oPublisher.count = iCnt

    End Sub

    Private Sub ShowArchivesList()

        _lista = New List(Of DisplayPublish)

        For Each oArch As Vblib.CloudPublish In Application.GetCloudPublishers.GetList
            Dim oNew As New DisplayPublish
            oNew.engine = oArch
            oNew.enabled = oArch.konfiguracja.enabled
            oNew.nazwa = oArch.konfiguracja.nazwa
            oNew.dymekAbout = oArch.sProvider

            CountPublished(oNew)
            oNew.dymekCount = oNew.count & "/" & oNew.maxCount

            _lista.Add(oNew)
        Next

        uiLista.ItemsSource = _lista
    End Sub

    Private Async Function ApplyOne(oSrc As DisplayPublish) As Task

        uiProgBarInEngine.Maximum = oSrc.maxCount
        uiProgBarInEngine.Value = 0
        uiProgBarInEngine.Visibility = Visibility.Visible

        Dim sIndexJson As String = ""

        For Each oPic As Vblib.OnePic In Application.GetBuffer.GetList
            uiProgBarInEngine.Value += 1

            If Not IO.File.Exists(oPic.InBufferPathName) Then Continue For   ' zabezpieczenie przed samoznikaniem
            If String.IsNullOrEmpty(oPic.TargetDir) Then Continue For

            If Not oPic.IsCloudPublishScheduledIn(oSrc.nazwa) Then Continue For

            Await oSrc.engine.SendFile(oPic)
            If Not oPic.IsArchivedIn(oSrc.nazwa) Then Continue For ' nieudane!

            Await Task.Delay(2) ' na wszelki wypadek, żeby był czas na przerysowanie progbar
        Next

        uiProgBarInEngine.Visibility = Visibility.Collapsed

        Application.GetBuffer.SaveData()  ' bo prawdopodobnie zmiany są w oPic.Published

    End Function

    Public Class DisplayPublish
        Public Property enabled As Boolean
        Public Property nazwa As String
        Public Property engine As Vblib.CloudPublish
        Public Property maxCount As Integer
        Public Property count As Integer
        Public Property dymekCount As String
        Public Property dymekAbout As String

    End Class

End Class

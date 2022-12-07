

' ma pokazać listę autotag engines, dla każdego policzyć ile zdjęc jest nieotagowanych

Imports Windows.ApplicationModel.Activation
Imports vb14 = Vblib.pkarlibmodule14

Public Class AutoTags

    Private _lista As List(Of JedenEngine)

    Private Async Sub uiGetAll_Click(sender As Object, e As RoutedEventArgs)
        If Not Await vb14.DialogBoxYNAsync("Aplikować wszystkie zaznaczone mechanizmy?") Then Return

        Dim iSelected As Integer = 0
        For Each oSrc As JedenEngine In _lista
            If oSrc.enabled Then iSelected += 1
        Next

        uiProgBarEngines.Maximum = iSelected
        uiProgBarEngines.Value = 0
        uiProgBarEngines.Visibility = Visibility.Visible

        uiGetAll.IsEnabled = False

        For Each oSrc As JedenEngine In _lista
            If Not oSrc.enabled Then Continue For

            uiProgBarEngines.ToolTip = oSrc.nazwa

            Await ApplyOne(oSrc)
            uiProgBarEngines.Value += 1
            Application.GetBuffer.SaveData()  ' bo zmieniono EXIF
        Next

        uiProgBarEngines.Visibility = Visibility.Collapsed
        uiGetAll.IsEnabled = True

        Window_Loaded(Nothing, Nothing)
    End Sub

    Private Async Sub uiGetThis_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oSrc As JedenEngine = oFE?.DataContext
        If oSrc Is Nothing Then Return

        Await ApplyOne(oSrc)
        Application.GetBuffer.SaveData()  ' bo zmieniono EXIF
        Window_Loaded(Nothing, Nothing)
    End Sub

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)

        _lista = New List(Of JedenEngine)
        Dim iMax As Integer = Application.GetBuffer.Count

        For Each oEngine As Vblib.AutotaggerBase In Application.gAutoTagery
            Dim oNew As New JedenEngine
            oNew.nazwa = oEngine.Nazwa
            oNew.engine = oEngine
            oNew.maxCount = iMax
            oNew.count = PoliczUstawione(oNew.nazwa)
            oNew.dymekCount = vbCrLf & oNew.count & "/" & oNew.maxCount
            oNew.dymekAbout = oEngine.DymekAbout

            _lista.Add(oNew)
        Next

        uiLista.ItemsSource = _lista
    End Sub

    Private Function PoliczUstawione(nazwa As String) As Integer
        Dim iCnt As Integer = 0

        For Each oItem As Vblib.OnePic In Application.GetBuffer.GetList
            If oItem.GetExifOfType(nazwa) IsNot Nothing Then iCnt += 1
        Next

        Return iCnt
    End Function

    Private Async Function ApplyOne(oSrc As JedenEngine) As Task

        uiProgBarInEngine.Maximum = oSrc.maxCount
        uiProgBarInEngine.Value = 0
        uiProgBarInEngine.Visibility = Visibility.Visible

        For Each oItem As Vblib.OnePic In Application.GetBuffer.GetList
            If Not IO.File.Exists(oItem.InBufferPathName) Then Continue For   ' zabezpieczenie przed samoznikaniem

            If oItem.GetExifOfType(oSrc.nazwa) Is Nothing Then
                Dim oExif As Vblib.ExifTag = Await oSrc.engine.GetForFile(oItem)
                If oExif IsNot Nothing Then
                    oItem.Exifs.Add(oExif)
                    oItem.TagsChanged = True
                End If
                Await Task.Delay(3) ' na wszelki wypadek, żeby był czas na przerysowanie progbar, nawet jak tworzenie EXIFa jest empty
                End If
                uiProgBarInEngine.Value += 1
        Next

        uiProgBarInEngine.Visibility = Visibility.Collapsed

    End Function


    Public Class JedenEngine
        Public Property enabled As Boolean
        Public Property nazwa As String
        Public Property engine As Vblib.AutotaggerBase
        Public Property maxCount As Integer
        Public Property count As Integer
        Public Property dymekCount As String
        Public Property dymekAbout As String

    End Class
End Class

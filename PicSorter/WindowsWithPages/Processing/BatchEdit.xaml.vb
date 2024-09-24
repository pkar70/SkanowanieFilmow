

' ma pokazać listę autotag engines, dla każdego policzyć ile zdjęc jest nieotagowanych

Imports vb14 = Vblib.pkarlibmodule14
Imports pkar.UI.Extensions

Public Class BatchEdit

    Private _lista As List(Of JedenEngine)
    Private _iMax As Integer

    Private Async Sub uiGetAll_Click(sender As Object, e As RoutedEventArgs)
        If Not Await Me.DialogBoxYNAsync("Aplikować wszystkie zaznaczone mechanizmy?") Then Return

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

            Await ApplyOne(oSrc)
            uiProgBarEngines.Value += 1
            ProcessPic.GetBuffer(Me).SaveData()  ' bo zmieniono EXIF
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
        ProcessPic.GetBuffer(Me).SaveData()  ' bo zmieniono EXIF
        Window_Loaded(Nothing, Nothing)
    End Sub

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        Me.InitDialogs

        _lista = New List(Of JedenEngine)
        _iMax = ProcessPic.GetBuffer(Me).Count

        For Each oEngine As Vblib.PostProcBase In Vblib.gPostProcesory
            If oEngine.Nazwa.Contains("("c) Then Continue For

            Dim oNew As New JedenEngine
            oNew.nazwa = oEngine.Nazwa
            oNew.engine = oEngine
            oNew.dymekAbout = oEngine.dymekAbout

            _lista.Add(oNew)
        Next

        uiLista.ItemsSource = _lista
    End Sub


    Private Async Function ApplyOne(oSrc As JedenEngine) As Task

        uiProgBarInEngine.Maximum = _iMax
        uiProgBarInEngine.Value = 0
        uiProgBarInEngine.Visibility = Visibility.Visible

        For Each oItem As Vblib.OnePic In ProcessPic.GetBuffer(Me).GetList
            If Not IO.File.Exists(oItem.InBufferPathName) Then Continue For   ' zabezpieczenie przed samoznikaniem

            ' *TODO* później będzie dokładniej może, typu pytanie o Exif, i tak dalej
            Await oSrc.engine.Apply(oItem, False, "")

            Await Task.Delay(1) ' na wszelki wypadek, żeby był czas na przerysowanie progbar, nawet jak tworzenie EXIFa jest empty
            uiProgBarInEngine.Value += 1
        Next

        uiProgBarInEngine.Visibility = Visibility.Collapsed

    End Function


    Public Class JedenEngine
        Public Property enabled As Boolean
        Public Property nazwa As String
        Public Property engine As Vblib.PostProcBase
        Public Property dymekAbout As String

    End Class
End Class

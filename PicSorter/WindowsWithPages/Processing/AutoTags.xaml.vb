

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

    Private Shared Function PoliczUstawione(nazwa As String) As Integer
        Dim iCnt As Integer = 0

        For Each oItem As Vblib.OnePic In Application.GetBuffer.GetList
            If oItem.GetExifOfType(nazwa) IsNot Nothing Then iCnt += 1
        Next

        Return iCnt
    End Function

    Private Async Function ApplyOne(oSrc As JedenEngine) As Task

        Application.ShowWait(True)

        uiProgBarInEngine.Maximum = oSrc.maxCount
        uiProgBarInEngine.Value = 0
        uiProgBarInEngine.Visibility = Visibility.Visible

        If oSrc.engine.Nazwa = "AUTO_GUID" Then
            If Application.GetBuffer.GetList.Where(Function(x) Not String.IsNullOrWhiteSpace(x.PicGuid)).Any Then
                If Await vb14.DialogBoxYNAsync("Istnieją przydzielone GUIDy, skasować je?") Then
                    For Each oPic As Vblib.OnePic In Application.GetBuffer.GetList
                        oPic.PicGuid = ""
                        oPic.RemoveExifOfType(Vblib.ExifSource.AutoGuid)
                    Next
                End If
            End If
        End If

        Dim maxGuard As Integer = If(oSrc.engine.Nazwa = "AUTO_AZURE", vb14.GetSettingsInt("uiAzureMaxBatch", 500), Integer.MaxValue)

        For Each oItem As Vblib.OnePic In Application.GetBuffer.GetList
            If Not IO.File.Exists(oItem.InBufferPathName) Then Continue For   ' zabezpieczenie przed samoznikaniem

            ' tu dodawać True Or jakby miały być powtarzane przebiegi po zmianie w kodzie
            If oItem.GetExifOfType(oSrc.nazwa) Is Nothing Then
                Dim oExif As Vblib.ExifTag = Await oSrc.engine.GetForFile(oItem)
                If oExif IsNot Nothing Then
                    oItem.ReplaceOrAddExif(oExif)
                    oItem.TagsChanged = True
                End If
                Await Task.Delay(3) ' na wszelki wypadek, żeby był czas na przerysowanie progbar, nawet jak tworzenie EXIFa jest empty
                maxGuard -= 1
                If maxGuard < 1 Then Exit For
            End If
            uiProgBarInEngine.Value += 1
        Next

        ' 2024.02.03, jakby nie było GUID w OnePic a był w OnePic.Exif(AutoGuid), to go przekopiuj
        If oSrc.engine.Nazwa = "AUTO_GUID" Then
            For Each oItem As Vblib.OnePic In Application.GetBuffer.GetList
                If String.IsNullOrWhiteSpace(oItem.PicGuid) Then
                    Dim oExif As Vblib.ExifTag = oItem.GetExifOfType(Vblib.ExifSource.AutoGuid)
                    If oExif IsNot Nothing Then
                        oItem.PicGuid = oExif.PicGuid
                        oItem.TagsChanged = True
                    End If
                End If
            Next
        End If


        uiProgBarInEngine.Visibility = Visibility.Collapsed

        Application.ShowWait(False)

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

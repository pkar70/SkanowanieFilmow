

' ma pokazać listę autotag engines, dla każdego policzyć ile zdjęc jest nieotagowanych

Imports Windows.ApplicationModel.Activation
Imports vb14 = Vblib.pkarlibmodule14
Imports pkar.UI.Extensions
Imports Auto_std2_Astro
Imports Vblib
Imports System.Threading
Imports pkar
Imports System.ComponentModel

Public Class AutoTags

    Private _lista As ObservableList(Of JedenEngine)
    Private _stopArchiving As Boolean

    Private Const MAX_BATCH_SAVE_METADATA As Integer = 100

    Private Async Sub uiGetAll_Click(sender As Object, e As RoutedEventArgs)

        If uiGetAll.Content = " STOP " Then
            _stopArchiving = True
            Me.ProgRingSetText("stopping")
            Return
        End If

        Dim iSelected As Integer = 0
        For Each oSrc As JedenEngine In _lista
            If oSrc.enabled Then iSelected += 1
        Next

        If iSelected < 1 Then
            Me.MsgBox("Żaden mechanizm nie jest zaznaczony")
            Return
        End If

        If Not Await Me.DialogBoxYNAsync("Aplikować wszystkie zaznaczone mechanizmy?") Then Return


        uiProgBarEngines.Maximum = iSelected
        uiProgBarEngines.Value = 0
        uiProgBarEngines.Visibility = Visibility.Visible

        uiGetAll.IsEnabled = False

        For Each oSrc As JedenEngine In _lista
            If Not oSrc.enabled Then Continue For

            uiProgBarEngines.ToolTip = oSrc.nazwa

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

        Dim prevCount As Integer = PoliczUstawione(oSrc.nazwa)
        Await ApplyOne(oSrc)

        If prevCount <> PoliczUstawione(oSrc.nazwa) Then
            ProcessPic.GetBuffer(Me).SaveData()  ' bo zmieniono EXIF
            Window_Loaded(Nothing, Nothing)
        Else
            Me.MsgBox("Nic się nie zmieniło... Pewnie mechanizm jest nieaplikowalny do pozostałych zdjęć.")
        End If

    End Sub

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        Me.InitDialogs
        Me.ProgRingInit(True, True)

        Dim tryb As Boolean = Vblib.GetSettingsBool("uiAutotagsExact")
        UstawListe(tryb)
    End Sub

    Private Sub UstawListe(tryb As Boolean)


        ' najpierw pokazujemy to co nie wymaga liczenia
        _lista = New ObservableList(Of JedenEngine)
        Dim iMax As Integer = ProcessPic.GetBuffer(Me).Count
        Dim autoSelect As String = Vblib.GetSettingsString("uiDefaultAutoTags")

        For Each oEngine As Vblib.AutotaggerBase In Vblib.gAutoTagery
            Dim oNew As New JedenEngine
            oNew.maxCount = iMax
            oNew.enabled = autoSelect.Contains(oEngine.Nazwa & "|")
            oNew.nazwa = oEngine.Nazwa
            oNew.engine = oEngine
            oNew.dymekAbout = oEngine.DymekAbout

            _lista.Add(oNew)
        Next
        uiLista.ItemsSource = _lista

        Thread.Sleep(10)    ' pokaż na ekranie


        ' teraz dopiero wyliczamy

        Me.ProgRingShow(True)
        Me.ProgRingSetText(If(tryb, "liczę dokładnie...", "sprawdzam"))

        For Each oEngine As JedenEngine In _lista

            If tryb Then
                UstawDymekCountAllDone(oEngine)
            Else
                UstawDymekCount(oEngine)
            End If

            If oEngine.allDone Then oEngine.dymekCount &= " (komplet)"

            oEngine.NotifyPropChange("count")
            oEngine.NotifyPropChange("dymekCount")
            oEngine.NotifyPropChange("count")
        Next

        Me.ProgRingShow(False)
        Me.ProgRingSetText("")

    End Sub

    Private Sub UstawDymekCount(oNew As JedenEngine)
        oNew.count = PoliczUstawione(oNew.nazwa)
        oNew.dymekCount = vbCrLf & oNew.count & "/" & oNew.maxCount
        oNew.allDone = (oNew.count >= oNew.maxCount)

        ' to jest zbyt długie - szukanie geotag tak często
        'Dim iNotPossible As Integer = ProcessPic.GetBuffer(Me).GetList.
        '    Where(Function(x) x.GetExifOfType(oNew.nazwa) IsNot Nothing).
        'Where(Function(x) oNew.engine.CanTag(x)).Count

    End Sub


    Private Sub UstawDymekCountAllDone(oNew As JedenEngine)

        Dim countJest As Integer = 0
        Dim countNieUmie As Integer = 0

        Dim starttime = Date.Now

        For Each oItem As Vblib.OnePic In ProcessPic.GetBuffer(Me).GetList
            If oItem.GetExifOfType(oNew.nazwa) IsNot Nothing Then
                countJest += 1
            Else
                If Not oNew.engine.CanTag(oItem) Then countNieUmie += 1
            End If
        Next

        'oNew.count = PoliczUstawione(oNew.nazwa)
        'Dim iNotPossible As Integer = ProcessPic.GetBuffer(Me).GetList.
        '    Where(Function(x) x.GetExifOfType(oNew.engine.CanTag(x)).
        'Where(Function(x) oNew.engine.CanTag(x)).Count
        oNew.count = countJest

        If countNieUmie > 0 Then
            oNew.dymekCount = $"{oNew.count} + {countNieUmie} = {oNew.count + countNieUmie} / {oNew.maxCount}"
        Else
            oNew.dymekCount = $"{oNew.count} / {oNew.maxCount}"
        End If

        If oNew.count + countNieUmie >= oNew.maxCount Then
            oNew.allDone = True
        Else
            oNew.allDone = False
        End If


        Vblib.DumpMessage($"UstawWszystko({oNew.nazwa}), liczenie zajęło {(Date.Now - starttime).ToStringDHMS}")

    End Sub
    Private Function PoliczUstawione(nazwa As String) As Integer

        Dim liczyk As Func(Of OnePic, Boolean)

        If nazwa = ExifSource.AutoGuid Then
            ' przypadek specjalny: liczy w OnePic
            liczyk = Function(x) Not String.IsNullOrWhiteSpace(x.PicGuid)
        Else
            liczyk = Function(x) x.GetExifOfType(nazwa) IsNot Nothing
        End If

        Return ProcessPic.GetBuffer(Me).GetList.Where(liczyk).Count

    End Function

    Private Async Function ApplyOne(oSrc As JedenEngine) As Task

        ' Application.ShowWait(True)
        Me.ProgRingShow(True)
        Me.ProgRingSetText(oSrc.engine.Nazwa)

        Me.ProgRingSetMax(oSrc.maxCount)
        Me.ProgRingSetVal(0)

        uiGetAll.Content = " STOP "

        ' musi być tutaj, bo inaczej pierwszy błąd z GetForFile kończy sprawdzanie :) 
        Auto_AzureTest._AzureExceptionsGuard = 2 ' po 4 exception ma przestać sprawdzać

        Dim maxGuard As Integer = Integer.MaxValue
        If oSrc.engine.Nazwa = "AUTO_AZURE" Then
            maxGuard = vb14.GetSettingsInt("uiAzureMaxBatch", 500)
            'Auto_AzureTest._AzureExceptionsGuard = 2 ' po 4 exception ma przestać sprawdzać
            Auto_AzureTest._AzureExceptionMsg = "" ' suma exceptionsow
        End If

        If oSrc.engine.Nazwa = "AUTO_WEATHER" Then
            Auto_Pogoda.maxGuard = vb14.GetSettingsInt("uiVisualCrossMaxBatch", 400)
        End If


        Dim autoTagLock As String = oSrc.engine.GetAutoTagDisableKwd

        Dim iBatchMax As Integer = MAX_BATCH_SAVE_METADATA

        For Each oItem As Vblib.OnePic In ProcessPic.GetBuffer(Me).GetList
            If Not IO.File.Exists(oItem.InBufferPathName) Then Continue For   ' zabezpieczenie przed samoznikaniem

            If oItem.GetSumOfDescriptionsKwds.Contains(autoTagLock) Then
                vb14.DumpMessage("Skippin because " & autoTagLock)
                Continue For
            End If

            ' tu dodawać True Or jakby miały być powtarzane przebiegi po zmianie w kodzie
            If oItem.GetExifOfType(oSrc.nazwa) Is Nothing Then

                ' dla AUTO_GUID zawsze wejdzie, ale to obsłużymy w GetForFile
                Dim oExif As Vblib.ExifTag = Await Task.Run(Async Function() Await oSrc.engine.GetForFile(oItem))
                ' dla AUTO_GUID będzie NULL, ale i tak zmienione tagi są

                If oExif IsNot Nothing Then
                    oItem.ReplaceOrAddExif(oExif)
                    oItem.TagsChanged = True
                Else
                    If Auto_AzureTest._AzureExceptionsGuard < 1 Then Exit For  ' po 4 exception ma przestać sprawdzać
                End If

                If oSrc.engine.IsWeb Then
                    iBatchMax -= 1
                    If iBatchMax < 0 Then
                        ProcessPic.GetBuffer(Me).SaveData()
                        iBatchMax = MAX_BATCH_SAVE_METADATA
                    End If
                End If

                Await Task.Delay(3) ' na wszelki wypadek, żeby był czas na przerysowanie progbar, nawet jak tworzenie EXIFa jest empty
                maxGuard -= 1
                If maxGuard < 1 Then Exit For
            End If
            Me.ProgRingInc

            If _stopArchiving Then Exit For
        Next

        'uiProgBarInEngine.Visibility = Visibility.Collapsed

        ' Application.ShowWait(False)
        Me.ProgRingShow(False)

        If Not String.IsNullOrEmpty(Auto_AzureTest._AzureExceptionMsg) Then
            Me.MsgBox(Auto_AzureTest._AzureExceptionMsg)
        End If

        uiGetAll.Content = " Run selected "



    End Function

    Private Async Sub uiRemoveTags_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oSrc As JedenEngine = oFE?.DataContext
        If oSrc Is Nothing Then Return

        If Not Await Me.DialogBoxYNAsync($"Naprawdę usunąć znacznik {oSrc.nazwa} z wszystkich zdjęć w buforze?") Then Return

        'If oSrc.nazwa = ExifSource.AutoGuid Then
        '    If Application.GetBuffer.GetList.Any(
        '        Function(x)
        '            Return Not String.IsNullOrWhiteSpace(x.PicGuid) AndAlso (Not String.IsNullOrWhiteSpace(x.CloudArchived) OrElse Not String.IsNullOrWhiteSpace(x.Archived))
        '        End Function) Then

        '        If Not Await Me.DialogBoxYNAsync("Mogę usunąć GUID/serno tylko z tych zdjęć, które jeszcze nie zostały zarchiwizowane. Kontynuować?") Then Return
        '    End If
        'End If

        ProcessPic.GetBuffer(Me).GetList.ForEach(Sub(x) x.RemoveExifOfType(oSrc.nazwa))

        'If oSrc.nazwa = ExifSource.AutoGuid Then
        '    UsunGUIDandSerNo()
        'End If

        ProcessPic.GetBuffer(Me).SaveData()

    End Sub

    '''' <summary>
    '''' Usuwa PicGuid oraz SerNo, ale tylko tam gdzie nie jest zarchiwizowane
    '''' </summary>
    'Private Shared Sub UsunGUIDandSerNo()
    '    Application.GetBuffer.GetList.ForEach(
    '        Sub(x)
    '            If String.IsNullOrWhiteSpace(x.CloudArchived) AndAlso String.IsNullOrWhiteSpace(x.Archived) Then
    '                x.PicGuid = Nothing
    '                x.serno = 0
    '            End If
    '        End Sub)
    '    ' i możemy nadawać od następnego po ostatnim wysłanym do archiwum
    '    vb14.SetSettingsInt("serNoLastAssigned", vb14.GetSettingsInt("serNoLastArchived"))
    'End Sub

    Public Class JedenEngine
        Implements ComponentModel.INotifyPropertyChanged

        Public Property enabled As Boolean
        Public Property nazwa As String
        'Public Property ineticon As String
        Public Property engine As Vblib.AutotaggerBase
        Public Property maxCount As Integer
        Public Property count As Integer
        Public Property dymekCount As String
        Public Property dymekAbout As String

        Public Property allDone As Boolean = True

        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

        Public Sub NotifyPropChange(propertyName As String)
            ' ale do niektórych to onepic się zmienia, więc niby rekurencyjnie powinno być :)
            Dim evChProp As New PropertyChangedEventArgs(propertyName)
            RaiseEvent PropertyChanged(Me, evChProp)
        End Sub

    End Class

End Class

Public Class ConverterNegate
    Inherits ValueConverterOneWaySimple

    Protected Overrides Function Convert(value As Object) As Object
        Dim bulek As Boolean = CType(value, Boolean)
        Return Not bulek
    End Function
End Class
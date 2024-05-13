

' ma pokazać listę autotag engines, dla każdego policzyć ile zdjęc jest nieotagowanych

Imports Windows.ApplicationModel.Activation
Imports vb14 = Vblib.pkarlibmodule14
Imports pkar.UI.Extensions
Imports Auto_std2_Astro
Imports Vblib

Public Class AutoTags

    Private _lista As List(Of JedenEngine)

    Private Async Sub uiGetAll_Click(sender As Object, e As RoutedEventArgs)

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

        Dim prevCount As Integer = PoliczUstawione(oSrc.nazwa)
        Await ApplyOne(oSrc)

        If prevCount <> PoliczUstawione(oSrc.nazwa) Then
            Application.GetBuffer.SaveData()  ' bo zmieniono EXIF
            Window_Loaded(Nothing, Nothing)
        Else
            Me.msgbox("Nic się nie zmieniło... Pewnie mechanizm jest nieaplikowalny do pozostałych zdjęć.")
        End If

    End Sub

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        Me.InitDialogs
        Me.ProgRingInit(True, False)

        _lista = New List(Of JedenEngine)
        Dim iMax As Integer = Application.GetBuffer.Count

        For Each oEngine As Vblib.AutotaggerBase In Application.gAutoTagery
            Dim oNew As New JedenEngine
            oNew.nazwa = oEngine.Nazwa
            oNew.ineticon = If(oEngine.IsWeb, "🌍", "")
            oNew.engine = oEngine
            oNew.maxCount = iMax
            oNew.count = PoliczUstawione(oNew.nazwa)
            oNew.dymekCount = vbCrLf & oNew.count & "/" & oNew.maxCount
            If oNew.count = iMax Then
                oNew.enabled = False
                oNew.dymekCount &= " (komplet)"
            End If
            oNew.dymekAbout = oEngine.DymekAbout

            _lista.Add(oNew)
        Next

        uiLista.ItemsSource = _lista
    End Sub

    Private Shared Function PoliczUstawione(nazwa As String) As Integer

        Dim liczyk As Func(Of OnePic, Boolean)

        If nazwa = ExifSource.AutoGuid Then
            ' przypadek specjalny: liczy w OnePic
            liczyk = Function(x) Not String.IsNullOrWhiteSpace(x.PicGuid)
        Else
            liczyk = Function(x) x.GetExifOfType(nazwa) IsNot Nothing
        End If

        Return Application.GetBuffer.GetList.Where(liczyk).Count

    End Function

    Private Async Function ApplyOne(oSrc As JedenEngine) As Task

        ' Application.ShowWait(True)
        Me.ProgRingShow(True)
        Me.ProgRingSetText(oSrc.engine.Nazwa)

        uiProgBarInEngine.Maximum = oSrc.maxCount
        uiProgBarInEngine.Value = 0
        uiProgBarInEngine.Visibility = Visibility.Visible

        'If oSrc.engine.Nazwa = "AUTO_GUID" Then
        '    Dim bAny As Boolean = Application.GetBuffer.GetList.Any(Function(x) Not String.IsNullOrWhiteSpace(x.PicGuid))
        '    If bAny Then
        '        Dim bFixed As Boolean = Application.GetBuffer.GetList.Any(Function(x) Not String.IsNullOrWhiteSpace(x.PicGuid) AndAlso (Not String.IsNullOrEmpty(x.CloudArchived) OrElse Not String.IsNullOrEmpty(x.Archived)))

        '        Dim bKasuj As Boolean = False
        '        If Not bFixed Then
        '            bKasuj = Await Me.DialogBoxYNAsync("Istnieją przydzielone GUIDy, i tylko w plikach z których mogę usunąć, skasować je?")
        '        Else
        '            Dim bEditable As Boolean = Application.GetBuffer.GetList.Any(Function(x) Not String.IsNullOrWhiteSpace(x.PicGuid) AndAlso String.IsNullOrEmpty(x.CloudArchived) AndAlso String.IsNullOrEmpty(x.Archived))
        '            If bEditable Then
        '                bKasuj = Await Me.DialogBoxYNAsync("Istnieją przydzielone GUIDy, i tylko w plikach z których tylko NIEKTÓRE mogę usunąć, skasować je?")
        '            End If
        '        End If

        '        If bKasuj Then
        '            UsunGUIDandSerNo()
        '        End If
        '    End If
        'End If

        Dim maxGuard As Integer = Integer.MaxValue
        If oSrc.engine.Nazwa = "AUTO_AZURE" Then
            maxGuard = vb14.GetSettingsInt("uiAzureMaxBatch", 500)
            Auto_AzureTest._AzureExceptionsGuard = 2 ' po 4 exception ma przestać sprawdzać
            Auto_AzureTest._AzureExceptionMsg = "" ' suma exceptionsow
        End If

        If oSrc.engine.Nazwa = "AUTO_WEATHER" Then
            Auto_Pogoda.maxGuard = vb14.GetSettingsInt("uiVisualCrossMaxBatch", 400)
        End If


        Dim autoTagLock As String = GetAutoTagDisableKwd(oSrc)

        For Each oItem As Vblib.OnePic In Application.GetBuffer.GetList
            If Not IO.File.Exists(oItem.InBufferPathName) Then Continue For   ' zabezpieczenie przed samoznikaniem

            If oItem.GetSumOfDescriptionsKwds.Contains(autoTagLock) Then
                vb14.DumpMessage("Skippin because " & autoTagLock)
                Continue For
            End If

            ' tu dodawać True Or jakby miały być powtarzane przebiegi po zmianie w kodzie
            If oItem.GetExifOfType(oSrc.nazwa) Is Nothing Then
                ' dla AUTO_GUID zawsze wejdzie, ale to obsłużymy w GetForFile
                Dim oExif As Vblib.ExifTag = Await oSrc.engine.GetForFile(oItem)
                ' dla AUTO_GUID będzie NULL, ale i tak zmienione tagi są
                If oExif IsNot Nothing Then
                    oItem.ReplaceOrAddExif(oExif)
                    oItem.TagsChanged = True
                Else
                    If Auto_AzureTest._AzureExceptionsGuard < 1 Then Exit For  ' po 4 exception ma przestać sprawdzać
                End If
                Await Task.Delay(3) ' na wszelki wypadek, żeby był czas na przerysowanie progbar, nawet jak tworzenie EXIFa jest empty
                maxGuard -= 1
                If maxGuard < 1 Then Exit For
            End If
            uiProgBarInEngine.Value += 1
        Next

        uiProgBarInEngine.Visibility = Visibility.Collapsed

        ' Application.ShowWait(False)
        Me.ProgRingShow(False)

        If Not String.IsNullOrEmpty(Auto_AzureTest._AzureExceptionMsg) Then
            Me.MsgBox(Auto_AzureTest._AzureExceptionMsg)
        End If




    End Function

    Private Function GetAutoTagDisableKwd(oSrc As JedenEngine) As String
        Dim ret As String = oSrc.nazwa
        Dim iInd As Integer = ret.IndexOf("_")
        If iInd < 1 Then Return "=NO:" & ret
        Return "=NO:" & ret.Substring(iInd + 1)
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

        Application.GetBuffer.GetList.ForEach(Sub(x) x.RemoveExifOfType(oSrc.nazwa))

        'If oSrc.nazwa = ExifSource.AutoGuid Then
        '    UsunGUIDandSerNo()
        'End If

        Application.GetBuffer.SaveData()

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
        Public Property enabled As Boolean
        Public Property nazwa As String
        Public Property ineticon As String
        Public Property engine As Vblib.AutotaggerBase
        Public Property maxCount As Integer
        Public Property count As Integer
        Public Property dymekCount As String
        Public Property dymekAbout As String

        Public Property allDone As Boolean = True
    End Class

End Class

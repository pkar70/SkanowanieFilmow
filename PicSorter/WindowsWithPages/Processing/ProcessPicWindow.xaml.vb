
Imports vb14 = Vblib.pkarlibmodule14
Imports pkar.UI.Extensions
Imports pkar.DotNetExtensions

Class ProcessPic

    Private _buforek As Vblib.BufferSortowania
    Private _isDefaultBuff As Boolean
    Private _afterInit As Boolean

    ' rozmiar okna - zob. AktualizujGuzikiSharingu
    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        Vblib.DumpCurrMethod()
        Me.InitDialogs
        Me.ProgRingInit(True, False)

        _buforek = vblib.GetBuffer
        _isDefaultBuff = True

        WypelnListeBuforow()
        _afterInit = True

        Window_Activated(Nothing, Nothing)

    End Sub


    Private Async Sub Window_Activated(sender As Object, e As EventArgs)
        Vblib.DumpCurrMethod()

        AktualizujGuziki()

        Await MozeClearPustyBufor()
    End Sub

    Private _niekasujArchived As Boolean

    Private Async Function MozeClearPustyBufor() As Task

        ' czyli wróć jeśli coś jest do archiwizacji
        If uiLocalArch.IsEnabled Then Return

        If _niekasujArchived Then Return

        ' wróć jeśli bufor jest pusty
        If _buforek.Count < 1 Then Return

        ' i jeszcze prosty test: bo może nic do archiwizacji, jako że nic nie ma targetDir ustalonego
        ' krótki test, jakby następny nie był optymalizowany
        If String.IsNullOrWhiteSpace(_buforek.GetList.ElementAt(0).TargetDir) Then Return

        ' lepszy test robimy: przeglądamy wszystkie TargetDiry
        If _buforek.GetList.Any(Function(x) String.IsNullOrEmpty(x.TargetDir)) Then Return

        If Not Await Me.DialogBoxYNAsync("Wszystkie pliki są w pełni zarchiwizowane, wyczyścić bufor?") Then
            _niekasujArchived = True
            Return
        End If

        ' skasowanie wszystkich plików z katalogu bufora - szybsze niż iterowanie JSON

        Me.ProgRingShow(True)
        Me.ProgRingSetText("Removing files...")

        _buforek.RemoveAllFiles()

        Me.ProgRingShow(True)

        AktualizujGuziki()

    End Function


    Private Function CountDoPublishing() As Integer

        Dim iCnt As Integer = 0
        For Each oPic As Vblib.OnePic In _buforek.GetList
            If String.IsNullOrWhiteSpace(oPic.TargetDir) Then Continue For  ' bo musimy wiedzieć gdzie wstawiać
            iCnt += oPic.CountPublishingWaiting
        Next

        Return iCnt

    End Function


    ''' <summary>
    ''' Aktualizuje liczbę zdjęć w guzikach, zmienia też guziki Sharingu
    ''' </summary>
    ''' <returns></returns>
    Private Sub AktualizujGuziki()

        If _buforek Is Nothing Then Return

        If _isDefaultBuff Then AktualizujGuzikiSharingu()

        ' z licznika z bufora
        Dim counter As Integer = _buforek.Count
        uiBrowse.Content = $"Browse ({counter})"
        uiAutotag.Content = $"Try autotag ({counter})"

        uiAutotag.IsEnabled = (counter > 0)
        uiBatchEdit.IsEnabled = (counter > 0)

        ' z licznika do archiwizacji
        counter = CountDoArchiwizacji(_buforek)
        uiLocalArch.Content = $"Local arch ({counter})"
        uiLocalArch.IsEnabled = (counter > 0)

        ' z licznika do web archiwizacji
        counter = _buforek.CountDoCloudArchiwizacji(Application.GetCloudArchives.GetList)
        uiCloudArch.Content = $"Cloud arch ({counter})"
        uiCloudArch.IsEnabled = (counter > 0)

        ' oraz bez licznika
        counter = CountDoPublishing()
        uiPublish.Content = $"Publish ({counter})"
        uiPublish.IsEnabled = (counter > 0)

    End Sub

    ''' <summary>
    ''' włącza/wyłącza guziki sharingu, odpowiednio zmieniając rozmiar okna
    ''' </summary>
    Private Sub AktualizujGuzikiSharingu()
        Dim wysok As Integer = 490

        'uiSharingRetrieve.Visibility = Visibility.Collapsed
        uiSharingDescrips.Visibility = Visibility.Collapsed

        'If Application.GetShareServers.Count > 0 Then
        '    uiSharingRetrieve.Visibility = Visibility.Visible
        '    wysok += 40
        'End If

        If vblib.GetShareDescriptionsIn.Count + vblib.GetShareDescriptionsOut.Count > 0 Then
            uiSharingDescrips.Content = $"Upload descrs ({vblib.GetShareDescriptionsOut.Count})"
            uiSharingDescrips.Visibility = Visibility.Visible
            wysok += 40
        End If

        Me.Height = wysok

    End Sub

    Private Sub uiBrowse_Click(sender As Object, e As RoutedEventArgs)
        PokazSubWindow(New ProcessBrowse(_buforek, _buforek.GetBufferName))
        AktualizujGuziki()
    End Sub

    Private Sub uiAutotag_Click(sender As Object, e As RoutedEventArgs)
        PokazSubWindow(New AutoTags)
    End Sub

    Private Sub uiBatchEdit_Click(sender As Object, e As RoutedEventArgs)
        PokazSubWindow(New BatchEdit)
    End Sub

    Private Sub uiLocalArch_Click(sender As Object, e As RoutedEventArgs)
        PokazSubWindow(New LocalArchive) ' multibuff raczej OK
    End Sub

    Private Sub uiCloudPublish_Click(sender As Object, e As RoutedEventArgs)
        PokazSubWindow(New CloudPublishing) ' multibuff raczej OK
    End Sub

    Private Sub uiCloudArch_Click(sender As Object, e As RoutedEventArgs)
        PokazSubWindow(New CloudArchiving) ' multibuff raczej OK
    End Sub

    Private Sub uiSequence_Click(sender As Object, e As RoutedEventArgs)
        PokazSubWindow(New SequenceHelperList) ' multibuff OK
    End Sub

    Private Sub uiRetrieve_Click(sender As Object, e As RoutedEventArgs)
        PokazSubWindow(New ProcessDownload) ' multibuff raczej OK
    End Sub

    Private Sub uiSharingDescrips_Click(sender As Object, e As RoutedEventArgs)
        PokazSubWindow(New ShareSendDescQueue) ' multibuff OK
    End Sub

    Private Sub uiSharingRetrieve_Click(sender As Object, e As RoutedEventArgs)

    End Sub

    Private Sub PokazSubWindow(okno As Window)
        ' zablokuj wybieranie/zmianę bufora
        uiZmiennikBufora.IsEnabled = False

        okno.Owner = Me
        okno.Show()
    End Sub
#Region "Multibuforowość"

    Public Function GetCurrentBuffer() As Vblib.BufferSortowania
        ' dla pod-okien
        Return _buforek
    End Function

    Public Function IsDefaultBuff() As Boolean
        Return _isDefaultBuff
    End Function


    Private Sub WypelnListeBuforow()

        uiBufory.Items.Clear()

        ' ten jest zawsze
        uiBufory.Items.Add(New ComboBoxItem With {.Content = "(default)", .IsSelected = True})

        Dim sFolder As String = vb14.GetSettingsString("uiFolderBuffer")
        If String.IsNullOrWhiteSpace(sFolder) Then Return
        If Not IO.Directory.Exists(sFolder) Then Return

        For Each direk As String In IO.Directory.GetDirectories(sFolder)
            Dim bufname As String = IO.Path.GetFileName(direk)
            If Not IsValidBufferName(bufname) Then Continue For

            uiBufory.Items.Add(New ComboBoxItem With {.Content = bufname})
        Next

    End Sub

    ''' <summary>
    ''' sprawdza czy dirname może być nazwą bufora (czy istnieje taki katalog, oraz plik indeksu do niego (u.FOLDER.json))
    ''' </summary>
    Private Function IsValidBufferName(dirname As String) As Boolean
        Dim sFolder As String = vb14.GetSettingsString("uiFolderBuffer")

        If Not IO.Directory.Exists(IO.Path.Combine(sFolder, dirname)) Then Return False

        Return IO.File.Exists(IO.Path.Combine(vblib.GetDataFolder, "u." & dirname & ".json"))
    End Function

    Private Sub uiBufory_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        If Not _afterInit Then Return

        _niekasujArchived = False

        Dim oCBI As ComboBoxItem = uiBufory.SelectedItem
        Dim folderName As String = oCBI?.Content
        If String.IsNullOrWhiteSpace(folderName) Then Return

        If folderName = "(default)" Then
            _buforek = New Vblib.BufferSortowania(Vblib.GetDataFolder)
            _isDefaultBuff = True
        Else
            _buforek = New Vblib.BufferSortowania(Vblib.GetDataFolder, folderName)
            _isDefaultBuff = False
        End If

        AktualizujGuziki()

    End Sub


    Private Async Sub uiAddBuff_Click(sender As Object, e As RoutedEventArgs)
        Dim newFolder As String = Await Me.InputBoxAsync("Podaj nazwę nowego bufora")
        If String.IsNullOrWhiteSpace(newFolder) Then Return

        If newFolder <> newFolder.ToPOSIXportableFilename Then
            Me.MsgBox("Nazwa zawiera niedozwolone znaki")
            Return
        End If

        If IsValidBufferName(newFolder) Then
            Me.MsgBox("Taki bufor już istnieje!")
            Return
        End If

        ' *TODO* załóż nowy bufor
        Dim sFolder As String = vb14.GetSettingsString("uiFolderBuffer")
        IO.Directory.CreateDirectory(IO.Path.Combine(sFolder, newFolder))

        _buforek = New Vblib.BufferSortowania(vblib.GetDataFolder, newFolder)

        ' musi być zapis, bo musi być plik do guzików
        '_buforek.SaveData()  - ale nie przejdzie, bo nie robi Save gdy count = 0
        Dim nowyCBI = New ComboBoxItem With {.Content = newFolder}
        uiBufory.Items.Add(nowyCBI)
        uiBufory.SelectedItem = nowyCBI

        _isDefaultBuff = False
        AktualizujGuziki()


    End Sub

#Region "callbacki dostępu do bufora"
    Public Shared Function GetBuffer(oWnd As Window) As Vblib.BufferSortowania
        Dim procPic As ProcessPic = GetOwner(oWnd)
        Return procPic?.GetCurrentBuffer
    End Function

    Public Shared Function IsDefaultBuff(oWnd As Window) As Boolean
        Dim procPic As ProcessPic = GetOwner(oWnd)
        If procPic Is Nothing Then Return False
        Return procPic.IsDefaultBuff
    End Function

    Public Shared Function GetOwner(oWnd As Window) As ProcessPic
        Dim procPic As ProcessPic = TryCast(oWnd.Owner, ProcessPic)
        If procPic Is Nothing Then
            Vblib.MsgBox("Nie ma ownera, lub nie jest to ProcessPic, nie mam skąd wziąć bufora!")
            Return Nothing
        End If

        Return procPic
    End Function

    Public Shared Async Function CheckSerNo(oWnd As Window) As Task(Of Boolean)
        Dim procPic As ProcessPic = GetOwner(oWnd)
        If procPic Is Nothing Then Return False

        Dim errname As String = procPic.GetCurrentBuffer.CheckSerNo
        If errname = "" Then Return True

        Await vb14.MsgBoxAsync($"Są zdjęcia bez serno, nie mogę kontynuować! ({errname}")
        Return False
    End Function

    Public Shared Function CountWithTargetDir(oWnd As Window) As Integer
        Dim procPic As ProcessPic = GetOwner(oWnd)
        If procPic Is Nothing Then Return -1

        Return procPic.GetCurrentBuffer.CountWithTargetDir

    End Function

    Public Shared Function CountDoArchiwizacji(oWnd As Window) As Integer

        Dim procPic As ProcessPic = GetOwner(oWnd)
        If procPic Is Nothing Then Return -1

        Return CountDoArchiwizacji(procPic.GetCurrentBuffer())

    End Function

    Private Shared Function CountDoArchiwizacji(lista As Vblib.BufferSortowania) As Integer

        Dim currentArchs As New List(Of String)
        Application.GetArchivesList.ForEach(Sub(x) If x.enabled Then currentArchs.Add(x.StorageName.ToLowerInvariant))

        Return lista.CountDoArchiwizacji(currentArchs)

    End Function




#End Region


#End Region


End Class


Imports Vblib
Imports pkar.UI.Extensions
Imports pkar.UI.Configs
Imports Windows.Storage
Imports pkar.DotNetExtensions
Imports Windows.Storage.Streams

Public NotInheritable Class MainPage
    Inherits Page

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        pkar.UI.Configs.InitSettings("", True, Nothing)
        Me.ProgRingInit(True, True)

        lib14_httpClnt.httpKlient._machineName = GetHostName()    ' musi być tak, bo lib jest też używana w std 1.4, a tam nie ma machinename

        If Not UpdateUiInitial() Then
            uiSettings_Click(Nothing, Nothing)
        End If
    End Sub

    Private Function UpdateUiInitial() As Boolean

        uiLastLocalUploadTime.GetSettingsString

        If Not String.IsNullOrWhiteSpace(uiLastLocalUploadTime.Text) Then
            PoliczZdjeciaOd(uiLastLocalUploadTime.Text, "(local)")
        End If


        Dim serwer As String = Vblib.GetSettingsString("linkFromQRcode")

        If String.IsNullOrWhiteSpace(serwer) Then
            uiUploadGrid.Visibility = Visibility.Collapsed
            Return False
        End If

        lib14_httpClnt.httpKlient.MS_SetServer(serwer)
        uiUploadGrid.Visibility = Visibility.Visible
        Return True

    End Function

    Private Async Sub uiLastUpload_Click(sender As Object, e As RoutedEventArgs)
        Await KiedySerwerMialImport()
    End Sub

    Private Async Function KiedySerwerMialImport() As Task
        Try

            Dim lastimport As String = Await lib14_httpClnt.httpKlient.MS_GetLastImport
            If lastimport.NotStartsWith("OK") Then
                Me.MsgBox("Server error:" & vbCrLf & lastimport)
                Return
            End If

            uiLastSrvrUploadTime.Text = lastimport.Substring(3)
            PoliczZdjeciaOd(lastimport.Substring(3), "(srvr)")
        Catch ex As Exception
            Me.MsgBox("Error checking server")
        End Try
    End Function

    Private Shared _countNew As Integer

    Private Async Sub PoliczZdjeciaOd(odDaty As String, dymek As String)
        ' data w formacie Exif

        ' "yyyy.MM.dd HH:mm:ss" -> 20240922_13_04_30
        odDaty = odDaty.Replace(".", "")    ' 20240922 13:04:30
        odDaty = odDaty.Replace(":", "_")   ' 20240922 13_04_30
        odDaty = odDaty.Replace(" ", "_")   ' 20240922_13_04_30

        odDaty = "WP_" & odDaty

        'Await Me.MsgBoxAsync("odDaty=" & odDaty)

        Dim oFold As StorageFolder = GetPhotoDir()
        'Await Me.MsgBoxAsync("folder=" & oFold.Path)

        Dim iSize As Integer = 0
        _countNew = 0

        Try
            For Each oFile As StorageFile In Await oFold.GetFilesAsync
                If oFile.Name.NotStartsWith("WP_") Then Continue For

                'Await Me.MsgBoxAsync($"{oFile.Name} ? {odDaty}")
                If oFile.Name.Substring(0, odDaty.Length) > odDaty Then
                    Dim basProp As FileProperties.BasicProperties = Await oFile.GetBasicPropertiesAsync
                    If basProp IsNot Nothing Then
                        iSize += (Await oFile.GetBasicPropertiesAsync).Size / 1024 + 1
                    End If
                    _countNew += 1
                End If
            Next
        Catch ex As Exception
            uiNewPics.Text = $"FAIL counting"
            Me.MsgBox("FAIL counting" & vbCr & ex.Message)
        End Try

        If _countNew > 0 Then
            iSize = iSize / 1024 + 1
            uiNewPics.Text = $"{_countNew} ({iSize} MiB)"
        Else
            uiNewPics.Text = $"0 (no new pics)"
        End If

        ToolTipService.SetToolTip(uiNewPics, dymek)
        uiNewPicsSrc.Text = dymek
    End Sub

    Private Async Sub uiUpload_Click(sender As Object, e As RoutedEventArgs)

        Me.ProgRingShow(True)
        uiUpload.IsEnabled = False

        If String.IsNullOrWhiteSpace(uiLastSrvrUploadTime.Text) Then
            If Await Me.DialogBoxYNAsync("Nie odpytałeś serwera o lastupload, zrobić to teraz?") Then
                Await KiedySerwerMialImport()
            End If
        End If


        Dim ret As Boolean = Await MS_UploadPics()
        Me.ProgRingShow(False)
        uiUpload.IsEnabled = True

        ' jeśli był błądu, to koniec
        If Not ret Then Return

        If Not Await Me.DialogBoxYNAsync("Transfer udany! :)" & vbCrLf & "Obsłużyć purge?") Then Return

        Await PurgePics()
    End Sub

    Private Async Function PurgePics() As Task

        Dim purgeList As String = Await lib14_httpClnt.httpKlient.MS_GetPurgeList

        ' *TODO* skasowanie plików wedle niego

        ' ale to narzuca 7 dni, nie m tu dostępu do dni purgowania
        Dim zwloka As Integer = Await lib14_httpClnt.httpKlient.MS_GetPurgeDelay
        If zwloka < 0 Then zwloka = 7 * 24
        Dim sPurgeDate As String = Date.Now.AddHours(zwloka).ToString("yyyyMMdd.HHmm")

        Dim folder As StorageFolder = GetPhotoDir()

        For Each purgeEntryLp As String In purgeList.Split(vbCrLf)
            Dim purgeEntry As String = purgeEntryLp.Trim

            If purgeEntry > sPurgeDate Then Continue For

            Dim iInd As Integer = purgeEntry.IndexOf(vbTab)
            If iInd < 2 Then
                Me.MsgBox("Błędny plik PURGE!")
                Return
            End If

            Dim plik As StorageFile = Await folder.TryGetItemAsync(purgeEntry.Substring(iInd))
            If plik IsNot Nothing Then Await plik.DeleteAsync
        Next

        Dim ret As String = Await lib14_httpClnt.httpKlient.MS_ClearPurgeList

        If ret.NotStartsWith("OK") Then
            Me.MsgBox("FAIL clearing purge list:" & vbCrLf & ret)
        End If

    End Function




#Region "QRcode"

    Private Async Function TryScanBarCode(oDispatch As Windows.UI.Core.CoreDispatcher, bAllFormats As Boolean) As Task(Of ZXing.Result)

        Dim oScanner As New ZXing.Mobile.MobileBarcodeScanner(oDispatch)
        'Tell our scanner to use the default overlay 
        oScanner.UseCustomOverlay = False
        ' //We can customize the top And bottom text of our default overlay 
        oScanner.TopText = "Ustaw barcode w polu widzenia" ' "Hold camera up to barcode"
        oScanner.BottomText = "Kod zostanie rozpoznany automatycznie" & vbCrLf & "Użyj 'back' by anulować" ' "Camera will automatically scan barcode" & vbCrLf & "Press the 'Back' button to Cancel"
        Dim oRes As ZXing.Result = Await oScanner.Scan()

        If oRes Is Nothing Then Return Nothing

        If oRes.BarcodeFormat = ZXing.BarcodeFormat.QR_CODE Then Return oRes

        Me.MsgBox("To nie jest QRcode!")
        Return Nothing
    End Function


    Private Async Function Skanowanie() As Task(Of ZXing.Result)
        ' kod paskowy do fotografowania
        Dim oRes As ZXing.Result = Await TryScanBarCode(Me.Dispatcher, False)

        ' ominiecie bledu? ale wczesniej (WezPigulka) bylo dobrze? Teraz jest 0:MainPage 1:Details
        'If Me.Frame.BackStack.Count > 0 Then
        '    If Me.Frame.BackStack.ElementAt(Me.Frame.BackStack.Count - 1).GetType Is Me.GetType Then
        '        Me.Frame.BackStack.RemoveAt(Me.Frame.BackStack.Count - 1)
        '    End If
        'End If

        Return oRes

    End Function
    Private Async Sub uiSettings_Click(sender As Object, e As RoutedEventArgs)
        Dim oRes As ZXing.Result = Await Skanowanie()
        If oRes.Text = "" Then Return

        Vblib.SetSettingsString("linkFromQRcode", oRes.Text)
        lib14_httpClnt.httpKlient.MS_SetServer(oRes.Text)

        UpdateUiInitial()
    End Sub
#End Region


    Private Async Function MS_UploadPics() As Task(Of Boolean)
        If _countNew < 1 Then Return True

        Me.ProgRingSetVal(0)
        Me.ProgRingSetMax(_countNew)

        Dim sRet As String = uiLastSrvrUploadTime.Text
        Dim lastimport As String = sRet.Replace(".", "")    ' 20240922 13:04:30
        lastimport = lastimport.Replace(":", "_")   ' 20240922 13_04_30
        lastimport = lastimport.Replace(" ", "_")   ' 20240922_13_04_30
        lastimport = "WP_" & lastimport

        Dim oFold As StorageFolder = GetPhotoDir()

        Dim lastName As String

        For Each oFile As StorageFile In Await oFold.GetFilesAsync
            If oFile.Name > lastimport Then
                lastName = oFile.Name

                Try

                    Using strumyk As IRandomAccessStream = Await oFile.OpenAsync(FileAccessMode.Read)
                        Using memstrim As New MemoryStream
                            strumyk.AsStreamForRead.CopyTo(memstrim)
                            memstrim.Position = 0
                            sRet = Await lib14_httpClnt.httpKlient.MS_sendPic(lastName, memstrim)
                        End Using

                        'Await Me.MsgBoxAsync("After sendPic: " & sRet)

                        ' ignorujemy ten plik
                        If sRet.StartsWith("DONT WANT") Then Continue For

                        If sRet.NotStartsWith("OK") Then
                                Me.MsgBox($"Błąd wysyłania zdjęcia {lastName}: " & vbCrLf & sRet)
                                Return False
                            End If
                    End Using
                Catch ex As Exception

                    Me.MsgBox("CATCH: " & ex.Message)

                End Try

                Me.ProgRingInc
            End If
        Next

        sRet = Await lib14_httpClnt.httpKlient.MS_SignalEndOfPics
        If sRet.NotStartsWith("OK") Then
            Me.MsgBox("Błąd po wysyłaniu zdjęć" & vbCrLf & sRet)
            Return False
        End If

        Vblib.SetSettingsDate("uiLastUploadTime", Date.Now)

        Return True

    End Function


    Private Function GetPhotoDir() As StorageFolder
        Return KnownFolders.CameraRoll
    End Function


    Public Shared Function GetHostName() As String
        Dim hostNames As IReadOnlyList(Of Windows.Networking.HostName) =
                Windows.Networking.Connectivity.NetworkInformation.GetHostNames()
        For Each oItem As Windows.Networking.HostName In hostNames
            If oItem.DisplayName.Contains(".local") Then
                Return oItem.DisplayName.Replace(".local", "")
            End If
        Next
        Return ""
    End Function
End Class

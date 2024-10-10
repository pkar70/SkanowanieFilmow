
Imports Vblib
Imports pkar.UI.Extensions
Imports Windows.Storage

Public NotInheritable Class MainPageOld
    Inherits Page

    Private _countNew As Integer = -1

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        pkar.UI.Configs.InitSettings("", True, Nothing)
        lib14_httpClnt.httpKlient._machineName = GetHostName()    ' musi być tak, bo lib jest też używana w std 1.4, a tam nie ma machinename

        UpdateUiUpload()

        Me.ProgRingInit(True, True)
    End Sub

    Private Sub UpdateUiUpload()

        If String.IsNullOrWhiteSpace(GetSettingsString("uiServer")) Then
            uiUploadGrid.Visibility = Visibility.Collapsed
        Else
            uiUploadGrid.Visibility = Visibility.Visible
            Dim lastupload As DateTimeOffset = Vblib.GetSettingsDate("uiLastUploadTime", New Date(1970, 1, 1))
            uiLastUploadTime.Text = lastupload.ToString("ddd, dd-MM-yyyy HH:mm")

            AktualizujCounter(lastupload) ' może sobie aktualizować w tle przecież
        End If

    End Sub


    Private Async Sub uiUpload_Click(sender As Object, e As RoutedEventArgs)

        Me.ProgRingShow(True)
        uiUpload.IsEnabled = False
        Dim oServer As ShareServer = ShareServer.CreateFromLink(Vblib.GetSettingsString("uiServer"))

        Dim lastDate As Date = Date.Now

        If Await UploadPics(oServer) Then
            ' data sprzed początku wysyłania - bo może będzie zdezaktualizowane (foto podczas wysyłania)
            SetSettingsDate("uiLastUploadTime", lastDate)
            Await PurgePics(oServer)
        End If

        Me.ProgRingShow(False)
        uiUpload.IsEnabled = True


    End Sub

    Private Async Function PurgePics(oServer As ShareServer) As Task
        If Not Vblib.GetSettingsBool("uiUsePurge") Then Return

        Dim sRet As String = Await lib14_httpClnt.httpKlient.PurgeIsMaintained(oServer)
        If Not sRet.StartsWith("YES") Then
            Me.MsgBox("Serwer nie ma listy do purge:" & vbCrLf & sRet)
            Return
        End If

        sRet = Await lib14_httpClnt.httpKlient.PurgeGetList(oServer)
        Dim entries As String() = sRet.Split(vbCrLf)

        ' ale to narzuca 7 dni, nie m tu dostępu do dni purgowania
        Dim sPurgeDate As String = Date.Now.AddDays(-7).ToString("yyyyMMdd.HHmm")

        For Each purgeEntry As String In entries
            If Not purgeEntry.Contains(lib14_httpClnt.httpKlient._machineName) Then Continue For

            If purgeEntry > sPurgeDate Then Continue For

            Dim iInd As Integer = purgeEntry.IndexOf(vbTab)
            If iInd < 2 Then
                Me.MsgBox("Błędny plik PURGE!")
                Return
            End If
            ' kasowanie gdy data niepoprawna
            ' DeleteFile(sFile.Substring(iInd + 1))
        Next


    End Function

    Private Async Function UploadPics(oServer As ShareServer) As Task(Of Boolean)
        If _countNew < 1 Then Return True

        Me.ProgRingSetVal(0)
        Me.ProgRingSetMax(_countNew)

        Dim sRet As String = Await lib14_httpClnt.httpKlient.TryConnect(oServer)
        If Not sRet.StartsWith("OK") Then
            Me.MsgBox("Nie mogę się połączyć z serwerem:" & vbCrLf & sRet)
            Return False
        End If

        sRet = Await lib14_httpClnt.httpKlient.CanUpload(oServer)
        If Not sRet.StartsWith("YES") Then
            Me.MsgBox("Serwer nie chce zdjęć:" & vbCrLf & sRet)
            Return False
        End If

        Dim lastupload As DateTimeOffset = Vblib.GetSettingsDate("uiLastUploadTime", New Date(1970, 1, 1))
        Dim oFold As StorageFolder = GetPhotoDir()
        Dim iCnt As Integer = 0

        Dim exif As Vblib.ExifTag = CreateExifSource()

        For Each oFile As StorageFile In Await oFold.GetFilesAsync
            If oFile.DateCreated <= lastupload Then Continue For
            ' tylko dla Windows - wp_
            If Not oFile.Name.StartsWith("WP_") Then Continue For

            Dim oOnePic As Vblib.OnePic = Await CreateOnePic(oFile, exif)
            If oOnePic Is Nothing Then
                Me.MsgBox($"Nie udało się stworzyć metadnych dla zdjęcia")
                Return False
            End If

            sRet = Await lib14_httpClnt.httpKlient.UploadPic(oServer, oOnePic)
            If Not sRet.StartsWith("OK") Then
                Me.MsgBox($"Błąd wysyłania zdjęcia {oOnePic.sSuggestedFilename}: " & vbCrLf & sRet)
                Return False
            End If

            Me.ProgRingInc
        Next
        Vblib.SetSettingsDate("uiLastUploadTime", Date.Now)

        Return True

    End Function

    Private Sub uiSettings_Click(sender As Object, e As RoutedEventArgs)
        Me.Navigate(GetType(SettingsOld))
    End Sub

    Private Async Function CreateOnePic(oFile As StorageFile, exif As Vblib.ExifTag) As Task(Of Vblib.OnePic)
        Dim oPic As New Vblib.OnePic
        oPic.Exifs.Add(exif)

        ' plik do pipelineoutput (jakby po pipeline)
        oPic._PipelineOutput = (Await oFile.OpenAsync(FileAccessMode.Read)).AsStream

        oPic.sSourceName = lib14_httpClnt.httpKlient._machineName
        oPic.sSuggestedFilename = oFile.Name
        oPic.sInSourceID = oPic.sSourceName & ":" & oPic.sSuggestedFilename  ' potrzebne dla purge

        Dim newExif As Vblib.ExifTag = New ExifTag(Vblib.ExifSource.SourceFile)
        newExif.DateMin = oFile.DateCreated.Date
        newExif.DateMax = (Await oFile.GetBasicPropertiesAsync).DateModified.Date
        oPic.Exifs.Add(newExif)

        Return oPic
    End Function

    Private Function CreateExifSource() As Vblib.ExifTag
        Dim exif As New ExifTag(Vblib.ExifSource.SourceDefault) With
            {
            .FileSourceDeviceType = FileSourceDeviceTypeEnum.digital,
            .Author = Vblib.GetSettingsString("uiAutor"),
            .Copyright = Vblib.GetSettingsString("uiCopyr")
            }

        Dim camera As String = Vblib.GetSettingsString("uiCamera")
        If Not String.IsNullOrWhiteSpace(camera) Then exif.CameraModel = camera

        Return exif

    End Function

    Private Function GetPhotoDir() As StorageFolder
        Dim oFold = KnownFolders.CameraRoll
        Return oFold

        'library = Await Windows.Storage.StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures)
        'For Each oFold In library.Folders
        '    Debug.WriteLine(oFold.Path)
        'Next
    End Function

    Private Async Function AktualizujCounter(lastupload As DateTimeOffset) As Task

        Dim oFold As StorageFolder = GetPhotoDir()
        Dim iCnt As Integer = 0
        Dim iSize As Integer = 0

        For Each oFile As StorageFile In Await oFold.GetFilesAsync
            If oFile.DateCreated > lastupload Then
                iCnt += 1
                iSize += (Await oFile.GetBasicPropertiesAsync).Size / 1024 + 1
            End If
        Next

        uiNewPics.Text = $"{iCnt} ({iSize} KiB)"
        _countNew = iCnt
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

Imports vb14 = Vblib.pkarlibmodule14
Imports pkar

Public NotInheritable Class Settings
    Inherits Page

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        uiVersion.ShowAppVers

        Dim sLastDate As String = Vblib.GetSettingsString("lastOneDriveCheck")
        If String.IsNullOrWhiteSpace(sLastDate) Then
            uiDataTimestamp.Text = "(no data)"
        Else
            uiDataTimestamp.Text = sLastDate

            FillComboArchives(uiArchives, Vblib.GetSettingsString("currentArchive"))
        End If
    End Sub

    Private Async Sub uiCheckOneDrive_Click(sender As Object, e As RoutedEventArgs)

        If mODroot Is Nothing Then mODroot = Await ODclient.GetRootAsync()
        'Dim cosik = Await mODroot.GetItemsAsStringsAsync(True, True)

        Dim sMsg As String = ""
        If Await CopyOneFileFromOneDriveIfNewer(App.GetDataFolder, MainPage.CLOUDARCH_FILENAME) Then
            sMsg = sMsg & "Plik z archiwami DOWNLOADED" & vbCrLf
        Else
            sMsg = sMsg & "Plik z archiwami AKTUALNTY" & vbCrLf
        End If

        If Await CopyOneFileFromOneDriveIfNewer(App.GetDataFolder, "dirstree.json") Then
            sMsg = sMsg & "Plik z katalogami DOWNLOADED" & vbCrLf
        Else
            sMsg = sMsg & "Plik z katalogami AKTUALNTY" & vbCrLf
        End If

        MainPage.GetCloudArchives() ' tu będzie LOAD

        FillComboArchives(uiArchives, Vblib.GetSettingsString("currentArchive"))

        Vblib.SetSettingsString("lastOneDriveCheck", Date.Now.ToExifString)

        vb14.DialogBox(sMsg)

    End Sub

    Private Sub uiOk_Click(sender As Object, e As RoutedEventArgs)
        ' jeśli mamy wybrane z ComboBoxa, to trzeba to wykorzystać
        If uiArchives.SelectedIndex > -1 Then
            Dim sCurrVal As String = TryCast(uiArchives.SelectedItem, ComboBoxItem).Content
            Vblib.SetSettingsString("currentArchive", sCurrVal)
        End If

        Me.GoBack
    End Sub

    Private Sub FillComboArchives(archivesCombo As ComboBox, selectedArchive As String)
        archivesCombo.Items.Clear()

        For Each oItem As Vblib.CloudConfig In MainPage.GetCloudArchives
            Dim oNew As New ComboBoxItem
            oNew.Content = oItem.nazwa
            oNew.DataContext = oItem
            If oItem.nazwa.ToLowerInvariant = selectedArchive.ToLowerInvariant Then
                oNew.IsSelected = True
            End If
            archivesCombo.Items.Add(oNew)
        Next

    End Sub

#Region "onedrive"
    ' z ListaZakupowa/Shoppingcards/mainpage , zmodyfikowane

    Private Shared mODroot As ODfolder ' nie może być tu inicjalizowane, bo potrzebuje UI do logowania do OneDrive

    ''' <summary>
    ''' Próba wczytania pliku z OneDrive
    ''' </summary>
    ''' <param name="sDstFolder">dokąd skopiować (katalog danych programu)</param>
    ''' <param name="sFilename">jaki plik</param>
    ''' <returns>False: nic nowego, True: skopiowano nowszy</returns>
    Private Async Function CopyOneFileFromOneDriveIfNewer(sDstFolder As String, sFilename As String) As Task(Of Boolean)
        vb14.DumpCurrMethod(sFilename & " do folderu " & sDstFolder)

        Dim sDstFile As String = IO.Path.Combine(sDstFolder, sFilename)

        'Dim oRoamFile As Windows.Storage.StorageFile
        Dim oODfile As ODfile = Nothing
        oODfile = Await mODroot.GetFileAsync(sFilename)
        If oODfile Is Nothing Then
            vb14.DumpMessage("tego pliku nie ma w OneDrive")
            Return False ' nie ma pliku w OneDrive, to go nie kopiujemy
        End If

        Dim oDTO As DateTimeOffset = oODfile.GetLastModDate

        If Not IO.File.Exists(sDstFile) Then
            vb14.DumpMessage("kopiuję - bo tego pliku nie ma lokalnie")
        Else
            If IO.File.GetLastWriteTime(sDstFile).AddSeconds(5) > oDTO Then
                Return False ' plik w OneDrive nie jest nowszy od lokalnej kopii
            End If
        End If

        ' no to kopiujemy
        Using oStreamOneDrive = Await oODfile.GetStreamAsync
            Using oStreamRoaming = IO.File.OpenWrite(sDstFile)
                oStreamOneDrive.CopyTo(oStreamRoaming)
                oStreamRoaming.Flush()
            End Using
        End Using

        IO.File.SetLastWriteTime(sDstFile, New Date(oDTO.Ticks))

        Return True

    End Function



#End Region

End Class


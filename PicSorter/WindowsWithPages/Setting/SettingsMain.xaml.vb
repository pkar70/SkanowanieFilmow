﻿Imports System.IO.Compression
Imports System.Reflection
Imports vb14 = Vblib.pkarlibmodule14
Imports pkar.UI.Extensions

' 2023.09.18, 663 px ma ekran settingsów
Class SettingsMain

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        Me.InitDialogs
        Me.ProgRingInit(True, False)

        uiVersion.ShowAppVers(True)
        If vb14.GetSettingsString("uiFolderBuffer") = "" Then
            uiGlobalSett_Click(Nothing, Nothing)
        End If
    End Sub

    Private Sub uiGlobalSett_Click(sender As Object, e As RoutedEventArgs)
        Me.NavigationService.Navigate(New SettingsGlobal)
    End Sub

    Private Sub uiSettSources_Click(sender As Object, e As RoutedEventArgs)
        Me.NavigationService.Navigate(New SettingsSources)
    End Sub

    Private Sub uiArchives_Click(sender As Object, e As RoutedEventArgs)
        Me.NavigationService.Navigate(New SettingsArchive)
    End Sub

    Private Sub uiCloudArchive_Click(sender As Object, e As RoutedEventArgs)
        Me.NavigationService.Navigate(New SettingsCloudArchive)
    End Sub

    Private Sub uiCloudPublish_Click(sender As Object, e As RoutedEventArgs)
        Me.NavigationService.Navigate(New SettingsCloudPublisher)
    End Sub



    Private Sub uiDirTree_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New SettingsDirTree(True)
        oWnd.ShowDialog()
    End Sub

    Private Sub uiKeywords_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New SettingsKeywords
        oWnd.ShowDialog()
        ' Me.NavigationService.Navigate(New SettingsKeywords)
    End Sub

    Private Sub uiAutoTags_Click(sender As Object, e As RoutedEventArgs)
        Me.NavigationService.Navigate(New SettingsAutoTags)
    End Sub


    Private Sub uiListSett_Click(sender As Object, e As RoutedEventArgs)
        Me.NavigationService.Navigate(New SettingListy)
    End Sub
    Private Sub uiMiscSett_Click(sender As Object, e As RoutedEventArgs)
        Me.NavigationService.Navigate(New SettingsMisc)
    End Sub

    Private Async Sub uiBackup_Click(sender As Object, e As RoutedEventArgs)

        Dim oPicker As New Microsoft.Win32.SaveFileDialog
        oPicker.Title = "Select directory for backup"
        oPicker.FileName = "picsorter." & Date.Now.ToString("yyyyMMdd") & ".zip"
        oPicker.CheckPathExists = True
        oPicker.InitialDirectory = Application.GetDataFolder

        ' Show open file dialog box
        Dim result? As Boolean = oPicker.ShowDialog()

        ' Process open file dialog box results
        If result <> True Then Return

        Dim filename As String = oPicker.FileName

        Me.ProgRingShow(True)

        Me.ProgRingSetText("database backup...")
        ' np. dump SQLa, i tym podobne rzeczy, z baz; zawsze robi LOAD bazy.
        Await Task.Run(Sub() Application.gDbase.PreBackup())

        ' jednak można do tego samego katalogu, bo pakujemy tylko JSON i TXT
        'If IO.Path.GetDirectoryName(filename).ToLowerInvariant = Application.GetDataFolder.ToLowerInvariant Then
        '    vb14.DialogBox("Nie można robić backup do katalogu z konfiguracją")
        '    Return
        'End If

        Me.ProgRingSetText("packing to ZIP file...")

        IO.File.Delete(filename)    ' nie daje Exception na nieistnienie pliku; potwierdzenie zapisania ZAMIAST jest przy okienku SaveDialog

        Await Task.Run(Sub()
                           Dim oArchive = IO.Compression.ZipFile.Open(filename, IO.Compression.ZipArchiveMode.Create)
                           For Each sFile As String In IO.Directory.GetFiles(Application.GetDataFolder, "*.txt")
                               oArchive.CreateEntryFromFile(sFile, IO.Path.GetFileName(sFile))
                           Next
                           For Each sFile As String In IO.Directory.GetFiles(Application.GetDataFolder, "*.json")
                               oArchive.CreateEntryFromFile(sFile, IO.Path.GetFileName(sFile))
                           Next

                           oArchive.Dispose()

                           Me.MsgBox("Archiwum konfiguracji i metadanych utworzone.")
                           ' System.IO.Compression.ZipFile.CreateFromDirectory()
                       End Sub
                )

        Me.ProgRingShow(False)

    End Sub

    Private Sub uiSlideshow_Click(sender As Object, e As RoutedEventArgs)
        Me.NavigationService.Navigate(New SettingsSlideshow)
    End Sub

    Private Sub uiSharingSett_Click(sender As Object, e As RoutedEventArgs)
        Me.NavigationService.Navigate(New SettingsShare)
    End Sub
End Class

﻿
Imports System.Linq.Expressions
Imports vb14 = Vblib.pkarlibmodule14

Class SettingsGlobal

    Private Sub FolderBrowser(oBox As TextBox, sDefaultDir As String)
        Dim oPicker As New Microsoft.Win32.SaveFileDialog
        oPicker.FileName = "none" ' Default file name
        oPicker.CheckPathExists = True
        If IO.Directory.Exists(oBox.Text) Then
            oPicker.InitialDirectory = oBox.Text
        Else
            oPicker.InitialDirectory = sDefaultDir
        End If

        ' Show open file dialog box
        Dim result? As Boolean = oPicker.ShowDialog()

        ' Process open file dialog box results
        If result <> True Then Return

        Dim filename As String = oPicker.FileName
        oBox.Text = IO.Path.GetDirectoryName(filename)
    End Sub


    Private Sub uiBrowseDataFolder(sender As Object, e As RoutedEventArgs)
        Dim sPathLocal As String = uiFolderData.Text
        If Not IO.Directory.Exists(sPathLocal) Then
            Dim sAppName As String = Application.Current.MainWindow.GetType().Assembly.GetName.Name
            Dim sLocalAppData As String = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            sPathLocal = IO.Path.Combine(sLocalAppData, sAppName)
            IO.Directory.CreateDirectory(sPathLocal)
        End If
        FolderBrowser(uiFolderData, sPathLocal)
    End Sub

    Private Sub uiBrowseBufferFolder(sender As Object, e As RoutedEventArgs)
        Dim iMax As Long = 0
        Dim sPath As String = ""

        Dim oDrives = IO.DriveInfo.GetDrives()
        For Each oDrive In oDrives
            If oDrive.DriveType = IO.DriveType.Fixed And oDrive.AvailableFreeSpace > iMax Then
                iMax = oDrive.AvailableFreeSpace
                sPath = oDrive.Name
            End If
        Next

        If iMax < 512 * 1024 * 1024 Then
            vb14.DialogBox("Nie masz dysku z choćby 512 MB?")
            Return
        End If

        FolderBrowser(uiFolderBuffer, sPath)
    End Sub

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        uiFolderBuffer.GetSettingsString()
        uiFolderData.GetSettingsString()
    End Sub

    Private Function CheckDirsExists(oTBox1 As TextBox, oTBox2 As TextBox) As Boolean
        If Not IO.Directory.Exists(oTBox1.Text) Then
            oTBox1.Text = ""
            Return False
        End If
        If Not IO.Directory.Exists(oTBox2.Text) Then
            oTBox2.Text = ""
            Return False
        End If
        Return True
    End Function

    Private Function CheckDirsOnFixed(oTBox1 As TextBox, oTBox2 As TextBox) As Boolean
        If Not (New IO.DriveInfo(oTBox1.Text)).DriveType = IO.DriveType.Fixed Then
            oTBox1.Text = ""
            Return False
        End If
        If Not (New IO.DriveInfo(oTBox2.Text)).DriveType = IO.DriveType.Fixed Then
            oTBox2.Text = ""
            Return False
        End If
        Return True
    End Function


    Private Sub uiOK_Click(sender As Object, e As RoutedEventArgs)

        If Not CheckDirsExists(uiFolderData, uiFolderBuffer) Then
            vb14.DialogBox("ERROR: popraw katalogi - muszą istnieć")
            Return
        End If

        If Not CheckDirsOnFixed(uiFolderData, uiFolderBuffer) Then
            vb14.DialogBox("ERROR: popraw katalogi - muszą być na dyskach wewnętrznych")
            Return
        End If

        uiFolderBuffer.SetSettingsString()
        uiFolderData.SetSettingsString()

        Me.NavigationService.GoBack()
    End Sub
End Class

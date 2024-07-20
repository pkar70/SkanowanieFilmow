
Imports pkar.UI.Configs.Extensions
Imports pkar.UI.Extensions

Class SettingsGlobal

    Public Shared Function FileSaveBrowser(sDefaultDir As String, sTitle As String, defaultFile As String) As String
        Dim oPicker As New Microsoft.Win32.SaveFileDialog
        oPicker.FileName = defaultFile ' Default file name
        oPicker.Title = sTitle
        oPicker.CheckPathExists = True
        oPicker.InitialDirectory = sDefaultDir

        ' Show open file dialog box
        Dim result? As Boolean = oPicker.ShowDialog()

        ' Process open file dialog box results
        If result <> True Then Return ""

        Return oPicker.FileName
    End Function

    Public Shared Function FolderBrowser(sDefaultDir As String, sTitle As String) As String

        Dim filename As String = FileSaveBrowser(sDefaultDir, sTitle, "none")
        If String.IsNullOrWhiteSpace(filename) Then Return ""
        Return IO.Path.GetDirectoryName(filename)
    End Function

    Public Shared Function FileOpenBrowser(sDefaultDir As String, sTitle As String, defaultFile As String) As String
        Dim oPicker As New Microsoft.Win32.OpenFileDialog
        oPicker.FileName = defaultFile ' Default file name
        oPicker.Title = sTitle
        oPicker.CheckPathExists = True
        oPicker.InitialDirectory = sDefaultDir

        ' Show open file dialog box
        Dim result? As Boolean = oPicker.ShowDialog()

        ' Process open file dialog box results
        If result <> True Then Return ""

        Return oPicker.FileName
    End Function


    Public Shared Sub FolderBrowser(oBox As TextBox, sDefaultDir As String, sTitle As String)

        Dim sDir As String
        If IO.Directory.Exists(oBox.Text) Then
            sDir = oBox.Text
        Else
            sDir = sDefaultDir
        End If

        sDir = FolderBrowser(sDir, sTitle)
        If String.IsNullOrWhiteSpace(sDir) Then Return

        oBox.Text = sDir
    End Sub


    Private Sub uiBrowseBufferFolder(sender As Object, e As RoutedEventArgs)
        Dim iMax As Long = 0
        Dim sPath As String = ""

        Dim oDrives = IO.DriveInfo.GetDrives()
        For Each oDrive As IO.DriveInfo In oDrives
            If oDrive.DriveType = IO.DriveType.Fixed AndAlso oDrive.IsReady AndAlso oDrive.AvailableFreeSpace > iMax Then
                iMax = oDrive.AvailableFreeSpace
                sPath = oDrive.Name
            End If
        Next

        If iMax < 512 * 1024 * 1024 Then
            Me.MsgBox("Nie masz dysku z choćby 512 MB?")
            Return
        End If

        FolderBrowser(uiFolderBuffer, sPath, "Select folder for buffering photos")
    End Sub

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        Me.InitDialogs

        uiFolderBuffer.GetSettingsString()
        'uiFolderData.GetSettingsString()
        uiUseOneDrive.GetSettingsBool
        'uiIdGUID.GetSettingsBool
        'uiIdSerno.GetSettingsBool
        uiSerNoDigits.GetSettingsInt()
        uiTitleFilename.GetSettingsBool
        uiTitleSerno.GetSettingsBool

    End Sub

    Private Shared Function CheckDirsExists(oTBox2 As TextBox) As Boolean
        'If Not IO.Directory.Exists(oTBox1.Text) Then
        '    oTBox1.Text = ""
        '    Return False
        'End If
        If Not IO.Directory.Exists(oTBox2.Text) Then
            oTBox2.Text = ""
            Return False
        End If
        Return True
    End Function

    Private Shared Function CheckDirsOnFixed(oTBox2 As TextBox) As Boolean
        'If Not (New IO.DriveInfo(oTBox1.Text)).DriveType = IO.DriveType.Fixed Then
        '    oTBox1.Text = ""
        '    Return False
        'End If
        If Not (New IO.DriveInfo(oTBox2.Text)).DriveType = IO.DriveType.Fixed Then
            oTBox2.Text = ""
            Return False
        End If
        Return True
    End Function


    Private Sub uiOK_Click(sender As Object, e As RoutedEventArgs)

        If Not CheckDirsExists(uiFolderBuffer) Then
            Me.MsgBox("ERROR: popraw katalogi - muszą istnieć")
            Return
        End If

        If Not CheckDirsOnFixed(uiFolderBuffer) Then
            Me.MsgBox("ERROR: popraw katalogi - muszą być na dyskach wewnętrznych")
            Return
        End If

        uiFolderBuffer.SetSettingsString()
        'uiFolderData.SetSettingsString()
        uiUseOneDrive.SetSettingsBool

        'uiIdGUID.SetSettingsBool
        'uiIdSerno.SetSettingsBool
        uiSerNoDigits.SetSettingsInt()
        uiTitleFilename.SetSettingsBool
        uiTitleSerno.SetSettingsBool


        Me.NavigationService.GoBack()
    End Sub

    Private Sub uiDbase_Click(sender As Object, e As RoutedEventArgs)
        Me.NavigationService.Navigate(New SettingsDbase)
    End Sub

    Private Sub uiTitle_CheckChange(sender As Object, e As RoutedEventArgs)
        If Not uiTitleSerno.IsChecked AndAlso Not uiTitleFilename.IsChecked Then
            uiTitleSerno.IsChecked = True
            Me.MsgBox("Coś w tytule musi być, przyjmuję że nr seryjny")
        End If
    End Sub
End Class

Public Class KonwersjaDigitsRange
    Inherits ValueConverterOneWaySimple

    Protected Overrides Function Convert(value As Object) As Object
        Dim ile As Integer = CType(value, Integer)

        Select Case ile
            Case 5
                Return "5 (100k)"
            Case 6
                Return "6 (1M)"
            Case 7
                Return "7 (10M)"
            Case 8
                Return "8 (100M)"
            Case Else
                Return ile
        End Select

    End Function
End Class

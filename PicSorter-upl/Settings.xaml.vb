Imports pkar.Uwp.Configs.Extensions
Imports pkar.Uwp.Ext
Imports pkar.DotNetExtensions
Imports Windows.Devices
Imports Vblib

''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>
Public NotInheritable Class Settings
    Inherits Page

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
#If DEBUG Then
        uiVers.ShowAppVers(True)
#Else
        uiVers.ShowAppVers(false)
#End If
        uiAutor.GetSettingsString
        uiCopyr.GetSettingsString
        uiCamera.GetSettingsString
        uiServer.GetSettingsString
        uiUsePurge.GetSettingsBool
        uiLastUploadTime.Text = "Last upload: " & Vblib.GetSettingsDate("uiLastUploadTime", New Date(1970, 1, 1)).ToString("ddd, dd-MM-yyyy HH:mm")
    End Sub

    Private Sub uiOk_Click(sender As Object, e As RoutedEventArgs)
        uiAutor.SetSettingsString
        uiCopyr.SetSettingsString
        uiCamera.SetSettingsString
        uiServer.SetSettingsString
        uiUsePurge.SetSettingsBool

        Me.GoBack
    End Sub

    Private Async Sub uiTry_Click(sender As Object, e As RoutedEventArgs)
        Dim oServer As ShareServer = ShareServer.CreateFromLink(uiServer.Text)

        If oServer.login = Guid.Empty Then
            Me.MsgBox("Nie mam co próbować - " & oServer.serverAddress)
            Return
        End If

        lib14_httpClnt.httpKlient._machineName = MainPage.GetHostName

        Dim sRet As String = Await lib14_httpClnt.httpKlient.TryConnect(oServer)

        Me.MsgBox(sRet)
    End Sub

    Private Sub uiAutor_LostFocus(sender As Object, e As RoutedEventArgs)
        If uiCopyr.Text <> "" AndAlso uiCopyr.Text <> "(c) , All rights reserved." Then Return

        uiCopyr.Text = $"(c) {uiAutor.Text}, All rights reserved."
    End Sub

    Private Sub uiLastUploadTime_DoubleTapped(sender As Object, e As DoubleTappedRoutedEventArgs)
        uiCalPick.Date = Vblib.GetSettingsDate("uiLastUploadTime", New DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero))
        uiCalPick.Visibility = Visibility.Visible
    End Sub

    Private Sub uiCalPick_DateChanged(sender As CalendarDatePicker, args As CalendarDatePickerDateChangedEventArgs)

        If uiCalPick.Date.HasValue Then
            Vblib.SetSettingsDate("uiLastUploadTime", uiCalPick.Date.Value)
        End If
        Page_Loaded(Nothing, Nothing)

    End Sub
End Class


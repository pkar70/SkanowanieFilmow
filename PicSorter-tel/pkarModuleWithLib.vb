' (...)
'            AddHandler rootFrame.NavigationFailed, AddressOf OnNavigationFailed
'
'            ' PKAR added wedle https://stackoverflow.com/questions/39262926/uwp-hardware-back-press-work-correctly-in-mobile-but-error-with-pc
'            AddHandler rootFrame.Navigated, AddressOf OnNavigatedAddBackButton
'            AddHandler Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested, AddressOf OnBackButtonPressed
' (...)


' 2022.04.03: sync z uzupelnionym pkarlibmodule, przerzucenie czesci rzeczy do Extensions

' PLIK DOŁĄCZANY
' założenie: jest VBlib z pkarlibmodule
' mklink pkarModule.vb ..\..\_mojeSuby\pkarModuleWithLib.vb
' PLIK DOŁACZANY

' historia:
' historia.pkarmodule.vb

' 2022.05.02: NetIsIPavail param bMsg jest teraz optional (default: bez pytania)

Imports System
Imports System.Collections.Generic
Imports System.Runtime.CompilerServices
Imports VBlib.Extensions
Imports pkar

Imports MsExtConfig = Microsoft.Extensions.Configuration
Imports MsExtPrim = Microsoft.Extensions.Primitives

Imports WinAppData = Windows.Storage.ApplicationData
Imports Microsoft.Extensions.Configuration

Partial Public Class App
    Inherits Application

#Region "Back button"

    ' PKAR added wedle https://stackoverflow.com/questions/39262926/uwp-hardware-back-press-work-correctly-in-mobile-but-error-with-pc
    Private Sub OnNavigatedAddBackButton(sender As Object, e As NavigationEventArgs)
        Try
            Dim oFrame As Frame = TryCast(sender, Frame)
            If oFrame Is Nothing Then Exit Sub

            Dim oNavig As Windows.UI.Core.SystemNavigationManager = Windows.UI.Core.SystemNavigationManager.GetForCurrentView

            If oFrame.CanGoBack Then
                oNavig.AppViewBackButtonVisibility = Windows.UI.Core.AppViewBackButtonVisibility.Visible
            Else
                oNavig.AppViewBackButtonVisibility = Windows.UI.Core.AppViewBackButtonVisibility.Collapsed
            End If

            Return

        Catch ex As Exception
            pkar.CrashMessageExit("@OnNavigatedAddBackButton", ex.Message)
        End Try

    End Sub

    Private Sub OnBackButtonPressed(sender As Object, e As Windows.UI.Core.BackRequestedEventArgs)
        Try
            TryCast(Window.Current.Content, Frame)?.GoBack()
            e.Handled = True
        Catch ex As Exception
        End Try
    End Sub

#End Region

#Region "RemoteSystem/Background"
    Private moTaskDeferal As Windows.ApplicationModel.Background.BackgroundTaskDeferral = Nothing
    Private moAppConn As Windows.ApplicationModel.AppService.AppServiceConnection
    Private msLocalCmdsHelp As String = ""

    Private Sub RemSysOnServiceClosed(appCon As Windows.ApplicationModel.AppService.AppServiceConnection, args As Windows.ApplicationModel.AppService.AppServiceClosedEventArgs)
        If appCon IsNot Nothing Then appCon.Dispose()
        If moTaskDeferal IsNot Nothing Then
            moTaskDeferal.Complete()
            moTaskDeferal = Nothing
        End If
    End Sub

    Private Sub RemSysOnTaskCanceled(sender As Windows.ApplicationModel.Background.IBackgroundTaskInstance, reason As Windows.ApplicationModel.Background.BackgroundTaskCancellationReason)
        If moTaskDeferal IsNot Nothing Then
            moTaskDeferal.Complete()
            moTaskDeferal = Nothing
        End If
    End Sub

    ''' <summary>
    ''' do sprawdzania w OnBackgroundActivated
    ''' jak zwróci True, to znaczy że nie wolno zwalniać moTaskDeferal !
    ''' sLocalCmdsHelp: tekst do odesłania na HELP
    ''' </summary>
    Public Function RemSysInit(args As BackgroundActivatedEventArgs, sLocalCmdsHelp As String) As Boolean
        Dim oDetails As Windows.ApplicationModel.AppService.AppServiceTriggerDetails =
                TryCast(args.TaskInstance.TriggerDetails, Windows.ApplicationModel.AppService.AppServiceTriggerDetails)
        If oDetails Is Nothing Then Return False

        msLocalCmdsHelp = sLocalCmdsHelp

        AddHandler args.TaskInstance.Canceled, AddressOf RemSysOnTaskCanceled
        moAppConn = oDetails.AppServiceConnection
        AddHandler moAppConn.RequestReceived, AddressOf RemSysOnRequestReceived
        AddHandler moAppConn.ServiceClosed, AddressOf RemSysOnServiceClosed
        Return True

    End Function

    Public Async Function CmdLineOrRemSys(sCommand As String) As Task(Of String)
        Dim sResult As String = AppServiceStdCmd(sCommand, msLocalCmdsHelp)
        If String.IsNullOrEmpty(sResult) Then
            sResult = Await AppServiceLocalCommand(sCommand)
        End If

        Return sResult
    End Function

    Public Async Function ObsluzCommandLine(sCommand As String) As Task

        Dim oFold As Windows.Storage.StorageFolder = Windows.Storage.ApplicationData.Current.TemporaryFolder
        If oFold Is Nothing Then Return

        Dim sLockFilepathname As String = IO.Path.Combine(oFold.Path, "cmdline.lock")
        Dim sResultFilepathname As String = IO.Path.Combine(oFold.Path, "stdout.txt")

        Try
            IO.File.WriteAllText(sLockFilepathname, "lock")
        Catch ex As Exception
            Return
        End Try

        Dim sResult = Await CmdLineOrRemSys(sCommand)
        If String.IsNullOrEmpty(sResult) Then
            sResult = "(empty - probably unrecognized command)"
        End If

        IO.File.WriteAllText(sResultFilepathname, sResult)

        IO.File.Delete(sLockFilepathname)

    End Function

    Private Async Sub RemSysOnRequestReceived(sender As Windows.ApplicationModel.AppService.AppServiceConnection, args As Windows.ApplicationModel.AppService.AppServiceRequestReceivedEventArgs)
        '// 'Get a deferral so we can use an awaitable API to respond to the message 

        Dim sStatus As String
        Dim sResult As String = ""
        Dim messageDeferral As Windows.ApplicationModel.AppService.AppServiceDeferral = args.GetDeferral()

        If VBlib.GetSettingsBool("remoteSystemDisabled") Then
            sStatus = "No permission"
        Else

            Dim oInputMsg As Windows.Foundation.Collections.ValueSet = args.Request.Message

            sStatus = "ERROR while processing command"

            If oInputMsg.ContainsKey("command") Then

                Dim sCommand As String = oInputMsg("command")
                sResult = Await CmdLineOrRemSys(sCommand)
            End If

            If sResult <> "" Then sStatus = "OK"

        End If

        Dim oResultMsg As New Windows.Foundation.Collections.ValueSet()
        oResultMsg.Add("status", sStatus)
        oResultMsg.Add("result", sResult)

        Await args.Request.SendResponseAsync(oResultMsg)

        messageDeferral.Complete()
        moTaskDeferal.Complete()

    End Sub


#End Region

    Public Shared Sub OpenRateIt()
        Dim sUri As New Uri("ms-windows-store://review/?PFN=" & Package.Current.Id.FamilyName)
        sUri.OpenBrowser
    End Sub

End Class

Public Module pkar

    ''' <summary>
    ''' dla starszych: InitLib(Nothing)
    ''' dla nowszych:  InitLib(Environment.GetCommandLineArgs)
    ''' </summary>
    Public Sub InitLib(aCmdLineArgs As List(Of String), Optional bUseOwnFolderIfNotSD As Boolean = True)
        InitSettings(aCmdLineArgs)
        VBlib.LibInitToast(AddressOf FromLibMakeToast)
        VBlib.LibInitDialogBox(AddressOf FromLibDialogBoxAsync, AddressOf FromLibDialogBoxYNAsync, AddressOf FromLibDialogBoxInputAllDirectAsync)

        VBlib.LibInitClip(AddressOf FromLibClipPut, AddressOf FromLibClipPutHtml)
#Disable Warning BC42358 ' Because this call is not awaited, execution of the current method continues before the call is completed
        InitDatalogFolder(bUseOwnFolderIfNotSD)
#Enable Warning BC42358 ' Because this call is not awaited, execution of the current method continues before the call is completed
    End Sub

#Region "CrashMessage"
    ' większość w VBlib

    ''' <summary>
    ''' DialogBox z dotychczasowym logiem i skasowanie logu
    ''' </summary>
    Public Async Function CrashMessageShowAsync() As Task
        Dim sTxt As String = VBlib.GetSettingsString("appFailData")
        If sTxt = "" Then Return
        Await VBlib.DialogBoxAsync("FAIL messages:" & vbCrLf & sTxt)
        VBlib.SetSettingsString("appFailData", "")
    End Function

    ''' <summary>
    ''' Dodaj do logu, ewentualnie toast, i zakończ App
    ''' </summary>
    Public Sub CrashMessageExit(sTxt As String, exMsg As String)
        VBlib.CrashMessageAdd(sTxt, exMsg)
        TryCast(Application.Current, App)?.Exit()
    End Sub

#End Region

    ' -- CLIPBOARD ---------------------------------------------

#Region "ClipBoard"
    Private Sub FromLibClipPut(sTxt As String)
        Dim oClipCont As New DataTransfer.DataPackage With {
            .RequestedOperation = DataTransfer.DataPackageOperation.Copy
        }
        oClipCont.SetText(sTxt)
        DataTransfer.Clipboard.SetContent(oClipCont)
    End Sub

    Private Sub FromLibClipPutHtml(sHtml As String)
        Dim oClipCont As New DataTransfer.DataPackage With {
            .RequestedOperation = DataTransfer.DataPackageOperation.Copy
        }
        oClipCont.SetHtmlFormat(sHtml)
        DataTransfer.Clipboard.SetContent(oClipCont)
    End Sub

    ''' <summary>
    ''' w razie Catch() zwraca ""
    ''' </summary>
    Public Async Function ClipGetAsync() As Task(Of String)
        Dim oClipCont As DataTransfer.DataPackageView = DataTransfer.Clipboard.GetContent
        Try
            Return Await oClipCont.GetTextAsync()
        Catch ex As Exception
            Return ""
        End Try
    End Function
#End Region


    ' -- Get/Set Settings ---------------------------------------------

#Region "Get/Set settings"
    ''' <summary>
    ''' inicjalizacja pełnych zmiennych, bez tego wywołania będą tylko defaulty z pliku INI (i nie będzie pamiętania)
    ''' </summary>
    Private Sub InitSettings(aCmdLineArgs As List(Of String))
        Dim sAppName As String = Windows.ApplicationModel.Package.Current.DisplayName

        'Dim oBuilder As New Microsoft.Extensions.Configuration.ConfigurationBuilder()
        'oBuilder = oBuilder.AddIniRelDebugSettings(Vblib.IniLikeDefaults.sIniContent)   ' defaults.ini w głównym katalogu Project, sekcje [main] oraz [debug]

        ' ale i tak jest Empty
        Dim oDict As IDictionary = Environment.GetEnvironmentVariables()    ' że, w 1.4, zwraca HashTable?
        'oBuilder = oBuilder.AddEnvironmentVariablesROConfigurationSource(sAppName, oDict) ' Environment.GetEnvironmentVariables, Std 2.0
        'oBuilder = oBuilder.AddUwpSettings()
        'oBuilder = oBuilder.AddJsonRwSettings(Windows.Storage.ApplicationData.Current.LocalFolder.Path,
        '                Windows.Storage.ApplicationData.Current.RoamingFolder.Path)
        'If aCmdLineArgs IsNot Nothing Then oBuilder = oBuilder.AddCommandLineRO(aCmdLineArgs)  ' Environment.GetCommandLineArgs, Std 1.5, ale nie w UWP?

        'Dim settings As Microsoft.Extensions.Configuration.IConfigurationRoot = oBuilder.Build

        ' Vblib.LibInitSettings(settings)
        VBlib.InitSettings(sAppName, oDict, New UwpConfigurationSource(),
                           Windows.Storage.ApplicationData.Current.LocalFolder.Path,
                            Windows.Storage.ApplicationData.Current.RoamingFolder.Path, aCmdLineArgs)
    End Sub

#If False Then

#Region "String"

    Private Sub FromLibSetSettings(sName As String, oVal As Object, bRoam As Boolean)
        If oVal.GetType Is GetType(Integer) Then
            FromLibSetSettingsInt(sName, CType(oVal, Integer), bRoam)
            Return
        End If
        If oVal.GetType Is GetType(Long) Then
            FromLibSetSettingsLong(sName, CType(oVal, Long), bRoam)
            Return
        End If
        If oVal.GetType Is GetType(Boolean) Then
            FromLibSetSettingsBool(sName, CType(oVal, Boolean), bRoam)
            Return
        End If
        FromLibSetSettingsString(sName, CType(oVal, String), bRoam)
    End Sub


    Public Function FromLibGetSettingsString(sName As String, sDefault As String) As String
        Dim sTmp As String

        sTmp = sDefault

        With Windows.Storage.ApplicationData.Current
            If .RoamingSettings.Values.ContainsKey(sName) Then
                sTmp = .RoamingSettings.Values(sName).ToString
            End If
            If .LocalSettings.Values.ContainsKey(sName) Then
                sTmp = .LocalSettings.Values(sName).ToString
            End If
        End With

        Return sTmp

    End Function

    Private Function FromLibSetSettingsString(sName As String, sValue As String, Optional bRoam As Boolean = False) As Boolean
        Try
            If bRoam Then Windows.Storage.ApplicationData.Current.RoamingSettings.Values(sName) = sValue
            Windows.Storage.ApplicationData.Current.LocalSettings.Values(sName) = sValue
            Return True
        Catch ex As Exception
            ' jesli przepełniony bufor (za długa zmienna) - nie zapisuj dalszych błędów
            Return False
        End Try
    End Function


#End Region
#Region "Int"
    Public Function FromLibGetSettingsInt(sName As String, iDefault As Integer) As Integer
        Dim sTmp As Integer

        sTmp = iDefault

        With Windows.Storage.ApplicationData.Current
            If .RoamingSettings.Values.ContainsKey(sName) Then
                sTmp = CInt(.RoamingSettings.Values(sName).ToString)
            End If
            If .LocalSettings.Values.ContainsKey(sName) Then
                sTmp = CInt(.LocalSettings.Values(sName).ToString)
            End If
        End With

        Return sTmp

    End Function

    Private Sub FromLibSetSettingsInt(sName As String, sValue As Integer, Optional bRoam As Boolean = False)
        With Windows.Storage.ApplicationData.Current
            If bRoam Then .RoamingSettings.Values(sName) = sValue.ToString
            .LocalSettings.Values(sName) = sValue.ToString
        End With
    End Sub
#End Region
#Region "Long"
    Public Function GetSettingsLongNIEMA(sName As String, Optional iDefault As Long = 0) As Long
        Dim sTmp As Long

        sTmp = iDefault

        With Windows.Storage.ApplicationData.Current
            If .RoamingSettings.Values.ContainsKey(sName) Then
                sTmp = CLng(.RoamingSettings.Values(sName).ToString)
            End If
            If .LocalSettings.Values.ContainsKey(sName) Then
                sTmp = CLng(.LocalSettings.Values(sName).ToString)
            End If
        End With

        Return sTmp

    End Function

    Private Sub FromLibSetSettingsLong(sName As String, sValue As Long, Optional bRoam As Boolean = False)
        With Windows.Storage.ApplicationData.Current
            If bRoam Then .RoamingSettings.Values(sName) = sValue.ToString
            .LocalSettings.Values(sName) = sValue.ToString
        End With
    End Sub
#End Region
#Region "Bool"
    Private Function FromLibGetSettingsBool(sName As String, iDefault As Boolean) As Boolean
        Dim sTmp As Boolean

        sTmp = iDefault
        With Windows.Storage.ApplicationData.Current
            If .RoamingSettings.Values.ContainsKey(sName) Then
                sTmp = CBool(.RoamingSettings.Values(sName).ToString)
            End If
            If .LocalSettings.Values.ContainsKey(sName) Then
                sTmp = CBool(.LocalSettings.Values(sName).ToString)
            End If
        End With

        Return sTmp

    End Function


    Public Sub FromLibSetSettingsBool(sName As String, sValue As Boolean, Optional bRoam As Boolean = False)
        With Windows.Storage.ApplicationData.Current
            If bRoam Then .RoamingSettings.Values(sName) = sValue.ToString
            .LocalSettings.Values(sName) = sValue.ToString
        End With
    End Sub


#End Region
#Region "Date"
    '    Public Sub SetSettingsDate(sName As String)
    '        Dim sValue As String = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
    '        SetSettingsString(sName, sValue)
    '    End Sub


#End Region

#End If

#End Region


    ' -- Testy sieciowe ---------------------------------------------

#Region "testy sieciowe"

    Public Function IsFamilyMobile() As Boolean
        Return (Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily = "Windows.Mobile")
    End Function

    Public Function IsFamilyDesktop() As Boolean
        Return (Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily = "Windows.Desktop")
    End Function


    ' <Obsolete("Jest w .Net Standard 2.0 (lib)")>
    Public Function NetIsIPavailable(Optional bMsg As Boolean = False) As Boolean
        If VBlib.GetSettingsBool("offline") Then Return False

        If Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable() Then Return True
        If bMsg Then
            VBlib.DialogBox("ERROR: no IP network available")
        End If
        Return False
    End Function

    ' <Obsolete("Jest w .Net Standard 2.0 (lib), ale on jest nie do telefonu :)")>
    Public Function NetIsCellInet() As Boolean
        Return Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile().IsWwanConnectionProfile
    End Function


    ' <Obsolete("Jest w .Net Standard 2.0 (lib)")>
    Public Function GetHostName() As String
        Dim hostNames As IReadOnlyList(Of Windows.Networking.HostName) =
                Windows.Networking.Connectivity.NetworkInformation.GetHostNames()
        For Each oItem As Windows.Networking.HostName In hostNames
            If oItem.DisplayName.Contains(".local") Then
                Return oItem.DisplayName.Replace(".local", "")
            End If
        Next
        Return ""
    End Function

    ' <Obsolete("Jest w .Net Standard 2.0 (lib)")>
    ''' <summary>
    ''' Ale to chyba przestało działać...
    ''' </summary>
    Public Function IsThisMoje() As Boolean
        Dim sTmp As String = GetHostName.ToLower
        If sTmp = "home-pkar" Then Return True
        If sTmp = "lumia_pkar" Then Return True
        If sTmp = "kuchnia_pk" Then Return True
        If sTmp = "ppok_pk" Then Return True
        'If sTmp.Contains("pkar") Then Return True
        'If sTmp.EndsWith("_pk") Then Return True
        Return False
    End Function

    ''' <summary>
    ''' w razie Catch() zwraca false
    ''' </summary>
    Public Async Function NetWiFiOffOnAsync() As Task(Of Boolean)

        Try
            ' https://social.msdn.microsoft.com/Forums/ie/en-US/60c4a813-dc66-4af5-bf43-e632c5f85593/uwpbluetoothhow-to-turn-onoff-wifi-bluetooth-programmatically?forum=wpdevelop
            Dim result222 As Windows.Devices.Radios.RadioAccessStatus = Await Windows.Devices.Radios.Radio.RequestAccessAsync()
            If result222 <> Windows.Devices.Radios.RadioAccessStatus.Allowed Then Return False

            Dim radios As IReadOnlyList(Of Windows.Devices.Radios.Radio) = Await Windows.Devices.Radios.Radio.GetRadiosAsync()

            For Each oRadio In radios
                If oRadio.Kind = Windows.Devices.Radios.RadioKind.WiFi Then
                    Dim oStat As Windows.Devices.Radios.RadioAccessStatus =
                    Await oRadio.SetStateAsync(Windows.Devices.Radios.RadioState.Off)
                    If oStat <> Windows.Devices.Radios.RadioAccessStatus.Allowed Then Return False
                    Await Task.Delay(3 * 1000)
                    oStat = Await oRadio.SetStateAsync(Windows.Devices.Radios.RadioState.On)
                    If oStat <> Windows.Devices.Radios.RadioAccessStatus.Allowed Then Return False
                End If
            Next

            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    Public Sub OpenBrowser(sLink As String)
        Dim oUri As New Uri(sLink)
        oUri.OpenBrowser
    End Sub

#Region "Bluetooth"
    ''' <summary>
    ''' Zwraca -1 (no radio), 0 (off), 1 (on), ale gdy bMsg to pokazuje dokładniej błąd (nie włączony, albo nie ma radia Bluetooth) - wedle stringów podanych, które mogą być jednak identyfikatorami w Resources
    ''' </summary>
    Public Async Function NetIsBTavailableAsync(bMsg As Boolean,
                                    Optional bRes As Boolean = False,
                                    Optional sBtDisabled As String = "ERROR: Bluetooth is not enabled",
                                    Optional sNoRadio As String = "ERROR: Bluetooth radio not found") As Task(Of Integer)


        'Dim result222 As Windows.Devices.Radios.RadioAccessStatus = Await Windows.Devices.Radios.Radio.RequestAccessAsync()
        'If result222 <> Windows.Devices.Radios.RadioAccessStatus.Allowed Then Return -1

        Dim oRadios As IReadOnlyList(Of Windows.Devices.Radios.Radio) = Await Windows.Devices.Radios.Radio.GetRadiosAsync()

#If DEBUG Then
        VBlib.DumpCurrMethod(", count=" & oRadios.Count)
        For Each oRadio As Windows.Devices.Radios.Radio In oRadios
            VBlib.DumpMessage("NEXT RADIO")
            VBlib.DumpMessage("name=" & oRadio.Name)
            VBlib.DumpMessage("kind=" & oRadio.Kind)
            VBlib.DumpMessage("state=" & oRadio.State)
        Next
#End If

        Dim bHasBT As Boolean = False

        For Each oRadio As Windows.Devices.Radios.Radio In oRadios
            If oRadio.Kind = Windows.Devices.Radios.RadioKind.Bluetooth Then
                If oRadio.State = Windows.Devices.Radios.RadioState.On Then Return 1
                bHasBT = True
            End If
        Next

        If bHasBT Then
            If bMsg Then
                If bRes Then
                    Await VBlib.DialogBoxResAsync(sBtDisabled)
                Else
                    Await VBlib.DialogBoxAsync(sBtDisabled)
                End If
            End If
            Return 0
        Else
            If bMsg Then
                If bRes Then
                    Await VBlib.DialogBoxResAsync(sNoRadio)
                Else
                    Await VBlib.DialogBoxAsync(sNoRadio)
                End If
            End If
            Return -1
        End If


    End Function

    ''' <summary>
    ''' Zwraca true/false czy State (po call) jest taki jak bOn; wymaga devCap=radios
    ''' </summary>
    Public Async Function NetTrySwitchBTOnAsync(bOn As Boolean) As Task(Of Boolean)
        Dim iCurrState As Integer = Await NetIsBTavailableAsync(False)
        If iCurrState = -1 Then Return False

        ' jeśli nie trzeba przełączać... 
        If bOn AndAlso iCurrState = 1 Then Return True
        If Not bOn AndAlso iCurrState = 0 Then Return True

        ' czy mamy prawo przełączyć? (devCap=radios)
        Dim result222 As Windows.Devices.Radios.RadioAccessStatus = Await Windows.Devices.Radios.Radio.RequestAccessAsync()
        If result222 <> Windows.Devices.Radios.RadioAccessStatus.Allowed Then Return False


        Dim radios As IReadOnlyList(Of Windows.Devices.Radios.Radio) = Await Windows.Devices.Radios.Radio.GetRadiosAsync()

        For Each oRadio In radios
            If oRadio.Kind = Windows.Devices.Radios.RadioKind.Bluetooth Then
                Dim oStat As Windows.Devices.Radios.RadioAccessStatus
                If bOn Then
                    oStat = Await oRadio.SetStateAsync(Windows.Devices.Radios.RadioState.On)
                Else
                    oStat = Await oRadio.SetStateAsync(Windows.Devices.Radios.RadioState.Off)
                End If
                If oStat <> Windows.Devices.Radios.RadioAccessStatus.Allowed Then Return False
            End If
        Next

        Return True
    End Function

#End Region

#End Region


    ' -- DialogBoxy - tylko jako wskok z VBLib ---------------------------------------------

#Region "DialogBoxy"

    Public Async Function FromLibDialogBoxAsync(sMsg As String) As Task
        Dim oMsg As New Windows.UI.Popups.MessageDialog(sMsg)
        Await oMsg.ShowAsync
    End Function

    ''' <summary>
    ''' Dla Cancel zwraca ""
    ''' </summary>
    Public Async Function FromLibDialogBoxYNAsync(sMsg As String, Optional sYes As String = "Tak", Optional sNo As String = "Nie") As Task(Of Boolean)
        Dim oMsg As New Windows.UI.Popups.MessageDialog(sMsg)
        Dim oYes As New Windows.UI.Popups.UICommand(sYes)
        Dim oNo As New Windows.UI.Popups.UICommand(sNo)
        oMsg.Commands.Add(oYes)
        oMsg.Commands.Add(oNo)
        oMsg.DefaultCommandIndex = 1    ' default: No
        oMsg.CancelCommandIndex = 1
        Dim oCmd As Windows.UI.Popups.IUICommand = Await oMsg.ShowAsync
        If oCmd Is Nothing Then Return False
        If oCmd.Label = sYes Then Return True

        Return False
    End Function

    Public Async Function FromLibDialogBoxInputAllDirectAsync(sMsg As String, Optional sDefault As String = "", Optional sYes As String = "Continue", Optional sNo As String = "Cancel") As Task(Of String)
        Dim oInputTextBox = New TextBox With {
            .AcceptsReturn = False,
            .Text = sDefault,
            .IsSpellCheckEnabled = False
        }

        Dim oDlg As New ContentDialog With {
            .Content = oInputTextBox,
            .PrimaryButtonText = sYes,
            .SecondaryButtonText = sNo,
            .Title = sMsg
        }

        Dim oCmd = Await oDlg.ShowAsync
        If oCmd <> ContentDialogResult.Primary Then Return ""

        Return oInputTextBox.Text

    End Function


#End Region


    ' --- INNE FUNKCJE ------------------------
#Region "Toasty itp"
    Public Sub SetBadgeNo(iInt As Integer)
        ' https://docs.microsoft.com/en-us/windows/uwp/controls-and-patterns/tiles-and-notifications-badges

        Dim oXmlBadge As Windows.Data.Xml.Dom.XmlDocument
        oXmlBadge = Windows.UI.Notifications.BadgeUpdateManager.GetTemplateContent(
                Windows.UI.Notifications.BadgeTemplateType.BadgeNumber)

        Dim oXmlNum As Windows.Data.Xml.Dom.XmlElement
        oXmlNum = CType(oXmlBadge.SelectSingleNode("/badge"), Windows.Data.Xml.Dom.XmlElement)
        oXmlNum.SetAttribute("value", iInt.ToString)

        Windows.UI.Notifications.BadgeUpdateManager.CreateBadgeUpdaterForApplication().Update(
                New Windows.UI.Notifications.BadgeNotification(oXmlBadge))
    End Sub

    <Obsolete("Czy na pewno ma być GetSettingsString a nie GetLangString?")>
    Public Function ToastAction(sAType As String, sAct As String, sGuid As String, sContent As String) As String
        Dim sTmp As String = sContent
        If sTmp <> "" Then sTmp = VBlib.GetSettingsString(sTmp, sTmp)

        Dim sTxt As String = "<action " &
            "activationType=""" & sAType & """ " &
            "arguments=""" & sAct & sGuid & """ " &
            "content=""" & sTmp & """/> "
        Return sTxt
    End Function

    Private Sub FromLibMakeToast(sMsg As String, sMsg1 As String)
        Dim sXml = "<visual><binding template='ToastGeneric'><text>" & VBlib.XmlSafeStringQt(sMsg)
        If sMsg1 <> "" Then sXml = sXml & "</text><text>" & VBlib.XmlSafeStringQt(sMsg1)
        sXml &= "</text></binding></visual>"
        Dim oXml = New Windows.Data.Xml.Dom.XmlDocument
        oXml.LoadXml("<toast>" & sXml & "</toast>")
        Dim oToast = New Windows.UI.Notifications.ToastNotification(oXml)
        Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier().Show(oToast)
    End Sub

    ''' <summary>
    ''' dwa kolejne teksty, sMsg oraz sMsg1
    ''' </summary>
    Public Sub MakeToast(sMsg As String, Optional sMsg1 As String = "")
        FromLibMakeToast(sMsg, sMsg1)
    End Sub
    Public Sub MakeToast(oDate As DateTime, sMsg As String, Optional sMsg1 As String = "")
        Dim sXml = "<visual><binding template='ToastGeneric'><text>" & VBlib.XmlSafeStringQt(sMsg)
        If sMsg1 <> "" Then sXml = sXml & "</text><text>" & VBlib.XmlSafeStringQt(sMsg1)
        sXml &= "</text></binding></visual>"
        Dim oXml = New Windows.Data.Xml.Dom.XmlDocument
        oXml.LoadXml("<toast>" & sXml & "</toast>")
        Try
            ' Dim oToast = New Windows.UI.Notifications.ScheduledToastNotification(oXml, oDate, TimeSpan.FromHours(1), 10)
            Dim oToast = New Windows.UI.Notifications.ScheduledToastNotification(oXml, oDate)
            Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier().AddToSchedule(oToast)
        Catch ex As Exception

        End Try
    End Sub

    Public Sub RemoveScheduledToasts()
        Try
            While Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier().GetScheduledToastNotifications().Count > 0
                Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier().RemoveFromSchedule(Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier().GetScheduledToastNotifications().Item(0))
            End While
        Catch ex As Exception
            ' ponoc na desktopm nie dziala
        End Try

    End Sub

    Public Sub RemoveCurrentToasts()
        Windows.UI.Notifications.ToastNotificationManager.History.Clear()
    End Sub

#End Region

#Region "WinVer, AppVer"


    Public Function WinVer() As Integer
        'Unknown = 0,
        'Threshold1 = 1507,   // 10240
        'Threshold2 = 1511,   // 10586
        'Anniversary = 1607,  // 14393 Redstone 1
        'Creators = 1703,     // 15063 Redstone 2
        'FallCreators = 1709 // 16299 Redstone 3
        'April = 1803		// 17134
        'October = 1809		// 17763
        '? = 190?		// 18???

        'April  1803, 17134, RS5

        Dim u As ULong = ULong.Parse(Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamilyVersion)
        u = (u And &HFFFF0000L) >> 16
        Return u
        'For i As Integer = 5 To 1 Step -1
        '    If Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", i) Then Return i
        'Next

        'Return 0
    End Function

    ' <Obsolete("Jest w .Net Standard 2.0 (lib)")>
    Public Function GetAppVers() As String

        Return Windows.ApplicationModel.Package.Current.Id.Version.Major & "." &
        Windows.ApplicationModel.Package.Current.Id.Version.Minor & "." &
        Windows.ApplicationModel.Package.Current.Id.Version.Build

    End Function

    Public Function GetBuildTimestamp() As String
        Dim install_folder As String = Windows.ApplicationModel.Package.Current.InstalledLocation.Path
        Dim sManifestPath As String = Path.Combine(install_folder, "AppxManifest.xml")

        If File.Exists(sManifestPath) Then
            Return File.GetLastWriteTime(sManifestPath).ToString("yyyy.MM.dd HH:mm")
        End If

        Return ""
    End Function


#End Region


#Region "triggers"
#Region "zwykłe"
    Public Function IsTriggersRegistered(sNameMask As String) As Boolean
        sNameMask = sNameMask.Replace(" ", "").Replace("'", "")

        Try
            For Each oTask As KeyValuePair(Of Guid, Background.IBackgroundTaskRegistration) In Background.BackgroundTaskRegistration.AllTasks
                If oTask.Value.Name.ToLower.Contains(sNameMask.ToLower) Then Return True
            Next
        Catch ex As Exception
            ' np. gdy nie ma permissions, to może być FAIL
        End Try

        Return False
    End Function

    ''' <summary>
    ''' jakikolwiek z prefixem Package.Current.DisplayName
    ''' </summary>
    Public Function IsTriggersRegistered() As Boolean
        Return IsTriggersRegistered(Windows.ApplicationModel.Package.Current.DisplayName)
    End Function

    ''' <summary>
    ''' wszystkie z prefixem Package.Current.DisplayName
    ''' </summary>
    Public Sub UnregisterTriggers()
        UnregisterTriggers(Windows.ApplicationModel.Package.Current.DisplayName)
    End Sub



    Public Sub UnregisterTriggers(sNamePrefix As String)
        sNamePrefix = sNamePrefix.Replace(" ", "").Replace("'", "")

        Try
            For Each oTask As KeyValuePair(Of Guid, Background.IBackgroundTaskRegistration) In Background.BackgroundTaskRegistration.AllTasks
                If String.IsNullOrEmpty(sNamePrefix) OrElse oTask.Value.Name.ToLower.Contains(sNamePrefix.ToLower) Then oTask.Value.Unregister(True)
            Next
        Catch ex As Exception
            ' np. gdy nie ma permissions, to może być FAIL
        End Try

        ' z innego wyszlo, ze RemoveAccess z wnetrza daje Exception
        ' If bAll Then BackgroundExecutionManager.RemoveAccess()

    End Sub

    Public Async Function CanRegisterTriggersAsync() As Task(Of Boolean)

        Dim oBAS As Background.BackgroundAccessStatus
        oBAS = Await Background.BackgroundExecutionManager.RequestAccessAsync()

        If oBAS = Windows.ApplicationModel.Background.BackgroundAccessStatus.AlwaysAllowed Then Return True
        If oBAS = Windows.ApplicationModel.Background.BackgroundAccessStatus.AllowedSubjectToSystemPolicy Then Return True

        Return False

    End Function

    Public Function RegisterTimerTrigger(sName As String, iMinutes As Integer, Optional bOneShot As Boolean = False, Optional oCondition As Windows.ApplicationModel.Background.SystemCondition = Nothing) As Windows.ApplicationModel.Background.BackgroundTaskRegistration

        Try
            Dim builder As New Background.BackgroundTaskBuilder
            Dim oRet As Background.BackgroundTaskRegistration

            builder.SetTrigger(New Windows.ApplicationModel.Background.TimeTrigger(iMinutes, bOneShot))
            builder.Name = sName
            If oCondition IsNot Nothing Then builder.AddCondition(oCondition)
            oRet = builder.Register()
            Return oRet
        Catch ex As Exception
            ' brak możliwości rejestracji (na przykład)
        End Try

        Return Nothing
    End Function

    Public Function RegisterUserPresentTrigger(Optional sName As String = "", Optional bOneShot As Boolean = False) As Windows.ApplicationModel.Background.BackgroundTaskRegistration

        Try
            Dim builder As New Background.BackgroundTaskBuilder
            Dim oRet As Background.BackgroundTaskRegistration

            Dim oTrigger As Windows.ApplicationModel.Background.SystemTrigger
            oTrigger = New Background.SystemTrigger(Background.SystemTriggerType.UserPresent, bOneShot)

            builder.SetTrigger(oTrigger)
            builder.Name = sName
            If String.IsNullOrEmpty(sName) Then builder.Name = GetTriggerNamePrefix() & "_userpresent"

            oRet = builder.Register()
            Return oRet
        Catch ex As Exception
            ' brak możliwości rejestracji (na przykład)
        End Try

        Return Nothing
    End Function

    Private Function GetTriggerNamePrefix() As String
        Dim sName As String = Windows.ApplicationModel.Package.Current.DisplayName
        sName = sName.Replace(" ", "").Replace("'", "")
        Return sName
    End Function

    Private Function GetTriggerPolnocnyName() As String
        Return GetTriggerNamePrefix() & "_polnocny"
    End Function


    ''' <summary>
    ''' Tak naprawdę powtarzalny - w OnBackgroundActivated wywołaj IsThisTriggerPolnocny
    ''' </summary>
    Public Async Function DodajTriggerPolnocny() As System.Threading.Tasks.Task
        If Not Await CanRegisterTriggersAsync() Then Return

        Dim oDateNew As New DateTime(Date.Now.Year, Date.Now.Month, Date.Now.Day, 23, 40, 0)
        If Date.Now.Hour > 21 Then oDateNew = oDateNew.AddDays(1)

        Dim iMin As Integer = (oDateNew - DateTime.Now).TotalMinutes
        Dim sName As String = GetTriggerPolnocnyName()

        RegisterTimerTrigger(sName, iMin, False)
    End Function

    ''' <summary>
    ''' para z DodajTriggerPolnocny
    ''' </summary>
    Public Function IsThisTriggerPolnocny(args As Windows.ApplicationModel.Activation.BackgroundActivatedEventArgs) As Boolean

        Dim sName As String = GetTriggerPolnocnyName()
        If args.TaskInstance.Task.Name <> sName Then Return False

        Dim sCurrDate As String = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        VBlib.SetSettingsString("lastPolnocnyTry", sCurrDate)

        Dim bRet As Boolean '= False
        Dim oDateNew As New DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 40, 0)

        If (DateTime.Now.Hour = 23 AndAlso DateTime.Now.Minute > 20) Then
            ' tak, to jest północny o północy
            bRet = True
            oDateNew = oDateNew.AddDays(1)
            VBlib.SetSettingsString("lastPolnocnyOk", sCurrDate)
        Else
            ' północny, ale nie o północy
            bRet = False
        End If
        Dim iMin As Integer = (oDateNew - DateTime.Now).TotalMinutes

        ' Usuwamy istniejący, robimy nowy
        UnregisterTriggers(sName)
        RegisterTimerTrigger(sName, iMin, False)

        Return bRet

    End Function



    Public Function RegisterServicingCompletedTrigger(sName As String) As Background.BackgroundTaskRegistration

        Try
            Dim builder As New Background.BackgroundTaskBuilder

            builder.SetTrigger(New Background.SystemTrigger(Background.SystemTriggerType.ServicingComplete, True))
            builder.Name = sName

            Dim oRet As Windows.ApplicationModel.Background.BackgroundTaskRegistration
            oRet = builder.Register()
            Return oRet
        Catch ex As Exception
            ' brak możliwości rejestracji (na przykład)
        End Try

        Return Nothing
    End Function

    Public Function RegisterToastTrigger(sName As String) As Background.BackgroundTaskRegistration

        Try
            Dim builder As New Background.BackgroundTaskBuilder
            Dim oRet As Windows.ApplicationModel.Background.BackgroundTaskRegistration

            builder.SetTrigger(New Windows.ApplicationModel.Background.ToastNotificationActionTrigger)
            builder.Name = sName
            oRet = builder.Register()
            Return oRet
        Catch ex As Exception
            ' brak możliwości rejestracji (na przykład)
        End Try

        Return Nothing
    End Function

#End Region
#Region "RemoteSystem"

    ''' <summary>
    ''' jeśli na wejściu jest jakaś standardowa komenda, to na wyjściu będzie jej rezultat. Else = ""
    ''' </summary>
    Public Function AppServiceStdCmd(sCommand As String, sLocalCmds As String) As String
        Dim sTmp As String = VBlib.LibAppServiceStdCmd(sCommand, sLocalCmds)
        If sTmp <> "" Then Return sTmp

        ' If sCommand.StartsWith("debug loglevel") Then - vbLib

        Select Case sCommand.ToLower()
            ' Case "ping" - vblib
            Case "ver"
                Return GetAppVers()
            Case "localdir"
                Return Windows.Storage.ApplicationData.Current.LocalFolder.Path
            ' Case "appdir" - vblib
            Case "installeddate"
                Return Windows.ApplicationModel.Package.Current.InstalledDate.ToString("yyyy.MM.dd HH:mm:ss")
            ' Case "help" - vblib

            ' Case "debug vars" - vblib
            Case "debug triggers"
                Return DumpTriggers()
            Case "debug toasts"
                Return DumpToasts()
            Case "debug memsize"
                Return Windows.System.MemoryManager.AppMemoryUsage.ToString() & "/" & Windows.System.MemoryManager.AppMemoryUsageLimit.ToString()
            Case "debug rungc"
                sTmp = "Memory usage before Global Collector call: " & Windows.System.MemoryManager.AppMemoryUsage.ToString() & vbCrLf
                GC.Collect()
                GC.WaitForPendingFinalizers()
                sTmp = sTmp & "After: " & Windows.System.MemoryManager.AppMemoryUsage.ToString() & "/" & Windows.System.MemoryManager.AppMemoryUsageLimit.ToString()
                Return sTmp
            ' Case "debug crashmsg"
            ' Case "debug crashmsg clear"

            Case "lib unregistertriggers"
                sTmp = DumpTriggers()
                UnregisterTriggers("") ' // całkiem wszystkie
                Return sTmp
            Case "lib isfamilymobile"
                Return IsFamilyMobile().ToString()
            Case "lib isfamilydesktop"
                Return IsFamilyDesktop().ToString()
            Case "lib netisipavailable"
                Return NetIsIPavailable(False).ToString()
            Case "lib netiscellinet"
                Return NetIsCellInet().ToString()
            Case "lib gethostname"
                Return GetHostName()
            Case "lib isthismoje"
                Return IsThisMoje().ToString()
            Case "lib istriggersregistered"
                Return IsTriggersRegistered().ToString()

                'Case "lib pkarmode 1"
                'Case "lib pkarmode 0"
                'Case "lib pkarmode"
        End Select

        Return ""  ' oznacza: to nie jest standardowa komenda
    End Function


    '    Private Function DumpSettings() As String
    '       Dim sRoam As String = ""
    '        Try
    '            For Each oVal In Windows.Storage.ApplicationData.Current.RoamingSettings.Values
    '                sRoam = sRoam & oVal.Key & vbTab & oVal.Value.ToString() & vbCrLf
    '            Next
    '        Catch
    '        End Try
    '
    '        Dim sLocal As String = ""
    '        Try
    '            For Each oVal In Windows.Storage.ApplicationData.Current.LocalSettings.Values
    '                sLocal = sLocal & oVal.Key & vbTab & oVal.Value.ToString() & vbCrLf
    '            Next
    '        Catch
    '        End Try
    '
    '        Dim sRet As String = "Dumping Settings" & vbCrLf
    '        If sRoam <> "" Then
    '            sRet = sRet & vbCrLf & "Roaming:" & vbCrLf & sRoam
    '        Else
    '            sRet = sRet & "(no roaming settings)" & vbCrLf
    '       End If
    '
    '        If sLocal <> "" Then
    '            sRet = sRet & vbCrLf & "Local:" & vbCrLf & sLocal
    '        Else
    '            sRet = sRet & "(no local settings)" & vbCrLf
    '        End If
    '
    '        Return sRet
    '    End Function


    Private Function DumpTriggers() As String
        Dim sRet As String = "Dumping Triggers" & vbCrLf & vbCrLf
        Try
            For Each oTask In Windows.ApplicationModel.Background.BackgroundTaskRegistration.AllTasks
                sRet &= oTask.Value.Name & vbCrLf ' //GetType niestety nie daje rzeczywistego typu
            Next
        Catch
        End Try


        Return sRet
    End Function

    Private Function DumpToasts() As String

        Dim sResult As String = ""
        For Each oToast As Windows.UI.Notifications.ScheduledToastNotification
            In Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier().GetScheduledToastNotifications()

            sResult = sResult & oToast.DeliveryTime.ToString("yyyy-MM-dd HH:mm:ss") & vbCrLf
        Next

        If sResult = "" Then
            sResult = "(no toasts scheduled)"
        Else
            sResult = "Toasts scheduled for dates: " & vbCrLf & sResult
        End If

        Return sResult
    End Function

#End Region


#End Region

#Region "DataLog folder support"


    Private Async Function GetSDcardFolderAsync() As Task(Of Windows.Storage.StorageFolder)
        ' uwaga: musi być w Manifest RemoteStorage oraz fileext!

        Dim oRootDir As Windows.Storage.StorageFolder

        Try
            oRootDir = Windows.Storage.KnownFolders.RemovableDevices
        Catch ex As Exception
            Return Nothing ' brak uprawnień, może być także THROW
        End Try

        Try
            Dim oCards As IReadOnlyList(Of Windows.Storage.StorageFolder) = Await oRootDir.GetFoldersAsync()
            If oCards.Count < 1 Then Return Nothing
            Return oCards(0)
        Catch ex As Exception
            ' nie udało się folderu SD
        End Try

        Return Nothing


    End Function

    Public Async Function GetLogFolderRootAsync(Optional bUseOwnFolderIfNotSD As Boolean = True) As Task(Of Windows.Storage.StorageFolder)
#Disable Warning IDE0059 ' Unnecessary assignment of a value
        Dim oSdCard As Windows.Storage.StorageFolder = Nothing
#Enable Warning IDE0059 ' Unnecessary assignment of a value
        Dim oFold As Windows.Storage.StorageFolder

        If IsFamilyMobile() Then
            oSdCard = Await GetSDcardFolderAsync()

            If oSdCard IsNot Nothing Then
                oFold = Await oSdCard.CreateFolderAsync("DataLogs", Windows.Storage.CreationCollisionOption.OpenIfExists)
                If oFold Is Nothing Then Return Nothing

                Dim sAppName As String = Package.Current.DisplayName
                sAppName = sAppName.Replace(" ", "").Replace("'", "")

                oFold = Await oFold.CreateFolderAsync(sAppName, Windows.Storage.CreationCollisionOption.OpenIfExists)
                If oFold Is Nothing Then Return Nothing
            Else
                If Not bUseOwnFolderIfNotSD Then Return Nothing
                oSdCard = Windows.Storage.ApplicationData.Current.LocalFolder
                oFold = Await oSdCard.CreateFolderAsync("DataLogs", Windows.Storage.CreationCollisionOption.OpenIfExists)
                If oFold Is Nothing Then Return Nothing
            End If
        Else
            oSdCard = Windows.Storage.ApplicationData.Current.LocalFolder
            oFold = Await oSdCard.CreateFolderAsync("DataLogs", Windows.Storage.CreationCollisionOption.OpenIfExists)
            If oFold Is Nothing Then Return Nothing
        End If

        Return oFold
    End Function


    ''' <summary>
    ''' do wywolania raz, na poczatku - inicjalizacja zmiennych w VBlib (sciezki root)
    ''' </summary>
    Public Async Function InitDatalogFolder(Optional bUseOwnFolderIfNotSD As Boolean = True) As Task
        Dim oFold As Windows.Storage.StorageFolder = Await GetLogFolderRootAsync(bUseOwnFolderIfNotSD)
        If oFold Is Nothing Then Return
        VBlib.LibInitDataLog(oFold.Path)
    End Function

#End Region


#Region "Bluetooth debugs"

    Public Async Function DebugBTGetServChar(uMAC As ULong,
                                      sService As String, sCharacteristic As String) As Task(Of Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic)
        Dim oDev As Windows.Devices.Bluetooth.BluetoothLEDevice
        oDev = Await Windows.Devices.Bluetooth.BluetoothLEDevice.FromBluetoothAddressAsync(uMAC)
        If oDev Is Nothing Then
            VBlib.DebugOut("DebugBTGetServChar called, cannot get device for uMAC = " & uMAC.ToHexBytesString)
            Return Nothing
        End If

        Return Await DebugBTGetServChar(oDev, sService, sCharacteristic)
    End Function

    Public Async Function DebugBTGetServChar(oDevice As Windows.Devices.Bluetooth.BluetoothLEDevice,
                                      sService As String, sCharacteristic As String) As Task(Of Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic)

        If oDevice Is Nothing Then
            VBlib.DebugOut("DebugBTGetServChar called with oDevice = null")
            Return Nothing
        End If

        Dim oSrv = Await oDevice.GetGattServicesAsync
        If oSrv.Status <> Windows.Devices.Bluetooth.GenericAttributeProfile.GattCommunicationStatus.Success Then
            VBlib.DebugOut("DebugBTGetServChar:GetGattServicesAsync.Status = " & oSrv.Status.ToString)
            Return Nothing
        End If

        Dim oSvc As Windows.Devices.Bluetooth.GenericAttributeProfile.GattDeviceService = Nothing
        For Each oSv In oSrv.Services
            If oSv.Uuid.ToString = sService.ToLower Then
                oSvc = oSv
            End If
        Next
        If oSvc Is Nothing Then
            VBlib.DebugOut("DebugBTGetServChar: cannot find service " & sService)
            Return Nothing
        End If

        Dim oChars = Await oSvc.GetCharacteristicsAsync
        If oChars Is Nothing Then
            VBlib.DebugOut("DebugBTGetServChar:GetCharacteristicsAsync = null")
            Return Nothing
        End If

        If oChars.Status <> Windows.Devices.Bluetooth.GenericAttributeProfile.GattCommunicationStatus.Success Then
            Debug.WriteLine("DebugBTGetServChar:GetCharacteristicsAsync.Status = " & oChars.Status.ToString)
            Return Nothing
        End If

        For Each oChr In oChars.Characteristics
            If oChr.Uuid.ToString = sCharacteristic.ToLower Then Return oChr
        Next

        Return Nothing
    End Function

#End Region



    Public Function GetDomekGeopos(Optional iDecimalDigits As UInt16 = 0) As Windows.Devices.Geolocation.BasicGeoposition
        Select Case iDecimalDigits
            Case 1
                Return NewBasicGeoposition(50.0, 19.9)
            Case 2
                Return NewBasicGeoposition(50.01, 19.97)
            Case 3
                Return NewBasicGeoposition(50.019, 19.978)
            Case 4
                Return NewBasicGeoposition(50.0198, 19.9787)
            Case 5
                Return NewBasicGeoposition(50.01985, 19.97872)
            Case Else
                Return NewBasicGeoposition(50, 20)
        End Select

    End Function

    Public Function NewBasicGeoposition(dLat As Double, dLon As Double) As Windows.Devices.Geolocation.BasicGeoposition
        Return New Windows.Devices.Geolocation.BasicGeoposition With {
            .Latitude = dLat,
            .Longitude = dLon
        }
    End Function

    Public Async Function IsFullVersion() As Task(Of Boolean)
#If DEBUG Then
        Return True
#End If

        If IsThisMoje() Then Return True

        ' Windows.Services.Store.StoreContext: min 14393 (1607)
        Dim oLicencja = Await Windows.Services.Store.StoreContext.GetDefault().GetAppLicenseAsync()
        If Not oLicencja.IsActive Then Return False ' bez licencji? jakżeż to możliwe?

        If oLicencja.IsTrial Then Return False

        Return True

    End Function


End Module

Module Extensions

#Region "Read/Write text"

    <Extension()>
    <Obsolete("Raczej się pozbądź, przejdź na .Net")>
    Public Async Function WriteAllTextAsync(ByVal oFile As Windows.Storage.StorageFile, sTxt As String) As Task
        Dim oStream As Stream = Await oFile.OpenStreamForWriteAsync
        Using oWriter As New Windows.Storage.Streams.DataWriter(oStream.AsOutputStream) With {
            .UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8
        }
            oWriter.WriteString(sTxt)
            Await oWriter.FlushAsync()
            Await oWriter.StoreAsync()
        End Using
    End Function

    ''' <summary>
    ''' appenduje string, i dodaje vbCrLf
    ''' </summary>
    <Extension()>
    <Obsolete("Raczej się pozbądź, przejdź na .Net")>
    Public Async Function AppendLineAsync(ByVal oFile As Windows.Storage.StorageFile, sTxt As String) As Task
        Await oFile.AppendStringAsync(sTxt & vbCrLf)
    End Function

    ''' <summary>
    ''' appenduje string, nic nie dodając. Zwraca FALSE gdy nie udało się otworzyć pliku.
    ''' </summary>
    <Extension()>
    <Obsolete("Raczej się pozbądź, przejdź na .Net")>
    Public Async Function AppendStringAsync(ByVal oFile As Windows.Storage.StorageFile, sTxt As String) As Task(Of Boolean)

        Dim oStream As Stream

        Try
            oStream = Await oFile.OpenStreamForWriteAsync
        Catch ex As Exception
            Return False ' mamy błąd otwarcia pliku
        End Try

        oStream.Seek(0, SeekOrigin.End)
        Using oWriter As New Windows.Storage.Streams.DataWriter(oStream.AsOutputStream) With {
            .UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8
        }
            oWriter.WriteString(sTxt)
            Await oWriter.FlushAsync()
            Await oWriter.StoreAsync()
        End Using
        Return True
        'oStream.Flush()
        'oStream.Dispose()
    End Function

    <Extension()>
    <Obsolete("Raczej się pozbądź, przejdź na .Net")>
    Public Async Function WriteAllTextToFileAsync(ByVal oFold As Windows.Storage.StorageFolder, sFileName As String, sTxt As String, Optional oOption As Windows.Storage.CreationCollisionOption = Windows.Storage.CreationCollisionOption.FailIfExists) As Task
        Dim oFile As Windows.Storage.StorageFile = Await oFold.CreateFileAsync(sFileName, oOption)
        If oFile Is Nothing Then Return

        Await oFile.WriteAllTextAsync(sTxt)
    End Function

#If False Then

    <Extension()>
    Public Async Function SerializeToJSONAsync(ByVal oFold As Windows.Storage.StorageFolder, sFileName As String, mItems As Object) As Task

        Dim sTxt As String = Newtonsoft.Json.JsonConvert.SerializeObject(mItems, Newtonsoft.Json.Formatting.Indented)
        Await oFold.WriteAllTextToFileAsync(sFileName, sTxt, Windows.Storage.CreationCollisionOption.ReplaceExisting)

    End Function
#End If

    <Extension()>
    <Obsolete("Raczej się pozbądź, przejdź na .Net")>
    Public Async Function ReadAllTextAsync(ByVal oFile As Windows.Storage.StorageFile) As Task(Of String)
        ' zamiast File.ReadAllText(oFile.Path)
        Dim sTxt As String

        Using oStream As Stream = Await oFile.OpenStreamForReadAsync
            Using oReader As New Windows.Storage.Streams.DataReader(oStream.AsInputStream) With {
            .UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8
        }
                Dim iSize As Integer = oStream.Length
                Await oReader.LoadAsync(iSize)
                sTxt = oReader.ReadString(iSize)
                oReader.Dispose()
            End Using
        End Using
        Return sTxt
    End Function

    ''' <summary>
    ''' Uwaga: zwraca NULL gdy nie ma pliku, lub tresc pliku
    ''' </summary>
    <Extension()>
    <Obsolete("Raczej się pozbądź, przejdź na .Net")>
    Public Async Function ReadAllTextFromFileAsync(ByVal oFold As Windows.Storage.StorageFolder, sFileName As String) As Task(Of String)
        Dim oFile As Windows.Storage.StorageFile = Await oFold.TryGetItemAsync(sFileName)
        If oFile Is Nothing Then Return Nothing
        Return Await oFile.ReadAllTextAsync
    End Function

#End Region

    <Extension()>
    Public Sub OpenExplorer(ByVal oFold As Windows.Storage.StorageFolder)
#Disable Warning BC42358 ' Because this call is not awaited, execution of the current method continues before the call is completed
        Windows.System.Launcher.LaunchFolderAsync(oFold)
#Enable Warning BC42358
    End Sub


    <Extension()>
    Public Sub OpenBrowser(ByVal oUri As System.Uri, Optional bForceEdge As Boolean = False)
        If bForceEdge Then
            ' tylko w FilteredRss
            Dim options = New Windows.System.LauncherOptions With
                {
                    .TargetApplicationPackageFamilyName = "Microsoft.MicrosoftEdge_8wekyb3d8bbwe"
                }
#Disable Warning BC42358 ' Because this call is not awaited, execution of the current method continues before the call is completed
            Windows.System.Launcher.LaunchUriAsync(oUri, options)
#Enable Warning BC42358
        Else

#Disable Warning BC42358 ' Because this call is not awaited, execution of the current method continues before the call is completed
            Windows.System.Launcher.LaunchUriAsync(oUri)
#Enable Warning BC42358
        End If
    End Sub


    <Extension()>
    <Obsolete("Raczej się pozbądź, przejdź na .Net")>
    Public Async Function FileExistsAsync(ByVal oFold As Windows.Storage.StorageFolder, sFileName As String) As Task(Of Boolean)
        Try
            Dim oTemp As Windows.Storage.StorageFile = Await oFold.TryGetItemAsync(sFileName)
            If oTemp Is Nothing Then Return False
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function


    <Extension()>
    Public Async Function GetDocumentHtml(ByVal uiWebView As WebView) As Task(Of String)
        Try
            Return Await uiWebView.InvokeScriptAsync("eval", New String() {"document.documentElement.outerHTML;"})
        Catch ex As Exception
            Return "" ' jesli strona jest pusta, jest Exception
        End Try
    End Function

#Region "GPS odleglosci"

    <Extension()>
    Public Function ToMyGeopos(ByVal oPos As Windows.Devices.Geolocation.BasicGeoposition) As BasicGeopos
        Return New BasicGeopos(oPos.Latitude, oPos.Longitude)
    End Function

    <Extension()>
    Public Function ToWinGeopoint(ByVal oPos As BasicGeopos) As Windows.Devices.Geolocation.Geopoint
        Return New Windows.Devices.Geolocation.Geopoint(oPos.ToWinGeopos())
    End Function


    <Extension()>
    Public Function ToWinGeopos(ByVal oPos As BasicGeopos) As Windows.Devices.Geolocation.BasicGeoposition
        Dim oPoint As New Windows.Devices.Geolocation.BasicGeoposition With
            {
                .Latitude = oPos.Latitude,
                .Longitude = oPos.Longitude,
                .Altitude = oPos.Altitude
            }
        Return oPoint
    End Function

    <Extension()>
    Public Function DistanceTo(ByVal oGeocoord0 As Windows.Devices.Geolocation.Geocoordinate, oGeocoord1 As Windows.Devices.Geolocation.Geocoordinate) As Double
        Return oGeocoord0.Point.Position.ToMyGeopos().DistanceTo(oGeocoord1.Point.Position.ToMyGeopos())
    End Function

    <Extension()>
    Public Function DistanceTo(ByVal oGeopos0 As Windows.Devices.Geolocation.Geoposition, oGeopos1 As Windows.Devices.Geolocation.Geoposition) As Double
        Return oGeopos0.Coordinate.DistanceTo(oGeopos1.Coordinate)
    End Function

    '<Extension()>
    'Public Function DistanceTo(ByVal oGeocoord0 As Windows.Devices.Geolocation.Geocoordinate, oGeocoord1 As Windows.Devices.Geolocation.Geocoordinate) As Integer
    '    Return oGeocoord0.Point.Position.DistanceTo(oGeocoord1.Point.Position)
    'End Function

    '<Extension()>
    'Public Function DistanceTo(ByVal oGeopos0 As Windows.Devices.Geolocation.Geoposition, oGeopos1 As Windows.Devices.Geolocation.Geoposition) As Integer
    '    Return oGeopos0.Coordinate.DistanceTo(oGeopos1.Coordinate)

    'End Function

    <Extension()>
    Public Function DistanceTo(ByVal oGeopos0 As Windows.Devices.Geolocation.BasicGeoposition, oGeopos1 As Windows.Devices.Geolocation.BasicGeoposition) As Integer
        Return oGeopos0.ToMyGeopos.DistanceTo(oGeopos1.ToMyGeopos)
    End Function

    '<Extension()>
    'Public Function DistanceTo(ByVal oGeopos0 As Windows.Devices.Geolocation.BasicGeoposition, oGeopos1 As Windows.Devices.Geolocation.BasicGeoposition) As Integer
    '    ' https://stackoverflow.com/questions/28569246/how-to-get-distance-between-two-locations-in-windows-phone-8-1

    '    Try
    '        Dim iRadix As Integer = 6371000
    '        Dim tLat As Double = (oGeopos1.Latitude - oGeopos0.Latitude) * Math.PI / 180
    '        Dim tLon As Double = (oGeopos1.Longitude - oGeopos0.Longitude) * Math.PI / 180
    '        Dim a As Double = Math.Sin(tLat / 2) * Math.Sin(tLat / 2) +
    '        Math.Cos(Math.PI / 180 * oGeopos0.Latitude) * Math.Cos(Math.PI / 180 * oGeopos1.Latitude) *
    '        Math.Sin(tLon / 2) * Math.Sin(tLon / 2)
    '        Dim c As Double = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)))
    '        Dim d As Double = iRadix * c

    '        Return d

    '    Catch ex As Exception
    '        Return 0    ' nie powinno sie nigdy zdarzyc, ale na wszelki wypadek...
    '    End Try

    'End Function

    '<Extension()>
    'Public Function DistanceTo(ByVal oGeopos0 As Windows.Devices.Geolocation.BasicGeoposition, dLat As Double, dLong As Double) As Integer
    '    Return oGeopos0.DistanceTo(NewBasicGeoposition(dLat, dLong))
    'End Function

    '''' <summary>
    '''' Czy punkt leży w miarę w Polsce (mniej niż 500 km od środka geometrycznego Polski)
    '''' </summary>
    '<Extension()>
    'Public Function IsInsidePoland(ByVal oPos As Windows.Devices.Geolocation.BasicGeoposition) As Boolean
    '    ' https://pl.wikipedia.org/wiki/Geometryczny_%C5%9Brodek_Polski

    '    Dim dOdl As Double = oPos.DistanceTo(NewBasicGeoposition(52.2159333, 19.1344222))
    '    If dOdl / 1000 > 500 Then Return False
    '    Return True    ' ale to nie jest pewne, tylko: "możliwe"
    'End Function

#End Region


    <Extension()>
    Public Function ToDebugString(ByVal oBuf As Windows.Storage.Streams.IBuffer, iMaxLen As Integer) As String
        Dim sRet As String = oBuf.Length & ": "
        Dim oArr As Byte() = oBuf.ToArray

        For i As Integer = 0 To Math.Min(oBuf.Length - 1, iMaxLen)
            sRet = sRet & oArr.ElementAt(i).ToString("X2") & " "
        Next

        Return sRet & vbCrLf
    End Function


#Region "Settingsy jako Extension"
    <Extension()>
    Public Sub GetSettingsString(ByVal oItem As TextBlock, Optional sName As String = "", Optional sDefault As String = "")
        If sName = "" Then sName = oItem.Name
        Dim sTxt As String = VBlib.GetSettingsString(sName, sDefault)
        oItem.Text = sTxt
    End Sub
    <Extension()>
    Public Sub SetSettingsString(ByVal oItem As TextBlock, Optional sName As String = "", Optional bRoam As Boolean = False)
        If sName = "" Then sName = oItem.Name
        VBlib.SetSettingsString(sName, oItem.Text, bRoam)
    End Sub

    <Extension()>
    Public Sub GetSettingsString(ByVal oItem As TextBox, Optional sName As String = "", Optional sDefault As String = "")
        If sName = "" Then sName = oItem.Name
        Dim sTxt As String = VBlib.GetSettingsString(sName, sDefault)
        oItem.Text = sTxt
    End Sub
    <Extension()>
    Public Sub SetSettingsString(ByVal oItem As TextBox, Optional sName As String = "", Optional bRoam As Boolean = False)
        If sName = "" Then sName = oItem.Name
        VBlib.SetSettingsString(sName, oItem.Text, bRoam)
    End Sub

    <Extension()>
    Public Sub GetSettingsBool(ByVal oItem As ToggleSwitch, Optional sName As String = "", Optional bDefault As Boolean = False)
        If sName = "" Then sName = oItem.Name
        Dim bBool As Boolean = VBlib.GetSettingsBool(sName, bDefault)
        oItem.IsOn = bBool
    End Sub
    <Extension()>
    Public Sub SetSettingsBool(ByVal oItem As ToggleSwitch, Optional sName As String = "", Optional bRoam As Boolean = False)
        If sName = "" Then sName = oItem.Name
        VBlib.SetSettingsBool(sName, oItem.IsOn, bRoam)
    End Sub

    <Extension()>
    Public Sub GetSettingsBool(ByVal oItem As ToggleButton, Optional sName As String = "", Optional bDefault As Boolean = False)
        If sName = "" Then sName = oItem.Name
        Dim bBool As Boolean = VBlib.GetSettingsBool(sName, bDefault)
        oItem.IsChecked = bBool
    End Sub
    <Extension()>
    Public Sub SetSettingsBool(ByVal oItem As ToggleButton, Optional sName As String = "", Optional bRoam As Boolean = False)
        If sName = "" Then sName = oItem.Name
        VBlib.SetSettingsBool(sName, oItem.IsChecked, bRoam)
    End Sub

    <Extension()>
    Public Sub GetSettingsBool(ByVal oItem As AppBarToggleButton, Optional sName As String = "", Optional bDefault As Boolean = False)
        If sName = "" Then sName = oItem.Name
        Dim bBool As Boolean = VBlib.GetSettingsBool(sName, bDefault)
        oItem.IsChecked = bBool
    End Sub
    <Extension()>
    Public Sub SetSettingsBool(ByVal oItem As AppBarToggleButton, Optional sName As String = "", Optional bRoam As Boolean = False)
        If sName = "" Then sName = oItem.Name
        VBlib.SetSettingsBool(sName, oItem.IsChecked, bRoam)
    End Sub

    ''' <summary>
    ''' Zapisanie SettingsInt z możliwością skalowania (TextBox: zł.gr to dScale = 100)
    ''' </summary>
    ''' <param name="oItem"></param>
    ''' <param name="sName"></param>
    ''' <param name="bRoam"></param>
    ''' <param name="dScale"></param>
    <Extension()>
    Public Sub SetSettingsInt(ByVal oItem As TextBox, Optional sName As String = "", Optional bRoam As Boolean = False, Optional dScale As Double = 1)
        If sName = "" Then sName = oItem.Name
        Dim dTmp As Integer
        If Not Double.TryParse(oItem.Text, dTmp) Then Return
        dTmp *= dScale
        VBlib.SetSettingsInt(sName, dTmp, bRoam)
    End Sub

    ''' <summary>
    ''' Pobranie SettingsInt z możliwością skalowania (TextBox: zł.gr to dScale = 100)
    ''' </summary>
    ''' <param name="oItem"></param>
    ''' <param name="sName"></param>
    ''' <param name="dScale"></param>
    <Extension()>
    Public Sub GetSettingsInt(ByVal oItem As TextBox, Optional sName As String = "", Optional dScale As Double = 1)
        If sName = "" Then sName = oItem.Name
        Dim dTmp As Integer = VBlib.GetSettingsInt(sName)
        dTmp /= dScale
        oItem.Text = dTmp
    End Sub

    <Extension()>
    Public Sub SetSettingsInt(ByVal oItem As Windows.UI.Xaml.Controls.Slider, Optional sName As String = "", Optional bRoam As Boolean = False)
        If sName = "" Then sName = oItem.Name
        VBlib.SetSettingsInt(sName, oItem.Value, bRoam)
    End Sub

    <Extension()>
    Public Sub GetSettingsInt(ByVal oItem As Windows.UI.Xaml.Controls.Slider, Optional sName As String = "")
        If sName = "" Then sName = oItem.Name
        oItem.Value = VBlib.GetSettingsInt(sName)
    End Sub

    <Extension()>
    Public Sub SetSettingsInt(ByVal oItem As ComboBox, Optional sName As String = "", Optional bRoam As Boolean = False)
        If sName = "" Then sName = oItem.Name
        VBlib.SetSettingsInt(sName, oItem.SelectedIndex, bRoam)
    End Sub

    <Extension()>
    Public Sub GetSettingsInt(ByVal oItem As ComboBox, Optional sName As String = "")
        If sName = "" Then sName = oItem.Name
        Dim temp As Integer = VBlib.GetSettingsInt(sName)
        If temp < oItem.Items.Count Then
            oItem.SelectedIndex = temp
        Else
            oItem.SelectedIndex = -1
        End If
    End Sub


    <Extension()>
    Public Sub SetSettingsDate(ByVal oItem As CalendarDatePicker, Optional sName As String = "", Optional bRoam As Boolean = False)
        If sName = "" Then sName = oItem.Name
        VBlib.SetSettingsDate(sName, oItem.Date.Value, bRoam)
    End Sub

    <Extension()>
    Public Sub GetSettingsDate(ByVal oItem As CalendarDatePicker, Optional sName As String = "")
        If sName = "" Then sName = oItem.Name
        Dim dDTOff As DateTimeOffset = VBlib.GetSettingsDate(sName)
        oItem.Date = dDTOff
    End Sub



#End Region
    <Extension()>
    Public Sub ShowAppVers(ByVal oItem As TextBlock)
        Dim sTxt As String = pkar.GetAppVers()
#If DEBUG Then
        sTxt &= " (debug " & GetBuildTimestamp() & ")"
#End If
        oItem.Text = sTxt
    End Sub

    <Extension()>
    Public Sub ShowAppVers(ByVal oPage As Page)

        Dim oGrid As Grid = TryCast(oPage.Content, Grid)
        If oGrid Is Nothing Then
            ' skoro to nie Grid, to nie ma jak umiescic koniecznych elementow
            Debug.WriteLine("GetAppVers(null) wymaga Grid jako podstawy Page")
            Throw New ArgumentException("GetAppVers(null) wymaga Grid jako podstawy Page")
        End If

        Dim iCols As Integer = 0
        If oGrid.ColumnDefinitions IsNot Nothing Then iCols = oGrid.ColumnDefinitions.Count ' może być 0
        Dim iRows As Integer = 0
        If oGrid.RowDefinitions IsNot Nothing Then iRows = oGrid.RowDefinitions.Count ' może być 0

        Dim oTB As New TextBlock With {
            .Name = "uiPkAutoVersion",
            .VerticalAlignment = VerticalAlignment.Center,
            .HorizontalAlignment = HorizontalAlignment.Center,
            .FontSize = 10
        }

        If iRows > 2 Then Grid.SetRow(oTB, 1)
        If iCols > 1 Then
            Grid.SetColumn(oTB, 0)
            Grid.SetColumnSpan(oTB, iCols)
        End If
        oGrid.Children.Add(oTB)

        oTB.ShowAppVers()
    End Sub

#Region "MAUI_ulatwiacz"

    ''' <summary>
    ''' żeby było tak samo jak w MAUI, skoro nie da się w MAUI tego zrobić
    ''' </summary>
    <Extension()>
    Public Sub GoBack(ByVal oPage As Page)
        oPage.Frame.GoBack()
    End Sub


    ''' <summary>
    ''' żeby było tak samo jak w MAUI, skoro nie da się w MAUI tego zrobić
    ''' </summary>
    <Extension()>
    Public Sub Navigate(ByVal oPage As Page, sourcePageType As Type, Optional parameter As Object = Nothing)
        oPage.Frame.Navigate(sourcePageType, parameter)
    End Sub
#End Region

    ' --- progring ------------------------

#Region "ProgressBar/Ring"
    ' dodałem 25 X 2020

    'Private _mProgRing As ProgressRing = Nothing
    'Private _mProgBar As ProgressBar = Nothing
    Private _mProgRingShowCnt As Integer = 0

    <Extension()>
    Public Sub ProgRingInit(ByVal oPage As Page, bRing As Boolean, bBar As Boolean)

        ' 2020.11.24: dodaję force-off do ProgRing na Init
        _mProgRingShowCnt = 0   ' skoro inicjalizuje, to znaczy że na pewno trzeba wyłączyć

        'Dim oFrame As Frame = Window.Current.Content
        'Dim oPage As Page = oFrame.Content
        Dim oGrid As Grid = TryCast(oPage.Content, Grid)
        If oGrid Is Nothing Then
            ' skoro to nie Grid, to nie ma jak umiescic koniecznych elementow
            Debug.WriteLine("ProgRingInit wymaga Grid jako podstawy Page")
            Throw New ArgumentException("ProgRingInit wymaga Grid jako podstawy Page")
        End If

        Dim iCols As Integer = 0
        If oGrid.ColumnDefinitions IsNot Nothing Then iCols = oGrid.ColumnDefinitions.Count ' mo¿e byæ 0
        Dim iRows As Integer = 0
        If oGrid.RowDefinitions IsNot Nothing Then iRows = oGrid.RowDefinitions.Count ' mo¿e byæ 0

        If oPage.FindName("uiPkAutoProgRing") Is Nothing Then
            If bRing Then
                Dim _mProgRing As New ProgressRing With {
                    .Name = "uiPkAutoProgRing",
                    .VerticalAlignment = VerticalAlignment.Center,
                    .HorizontalAlignment = HorizontalAlignment.Center,
                    .Visibility = Visibility.Collapsed
                }
                Canvas.SetZIndex(_mProgRing, 10000)
                If iRows > 1 Then
                    Grid.SetRow(_mProgRing, 0)
                    Grid.SetRowSpan(_mProgRing, iRows)
                End If
                If iCols > 1 Then
                    Grid.SetColumn(_mProgRing, 0)
                    Grid.SetColumnSpan(_mProgRing, iCols)
                End If
                oGrid.Children.Add(_mProgRing)
            End If
        End If

        If oPage.FindName("uiPkAutoProgBar") Is Nothing Then
            If bBar Then
                Dim _mProgBar As New ProgressBar With {
                    .Name = "uiPkAutoProgBar",
                    .VerticalAlignment = VerticalAlignment.Bottom,
                    .HorizontalAlignment = HorizontalAlignment.Stretch,
                    .Visibility = Visibility.Collapsed
                }
                Canvas.SetZIndex(_mProgBar, 10000)
                If iRows > 1 Then Grid.SetRow(_mProgBar, iRows - 1)
                If iCols > 1 Then
                    Grid.SetColumn(_mProgBar, 0)
                    Grid.SetColumnSpan(_mProgBar, iCols)
                End If
                oGrid.Children.Add(_mProgBar)
            End If
        End If

    End Sub

    <Extension()>
    Public Sub ProgRingShow(ByVal oPage As Page, bVisible As Boolean, Optional bForce As Boolean = False, Optional dMin As Double = 0, Optional dMax As Double = 100)

        '2021.10.02: tylko gdy jeszcze nie jest pokazywany
        '2021.10.13: gdy min<>max, oraz tylko gdy ma pokazać - inaczej nie zmieniaj zakresu!

        ' FrameworkElement.FindName(String) 

        Dim _mProgBar As ProgressBar = TryCast(oPage.FindName("uiPkAutoProgBar"), ProgressBar)
        If bVisible And _mProgBar IsNot Nothing And _mProgRingShowCnt < 1 Then
            If dMin <> dMax Then
                Try
                    _mProgBar.Minimum = dMin
                    _mProgBar.Value = dMin
                    _mProgBar.Maximum = dMax
                Catch ex As Exception
                End Try
            End If
        End If

        If bForce Then
            If bVisible Then
                _mProgRingShowCnt = 1
            Else
                _mProgRingShowCnt = 0
            End If
        Else
            If bVisible Then
                _mProgRingShowCnt += 1
            Else
                _mProgRingShowCnt -= 1
            End If
        End If
        Debug.WriteLine("ProgRingShow(" & bVisible & ", " & bForce & "...), current ShowCnt=" & _mProgRingShowCnt)


        Try
            Dim _mProgRing As ProgressRing = TryCast(oPage.FindName("uiPkAutoProgRing"), ProgressRing)
            If _mProgRingShowCnt > 0 Then
                VBlib.DebugOut("ProgRingShow - mam pokazac")
                If _mProgRing IsNot Nothing Then
                    Dim dSize As Double
                    dSize = (Math.Min(TryCast(_mProgRing.Parent, Grid).ActualHeight, TryCast(_mProgRing.Parent, Grid).ActualWidth)) / 2
                    dSize = Math.Max(dSize, 50) ' g³ównie na póŸniej, dla Android
                    _mProgRing.Width = dSize
                    _mProgRing.Height = dSize

                    _mProgRing.Visibility = Visibility.Visible
                    _mProgRing.IsActive = True
                End If
                If _mProgBar IsNot Nothing Then _mProgBar.Visibility = Visibility.Visible
            Else
                VBlib.DebugOut("ProgRingShow - mam ukryc")
                If _mProgRing IsNot Nothing Then
                    _mProgRing.Visibility = Visibility.Collapsed
                    _mProgRing.IsActive = False
                End If
                If _mProgBar IsNot Nothing Then _mProgBar.Visibility = Visibility.Collapsed
            End If

        Catch ex As Exception
        End Try
    End Sub

    <Extension()>
    Public Sub ProgRingMaxVal(ByVal oPage As Page, dMaxValue As Double)
        Dim _mProgBar As ProgressBar = TryCast(oPage.FindName("uiPkAutoProgBar"), ProgressBar)
        If _mProgBar Is Nothing Then
            ' skoro to nie Grid, to nie ma jak umiescic koniecznych elementow
            Debug.WriteLine("ProgRing(double) wymaga wczesniej ProgRingInit")
            Throw New ArgumentException("ProgRing(double) wymaga wczesniej ProgRingInit")
        End If

        _mProgBar.Maximum = dMaxValue

    End Sub

    <Extension()>
    Public Sub ProgRingVal(ByVal oPage As Page, dValue As Double)
        Dim _mProgBar As ProgressBar = TryCast(oPage.FindName("uiPkAutoProgBar"), ProgressBar)
        If _mProgBar Is Nothing Then
            ' skoro to nie Grid, to nie ma jak umiescic koniecznych elementow
            Debug.WriteLine("ProgRing(double) wymaga wczesniej ProgRingInit")
            Throw New ArgumentException("ProgRing(double) wymaga wczesniej ProgRingInit")
        End If

        _mProgBar.Value = dValue

    End Sub

    <Extension()>
    Public Sub ProgRingInc(ByVal oPage As Page)
        Dim _mProgBar As ProgressBar = TryCast(oPage.FindName("uiPkAutoProgBar"), ProgressBar)
        If _mProgBar Is Nothing Then
            ' skoro to nie Grid, to nie ma jak umiescic koniecznych elementow
            Debug.WriteLine("ProgRing(double) wymaga wczesniej ProgRingInit")
            Throw New ArgumentException("ProgRing(double) wymaga wczesniej ProgRingInit")
        End If

        Dim dVal As Double = _mProgBar.Value + 1
        If dVal > _mProgBar.Maximum Then
            Debug.WriteLine("ProgRingInc na wiecej niz Maximum?")
            _mProgBar.Value = _mProgBar.Maximum
        Else
            _mProgBar.Value = dVal
        End If

    End Sub




#End Region

#Region "Bluetooth debug strings"

    <Extension()>
    Public Function ToDebugString(ByVal oAdv As Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisement) As String

        If oAdv Is Nothing Then
            Return "ERROR: Advertisement is Nothing, unmoglich!"
        End If

        Dim sRet As String = ""

        If oAdv.DataSections IsNot Nothing Then
            sRet = sRet & "Adverisement, number of data sections: " & oAdv.DataSections.Count & vbCrLf
            For Each oItem As Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementDataSection In oAdv.DataSections
                sRet = sRet & " DataSection: " & oItem.Data.ToDebugString(32)
            Next
        End If

        If oAdv.Flags IsNot Nothing Then sRet = sRet & "Adv.Flags: " & CInt(oAdv.Flags) & vbCrLf

        sRet = sRet & "Adv local name: " & oAdv.LocalName & vbCrLf

        If oAdv.ManufacturerData IsNot Nothing Then
            For Each oItem As Windows.Devices.Bluetooth.Advertisement.BluetoothLEManufacturerData In oAdv.ManufacturerData
                sRet = sRet & " ManufacturerData.Company: " & oItem.CompanyId & vbCrLf
                sRet = sRet & " ManufacturerData.Data: " & oItem.Data.ToDebugString(32) & vbCrLf
            Next
        End If

        If oAdv.ServiceUuids IsNot Nothing Then
            For Each oItem As Guid In oAdv.ServiceUuids
                sRet = sRet & " service " & oItem.ToString & vbCrLf
            Next
        End If

        Return sRet
    End Function

    <Extension()>
    Public Async Function ToDebugStringAsync(ByVal oDescriptor As Windows.Devices.Bluetooth.GenericAttributeProfile.GattDescriptor) As Task(Of String)
        Dim sRet As String

        sRet = "      descriptor: " & oDescriptor.Uuid.ToString & vbTab & oDescriptor.Uuid.AsGattReservedDescriptorName & vbCrLf
        Dim oRdVal = Await oDescriptor.ReadValueAsync
        If oRdVal.Status = Windows.Devices.Bluetooth.GenericAttributeProfile.GattCommunicationStatus.Success Then
            Dim oVal = oRdVal.Value
            sRet = sRet & oVal.ToArray.ToDebugString(8) & vbCrLf
        Else
            sRet = sRet & "      ReadValueAsync status = " & oRdVal.Status.ToString & vbCrLf
        End If
        Return sRet
    End Function


    <Extension()>
    Public Function ToDebugString(ByVal oProp As Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristicProperties) As String

        Dim sRet As String = "      CharacteristicProperties: "

        If oProp.HasFlag(Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristicProperties.Read) Then
            sRet &= "[read] "
        End If

        If oProp.HasFlag(Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristicProperties.AuthenticatedSignedWrites) Then
            sRet &= "[AuthenticatedSignedWrites] "
        End If
        If oProp.HasFlag(Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristicProperties.Broadcast) Then
            sRet &= "[broadcast] "
        End If
        If oProp.HasFlag(Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristicProperties.Indicate) Then
            sRet &= "[indicate] "
            ' bCanRead = False
        End If
        If oProp.HasFlag(Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristicProperties.None) Then
            sRet &= "[NONE] "
        End If
        If oProp.HasFlag(Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristicProperties.Notify) Then
            sRet &= "[notify] "
            ' bCanRead = False
        End If
        If oProp.HasFlag(Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristicProperties.ReliableWrites) Then
            sRet &= "[reliableWrite] "
        End If
        If oProp.HasFlag(Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristicProperties.Write) Then
            sRet &= "[write] "
        End If
        If oProp.HasFlag(Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristicProperties.WritableAuxiliaries) Then
            sRet &= "[WritableAuxiliaries] "
        End If
        If oProp.HasFlag(Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristicProperties.WriteWithoutResponse) Then
            sRet &= "[writeNoResponse] "
        End If

        Return sRet
    End Function

    <Extension()>
    Public Async Function ToDebugStringAsync(ByVal oChar As Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic) As Task(Of String)

        Dim sRet As String = "      CharacteristicProperties: " & oChar.CharacteristicProperties.ToDebugString & vbCrLf
        Dim bCanRead As Boolean = False
        If sRet.Contains("[read]") Then bCanRead = True
        ' ewentualnie wygaszenie gdy:
        'sProp &= "[indicate] "
        ' bCanRead = False
        '   sProp &= "[notify] "
        ' bCanRead = False


        Dim oDescriptors = Await oChar.GetDescriptorsAsync
        If oDescriptors Is Nothing Then Return sRet

        If oDescriptors.Status <> Windows.Devices.Bluetooth.GenericAttributeProfile.GattCommunicationStatus.Success Then
            sRet = sRet & "      GetDescriptorsAsync.Status = " & oDescriptors.Status.ToString & vbCrLf
            Return sRet
        End If


        For Each oDescr In oDescriptors.Descriptors
            sRet = sRet & Await oDescr.ToDebugStringAsync & vbCrLf
        Next

        If bCanRead Then
            Dim oRd = Await oChar.ReadValueAsync()
            If oRd.Status <> Windows.Devices.Bluetooth.GenericAttributeProfile.GattCommunicationStatus.Success Then
                sRet = sRet & "ReadValueAsync.Status=" & oRd.Status & vbCrLf
            Else
                sRet = sRet & "      characteristic data (read):" & vbCrLf
                sRet = sRet & oRd.Value.ToArray.ToDebugString(8) & vbCrLf
            End If

        End If

        Return sRet
    End Function

    <Extension()>
    Public Async Function ToDebusStringAsync(ByVal oServ As Windows.Devices.Bluetooth.GenericAttributeProfile.GattDeviceService) As Task(Of String)

        If oServ Is Nothing Then Return ""

        Dim oChars = Await oServ.GetCharacteristicsAsync
        If oChars Is Nothing Then Return ""
        If oChars.Status <> Windows.Devices.Bluetooth.GenericAttributeProfile.GattCommunicationStatus.Success Then
            Return "    GetCharacteristicsAsync.Status = " & oChars.Status.ToString
        End If

        Dim sRet As String = ""
        For Each oChr In oChars.Characteristics
            sRet = sRet & vbCrLf & "    characteristic: " & oChr.Uuid.ToString & oChr.Uuid.AsGattReservedCharacteristicName & vbCrLf
            sRet = sRet & Await oChr.ToDebugStringAsync & vbCrLf
        Next

        Return sRet
    End Function

    <Extension()>
    Public Async Function ToDebusStringAsync(ByVal oDevice As Windows.Devices.Bluetooth.BluetoothLEDevice) As Task(Of String)

        'If oDevice.BluetoothAddress = mLastBTdeviceDumped Then
        '    DebugOut("DebugBTdevice, but MAC same as previous - skipping")
        '    Return
        'End If
        'mLastBTdeviceDumped = oDevice.BluetoothAddress

        Dim sRet As String = ""

        sRet = sRet & "DebugBTdevice, data dump:" & vbCrLf
        sRet = sRet & "Device name: " & oDevice.Name & vbCrLf
        sRet = sRet & "MAC address: " & oDevice.BluetoothAddress.ToHexBytesString & vbCrLf
        sRet = sRet & "Connection status: " & oDevice.ConnectionStatus.ToString & vbCrLf

        Dim oDAI = oDevice.DeviceAccessInformation
        sRet = sRet & vbCrLf & "DeviceAccessInformation:" & vbCrLf
        sRet = sRet & "  CurrentStatus: " & oDAI.CurrentStatus.ToString & vbCrLf

        Dim oDApperr = oDevice.Appearance
        sRet = sRet & vbCrLf & "Appearance:" & vbCrLf
        sRet = sRet & "  Category: " & oDApperr.Category & vbCrLf
        sRet = sRet & "  Subcategory: " & oDApperr.SubCategory & vbCrLf

        sRet = sRet & "Services: " & oDApperr.SubCategory & vbCrLf

        Dim oSrv = Await oDevice.GetGattServicesAsync
        If oSrv.Status <> Windows.Devices.Bluetooth.GenericAttributeProfile.GattCommunicationStatus.Success Then
            sRet = sRet & "  GetGattServicesAsync.Status = " & oSrv.Status.ToString & vbCrLf
            Return sRet
        End If

        For Each oSv In oSrv.Services
            sRet = sRet & vbCrLf & "  service: " & oSv.Uuid.ToString & vbTab & vbTab & oSv.Uuid.AsGattReservedServiceName & vbCrLf
            sRet = sRet & Await oSv.ToDebusStringAsync & vbCrLf
        Next

        Return sRet

    End Function


#End Region
End Module


#Region ".Net configuration - UWP settings"

Public Class UwpConfigurationProvider
    ' Inherits MsExtConfig.ConfigurationProvider
    Implements MsExtConfig.IConfigurationProvider

    Private ReadOnly _roamPrefix1 As String = Nothing
    Private ReadOnly _roamPrefix2 As String = Nothing

    ''' <summary>
    ''' Create Configuration Provider, for LocalSettings and RoamSettings
    ''' </summary>
    ''' <param name="sRoamPrefix1">prefix for RoamSettings, use NULL if want only LocalSettings</param>
    ''' <param name="sRoamPrefix2">prefix for RoamSettings, use NULL if want only LocalSettings</param>
    Public Sub New(Optional sRoamPrefix1 As String = "[ROAM]", Optional sRoamPrefix2 As String = Nothing)
        Data = New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        _roamPrefix1 = sRoamPrefix1
        _roamPrefix2 = sRoamPrefix2
    End Sub

    Private Sub LoadData(settSource As IPropertySet)
        For Each oItem In settSource
            Data(oItem.Key) = oItem.Value
        Next
    End Sub

    ''' <summary>
    ''' read current state of settings (all values); although it is not used in TryGet, but we should have Data property set for other reasons (e.g. for listing all variables)...
    ''' </summary>
    Public Sub Load() Implements MsExtConfig.IConfigurationProvider.Load
        LoadData(WinAppData.Current.RoamingSettings.Values)
        LoadData(WinAppData.Current.LocalSettings.Values)
    End Sub


    ''' <summary>
    ''' always set LocalSettings, and if value is prefixed with Roam prefix, also RoamSettings (prefix is stripped)
    ''' </summary>
    ''' <param name="key"></param>
    ''' <param name="value"></param>
    Public Sub [Set](key As String, value As String) Implements MsExtConfig.IConfigurationProvider.Set
        If value Is Nothing Then value = ""

        If _roamPrefix1 IsNot Nothing AndAlso value.ToUpperInvariant().StartsWith(_roamPrefix1, StringComparison.Ordinal) Then
            value = value.Substring(_roamPrefix1.Length)
            Try
                WinAppData.Current.RoamingSettings.Values(key) = value
            Catch
                ' probably length is too big
            End Try
        End If

        If _roamPrefix2 IsNot Nothing AndAlso value.ToUpperInvariant().StartsWith(_roamPrefix2, StringComparison.Ordinal) Then
            value = value.Substring(_roamPrefix2.Length)
            Try
                WinAppData.Current.RoamingSettings.Values(key) = value
            Catch
                ' probably length is too big
            End Try
        End If

        Data(key) = value
        Try
            WinAppData.Current.LocalSettings.Values(key) = value
        Catch
            ' probably length is too big
        End Try

    End Sub

    ''' <summary>
    ''' this is used only for iterating keys, not for Get/Set
    ''' </summary>
    ''' <returns></returns>
    Protected Property Data As IDictionary(Of String, String)

    ''' <summary>
    ''' gets current Value of Key; local value overrides roaming value
    ''' </summary>
    ''' <returns>True if Key is found (and Value is set)</returns>
    Public Function TryGet(key As String, ByRef value As String) As Boolean Implements MsExtConfig.IConfigurationProvider.TryGet

        Dim bFound As Boolean = False

        If WinAppData.Current.RoamingSettings.Values.ContainsKey(key) Then
            value = WinAppData.Current.RoamingSettings.Values(key).ToString
            bFound = True
        End If

        If WinAppData.Current.LocalSettings.Values.ContainsKey(key) Then
            value = WinAppData.Current.LocalSettings.Values(key).ToString
            bFound = True
        End If

        Return bFound

    End Function

    Public Function GetReloadToken() As MsExtPrim.IChangeToken Implements MsExtConfig.IConfigurationProvider.GetReloadToken
        Return New ConfigurationReloadToken
    End Function

    Public Function GetChildKeys(earlierKeys As IEnumerable(Of String), parentPath As String) As IEnumerable(Of String) Implements MsExtConfig.IConfigurationProvider.GetChildKeys
        ' in this configuration, we don't have structure - so just return list

        Dim results As New List(Of String)
        For Each kv As KeyValuePair(Of String, String) In Data
            results.Add(kv.Key)
        Next

        results.Sort()

        Return results
    End Function

End Class

Public Class UwpConfigurationSource
    Implements MsExtConfig.IConfigurationSource

    Private ReadOnly _roamPrefix1 As String = Nothing
    Private ReadOnly _roamPrefix2 As String = Nothing

    Public Function Build(builder As MsExtConfig.IConfigurationBuilder) As MsExtConfig.IConfigurationProvider Implements MsExtConfig.IConfigurationSource.Build
        Return New UwpConfigurationProvider(_roamPrefix1, _roamPrefix2)
    End Function

    Public Sub New(Optional sRoamPrefix1 As String = "[ROAM]", Optional sRoamPrefix2 As String = Nothing)
        _roamPrefix1 = sRoamPrefix1
        _roamPrefix2 = sRoamPrefix2
    End Sub
End Class

Partial Module Extensions
    <Runtime.CompilerServices.Extension()>
    Public Function AddUwpSettings(ByVal configurationBuilder As MsExtConfig.IConfigurationBuilder, Optional sRoamPrefix1 As String = "[ROAM]", Optional sRoamPrefix2 As String = Nothing) As MsExtConfig.IConfigurationBuilder
        configurationBuilder.Add(New UwpConfigurationSource(sRoamPrefix1, sRoamPrefix2))
        Return configurationBuilder
    End Function
End Module


#End Region

#Region "Konwertery Bindings XAML"
' nie mogą być w VBlib, bo Implements Microsoft.UI.Xaml.Data.IValueConverter

' parameter = NEG robi negację
Public Class KonwersjaVisibility
    Implements IValueConverter

    Public Function Convert(ByVal value As Object,
    ByVal targetType As Type, ByVal parameter As Object,
    ByVal language As System.String) As Object _
    Implements IValueConverter.Convert

        Dim bTemp As Boolean = CType(value, Boolean)
        If parameter IsNot Nothing Then
            Dim sParam As String = CType(parameter, String)
            If sParam.ToUpperInvariant = "NEG" Then bTemp = Not bTemp
        End If
        If bTemp Then Return Visibility.Visible

        Return Visibility.Collapsed

    End Function


    ' ConvertBack is not implemented for a OneWay binding.
    Public Function ConvertBack(ByVal value As Object,
    ByVal targetType As Type, ByVal parameter As Object,
    ByVal language As System.String) As Object _
    Implements IValueConverter.ConvertBack

        Throw New NotImplementedException

    End Function
End Class

' ULONG to String
Public Class KonwersjaMAC
    Implements IValueConverter

    ' Define the Convert method to change a DateTime object to
    ' a month string.
    Public Function Convert(ByVal value As Object,
            ByVal targetType As Type, ByVal parameter As Object,
            ByVal language As System.String) As Object _
            Implements IValueConverter.Convert

        ' value is the data from the source object.

        Dim uMAC As ULong = CType(value, ULong)
        If uMAC = 0 Then Return ""

        Return uMAC.ToHexBytesString()

    End Function

    ' ConvertBack is not implemented for a OneWay binding.
    Public Function ConvertBack(ByVal value As Object,
            ByVal targetType As Type, ByVal parameter As Object,
            ByVal language As System.String) As Object _
            Implements IValueConverter.ConvertBack

        Throw New NotImplementedException

    End Function
End Class

Public Class KonwersjaVal2StringFormat
    Implements IValueConverter

    ' Define the Convert method to change a DateTime object to
    ' a month string.
    Public Function Convert(ByVal value As Object,
            ByVal targetType As Type, ByVal parameter As Object,
            ByVal language As System.String) As Object _
            Implements IValueConverter.Convert

        Dim sFormat As String = ""
        If parameter IsNot Nothing Then
            sFormat = CType(parameter, String)
        End If

        ' value is the data from the source object.
        If value.GetType Is GetType(Integer) Then
            Dim temp = CType(value, Integer)
            If sFormat = "" Then
                Return temp.ToString
            Else
                Return temp.ToString(sFormat)
            End If
        End If

        If value.GetType Is GetType(Long) Then
            Dim temp = CType(value, Long)
            If sFormat = "" Then
                Return temp.ToString
            Else
                Return temp.ToString(sFormat)
            End If
        End If

        If value.GetType Is GetType(Double) Then
            Dim temp = CType(value, Double)
            If sFormat = "" Then
                Return temp.ToString
            Else
                Return temp.ToString(sFormat)
            End If
        End If

        If value.GetType Is GetType(String) Then
            Dim temp = CType(value, String)
            If sFormat = "" Then
                Return temp.ToString
            Else
                Return String.Format(sFormat, temp)
            End If
        End If

        Return "???"

    End Function

    ' ConvertBack is not implemented for a OneWay binding.
    Public Function ConvertBack(ByVal value As Object,
            ByVal targetType As Type, ByVal parameter As Object,
            ByVal language As System.String) As Object _
            Implements IValueConverter.ConvertBack

        Throw New NotImplementedException

    End Function


End Class


#End Region

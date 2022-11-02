Imports Vblib.Extensions


Partial Public Class App
    Inherits Application

    ' not supported
    '#Region "Back button"
    '#Region "RemoteSystem/Background"
#Region "Commandline"
    Private msLocalCmdsHelp As String = ""

    Sub app_Startup(sender As Object, e As StartupEventArgs)
        If e.Args.Length > 0 Then
            Dim sCmdLine As String = ""
            For Each oneCmd In e.Args
                If sCmdLine <> "" Then sCmdLine &= " "
                sCmdLine &= oneCmd
            Next

            ObsluzCommandLine(sCmdLine).Wait()

        End If
    End Sub

    Public Async Function CmdLineOrRemSys(sCommand As String) As Task(Of String)
        Dim sResult As String = AppServiceStdCmd(sCommand, msLocalCmdsHelp)
        If String.IsNullOrEmpty(sResult) Then
            sResult = Await AppServiceLocalCommand(sCommand)
        End If

        Return sResult
    End Function

    Public Async Function ObsluzCommandLine(sCommand As String) As Task

        Dim sResult = Await CmdLineOrRemSys(sCommand)
        If String.IsNullOrEmpty(sResult) Then
            sResult = "(empty - probably unrecognized command)"
        End If

        Console.WriteLine(sResult)
    End Function

#End Region

End Class

Public Module pkar

    ''' <summary>
    ''' dla starszych: InitLib(Nothing)
    ''' dla nowszych:  InitLib(Environment.GetCommandLineArgs)
    ''' </summary>
    Public Sub InitLib(aCmdLineArgs As List(Of String), Optional bUseOwnFolderIfNotSD As Boolean = True)

        InitSettings(aCmdLineArgs)
        Vblib.LibInitToast(AddressOf FromLibMakeToast)
        Vblib.LibInitDialogBox(AddressOf FromLibDialogBoxAsync, AddressOf FromLibDialogBoxYNAsync, AddressOf FromLibDialogBoxInputAllDirectAsync)

        Vblib.LibInitClip(AddressOf FromLibClipPut, AddressOf FromLibClipPutHtml)
#Disable Warning BC42358 ' Because this call is not awaited, execution of the current method continues before the call is completed
        ' InitDatalogFolder(bUseOwnFolderIfNotSD)
#Enable Warning BC42358 ' Because this call is not awaited, execution of the current method continues before the call is completed
    End Sub

#Region "CrashMessage"
    ' większość w VBlib

    ''' <summary>
    ''' DialogBox z dotychczasowym logiem i skasowanie logu
    ''' </summary>
    Public Async Function CrashMessageShowAsync() As Task
        Dim sTxt As String = Vblib.GetSettingsString("appFailData")
        If sTxt = "" Then Return
        Await Vblib.DialogBoxAsync("FAIL messages:" & vbCrLf & sTxt)
        Vblib.SetSettingsString("appFailData", "")
    End Function

    ''' <summary>
    ''' Dodaj do logu, ewentualnie toast, i zakończ App
    ''' </summary>
    Public Sub CrashMessageExit(sTxt As String, exMsg As String)
        Vblib.CrashMessageAdd(sTxt, exMsg)
        Application.Current.Shutdown()
    End Sub

#End Region

    ' -- CLIPBOARD ---------------------------------------------

#Region "ClipBoard"
    Private Sub FromLibClipPut(sTxt As String)
        Clipboard.SetText(sTxt)
    End Sub

    Private Sub FromLibClipPutHtml(sHtml As String)
        Clipboard.SetText(sHtml, TextDataFormat.Html)
    End Sub

    ''' <summary>
    ''' w razie Catch() zwraca ""
    ''' </summary>
    Public Async Function ClipGetAsync() As Task(Of String)
        Return Clipboard.GetText()
    End Function
#End Region


    ' -- Get/Set Settings ---------------------------------------------

#Region "Get/Set settings"
    ''' <summary>
    ''' inicjalizacja pełnych zmiennych, bez tego wywołania będą tylko defaulty z pliku INI (i nie będzie pamiętania)
    ''' </summary>
    Private Sub InitSettings(aCmdLineArgs As List(Of String))
        Dim sAppName As String = Application.Current.MainWindow.GetType().Assembly.GetName.Name

        Dim oBuilder As New Microsoft.Extensions.Configuration.ConfigurationBuilder()
        oBuilder = oBuilder.AddIniRelDebugSettings(Vblib.IniLikeDefaults.sIniContent)   ' defaults.ini w głównym katalogu Project, sekcje [main] oraz [debug]

        ' ale i tak jest Empty
        Dim oDict As IDictionary = Environment.GetEnvironmentVariables()    ' że, w 1.4, zwraca HashTable?
        oBuilder = oBuilder.AddEnvironmentVariablesROConfigurationSource(sAppName, oDict) ' Environment.GetEnvironmentVariables, Std 2.0

        Dim sPathLocal As String = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), sAppName)
        Dim sPathRoam As String = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), sAppName)

        oBuilder = oBuilder.AddJsonRwSettings(sPathLocal, sPathRoam)
        If aCmdLineArgs IsNot Nothing Then oBuilder = oBuilder.AddCommandLineRO(aCmdLineArgs)  ' Environment.GetCommandLineArgs, Std 1.5, ale nie w UWP?

        Dim settings As Microsoft.Extensions.Configuration.IConfigurationRoot = oBuilder.Build

        Vblib.LibInitSettings(settings)
    End Sub

#If FalseThen Then

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
        Return False
    End Function

    Public Function IsFamilyDesktop() As Boolean
        Return True
    End Function

    ' <Obsolete("Jest w .Net Standard 2.0 (lib)")>
    Public Function NetIsIPavailable(Optional bMsg As Boolean = False) As Boolean
        If Vblib.GetSettingsBool("offline") Then Return False

        If Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable() Then Return True
        If bMsg Then
            Vblib.DialogBox("ERROR: no IP network available")
        End If
        Return False
    End Function

    '' <Obsolete("Jest w .Net Standard 2.0 (lib), ale on jest nie do telefonu :)")>
    'Public Function NetIsCellInet() As Boolean
    '    Return Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile().IsWwanConnectionProfile
    'End Function

    Public Function GetHostName() As String
        ' można tak sprawdzić wszystkie, i jeśli jes 
        For Each oNIC As Net.NetworkInformation.NetworkInterface In Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()

            ' jeśli nie jest to link aktywny, to ignorujemy go
            If oNIC.OperationalStatus <> Net.NetworkInformation.OperationalStatus.Up Then Continue For

            ' nie jest to pełna logika, bo mogą być dodatkowe typy kiedyś...
            Select Case oNIC.NetworkInterfaceType
                Case Net.NetworkInformation.NetworkInterfaceType.Wman
                    Return True
                Case Net.NetworkInformation.NetworkInterfaceType.Wwanpp
                    Return True
                Case Net.NetworkInformation.NetworkInterfaceType.Wwanpp2
                    Return True
                Case Net.NetworkInformation.NetworkInterfaceType.GenericModem
                    Return True
                Case Net.NetworkInformation.NetworkInterfaceType.HighPerformanceSerialBus
                    Return True
                Case Net.NetworkInformation.NetworkInterfaceType.Ppp
                    Return True
                Case Net.NetworkInformation.NetworkInterfaceType.Slip
                    Return True
            End Select
        Next

        Return False
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

    '''' <summary>
    '''' w razie Catch() zwraca false
    '''' </summary>
    'Public Async Function NetWiFiOffOnAsync() As Task(Of Boolean)

    '    Try
    '        ' https://social.msdn.microsoft.com/Forums/ie/en-US/60c4a813-dc66-4af5-bf43-e632c5f85593/uwpbluetoothhow-to-turn-onoff-wifi-bluetooth-programmatically?forum=wpdevelop
    '        Dim result222 As Windows.Devices.Radios.RadioAccessStatus = Await Windows.Devices.Radios.Radio.RequestAccessAsync()
    '        If result222 <> Windows.Devices.Radios.RadioAccessStatus.Allowed Then Return False

    '        Dim radios As IReadOnlyList(Of Windows.Devices.Radios.Radio) = Await Windows.Devices.Radios.Radio.GetRadiosAsync()

    '        For Each oRadio In radios
    '            If oRadio.Kind = Windows.Devices.Radios.RadioKind.WiFi Then
    '                Dim oStat As Windows.Devices.Radios.RadioAccessStatus =
    '            Await oRadio.SetStateAsync(Windows.Devices.Radios.RadioState.Off)
    '                If oStat <> Windows.Devices.Radios.RadioAccessStatus.Allowed Then Return False
    '                Await Task.Delay(3 * 1000)
    '                oStat = Await oRadio.SetStateAsync(Windows.Devices.Radios.RadioState.On)
    '                If oStat <> Windows.Devices.Radios.RadioAccessStatus.Allowed Then Return False
    '            End If
    '        Next

    '        Return True
    '    Catch ex As Exception
    '        Return False
    '    End Try
    'End Function

    'Public Sub OpenBrowser(sLink As String)
    '        Dim oUri As New Uri(sLink)
    '        oUri.OpenBrowser
    '    End Sub

#Region "Bluetooth"
    '    ''' <summary>
    '    ''' Zwraca -1 (no radio), 0 (off), 1 (on), ale gdy bMsg to pokazuje dokładniej błąd (nie włączony, albo nie ma radia Bluetooth) - wedle stringów podanych, które mogą być jednak identyfikatorami w Resources
    '    ''' </summary>
    '    Public Async Function NetIsBTavailableAsync(bMsg As Boolean,
    '                                    Optional bRes As Boolean = False,
    '                                    Optional sBtDisabled As String = "ERROR: Bluetooth is not enabled",
    '                                    Optional sNoRadio As String = "ERROR: Bluetooth radio not found") As Task(Of Integer)


    '            'Dim result222 As Windows.Devices.Radios.RadioAccessStatus = Await Windows.Devices.Radios.Radio.RequestAccessAsync()
    '            'If result222 <> Windows.Devices.Radios.RadioAccessStatus.Allowed Then Return -1

    '            Dim oRadios As IReadOnlyList(Of Windows.Devices.Radios.Radio) = Await Windows.Devices.Radios.Radio.GetRadiosAsync()

    '#If DEBUG Then
    '        VBlib.DumpCurrMethod(", count=" & oRadios.Count)
    '        For Each oRadio As Windows.Devices.Radios.Radio In oRadios
    '            VBlib.DumpMessage("NEXT RADIO")
    '            VBlib.DumpMessage("name=" & oRadio.SourceName)
    '            VBlib.DumpMessage("kind=" & oRadio.Kind)
    '            VBlib.DumpMessage("state=" & oRadio.State)
    '        Next
    '#End If

    '            Dim bHasBT As Boolean = False

    '            For Each oRadio As Windows.Devices.Radios.Radio In oRadios
    '                If oRadio.Kind = Windows.Devices.Radios.RadioKind.Bluetooth Then
    '                    If oRadio.State = Windows.Devices.Radios.RadioState.On Then Return 1
    '                    bHasBT = True
    '                End If
    '            Next

    '            If bHasBT Then
    '                If bMsg Then
    '                    If bRes Then
    '                        Await Vblib.DialogBoxResAsync(sBtDisabled)
    '                    Else
    '                        Await Vblib.DialogBoxAsync(sBtDisabled)
    '                    End If
    '                End If
    '                Return 0
    '            Else
    '                If bMsg Then
    '                    If bRes Then
    '                        Await Vblib.DialogBoxResAsync(sNoRadio)
    '                    Else
    '                        Await Vblib.DialogBoxAsync(sNoRadio)
    '                    End If
    '                End If
    '                Return -1
    '            End If


    '        End Function

    '        ''' <summary>
    '        ''' Zwraca true/false czy State (po call) jest taki jak bOn; wymaga devCap=radios
    '        ''' </summary>
    '        Public Async Function NetTrySwitchBTOnAsync(bOn As Boolean) As Task(Of Boolean)
    '            Dim iCurrState As Integer = Await NetIsBTavailableAsync(False)
    '            If iCurrState = -1 Then Return False

    '            ' jeśli nie trzeba przełączać... 
    '            If bOn AndAlso iCurrState = 1 Then Return True
    '            If Not bOn AndAlso iCurrState = 0 Then Return True

    '            ' czy mamy prawo przełączyć? (devCap=radios)
    '            Dim result222 As Windows.Devices.Radios.RadioAccessStatus = Await Windows.Devices.Radios.Radio.RequestAccessAsync()
    '            If result222 <> Windows.Devices.Radios.RadioAccessStatus.Allowed Then Return False


    '            Dim radios As IReadOnlyList(Of Windows.Devices.Radios.Radio) = Await Windows.Devices.Radios.Radio.GetRadiosAsync()

    '            For Each oRadio In radios
    '                If oRadio.Kind = Windows.Devices.Radios.RadioKind.Bluetooth Then
    '                    Dim oStat As Windows.Devices.Radios.RadioAccessStatus
    '                    If bOn Then
    '                        oStat = Await oRadio.SetStateAsync(Windows.Devices.Radios.RadioState.On)
    '                    Else
    '                        oStat = Await oRadio.SetStateAsync(Windows.Devices.Radios.RadioState.Off)
    '                    End If
    '                    If oStat <> Windows.Devices.Radios.RadioAccessStatus.Allowed Then Return False
    '                End If
    '            Next

    '            Return True
    '        End Function

#End Region

#End Region


    ' -- DialogBoxy - tylko jako wskok z VBLib ---------------------------------------------

#Region "DialogBoxy"

    Public Async Function FromLibDialogBoxAsync(sMsg As String) As Task
        Dim sAppName As String = Application.Current.MainWindow.GetType().Assembly.GetName.Name
        MessageBox.Show(sMsg, sAppName)
    End Function

    ''' <summary>
    ''' Dla Cancel zwraca ""
    ''' </summary>
    Public Async Function FromLibDialogBoxYNAsync(sMsg As String, Optional sYes As String = "Tak", Optional sNo As String = "Nie") As Task(Of Boolean)
        Dim sAppName As String = Application.Current.MainWindow.GetType().Assembly.GetName.Name
        Dim iRet As MessageBoxResult = MessageBox.Show(sMsg, sAppName, MessageBoxButton.YesNo)
        If iRet = MessageBoxResult.Yes Then Return True
        Return False
    End Function

    Public Async Function FromLibDialogBoxInputAllDirectAsync(sMsg As String, Optional sDefault As String = "", Optional sYes As String = "Continue", Optional sNo As String = "Cancel") As Task(Of String)
        Dim sAppName As String = Application.Current.MainWindow.GetType().Assembly.GetName.Name
        Return InputBox(sMsg, sAppName, sDefault)
    End Function


#End Region


    ' --- INNE FUNKCJE ------------------------
#Region "Toasty itp"

    Private Sub FromLibMakeToast(sMsg As String, sMsg1 As String)
        MessageBox.Show("Not yet")
    End Sub

    ''' <summary>
    ''' dwa kolejne teksty, sMsg oraz sMsg1
    ''' </summary>
    Public Sub MakeToast(sMsg As String, Optional sMsg1 As String = "")
        FromLibMakeToast(sMsg, sMsg1)
    End Sub
    Public Sub MakeToast(oDate As DateTime, sMsg As String, Optional sMsg1 As String = "")
        MessageBox.Show("Not yet")
    End Sub

#End Region

#Region "WinVer, AppVer"

    Public Function GetAppVers() As String
        Return System.Reflection.Assembly.GetExecutingAssembly.GetName.Version.ToString()
    End Function

#End Region


#Region "triggers"
#Region "RemoteSystem"

    ''' <summary>
    ''' jeśli na wejściu jest jakaś standardowa komenda, to na wyjściu będzie jej rezultat. Else = ""
    ''' </summary>
    Public Function AppServiceStdCmd(sCommand As String, sLocalCmds As String) As String
        Dim sTmp As String = Vblib.LibAppServiceStdCmd(sCommand, sLocalCmds)
        If sTmp <> "" Then Return sTmp

        ' If sCommand.StartsWith("debug loglevel") Then - vbLib

        Select Case sCommand.ToLower()
            ' Case "ping" - vblib
            Case "ver"
                Return GetAppVers()
                'Case "localdir"
                '    Return Windows.Storage.ApplicationData.Current.LocalFolder.Path
            ' Case "appdir" - vblib
            'Case "installeddate"
            '        Return Windows.ApplicationModel.Package.Current.InstalledDate.ToString("yyyy.MM.dd HH:mm:ss")
            ' Case "help" - vblib

            ' Case "debug vars" - vblib
            'Case "debug triggers"
            '    Return DumpTriggers()
            'Case "debug toasts"
            '    Return DumpToasts()
            '    Case "debug memsize"
            '        Return Windows.System.MemoryManager.AppMemoryUsage.ToString() & "/" & Windows.System.MemoryManager.AppMemoryUsageLimit.ToString()
            '    Case "debug rungc"
            '        sTmp = "Memory usage before Global Collector call: " & Windows.System.MemoryManager.AppMemoryUsage.ToString() & vbCrLf
            '        GC.Collect()
            '        GC.WaitForPendingFinalizers()
            '        sTmp = sTmp & "After: " & Windows.System.MemoryManager.AppMemoryUsage.ToString() & "/" & Windows.System.MemoryManager.AppMemoryUsageLimit.ToString()
            '        Return sTmp
            '' Case "debug crashmsg"
            '' Case "debug crashmsg clear"

            '    Case "lib unregistertriggers"
            '        sTmp = DumpTriggers()
            '        UnregisterTriggers("") ' // całkiem wszystkie
            '        Return sTmp
            Case "lib isfamilymobile"
                Return IsFamilyMobile().ToString()
            Case "lib isfamilydesktop"
                Return IsFamilyDesktop().ToString()
            Case "lib netisipavailable"
                Return NetIsIPavailable(False).ToString()
                'Case "lib netiscellinet"
                '    Return NetIsCellInet().ToString()
            Case "lib gethostname"
                Return GetHostName()
            Case "lib isthismoje"
                Return IsThisMoje().ToString()
                'Case "lib istriggersregistered"
                '    Return IsTriggersRegistered().ToString()

                'Case "lib pkarmode 1"
                'Case "lib pkarmode 0"
                'Case "lib pkarmode"
        End Select

        Return ""  ' oznacza: to nie jest standardowa komenda
    End Function



#End Region


#End Region

End Module

Module Extensions


    '    <Extension()>
    '    Public Sub OpenExplorer(ByVal oFold As Windows.Storage.StorageFolder)
    '#Disable Warning BC42358 ' Because this call is not awaited, execution of the current method continues before the call is completed
    '        Windows.System.Launcher.LaunchFolderAsync(oFold)
    '#Enable Warning BC42358
    '    End Sub


    '    <Extension()>
    '    Public Sub OpenBrowser(ByVal oUri As System.Uri, Optional bForceEdge As Boolean = False)
    '        If bForceEdge Then
    '            ' tylko w FilteredRss
    '            Dim options = New Windows.System.LauncherOptions With
    '                {
    '                    .TargetApplicationPackageFamilyName = "Microsoft.MicrosoftEdge_8wekyb3d8bbwe"
    '                }
    '#Disable Warning BC42358 ' Because this call is not awaited, execution of the current method continues before the call is completed
    '            Windows.System.Launcher.LaunchUriAsync(oUri, options)
    '#Enable Warning BC42358
    '        Else

    '#Disable Warning BC42358 ' Because this call is not awaited, execution of the current method continues before the call is completed
    '            Windows.System.Launcher.LaunchUriAsync(oUri)
    '#Enable Warning BC42358
    '        End If
    '    End Sub



    '<Extension()>
    'Public Async Function GetDocumentHtml(ByVal uiWebView As WebView) As Task(Of String)
    '    Try
    '        Return Await uiWebView.InvokeScriptAsync("eval", New String() {"document.documentElement.outerHTML;"})
    '    Catch ex As Exception
    '        Return "" ' jesli strona jest pusta, jest Exception
    '    End Try
    'End Function


#Region "Settingsy jako Extension"
    <Runtime.CompilerServices.Extension>
    Public Sub GetSettingsString(ByVal oItem As TextBlock, Optional sName As String = "", Optional sDefault As String = "")
        If sName = "" Then sName = oItem.Name
        Dim sTxt As String = Vblib.GetSettingsString(sName, sDefault)
        oItem.Text = sTxt
    End Sub
    <Runtime.CompilerServices.Extension>
    Public Sub SetSettingsString(ByVal oItem As TextBlock, Optional sName As String = "", Optional bRoam As Boolean = False)
        If sName = "" Then sName = oItem.Name
        Vblib.SetSettingsString(sName, oItem.Text, bRoam)
    End Sub

    <Runtime.CompilerServices.Extension>
    Public Sub GetSettingsString(ByVal oItem As TextBox, Optional sName As String = "", Optional sDefault As String = "")
        If sName = "" Then sName = oItem.Name
        Dim sTxt As String = Vblib.GetSettingsString(sName, sDefault)
        oItem.Text = sTxt
    End Sub
    <Runtime.CompilerServices.Extension>
    Public Sub SetSettingsString(ByVal oItem As TextBox, Optional sName As String = "", Optional bRoam As Boolean = False)
        If sName = "" Then sName = oItem.Name
        Vblib.SetSettingsString(sName, oItem.Text, bRoam)
    End Sub

    '<Extension()>
    'Public Sub GetSettingsBool(ByVal oItem As ToggleSwitch, Optional sName As String = "", Optional bDefault As Boolean = False)
    '    If sName = "" Then sName = oItem.SourceName
    '    Dim bBool As Boolean = Vblib.GetSettingsBool(sName, bDefault)
    '    oItem.IsOn = bBool
    'End Sub
    '<Extension()>
    'Public Sub SetSettingsBool(ByVal oItem As ToggleSwitch, Optional sName As String = "", Optional bRoam As Boolean = False)
    '    If sName = "" Then sName = oItem.SourceName
    '    Vblib.SetSettingsBool(sName, oItem.IsOn, bRoam)
    'End Sub

    '<Extension()>
    'Public Sub GetSettingsBool(ByVal oItem As ToggleButton, Optional sName As String = "", Optional bDefault As Boolean = False)
    '    If sName = "" Then sName = oItem.SourceName
    '    Dim bBool As Boolean = Vblib.GetSettingsBool(sName, bDefault)
    '    oItem.IsChecked = bBool
    'End Sub
    '<Extension()>
    'Public Sub SetSettingsBool(ByVal oItem As ToggleButton, Optional sName As String = "", Optional bRoam As Boolean = False)
    '    If sName = "" Then sName = oItem.SourceName
    '    Vblib.SetSettingsBool(sName, oItem.IsChecked, bRoam)
    'End Sub

    '<Extension()>
    'Public Sub GetSettingsBool(ByVal oItem As AppBarToggleButton, Optional sName As String = "", Optional bDefault As Boolean = False)
    '    If sName = "" Then sName = oItem.SourceName
    '    Dim bBool As Boolean = Vblib.GetSettingsBool(sName, bDefault)
    '    oItem.IsChecked = bBool
    'End Sub
    '<Extension()>
    'Public Sub SetSettingsBool(ByVal oItem As AppBarToggleButton, Optional sName As String = "", Optional bRoam As Boolean = False)
    '    If sName = "" Then sName = oItem.SourceName
    '    Vblib.SetSettingsBool(sName, oItem.IsChecked, bRoam)
    'End Sub

    '''' <summary>
    '''' Zapisanie SettingsInt z możliwością skalowania (TextBox: zł.gr to dScale = 100)
    '''' </summary>
    '''' <param name="oItem"></param>
    '''' <param name="sName"></param>
    '''' <param name="bRoam"></param>
    '''' <param name="dScale"></param>
    '<Extension()>
    'Public Sub SetSettingsInt(ByVal oItem As TextBox, Optional sName As String = "", Optional bRoam As Boolean = False, Optional dScale As Double = 1)
    '    If sName = "" Then sName = oItem.SourceName
    '    Dim dTmp As Integer
    '    If Not Double.TryParse(oItem.Text, dTmp) Then Return
    '    dTmp *= dScale
    '    Vblib.SetSettingsInt(sName, dTmp, bRoam)
    'End Sub

    '''' <summary>
    '''' Pobranie SettingsInt z możliwością skalowania (TextBox: zł.gr to dScale = 100)
    '''' </summary>
    '''' <param name="oItem"></param>
    '''' <param name="sName"></param>
    '''' <param name="dScale"></param>
    '<Extension()>
    'Public Sub GetSettingsInt(ByVal oItem As TextBox, Optional sName As String = "", Optional dScale As Double = 1)
    '    If sName = "" Then sName = oItem.SourceName
    '    Dim dTmp As Integer = Vblib.GetSettingsInt(sName)
    '    dTmp /= dScale
    '    oItem.Text = dTmp
    'End Sub

    '<Extension()>
    'Public Sub SetSettingsInt(ByVal oItem As Windows.UI.Xaml.Controls.Slider, Optional sName As String = "", Optional bRoam As Boolean = False)
    '    If sName = "" Then sName = oItem.SourceName
    '    Vblib.SetSettingsInt(sName, oItem.Value, bRoam)
    'End Sub

    '<Extension()>
    'Public Sub GetSettingsInt(ByVal oItem As Windows.UI.Xaml.Controls.Slider, Optional sName As String = "")
    '    If sName = "" Then sName = oItem.SourceName
    '    oItem.Value = Vblib.GetSettingsInt(sName)
    'End Sub

    '<Extension()>
    'Public Sub SetSettingsDate(ByVal oItem As CalendarDatePicker, Optional sName As String = "", Optional bRoam As Boolean = False)
    '    If sName = "" Then sName = oItem.SourceName
    '    Vblib.SetSettingsDate(sName, oItem.Date.Value, bRoam)
    'End Sub

    '<Extension()>
    'Public Sub GetSettingsDate(ByVal oItem As CalendarDatePicker, Optional sName As String = "")
    '    If sName = "" Then sName = oItem.SourceName
    '    Dim dDTOff As DateTimeOffset = Vblib.GetSettingsDate(sName)
    '    oItem.Date = dDTOff
    'End Sub


#End Region

    <Runtime.CompilerServices.Extension>
    Public Sub ShowAppVers(ByVal oItem As TextBlock)
        Dim sTxt As String = pkar.GetAppVers()
#If DEBUG Then
        sTxt &= " (debug)" ' & GetBuildTimestamp() & ")"
#End If
        oItem.Text = sTxt
    End Sub

    <Runtime.CompilerServices.Extension>
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


    ' --- progring ------------------------

#Region "ProgressBar/Ring"
    '    ' dodałem 25 X 2020

    '    'Private _mProgRing As ProgressRing = Nothing
    '    'Private _mProgBar As ProgressBar = Nothing
    '    Private _mProgRingShowCnt As Integer = 0

    '    <Extension()>
    '    Public Sub ProgRingInit(ByVal oPage As Page, bRing As Boolean, bBar As Boolean)

    '        ' 2020.11.24: dodaję force-off do ProgRing na Init
    '        _mProgRingShowCnt = 0   ' skoro inicjalizuje, to znaczy że na pewno trzeba wyłączyć

    '        'Dim oFrame As Frame = Window.Current.Content
    '        'Dim oPage As Page = oFrame.Content
    '        Dim oGrid As Grid = TryCast(oPage.Content, Grid)
    '        If oGrid Is Nothing Then
    '            ' skoro to nie Grid, to nie ma jak umiescic koniecznych elementow
    '            Debug.WriteLine("ProgRingInit wymaga Grid jako podstawy Page")
    '            Throw New ArgumentException("ProgRingInit wymaga Grid jako podstawy Page")
    '        End If

    '        Dim iCols As Integer = 0
    '        If oGrid.ColumnDefinitions IsNot Nothing Then iCols = oGrid.ColumnDefinitions.Count ' mo¿e byæ 0
    '        Dim iRows As Integer = 0
    '        If oGrid.RowDefinitions IsNot Nothing Then iRows = oGrid.RowDefinitions.Count ' mo¿e byæ 0

    '        If oPage.FindName("uiPkAutoProgRing") Is Nothing Then
    '            If bRing Then
    '                Dim _mProgRing As New ProgressRing With {
    '                    .SourceName = "uiPkAutoProgRing",
    '                    .VerticalAlignment = VerticalAlignment.Center,
    '                    .HorizontalAlignment = HorizontalAlignment.Center,
    '                    .Visibility = Visibility.Collapsed
    '                }
    '                Canvas.SetZIndex(_mProgRing, 10000)
    '                If iRows > 1 Then
    '                    Grid.SetRow(_mProgRing, 0)
    '                    Grid.SetRowSpan(_mProgRing, iRows)
    '                End If
    '                If iCols > 1 Then
    '                    Grid.SetColumn(_mProgRing, 0)
    '                    Grid.SetColumnSpan(_mProgRing, iCols)
    '                End If
    '                oGrid.Children.Add(_mProgRing)
    '            End If
    '        End If

    '        If oPage.FindName("uiPkAutoProgBar") Is Nothing Then
    '            If bBar Then
    '                Dim _mProgBar As New ProgressBar With {
    '                    .SourceName = "uiPkAutoProgBar",
    '                    .VerticalAlignment = VerticalAlignment.Bottom,
    '                    .HorizontalAlignment = HorizontalAlignment.Stretch,
    '                    .Visibility = Visibility.Collapsed
    '                }
    '                Canvas.SetZIndex(_mProgBar, 10000)
    '                If iRows > 1 Then Grid.SetRow(_mProgBar, iRows - 1)
    '                If iCols > 1 Then
    '                    Grid.SetColumn(_mProgBar, 0)
    '                    Grid.SetColumnSpan(_mProgBar, iCols)
    '                End If
    '                oGrid.Children.Add(_mProgBar)
    '            End If
    '        End If

    '    End Sub

    '    <Extension()>
    '    Public Sub ProgRingShow(ByVal oPage As Page, bVisible As Boolean, Optional bForce As Boolean = False, Optional dMin As Double = 0, Optional dMax As Double = 100)

    '        '2021.10.02: tylko gdy jeszcze nie jest pokazywany
    '        '2021.10.13: gdy min<>max, oraz tylko gdy ma pokazać - inaczej nie zmieniaj zakresu!

    '        ' FrameworkElement.FindName(String) 

    '        Dim _mProgBar As ProgressBar = TryCast(oPage.FindName("uiPkAutoProgBar"), ProgressBar)
    '        If bVisible And _mProgBar IsNot Nothing And _mProgRingShowCnt < 1 Then
    '            If dMin <> dMax Then
    '                Try
    '                    _mProgBar.Minimum = dMin
    '                    _mProgBar.Value = dMin
    '                    _mProgBar.Maximum = dMax
    '                Catch ex As Exception
    '                End Try
    '            End If
    '        End If

    '        If bForce Then
    '            If bVisible Then
    '                _mProgRingShowCnt = 1
    '            Else
    '                _mProgRingShowCnt = 0
    '            End If
    '        Else
    '            If bVisible Then
    '                _mProgRingShowCnt += 1
    '            Else
    '                _mProgRingShowCnt -= 1
    '            End If
    '        End If
    '        Debug.WriteLine("ProgRingShow(" & bVisible & ", " & bForce & "...), current ShowCnt=" & _mProgRingShowCnt)


    '        Try
    '            Dim _mProgRing As ProgressRing = TryCast(oPage.FindName("uiPkAutoProgRing"), ProgressRing)
    '            If _mProgRingShowCnt > 0 Then
    '                Vblib.DebugOut("ProgRingShow - mam pokazac")
    '                If _mProgRing IsNot Nothing Then
    '                    Dim dSize As Double
    '                    dSize = (Math.Min(TryCast(_mProgRing.Parent, Grid).ActualHeight, TryCast(_mProgRing.Parent, Grid).ActualWidth)) / 2
    '                    dSize = Math.Max(dSize, 50) ' g³ównie na póŸniej, dla Android
    '                    _mProgRing.Width = dSize
    '                    _mProgRing.Height = dSize

    '                    _mProgRing.Visibility = Visibility.Visible
    '                    _mProgRing.IsActive = True
    '                End If
    '                If _mProgBar IsNot Nothing Then _mProgBar.Visibility = Visibility.Visible
    '            Else
    '                Vblib.DebugOut("ProgRingShow - mam ukryc")
    '                If _mProgRing IsNot Nothing Then
    '                    _mProgRing.Visibility = Visibility.Collapsed
    '                    _mProgRing.IsActive = False
    '                End If
    '                If _mProgBar IsNot Nothing Then _mProgBar.Visibility = Visibility.Collapsed
    '            End If

    '        Catch ex As Exception
    '        End Try
    '    End Sub

    '    <Extension()>
    '    Public Sub ProgRingMaxVal(ByVal oPage As Page, dMaxValue As Double)
    '        Dim _mProgBar As ProgressBar = TryCast(oPage.FindName("uiPkAutoProgBar"), ProgressBar)
    '        If _mProgBar Is Nothing Then
    '            ' skoro to nie Grid, to nie ma jak umiescic koniecznych elementow
    '            Debug.WriteLine("ProgRing(double) wymaga wczesniej ProgRingInit")
    '            Throw New ArgumentException("ProgRing(double) wymaga wczesniej ProgRingInit")
    '        End If

    '        _mProgBar.Maximum = dMaxValue

    '    End Sub

    '    <Extension()>
    '    Public Sub ProgRingVal(ByVal oPage As Page, dValue As Double)
    '        Dim _mProgBar As ProgressBar = TryCast(oPage.FindName("uiPkAutoProgBar"), ProgressBar)
    '        If _mProgBar Is Nothing Then
    '            ' skoro to nie Grid, to nie ma jak umiescic koniecznych elementow
    '            Debug.WriteLine("ProgRing(double) wymaga wczesniej ProgRingInit")
    '            Throw New ArgumentException("ProgRing(double) wymaga wczesniej ProgRingInit")
    '        End If

    '        _mProgBar.Value = dValue

    '    End Sub

    '    <Extension()>
    '    Public Sub ProgRingInc(ByVal oPage As Page)
    '        Dim _mProgBar As ProgressBar = TryCast(oPage.FindName("uiPkAutoProgBar"), ProgressBar)
    '        If _mProgBar Is Nothing Then
    '            ' skoro to nie Grid, to nie ma jak umiescic koniecznych elementow
    '            Debug.WriteLine("ProgRing(double) wymaga wczesniej ProgRingInit")
    '            Throw New ArgumentException("ProgRing(double) wymaga wczesniej ProgRingInit")
    '        End If

    '        Dim dVal As Double = _mProgBar.Value + 1
    '        If dVal > _mProgBar.Maximum Then
    '            Debug.WriteLine("ProgRingInc na wiecej niz Maximum?")
    '            _mProgBar.Value = _mProgBar.Maximum
    '        Else
    '            _mProgBar.Value = dVal
    '        End If

    '    End Sub




#End Region

End Module


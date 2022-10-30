Imports System.Reflection
Imports Microsoft
Imports Microsoft.Extensions.Configuration
Imports Newtonsoft.Json.Linq



' Partial Public Class App
' #Region "Back button" - not in .Net
' #Region "RemoteSystem/Background" - not in .Net

#Disable Warning IDE0079 ' Remove unnecessary suppression
#Disable Warning CA2007 'Consider calling ConfigureAwait On the awaited task

Partial Public Module pkarlibmodule14

    Private sLastError As String = ""

    Public Function LibLastError() As String
        Return sLastError
    End Function


#Region "Dump/Crash"
    'Private miLogLevel As Integer = 0
    'Private msLogfilePath As String = ""
    Private msCurrentLog As String = ""

    ''' <summary>
    ''' gdy sLogFilePath="", to nie ma zapisywania do pliku, szablon poniżej w remark
    ''' VB: VBlib.pkarlibmodule.InitDump(GetSettingsInt("debugLogLevel", 0), Windows.Storage.ApplicationData.Current.TemporaryFolder.Path)
    ''' </summary>
    'Public Sub LibInitDump(iLogLevel As Integer, Optional sLogfilePath As String = "")
    '    ' VB: VBlib.pkarlibmodule.InitDump(GetSettingsInt("debugLogLevel", 0), Windows.Storage.ApplicationData.Current.TemporaryFolder.Path)
    '    miLogLevel = iLogLevel
    '    If sLogfilePath <> "" Then sLogfilePath = IO.Path.Combine(sLogfilePath, "log-lib.txt")
    '    'msLogfilePath = sLogfilePath
    'End Sub


    Private Sub DumpMethodOrMsg(bAddMethod As Boolean, sMsg As String, iLevel As Integer)
        ' wyłączenie do SUB poniższych, żeby był wspólny kod do liczenia głębokości

        Dim sPrefix As String = ""
        Dim iDepth As Integer = 0
        Dim sCurrMethod As String = ""

        Dim sTrace As String = Environment.StackTrace
        If String.IsNullOrWhiteSpace(sTrace) Then
            sCurrMethod = "<stack is empty>"
        Else
            Dim subs As String() = sTrace.Split(vbCr, options:=StringSplitOptions.RemoveEmptyEntries)
            Dim iCurrMethod As Integer = -1

            For iLoop As Integer = 0 To subs.Length - 2
                If subs(iLoop).Contains(".DumpMethodOrMsg(") Then
                    If subs(iLoop + 1).Contains(".DumpCurrMethod(") OrElse
                                subs(iLoop + 1).Contains(".DumpMessage(") Then

                        iCurrMethod = iLoop + 2 ' bo ma pominąć: DumpMethodOrMsg oraz DumpCurrMethod, 
                    Else
                        iCurrMethod = iLoop + 1 ' bo ma pominąć: DumpMethodOrMsg (w Release nie ma DumpCurrMethod, kod jest optymalizowany?)
                    End If

                    Exit For
                End If
            Next


            If iCurrMethod < 1 Then
                sCurrMethod = "<bad stack?>"
            Else
                'Debug.WriteLine("subs(iCurrMethod)=" & subs(iCurrMethod).Trim)
                If bAddMethod Then sCurrMethod = subs(iCurrMethod).Trim.Substring(3)    ' z pominięciem "at "
                'Debug.WriteLine("iLoop from " & iCurrMethod + 1 & " to " & subs.Length - 1)
                For iLoop As Integer = iCurrMethod + 1 To subs.Length - 1
                    If subs(iLoop).Contains("System.Runtime.CompilerServices.") Then Continue For
                    If subs(iLoop).Contains("System.Threading.Tasks.") Then Continue For

                    sPrefix &= "  "
                    iDepth += 1
                Next

            End If

            'Debug.WriteLine("iDepth=" & iDepth)

            '' skrócenie bardzo długiego typu:
            '' BtWatchDump.MainPage.VB$StateMachine_13_BTwatch_Received.MoveNext() 
            sCurrMethod = sCurrMethod.Replace(".VB$StateMachine_", ".VB$")

            If sCurrMethod.EndsWithOrdinal(".MoveNext()") Then sCurrMethod = sCurrMethod.Substring(0, sCurrMethod.Length - 11)
        End If

        DebugOut(iDepth + iLevel, sPrefix & sCurrMethod & " " & sMsg)

    End Sub

    ''' <summary>
    ''' DebugOut z nazwą aktualnej funkcji i sMsg, oraz odpowiednio głęboko cofnięte
    ''' </summary>
    Public Sub DumpCurrMethod(Optional sMsg As String = "")
        DumpMethodOrMsg(True, sMsg, 0)
    End Sub

    ''' <summary>
    ''' DebugOut z komunikatem, odpowiednio głęboko cofnięte wedle głębokości CallStack oraz iLevel
    ''' </summary>
    Public Sub DumpMessage(sMsg As String, Optional iLevel As Integer = 1)
        DumpMethodOrMsg(False, sMsg, iLevel)
    End Sub

    ''' <summary>
    ''' Wyślij DebugOut dodając prefix --PKAR-- (do łatwiejszego znajdywania w logu), także do pliku/zmiennej
    ''' </summary>
    <CodeAnalysis.SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification:="<Pending>")>
    Public Sub DebugOut(logLevel As Integer, sMsg As String)
        Debug.WriteLine("--PKAR---:    " & sMsg)

        Dim iLogLevel As Integer = GetSettingsInt("debugLogLevel", 0)  ' nawet jak nie będzie wcześniej inicjalizacji, i tak sobie poradzi

        If iLogLevel < logLevel Then Return
        msCurrentLog = msCurrentLog & vbCrLf & Date.Now.ToString("yyyy-MM-dd HH:mm:ss") & " " & sMsg & vbCrLf
        If msCurrentLog.Length < 2048 Then Return
        DebugOutFlush()
        msCurrentLog = ""
    End Sub

    ''' <summary>
    ''' zapis zmiennej do pliku, gdy go nie ma - create wraz z nagłówkiem (datowanie rozpoczecia pliku)
    ''' </summary>
    <CodeAnalysis.SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification:="<Pending>")>
    Public Sub DebugOutFlush()
        ' ISSUE: reference to a compiler-generated method

        Dim sLogfilePath As String = IO.Path.GetTempPath
        sLogfilePath = sLogfilePath.Replace("AC\Temp", "TempState")
        sLogfilePath = IO.Path.Combine(sLogfilePath, "log-lib.txt")

        If Not IO.File.Exists(sLogfilePath) Then IO.File.AppendAllLines(sLogfilePath,
                             {vbCrLf & "===========================================",
                             "Start @" & Date.Now.ToString("yyyy.MM.dd HH:mm:ss") & vbCrLf})
        IO.File.AppendAllText(sLogfilePath, msCurrentLog)
        msCurrentLog = ""
    End Sub

    ''' <summary>
    ''' Wyślij DebugOut dodając prefix --PKAR-- (do łatwiejszego znajdywania w logu), także do pliku/zmiennej, dla Level=1
    ''' </summary>
    Public Sub DebugOut(sMsg As String)
        DebugOut(1, sMsg)
    End Sub

    ''' <summary>
    ''' Zwykły Dump plus dodaj do logu w zmiennej, Toast jeśli wiadomo jak
    ''' </summary>
    <CodeAnalysis.SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification:="<Pending>")>
    Public Sub CrashMessageAdd(sTxt As String)
        Dim sMsg = Date.Now.ToString("HH:mm:ss") & " " & sTxt & vbCrLf & sTxt & vbCrLf
        DebugOut(0, sMsg)
#If DEBUG Then
        MakeToast(sMsg)
#End If
        SetSettingsString("appFailData", GetSettingsString("appFailData") & sMsg)
    End Sub

    ''' <summary>
    ''' Zwykły Dump plus dodaj do logu w zmiennej 
    ''' </summary>
    Public Sub CrashMessageAdd(sTxt As String, exMsg As String)
        CrashMessageAdd(sTxt & vbCrLf & exMsg)
    End Sub

    ''' <summary>
    ''' Zwykły Dump plus dodaj do logu w zmiennej 
    ''' </summary>
    Public Sub CrashMessageAdd(sTxt As String, ex As Exception, Optional bWithStack As Boolean = False)
        Dim exMsg As String = ""
        If ex IsNot Nothing Then
            exMsg = ex.ToString() & ":" & ex.Message
            If bWithStack AndAlso Not Equals(ex.StackTrace, Nothing) Then exMsg = exMsg & vbCrLf & ex.StackTrace
        End If
        CrashMessageAdd(sTxt, exMsg)
    End Sub

#End Region

#Region "ClipBoard"
    Public Delegate Sub UIclipPut(sTxt As String)
    Public Delegate Sub UIclipPutHtml(sHtml As String)
    'Public Delegate Function UIclipGet() As Task(Of String)

    Private moUIclipPut As UIclipPut ' = Nothing przez samą deklarację
    Private moUIclipPutHtml As UIclipPutHtml
    'Private moUIclipGet As UIclipGet

    Public Sub LibInitClip(oUIclipPut As UIclipPut, oUIclipPutHtml As UIclipPutHtml)
        moUIclipPut = oUIclipPut
        moUIclipPutHtml = oUIclipPutHtml
    End Sub


    Public Sub ClipPut(sTxt As String)
        moUIclipPut(sTxt)
    End Sub

    Public Sub ClipPutHtml(sHtml As String)
        moUIclipPutHtml(sHtml)
    End Sub

    '''' <summary>
    '''' w razie Catch() zwraca ""
    '''' </summary>
    'Public Async Function ClipGetAsync() As Task(Of String)
    '    Return Await moUIclipGet
    'End Function


#End Region


#Region "Settings"

    Friend _settingsGlobal As Microsoft.Extensions.Configuration.IConfigurationRoot
    Public Sub LibInitSettings(settings As Microsoft.Extensions.Configuration.IConfigurationRoot)
        _settingsGlobal = settings
    End Sub

    Private Sub SettingsCheckInit()
        If _settingsGlobal IsNot Nothing Then Return

        DumpMessage("operacja na Settings bez LibInitSettings, wczytuję defaultowe (Std 1.4 bez parametrowe, czyli plik INI jedynie)")
        ' w wersji .Net 2.0 mogłoby być więcej - tzn. commandline przynajmniej jeszcze
        ' na razie jest INI z appx, oraz (jako jakiekolwiek pamiętanie: InMemoryStorage. Nie jest dobre, bo będzie się plątać przy ROAM
        ' i w dodatku działa tylko na UWP :(  w Droid jest ""
        Dim settings As Microsoft.Extensions.Configuration.IConfigurationRoot =
                (New Microsoft.Extensions.Configuration.ConfigurationBuilder).AddIniRelDebugSettings(IniLikeDefaults.sIniContent).AddInMemoryCollection().Build()

    End Sub

    Public Sub SetSettingsString(sName As String, value As String, Optional bRoam As Boolean = False)
        SettingsCheckInit()

        If bRoam Then value = "[ROAM]" & value

        _settingsGlobal(sName) = value

    End Sub

    Public Sub SetSettingsInt(sName As String, value As Integer, Optional bRoam As Boolean = False)
        SetSettingsString(sName, value.ToString(System.Globalization.CultureInfo.InvariantCulture), bRoam)
    End Sub

    Public Sub SetSettingsBool(sName As String, value As Boolean, Optional bRoam As Boolean = False)
        SetSettingsString(sName, If(value, "True", "False"), bRoam)
    End Sub

    Public Sub SetSettingsLong(sName As String, value As Long, Optional bRoam As Boolean = False)
        SetSettingsString(sName, value.ToString(System.Globalization.CultureInfo.InvariantCulture), bRoam)
    End Sub

    Public Sub SetSettingsDate(sName As String, value As DateTimeOffset, Optional bRoam As Boolean = False)
        SetSettingsString(sName, value.ToString("yyyy.MM.dd HH:mm:ss"), bRoam)
    End Sub

    Public Sub SetSettingsCurrentDate(sName As String, Optional bRoam As Boolean = False)
        SetSettingsDate(sName, DateTimeOffset.Now, bRoam)
    End Sub


    Private Function GetSettingsNet(sName As String, sDefault As String)
        SettingsCheckInit()
        Dim sRetVal As String = _settingsGlobal(sName)
        If sRetVal IsNot Nothing Then Return sRetVal

        Return sDefault
        ' https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Configuration/src/ConfigurationRoot.cs
        ' widać że zwraca NULL gdy nie trafi na zmienną nigdzie
    End Function

    Public Function GetSettingsString(sName As String, Optional sDefault As String = "") As String
        Return GetSettingsNet(sName, sDefault)
    End Function

    Public Function GetSettingsInt(sName As String, Optional iDefault As Integer = 0) As Integer
        Dim sRetVal As String = GetSettingsNet(sName, iDefault.ToString(System.Globalization.CultureInfo.InvariantCulture))
        Dim iRetVal As Integer = 0
        If Integer.TryParse(sRetVal, Globalization.NumberStyles.Integer, Globalization.CultureInfo.InvariantCulture, iRetVal) Then
            Return iRetVal
        End If
        Return iDefault
    End Function

    Public Function GetSettingsBool(sName As String, Optional bDefault As Boolean = False) As Boolean
        Dim sRetVal As String = GetSettingsNet(sName, If(bDefault, "True", "False"))
        If sRetVal.ToLower = "true" Then Return True
        Return False
    End Function

    Public Function GetSettingsLong(sName As String, Optional iDefault As Long = 0) As Long
        Dim sRetVal As String = GetSettingsNet(sName, iDefault.ToString(System.Globalization.CultureInfo.InvariantCulture))
        Dim iRetVal As Long = 0
        If Long.TryParse(sRetVal, Globalization.NumberStyles.Integer, Globalization.CultureInfo.InvariantCulture, iRetVal) Then
            Return iRetVal
        End If
        Return iDefault
    End Function

    Public Function GetSettingsDate(sName As String, Optional sDefault As String = "") As DateTimeOffset
        If sDefault = "" Then sDefault = DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss")
        Dim sRetVal As String = GetSettingsNet(sName, sDefault)
        Dim dRetVal As DateTimeOffset
        If DateTimeOffset.TryParseExact(sRetVal, {"yyyy.MM.dd HH:mm:ss"},
                             Globalization.CultureInfo.InvariantCulture.DateTimeFormat,
                             Globalization.DateTimeStyles.AllowWhiteSpaces, dRetVal) Then
            Return dRetVal
        End If

        Return DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss")
    End Function

    ' wersja z przeskokami do UWP
#If False Then

    Public Delegate Function UIGetSettingsString(sName As String, defValue As String) As String
    Public Delegate Function UIGetSettingsInt(sName As String, defValue As Integer) As Integer
    Public Delegate Function UIGetSettingsBool(sName As String, defValue As Boolean) As Boolean
    Public Delegate Sub UISetSettings(sName As String, oVal As Object, bRoam As Boolean)

    Private moGetString As UIGetSettingsString = Nothing
    Private moGetInt As UIGetSettingsInt = Nothing
    Private moGetBool As UIGetSettingsBool = Nothing
    Private moSetSettings As UISetSettings = Nothing

    Private moConfig As IConfigurationRoot = Nothing
    Private miConfigUwpGet As Integer = 2
    Private miConfigUwpSet As Integer = 1

    Private Sub InitSettingsOldDotNet(sDirForSettngs As String)
        If Not IO.Directory.Exists(sDirForSettngs) Then
            Throw New Exception("PkarLibModule.LibInitSettingsDotNet: sDirForSettngs doesnt exist!")
        End If

        Dim sFilename As String = IO.Path.Combine(sDirForSettngs, "appsettings.json")
        Dim oBldr As New Microsoft.Extensions.Configuration.ConfigurationBuilder()
        moConfig = oBldr.AddJsonFile(sFilename, True, False).Build
    End Sub

    ''' <summary>
    ''' inicjalizacja settingsów w wersji .Net, wywoływać bezpośrednio tylko gdy się nie wymaga UWP
    ''' iUwpGet: =0: nie używa UWP, =1: używa, jeśli było LibInitSettings, .Net ważniejszy, =2: musi użyć; 
    ''' iUwpSet: =0: nie używa UWP, =1: używa, jeśli było LibInitSettings, =2: musi użyć; do .Net zapis zawsze
    ''' </summary>
    Public Sub LibInitSettingsDotNet(sDirForSettngs As String, Optional iUwpGet As Integer = 0, Optional iUwpSet As Integer = 0)
        ' do sprawdzenia GDZIE ten plik się pojawia
        ' JSON: może być jeszcze jedno True, oznaczające że reload on change (gdy emulowane na nim byłoby Roaming)

        Try
            ' .FileSystem.Watcher, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'. The system cannot find the file specified.
            ' ale przy nowszych wersjach (dla .Net Std 2.0), działa poprawnie (znaczy nie ma tu błędu).
            Dim oBldr As New Microsoft.Extensions.Configuration.ConfigurationBuilder()
            moConfig = oBldr.AddJsonFile("appsettings.json", True, False).Build
            miConfigUwpGet = iUwpGet
            miConfigUwpSet = iUwpSet
            Return
        Catch ex As Exception
        End Try

        ' InitSettingsOldDotNet(sDirForSettngs) ' wersja samodzielna (przepisane z Uno Settingsy?

        Debug.WriteLine("PKAR: ERROR: cannot init Settings from JSON file")
        moConfig = Nothing
        miConfigUwpGet = 2
        miConfigUwpSet = 2

    End Sub


    ''' <summary>
    ''' inicjalizacja, zob. pkarModuleWithLib.InitLib
    ''' </summary>
    Public Sub LibInitSettings(oSetSetting As UISetSettings, ByVal oGetString As UIGetSettingsString, ByVal oGetInt As UIGetSettingsInt, ByVal oGetBool As UIGetSettingsBool)
        ' VB: VBlib.pkarlibmodule.InitSettings(AddressOf pkar.SetSettingsString, AddressOf pkar.SetSettingsInt, AddressOf pkar.SetSettingsBool, AddressOf pkar.GetSettingsString, AddressOf pkar.GetSettingsInt, AddressOf pkar.GetSettingsBool)

        moSetSettings = oSetSetting

        moGetString = oGetString
        moGetInt = oGetInt
        moGetBool = oGetBool

        ' LibInitSettingsDotNet(1, 1)
    End Sub

    ''' <summary>
    ''' tylko dla wewnątrz pkarlibmodule, App powinno używać tego w pkarmodulewithlib
    ''' </summary>
    Private Sub SetSettingsUwp(sName As String, oVal As Object, bRoam As Boolean)
        If moSetSettings Is Nothing Then Throw New InvalidOperationException("SetSettingsUwp w VBLib wymaga wczesniejszego InitSettings")
        moSetSettings?(sName, oVal, bRoam)
    End Sub

    ''' <summary>
    ''' UWP oraz .Net, w zależności od LibInit
    ''' </summary>
    Public Sub SetSettingsString(sName As String, value As String, Optional bRoam As Boolean = False)
        If moConfig IsNot Nothing Then moConfig.Item(sName) = value

        If miConfigUwpSet = 1 AndAlso moSetSettings IsNot Nothing Then SetSettingsUwp(sName, value, bRoam)
        If miConfigUwpSet = 2 Then SetSettingsUwp(sName, value, bRoam)
    End Sub

    ''' <summary>
    ''' UWP oraz .Net, w zależności od LibInit
    ''' </summary>
    Public Sub SetSettingsInt(sName As String, value As Integer, Optional bRoam As Boolean = False)
        If moConfig IsNot Nothing Then moConfig.Item(sName) = value

        If miConfigUwpSet = 1 AndAlso moSetSettings IsNot Nothing Then SetSettingsUwp(sName, value, bRoam)
        If miConfigUwpSet = 2 Then SetSettingsUwp(sName, value, bRoam)
    End Sub


    ''' <summary>
    ''' UWP oraz .Net, w zależności od LibInit
    ''' </summary>
    Public Sub SetSettingsBool(sName As String, value As Boolean, Optional bRoam As Boolean = False)
        If moConfig IsNot Nothing Then moConfig.Item(sName) = If(value, "true", "false")    ' moje, żeby niezależnie od języka było

        If miConfigUwpSet = 1 AndAlso moSetSettings IsNot Nothing Then SetSettingsUwp(sName, value, bRoam)
        If miConfigUwpSet = 2 Then SetSettingsUwp(sName, value, bRoam)
    End Sub


    ''' <summary>
    ''' UWP oraz .Net, w zależności od LibInit
    ''' </summary>
    Public Sub SetSettingsLong(sName As String, value As Long, bRoam As Boolean)
        If moConfig IsNot Nothing Then moConfig.Item(sName) = value

        If miConfigUwpSet = 1 AndAlso moSetSettings IsNot Nothing Then SetSettingsUwp(sName, value, bRoam)
        If miConfigUwpSet = 2 Then SetSettingsUwp(sName, value, bRoam)
    End Sub

    Public Sub SetSettingsLong(sName As String, value As Long)
        SetSettingsLong(sName, value, False)
    End Sub

    Private Function GetSettingsNet(sName As String)
        Try
            Return moConfig.Item(sName)
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' UWP oraz .Net, w zależności od LibInit
    ''' </summary>
    Public Function GetSettingsString(sName As String, Optional sDefault As String = "") As String

        Select Case miConfigUwpGet
            Case 0
                Dim sTmp As String = GetSettingsNet(sName)
                If sTmp IsNot Nothing Then Return sTmp
                Return sDefault
            Case 1
                Dim sTmp As String = GetSettingsNet(sName)
                If sTmp IsNot Nothing Then Return sTmp

                If moGetString Is Nothing Then Return sDefault

                Return moGetString(sName, sDefault)
            Case 2
                If moGetString Is Nothing Then Throw New InvalidOperationException("GetSettingsString w VBLib wymaga wczesniejszego InitSettings")
                Return moGetString(sName, sDefault)
        End Select

        Return sDefault

    End Function

    ''' <summary>
    ''' UWP oraz .Net, w zależności od LibInit
    ''' </summary>
    Public Function GetSettingsInt(sName As String, Optional iDefault As Integer = 0) As Integer
        Dim iInt As Integer = 0
        Select Case miConfigUwpGet
            Case 0
                Dim sTmp As String = GetSettingsNet(sName)
                If sTmp Is Nothing Then Return iDefault
                If Integer.TryParse(sTmp, iInt) Then Return iInt
                Return iDefault
            Case 1
                Dim sTmp As String = GetSettingsNet(sName)
                If sTmp IsNot Nothing Then
                    If Integer.TryParse(sTmp, iInt) Then Return iInt
                    Return iDefault
                End If

                If moGetInt Is Nothing Then Return iDefault
                Return moGetInt(sName, iDefault)
            Case 2
                If moGetInt Is Nothing Then Throw New InvalidOperationException("GetSettingsInt w VBLib wymaga wczesniejszego InitSettings")
                Return moGetInt(sName, iDefault)
        End Select

        Return iDefault

    End Function

    ''' <summary>
    ''' UWP oraz .Net, w zależności od LibInit
    ''' </summary>
    Public Function GetSettingsBool(sName As String, Optional bDefault As Boolean = False) As Boolean

        Dim iInt As Integer = 0
        Select Case miConfigUwpGet
            Case 0
                Dim sTmp As String = GetSettingsNet(sName)
                If sTmp Is Nothing Then Return bDefault
                If sTmp.ToLower = "true" Then Return True
                Return False
            Case 1
                Dim sTmp As String = GetSettingsNet(sName)
                If sTmp IsNot Nothing Then
                    If sTmp.ToLower = "true" Then Return True
                    Return False
                End If

                If moGetBool Is Nothing Then Return bDefault
                Return moGetBool(sName, bDefault)
            Case 2
                If moGetBool Is Nothing Then Throw New InvalidOperationException("GetSettingsBool w VBLib wymaga wczesniejszego InitSettings")
                Return moGetBool(sName, bDefault)
        End Select

        Return bDefault

    End Function
#End If


#End Region

    ' IsFamilyMobile  - not in .Net
    ' IsFamilyDesktop - not in .Net
    ' NetIsIPavailable (Std2.0) - not in .Net
    ' NetIsCellInet - not in .Net
    ' GetHostName (Std2.0) - not in .Net
    ' IsThisMoje (bo korzysta z GetHostName) - not in .Net
    ' IsFullVersion - not in .Net
    ' NetWiFiOffOnAsync - not in .Net
    ' NetIsBTavailableAsync - not in .Net
    ' NetTrySwitchBTOnAsync - not in .Net

#Region "DialogBoxes"

    Public Delegate Function UIdialogBox(sMsg As String) As Task
    Public Delegate Function UIdialogBoxYN(sMsg As String, sYes As String, sNo As String) As Task(Of Boolean)
    Public Delegate Function UIdialogBoxInput(sMsgResId As String, sDefault As String, sYes As String, sNo As String) As Task(Of String)

    Private moUIdialogBox As UIdialogBox ' = Nothing przez samą deklarację
    Private moUIdialogBoxYN As UIdialogBoxYN
    Private moUIdialogBoxInput As UIdialogBoxInput

    ''' <summary>
    ''' inicjalizacja, zob. pkarModuleWithLib.InitLib
    ''' </summary>
    Public Sub LibInitDialogBox(ByVal oUIdialogBox As UIdialogBox, ByVal oUIdialogBoxYN As UIdialogBoxYN, ByVal oUIdialogBoxInput As UIdialogBoxInput)
        ' VB: VBlib.pkarlibmodule.InitDialogBox(AddressOf pkar.FromLibDialogBoxAsync, AddressOf pkar.FromLibDialogBoxYNAsync, AddressOf pkar.FromLibDialogBoxInputAllDirectAsync)
        moUIdialogBox = oUIdialogBox
        moUIdialogBoxYN = oUIdialogBoxYN
        moUIdialogBoxInput = oUIdialogBoxInput
    End Sub

    ''' <summary>
    ''' Dialog, z czekaniem
    ''' </summary>
    Public Async Function DialogBoxAsync(sMsg As String) As Task
        If moUIdialogBox Is Nothing Then Throw New InvalidOperationException("DialogBoxAsync w VBLib wymaga wczesniejszego InitMsgBox")
        Await moUIdialogBox(sMsg)
    End Function

    Public Sub DialogBox(sMsg As String)
#Disable Warning BC42358 ' Because this call is not awaited, execution of the current method continues before the call is completed
        DialogBoxAsync(sMsg)
#Enable Warning BC42358
    End Sub

    Public Async Function DialogBoxResAsync(sResId As String) As Task
        sResId = GetLangString(sResId)
        Await DialogBoxAsync(sResId)
    End Function

    Public Sub DialogBoxRes(sResId As String)
#Disable Warning BC42358 ' Because this call is not awaited, execution of the current method continues before the call is completed
        DialogBoxResAsync(sResId)
#Enable Warning BC42358
    End Sub

    Public Async Function DialogBoxYNAsync(sMsg As String, Optional sYes As String = "Tak", Optional sNo As String = "Nie") As Task(Of Boolean)
        If moUIdialogBoxYN Is Nothing Then Throw New InvalidOperationException("DialogBoxYNAsync w VBLib wymaga wczesniejszego InitMsgBox")
        Return Await moUIdialogBoxYN(sMsg, sYes, sNo)
    End Function

    Public Async Function DialogBoxResYNAsync(sMsgResId As String, Optional sYesResId As String = "resDlgYes", Optional sNoResId As String = "resDlgNo") As Task(Of Boolean)
        Dim sMsg = GetLangString(sMsgResId)
        Dim sYes = GetLangString(sYesResId)
        Dim sNo = GetLangString(sNoResId)
        Return Await DialogBoxYNAsync(sMsg, sYes, sNo)
    End Function

    ''' <summary>
    ''' Dla Cancel zwraca ""
    ''' </summary>
    Public Async Function DialogBoxInputAllDirectAsync(sMsg As String, Optional sDefault As String = "", Optional sYes As String = "Yes", Optional sNo As String = "No") As Task(Of String)
        If moUIdialogBoxInput Is Nothing Then Throw New InvalidOperationException("DialogBoxInputAllDirectAsync w VBLib wymaga wczesniejszego InitMsgBox")
        Return Await moUIdialogBoxInput(sMsg, sDefault, sYes, sNo)
    End Function

    ''' <summary>
    ''' Dla Cancel zwraca ""
    ''' </summary>
    Public Async Function DialogBoxInputDirectAsync(sMsg As String, Optional sDefault As String = "", Optional sYesResId As String = "resDlgContinue", Optional sNoResId As String = "resDlgCancel") As Task(Of String)
        Dim sYes = GetLangString(sYesResId)
        Dim sNo = GetLangString(sNoResId)
        Return Await DialogBoxInputAllDirectAsync(sMsg, sDefault, sYes, sNo)
    End Function

    ''' <summary>
    ''' Dla Cancel zwraca ""
    ''' </summary>
    Public Async Function DialogBoxInputResAsync(sMsgResId As String, Optional sDefaultResId As String = "", Optional sYesResId As String = "resDlgContinue", Optional sNoResId As String = "resDlgCancel") As Task(Of String)
        Dim sDefault = ""
        Dim sMsg = GetLangString(sMsgResId)
        If sDefaultResId <> "" Then sDefault = GetLangString(sDefaultResId)
        Return Await DialogBoxInputDirectAsync(sMsg, sDefault, sYesResId, sNoResId)
    End Function

#End Region

#Region "Globalization"
    Private moResMan As Resources.ResourceManager ' = Nothing
    Public Function GetLangString(sResID As String, Optional sDefault As String = "") As String
        If sResID = "" Then Return ""
        If moResMan Is Nothing Then
            If Globalization.CultureInfo.CurrentCulture.Name.ToUpperInvariant.StartsWithOrdinal("PL") Then
                moResMan = My.Resources.Resource_PL.ResourceManager
            Else
                moResMan = My.Resources.Resource_EN.ResourceManager
            End If
        End If
        Dim sRet As String
#Disable Warning CA1304 ' Specify CultureInfo
        Try
            sRet = moResMan.GetString(sResID)
        Catch ex As Exception
            ' jakby co
            ' bez dodania dla VBLib defLang - zdarza się zawsze w RELEASE (i dla PL i dla EN)
            sRet = ""
        End Try

#Enable Warning CA1304 ' Specify CultureInfo
        If sRet <> "" Then Return sRet
        If sDefault <> "" Then Return sDefault
        Return sResID
    End Function

#End Region

#Region "Toasty itp"

    Public Delegate Sub UiMakeToast(sMsg As String, sMsg1 As String)
    Private moMakeToast As UiMakeToast ' = Nothing

    ''' <summary>
    ''' Umożliwienie Toast z VBlib
    ''' VB: VBlib.pkarlibmodule.LibInitToast(AddressOf pkar.MakeToast)
    ''' </summary>
    Public Sub LibInitToast(ByVal oMakeToast As UiMakeToast)
        ' VB: VBlib.pkarlibmodule.LibInitToast(AddressOf pkar.MakeToast)
        moMakeToast = oMakeToast
    End Sub


    ' SetBadgeNo - not in .Net

    Public Function XmlSafeString(sInput As String) As String
        Return New XText(sInput).ToString()
    End Function

    Public Function XmlSafeStringQt(sInput As String) As String
        Dim sTmp As String
        sTmp = XmlSafeString(sInput)
        sTmp = sTmp.Replace("""", "&quote;")
        Return sTmp
    End Function

    ''' <summary>
    ''' Tylko przerzucenie do App - wiec wykorzystywac tylko w VBlib!
    ''' </summary>
    Public Sub MakeToast(sMsg As String, Optional sMsg1 As String = "")
        If moMakeToast Is Nothing Then Throw New InvalidOperationException("MakeToast w VBLib wymaga wczesniejszego LibInitToast")
        moMakeToast(sMsg, sMsg1)
    End Sub

    ' ToastAction - not in .Net
    ' MakeToast(oDate As DateTime, sMsg As String, Optional sMsg1 As String = "") - not in .Net
    ' RemoveScheduledToasts - not in .Net
#End Region

    ' #Region "WinVer, AppVer"
    ' WinVer - not in .Net
    ' GetAppVers - not in .Net (System.Reflection.Assembly.GetExecutingAssembly.GetName.Version.ToString , ale od .Net 7)

#Region "GetWebPage + pomocnicze"
    Private moHttp As New Net.Http.HttpClient
    Private Const msDefaultHttpAgent As String = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/88.0.4321.0 Safari/537.36 Edg/88.0.702.0"
    Private msAgent As String = msDefaultHttpAgent

    Public Sub HttpPageSetAgent(Optional sAgent As String = msDefaultHttpAgent)
        msAgent = sAgent
        moHttp?.DefaultRequestHeaders.UserAgent.TryParseAdd(msAgent)
    End Sub

    Public Sub HttpPageReset(Optional bAllowRedirects As Boolean = True)
#Disable Warning CA2000 ' Dispose objects before losing scope
        ' będzie Dispose razem z moHttp dispose
        Dim oHandler As New Net.Http.HttpClientHandler With {
            .AllowAutoRedirect = bAllowRedirects
        }
#Enable Warning CA2000 ' Dispose objects before losing scope
        If moHttp IsNot Nothing Then
            moHttp.Dispose()
        End If

        moHttp = New Net.Http.HttpClient(oHandler)
        moHttp.DefaultRequestHeaders.UserAgent.TryParseAdd(msAgent)
    End Sub

    Public Async Function HttpPageAsync(sLink As String, Optional sData As String = "", Optional bReset As Boolean = False) As Task(Of String)
        Return Await HttpPageAsync(New Uri(sLink), sData, bReset)
    End Function


    Public Async Function HttpPageAsync(oUri As Uri, Optional sData As String = "", Optional bReset As Boolean = False) As Task(Of String)
        DumpCurrMethod("uri=" & oUri.AbsoluteUri)
        If oUri Is Nothing OrElse oUri.ToString = "" Then
            sLastError = "HttpPageAsync but sUrl is empty"
            Return ""
        End If

        If moHttp Is Nothing OrElse bReset Then HttpPageReset()
        sLastError = ""
        Dim oResp As Net.Http.HttpResponseMessage

        ' przygotuj pContent, będzie przy redirect używany ponownie
        Dim pContent As Net.Http.StringContent = Nothing    ' żeby nie krzyczał że używam nieinicjalizowanego
        If sData <> "" Then pContent = New Net.Http.StringContent(sData, Text.Encoding.UTF8, "application/x-www-form-urlencoded")

        Try
            ' ISSUE: reference to a compiler-generated method
            If sData <> "" Then
                oResp = Await moHttp.PostAsync(oUri, pContent)
            Else
                oResp = Await moHttp.GetAsync(oUri)
            End If

#Disable Warning CA1031 ' Do not catch general exception types
        Catch ex As Exception
#Enable Warning CA1031 ' Do not catch general exception types
            sLastError = "ERROR @HttpPageAsync get/post " & oUri.ToString & " : " & ex.Message
            DumpMessage(sLastError)
            pContent?.Dispose()
            Return ""
        End Try

        If oResp.StatusCode = 303 Or oResp.StatusCode = 302 Or oResp.StatusCode = 301 Then
            ' redirect
            oUri = oResp.Headers.Location
            'If sUrl.ToLower.Substring(0, 4) <> "http" Then
            '    sUrl = "https://sympatia.onet.pl/" & sUrl   ' potrzebne przy szukaniu
            'End If

            If sData <> "" Then
                oResp = Await moHttp.PostAsync(oUri, pContent)
            Else
                oResp = Await moHttp.GetAsync(oUri)
            End If
        End If
        pContent?.Dispose()

        Dim sPage As String

        ' override dla Facebook
        If oResp.Content.Headers.Contains("Content-Type") Then
            If oResp.Content.Headers.ContentType.CharSet = """utf-8""" Then
                oResp.Content.Headers.ContentType.CharSet = "utf-8"
            End If
        End If
        Try
            sPage = Await oResp.Content.ReadAsStringAsync()
#Disable Warning CA1031 ' Do not catch general exception types
        Catch ex As Exception
#Enable Warning CA1031 ' Do not catch general exception types
            sLastError = "ERROR @HttpPageAsync ReadAsync: " & ex.Message
            DumpMessage(sLastError)
            Return ""
        End Try

        Return sPage
    End Function


    Public Function RemoveHtmlTags(sHtml As String) As String
        If String.IsNullOrWhiteSpace(sHtml) Then Return ""

        Dim iInd0, iInd1 As Integer

        iInd0 = sHtml.IndexOfOrdinal("<script")
        If iInd0 > 0 Then
            iInd1 = sHtml.IndexOf("</script>", iInd0, StringComparison.Ordinal)
            If iInd1 > 0 Then
                sHtml = sHtml.Remove(iInd0, iInd1 - iInd0 + 9)
            End If
        End If

        iInd0 = sHtml.IndexOfOrdinal("<")
        iInd1 = sHtml.IndexOfOrdinal(">")
        While iInd0 > -1
            If iInd1 > -1 Then
                sHtml = sHtml.Remove(iInd0, iInd1 - iInd0 + 1)
            Else
                sHtml = sHtml.Substring(0, iInd0)
            End If
            sHtml = sHtml.Trim

            iInd0 = sHtml.IndexOfOrdinal("<")
            iInd1 = sHtml.IndexOfOrdinal(">")
        End While

        sHtml = sHtml.Replace("&nbsp;", " ")
        sHtml = sHtml.Replace(vbLf, vbCrLf)
        sHtml = sHtml.Replace(vbCrLf & vbCrLf, vbCrLf)
        sHtml = sHtml.Replace(vbCrLf & vbCrLf, vbCrLf)
        sHtml = sHtml.Replace(vbCrLf & vbCrLf, vbCrLf)

        Return sHtml.Trim

    End Function

    ' OpenBrowser - not in .Net

#End Region

#Region "triggers"
    '    #Region "zwykłe" - not in .Net
#Region "RemoteSystem"

    ''' <summary>
    ''' jeśli na wejściu jest jakaś standardowa komenda (obsługiwalna w .Net Standard),
    ''' to na wyjściu będzie jej rezultat. Else = ""
    ''' </summary>
    Public Function LibAppServiceStdCmd(sCommand As String, sLocalCmds As String) As String
        If String.IsNullOrWhiteSpace(sCommand) Then Return ""

        Dim sTmp As String

        If sCommand.StartsWithOrdinal("debug loglevel") Then
            Dim sRetVal As String = "Previous loglevel: " & GetSettingsInt("debugLogLevel") & vbCrLf
            sCommand = sCommand.Replace("debug loglevel", "").Trim
            Dim iTemp As Integer = 0
            If Not Integer.TryParse(sCommand, iTemp) Then
                Return sRetVal & "Not changed - bad loglevel value"
            End If

            SetSettingsInt("debugLogLevel", iTemp)
            Return sRetVal & "Current loglevel: " & iTemp
        End If

        Select Case sCommand.ToLower()
            Case "ping"
                Return "pong"
            ' Case "ver"
            ' Case "localdir"
            Case "appdir"
                Return System.AppContext.BaseDirectory
            ' Case "installeddate"
            Case "help"
                Return "App specific commands:" & vbCrLf & sLocalCmds

            Case "debug vars"
                Return DumpSettings()
            ' Case "debug triggers"
            ' Case "debug toasts"
            ' Case "debug memsize"
            ' Case "debug rungc"
            Case "debug crashmsg"
                sTmp = GetSettingsString("appFailData", "")
                If sTmp = "" Then sTmp = "No saved crash info"
                Return sTmp
            Case "debug crashmsg clear"
                sTmp = GetSettingsString("appFailData", "")
                If sTmp = "" Then sTmp = "No saved crash info"
                SetSettingsString("appFailData", "")
                Return sTmp

            ' Case "lib unregistertriggers"
            ' Case "lib isfamilymobile"
            ' Case "lib isfamilydesktop"
            ' Case "lib netisipavailable"
            ' Case "lib netiscellinet"
            ' Case "lib gethostname"
            ' Case "lib isthismoje"
            ' Case "lib istriggersregistered"
            Case "lib pkarmode 1"
                SetSettingsBool("pkarMode", True)
                Return "DONE"
            Case "lib pkarmode 0"
                SetSettingsBool("pkarMode", False)
                Return "DONE"
            Case "lib pkarmode"
                Return GetSettingsBool("pkarMode").ToString()
        End Select

        Return ""  ' oznacza: to nie jest standardowa komenda
    End Function

    Private Function DumpSettings() As String
        ' GetDebugView(IConfigurationRoot) - ale to od późniejszych .Net, od platform extension 3

        Dim sRet As String = "Dump settings (VBlib version, v.1.4)" & vbCrLf

        For Each oSett In _settingsGlobal.AsEnumerable
            sRet = sRet & oSett.Key & vbTab & oSett.Value & vbCrLf
        Next

        Return sRet
    End Function

#End Region


#End Region

#Region "DataLog folder support"
    Private msDataLogRootFolder As String = ""
    Public Sub LibInitDataLog(sPath As String)
        msDataLogRootFolder = sPath
    End Sub

    <CodeAnalysis.SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification:="<Pending>")>
    Public Function GetLogFolderYear() As String
        If msDataLogRootFolder = "" Then
            Throw New InvalidOperationException("GetLogFolderYearAsync w VBLib wymaga wczesniejszego LibInitDataLog")
        End If

        Dim sFold As String = IO.Path.Combine(msDataLogRootFolder, Date.Now.ToString("yyyy"))
        If Not IO.Directory.Exists(sFold) Then IO.Directory.CreateDirectory(sFold)
        If Not IO.Directory.Exists(sFold) Then Return ""    ' error creating directory

        Return sFold
    End Function

    <CodeAnalysis.SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification:="<Pending>")>
    Public Function GetLogFolderMonth() As String
        Dim sFold As String = GetLogFolderYear()
        If sFold = "" Then Return ""

        sFold = IO.Path.Combine(sFold, Date.Now.ToString("MM"))
        If Not IO.Directory.Exists(sFold) Then IO.Directory.CreateDirectory(sFold)
        If Not IO.Directory.Exists(sFold) Then Return ""    ' error creating directory
        Return sFold

    End Function

    ''' <summary>
    ''' Podaje nazwę pliku base + "yyyy.MM.dd" + ext
    ''' </summary>
    ''' <param name="sBaseName"></param>
    ''' <param name="sExtension"></param>
    ''' <returns></returns>
    <CodeAnalysis.SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification:="<Pending>")>
    Public Function GetLogFileDaily(sBaseName As String, sExtension As String) As String
        Return GetLogFileDailyFilename(sBaseName, sExtension, "yyyy.MM.dd")
    End Function

    ''' <summary>
    ''' Podaje nazwę pliku base + "yyyy.MM.dd.HH.mm" + ext
    ''' </summary>
    ''' <param name="sBaseName"></param>
    ''' <param name="sExtension"></param>
    ''' <returns></returns>
    <CodeAnalysis.SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification:="<Pending>")>
    Public Function GetLogFileDailyWithTime(sBaseName As String, sExtension As String) As String
        Return GetLogFileDailyFilename(sBaseName, sExtension, "yyyy.MM.dd.HH.mm")
    End Function

    Private Function GetLogFileDailyFilename(sBaseName As String, sExtension As String, sFormatDate As String) As String
        If sExtension Is Nothing Then
            sExtension = ""
        Else
            If Not sExtension.StartsWithOrdinal(".") Then sExtension = "." & sExtension
        End If

        Dim sFile As String = ""
        If sBaseName <> "" Then sFile = sFile & " " & Date.Now.ToString(sFormatDate)
        sFile &= sExtension
        Return GetLogFileDaily(sFile)
    End Function

    Public Function GetLogFileDaily(sFileName As String) As String
        Dim sFold As String = GetLogFolderMonth()
        If sFold = "" Then Return ""

        Return IO.Path.Combine(sFold, sFileName)
    End Function

    <CodeAnalysis.SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification:="<Pending>")>
    Public Function GetLogFileMonthly(sBaseName As String, sExtension As String) As String
        ' 2021.08.20: połączone z tym niżej, tu tylko ustalenie nazwy
        If String.IsNullOrWhiteSpace(sExtension) Then sExtension = ".txt"
        If Not sExtension.StartsWithOrdinal(".") Then sExtension = "." & sExtension

        Dim sFile As String
        If String.IsNullOrWhiteSpace(sBaseName) Then
            sFile = Date.Now.ToString("yyyy.MM") & sExtension
        Else
            sFile = sBaseName & " " & Date.Now.ToString("yyyy.MM") & sExtension
        End If

        Return GetLogFileMonthly(sFile)
    End Function

    Public Function GetLogFileMonthly(sFileName As String) As String
        Dim sFold As String = GetLogFolderYear()
        If sFold = "" Then Return ""

        Return IO.Path.Combine(sFold, sFileName)
    End Function

    <CodeAnalysis.SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification:="<Pending>")>
    Public Function GetLogFileYearly(sBaseName As String, sExtension As String) As String
        If String.IsNullOrWhiteSpace(sExtension) Then sExtension = ".txt"
        If Not sExtension.StartsWithOrdinal(".") Then sExtension = "." & sExtension

        Dim sFile As String
        If String.IsNullOrWhiteSpace(sBaseName) Then
            sFile = Date.Now.ToString("yyyy") & sExtension
        Else
            sFile = sBaseName & " " & Date.Now.ToString("yyyy") & sExtension
        End If

        Return GetLogFileYearly(sFile)
    End Function

    Public Function GetLogFileYearly(sFileName As String) As String
        If msDataLogRootFolder = "" Then
            Throw New InvalidOperationException("GetLogFolderYearAsync w VBLib wymaga wczesniejszego LibInitDataLog")
        End If

        Return IO.Path.Combine(msDataLogRootFolder, sFileName)

    End Function

    Public Sub AppendLogYearly(sTxt As String, Optional sFileName As String = "log")
        Dim sFile As String = GetLogFileYearly(sFileName)
        IO.File.AppendAllText(sFile, sTxt)
    End Sub

    Public Sub AppendLogMonthly(sTxt As String, Optional sFileName As String = "log")
        Dim sFile As String = GetLogFileMonthly(sFileName)
        IO.File.AppendAllText(sFile, sTxt)
    End Sub

    Public Sub AppendLogDaily(sTxt As String, Optional sFileName As String = "log")
        Dim sFile As String = GetLogFileDaily(sFileName)
        IO.File.AppendAllText(sFile, sTxt)
    End Sub


#End Region

    ' #Region "Bluetooth debugs"  - not in .Net
    ' DebugBTGetServChar - not in .Net
    ' DebugBTGetServChar - not in .Net
    ' 

    ''' <summary>
    ''' ale to wymaga sprawdzenia jak się zachowuje na Android w Uno na przykład
    ''' </summary>
    Public Function GetPlatform() As String
        If Runtime.InteropServices.RuntimeInformation.IsOSPlatform(Runtime.InteropServices.OSPlatform.Windows) Then Return "uwp"
        If Runtime.InteropServices.RuntimeInformation.IsOSPlatform(Runtime.InteropServices.OSPlatform.OSX) Then Return "ios"
        If Runtime.InteropServices.RuntimeInformation.IsOSPlatform(Runtime.InteropServices.OSPlatform.Linux) Then Return "android"
        ' jest jeszcze FreeBSD, moze później będzie jeszcze więcej różnych
        Return "other"
    End Function


    ''' <summary>
    ''' Wybierze co ma być użyte - czy obiekt1 (OneDrive, ret 1), czy obiekt2 (local, ret 2), czy też są takie same (0)
    ''' </summary>
    ''' <param name="oDate1">data danych 1 (zwykle OneDrive)</param>
    ''' <param name="oDate2">data danych 2 (zwykle local)</param>
    ''' <param name="iTolerance">tolerancja w sekundach</param>
    ''' <param name="bLastWas2">gdy oDate1 > oDate2, i bLastWas2, to kolizja</param>
    ''' <returns></returns>
    Public Async Function SelectOneContentChoose(oDate1 As DateTimeOffset, oDate2 As DateTimeOffset, bLastWas2 As Boolean,
                                                 Optional iTolerance As Integer = 10,
                                                 Optional resIdCollisionMsg As String = "msgConflictModifiedODandLocal",
                                                 Optional resIdCollisionUse1 As String = "msgConflictUseOD",
                                                 Optional resIdCollisionUse2 As String = "msgConflictUseLocal") As Task(Of Integer)

        If Math.Abs((oDate1 - oDate2).TotalSeconds) < iTolerance Then Return 0

        If oDate1.AddSeconds(iTolerance) > oDate2 Then
            If bLastWas2 Then
                ' ale lokalnie też zmienione i nie zapisane do OD, więc kolizja
                If Await DialogBoxResYNAsync(resIdCollisionMsg, resIdCollisionUse1, resIdCollisionUse2) Then Return 1
                Return 2
            Else
                ' nowsze OD, lokalnie nie było zapisu, więc wczytuj OD
                Return 1
            End If
        End If

        ' b) nowszy Roam
        If oDate2.AddSeconds(iTolerance) > oDate1 Then
            If bLastWas2 Then
                ' lokalnie było zmienione i nie zapisane do OD, więc OK, używaj lokalnego
                Return 2
            Else
                ' nie powinno się zdarzyć: nowsze lokalnie, ale ostatni zapis był do OneDrive
                Return 2
            End If
        End If

        Return 0

    End Function


End Module

Partial Public Module Extensions

    <Runtime.CompilerServices.Extension()>
    Public Function FileLen2string(ByVal iBytes As Long) As String
        If iBytes = 1 Then Return "1 byte"
        If iBytes < 10000 Then Return iBytes & " bytes"
        iBytes \= 1024
        If iBytes = 1 Then Return "1 kibibyte"
        If iBytes < 2000 Then Return iBytes & " kibibytes"
        iBytes \= 1024
        If iBytes = 1 Then Return "1 mebibyte"
        If iBytes < 2000 Then Return iBytes & " mebibytes"
        iBytes \= 1024
        If iBytes = 1 Then Return "1 gibibyte"
        Return iBytes & " gibibytes"
    End Function

    <Runtime.CompilerServices.Extension()>
    Public Function Between(Of T)(ByVal value As T, minVal As T, maxVal As T) As T
        If Comparer(Of T).Default.Compare(minVal, value) > 0 Then Return minVal
        If Comparer(Of T).Default.Compare(maxVal, value) < 0 Then Return maxVal
        Return value
    End Function


    ' #Region "Read/Write text" - wszystko oznaczone jako OBSOLETE
    ' WriteAllTextAsync - not in .Net
    ' AppendLineAsync - not in .Net
    ' AppendStringAsync - not in .Net
    ' WriteAllTextToFileAsync - not in .Net
    ' ReadAllTextAsync - not in .Net
    ' FileExistsAsync - not in .Net

    ' OpenExplorer - not in .Net

    <Runtime.CompilerServices.Extension()>
    Public Function ToHexBytesString(ByVal iVal As ULong) As String
        Dim sTmp As String = String.Format(Globalization.CultureInfo.InvariantCulture, "{0:X}", iVal)
        If sTmp.Length Mod 2 <> 0 Then sTmp = "0" & sTmp

        Dim sRet As String = ""
        Dim bDwukrop As Boolean = False

        While sTmp.Length > 0
            If bDwukrop Then sRet &= ":"
            bDwukrop = True
            sRet &= sTmp.Substring(0, 2)
            sTmp = sTmp.Substring(2)
        End While

        ' gniazdko BT18, daje 15:A6:00:E8:07 (bez 00:)
        ' 71:0A:22:CD:4F:20
        ' 12345678901234567
        If sRet.Length < 17 Then sRet = "00:" & sRet
        If sRet.Length < 17 Then sRet = "00:" & sRet


        Return sRet
    End Function

    <Runtime.CompilerServices.Extension()>
    Public Function MacStringToULong(ByVal sStr As String) As ULong
        If String.IsNullOrWhiteSpace(sStr) Then Throw New ArgumentNullException(NameOf(sStr), "MacStringToULong powinno miec parametr")
        If Not sStr.Contains(":") Then Throw New ArgumentException("MacStringToULong - nie ma dwukropków w sStr")

        sStr = sStr.Replace(":", "")
        Dim uLng As ULong = ULong.Parse(sStr, System.Globalization.NumberStyles.HexNumber, Globalization.CultureInfo.InvariantCulture)

        Return uLng
    End Function


    ' GetDocumentHtml - not in .Net
    ' #Region "GPS odleglosci"
    ' DistanceTo (dla różnych typów) - not in .Net

    <Runtime.CompilerServices.Extension()>
    Public Function DePascal(ByVal input As String)
        If String.IsNullOrWhiteSpace(input) Then Return ""

        Dim result As String = ""
        Dim letter As String = ""
        'foreach(Char letter In input)
        '{ if(char.isupper(letter) result = result.trim() + " ";
        '  result += letter
        '}
        For i = 0 To input.Length - 1
            letter = input.Substring(0, 1)
            If letter.ToUpperInvariant = letter Then
                result = result.Trim() & " "
            End If
            result &= letter
        Next

        Return result.Trim
    End Function



    ''' <summary>
    ''' odpowiednik StartsWith(sStart, StringComparison.Ordinal)
    ''' </summary>
    <Runtime.CompilerServices.Extension()>
    <CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification:="<Pending>")>
    Public Function StartsWithOrdinal(ByVal baseString As String, value As String) As Boolean
        Return baseString.StartsWith(value, StringComparison.Ordinal)
    End Function

    ''' <summary>
    ''' odpowiednik EndsWith(sEnd, StringComparison.Ordinal)
    ''' </summary>
    <Runtime.CompilerServices.Extension()>
    <CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification:="<Pending>")>
    Public Function EndsWithOrdinal(ByVal baseString As String, value As String) As Boolean
        Return baseString.EndsWith(value, StringComparison.Ordinal)
    End Function

    ''' <summary>
    ''' odpowiednik IndexOf() z StringComparison.Ordinal
    ''' </summary>
    <Runtime.CompilerServices.Extension()>
    <CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification:="<Pending>")>
    Public Function IndexOfOrdinal(ByVal baseString As String, value As String) As Integer
        Return baseString.IndexOf(value, StringComparison.Ordinal)
    End Function

    '         bEvent.IsEnabled = sTmp.Contains("E", StringComparison.Ordinal)


    ''' <summary>
    ''' Zwraca od sStart do końca (lub bez zmian, gdy nie ma sStart)
    ''' </summary>
    <Runtime.CompilerServices.Extension()>
    <CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification:="<Pending>")>
    Public Function TrimBefore(ByVal baseString As String, sStart As String) As String
        If String.IsNullOrEmpty(sStart) Then Return baseString

        Dim iInd As Integer = baseString.IndexOf(sStart, StringComparison.Ordinal)
        If iInd < 0 Then Return baseString
        Return baseString.Substring(iInd)
    End Function

    ''' <summary>
    ''' Zwraca od początku do sEnd (bez niego) (lub bez zmian, gdy nie ma sEnd)
    ''' </summary>
    <Runtime.CompilerServices.Extension()>
    <CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification:="<Pending>")>
    Public Function TrimAfter(ByVal baseString As String, sEnd As String) As String
        If String.IsNullOrEmpty(sEnd) Then Return baseString

        Dim iInd As Integer = baseString.IndexOf(sEnd, StringComparison.Ordinal)
        If iInd < 0 Then Return baseString
        Return baseString.Substring(0, iInd + sEnd.Length)
    End Function

    ''' <summary>
    ''' Zwraca od sStart do końca (lub bez zmian, gdy nie ma sStart) - szuka od końca
    ''' </summary>
    <Runtime.CompilerServices.Extension()>
    <CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification:="<Pending>")>
    Public Function TrimBeforeLast(ByVal baseString As String, sStart As String) As String
        If String.IsNullOrEmpty(sStart) Then Return baseString

        Dim iInd As Integer = baseString.LastIndexOf(sStart, StringComparison.Ordinal)
        If iInd < 0 Then Return baseString
        Return baseString.Substring(iInd)
    End Function

    ''' <summary>
    ''' Zwraca od początku do sEnd do końca (lub bez zmian, gdy nie ma sEnd) - szuka od końca
    ''' </summary>
    <Runtime.CompilerServices.Extension()>
    <CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification:="<Pending>")>
    Public Function TrimAfterLast(ByVal baseString As String, sEnd As String) As String
        If String.IsNullOrEmpty(sEnd) Then Return baseString

        Dim iInd As Integer = baseString.LastIndexOf(sEnd, StringComparison.Ordinal)
        If iInd < 0 Then Return baseString
        Return baseString.Substring(0, iInd + sEnd.Length)
    End Function

    ''' <summary>
    ''' Wycina substring ze stringu, pomiędzy sStart a sEnd
    ''' </summary>
    ''' <param name="baseString"></param>
    ''' <param name="sStart">prefix wycinanego (nie będzie w zwracanym)</param>
    ''' <param name="sEnd">sufix wycinanego (nie będzie w zwracanym)</param>
    ''' <returns>wycięty string lub Empty, jeśli nie ma któregoś końca</returns>
    <Runtime.CompilerServices.Extension()>
    <CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification:="<Pending>")>
    Public Function SubstringBetween(ByVal baseString As String, sStart As String, sEnd As String) As String
        Dim iInd As Integer = baseString.IndexOf(sStart)
        If iInd < 0 Then Return ""

        baseString = baseString.Substring(iInd + sStart.Length)
        iInd = baseString.IndexOf(sEnd)
        If iInd < 0 Then Return ""

        Return baseString.Substring(0, iInd)

    End Function


    ''' <summary>
    ''' Wycina fragment od sStart do sEnd  (włącznie z sStart/sEnd), jeśli któregoś nie ma - nie tyka
    ''' </summary>
    <Runtime.CompilerServices.Extension()>
    <CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification:="<Pending>")>
    Public Function RemoveBetween(ByVal baseString As String, sStart As String, sEnd As String) As String
        If String.IsNullOrEmpty(sStart) Then Return baseString
        If String.IsNullOrEmpty(sEnd) Then Return baseString

        Dim iIndS As Integer = baseString.IndexOf(sStart, StringComparison.Ordinal)
        If iIndS < 0 Then Return baseString
        Dim iIndE As Integer = baseString.IndexOf(sEnd, StringComparison.Ordinal)
        If iIndE < 0 Then Return baseString
        Return baseString.Remove(iIndS + sStart.Length, iIndE - iIndS + 1 - sStart.Length)
    End Function

    <Runtime.CompilerServices.Extension()>
    Public Function ToValidPath(ByVal basestring As String, Optional bDepolituj As Boolean = True, Optional cInvalidCharPlaceholder As String = "") As String
        Dim sRet As String = basestring
        If bDepolituj Then
            sRet = sRet.Replace("ą", "a")
            sRet = sRet.Replace("ć", "c")
            sRet = sRet.Replace("ę", "e")
            sRet = sRet.Replace("ł", "l")
            sRet = sRet.Replace("ń", "n")
            sRet = sRet.Replace("ó", "o")
            sRet = sRet.Replace("ś", "s")
            sRet = sRet.Replace("ż", "z")
            sRet = sRet.Replace("ź", "z")
            sRet = sRet.Replace("Ą", "a")
            sRet = sRet.Replace("Ć", "c")
            sRet = sRet.Replace("Ę", "E")
            sRet = sRet.Replace("Ł", "L")
            sRet = sRet.Replace("Ń", "N")
            sRet = sRet.Replace("Ó", "O")
            sRet = sRet.Replace("Ś", "S")
            sRet = sRet.Replace("Ż", "Z")
            sRet = sRet.Replace("Ź", "Z")
        End If

        Dim aInvChars As Char() = IO.Path.GetInvalidFileNameChars
        For Each sInvChar As Char In aInvChars
            sRet = sRet.Replace(sInvChar, cInvalidCharPlaceholder)
        Next

        aInvChars = IO.Path.GetInvalidPathChars
        For Each sInvChar As Char In aInvChars
            sRet = sRet.Replace(sInvChar, cInvalidCharPlaceholder)
        Next

        Return sRet

    End Function

    <Runtime.CompilerServices.Extension()>
    <CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification:="<Pending>")>
    <CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification:="<Pending>")>
    Public Function ToDebugString(ByVal aArr As Byte(), iSpaces As Integer) As String

        Dim sPrefix As String = ""
        For i As Integer = 1 To iSpaces
            sPrefix &= " "
        Next

        Dim sBytes As String = ""
        Dim sAscii As String = sBytes

        For i As Integer = 0 To Math.Min(aArr.Length - 1, 32) ' bylo oVal

            Dim cBajt As Byte = aArr.ElementAt(i)

            ' hex: tylko 16 bajtow
            If i < 16 Then
                Try
                    sBytes = sBytes & " 0x" & String.Format(Globalization.CultureInfo.InvariantCulture, "{0:X}", cBajt)
                Catch ex As Exception
                    sBytes &= " ??"
                End Try
            End If

            ' ascii: do 32 bajtow
            If cBajt > 31 And cBajt < 160 Then
                sAscii &= ChrW(cBajt)
            Else
                sAscii &= "?"
            End If
        Next

        If aArr.Length - 1 > 16 Then sBytes &= " ..."
        If aArr.Length - 1 > 32 Then sAscii &= " ..."

        Dim sRet As String = ""
        If aArr.Length > 6 Then sRet = sPrefix & "length: " & aArr.Length
        sRet = sRet & sPrefix & "binary: " & sBytes & vbCrLf &
            sPrefix & "ascii:  " & sAscii

        Return sRet & vbCrLf

    End Function

    ' ToDebugString(ByVal oBuf As Windows.Storage.Streams.IBuffer - not in .Net

    <Runtime.CompilerServices.Extension()>
    Public Function ToStringWithSpaces(ByVal iInteger As Integer) As String
        Dim nfi As System.Globalization.NumberFormatInfo
        nfi = System.Globalization.NumberFormatInfo.InvariantInfo.Clone
        nfi.NumberGroupSeparator = " "
        Return iInteger.ToString(nfi)
    End Function

    <Runtime.CompilerServices.Extension()>
    Public Function ToStringWithSpaces(ByVal iLong As Long) As String
        Dim nfi As System.Globalization.NumberFormatInfo
        nfi = System.Globalization.NumberFormatInfo.InvariantInfo.Clone
        nfi.NumberGroupSeparator = " "
        Return iLong.ToString(nfi)
    End Function

    <Runtime.CompilerServices.Extension()>
    Public Function ToStringDHMS(ByVal iSecs As Long) As String
        Dim sTmp As String = ""
        If iSecs > 60 * 60 * 24 Then
            sTmp = sTmp & iSecs \ (60 * 60 * 24) & "d "
            iSecs = iSecs Mod (60 * 60 * 24)
        End If
        If iSecs > 60 * 60 Then
            sTmp = sTmp & iSecs \ (60 * 60) & ":"
            iSecs = iSecs Mod (60 * 60)
        End If
        If iSecs \ 60 < 10 And sTmp.Length > 1 Then sTmp &= "0"
        sTmp = sTmp & iSecs \ 60 & ":"
        iSecs = iSecs Mod 60
        If iSecs < 10 And sTmp.Length > 1 Then sTmp &= "0"
        sTmp &= iSecs.ToString(Globalization.CultureInfo.InvariantCulture)

        Return sTmp
    End Function

    <Runtime.CompilerServices.Extension()>
    Public Function ToStringISOsufix(ByVal iValue As Long, Optional sUnit As String = "Bytes") As String
        ' zakres integer: 2.1 Gi        (2 147 483 647)
        ' zakres long: 9.2  (9 223 372 036 854 775 808)

        If iValue < 9999 Then Return iValue.BigNumFormat & If(sUnit = "", "", " " & sUnit)

        iValue = iValue / 1024 + 1
        If iValue < 9999 Then Return iValue.BigNumFormat & " Ki" & sUnit

        iValue = iValue / 1024 + 1
        If iValue < 9999 Then Return iValue.BigNumFormat & " Mi" & sUnit

        iValue = iValue / 1024 + 1
        If iValue < 9999 Then Return iValue.BigNumFormat & " Gi" & sUnit

        iValue = iValue / 1024 + 1
        If iValue < 9999 Then Return iValue.BigNumFormat & " Ti" & sUnit

        iValue = iValue / 1024 + 1
        Return iValue.BigNumFormat & " Ei" & sUnit


    End Function

    ''' <summary>
    ''' Max ~150 dni
    ''' </summary>
    ''' <param name="iSecs"></param>
    ''' <returns></returns>
    <Runtime.CompilerServices.Extension()>
    Public Function ToStringDHMS(ByVal iSecs As Integer) As String
        ' integer = 2,147,483,647, z sekund na 3600 godzin, 150 dni
        Dim temp As Long = iSecs
        Return temp.ToStringDHMS
    End Function

    ''' <summary>
    ''' wstawia spację tysięczną
    ''' </summary>
    ''' <param name="iValue"></param>
    ''' <returns></returns>
    <Runtime.CompilerServices.Extension()>
    Public Function BigNumFormat(ByVal iValue As Long) As String
        Dim sTxt As String = iValue.ToString
        If sTxt.Length > 4 Then sTxt = sTxt.Substring(0, sTxt.Length - 3) & " " & sTxt.Substring(sTxt.Length - 3)
        Return sTxt
    End Function

    <Runtime.CompilerServices.Extension()>
    Public Function BigNumFormat(ByVal iValue As Integer) As String
        Dim sTxt As String = iValue.ToString
        If sTxt.Length > 4 Then sTxt = sTxt.Substring(0, sTxt.Length - 3) & " " & sTxt.Substring(sTxt.Length - 3)
        Return sTxt
    End Function

    ' #Region "Settingsy jako Extension" - not in .Net
    ' ShowAppVers(ByVal oItem As TextBlock) - not in .Net
    ' ShowAppVers(ByVal oPage As Page) - not in .Net
    ' #Region "ProgressBar/Ring" - not in .Net

#Region "Bluetooth debug strings"

    ' ToDebugString(ByVal oAdv As Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisement - not in .Net
    ' ToDebugStringAsync(ByVal oDescriptor As Windows.Devices.Bluetooth.GenericAttributeProfile - not in .Net
    ' ToDebugString(ByVal oProp As Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristicProperties - not in .Net
    ' ToDebugStringAsync(ByVal oChar As Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic - not in .Net
    ' ToDebusStringAsync(ByVal oServ As Windows.Devices.Bluetooth.GenericAttributeProfile.GattDeviceService - not in .Net
    ' ToDebusStringAsync(ByVal oDevice As Windows.Devices.Bluetooth.BluetoothLEDevice - not in .Net

    <Runtime.CompilerServices.Extension()>
    Public Function AsGattReservedDescriptorName(ByVal oGUID As Guid) As String
        Dim sGuid As String = oGUID.ToString
        Select Case sGuid
            Case "00002900-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Characteristic Extended Properties"
            Case "00002901-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Characteristic User Description"
            Case "00002902-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Client Characteristic Configuration"
            Case "00002903-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Server Characteristic Configuration"
            Case "00002904-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Characteristic Presentation Format"
            Case "00002905-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Characteristic Aggregate Format"
            Case "00002906-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Valid Range"
            Case "00002907-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: External Report Reference"
            Case "00002908-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Report Reference"
            Case "00002909-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Number of Digitals"
            Case "0000290a-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Value Trigger Setting"
            Case "0000290b-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Environmental Sensing Configuration"
            Case "0000290c-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Environmental Sensing Measurement"
            Case "0000290d-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Environmental Sensing Trigger Setting"
            Case "0000290e-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Time Trigger Setting"
        End Select
        Return ""
    End Function

    <Runtime.CompilerServices.Extension()>
    Public Function AsGattReservedServiceName(ByVal oGUID As Guid) As String
        Dim sServ As String = oGUID.ToString

        Select Case sServ
            Case "00001800-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Generic Access"
            Case "00001801-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Generic Attribute"
            Case "00001802-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Immediate Alert"
            Case "00001803-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Link Loss"
            Case "00001804-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Tx Power"
            Case "00001805-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Current Time Service"
            Case "00001806-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Reference Time Update Service"
            Case "00001807-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Next DST Change Service"
            Case "00001808-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Glucose"
            Case "00001809-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Health Thermometer"
            Case "0000180a-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Device Information"
            Case "0000180d-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Heart Rate"
            Case "0000180e-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Phone Alert Status Service"
            Case "0000180f-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Battery Service"
            Case "00001810-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Blood Pressure"
            Case "00001811-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Alert Notification Service"
            Case "00001812-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Human Interface Device"
            Case "00001813-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Scan Parameters"
            Case "00001814-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Running Speed and Cadence"
            Case "00001815-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Automation IO"
            Case "00001816-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Cycling Speed and Cadence"
            Case "00001818-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Cycling Power"
            Case "00001819-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Location and Navigation"
            Case "0000181a-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Environmental Sensing"
            Case "0000181b-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Body Composition"
            Case "0000181c-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: User Data"
            Case "0000181d-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Weight Scale"
            Case "0000181e-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Bond Management Service"
            Case "0000181f-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Continuous Glucose Monitoring"
            Case "00001820-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Internet Protocol Support Service"
            Case "00001821-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Indoor Positioning"
            Case "00001822-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Pulse Oximeter Service"
            Case "00001823-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: HTTP Proxy"
            Case "00001824-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Transport Discovery"
            Case "00001825-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Object Transfer Service"
            Case "00001826-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Fitness Machine"
            Case "00001827-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Mesh Provisioning Service"
            Case "00001828-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Mesh Proxy Service"
            Case "00001829-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Reconnection Configuration"
        End Select

        Return ""
    End Function


    <Runtime.CompilerServices.Extension()>
    Public Function AsGattReservedCharacteristicName(ByVal oGUID As Guid) As String
        Dim sChar As String = oGUID.ToString

        Select Case sChar
            Case "00002a00-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Device Name"
            Case "00002a01-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Appearance"
            Case "00002a02-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Peripheral Privacy Flag"
            Case "00002a03-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Reconnection Address"
            Case "00002a04-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Peripheral Preferred Connection Parameters"
            Case "00002a05-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Service Changed"
            Case "00002a06-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Alert Level"
            Case "00002a07-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Tx Power Level"
            Case "00002a08-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Date Time"
            Case "00002a09-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Day of Week"
            Case "00002a0a-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Day Date Time"
            Case "00002a0b-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Exact Time 100"
            Case "00002a0c-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Exact Time 256"
            Case "00002a0d-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: DST Offset"
            Case "00002a0e-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Time Zone"
            Case "00002a0f-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Local Time Information"
            Case "00002a10-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Secondary Time Zone"
            Case "00002a11-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Time with DST"
            Case "00002a12-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Time Accuracy"
            Case "00002a13-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Time Source"
            Case "00002a14-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Reference Time Information"
            Case "00002a15-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Time Broadcast"
            Case "00002a16-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Time Update Control Point"
            Case "00002a17-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Time Update State"
            Case "00002a18-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Glucose Measurement"
            Case "00002a19-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Battery Level"
            Case "00002a1a-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Battery Power State"
            Case "00002a1b-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Battery Level State"
            Case "00002a1c-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Temperature Measurement"
            Case "00002a1d-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Temperature Type"
            Case "00002a1e-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Intermediate Temperature"
            Case "00002a1f-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Temperature Celsius"
            Case "00002a20-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Temperature Fahrenheit"
            Case "00002a21-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Measurement Interval"
            Case "00002a22-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Boot Keyboard Input Report"
            Case "00002a23-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: System ID"
            Case "00002a24-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Model Number String"
            Case "00002a25-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Serial Number String"
            Case "00002a26-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Firmware Revision String"
            Case "00002a27-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Hardware Revision String"
            Case "00002a28-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Software Revision String"
            Case "00002a29-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Manufacturer Name String"
            Case "00002a2a-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: IEEE 11073-20601 Regulatory Certification Data List"
            Case "00002a2b-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Current Time"
            Case "00002a2c-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Magnetic Declination"
            Case "00002a2f-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Position 2D"
            Case "00002a30-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Position 3D"
            Case "00002a31-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Scan Refresh"
            Case "00002a32-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Boot Keyboard Output Report"
            Case "00002a33-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Boot Mouse Input Report"
            Case "00002a34-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Glucose Measurement Context"
            Case "00002a35-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Blood Pressure Measurement"
            Case "00002a36-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Intermediate Cuff Pressure"
            Case "00002a37-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Heart Rate Measurement"
            Case "00002a38-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Body Sensor Location"
            Case "00002a39-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Heart Rate Control Point"
            Case "00002a3a-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Removable"
            Case "00002a3b-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Service Required"
            Case "00002a3c-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Scientific Temperature Celsius"
            Case "00002a3d-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: String"
            Case "00002a3e-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Network Availability"
            Case "00002a3f-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Alert Status"
            Case "00002a40-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Ringer Control point"
            Case "00002a41-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Ringer Setting"
            Case "00002a42-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Alert Category ID Bit Mask"
            Case "00002a43-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Alert Category ID"
            Case "00002a44-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Alert Notification Control Point"
            Case "00002a45-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Unread Alert Status"
            Case "00002a46-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: New Alert"
            Case "00002a47-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Supported New Alert Category"
            Case "00002a48-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Supported Unread Alert Category"
            Case "00002a49-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Blood Pressure Feature"
            Case "00002a4a-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: HID Information"
            Case "00002a4b-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Report Map"
            Case "00002a4c-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: HID Control Point"
            Case "00002a4d-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Report"
            Case "00002a4e-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Protocol Mode"
            Case "00002a4f-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Scan Interval Window"
            Case "00002a50-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: PnP ID"
            Case "00002a51-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Glucose Feature"
            Case "00002a52-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Record Access Control Point"
            Case "00002a53-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: RSC Measurement"
            Case "00002a54-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: RSC Feature"
            Case "00002a55-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: SC Control Point"
            Case "00002a56-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Digital"
            Case "00002a57-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Digital Output"
            Case "00002a58-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Analog"
            Case "00002a59-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Analog Output"
            Case "00002a5a-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Aggregate"
            Case "00002a5b-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: CSC Measurement"
            Case "00002a5c-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: CSC Feature"
            Case "00002a5d-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Sensor Location"
            Case "00002a5e-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: PLX Spot-Check Measurement"
            Case "00002a5f-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: PLX Continuous Measurement Characteristic"
            Case "00002a60-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: PLX Features"
            Case "00002a62-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Pulse Oximetry Control Point"
            Case "00002a63-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Cycling Power Measurement"
            Case "00002a64-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Cycling Power Vector"
            Case "00002a65-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Cycling Power Feature"
            Case "00002a66-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Cycling Power Control Point"
            Case "00002a67-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Location and Speed Characteristic"
            Case "00002a68-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Navigation"
            Case "00002a69-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Position Quality"
            Case "00002a6a-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: LN Feature"
            Case "00002a6b-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: LN Control Point"
            Case "00002a6c-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Elevation"
            Case "00002a6d-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Pressure"
            Case "00002a6e-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Temperature"
            Case "00002a6f-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Humidity"
            Case "00002a70-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: True Wind Speed"
            Case "00002a71-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: True Wind Direction"
            Case "00002a72-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Apparent Wind Speed"
            Case "00002a73-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Apparent Wind Direction"
            Case "00002a74-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Gust Factor"
            Case "00002a75-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Pollen Concentration"
            Case "00002a76-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: UV Index"
            Case "00002a77-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Irradiance"
            Case "00002a78-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Rainfall"
            Case "00002a79-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Wind Chill"
            Case "00002a7a-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Heat Index"
            Case "00002a7b-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Dew Point"
            Case "00002a7d-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Descriptor Value Changed"
            Case "00002a7e-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Aerobic Heart Rate Lower Limit"
            Case "00002a7f-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Aerobic Threshold"
            Case "00002a80-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Age"
            Case "00002a81-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Anaerobic Heart Rate Lower Limit"
            Case "00002a82-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Anaerobic Heart Rate Upper Limit"
            Case "00002a83-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Anaerobic Threshold"
            Case "00002a84-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Aerobic Heart Rate Upper Limit"
            Case "00002a85-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Date of Birth"
            Case "00002a86-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Date of Threshold Assessment"
            Case "00002a87-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Email Address"
            Case "00002a88-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Fat Burn Heart Rate Lower Limit"
            Case "00002a89-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Fat Burn Heart Rate Upper Limit"
            Case "00002a8a-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: First Name"
            Case "00002a8b-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Five Zone Heart Rate Limits"
            Case "00002a8c-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Gender"
            Case "00002a8d-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Heart Rate Max"
            Case "00002a8e-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Height"
            Case "00002a8f-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Hip Circumference"
            Case "00002a90-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Last Name"
            Case "00002a91-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Maximum Recommended Heart Rate"
            Case "00002a92-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Resting Heart Rate"
            Case "00002a93-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Sport Type for Aerobic and Anaerobic Thresholds"
            Case "00002a94-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Three Zone Heart Rate Limits"
            Case "00002a95-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Two Zone Heart Rate Limit"
            Case "00002a96-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: VO2 Max"
            Case "00002a97-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Waist Circumference"
            Case "00002a98-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Weight"
            Case "00002a99-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Database Change Increment"
            Case "00002a9a-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: User Index"
            Case "00002a9b-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Body Composition Feature"
            Case "00002a9c-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Body Composition Measurement"
            Case "00002a9d-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Weight Measurement"
            Case "00002a9e-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Weight Scale Feature"
            Case "00002a9f-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: User Control Point"
            Case "00002aa0-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Magnetic Flux Density - 2D"
            Case "00002aa1-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Magnetic Flux Density - 3D"
            Case "00002aa2-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Language"
            Case "00002aa3-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Barometric Pressure Trend"
            Case "00002aa4-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Bond Management Control Point"
            Case "00002aa5-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Bond Management Features"
            Case "00002aa6-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Central Address Resolution"
            Case "00002aa7-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: CGM Measurement"
            Case "00002aa8-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: CGM Feature"
            Case "00002aa9-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: CGM Status"
            Case "00002aaa-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: CGM Session Start Time"
            Case "00002aab-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: CGM Session Run Time"
            Case "00002aac-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: CGM Specific Ops Control Point"
            Case "00002aad-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Indoor Positioning Configuration"
            Case "00002aae-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Latitude"
            Case "00002aaf-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Longitude"
            Case "00002ab0-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Local North Coordinate"
            Case "00002ab1-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Local East Coordinate"
            Case "00002ab2-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Floor Number"
            Case "00002ab3-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Altitude"
            Case "00002ab4-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Uncertainty"
            Case "00002ab5-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Location Name"
            Case "00002ab6-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: URI"
            Case "00002ab7-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: HTTP Headers"
            Case "00002ab8-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: HTTP Status Code"
            Case "00002ab9-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: HTTP Entity Body"
            Case "00002aba-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: HTTP Control Point"
            Case "00002abb-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: HTTPS Security"
            Case "00002abc-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: TDS Control Point"
            Case "00002abd-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: OTS Feature"
            Case "00002abe-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Object Name"
            Case "00002abf-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Object Type"
            Case "00002ac0-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Object Size"
            Case "00002ac1-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Object First-Created"
            Case "00002ac2-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Object Last-Modified"
            Case "00002ac3-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Object ID"
            Case "00002ac4-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Object Properties"
            Case "00002ac5-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Object Action Control Point"
            Case "00002ac6-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Object List Control Point"
            Case "00002ac7-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Object List Filter"
            Case "00002ac8-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Object Changed"
            Case "00002ac9-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Resolvable Private Address Only"
            Case "00002acc-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Fitness Machine Feature"
            Case "00002acd-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Treadmill Data"
            Case "00002ace-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Cross Trainer Data"
            Case "00002acf-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Step Climber Data"
            Case "00002ad0-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Stair Climber Data"
            Case "00002ad1-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Rower Data"
            Case "00002ad2-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Indoor Bike Data"
            Case "00002ad3-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Training Status"
            Case "00002ad4-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Supported Speed Range"
            Case "00002ad5-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Supported Inclination Range"
            Case "00002ad6-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Supported Resistance Level Range"
            Case "00002ad7-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Supported Heart Rate Range"
            Case "00002ad8-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Supported Power Range"
            Case "00002ad9-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Fitness Machine Control Point"
            Case "00002ada-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Fitness Machine Status"
            Case "00002aed-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Date UTC"
            Case "00002b1d-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: RC Feature"
            Case "00002b1e-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: RC Settings"
            Case "00002b1f-0000-1000-8000-00805f9b34fb"
                Return vbTab & "known as: Reconnection Configuration Control Point"
        End Select

        Return ""
    End Function
#End Region

End Module

#Region ".Net Standard Settings"

#Region "JSON read/write"

' a) w .Net Standard 1.4 zwykły JSON nie działa
' b) zwykłe JSON jest read/only
' c) ograniczenie: nie ma drzewka wartości
' d) lepsze od INI: bo obsługa \n i tym podobnych rzeczy
' e) podział zmiennych na ROAM oraz zwykłe (dwa pliki)

Friend Class JsonRwConfigurationProvider
    Inherits Microsoft.Extensions.Configuration.ConfigurationProvider

    Private ReadOnly _sPathnameLocal As String
    Private ReadOnly _sPathnameRoam As String
    Private ReadOnly _bReadOnly As Boolean

    Protected DataRoam As New Dictionary(Of String, String)

    Friend Sub New(sPathnameLocal As String, sPathnameRoam As String, bReadOnly As Boolean)
        _sPathnameLocal = sPathnameLocal
        _sPathnameRoam = sPathnameRoam
        _bReadOnly = bReadOnly
    End Sub

    Public Overrides Sub Load()

        ' load settings

        If _sPathnameLocal <> "" AndAlso IO.File.Exists(_sPathnameLocal) Then
            Dim sFileContent As String = IO.File.ReadAllText(_sPathnameLocal)
            Data = Newtonsoft.Json.JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(sFileContent)
        End If

        If _sPathnameRoam <> "" AndAlso IO.File.Exists(_sPathnameRoam) Then
            Dim sFileContent As String = IO.File.ReadAllText(_sPathnameRoam)
            DataRoam = Newtonsoft.Json.JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(sFileContent)
        End If

    End Sub

    Public Overrides Sub [Set](key As String, value As String)

        If _bReadOnly Then
            Data.Remove(key)
            DataRoam.Remove(key)
            Return
        End If

        If _sPathnameRoam <> "" Then
            ' interpretujemy [roam]
            If value.ToLower.StartsWith("[roam]", StringComparison.Ordinal) Then
                ' wersja ROAM
                value = value.Substring("[roam]".Length)
                DataRoam(key) = value
                Dim sJson As String = Newtonsoft.Json.JsonConvert.SerializeObject(DataRoam, Newtonsoft.Json.Formatting.Indented)
                IO.File.WriteAllText(_sPathnameRoam, sJson)
            End If

        End If

        If _sPathnameLocal <> "" Then
            Data(key) = value
            Dim sJson As String = Newtonsoft.Json.JsonConvert.SerializeObject(Data, Newtonsoft.Json.Formatting.Indented)
            IO.File.WriteAllText(_sPathnameLocal, sJson)
        End If

    End Sub

    Public Overrides Function TryGet(key As String, ByRef value As String) As Boolean
        Dim bRoam As Boolean = DataRoam.TryGetValue(key, value)
        Dim bLocal As Boolean = Data.TryGetValue(key, value)

        Return (bLocal Or bRoam)

    End Function

End Class

Friend Class JsonRwConfigurationSource
    Implements Microsoft.Extensions.Configuration.IConfigurationSource

    Private ReadOnly _sPathnameLocal As String
    Private ReadOnly _sPathnameRoam As String
    Private _bReadOnly As Boolean

    Public Function Build(builder As IConfigurationBuilder) As IConfigurationProvider Implements IConfigurationSource.Build
        Return New JsonRwConfigurationProvider(_sPathnameLocal, _sPathnameRoam, _bReadOnly)
    End Function

    Public Sub New(sPathnameLocal As String, sPathnameRoam As String, bReadOnly As Boolean)

        If String.IsNullOrWhiteSpace(sPathnameLocal) AndAlso String.IsNullOrWhiteSpace(sPathnameRoam) Then
            Throw New ArgumentException("You have to use at least one real path (to file, or to folder) for JsonRwConfigurationSource constructor")
        End If

        _bReadOnly = bReadOnly  ' przed poniższymi, bo poniższe włącza r/o gdy jest to plik w appx
        _sPathnameLocal = TryFileOrPathExist(sPathnameLocal, "AppSettings.json")
        _sPathnameRoam = TryFileOrPathExist(sPathnameRoam, "AppRoamSettings.json")

    End Sub

    Private Function TryFileOrPathExist(sPath As String, sDefaultFileName As String) As String

        If String.IsNullOrWhiteSpace(sPath) Then Return ""

        ' może być ścieżka w ramach appx - ale wtedy readonly
        If Not IO.Path.IsPathRooted(sPath) Then
            sPath = IO.Path.Combine(System.AppContext.BaseDirectory, sPath)
            _bReadOnly = True
        End If

        ' gdy plik istnieje, to jest OK
        If IO.File.Exists(sPath) Then Return sPath

        ' gdy to ścieżka (katalog), to ma robimy tam plik o domyślnej nazwie
        If IO.Directory.Exists(sPath) Then
            sPath = IO.Path.Combine(sPath, sDefaultFileName)
            Return sPath
        End If

        ' gdy jest to ścieżka do pliku, który nie istnieje - też jest OK
        If IO.Directory.Exists(IO.Path.GetDirectoryName(sPath)) Then Return sPath

        Throw New ArgumentException("Pathname doesn't point to file or ")

    End Function


End Class

#End Region

#Region "INI release/debug"

' a) ograniczenie: nie ma drzewka wartości
' b) lepsze od JSON, bo prostsze do tworzenia
' c) gorsze od JSON, bo nie ma obsługi \n i tym podobnych rzeczy
' d) sekcja [main] oraz [debug] (tylko przy DEBUG), a także komentarze # ' //
' e) nie robi Set, a więc nie zaśmieca zmiennych (zwłaszcza przy Value=[roam]value

Friend Class IniDefaultsConfigurationProvider
    Inherits Microsoft.Extensions.Configuration.ConfigurationProvider

    Private _sIniContent As String

    Public Overrides Sub Load()
        ' load settings
        If _sIniContent = "" Then Return ' nie ma pliku, pewnie Android (wersja bez Init)

        Dim aFileContent As String() = _sIniContent.Split(vbCrLf, options:=StringSplitOptions.RemoveEmptyEntries)
        LoadSection(aFileContent, "main")
#If DEBUG Then
        LoadSection(aFileContent, "debug")
#End If

    End Sub

    Public Overrides Sub [Set](key As String, value As String)
        ' specjalnie nie zapisuje nowej wartosci, a nawet usuwa - żeby nie śmiecić
        Data.Remove(key)
    End Sub

    Private Sub LoadSection(ByRef aArray As String(), sSection As String)
        Dim bInSection As Boolean = False

        sSection = "[" & sSection.ToLower & "]"

        For Each sLine In aArray
            Dim sLineTrim As String = sLine.Trim

            If sLineTrim.StartsWith("[", StringComparison.Ordinal) AndAlso
                sLineTrim.EndsWith("]", StringComparison.Ordinal) Then

                If sLineTrim.ToLower = sSection Then
                    bInSection = True
                Else
                    bInSection = False
                End If
            Else
                If bInSection Then
                    If sLineTrim.StartsWithOrdinal("#") Then Continue For
                    If sLineTrim.StartsWithOrdinal("'") Then Continue For
                    If sLineTrim.StartsWithOrdinal(";") Then Continue For
                    If sLineTrim.StartsWithOrdinal("//") Then Continue For

                    Dim iInd As Integer = sLineTrim.IndexOf(" # ")
                    If iInd > 0 Then sLineTrim = sLineTrim.Substring(0, iInd)

                    iInd = sLineTrim.IndexOf(" ' ")
                    If iInd > 0 Then sLineTrim = sLineTrim.Substring(0, iInd)

                    iInd = sLineTrim.IndexOf(" ; ")
                    If iInd > 0 Then sLineTrim = sLineTrim.Substring(0, iInd)

                    iInd = sLineTrim.IndexOf(" // ")
                    If iInd > 0 Then sLineTrim = sLineTrim.Substring(0, iInd)

                    iInd = sLineTrim.IndexOf(vbTab & "# ")
                    If iInd > 0 Then sLineTrim = sLineTrim.Substring(0, iInd)

                    iInd = sLineTrim.IndexOf(vbTab & "' ")
                    If iInd > 0 Then sLineTrim = sLineTrim.Substring(0, iInd)

                    iInd = sLineTrim.IndexOf(vbTab & "; ")
                    If iInd > 0 Then sLineTrim = sLineTrim.Substring(0, iInd)

                    iInd = sLineTrim.IndexOf(vbTab & "// ")
                    If iInd > 0 Then sLineTrim = sLineTrim.Substring(0, iInd)

                    Dim aKeyVal As String() = sLineTrim.Split("=")
                    If aKeyVal.Length = 2 Then
                        Data(aKeyVal(0).Trim) = aKeyVal(1).Trim
                    Else
                        Debug.WriteLine("IniDefaultsConfigurationProvider.LoadSection: unrecognized line: " & sLineTrim)
                    End If
                End If
            End If
        Next
    End Sub

    Public Sub New(sIniContent As String)
        _sIniContent = sIniContent
    End Sub

End Class

Friend Class IniDefaultsConfigurationSource
    Implements Microsoft.Extensions.Configuration.IConfigurationSource

    Private _sIniContent As String

    Public Function Build(builder As IConfigurationBuilder) As IConfigurationProvider Implements IConfigurationSource.Build
        Return New IniDefaultsConfigurationProvider(_sIniContent)
    End Function

    Public Sub New(sIniContent As String)
        _sIniContent = sIniContent
    End Sub
End Class

#End Region

#Region "CmdlineRO"

' a) ograniczenie: nie ma drzewka wartości
' b) lepsze od JSON, bo prostsze do tworzenia
' c) gorsze od JSON, bo nie ma obsługi \n i tym podobnych rzeczy
' d) sekcja [main] oraz [debug] (tylko przy DEBUG), a także komentarze # ' //
' e) nie robi Set, a więc nie zaśmieca zmiennych (zwłaszcza przy Value=[roam]value

Friend Class CommandLineROConfigurationProvider
    Inherits Microsoft.Extensions.Configuration.ConfigurationProvider

    Private ReadOnly _aArgs As List(Of String)

    Public Overrides Sub Load()
        ' niemal dosłowna kopia z
        ' https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Configuration.CommandLine/src/CommandLineConfigurationProvider.cs
        Dim key, value As String

        Using enumerator As IEnumerator(Of String) = _aArgs.GetEnumerator()
            ' key1=value1 --key2=value2 /key3=value3 --key4 value4 /key5 value5
            While enumerator.MoveNext()

                Dim currentArg As String = enumerator.Current
                Dim bWasKeyPrefix As Boolean = True

                If currentArg.StartsWith("--", StringComparison.Ordinal) Then
                    currentArg = currentArg.Substring(2)
                ElseIf currentArg.StartsWith("-", StringComparison.Ordinal) Then
                    currentArg = currentArg.Substring(1)
                ElseIf currentArg.StartsWith("/", StringComparison.Ordinal) Then
                    currentArg = currentArg.Substring(1)
                Else
                    bWasKeyPrefix = False
                End If

                Dim separator = currentArg.IndexOfOrdinal("=")

                If separator < 0 Then
                    ' nie ma '=', a więc następne powinno być wartością (--key4 value4 /key5 value5)
                    If Not bWasKeyPrefix Then
                        ' If there is neither equal sign nor prefix in current argument, it is an invalid format
                        ' Ignore invalid formats
                        Continue While
                    End If

                    key = currentArg

                    If Not enumerator.MoveNext() Then
                        ' ignore missing values
                        Continue While
                    End If

                    value = enumerator.Current

                Else
                    ' jest '=', a więc: key1=value1 --key2=value2 /key3=value3
                    key = currentArg.Substring(0, separator)
                    value = currentArg.Substring(separator + 1)
                End If

                ' Override value when key is duplicated. So we always have the last argument win.
                Data(key) = value
            End While
        End Using

    End Sub


    Public Overrides Sub [Set](key As String, value As String)
        ' specjalnie nie zapisuje nowej wartosci, a nawet usuwa - żeby nie śmiecić
        Data.Remove(key)
    End Sub

    Public Sub New(aArgs As List(Of String))
        _aArgs = aArgs
    End Sub

End Class

Friend Class CommandLineROConfigurationSource
    Implements Microsoft.Extensions.Configuration.IConfigurationSource

    Private ReadOnly _aArgs As List(Of String)

    Public Function Build(builder As IConfigurationBuilder) As IConfigurationProvider Implements IConfigurationSource.Build
        Return New CommandLineROConfigurationProvider(_aArgs)
    End Function

    Public Sub New(aArgs As List(Of String))
        _aArgs = aArgs
    End Sub

End Class

#End Region

#Region "Environment variables"
' a) wersja Microsoft jest od .Net 2.0
' b) nie robi Set, a więc nie zaśmieca zmiennych (zwłaszcza przy Value=[roam]value)

Friend Class EnvironmentVariablesROConfigurationProvider
    Inherits Microsoft.Extensions.Configuration.ConfigurationProvider

    Private ReadOnly _sPrefix As String
    Private ReadOnly _oDict As System.Collections.IDictionary

    ' używa tylko tych z prefiksem (dla app), zawierające "pkar" oraz tu podane
    Private ReadOnly _AlwaysCopy As String = "|COMPUTERNAME|USERNAME|"

    Public Overrides Sub Load()

        For Each sVariable As DictionaryEntry In _oDict
            Dim sKey As String = sVariable.Key.ToString.ToLower
            Dim sVal As String = sVariable.Value.ToString
            If sKey.StartsWithOrdinal("pkar") Then
                Data(sKey) = sVal
            ElseIf sKey.StartsWithOrdinal(_sPrefix) Then
                sKey = sKey.Substring(_sPrefix.Length)
                Data(sKey) = sVal
            ElseIf _AlwaysCopy.Contains("|" & sKey & "|") Then
                Data(sKey) = sVal
            End If
        Next

    End Sub


    Public Overrides Sub [Set](key As String, value As String)
        ' specjalnie nie zapisuje nowej wartosci, a nawet usuwa - żeby nie śmiecić
        Data.Remove(key)
    End Sub

    Public Sub New(sPrefix As String, oDict As System.Collections.IDictionary)
        _sPrefix = sPrefix.ToLower
        _oDict = oDict
    End Sub

End Class

Friend Class EnvironmentVariablesROConfigurationSource
    Implements Microsoft.Extensions.Configuration.IConfigurationSource

    Private ReadOnly _sPrefix As String
    Private ReadOnly _oDict As IDictionary(Of String, String)

    Public Function Build(builder As IConfigurationBuilder) As IConfigurationProvider Implements IConfigurationSource.Build
        Return New EnvironmentVariablesROConfigurationProvider(_sPrefix, _oDict)
    End Function

    Public Sub New(sPrefix As String, oDict As System.Collections.IDictionary)
        _sPrefix = sPrefix

        _oDict = New Dictionary(Of String, String)
        For Each oItem As DictionaryEntry In oDict
            _oDict(oItem.Key) = oItem.Value
        Next

    End Sub

End Class

#End Region

Partial Module Extensions
    <Runtime.CompilerServices.Extension()>
    <CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification:="<Pending>")>
    Public Function AddJsonRwSettings(ByVal configurationBuilder As IConfigurationBuilder, sPathnameLocal As String, sPathnameRoam As String, Optional bReadOnly As Boolean = False) As IConfigurationBuilder
        configurationBuilder.Add(New JsonRwConfigurationSource(sPathnameLocal, sPathnameRoam, bReadOnly))
        Return configurationBuilder
    End Function

    <Runtime.CompilerServices.Extension()>
    <CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification:="<Pending>")>
    Public Function AddIniRelDebugSettings(ByVal configurationBuilder As IConfigurationBuilder, sIniContent As String) As IConfigurationBuilder
        configurationBuilder.Add(New IniDefaultsConfigurationSource(sIniContent))
        Return configurationBuilder
    End Function

    <Runtime.CompilerServices.Extension()>
    <CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification:="<Pending>")>
    Public Function AddCommandLineRO(ByVal configurationBuilder As IConfigurationBuilder, aArgs As List(Of String)) As IConfigurationBuilder
        configurationBuilder.Add(New CommandLineROConfigurationSource(aArgs))
        Return configurationBuilder
    End Function


    <Runtime.CompilerServices.Extension()>
    <CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification:="<Pending>")>
    Public Function AddEnvironmentVariablesROConfigurationSource(ByVal configurationBuilder As IConfigurationBuilder, sPrefix As String, oDict As System.Collections.IDictionary) As IConfigurationBuilder
        configurationBuilder.Add(New EnvironmentVariablesROConfigurationSource(sPrefix, oDict))
        Return configurationBuilder
    End Function

    <Obsolete("to może nie działać!")>
    <Runtime.CompilerServices.Extension()>
    Public Function SelectSingleNode(ByVal oNode As Xml.XmlNode, sNodeName As String) As Xml.XmlNode
        Dim oElement As Xml.XmlElement = TryCast(oNode, Xml.XmlElement)
        If oElement Is Nothing Then Return Nothing

        Dim oListEls As Xml.XmlNodeList = oElement.GetElementsByTagName(sNodeName)
        If oListEls.Count < 1 Then Return Nothing
        Return oListEls(0)
    End Function

End Module



#End Region


#Region "podstawalist"

''' <summary>
''' klasa bazowa dla moich list
''' </summary>
''' <typeparam name="TYP"></typeparam>
Public Class MojaLista(Of TYP)
    Protected _lista As List(Of TYP)
    Private _filename As String

    Public Sub New(sFolder As String, Optional sFileName As String = "items.json")
        DumpCurrMethod($"sFolder={sFolder}, sFile={sFileName}")

        If String.IsNullOrWhiteSpace(sFolder) OrElse String.IsNullOrWhiteSpace(sFileName) Then
            DebugOut(0, "FAIL MojaLista.New z pustym parametrem!")
            Throw New ArgumentException("musi być i folder i filename podane")
        End If
        _lista = New List(Of TYP)
        _filename = IO.Path.Combine(sFolder, sFileName)
    End Sub

    Public Function Load() As Boolean
        DumpCurrMethod()

        Dim sTxt As String = ""
        If IO.File.Exists(_filename) Then
            sTxt = IO.File.ReadAllText(_filename)
        End If

        If sTxt Is Nothing OrElse sTxt.Length < 5 Then
            Clear()
            Return False
        End If

        _lista = Newtonsoft.Json.JsonConvert.DeserializeObject(sTxt, GetType(List(Of TYP)))
        Return True
    End Function

    Public Function Save() As Boolean
        DumpCurrMethod()

        If _lista Is Nothing Then
            DumpMessage("glItems null")
            Return False
        End If
        If _lista.Count < 1 Then
            DumpMessage("glItems.count<1")
            Return False
        End If

        Dim sTxt As String = Newtonsoft.Json.JsonConvert.SerializeObject(_lista, Newtonsoft.Json.Formatting.Indented)
        IO.File.WriteAllText(_filename, sTxt)

        Return True
    End Function

    Public Function GetFileDate() As Date
        If IO.File.Exists(_filename) Then
            Return IO.File.GetLastWriteTime(_filename)
        Else
            Return New Date(1970, 1, 1)
        End If
    End Function

    Public Function IsObsolete(iDays As Integer)
        If GetFileDate.AddDays(iDays) < Date.Now Then Return True
        Return False
    End Function

    Public Function GetList() As List(Of TYP)
        Return _lista
    End Function

    Public Function Count() As Integer
        Return _lista.Count
    End Function

    Public Sub Clear()
        _lista.Clear()
    End Sub

    Public Sub Add(oNew As TYP)
        _lista.Add(oNew)
    End Sub

    Public Sub Remove(oDel As TYP)
        _lista.Remove(oDel)
    End Sub

    ''' <summary>
    '''  Znajduje itemkę wedle funkcji, jak przykład
    '''  Find(Function(x) x.PartName.Contains("seat"))
    ''' </summary>
    ''' <param name="match"></param>
    ''' <returns></returns>
    Public Function Find(match As Predicate(Of TYP)) As TYP
        Return _lista.Find(match)
    End Function

    ''' <summary>
    '''  Usuwa itemkę wedle funkcji, jak przykład
    '''  Remove(Function(x) x.PartName.Contains("seat"))
    '''  (zabezpieczone przed nieznalezieniem)
    ''' </summary>
    ''' <param name="match"></param>
    Public Sub Remove(match As Predicate(Of TYP))
        Dim oItem As TYP = Find(match)
        If oItem Is Nothing Then Return
        _lista.Remove(oItem)
    End Sub

#If False Then
    Public Function Find(iID As Integer) As TYP
        Dim t As Type = TYP.GetType
        For Each oItem In _lista
            For Each oItem.GetType.
        Next
    End Function

    Public Function Find(sID As String) As TYP
        For Each oItem In _lista

        Next
    End Function
#End If

End Class

#End Region

#Region "Podstawa typów"
Public MustInherit Class MojaStruct

    Public Function DumpAsJSON() As String
        Return Newtonsoft.Json.JsonConvert.SerializeObject(Me, Newtonsoft.Json.Formatting.Indented)
    End Function

    Public Function DumpAsText() As String
        Dim oTypek As Type = Me.GetType
        Dim sTxt As String = Me.ToString & ":" & vbCrLf

        For Each oProp As PropertyInfo In oTypek.GetRuntimeProperties
            sTxt = sTxt & oProp.Name & ":" & vbTab
            If oProp.GetValue(Me) Is Nothing Then
                sTxt &= " (null)"
            Else
                sTxt &= oProp.GetValue(Me).ToString
            End If
        Next

        Return sTxt
    End Function
End Class
#End Region

#Enable Warning CA2007 'Consider calling ConfigureAwait On the awaited task
#Enable Warning IDE0079 ' Remove unnecessary suppression

#Region "GPS"
''' <summary>
''' kopia Windows.Devices.Geolocation.BasicGeoposition, bo nic takiego nie ma w .Net
''' </summary>
Public Class MyBasicGeoposition
    Public Altitude As Double
    Public Latitude As Double
    Public Longitude As Double

    Public Sub New(lat As Double, lon As Double, alt As Double)
        Altitude = alt
        Longitude = lon
        Latitude = lat
    End Sub
    Public Sub New(lat As Double, lon As Double)
        Altitude = 0
        Longitude = lon
        Latitude = lat
    End Sub

    Public Function DistanceTo(dLatitude As Double, dLongitude As Double) As Double
        Dim num1 As Integer

        Try
            Dim iRadix = 6371000
            Dim tLat = (dLatitude - Latitude) * Math.PI / 180.0
            Dim tLon = (dLongitude - Longitude) * Math.PI / 180.0
            Dim a = 2.0 * Math.Asin(Math.Min(1.0, Math.Sqrt(Math.Sin(tLat / 2.0) *
                Math.Sin(tLat / 2.0) + Math.Cos(Math.PI / 180.0 * Latitude) * Math.Cos(Math.PI / 180.0 * dLatitude) *
                Math.Sin(tLon / 2.0) * Math.Sin(tLon / 2.0))))
            Return iRadix * a
        Catch ex As Exception
            Return 0
        End Try

        Return num1
    End Function


    Public Function DistanceTo(oGeocoord As MyBasicGeoposition) As Double
        Return DistanceTo(oGeocoord.Latitude, oGeocoord.Longitude)
    End Function



    Public Function IsInsidePoland() As Boolean
        ' https//pl.wikipedia.org/wiki/Geometryczny_%C5%9Brodek_Polski

        Dim dOdl As Double = DistanceTo(New MyBasicGeoposition(52.2159333, 19.1344222))
        If dOdl / 1000 > 500 Then Return False
        Return True    ' ale To nie jest pewne, tylko: "możliwe"
    End Function

    Public Shared Function GetDomekGeopos(Optional iDecimalDigits As UInteger = 0) As MyBasicGeoposition
        Dim iDigits As Integer = iDecimalDigits
        If iDigits > 5 Then iDigits = 0

        Return New MyBasicGeoposition(Math.Round(50.01985, iDigits), Math.Round(19.97872, iDigits))
    End Function

    Public Shared Function GetKrakowGeopos() As MyBasicGeoposition
        Return New MyBasicGeoposition(50.06138, 19.93833)
    End Function

End Class

#End Region
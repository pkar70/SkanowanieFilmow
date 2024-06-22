

Imports pkar
Imports Vblib
Imports pkar.UI.Extensions
Imports System.Windows.Interop

Class MainWindow
    Inherits Window

    ''' <summary>
    ''' Sprawdzenie czy już nie działa, jak tak to upewnia się że ma być druga instancja, jeśli nie - zamyka app
    ''' </summary>
    Private Async Function CheckIfRunning() As Task
        Dim currentProcess As Process = Process.GetCurrentProcess()

        Dim runningProcess As Process = (From process In Process.GetProcessesByName(currentProcess.ProcessName)
                                         Where
                                    process.Id <> currentProcess.Id
                                         Select process).FirstOrDefault()

        If runningProcess Is Nothing Then Return

        ' mamy poprzednią instancję
        If Await Me.DialogBoxYNAsync("To byłoby drugie uruchomienie app, zamknąć się?") Then
            ' ewentualnie ShowWindow(runningProcess.MainWindowHandle, SW_SHOWMAXIMIZED)
            ' przy [DllImport("user32.dll")]
            ' Private Static extern Boolean ShowWindow(IntPtr hWnd, Int32 nCmdShow);

            Application.Current.Shutdown()
            Return
        End If

    End Function


    Private Async Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        InitLib(Nothing)
        Page_Loaded(Nothing, Nothing)    ' tak prościej, bo wklejam tu zawartość dawnego Page

        lib14_httpClnt.httpKlient._machineName = Environment.MachineName    ' musi być tak, bo lib jest też używana w std 1.4, a tam nie ma machinename

        ' https://stackoverflow.com/questions/50858209/system-notsupportedexception-no-data-is-available-for-encoding-1252
        ' z nugetem System.Text.Encoding.CodePages
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance)

        ' narzucona nazwa instancji (warning)
        Dim appname As String = GetSettingsString("name")
        If Not String.IsNullOrEmpty(appname) Then
            Me.MsgBox("Narzucona nazwa instancji:" & vbCrLf & appname)
        End If


        ' gdy nie ma cmd line, kończymy
        If String.IsNullOrEmpty(Environment.CommandLine) Then Return

        ' specjalne uruchomienia
        Dim argsy As String() = Environment.CommandLine.Split(" ")
        If argsy.Count < 2 Then Return


        Select Case argsy(1).ToLowerInvariant
            Case "expl"
                If argsy.Count <> 3 Then
                    Console.WriteLine("No param for 'expl'")
                    Return
                End If

                Dim folder As String = CmdLineGetFolder(argsy(2))
                Dim apka As New Process()
                apka.StartInfo.UseShellExecute = True
                apka.StartInfo.FileName = folder
                apka.Start()
                Window_Closing(Nothing, Nothing)
            'Case "cd"
            '    If argsy.Count <> 3 Then
            '        Console.WriteLine("No param for 'cd'")
            '        Return
            '    End If

            '    Dim folder As String = CmdLineGetFolder(argsy(2))
            '    If String.IsNullOrEmpty(folder) Then Return

            '    Environment.CurrentDirectory = folder
            '    Window_Closing(Nothing, Nothing)
            Case "ntp"
                If argsy.Count <> 3 Then
                    Console.WriteLine("No param for 'ntp'")
                    Return
                End If

                Dim plik As String = Application.GetDataFile("", argsy(2) & ".json")

                If IO.File.Exists(plik) Then
                    Process.Start("notepad", plik)
                    Window_Closing(Nothing, Nothing)
                Else
                    Console.WriteLine("Nonexistent file for notepad")
                End If


            Case "tool"
                If argsy.Count <> 4 Then
                    Console.WriteLine("No params for 'tools'")
                    Return
                End If

                If Await CmdLineRunTool(argsy(2), argsy(3)) Then Window_Closing(Nothing, Nothing)

                ' case "retrieve" SOURCE
                ' case "autotag" TAGGER
                ' case tool weather|moon|astro DATA (z tego potem pogoda BN/Wlknoc do sprawdzenia ;) ) dane dla Krakowa
                ' case tool ocr|face|azure|exif|fullExif FILE
        End Select
    End Sub


#Region "commandline"

    Private Function CmdLineGetFolder(param As String) As String
        Select Case param.ToLowerInvariant
            Case "data"
                Return Application.GetDataFolder(False)
            Case "buff"
                Return Vblib.GetSettingsString("uiFolderBuffer")
        End Select
        Return ""
    End Function

    ''' <summary>
    ''' uruchom TOOL toolName z toolParam, zwróć FALSE gdy nieudane (i normalny start app) lub TRUE gdy ma być app zamykana
    ''' </summary>
    ''' <param name="toolName">nazwa toola</param>
    ''' <param name="toolParam">parametr dla toola</param>
    ''' <returns></returns>
    Private Async Function CmdLineRunTool(toolName As String, toolParam As String) As Task(Of Boolean)

        Dim retVal As String = ""

        Select Case toolName.ToLowerInvariant
            Case "weather", "moon", "astro"
                retVal = "For " & toolParam & vbCrLf & Await CmdLineRunToolData("AUTO_" & toolName, toolParam)
            Case "winocr", "winface", "azure", "exif", "fullExif"
                retVal = "For " & toolParam & vbCrLf & Await CmdLineRunToolFile("AUTO_" & toolName, toolParam)
        End Select

        If String.IsNullOrEmpty(retVal) Then Return False

        Dim tempfile As String = IO.Path.GetTempFileName
        IO.File.WriteAllText(tempfile, retVal)
        Process.Start("notepad", tempfile)

        Return True
    End Function

    Private Async Function CmdLineRunToolFile(toolName As String, toolParam As String) As Task(Of String)

        If Not IO.File.Exists(toolParam) Then
            Console.WriteLine("Non existent file (for tool)")
            Return ""
        End If

        For Each tool In Application.gAutoTagery
            If tool.Nazwa.EqualsCIAI(toolName) Then
                Dim picek As New Vblib.OnePic
                picek.InBufferPathName = toolParam

                Dim exiftag As Vblib.ExifTag = Await tool.GetForFile(picek)
                If exiftag Is Nothing Then
                    Console.WriteLine("Tool returns NULL")
                    Return ""
                Else
                    Return exiftag.DumpAsJSON(True)
                End If
            End If
        Next

        Console.WriteLine("Nie znam takiego toola")
        Return ""
    End Function

    Private Async Function CmdLineRunToolData(toolName As String, toolParam As String) As Task(Of String)

        Dim data As Date
        If Not Date.TryParseExact(toolParam, "yyyy.MM.dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AllowWhiteSpaces, data) Then
            Console.WriteLine("Chyba zły format daty - ma być yyyy.MM.dd")
            Return ""
        End If

        For Each tool In Application.gAutoTagery
            If tool.Nazwa.EqualsCIAI(toolName) Then
                Dim picek As New Vblib.OnePic

                ' date z parametru, oraz dla Krakowa
                Dim exif As New Vblib.ExifTag(Vblib.ExifSource.FileExif)
                exif.DateTimeOriginal = data.ToExifString
                exif.GeoTag = BasicGeopos.GetKrakowCenter
                picek.ReplaceOrAddExif(exif)

                Dim exiftag As Vblib.ExifTag = Await tool.GetForFile(picek)
                If exiftag Is Nothing Then
                    Console.WriteLine("Tool returns NULL")
                    Return ""
                Else
                    Return exiftag.DumpAsJSON(True)
                End If
            End If
        Next

        Console.WriteLine("Nie znam takiego toola")
        Return ""

        ' case tool weather|moon|astro DATA (z tego potem pogoda BN/Wlknoc do sprawdzenia ;) ) dane dla Krakowa
        ' case tool ocr|face|azure|exif|fullExif FILE
    End Function
#End Region


#Region "zamykanie i ikonka"

    Private Async Sub Window_Closing(sender As Object, e As ComponentModel.CancelEventArgs)

        If Vblib.GetSettingsBool("uiServerEnabled") AndAlso Application.gWcfServer IsNot Nothing Then
            ' *TODO* YNCancel, zamknąć, zikonizować, cancel
            Dim msg As String = "Program działa jako serwer"
            Dim lastaccess As String = Application.gWcfServer._lastNetAccess.GetString
            If Not lastaccess.StartsWithCI("No") Then
                msg += " " & lastaccess
            End If

            If Not Await Me.DialogBoxYNAsync(msg & ", zamknąć go?") Then
                If e IsNot Nothing Then e.Cancel = True
                Return
            End If
            Application.gWcfServer?.StopSvc()
        End If

        'If Application.Current.Windows.Count > 2 Then
        '    If Not Await Vblib.DialogBoxYNAsync("Zamknąć program?") Then Return
        ' bez tego zamyka tylko to jedno okno, a reszty już NIE
        Application.Current.Shutdown()
        'End If

    End Sub

    Private Async Sub Window_StateChanged(sender As Object, e As EventArgs)
        ' https://www.codeproject.com/Articles/36468/WPF-NotifyIcon-2

        ' podmieniamy na ikonke tylko gdy jest jedno okno - inaczej zwykła miniaturyzacja
        If Application.Current.Windows.Count > 1 Then Return
        If Me.WindowState <> WindowState.Minimized Then Return
        If Not Await Me.DialogBoxYNAsync("Zamknąć do SysTray?") Then Return

        IkonkujMnie()
    End Sub

    Private Sub IkonkujMnie()

        If Me.Visibility <> Visibility.Visible Then Return

        If myNotifyIcon.Visibility <> Visibility.Visible Then

            Dim ikonka As System.Drawing.Icon = Nothing

            Try
                ikonka = New System.Drawing.Icon("icons/trayIcon1.ico")
            Catch ex As Exception

            End Try

            If ikonka Is Nothing Then
                Dim localIcon As String = IO.Path.Combine(Application.GetDataFolder, "trayIcon1.ico")
                If Not IO.File.Exists(localIcon) Then
                    ' ściągnij z mojego serwera?
                End If

                Try
                    ikonka = New System.Drawing.Icon(localIcon)
                Catch ex As Exception

                End Try
            End If

            Try
                myNotifyIcon.Visibility = Visibility.Visible
                If myNotifyIcon.Icon Is Nothing Then
                    myNotifyIcon.Icon = ikonka
                    myNotifyIcon.ContextMenu = CreateIconContextMenu()
                End If

            Catch ex As Exception
                Me.MsgBox("Chciałem się przełączyć do ikonki, ale: " & vbCrLf & ex.Message)
            End Try
        End If

        Me.Hide()

    End Sub

    Private Sub JeslimSamToPokaz()
        If Application.Current.Windows.Count > 2 Then Return
        Me.Show()
    End Sub

    Private _ctxSettings As MenuItem

    Private Function CreateIconContextMenu() As ContextMenu

        Dim ctxMenu As New ContextMenu
        ctxMenu.Items.Add(CreateMenuItem("Process", "Porządkowanie zdjęć", AddressOf uiProcess_Click))

        Dim archMI As New MenuItem With {.Header = "Archiwum", .ToolTip = "obsługa archiwum"}
        archMI.Items.Add(CreateMenuItem("Browse", "Przeglądanie archiwum wg katalogów", AddressOf uiBrowseArch_Click))
        archMI.Items.Add(CreateMenuItem("Search", "Wyszukiwanie w archiwum", AddressOf uiSearch_Click))
        archMI.Items.Add(CreateMenuItem("Statistics", "Statystyki zdjęć w archiwum", AddressOf uiStats_Click))
        archMI.Items.Add(New Separator)
        archMI.Items.Add(CreateMenuItem("Status", "", AddressOf ArchStatus_Click))
        ' * freemem - zwolnienie Archive.OnePic
        ctxMenu.Items.Add(archMI)

        If Vblib.GetSettingsBool("uiServerEnabled") Then
            Dim srvMI As New MenuItem With {.Header = "Server", .ToolTip = "PicSort jako serwer"}
            srvMI.Items.Add(CreateMenuItem("start/stop", "Uruchamianie/zatrzymywanie serwera", AddressOf SrvStartStop_Click))
            srvMI.Items.Add(CreateMenuItem("status", "Ostatnie połączenia", AddressOf SrvLastLog_Click))
            ctxMenu.Items.Add(srvMI)
        End If


        _ctxSettings = CreateMenuItem("Settings", "Ustawienia", AddressOf uiSettings_Click)
        ctxMenu.Items.Add(_ctxSettings)

        ctxMenu.Items.Add(CreateMenuItem("About", "O programie", AddressOf About_Click))

        Return ctxMenu
    End Function

    Private Async Sub SrvStartStop_Click(sender As Object, e As RoutedEventArgs)
        If Application.gWcfServer Is Nothing Then
            If Not Await Me.DialogBoxYNAsync("Uruchomić serwer?") Then Return
            SettingsShare.StartServicing()
        Else
            If Not Await Me.DialogBoxYNAsync("Zatrzymać serwer?") Then Return
            Application.gWcfServer.StopSvc()
            Application.gWcfServer = Nothing
        End If
    End Sub

    Private Sub About_Click(sender As Object, e As RoutedEventArgs)
        Me.MsgBox(GetAppVers() & " (" & BUILD_TIMESTAMP & ")")
    End Sub

    Private Sub SrvLastLog_Click(sender As Object, e As RoutedEventArgs)
        ' *TODO* ma być pełny log pokazywany
        Me.MsgBox(Application.gWcfServer._lastNetAccess.GetString)
    End Sub

    Private Sub ArchStatus_Click(sender As Object, e As RoutedEventArgs)
        ' *TODO* rozmiar pamięci, oraz możliwość zwolnienia jej
        If Application.gDbase.IsLoaded Then
            Me.MsgBox($"Loaded, {Application.gDbase.Count} items")
        Else
            Me.MsgBox("Nie mam bazy wczytanej")
        End If
    End Sub

    Private Function CreateMenuItem(hdr As String, dymek As String, handlerek As RoutedEventHandler) As MenuItem
        Dim oNew As New MenuItem With {.Header = hdr, .ToolTip = dymek}
        If handlerek IsNot Nothing Then AddHandler oNew.Click, handlerek
        Return oNew
    End Function

    Private Sub uiTrayIcon_DoubleClick(sender As Object, e As RoutedEventArgs)
        Show()
        Me.WindowState = WindowState.Normal
        'SystemCommands.RestoreWindow(Me)
        myNotifyIcon.Visibility = Visibility.Collapsed
    End Sub
#End Region

    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        Me.InitDialogs

        Await CheckIfRunning()

        ' jednak podstawowy folder musi istnieć, sami go sobie stworzymy jakby co.
        If Vblib.GetSettingsString("uiFolderBuffer") = "" Then
            Me.MsgBox("Nie ma ustawień, konieczne Settings")
            uiSettings_Click(Nothing, Nothing)
            Vblib.SetSettingsInt("uiMaxThumbs", CalculateMaxThumbCount)
        End If

        ' guzik Retrieve wyłączany jak nie ma zdefiniowanych sources
        ' uiRetrieve.IsEnabled = IO.File.Exists(App.GetDataFile("", "sources.json", False))

        ' *TODO* guziki pozostałe wyłączane jak nie ma LocalStorage

        Dim count As Integer = Application.GetBuffer.Count
        uiProcess.Content = $"Process ({count})"
        If count = 0 Then
            Dim path As String = IO.Path.Combine(Application.GetDataFolder, "buffer.json")
            If IO.File.Exists(path) Then
                If New IO.FileInfo(path).Length > 5 Then
                    Dim lista As List(Of Vblib.OnePic)
                    Dim sTxt = IO.File.ReadAllText(path)
                    Dim sErr As String = ""
                    Try
                        lista = Newtonsoft.Json.JsonConvert.DeserializeObject(sTxt, GetType(ObservableList(Of Vblib.OnePic)))
                    Catch ex As Exception
                        sErr = ex.Message
                    End Try

                    If sErr <> "" Then Me.MsgBox("Coś dziwnego, chyba błąd w pliku bufffer.json?" & vbCrLf & sErr)
                    Debug.WriteLine(sErr)
                    Dim iInd As Integer = sErr.IndexOf(", line ")
                    If iInd > 0 Then
                        sErr = sErr.Substring(iInd + ", line ".Length)
                        iInd = sErr.IndexOf(",")
                        If iInd > 0 AndAlso iInd < 10 Then
                            sErr = sErr.Substring(0, iInd).Trim
                        End If
                    End If
                    sErr.SendToClipboard
                End If
            End If
        End If


        uiBrowseArch.IsEnabled = IsAnyArchPresent()

        ' zablokowane, do czasu poprawienia
        If Vblib.GetSettingsBool("uiServerEnabled") Then SettingsShare.StartServicing()
        ' na tej linii ERROR że nie może znaleźć System.ServiceModel v4.0.0, debugger nie widzi wejścia do StartSvc
        'Application.gWcfServer.StartSvc()

        uiVers.Text = GetAppVers() & " (" & BUILD_TIMESTAMP & ")"

        'PoprawReelFromTarget
        'UsunAstroMoon
        'Album_dw_04()
    End Sub


#Region "poprawianie plików"

#If False Then

    Private Sub Album_dw_04()

        For Each oFile In Application.GetBuffer.GetList
            If oFile.sSuggestedFilename.StartsWithCI("dW_alb04") Then
                Dim oExif As Vblib.ExifTag = oFile.GetExifOfType(Vblib.ExifSource.SourceDefault)
                oExif.DateMax = New Date(1945, 1, 1)
            End If
        Next
        Application.GetBuffer.SaveData()

    End Sub


    Private Sub UsunAstroMoon()

        For Each oFile In Application.GetBuffer.GetList
            oFile.RemoveExifOfType(Vblib.ExifSource.AutoMoon)
            oFile.RemoveExifOfType(Vblib.ExifSource.AutoAstro)
        Next
        Application.GetBuffer.SaveData()

    End Sub


    Private Sub PoprawReelFromTarget()

        For Each oFile In Application.GetBuffer.GetList

            Dim oExif As Vblib.ExifTag = oFile.GetExifOfType(Vblib.ExifSource.SourceDefault)
            If oExif Is Nothing Then Continue For

            oExif.ReelName = oFile.TargetDir.Replace("reel\", "").Replace("\", "_")

        Next
        Application.GetBuffer.SaveData()

    End Sub

    Private Sub ArchRemoveAzureUserComment()
        Dim arch As New BaseList(Of Vblib.OnePic)("C:\Users\pkar\AppData\Local\PicSorter", "archIndexFull.json")
        arch.Load()

        For Each oPic As Vblib.OnePic In arch
            Dim oAzure As Vblib.ExifTag = oPic.GetExifOfType(Vblib.ExifSource.AutoAzure)
            If oAzure IsNot Nothing Then oAzure.UserComment = Nothing
        Next

        arch.Save(True)

    End Sub


    Private Sub UsunDefaultyArch()
        Dim arch As New BaseList(Of Vblib.OnePic)("C:\Users\pkar\AppData\Local\PicSorter", "archIndexFull.json")
        arch.Load()
        arch.Save(True)
    End Sub

    Private Sub DodajSerNoDoArchLocal()
        Dim arch As New BaseList(Of Vblib.OnePic)("C:\Users\pkar\AppData\Local\PicSorter", "archIndexFull.json")
        arch.Load()

        Dim iSerNo As Integer = 0

        arch.ForEach(Sub(x)
                         iSerNo += 1
                         x.serno = iSerNo
                         x.RemoveExifOfType(Vblib.ExifSource.AutoGuid)
                     End Sub
            )

        arch.Save(true)


        For Each oFile In Application.GetBuffer.GetList
            iSerNo += 1
            oFile.serno = iSerNo
        Next
        Application.GetBuffer.SaveData()

        Vblib.SetSettingsInt("lastSerNo", iSerNo)

    End Sub


    Private Sub Popraw20240411()
        ' poprawiam, bo zapomniałem przed archiwizacją

        Dim arch As New BaseList(Of Vblib.OnePic)("C:\Users\pkar\AppData\Local\PicSorter", "archIndexFull.json")
        arch.Load()

        If arch.Count < 1 Then
            Me.MsgBox("Niby władowałem, ale zero?")
            Return
        End If

        For Each oItem As Vblib.OnePic In arch
            If Not oItem.TargetDir.Contains("PAT\Ludziki") Then Continue For

            Dim oExif As Vblib.ExifTag = oItem.GetExifOfType(Vblib.ExifSource.SourceDefault)
            oExif.FileSourceDeviceType = FileSourceDeviceTypeEnum.scannerReflex

            Dim oExifDates = New ExifTag(ExifSource.ManualDate)
            If oItem.TargetDir.Contains("Karolina") Then
                ' wczesna szkoła: x_3436bfe2.jpg x_4336be00.jpg x_577082c9.jpg x_6742f1c1.jpg x_ac3f7d29.jpg x_de616f29.jpg
                If oItem.InBufferPathName.Contains("x_3436bfe2.jpg") OrElse
                        oItem.InBufferPathName.Contains("x_4336be00.jpg") OrElse
                        oItem.InBufferPathName.Contains("x_577082c9.jpg") OrElse
                        oItem.InBufferPathName.Contains("x_6742f1c1.jpg") OrElse
                        oItem.InBufferPathName.Contains("x_ac3f7d29.jpg") OrElse
                        oItem.InBufferPathName.Contains("x_de616f29.jpg") Then
                    oExifDates.DateMin = New Date(1995, 1, 1)
                    oExifDates.DateMax = New Date(2005, 1, 1)
                Else
                    oExifDates.DateMin = New Date(2005, 1, 1)
                    oExifDates.DateMax = New Date(2011, 1, 1)
                End If
            ElseIf oItem.TargetDir.Contains("Sylwia") Then
                If oItem.InBufferPathName.Contains("DSCF") Then
                    Continue For
                Else
                    oExifDates.DateMin = New Date(2005, 1, 1)
                    oExifDates.DateMax = New Date(2011, 1, 1)
                End If
            Else
                oExifDates.DateMin = New Date(2007, 1, 1)
                oExifDates.DateMax = New Date(2011, 1, 1)
            End If

            oItem.ReplaceOrAddExif(oExifDates)

            Dim guid As String = oItem.GetSuggestedGuid
            oItem.PicGuid = guid
            oExif = New ExifTag(Vblib.ExifSource.AutoGuid)
            oExif.PicGuid = guid
        Next

        arch.Save(True)
        Me.MsgBox("Popraw koniec pliku:" & vbCrLf & """PicGuid"": ""t20110812131125""" & vbCrLf & "}")

    End Sub


    Private Sub DodajKrecika()
        For Each oFile In Application.GetBuffer.GetList
            If Not oFile.sSuggestedFilename.StartsWith("SSA") Then Continue For

            Dim oExif As Vblib.ExifTag = oFile.GetExifOfType(Vblib.ExifSource.FileExif)
            If oExif Is Nothing Then Continue For

            oExif.Author = "Łukasz Kluba"
            oExif.Copyright = "(C) Łukasz Kluba, All rights reserved."

        Next
        Application.GetBuffer.SaveData()
    End Sub

    Private Sub PoprawAutora()

        For Each oFile In Application.GetBuffer.GetList

            Dim oExif As Vblib.ExifTag = oFile.GetExifOfType(Vblib.ExifSource.SourceDefault)
            If oExif Is Nothing Then Continue For

            If Not String.IsNullOrWhiteSpace(oExif.Author) Then Continue For

            If oFile.sInSourceID.ContainsCI("MagdaMieszk") Then
                oExif.Author = "Magdalena Zgadzaj"
                oExif.Copyright = "(C) Magdalena Zgadzaj, All rights reserved."
            ElseIf oFile.sInSourceID.ContainsCI("AutorJA") Then
                oExif.Author = "Piotr Karocki"
                oExif.Copyright = "(C) Piotr Karocki, All rights reserved."
            ElseIf oFile.sInSourceID.ContainsCI("AutorEwelina") Then
                oExif.Author = "Ewelina Michalik"
                oExif.Copyright = "(C) Ewelina Michalik, All rights reserved."
            End If

        Next


        Application.GetBuffer.SaveData()
    End Sub

    Private Sub PoprawAutora1()

        For Each oFile In Application.GetBuffer.GetList

            Dim oExif As Vblib.ExifTag = oFile.GetExifOfType(Vblib.ExifSource.SourceDefault)
            If oExif Is Nothing Then Continue For

            'If Not String.IsNullOrWhiteSpace(oExif.Author) Then Continue For

            If oFile.sInSourceID.ContainsCI("AutorJA") Then
                'oExif.Author = "Piotr Karocki"
                'oExif.Copyright = "(C) Piotr Karocki, All rights reserved."

                If oFile.sInSourceID.Contains("\Mio\") Then oExif.CameraModel = "Mio A701"

                'ElseIf oFile.sInSourceID.ContainsCI("AutorEwelina") Then
                '    oExif.Author = "Ewelina Michalik"
                '    oExif.Copyright = "(C) Ewelina Michalik, All rights reserved."

            ElseIf oFile.sInSourceID.ContainsCI("AutorInni") Then
                oExif.Author = ""
                oExif.Copyright = "(C) Piotr Karocki, All rights reserved."
            End If

        Next


        Application.GetBuffer.SaveData()
    End Sub



    Private Sub PoprawkiArchUniq()
        ' zrobienie pliku archiwum uniq (bo są w nim powtórki?)
        Dim lista As List(Of Vblib.OnePic)
        Dim sTxt = IO.File.ReadAllText(Application.GetDataFile("", "archIndexFull.json"))
        sTxt &= "]"
        Dim sErr As String = ""
        Try
            lista = Newtonsoft.Json.JsonConvert.DeserializeObject(sTxt, GetType(ObservableList(Of Vblib.OnePic)))
        Catch ex As Exception
            sErr = ex.Message
        End Try

        If sErr <> "" Then
            Vblib.DialogBox(sErr)
            Return
        End If

        Dim listaNew As New List(Of Vblib.OnePic)
        Dim cntDublet As Integer = 0
        Dim cntUniq As Integer = 0
        Dim cntNull As Integer = 0

        For Each oPic As Vblib.OnePic In lista
            If oPic Is Nothing Then
                cntNull += 1
                Continue For
            End If
            Dim bJuzMam As Boolean = False
            For Each oPicNew As Vblib.OnePic In listaNew
                If oPic.sSuggestedFilename = oPicNew.sSuggestedFilename AndAlso
                oPic.TargetDir = oPicNew.TargetDir Then
                    bJuzMam = True
                    Exit For
                End If
            Next

            If bJuzMam Then
                Debug.WriteLine($"DUBLET {oPic.sSuggestedFilename} in {oPic.TargetDir}")
                cntDublet += 1
            Else
                Debug.WriteLine($"NEW {oPic.sSuggestedFilename} in {oPic.TargetDir}")
                listaNew.Add(oPic)
                cntUniq += 1
            End If

        Next

        Dim oSerSet As New Newtonsoft.Json.JsonSerializerSettings With {.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore, .DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Ignore}
        Dim sTxtNew As String = Newtonsoft.Json.JsonConvert.SerializeObject(listaNew, Newtonsoft.Json.Formatting.Indented, oSerSet)
        IO.File.WriteAllText(Application.GetDataFile("", "archIndexFullNew.json"), sTxtNew)

        Debug.WriteLine($"SUMMARY: uniq {cntUniq}, dublets {cntDublet}, nulls {cntNull}")

    End Sub

    Private Sub PoprawkiArchindex20230918()
        Dim pliczek As New BaseList(Of Vblib.OnePic)(Application.GetDataFolder, "archIndexFull.json")
        pliczek.Load()
        Debug.WriteLine("Old count: " & pliczek.Count)

        Dim pliczek1 As New BaseList(Of Vblib.OnePic)(Application.GetDataFolder, "archIndexFullNew.json")

        Dim _uniqId As New UniqID(Application.GetDataFolder)
        Dim noGuidCnt As Integer = 0
        Dim noTargetDirCnt As Integer = 0

        For Each oPic As Vblib.OnePic In pliczek
            ' usunac te, ktore maja targetdir null
            If oPic Is Nothing Then Continue For
            If String.IsNullOrWhiteSpace(oPic.TargetDir) Then
                Debug.WriteLine("Null targetdir in " & oPic.sSuggestedFilename)
                noTargetDirCnt += 1
                Continue For
            End If

            ' picguidy zrobic tam gdzie nie ma
            If String.IsNullOrWhiteSpace(oPic.PicGuid) Then
                ' dorobienie, "ExifSource": "AUTO_GUID", oraz w oPic
                noGuidCnt += 1
                _uniqId.SetGUIDforPic(oPic)
                If Not String.IsNullOrWhiteSpace(oPic.PicGuid) Then

                    Dim oExif As New ExifTag(ExifSource.AutoGuid)
                    oExif.PicGuid = oPic.PicGuid
                    oPic.ReplaceOrAddExif(oExif)
                Else
                    Debug.WriteLine("Nie umiem zrobic guid dla " & oPic.sSuggestedFilename)
                End If
            End If

            ' przetworzyc keywords na uniq (się powtarzają), zapewnic by teraz było tylko raz
            ' zabrać ManualTag, w nim jest:
            '"ExifSource" "MANUAL_TAG",
            '"DateMin": "1970-05-23T00:00:00",
            '"Keywords": " -JA",
            '"UserComment": " | ja"
            Dim oExifKwdOld As ExifTag = oPic.GetExifOfType("MANUAL_TAG")
            If oExifKwdOld?.Keywords IsNot Nothing Then
                Dim oExifKwdNew As ExifTag = New ExifTag("MANUAL_TAG")
                oExifKwdNew.DateMax = oExifKwdOld.DateMax
                oExifKwdNew.DateMin = oExifKwdOld.DateMin
                oExifKwdNew.Keywords = ""

                Dim akwds As String() = oExifKwdOld.Keywords.Split(" ")
                For Each kwd As String In akwds
                    kwd = kwd.Trim
                    If kwd.Length < 2 Then Continue For

                    If Not oExifKwdNew.Keywords.Contains(kwd) Then
                        oExifKwdNew.Keywords &= " " & kwd
                        Dim oKwd As OneKeyword = Application.GetKeywords.GetKeyword(kwd)
                        If oKwd IsNot Nothing Then
                            oExifKwdNew.UserComment &= oKwd.sDisplayName & " | "
                        End If
                    End If
                Next
            End If

            pliczek1.Add(oPic)
        Next

        Debug.WriteLine("Plikow z pustym GUID bylo " & noGuidCnt)
        Debug.WriteLine("Plikow bez targetdir bylo " & noTargetDirCnt)
        Debug.WriteLine("Nowy indeks ma plikow " & pliczek1.Count)

        pliczek1.Save(True)
        Me.MsgBox("Zrobilem nowy indeks, ktory powinien byc juz poprawny")
    End Sub

#End If

#End Region

    ''' <summary>
    '''  to miało ustalać MaxThumbs z ilości RAM, ale ponieważ to nie działa, to na sztywno 1000
    ''' </summary>
    ''' <returns></returns>
    Private Shared Function CalculateMaxThumbCount() As Integer
        ' policz ile zdjęć można mieć
        ' zakładam jeden thumb to 500 KiB (tak zapisałem kiedyś w userGuide)

        ' ale to nie chce teraz działać :(
        ' Could not load type 'Microsoft.VisualBasic.Devices.ComputerInfo' from assembly 'Microsoft.VisualBasic, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'.

        Return 1000

        'Dim iGB As Integer = vblib46ram.GetGBram
        '' Dim iGB As Integer = vbStd2getram.GetRam.GetGBram
        'If iGB > 8 Then Return 1000 ' dla >8 GB, 1000 obrazków (500 MB)
        'If iGB > 4 Then Return 500 ' dla >4 GB, 500 obrazków (256 MB)

        Return 100

    End Function

#Region "guziki przejścia do stron/okien"

    Private Sub uiSettings_Click(sender As Object, e As RoutedEventArgs)
        IkonkujMnie()
        uiSettings.IsEnabled = False
        _ctxSettings.IsEnabled = False
        Dim oWnd As New SettingsWindow
        oWnd.Owner = Me
        oWnd.ShowDialog()
        uiSettings.IsEnabled = True
        _ctxSettings.IsEnabled = True
        JeslimSamToPokaz()
    End Sub

    Private Sub uiProcess_Click(sender As Object, e As RoutedEventArgs)
        IkonkujMnie()
        uiProcess.IsEnabled = False

        Dim oWnd As ProcessPic = Nothing

        For Each oWin In OwnedWindows
            oWnd = TryCast(oWin, ProcessPic)
            If oWnd IsNot Nothing Then Exit For
        Next

        If oWnd Is Nothing Then
            oWnd = New ProcessPic
            oWnd.Owner = Me
        End If
        oWnd.Show()
        oWnd.Activate()   ' bring to front
        AddHandler oWnd.Closed, AddressOf ZamykamPodokno
        uiProcess.IsEnabled = True
    End Sub

    Private Sub ZamykamPodokno(sender As Object, e As EventArgs)
        JeslimSamToPokaz()
    End Sub

    Private Sub uiBrowseArch_Click(sender As Object, e As RoutedEventArgs)
        PokazSubWindow(New SettingsDirTree(False))
    End Sub

    Private Sub uiSearch_Click(sender As Object, e As RoutedEventArgs)
        PokazSubWindow(New SearchWindow)
    End Sub

    Private Sub uiStats_Click(sender As Object, e As RoutedEventArgs)
        PokazSubWindow(New PokazStatystyke("", Nothing))
    End Sub

    Private Sub PokazSubWindow(oWnd As Window)
        IkonkujMnie()
        oWnd.Owner = Me
        AddHandler oWnd.Closed, AddressOf ZamykamPodokno

        oWnd.Show()
    End Sub

#End Region

    Private Shared Function IsAnyArchPresent() As Boolean
        Return Application.GetArchivesList.Exists(Function(x) x.IsPresent)
        'Dim anyPresent As Boolean = False
        'For Each oArch As lib_PicSource.LocalStorageMiddle In Application.GetArchivesList.Exists
        '    If oArch.IsPresent Then Return True
        'Next
        'Return False
    End Function


End Class



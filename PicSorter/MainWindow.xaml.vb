


'Imports Org.BouncyCastle.Asn1
Imports System.Net.Http.Json
'Imports Org.BouncyCastle.Utilities
Imports pkar
Imports Vblib

Class MainWindow
    Inherits Window


    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        InitLib(Nothing)
        Page_Loaded(Nothing, Nothing)    ' tak prościej, bo wklejam tu zawartość dawnego Page
    End Sub

#Region "zamykanie i ikonka"
    Private Async Sub Window_Closing(sender As Object, e As ComponentModel.CancelEventArgs)

        If Vblib.GetSettingsBool("uiServerEnabled") Then
            ' *TODO* YNCancel, zamknąć, zikonizować, cancel
            If Not Await Vblib.DialogBoxYNAsync("Program działa jako serwer, zamknąć go?") Then
                e.Cancel = True
                Return
            End If
            Application.gWcfServer?.StopSvc()
        End If

        If Application.Current.Windows.Count > 2 Then
            '    If Not Await Vblib.DialogBoxYNAsync("Zamknąć program?") Then Return
            ' bez tego zamyka tylko to jedno okno, a reszty już NIE
            Application.Current.Shutdown()
        End If

    End Sub

    Private Async Sub Window_StateChanged(sender As Object, e As EventArgs)
        ' https://www.codeproject.com/Articles/36468/WPF-NotifyIcon-2
        If Me.WindowState = WindowState.Minimized Then
            If Await Vblib.pkarlibmodule14.DialogBoxYNAsync("Zamknąć do SysTray?") Then
                myNotifyIcon.Visibility = Visibility.Visible
                myNotifyIcon.Icon = New System.Drawing.Icon("icons/trayIcon1.ico")
                Me.Hide()
            End If
        End If
    End Sub

    Private Sub uiTrayIcon_DoubleClick(sender As Object, e As RoutedEventArgs)
        Show()
        Me.WindowState = WindowState.Normal
        'SystemCommands.RestoreWindow(Me)
        myNotifyIcon.Visibility = Visibility.Collapsed
    End Sub
#End Region

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)

        ' jednak podstawowy folder musi istnieć, sami go sobie stworzymy jakby co.
        If Vblib.GetSettingsString("uiFolderBuffer") = "" Then
            Vblib.DialogBox("Nie ma ustawień, konieczne Settings")
            uiSettings_Click(Nothing, Nothing)
            Vblib.SetSettingsInt("uiMaxThumbs", CalculateMaxThumbCount)
        End If

        ' guzik Retrieve wyłączany jak nie ma zdefiniowanych sources
        ' uiRetrieve.IsEnabled = IO.File.Exists(App.GetDataFile("", "sources.json", False))

        ' *TODO* guziki pozostałe wyłączane jak nie ma LocalStorage

        Dim count As Integer = Application.GetBuffer.Count
        uiProcess.Content = $"Process ({count})"

        uiBrowseArch.IsEnabled = IsAnyArchPresent()

        ' zablokowane, do czasu poprawienia
        If Vblib.GetSettingsBool("uiServerEnabled") Then SettingsShare.StartServicing()
        ' na tej linii ERROR że nie może znaleźć System.ServiceModel v4.0.0, debugger nie widzi wejścia do StartSvc
        'Application.gWcfServer.StartSvc()

        uiVers.ShowAppVers

        'Dim pliczek As New BaseList(Of Vblib.OnePic)("E:\Temp\picsortrecovery", "komplet.json")
        'pliczek.Load()

        'Dim pliczek1 As New BaseList(Of Vblib.OnePic)(Application.GetDataFolder, "archIndexFull.json")
        'pliczek1.Load()

        'For Each oItem1 In pliczek1.GetList
        '    Dim bFound As Boolean = False
        '    For Each oItem In pliczek.GetList
        '        If oItem.sSuggestedFilename <> oItem1.sSuggestedFilename Then Continue For
        '        If oItem.PicGuid <> oItem1.PicGuid Then Continue For

        '        bFound = True
        '        Exit For
        '    Next

        '    If Not bFound Then
        '        pliczek.Add(oItem1)
        '    End If

        'Next

        'pliczek.Save(True)

        ' 2023.02.06, poprawienie pliku w ktorym były NULLe. Z 31 MB do 16 MB zeszlo.
        'Dim _fullArchive As New BaseList(Of Vblib.OnePic)(Application.GetDataFolder, "archIndexFull.json") ' "archIndex.flat.json"
        '_fullArchive.Load()
        '_fullArchive.Save(True)

        'Vblib.PicSourceBase.UseTagsFromFilename(Application.GetBuffer.GetList, Application.GetKeywords.ToFlatList)
        'Application.GetBuffer.SaveData()
    End Sub

    Private Sub ProbaWczytaniaJSONArch20231002()
        Dim lista As List(Of Vblib.OnePic)
        Dim sTxt = IO.File.ReadAllText(Application.GetDataFile("", "archIndexFull.json"))
        Dim sErr As String = ""
        Try
            lista = Newtonsoft.Json.JsonConvert.DeserializeObject(sTxt, GetType(ObservableList(Of Vblib.OnePic)))
        Catch ex As Exception
            sErr = ex.Message
        End Try

        If sErr <> "" Then Vblib.DialogBox(sErr)
        Debug.WriteLine(sErr)
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
                _uniqId.GetIdForPic(oPic)
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
        Vblib.DialogBox("Zrobilem nowy indeks, ktory powinien byc juz poprawny")
    End Sub

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
        uiSettings.IsEnabled = False
        Dim oWnd As New SettingsWindow
        oWnd.ShowDialog()
        uiSettings.IsEnabled = True
    End Sub

    Private Sub uiProcess_Click(sender As Object, e As RoutedEventArgs)
        uiProcess.IsEnabled = False
        Dim oWnd As New ProcessPic
        oWnd.Show()
        uiProcess.IsEnabled = True
    End Sub



    Private Sub uiBrowseArch_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New SettingsDirTree(False)
        oWnd.Show()
    End Sub

    Private Sub uiSearch_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New SearchWindow
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

'Public Class probaKlasy

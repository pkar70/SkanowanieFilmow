Imports pkar.WPF.Configs

Class SettingsDbase

    Private _isLoading As Boolean = True


    Private Sub uiSqlEnable_Checked(sender As Object, e As RoutedEventArgs)
        If _isLoading Then Return

        If Not VerifyData() Then
            Vblib.DialogBox("Niepełne dane logowania do bazy!")
            uiSqlEnabled.IsChecked = False
            Return
        End If

        'Vblib.DialogBox("pytanie o inicjalizację bazy, i zrobienie tego - jeśli są poprawne dane logowania")
    End Sub

    Private Sub uiTryConnect_Click(sender As Object, e As RoutedEventArgs)
        ' na pewno nie zostanie wywołane gdy SQL jest nie-enabled, bo IsEnabled=IsChecked

        If Not StoreData() Then Return  ' musimy zapamiętać, bo z GetSettings jest robiony connstring

        Application.ShowWait(True)
        If Application.gDbase.Connect("SQL") Then
            Vblib.DialogBox("Połączenie poprawne")
        Else
            Vblib.DialogBox("Błąd podłączenia bazy - sprawdź czemu")
        End If
        Application.gDbase.Disconnect("SQL")

        Application.ShowWait(False)

    End Sub


    Private Async Sub uiJsonToSQL_Click(sender As Object, e As RoutedEventArgs)
        If Not VerifyData() Then
            Vblib.DialogBox("Niepełne dane logowania do bazy!")
            Return
        End If

        If Not Await Vblib.DialogBoxYNAsync("Jesteś pewien? Aktualne dane w SQL zostaną usunięte!") Then Return

        Application.ShowWait(True)
        Dim iRet As Integer = Application.gDbase.CopyDatabase("JSON", "SQL")
        Application.ShowWait(False)

        If iRet < 0 Then
            Vblib.DialogBox("Błąd kopiowania bazy: " & iRet)
        Else
            Vblib.DialogBox($"Skopiowałem {iRet} rekordów")
        End If

    End Sub

    Private Async Sub uiSqlToJson_Click(sender As Object, e As RoutedEventArgs)
        If Not VerifyData() Then
            Vblib.DialogBox("Niepełne dane logowania do bazy!")
            Return
        End If

        If Not Await Vblib.DialogBoxYNAsync("Jesteś pewien? Aktualne dane w JSON zostaną usunięte!") Then Return

        Application.ShowWait(True)
        Dim iRet As Integer = Application.gDbase.CopyDatabase("SQL", "JSON")
        Application.ShowWait(False)

        If iRet < 0 Then
            Vblib.DialogBox("Błąd kopiowania bazy: " & iRet)
        Else
            Vblib.DialogBox($"Skopiowałem {iRet} rekordów")
        End If
    End Sub

    Private Function StoreData() As Boolean
        If Not VerifyData() Then
            Vblib.DialogBox("Popraw dane...")
            Return False
        End If

        uiJsonEnabled.SetSettingsBool
        uiSqlEnabled.SetSettingsBool
        uiSqlTrusted.SetSettingsBool
        uiSqlUserName.SetSettingsString
        uiSqlPassword.SetSettingsString
        uiSqlInstance.SetSettingsString

        Return True
    End Function

    Private Sub uiOK_Click(sender As Object, e As RoutedEventArgs)
        If Not StoreData() Then Return
        Me.NavigationService.GoBack()
    End Sub

    Private Function VerifyData() As Boolean

        If Not uiSqlEnabled.IsChecked Then
            ' nie może być obu wyłączonych, ale jeśli jest wyłączone SQL, to nie trzeba sprawdzać danych połączenia
            Return uiJsonEnabled.IsChecked
        End If

        If uiSqlTrusted.IsChecked Then Return True
        If String.IsNullOrWhiteSpace(uiSqlUserName.Text) Then Return False
        Return Not String.IsNullOrWhiteSpace(uiSqlPassword.Password)

    End Function

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        _isLoading = True
        uiJsonEnabled.GetSettingsBool
        uiSqlEnabled.GetSettingsBool
        uiSqlTrusted.GetSettingsBool
        uiSqlUserName.GetSettingsString
        uiSqlPassword.GetSettingsString
        uiSqlInstance.GetSettingsString()
        _isLoading = False

    End Sub

End Class

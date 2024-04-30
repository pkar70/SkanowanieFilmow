Class MainWindow


    Private Sub Przelicz()
        If uiWys Is Nothing Then Return
        If uiSzer Is Nothing Then Return
        If uiZrodlo Is Nothing Then Return

        If String.IsNullOrWhiteSpace(uiSzer.Text) Then Return
        If String.IsNullOrWhiteSpace(uiWys.Text) Then Return

        Dim papWys As Integer = uiWys.Text
        Dim papSzer As Integer = uiSzer.Text


        Dim sTxt As String = TryCast(uiZrodlo.SelectedValue, ComboBoxItem).Content

        ' przetworzenie ComboBox 
        Dim iInd As Integer = sTxt.IndexOf(":")
        If iInd < 0 Then
            MsgBox("Coś nie tak, błędny ComboBoxItem (bez ':')")
            Return
        End If

        sTxt = sTxt.Substring(iInd + 1).Trim
        iInd = sTxt.IndexOf("×")
        If iInd < 0 Then
            MsgBox("Coś nie tak, błędny ComboBoxItem (bez '×')")
            Return
        End If

        Dim orgSzer As Double = 40
        If Not Double.TryParse(sTxt.Substring(0, iInd).Trim, orgSzer) Then
            MsgBox("Coś nie tak, błędny ComboBoxItem (TryParse szerokość)")
            Return
        End If
        orgSzer *= 10

        sTxt = sTxt.Substring(iInd + 1).Trim ' omijam '×'

        iInd = sTxt.IndexOf("@")
        If iInd < 0 Then
            MsgBox("Coś nie tak, błędny ComboBoxItem (bez '@')")
            Return
        End If

        Dim orgWys As Double = 40
        If Not Double.TryParse(sTxt.Substring(0, iInd).Trim, orgWys) Then
            MsgBox("Coś nie tak, błędny ComboBoxItem (TryParse wysokość)")
            Return
        End If
        orgWys *= 10

        ' odczytanie resolution (@../mm) z combobox
        Dim orgResol As Integer = 90
        sTxt = sTxt.Substring(iInd + 1)
        iInd = sTxt.IndexOf("/")
        If iInd > 0 Then
            orgResol = sTxt.Substring(0, iInd)
        End If


        Dim skalaX As Double = Math.Min(orgSzer / papSzer, orgSzer / papWys)
        Dim skalaY As Double = Math.Min(orgWys / papSzer, orgWys / papWys)

        Dim skala As Double = Math.Max(skalaX, skalaY)

        uiScanDpi.Text = Math.Ceiling(25.4 * Math.Min(75, orgResol * skala))

        ' 60 / 40 => 1.5
        ' 82 / 40 => 2.0

    End Sub

    Private Sub uiPrzelicz_TextChanged(sender As Object, e As TextChangedEventArgs)
        Przelicz
    End Sub

    Private Sub uiPrzelicz__SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        Przelicz()
    End Sub
End Class

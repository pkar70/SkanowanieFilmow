
' być może także guzik EXPORT?

Public Class StatystykiWindow

    Private Async Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        If Application.gDbase.IsLoaded Then Return

        Application.ShowWait(True)
        Application.gDbase.Load()
        Application.ShowWait(False)

        If Application.gDbase.IsLoaded Then Return

        Await Vblib.DialogBoxAsync("Niestety, nie udało się wczytać żadnej bazy danych")
        Me.Close()

    End Sub

    Private Sub uiPicByRok_Click(sender As Object, e As RoutedEventArgs)

        ' założenie: mamy bazę

        ' Dim listka = From c In Application.gDbase.GetFirstLoaded.GetAll Group By label = c.GetMostProbablyDate.Year.ToString Into licznik = Count Order By label
        NarysujWykres(Function(picek) picek.GetMostProbablyDate.Year.ToString)

    End Sub

    Private Sub uiPicByMonth_Click(sender As Object, e As RoutedEventArgs)
        NarysujWykres(Function(picek) picek.GetMostProbablyDate.Month.ToString("00"))
    End Sub

    Private Sub uiPicByHour_Click(sender As Object, e As RoutedEventArgs)
        NarysujWykres(Function(picek) picek.GetMostProbablyDate.Hour.ToString("00"))
    End Sub

    Private Sub uiPicByDOW_Click(sender As Object, e As RoutedEventArgs)
        NarysujWykres(Function(picek) picek.GetMostProbablyDate.DayOfWeek.ToString)
    End Sub

    Private Sub uiPicByCamera_Click(sender As Object, e As RoutedEventArgs)
        NarysujWykres(AddressOf ExtractCamera)
    End Sub

    Private Function ExtractCamera(picek As Vblib.OnePic) As String
        If picek Is Nothing Then Return ""
        For Each oExif As Vblib.ExifTag In picek.Exifs
            If Not String.IsNullOrWhiteSpace(oExif.CameraModel) Then Return oExif.CameraModel
        Next

        Return ""
    End Function

    Private Sub uiPicByPogodaIcon_Click(sender As Object, e As RoutedEventArgs)
        NarysujWykres(AddressOf ExtractPogodaIcon)
    End Sub

    Private Function ExtractPogodaIcon(picek As Vblib.OnePic) As String
        Dim oExif As Vblib.ExifTag = picek?.GetExifOfType(Vblib.ExifSource.AutoVisCrosWeather)
        If oExif Is Nothing Then Return "?"

        Return oExif.PogodaAstro.currentConditions.icon
    End Function


    Private Sub uiPicByTemp_Click(sender As Object, e As RoutedEventArgs)
        NarysujWykres(AddressOf ExtractCurrTemp)
    End Sub
    Private Function ExtractCurrTemp(picek As Vblib.OnePic) As String
        Dim oExif As Vblib.ExifTag = picek?.GetExifOfType(Vblib.ExifSource.AutoVisCrosWeather)
        If oExif Is Nothing Then Return "?"

        Return Math.Round(oExif.PogodaAstro.currentConditions.temp).ToString("00")
    End Function

    Private Sub uiPicByFaces_Click(sender As Object, e As RoutedEventArgs)
        NarysujWykres(AddressOf ExtractFacesCount)
    End Sub
    Private Function ExtractFacesCount(picek As Vblib.OnePic) As String
        If picek Is Nothing Then Return ""
        Dim oExif As Vblib.ExifTag = picek?.GetExifOfType(Vblib.ExifSource.AutoAzure)
        If oExif IsNot Nothing Then Return If(oExif.AzureAnalysis?.Faces?.lista.Count, "?")

        oExif = picek?.GetExifOfType(Vblib.ExifSource.AutoWinFace)
        If oExif IsNot Nothing Then Return If(oExif.AzureAnalysis?.Faces?.lista.Count, "?")

        Return "?"
    End Function

    Private Sub uiPicByDomColor_Click(sender As Object, e As RoutedEventArgs)
        NarysujWykres(AddressOf ExtractDomColor)
    End Sub

    Private Function ExtractDomColor(picek As Vblib.OnePic) As String
        Dim oExif As Vblib.ExifTag = picek?.GetExifOfType(Vblib.ExifSource.AutoAzure)
        If oExif Is Nothing Then Return "?"

        Return If(oExif.AzureAnalysis?.Colors?.DominantColorForeground, "?")
    End Function

    Private Sub uiPicByKraj_Click(sender As Object, e As RoutedEventArgs)
        NarysujWykres(AddressOf ExtractKraj)
    End Sub

    Private Function ExtractKraj(picek As Vblib.OnePic) As String
        Dim oExif As Vblib.ExifTag = picek?.GetExifOfType(Vblib.ExifSource.AutoOSM)
        If oExif Is Nothing Then Return "?"

        Dim sName As String = oExif.GeoName
        Dim iInd As Integer = sName.LastIndexOf(",")
        If iInd < 5 Then Return "?"

        Return sName.Substring(iInd + 1).Trim
    End Function

    Private Sub NarysujWykres(keySel As Func(Of Vblib.OnePic, String))

        'Dim stats = Application.gDbase.GetFirstLoaded.GetAll.GroupBy _
        '    (Function(picek) picek.GetMostProbablyDate.Year.ToString,
        '     Function(picek) picek,
        '     Function(label, lista) New StatEntry With {.label = label, .licznik = lista.Count})

        Dim stats = Application.gDbase.GetFirstLoaded.GetAll.GroupBy(keySel,
                        Function(picek) picek,
                        Function(label, lista) New StatEntry With {.label = label, .licznik = lista.Count, .lista = lista}).
                 OrderBy(Function(entry) entry.label)

        'uiWykres.Children.Clear()

        Dim txtOut As String = ""

        For Each entry As StatEntry In stats
            txtOut &= entry.label & ":" & vbTab & entry.licznik & vbCrLf
        Next

        uiDump.Text = txtOut

    End Sub



End Class

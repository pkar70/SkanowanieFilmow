Imports Org.BouncyCastle.Asn1.Utilities
Imports pkar
Imports Vblib
Imports Windows.Security.EnterpriseData

Public Class PokazStatystyke

    Private _history As String

    ''' <summary>
    ''' pokaż statystykę zawartą w entries
    ''' </summary>
    ''' <param name="history">poprzednie kroki (statystyki)</param>
    ''' <param name="entries">aktualna statystyka</param>
    ''' <param name="licznik">licznik itemów (suma .count w entries)</param>
    Public Sub New(history As String, entries As List(Of StatEntry))

        ' This call is required by the designer.
        InitializeComponent()

        _history = history

        If entries Is Nothing Then
            If Not Application.gDbase.IsLoaded Then

                Application.ShowWait(True)
                Application.gDbase.Load()
                Application.ShowWait(False)

                If Not Application.gDbase.IsLoaded Then
                    '  Vblib.DialogBox("Niestety, nie udało się wczytać żadnej bazy danych")
                    Me.Close()
                End If
            End If

            history = ""
                Dim entry As New StatEntry
            entry.label = "Root"
            entry.lista = Application.gDbase.GetFirstLoaded.GetAll
                entry.licznik = entry.lista.Count
                entries = New List(Of StatEntry)
                entries.Add(entry)
            End If

            Dim licznik As Integer
        For Each entry In entries
            licznik += entry.lista.Count
        Next

        uiLista.ItemsSource = entries
        uiStatTitle.Text = _history & " (" & licznik & ")"

    End Sub

    Private Sub uiStatToClip_Click(sender As Object, e As RoutedEventArgs)

        If uiLista.ItemsSource Is Nothing Then Return

        Dim sTxt As String = _history & vbCrLf & vbCrLf

        For Each entry As StatEntry In uiLista.ItemsSource
            sTxt &= entry.label & ":" & vbTab & entry.licznik & vbCrLf
        Next

        Clipboard.SetText(sTxt)
    End Sub

    Private Async Sub uiShowThumbs_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        If oFE Is Nothing Then Return
        Dim entry As StatEntry = oFE.DataContext
        If entry Is Nothing Then Return

        If entry.licznik > 2000 Then
            Vblib.DialogBox("Za dużo zdjęć")
            Return
        End If

        If entry.licznik > 500 Then
            If Not Await Vblib.DialogBoxYNAsync("Na pewno? Jest dużo zdjęć, to trochę potrwa...") Then Return
        End If

        ' ten fragment jest przeróbką z SearchWindow.uiGoMiniaturki_Click

        Dim lista As New Vblib.BufferFromQuery()

        For Each oPic As Vblib.OnePic In entry.lista

            For Each oArch As lib_PicSource.LocalStorageMiddle In Application.GetArchivesList
                'vb14.DumpMessage($"trying archive {oArch.StorageName}")
                Dim sRealPath As String = oArch.GetRealPath(oPic.TargetDir, oPic.sSuggestedFilename)
                If Not String.IsNullOrWhiteSpace(sRealPath) Then
                    Dim oPicNew As Vblib.OnePic = oPic.Clone
                    oPic.InBufferPathName = sRealPath
                    Await lista.AddFile(oPic)
                    Exit For
                End If
            Next
        Next

        Dim oWnd As New ProcessBrowse(lista, True, _history & ":" & entry.label)
        oWnd.Show()
        Return

    End Sub



#Region "kolejne statystyki"

#Region "date related"

    Private Sub uiPicByRok_Click(sender As Object, e As RoutedEventArgs)
        ' Dim listka = From c In Application.gDbase.GetFirstLoaded.GetAll Group By label = c.GetMostProbablyDate.Year.ToString Into licznik = Count Order By label
        ZrobStatystyke(sender, Function(picek) picek.GetMostProbablyDate.Year.ToString)
    End Sub

    Private Sub uiPicByMonth_Click(sender As Object, e As RoutedEventArgs)
        ZrobStatystyke(sender, Function(picek) picek.GetMostProbablyDate.Month.ToString("00"))
    End Sub

    Private Sub uiPicByHour_Click(sender As Object, e As RoutedEventArgs)
        ZrobStatystyke(sender, Function(picek) picek.GetMostProbablyDate.Hour.ToString("00"))
    End Sub

    Private Sub uiPicByDOW_Click(sender As Object, e As RoutedEventArgs)
        ZrobStatystyke(sender, AddressOf ExtractDOW)
    End Sub
    Private Function ExtractDOW(picek As Vblib.OnePic) As String
        Dim data As Date = picek.GetMostProbablyDate
        If Not data.IsDateValid Then Return "?"
        Dim dow As Integer = data.DayOfWeek
        Return dow.ToString & " " & data.DayOfWeek.ToString
    End Function

    Private Sub uiPicBySunHour_Click(sender As Object, e As RoutedEventArgs)
        ZrobStatystyke(sender, AddressOf ExtractSunHour)
    End Sub
    Private Function ExtractSunHour(picek As Vblib.OnePic) As String
        Dim oExif As Vblib.ExifTag = picek?.GetExifOfType(Vblib.ExifSource.AutoVisCrosWeather)
        If oExif Is Nothing Then oExif = picek?.GetExifOfType(Vblib.ExifSource.AutoAstro)
        If oExif Is Nothing Then Return "?"

        Return oExif.PogodaAstro.day.sunhour.ToString("00")
    End Function

    Private Sub uiPicByDayNight_Click(sender As Object, e As RoutedEventArgs)
        ZrobStatystyke(sender, AddressOf ExtractDayNight)
    End Sub
    Private Function ExtractDayNight(picek As Vblib.OnePic) As String
        Dim oExif As Vblib.ExifTag = picek?.GetExifOfType(Vblib.ExifSource.AutoVisCrosWeather)
        If oExif Is Nothing Then oExif = picek?.GetExifOfType(Vblib.ExifSource.AutoAstro)
        If oExif Is Nothing Then Return "?"

        If oExif.PogodaAstro.day.sunhour < 0 Then Return "night"
        Return "day"
    End Function

#End Region

#Region "pogodowe"
    Private Sub uiPicByPogodaIcon_Click(sender As Object, e As RoutedEventArgs)
        ZrobStatystyke(sender, AddressOf ExtractPogodaIcon)
    End Sub

    Private Function ExtractPogodaIcon(picek As Vblib.OnePic) As String
        Dim oExif As Vblib.ExifTag = picek?.GetExifOfType(Vblib.ExifSource.AutoVisCrosWeather)
        If oExif Is Nothing Then Return "?"

        Return oExif.PogodaAstro.currentConditions.icon
    End Function


    Private Sub uiPicByTemp_Click(sender As Object, e As RoutedEventArgs)
        ZrobStatystyke(sender, AddressOf ExtractCurrTemp)
    End Sub
    Private Function ExtractCurrTemp(picek As Vblib.OnePic) As String
        Dim oExif As Vblib.ExifTag = picek?.GetExifOfType(Vblib.ExifSource.AutoVisCrosWeather)
        If oExif Is Nothing Then Return "?"

        Return Math.Round(oExif.PogodaAstro.currentConditions.temp).ToString("00")
    End Function

    Private Sub uiPicByTempOdcz_Click(sender As Object, e As RoutedEventArgs)
        ZrobStatystyke(sender, AddressOf ExtractTempOdcz)
    End Sub
    Private Function ExtractTempOdcz(picek As Vblib.OnePic) As String
        Dim oExif As Vblib.ExifTag = picek?.GetExifOfType(Vblib.ExifSource.AutoVisCrosWeather)
        If oExif Is Nothing Then Return "?"

        Return Math.Round(oExif.PogodaAstro.currentConditions.feelslike).ToString("00")
    End Function

    Private Sub uiPicByDayAvgTemp_Click(sender As Object, e As RoutedEventArgs)
        ZrobStatystyke(sender, AddressOf ExtractDayAvgTemp)
    End Sub
    Private Function ExtractDayAvgTemp(picek As Vblib.OnePic) As String
        Dim oExif As Vblib.ExifTag = picek?.GetExifOfType(Vblib.ExifSource.AutoVisCrosWeather)
        If oExif Is Nothing Then Return "?"

        Return Math.Round(oExif.PogodaAstro.day.temp).ToString("00")
    End Function

    Private Sub uiPicByDayPrecipType_Click(sender As Object, e As RoutedEventArgs)
        ZrobStatystyke(sender, AddressOf ExtractPrecipType)
    End Sub
    Private Function ExtractPrecipType(picek As Vblib.OnePic) As String
        Dim oExif As Vblib.ExifTag = picek?.GetExifOfType(Vblib.ExifSource.AutoVisCrosWeather)
        If oExif Is Nothing Then Return "?"

        If oExif.PogodaAstro.day.preciptype Is Nothing Then Return ""
        Return oExif.PogodaAstro.day.preciptype(0)
    End Function

#End Region

#Region "geography"
    Private Sub uiPicByKraj_Click(sender As Object, e As RoutedEventArgs)
        ZrobStatystyke(sender, AddressOf ExtractKraj)
    End Sub

    Private Function ExtractKraj(picek As Vblib.OnePic) As String
        Dim oExif As Vblib.ExifTag = picek?.GetExifOfType(Vblib.ExifSource.AutoOSM)
        If oExif Is Nothing Then Return "?"

        Dim sName As String = oExif.GeoName
        Dim iInd As Integer = sName.LastIndexOf(",")
        If iInd < 5 Then Return "?"

        Return sName.Substring(iInd + 1).Trim
    End Function

    Private Sub uiPicByPLwoj_Click(sender As Object, e As RoutedEventArgs)
        ZrobStatystyke(sender, AddressOf ExtractPLwoj)
    End Sub
    Private Function ExtractPLwoj(picek As Vblib.OnePic) As String
        Dim oExif As Vblib.ExifTag = picek?.GetExifOfType(Vblib.ExifSource.AutoImgw)
        If oExif Is Nothing Then Return "?"

        Dim sName As String = oExif.GeoName
        Dim iInd As Integer = sName.IndexOf("»")
        If iInd < 2 Then Return "?"

        Return sName.Substring(0, iInd - 1).Trim
    End Function


    Private Async Sub uiPicByDistance_Click(sender As Object, e As RoutedEventArgs)
        If Not Await Vblib.DialogBoxYNAsync("To trwa dość długo, kontynuować?") Then Return
        ZrobStatystyke(sender, AddressOf ExtractDistance)
    End Sub
    Private Function ExtractDistance(picek As Vblib.OnePic) As String
        Dim geotag = picek?.GetGeoTag
        If geotag Is Nothing Then Return "?"
        Dim odlegl As Integer = geotag.DistanceKmTo(BasicGeopos.GetKrakowCenter)

        If odlegl < 20 Then Return "   0..20"
        If odlegl < 50 Then Return "  20..50"
        If odlegl < 100 Then Return "  50..100"
        If odlegl < 200 Then Return " 100..200"
        If odlegl < 500 Then Return " 200..500"
        If odlegl < 1000 Then Return " 500..1000"
        If odlegl < 3000 Then Return "1000..3000"
        Return "3000...+"
    End Function

#End Region


#Region "z azure"
    Private Sub uiPicByFaces_Click(sender As Object, e As RoutedEventArgs)
        ZrobStatystyke(sender, AddressOf ExtractFacesCount)
    End Sub
    Private Function ExtractFacesCount(picek As Vblib.OnePic) As String
        If picek Is Nothing Then Return ""
        Dim oExif As Vblib.ExifTag = picek?.GetExifOfType(Vblib.ExifSource.AutoAzure)
        If oExif IsNot Nothing Then Return If(oExif.AzureAnalysis?.Faces?.lista.Count, "?")

        oExif = picek?.GetExifOfType(Vblib.ExifSource.AutoWinFace)
        If oExif IsNot Nothing Then Return If(oExif.AzureAnalysis?.Faces?.lista.Count, "?")

        Return "?"
    End Function

    Private Sub uiPicByDomFgColor_Click(sender As Object, e As RoutedEventArgs)
        ZrobStatystyke(sender, AddressOf ExtractDomFgColor)
    End Sub

    Private Function ExtractDomFgColor(picek As Vblib.OnePic) As String
        Dim oExif As Vblib.ExifTag = picek?.GetExifOfType(Vblib.ExifSource.AutoAzure)
        If oExif Is Nothing Then Return "?"

        Return If(oExif.AzureAnalysis?.Colors?.DominantColorForeground, "?")
    End Function

    Private Sub uiPicByDomBgColor_Click(sender As Object, e As RoutedEventArgs)
        ZrobStatystyke(sender, AddressOf ExtractDomBgColor)
    End Sub

    Private Function ExtractDomBgColor(picek As Vblib.OnePic) As String
        Dim oExif As Vblib.ExifTag = picek?.GetExifOfType(Vblib.ExifSource.AutoAzure)
        If oExif Is Nothing Then Return "?"

        Return If(oExif.AzureAnalysis?.Colors?.DominantColorBackground, "?")
    End Function

    Private Sub uiPicByAzureTag_Click(sender As Object, e As RoutedEventArgs)

    End Sub

    Private Sub uiPicByAzureObjects_Click(sender As Object, e As RoutedEventArgs)

    End Sub


#End Region

    Private Sub uiPicByCamera_Click(sender As Object, e As RoutedEventArgs)
        ZrobStatystyke(sender, AddressOf ExtractCamera)
    End Sub

    Private Function ExtractCamera(picek As Vblib.OnePic) As String
        If picek Is Nothing Then Return ""
        For Each oExif As Vblib.ExifTag In picek.Exifs
            If Not String.IsNullOrWhiteSpace(oExif.CameraModel) Then Return oExif.CameraModel
        Next

        Return ""
    End Function


    Private Sub uiPicByKeyword_Click(sender As Object, e As RoutedEventArgs)

    End Sub

    Private Sub uiPicByAutor_Click(sender As Object, e As RoutedEventArgs)
        ZrobStatystyke(sender, AddressOf ExtractAuthor)
    End Sub
    Private Function ExtractAuthor(picek As Vblib.OnePic) As String
        Dim oExif As Vblib.ExifTag = picek?.GetExifOfType(Vblib.ExifSource.SourceDefault)
        If oExif Is Nothing Then Return "?"

        Return oExif.Author
    End Function

    Private Sub ZrobStatystyke(sender As Object, keySel As Func(Of Vblib.OnePic, String))

        Dim oFE As FrameworkElement = sender
        If oFE Is Nothing Then Return
        Dim entry As StatEntry = oFE.DataContext
        If entry Is Nothing Then Return

        Dim stats = entry.lista.GroupBy(keySel,
                        Function(picek) picek,
                        Function(label, lista) New StatEntry With {.label = label, .licznik = lista.Count, .lista = lista}).
                 OrderBy(Function(entry1) entry1.label).ToList

        Dim oWnd As New PokazStatystyke(_history & ":" & entry.label, stats)
        oWnd.Show()

    End Sub

#End Region

End Class

Public Class StatEntry
    Public Property label As String
    Public Property licznik As Integer
    Public Property lista As IEnumerable(Of Vblib.OnePic)
End Class

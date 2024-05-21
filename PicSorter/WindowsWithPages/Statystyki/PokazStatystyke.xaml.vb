Imports pkar
Imports Vblib
Imports pkar.UI.Extensions

Public Class PokazStatystyke

    Private _history As String
    Private _entries As List(Of StatEntry)

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
        _entries = entries
    End Sub

    Private Async Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        Me.InitDialogs
        Me.ProgRingInit(True, False)

        If _entries Is Nothing Then
            If Not Application.gDbase.IsLoaded Then

                Me.ProgRingShow(True)
                Me.ProgRingSetText("Reading database...")
                Await Task.Run(Sub() Application.gDbase.Load())
                Me.ProgRingShow(False)

                If Not Application.gDbase.IsLoaded Then
                    '  Vblib.DialogBox("Niestety, nie udało się wczytać żadnej bazy danych")
                    Me.Close()
                End If
            End If
            'history = ""
            Dim entry As New StatEntry
            entry.label = "Root"
            entry.lista = Application.gDbase.GetFirstLoaded.GetAll
            entry.licznik = entry.lista.Count
            entry.total = entry.licznik ' na początek mamy tyle samo
            _entries = New List(Of StatEntry)
            _entries.Add(entry)
        End If

        ' policz procenty
        For Each entry In _entries
            uiStatTitle.Text = _history & " (" & entry.total & ")"
            entry.percent = Math.Round(100 * entry.licznik / entry.total).ToString("##0") & "%"
        Next

        uiLista.ItemsSource = _entries

        uiFilterek.IsEnabled = _entries.Count > 10

    End Sub


    Private Sub uiFilterek_TextChanged(sender As Object, e As TextChangedEventArgs)

        uiLista.ItemsSource = Nothing
        If uiFilterek.Text = "" Then
            uiLista.ItemsSource = _entries
        Else
            uiLista.ItemsSource = _entries.Where(Function(x) x.label.ContainsCI(uiFilterek.Text))
        End If
    End Sub

    Private Sub uiStatToClip_Click(sender As Object, e As RoutedEventArgs)

        If _entries Is Nothing Then Return

        Dim sTxt As String = _history & vbCrLf & vbCrLf

        For Each entry As StatEntry In _entries
            sTxt &= entry.label & ":" & vbTab & entry.licznik & vbTab & entry.percent & vbCrLf
        Next

        Clipboard.SetText(sTxt)
    End Sub

    Private Async Sub uiShowThumbs_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        If oFE Is Nothing Then Return
        Dim entry As StatEntry = oFE.DataContext
        If entry Is Nothing Then Return

        If entry.licznik > 2000 Then
            Me.MsgBox("Za dużo zdjęć")
            Return
        End If

        If entry.licznik > 500 Then
            If Not Await Me.DialogBoxYNAsync("Na pewno? Jest dużo zdjęć, to trochę potrwa...") Then Return
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
        If Not Await Me.DialogBoxYNAsync("To trwa dość długo, kontynuować?") Then Return
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
        Dim oFE As FrameworkElement = sender
        If oFE Is Nothing Then Return
        Dim entry As StatEntry = oFE.DataContext
        If entry Is Nothing Then Return

        Dim tagi As New List(Of String)

        ' wyciągnij listę tagów
        For Each oPic As Vblib.OnePic In entry.lista
            Dim azurek As Vblib.ExifTag = oPic.GetExifOfType(Vblib.ExifSource.AutoAzure)
            If azurek?.AzureAnalysis?.Tags Is Nothing Then Continue For

            For Each oTag In azurek.AzureAnalysis.Tags.GetList
                If tagi.Contains(oTag.tekst) Then Continue For

                tagi.Add(oTag.tekst)
            Next
        Next

        StatystykaAzureWgListy(entry, tagi, AddressOf AzureByTag)
    End Sub

    Private Sub StatystykaAzureWgListy(entry As StatEntry, tagi As List(Of String), wyszukiwacz As Func(Of OnePic, String, Boolean))

        Dim stats As New List(Of StatEntry)
        Dim total As Integer = entry.lista.Count

        For Each tag As String In tagi.OrderBy(Of String)(Function(x) x)
            Dim withKwd As New StatEntry With {.label = tag}
            withKwd.lista = entry.lista.Where(Function(x) wyszukiwacz(x, tag))
            withKwd.licznik = withKwd.lista.Count
            withKwd.total = total
            ' withKwd.percent będzie policzone przy nowym oknie 
            stats.Add(withKwd)
        Next

        Dim oWnd As New PokazStatystyke(_history & ":" & entry.label, stats)
        oWnd.Show()
    End Sub

    Private Function AzureByTag(oPic As Vblib.OnePic, tag As String) As Boolean
        Dim azurek As Vblib.ExifTag = oPic.GetExifOfType(Vblib.ExifSource.AutoAzure)
        If azurek?.AzureAnalysis?.Tags Is Nothing Then Return False
        Return azurek.AzureAnalysis.Tags.GetList.Any(Function(x) x.tekst = tag)
    End Function


    Private Sub uiPicByAzureObjects_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        If oFE Is Nothing Then Return
        Dim entry As StatEntry = oFE.DataContext
        If entry Is Nothing Then Return

        Dim tagi As New List(Of String)

        Dim dstart = Date.Now

        ' wyciągnij listę tagów
        For Each oPic As Vblib.OnePic In entry.lista
            Dim azurek As Vblib.ExifTag = oPic.GetExifOfType(Vblib.ExifSource.AutoAzure)
            If azurek?.AzureAnalysis?.Objects Is Nothing Then Continue For

            For Each oTag In azurek.AzureAnalysis.Objects.GetList
                If tagi.Contains(oTag.tekst) Then Continue For

                tagi.Add(oTag.tekst)
            Next
        Next

        'Dim dend = Date.Now
        'Dim msec = (dend - dstart).TotalMilliseconds
        'Me.MsgBox("Wynajdowanie wszystkich objektów: " & msec & " msec")

        StatystykaAzureWgListy(entry, tagi, AddressOf AzureByObject)

    End Sub

    Private Function AzureByObject(oPic As Vblib.OnePic, tag As String) As Boolean
        Dim azurek As Vblib.ExifTag = oPic.GetExifOfType(Vblib.ExifSource.AutoAzure)
        If azurek?.AzureAnalysis?.Objects Is Nothing Then Return False
        Return azurek.AzureAnalysis.Objects.GetList.Any(Function(x) x.tekst = tag)
    End Function

    Private Sub uiPicByAzureBrands_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        If oFE Is Nothing Then Return
        Dim entry As StatEntry = oFE.DataContext
        If entry Is Nothing Then Return

        Dim tagi As New List(Of String)

        Dim dstart = Date.Now

        ' wyciągnij listę tagów
        For Each oPic As Vblib.OnePic In entry.lista
            Dim azurek As Vblib.ExifTag = oPic.GetExifOfType(Vblib.ExifSource.AutoAzure)
            If azurek?.AzureAnalysis?.Brands Is Nothing Then Continue For

            For Each oTag In azurek.AzureAnalysis.Brands.GetList
                If tagi.Contains(oTag.tekst) Then Continue For

                tagi.Add(oTag.tekst)
            Next
        Next

        'Dim dend = Date.Now
        'Dim msec = (dend - dstart).TotalMilliseconds
        'Me.MsgBox("Wynajdowanie wszystkich objektów: " & msec & " msec")

        StatystykaAzureWgListy(entry, tagi, AddressOf AzureByBrand)

    End Sub

    Private Function AzureByBrand(oPic As Vblib.OnePic, tag As String) As Boolean
        Dim azurek As Vblib.ExifTag = oPic.GetExifOfType(Vblib.ExifSource.AutoAzure)
        If azurek?.AzureAnalysis?.Brands Is Nothing Then Return False
        Return azurek.AzureAnalysis.Brands.GetList.Any(Function(x) x.tekst = tag)
    End Function
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

    Private Sub PicByKeyword(sender As Object, prefix As String)
        Dim oFE As FrameworkElement = sender
        If oFE Is Nothing Then Return
        Dim entry As StatEntry = oFE.DataContext
        If entry Is Nothing Then Return

        Dim stats As New List(Of StatEntry)
        Dim total As Integer = entry.lista.Count

        For Each oKey As Vblib.OneKeyword In Application.GetKeywords.ToFlatList
            If Not oKey.sId.StartsWith(prefix) Then Continue For

            Dim withKwd As New StatEntry With {.label = oKey.sId}
            withKwd.lista = entry.lista.Where(Function(x) x.sumOfKwds.Contains(oKey.sId))
            withKwd.licznik = withKwd.lista.Count
            withKwd.total = total
            ' withKwd.percent będzie policzone przy nowym oknie 
            stats.Add(withKwd)
        Next

        Dim oWnd As New PokazStatystyke(_history & ":" & entry.label, stats)
        oWnd.Show()
    End Sub


    Private Sub uiPicByKeywordO_Click(sender As Object, e As RoutedEventArgs)
        PicByKeyword(sender, "-")
    End Sub
    Private Sub uiPicByKeywordM_Click(sender As Object, e As RoutedEventArgs)
        PicByKeyword(sender, "=")
    End Sub
    Private Sub uiPicByKeywordI_Click(sender As Object, e As RoutedEventArgs)
        PicByKeyword(sender, "=")
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

        Dim total As Integer = entry.lista.Count

        Dim stats = entry.lista.GroupBy(keySel,
                        Function(picek) picek,
                        Function(label, lista) New StatEntry With {.label = label, .licznik = lista.Count, .lista = lista, .total = total}).
                 OrderBy(Function(entry1) entry1.label).ToList

        Dim oWnd As New PokazStatystyke(_history & ":" & entry.label, stats)
        oWnd.Show()

    End Sub



#End Region

End Class

Public Class StatEntry
    Public Property label As String
    Public Property licznik As Integer
    Public Property total As Integer
    Public Property percent As String
    Public Property lista As IEnumerable(Of Vblib.OnePic)
End Class

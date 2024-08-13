Imports pkar
Imports Vblib
Imports pkar.UI.Extensions
Imports MetadataExtractor

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

            If System.IO.File.Exists(oPic.InBufferPathName) Then
                Await lista.AddFile(oPic)
            Else

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

            End If

        Next

        Dim oWnd As New ProcessBrowse(lista, _history & ":" & entry.label)
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
        ZrobStatystyke(sender,
                       Function(x As Vblib.OnePic) As String
                           Dim oExif As Vblib.ExifTag = x?.GetExifOfType(Vblib.ExifSource.AutoVisCrosWeather)
                           If oExif Is Nothing Then Return "?"

                           Return oExif.PogodaAstro.currentConditions.icon
                       End Function)
    End Sub

    Private Sub uiPicByTemp_Click(sender As Object, e As RoutedEventArgs)
        ZrobStatystyke(sender, Function(picek As Vblib.OnePic) As String
                                   Dim oExif As Vblib.ExifTag = picek?.GetExifOfType(Vblib.ExifSource.AutoVisCrosWeather)
                                   If oExif Is Nothing Then Return "?"

                                   Return Math.Round(oExif.PogodaAstro.currentConditions.temp).ToString("00")
                               End Function,
                       True)
    End Sub

    Private Sub uiPicByTempOdcz_Click(sender As Object, e As RoutedEventArgs)
        ZrobStatystyke(sender, Function(picek As Vblib.OnePic) As String
                                   Dim oExif As Vblib.ExifTag = picek?.GetExifOfType(Vblib.ExifSource.AutoVisCrosWeather)
                                   If oExif Is Nothing Then Return "?"

                                   Return Math.Round(oExif.PogodaAstro.currentConditions.feelslike).ToString("00")
                               End Function,
                       True)
    End Sub

    Private Sub uiPicByDayAvgTemp_Click(sender As Object, e As RoutedEventArgs)
        ZrobStatystyke(sender, Function(picek As Vblib.OnePic) As String
                                   Dim oExif As Vblib.ExifTag = picek?.GetExifOfType(Vblib.ExifSource.AutoVisCrosWeather)
                                   If oExif Is Nothing Then Return "?"

                                   Return Math.Round(oExif.PogodaAstro.day.temp).ToString("00")
                               End Function,
                       True)
    End Sub

    Private Sub uiPicByDayPrecipType_Click(sender As Object, e As RoutedEventArgs)
        ZrobStatystyke(sender, Function(picek As Vblib.OnePic) As String
                                   Dim oExif As Vblib.ExifTag = picek?.GetExifOfType(Vblib.ExifSource.AutoVisCrosWeather)
                                   If oExif Is Nothing Then Return "?"

                                   If oExif.PogodaAstro.day.preciptype Is Nothing Then Return ""
                                   Return oExif.PogodaAstro.day.preciptype(0)
                               End Function)
    End Sub

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
        ZrobStatystyke(sender, Function(x)
                                   If x Is Nothing Then Return ""
                                   Dim oExif As Vblib.ExifTag = x?.GetExifOfType(Vblib.ExifSource.AutoAzure)
                                   If oExif IsNot Nothing Then Return If(oExif.AzureAnalysis?.Faces?.lista.Count, "?")

                                   oExif = x?.GetExifOfType(Vblib.ExifSource.AutoWinFace)
                                   If oExif IsNot Nothing Then Return If(oExif.AzureAnalysis?.Faces?.lista.Count, "?")

                                   Return "?"
                               End Function)
    End Sub

    Private Sub uiPicByDomFgColor_Click(sender As Object, e As RoutedEventArgs)
        ZrobStatystyke(sender, Function(x)
                                   Dim oExif As Vblib.ExifTag = x?.GetExifOfType(Vblib.ExifSource.AutoAzure)
                                   If oExif Is Nothing Then Return "?"

                                   Return If(oExif.AzureAnalysis?.Colors?.DominantColorForeground, "?")
                               End Function)
    End Sub

    Private Sub uiPicByDomBgColor_Click(sender As Object, e As RoutedEventArgs)
        ZrobStatystyke(sender, Function(x)
                                   Dim oExif As Vblib.ExifTag = x?.GetExifOfType(Vblib.ExifSource.AutoAzure)
                                   If oExif Is Nothing Then Return "?"

                                   Return If(oExif.AzureAnalysis?.Colors?.DominantColorBackground, "?")
                               End Function)
    End Sub

    Private Sub uiPicByAzureTag_Click(sender As Object, e As RoutedEventArgs)
        PicByAzureListProperty(sender, "Tags")
    End Sub

    Private Sub uiPicByAzureCategories_Click(sender As Object, e As RoutedEventArgs)
        PicByAzureListProperty(sender, "Categories")
    End Sub
    Private Sub uiPicByAzureLandmarks_Click(sender As Object, e As RoutedEventArgs)
        PicByAzureListProperty(sender, "Landmarks")
    End Sub
    Private Sub uiPicByAzureObjects_Click(sender As Object, e As RoutedEventArgs)
        PicByAzureListProperty(sender, "Objects")
    End Sub
    Private Sub uiPicByAzureBrands_Click(sender As Object, e As RoutedEventArgs)
        PicByAzureListProperty(sender, "Brands")
    End Sub
    Private Sub uiPicByAzureCelebrities_Click(sender As Object, e As RoutedEventArgs)
        PicByAzureListProperty(sender, "Celebrities")
    End Sub

    Private Sub PicByAzureListProperty(sender As Object, listaPropName As String)
        Dim oFE As FrameworkElement = sender
        If oFE Is Nothing Then Return
        Dim entry As StatEntry = oFE.DataContext
        If entry Is Nothing Then Return

        ' wyciągnij listę tagów
        Dim tagi As List(Of String) = WyciagnijListeMozliwych(listaPropName, entry.lista)
        StatystykaAzureWgListy(entry, tagi, listaPropName)
    End Sub

    Public Shared Function WyciagnijListeMozliwych(listaPropName As String, lista As IEnumerable(Of Vblib.OnePic)) As List(Of String)

        Dim tagi As New List(Of String)

        For Each oPic As Vblib.OnePic In lista
            Dim azurek As Vblib.ExifTag = oPic.GetExifOfType(Vblib.ExifSource.AutoAzure)
            If azurek?.AzureAnalysis Is Nothing Then Continue For

            Dim listaProb As ListTextWithProbability = TryCast(azurek.AzureAnalysis.GetType.GetProperty(listaPropName).GetValue(azurek.AzureAnalysis), ListTextWithProbability)
            Dim listaProbBox As ListTextWithProbabAndBox = TryCast(azurek.AzureAnalysis.GetType.GetProperty(listaPropName).GetValue(azurek.AzureAnalysis), ListTextWithProbabAndBox)

            If listaProb Is Nothing AndAlso listaProbBox Is Nothing Then Continue For

            If listaProb IsNot Nothing Then
                For Each oTag In listaProb.GetList
                    If tagi.Contains(oTag.tekst) Then Continue For
                    tagi.Add(oTag.tekst)
                Next
            Else
                For Each oTag In listaProbBox.GetList
                    If tagi.Contains(oTag.tekst) Then Continue For
                    tagi.Add(oTag.tekst)
                Next
            End If

        Next

        Return tagi
    End Function

    Private Sub StatystykaAzureWgListy(entry As StatEntry, tagi As List(Of String), listaPropName As String)

        Dim stats As New List(Of StatEntry)
        Dim total As Integer = entry.lista.Count

        For Each tag As String In tagi.OrderBy(Of String)(Function(x) x)
            Dim withKwd As New StatEntry With {.label = tag}
            withKwd.lista = entry.lista.Where(Function(x)
                                                  Dim azurek As Vblib.ExifTag = x.GetExifOfType(Vblib.ExifSource.AutoAzure)
                                                  If azurek?.AzureAnalysis Is Nothing Then Return False
                                                  Dim listaProb As ListTextWithProbability = TryCast(azurek.AzureAnalysis.GetType.GetProperty(listaPropName).GetValue(azurek.AzureAnalysis), ListTextWithProbability)
                                                  Dim listaProbBox As ListTextWithProbabAndBox = TryCast(azurek.AzureAnalysis.GetType.GetProperty(listaPropName).GetValue(azurek.AzureAnalysis), ListTextWithProbabAndBox)

                                                  If listaProb Is Nothing AndAlso listaProbBox Is Nothing Then Return False
                                                  If listaProb IsNot Nothing Then
                                                      Return listaProb.GetList.Any(Function(y) y.tekst = tag)
                                                  Else
                                                      Return listaProbBox.GetList.Any(Function(y) y.tekst = tag)
                                                  End If
                                              End Function)
            withKwd.licznik = withKwd.lista.Count
            withKwd.total = total
            ' withKwd.percent będzie policzone przy nowym oknie 
            stats.Add(withKwd)
        Next

        Dim oWnd As New PokazStatystyke(_history & ":" & entry.label, stats)
        oWnd.Show()
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

    Private Sub PicByKeyword(sender As Object, prefix As String)
        Dim oFE As FrameworkElement = sender
        If oFE Is Nothing Then Return
        Dim entry As StatEntry = oFE.DataContext
        If entry Is Nothing Then Return

        Dim stats As New List(Of StatEntry)
        Dim total As Integer = entry.lista.Count

        For Each oKey As Vblib.OneKeyword In vblib.GetKeywords.ToFlatList
            If Not oKey.sId.StartsWith(prefix) Then Continue For

            Dim withKwd As New StatEntry With {.label = oKey.sId}
            withKwd.lista = entry.lista.Where(Function(x) x.sumOfKwds.Contains(oKey.sId))
            withKwd.licznik = withKwd.lista.Count
            withKwd.total = total
            ' withKwd.percent będzie policzone przy nowym oknie 
            If withKwd.licznik > 0 Then stats.Add(withKwd)
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

    Private Sub ZrobStatystyke(sender As Object, keySel As Func(Of Vblib.OnePic, String), Optional sortnum As Boolean = False)

        Dim oFE As FrameworkElement = sender
        If oFE Is Nothing Then Return
        Dim entry As StatEntry = oFE.DataContext
        If entry Is Nothing Then Return

        Dim total As Integer = entry.lista.Count

        Dim statsTemp As IEnumerable(Of StatEntry) = entry.lista.GroupBy(keySel,
                        Function(picek) picek,
                        Function(label, lista) New StatEntry With {.label = label, .licznik = lista.Count, .lista = lista, .total = total})

        Dim stats As List(Of StatEntry)
        If sortnum Then
            stats = statsTemp.OrderBy(Function(entry1) entry1.label).ToList
        Else
            stats = statsTemp.OrderBy(Of Double)(Function(entry1)
                                                     Dim ret As Double
                                                     If Double.TryParse(entry1.label, ret) Then
                                                         Return ret
                                                     Else
                                                         Return 0
                                                     End If
                                                 End Function).ToList
        End If


        Dim oWnd As New PokazStatystyke(_history & ":" & entry.label, stats)
        oWnd.Show()

    End Sub

    Private Sub uiPicByRealDateYN_Click(sender As Object, e As RoutedEventArgs)
        ZrobStatystyke(sender, Function(x)
                                   If x Is Nothing Then Return "?"
                                   Return If(x.HasRealDate, "Yes", "No")
                               End Function)
    End Sub

    Private Sub uiPicByGeotagYN_Click(sender As Object, e As RoutedEventArgs)
        ZrobStatystyke(sender, Function(x)
                                   If x Is Nothing Then Return "?"
                                   Return If(x.GetGeoTag Is Nothing, "No", "Yes")
                               End Function)
    End Sub

    Private Sub uiPicByMono_Click(sender As Object, e As RoutedEventArgs)
        ZrobStatystyke(sender, Function(x)
                                   Dim azurek As Vblib.ExifTag = x.GetExifOfType(Vblib.ExifSource.AutoAzure)
                                   If azurek?.AzureAnalysis Is Nothing Then Return "?"
                                   Return If(azurek?.AzureAnalysis.IsBW, "black/white", "color")
                               End Function)
    End Sub

    Private Sub uiPicByAzureAdult_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        If oFE Is Nothing Then Return
        Dim entry As StatEntry = oFE.DataContext
        If entry Is Nothing Then Return

        Dim stats As New List(Of StatEntry)
        Dim total As Integer = entry.lista.Count

        For Each tag As String In {"ADULTPIC", "GORYPIC", "RACYPIC"}
            Dim withKwd As New StatEntry With {.label = tag.Replace("PIC", "")}
            withKwd.lista = entry.lista.Where(Function(x)
                                                  Dim azurek As Vblib.ExifTag = x.GetExifOfType(Vblib.ExifSource.AutoAzure)
                                                  If azurek?.AzureAnalysis?.Wiekowe Is Nothing Then Return False

                                                  Return azurek.AzureAnalysis.Wiekowe.Contains(tag)
                                              End Function)
            withKwd.licznik = withKwd.lista.Count
            withKwd.total = total
            ' withKwd.percent będzie policzone przy nowym oknie 
            stats.Add(withKwd)
        Next

        Dim oWnd As New PokazStatystyke(_history & ":" & entry.label, stats)
        oWnd.Show()
    End Sub


    Private Sub uiPicByTarget_Click(sender As Object, e As RoutedEventArgs)
        ZrobStatystyke(sender, Function(x)
                                   If x Is Nothing Then Return "?"
                                   If x.TargetDir.StartsWith("reel") Then Return "reel"
                                   If x.TargetDir.StartsWith("inet") Then Return "inet"
                                   Return "std"
                               End Function)
    End Sub

    Private Sub uiPicByType_Click(sender As Object, e As RoutedEventArgs)
        ZrobStatystyke(sender, Function(x)
                                   If x Is Nothing Then Return "?"
                                   Dim ext As String = System.IO.Path.GetExtension(x.sSuggestedFilename).ToLowerInvariant & ";"

                                   If OnePic.ExtsMovie.Contains(ext) Then Return "movie"
                                   If OnePic.ExtsPic.Contains(ext) Then Return "pic"
                                   If OnePic.ExtsStereo.Contains(ext) Then Return "stereo"

                                   Return "other"
                               End Function)
    End Sub

    Private Sub uiPicByPicOrient_Click(sender As Object, e As RoutedEventArgs)
        ZrobStatystyke(sender, Function(x)
                                   Dim exifek As Vblib.ExifTag = x?.GetExifOfType(Vblib.ExifSource.FileExif)
                                   If exifek Is Nothing Then Return "?"

                                   Dim uklad As Integer = 0
                                   If exifek.x * 1.1 < exifek.y Then uklad = 1 ' Return "portrait"
                                   If exifek.y * 1.1 < exifek.x Then uklad = -1 'Return "landscape"

                                   If uklad = 0 Then Return "square"

                                   If exifek.Orientation > 4 Then uklad = -uklad

                                   If uklad = 1 Then Return "portrait"
                                   Return "landscape"

                               End Function)
    End Sub

    Private Sub uiPicByPicSize_Click(sender As Object, e As RoutedEventArgs)
        ZrobStatystyke(sender, Function(x)
                                   Dim exifek As Vblib.ExifTag = x?.GetExifOfType(Vblib.ExifSource.FileExif)
                                   If exifek Is Nothing Then Return "?"

                                   Dim mpix As Integer = exifek.x * exifek.y / 1000 / 1000

                                   Return mpix
                               End Function)
    End Sub

    '
#End Region

End Class

Public Class StatEntry
    Public Property label As String
    Public Property licznik As Integer
    Public Property total As Integer
    Public Property percent As String
    Public Property lista As IEnumerable(Of Vblib.OnePic)
End Class

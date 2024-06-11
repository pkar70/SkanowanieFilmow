'Imports System.Drawing
Imports pkar
Imports Vblib
Imports pkar.UI.Extensions
Imports PicSorterNS.ProcessBrowse
Imports System.Text.RegularExpressions
Imports System.IO
Imports Microsoft.EntityFrameworkCore
Imports System.Drawing


Public Class ProcessDownloadInternet

    Public Property Counter As Integer
    Private _source As Vblib.PicSourceBase
    Private _picek As Vblib.OnePic
    Private _lastgeo As Vblib.ExifTag
    Private Shared _autorzy As String()

    Sub New(oSrc As Vblib.PicSourceBase)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        _source = oSrc


        Dim sPath As String = IO.Path.Combine(App.GetDataFolder, "inetauthors.txt")
        If IO.File.Exists(sPath) Then _autorzy = IO.File.ReadAllLines(sPath)

    End Sub

    Private Async Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        Me.InitDialogs
        Me.ProgRingInit(True, False)

        Me.Title = "Importing for " & _source.SourceName

        If Not String.IsNullOrWhiteSpace(_source.Path) Then
            Dim urik As New Uri(_source.Path)
            urik.OpenBrowser
        End If

        Dim sTxt As String = "(nieznana)"
        If _source.lastDownload.IsDateValid Then
            Await Me.MsgBoxAsync($"Last download: {_source.lastDownload.ToExifString} 
Pic: {_source.VolLabel}
Możesz przewinąć stronę WWW do tego zdjęcia...")
        End If

        Mouse.OverrideCursor = Nothing ' bo z poprzedniego okna jest override, i przeszkadza

        uiKeywords.uiSlowka.Text = _source.defaultKwds

        NextPic()
    End Sub

    Private Sub NextPic()
        Vblib.DumpCurrMethod()

        Dim countnew As Integer = _source.ReadDirectory(Nothing)
        If countnew <= 0 Then Return

        _picek = _source.GetLast
        If _picek Is Nothing Then
            Me.MsgBox("Koniec plików")
            uiPicek.Source = Nothing
            Return
        End If
        _picek.oContent.Close() ' nie potrzebujemy, a poza tym blokuje pokazanie obrazka :)

        uiSourceFilename.Text = _picek.sSuggestedFilename
        uiPicek.Source = ThumbPicek.ThumbCreateFromNormal(_picek.sInSourceID)
        uiGeo.Content = " Set "
        uiGeo.ToolTip = ""

        Vblib.DumpMessage("Picek: " & uiSourceFilename.Text)

        uiAutor.Text = "" ' autora nie chcemy powtarzać
        uiLink.Text = "" ' jak się nie uda ściągnąć, to trzeba będzie wpisać
        TryGetLastLink()

        uiKeywords.uiSlowka.Text = _source.defaultKwds

        uiDescription.Focus()

        uiMenuGeo.IsEnabled = _lastgeo IsNot Nothing
    End Sub

    Private Sub uiEnd_Click(sender As Object, e As RoutedEventArgs)
        Application.GetBuffer.SaveData()
        'Application.GetSourcesList.Save() - zapis będzie z "piętro wyżej", razem z datą itp.

        DialogResult = True
        Me.Close()
    End Sub

    Private Async Sub uiAdd_Click(sender As Object, e As RoutedEventArgs)
        Counter += 1

        _picek.TargetDir = "inet\" & _source.SourceName

        Dim srcExif As Vblib.ExifTag = _picek.GetExifOfType(Vblib.ExifSource.SourceDefault).Clone
        srcExif.Author = uiAutor.Text
        If uiDateRange.UseMin Then srcExif.DateMin = uiDateRange.MinDate
        If uiDateRange.UseMax Then srcExif.DateMax = uiDateRange.MaxDate
        _picek.ReplaceOrAddExif(srcExif)    ' bo się powtarzało, nie wiem czemu - inne dane w _picek są ładnie zmienne

        ' MANUAL_TAG, ale z pełnym opisem, a także z GeoTag jak trzeba
        _picek.ReplaceOrAddExif(Application.GetKeywords.CreateManualTagFromKwds(uiKeywords.uiSlowka.Text))

        Dim descr As New OneDescription(uiDescription.Text, "")
        _picek.AddDescription(descr)

        Dim linek As New OneLink() With {.link = uiLink.Text, .opis = "source"}
        _picek.AddLink(linek)

        _picek.sSuggestedFilename = _source.SourceName & "_" & Date.Now.ToString("yy.MM.dd_HH.mm.ss") & IO.Path.GetExtension(_picek.sInSourceID)
        _picek.oContent = IO.File.OpenRead(_picek.sInSourceID)
        Await Application.GetBuffer.AddFile(_picek)

        _picek.oContent.Close() ' bez tego nie byłoby możliwe delete

        ' INET używa Recursive jako "Immediately delete"
        If _source.Recursive Then IO.File.Delete(_picek.sInSourceID)

        _source.VolLabel = uiDescription.Text
        If _source.VolLabel.Length > 50 Then _source.VolLabel = _source.VolLabel.Substring(0, 48)

        NextPic()
    End Sub

    Private Sub uiSearch_Click(sender As Object, e As RoutedEventArgs)

    End Sub

    Private Sub uiSetGeo_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New EnterGeoTag
        If Not oWnd.ShowDialog Then Return

        Dim geoExif As New Vblib.ExifTag(Vblib.ExifSource.ManualGeo)
        geoExif.GeoTag = oWnd.GetGeoPos
        geoExif.GeoZgrubne = oWnd.IsZgrubne
        _picek.ReplaceOrAddExif(geoExif)
        _lastgeo = geoExif
        uiGeo.Content = " Change "
        uiGeo.ToolTip = _lastgeo.GeoTag.FormatLink("%lat / %lon")

    End Sub

    Private Sub uiLink_TextChanged(sender As Object, e As TextChangedEventArgs)
        Dim tekst As String = uiLink.Text
        If Not tekst.StartsWithCI("https://www.facebook.com/photo/?fbid") Then Return

        Dim iInd As Integer = tekst.IndexOf("&")
        If iInd < 1 Then Return

        uiLink.Text = tekst.Substring(0, iInd)

    End Sub

    Private Sub uiDescription_TextChanged(sender As Object, e As TextChangedEventArgs)
        Dim tekst As String = uiDescription.Text
        SprobujRozpoznacAutora(tekst)
        SprobujRozpoznacDate(tekst)

        If Not tekst.Contains(vbCr) AndAlso Not tekst.Contains(vbLf) Then Return

        tekst = tekst.Replace(vbCr, " ")
        tekst = tekst.Replace(vbLf, " ")

        uiDescription.Text = tekst


    End Sub

    Private Sub SprobujRozpoznacDate(tekst As String)
        ' dd.mm.yyyy
        ' yyyy
        ' yyyy.mm
        Dim data As Date

        Dim mam As Match = Regex.Match(tekst, "[0-3][0-9].[0-1][0-9].[12][0-9][0-9][0-9]")
        If mam.Success Then
            If Date.TryParseExact(mam.Value, "dd.MM.yyyy", Nothing, Globalization.DateTimeStyles.None, data) Then
                uiDateRange.RangeAsText = data.ToString("yyyy.MM.dd")
                Return
            End If
        End If

        mam = Regex.Match(tekst, "[12][0-9][0-9][0-9].[0-1][0-9].[0-3][0-9]")
        If mam.Success Then
            If Date.TryParseExact(mam.Value, "yyyy.MM.dd", Nothing, Globalization.DateTimeStyles.None, data) Then
                uiDateRange.RangeAsText = data.ToString("yyyy.MM.dd")
                Return
            End If
        End If

        mam = Regex.Match(tekst, "[12][0-9][0-9][0-9].[0-1][0-9]")
        If mam.Success Then
            If Date.TryParseExact(mam.Value & ".01", "yyyy.MM.dd", Nothing, Globalization.DateTimeStyles.None, data) Then
                uiDateRange.RangeAsText = data.ToString("yyyy.MM")
                Return
            End If
        End If

        mam = Regex.Match(tekst, "[12][0-9][0-9][0-9]")
        If mam.Success Then
            Dim tempInt As Integer
            If Integer.TryParse(mam.Value, tempInt) Then
                uiDateRange.RangeAsText = mam.Value

                Try

                Catch ex As Exception
                    Dim iInd As Integer = tekst.LastIndexOf(" ", mam.Index - 2)
                    Dim prevWyraz As String = tekst.Substring(iInd, mam.Index - 2 - iInd).Trim
                    Dim month As Integer = TryWyraz2Miesiac(prevWyraz)
                    If month > 0 Then
                        uiDateRange.RangeAsText = mam.Value & "." & month
                    End If
                End Try

                Return
            End If
        End If

        mam = Regex.Match(tekst, "ata [0-9][0-9]-[0-9][0-9]")
        If Not mam.Success Then mam = Regex.Match(tekst, "ata [0-9][0-9]/[0-9][0-9]")
        If mam.Success Then
            Dim tempStr As String = mam.Value.Replace("ata ", "")
            Dim rokOd, rokDo As Integer
            Integer.TryParse(tempStr.Substring(0, 2), rokOd)
            Integer.TryParse(tempStr.Substring(3, 2), rokDo)
            uiDateRange.MinDate = New Date(rokOd, 1, 1)
            uiDateRange.MinDate = (New Date(rokDo + 1, 1, 1)).AddMinutes(-1)
            Return
        End If


        mam = Regex.Match(tekst, "ata [0-9]0")
        If mam.Success Then
            Dim tempInt As Integer
            If Integer.TryParse(mam.Value.Replace("ata ", ""), tempInt) Then
                uiDateRange.RangeAsText = 1900 + tempInt * 10
                Return
            End If
        End If

        mam = Regex.Match(tekst, "ata '[0-9]0")
        If mam.Success Then
            Dim tempInt As Integer
            If Integer.TryParse(mam.Value.Replace("ata '", ""), tempInt) Then
                uiDateRange.RangeAsText = 1900 + tempInt * 10
                Return
            End If
        End If
    End Sub

    Private Function TryWyraz2Miesiac(wyraz As String) As Integer
        ' wedle liczby rzymskiej lub nazwy
        Select Case wyraz.ToLowerInvariant
            Case "stycznia", "styczeń"
                Return 1
            Case "lutego", "luty", "ii"
                Return 2
            Case "marca", "marzec", "iii"
                Return 3
            Case "kwietnia", "kwiecień", "iv"
                Return 4
            Case "maja", "maj", "v"
                Return 5
            Case "czerwca", "czerwiec", "vi"
                Return 6
            Case "lipca", "lipiec", "vii"
                Return 7
            Case "sierpnia", "sierpień", "viii"
                Return 8
            Case "września", "wrzesień", "ix"
                Return 9
            Case "października", "październik", "x"
                Return 10
            Case "listopada", "listopad", "xi"
                Return 11
            Case "grudnia", "grudzień", "xii"
                Return 12
        End Select

        Return 0
    End Function

    Private Sub SprobujRozpoznacAutora(tekst As String)
        If _autorzy Is Nothing Then Return

        For Each autor As String In _autorzy
            If tekst.Contains(autor) Then
                uiAutor.Text = autor
                Return
            End If
        Next
    End Sub


    Private Sub uiRefresh_Click(sender As Object, e As RoutedEventArgs)
        NextPic()
    End Sub

    ''' <summary>
    ''' próba odczytania ostatnio przeczytanej strony ze zdjęciem z facebook (z logu Edge) - ale nie próbuje szukać gdy src.path nie jest facebookowy
    ''' </summary>
    Private Sub TryGetLastLink()
        If Not _source.Path.Contains("facebook.com") Then Return

        Dim target As String = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
        target = target & "\Microsoft\Edge\User Data\Default\Sync Data\LevelDB\"
        Dim lastlog As String = ""
        For Each plik In IO.Directory.GetFiles(target, "*.log")
            lastlog = plik
        Next

        If lastlog = "" Then
            Vblib.DumpMessage("Nie znalazłem logu!")
            Return
        End If

        Vblib.DumpMessage("skorzystam z pliku logu " & lastlog)

        If (Date.Now - IO.File.GetLastWriteTime(lastlog)).TotalMinutes > 5 Then
            Vblib.DumpMessage("Za stary log chyba...")
            Return
        End If

        'OpenRead ma Share.Read, i wtedy jest exception :)
        Dim strumyk As IO.FileStream = IO.File.Open(lastlog, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.ReadWrite)
        Dim rider = New StreamReader(strumyk)
        Dim content As String = rider.ReadToEnd
        rider.Dispose()
        strumyk.Close()

        ' tak byc nie moze, bo sharing violation
        'Dim content As String = IO.File.ReadAllText(lastlog)
        Dim iInd As Integer = content.LastIndexOf("https://www.facebook.com/photo/?")
        If iInd < 1 Then
            uiLink.Text = "???"
            Return
        End If

        content = content.Substring(iInd)

        Dim pageaddr As String = ""
        For iLp = 0 To content.Length
            Dim znak As Char = content.Chars(iLp)
            If znak < " " Or znak > "z" Then Exit For
            pageaddr &= znak
        Next

        Vblib.DumpMessage("Ostatni URL to " & pageaddr)
        uiLink.Text = pageaddr
    End Sub

    Private Sub uiAddAuthor_Click(sender As Object, e As RoutedEventArgs)

        ' próba dopisania autora
        Dim newAutor As String = uiAutor.Text
        For Each autor As String In _autorzy
            If autor.EqualsCI(newAutor) Then Return ' już mamy
        Next

        Dim sPath As String = IO.Path.Combine(App.GetDataFolder, "inetauthors.txt")
        IO.File.AppendAllLines(sPath, {newAutor})

        _autorzy = IO.File.ReadAllLines(sPath)

    End Sub

    Private Sub uiMenuGeo_Click(sender As Object, e As RoutedEventArgs)
        uiMenuGeoMenu.IsOpen = Not uiMenuGeoMenu.IsOpen
    End Sub

    Private Async Sub uiSearchArch_Click(sender As Object, e As RoutedEventArgs)
        uiMenuGeoMenu.IsOpen = False

        Dim query As New Vblib.SearchQuery
        query.ogolne.adv.TargetDir = "inet\" & _source.SourceName
        query.ogolne.geo.AlsoEmpty = False
        query.ogolne.geo.OnlyExact = True
        query.ogolne.geo.Location = New BasicGeoposWithRadius(_lastgeo.GeoTag, 200)

        Dim _queryResults As IEnumerable(Of Vblib.OnePic) ' wynik szukania

        Me.ProgRingShow(True)

        Await Task.Run(Sub() _queryResults = Application.gDbase.Search(query))

        If _queryResults Is Nothing OrElse _queryResults.Count < 1 Then
            Me.MsgBox("Nie znalazłem takich zdjęć")
            Me.ProgRingShow(False)
            Return
        End If

        Dim lista As New Vblib.BufferFromQuery()
        For Each oPic As Vblib.OnePic In _queryResults

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

        Dim oWnd As New ProcessBrowse(lista, True, "Found")
        oWnd.Show()

    End Sub

    Private Sub uiSameGeo_Click(sender As Object, e As RoutedEventArgs)
        uiMenuGeoMenu.IsOpen = False

        _picek.ReplaceOrAddExif(_lastgeo)

        uiGeo.Content = " Change "
        uiGeo.ToolTip = _lastgeo.GeoTag.FormatLink("%lat / %lon")
    End Sub

End Class

'Imports System.Drawing
Imports pkar
Imports Vblib
Imports pkar.UI.Extensions
Imports PicSorterNS.ProcessBrowse
Imports System.Text.RegularExpressions


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

        Vblib.DumpMessage("Picek: " & uiSourceFilename.Text)

        uiAutor.Text = "" ' autora nie chcemy powtarzać
        uiDescription.Focus()

        uiSameGeo.IsEnabled = _lastgeo IsNot Nothing
    End Sub

    Private Sub uiEnd_Click(sender As Object, e As RoutedEventArgs)
        Application.GetBuffer.SaveData()
        DialogResult = True
        Me.Close()
    End Sub

    Private Async Sub uiAdd_Click(sender As Object, e As RoutedEventArgs)
        Counter += 1

        _picek.TargetDir = "inet" & _source.SourceName

        Dim srcExif As Vblib.ExifTag = _picek.GetExifOfType(Vblib.ExifSource.SourceDefault)
        srcExif.Author = uiAutor.Text
        If uiDateRange.UseMin Then srcExif.DateMin = uiDateRange.MinDate
        If uiDateRange.UseMax Then srcExif.DateMax = uiDateRange.MaxDate


        Dim descr As New OneDescription(uiDescription.Text, uiKeywords.Text)
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
    End Sub

    Private Sub uiLink_TextChanged(sender As Object, e As TextChangedEventArgs)
        Dim tekst As String = uiLink.Text
        If Not tekst.StartsWithCI("https://www.facebook.com/photo/?fbid") Then Return

        Dim iInd As Integer = tekst.IndexOf("&")
        If iInd < 1 Then Return

        uiLink.Text = tekst.Substring(0, iInd - 1)

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
                Return
            End If
        End If

        mam = Regex.Match(tekst, "ata [0-9]0")
        If mam.Success Then
            Dim tempInt As Integer
            If Integer.TryParse(mam.Value, tempInt) Then
                uiDateRange.RangeAsText = 1900 + tempInt * 10
                Return
            End If
        End If

    End Sub

    Private Sub SprobujRozpoznacAutora(tekst As String)
        If _autorzy Is Nothing Then Return

        For Each autor As String In _autorzy
            If tekst.Contains(autor) Then
                uiAutor.Text = autor
                Return
            End If
        Next
    End Sub

    Private Sub uiSameGeo_Click(sender As Object, e As RoutedEventArgs)
        _picek.ReplaceOrAddExif(_lastgeo)
        _lastgeo = Nothing
        uiSameGeo.IsEnabled = False
        uiGeo.Content = " Change "
    End Sub
End Class

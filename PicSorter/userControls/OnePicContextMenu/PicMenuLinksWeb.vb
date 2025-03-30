Imports PicSorterNS.ProcessBrowse
Imports Vblib
Imports pkar.UI.Extensions
Imports System.IO.Compression
Imports System.Text.RegularExpressions
Imports System.IO
Imports pkar.DotNetExtensions
Imports Org.BouncyCastle.Crypto.Prng
Imports pkar

Public Class PicMenuLinksWeb
    Inherits PicMenuBase

    Protected Overrides Property _minAktualne As SequenceStages = SequenceStages.CropRotate


    Private Shared _itemLinki As MenuItem
    Private Shared _miCopy As MenuItem
    Private Shared _miPaste As MenuItem
    Private Shared _miOpenReverse As MenuItem
    Private Shared _miWikiLinks As MenuItem

    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If Not String.IsNullOrWhiteSpace(Me.Header) Then Return

        MyBase.OnApplyTemplate()

        If Not InitEnableDisable("Pic links", "Operacje na linkach", True) Then Return

        Me.Items.Clear()
        AddMenuItem("Add new", "Dodaj link", AddressOf uiAddLink_Click)

        AddHandler Me.SubmenuOpened, AddressOf OpeningMenu ' .ContextMenuOpening
        _itemLinki = New MenuItem With {.Header = "Open"}
        Me.Items.Add(_itemLinki)

        _miOpenReverse = New MenuItem With {.Header = "Reverse"}
        Me.Items.Add(_miOpenReverse)

        _miWikiLinks = New MenuItem With {.Header = "geowiki " & AutotaggerBase.IconWeb}
        ToolTipService.SetShowOnDisabled(_miWikiLinks, True)
        Me.Items.Add(_miWikiLinks)

        Me.Items.Add(New Separator)
        _miCopy = AddMenuItem("Create", "Stwórz link do zaznaczonego zdjęcia", AddressOf CopyCalled)
        _miPaste = AddMenuItem("Paste", "Dodaj zapamiętany link", AddressOf PasteCalled, False)

        Me.Items.Add(New Separator)
        AddMenuItem("Połącz", "Połącz wzajemnie dwa zdjęcia", AddressOf uiWzajemnie_Click)

    End Sub

    Private _clip As OneLink

    Public Overrides Sub MenuOtwieramy()
        MyBase.MenuOtwieramy()

        ' otwieranie menu CONTEXT
        If _miCopy Is Nothing Then Return

        OpeningMenu(Nothing, Nothing)

        If Not UseSelectedItems Then Return

        If _miCopy IsNot Nothing Then _miCopy.IsEnabled = (GetSelectedItems()?.Count = 1)

    End Sub
    Private Sub OpeningMenu(sender As Object, e As RoutedEventArgs)

        ' przetworzenie Menu - dodanie linków - otwieranie menu ME
        _itemLinki.Items.Clear()
        OneOrMany(AddressOf DodajLinkizJednego)
        _itemLinki.IsEnabled = _itemLinki.Items.Count > 0

        ' obsluga wikilink
        OpeningMenuUstawWiki()

        OpeningMenuRevLink()

    End Sub


    Private Sub OpeningMenuRevLink()
        _miOpenReverse.IsEnabled = False
        If Not Application.gDbase.IsLoaded Then Return
        _miOpenReverse.IsEnabled = True
        _miOpenReverse.Items.Clear()

        ' znajdz
        OneOrMany(AddressOf DodajRevLinkizJednego)

        ' jesli nie
        If _miOpenReverse.Items.Count < 1 Then
            _miOpenReverse.Items.Add(New MenuItem With {.Header = "(empty)", .IsEnabled = False})
        End If

    End Sub

    Private Sub OpeningMenuUstawWiki()

        RemoveHandler _miWikiLinks.Click, AddressOf uiOpenWikiLink

        If UseSelectedItems Then
            _miWikiLinks.ToolTip = "działa tylko dla pojedyńczego zdjęcia"
            _miWikiLinks.IsEnabled = False
            Return
        End If

        Dim oGeo As BasicGeoposWithRadius = GetFromDataContext()?.sumOfGeo
        If oGeo Is Nothing Then
            _miWikiLinks.IsEnabled = False
            _miWikiLinks.ToolTip = "brak geotag"
            Return
        End If

        AddHandler _miWikiLinks.Click, AddressOf uiOpenWikiLink
        _miWikiLinks.IsEnabled = True
        _miWikiLinks.ToolTip = "kliknij by wczytać linki z Wikipedii"

    End Sub

    Private _lastSerNo As Integer

    Private Async Sub uiOpenWikiLink(sender As Object, e As RoutedEventArgs)
        ' kliknięto na Wikilinks

        RemoveHandler _miWikiLinks.Click, AddressOf uiOpenWikiLink

        Dim oPic As Vblib.OnePic = GetFromDataContext()
        If oPic Is Nothing Then Return

        ' jesteśmy w tym samym miejscu - czyli nie robimy skanowania Wiki
        If _lastSerNo = oPic.serno Then Return

        ' nie mamy geo, to nic nie zrobimy
        Dim oGeo As BasicGeoposWithRadius = oPic.sumOfGeo
        If oGeo Is Nothing Then Return

        _lastSerNo = oPic.serno

        _miWikiLinks.ToolTip = Nothing

        Dim radius As Integer = Vblib.GetSettingsInt("uiGeoWikiRadius")
        Dim count As Integer = Vblib.GetSettingsInt("uiGeoWikiCount")

        _miWikiLinks.Items.Clear()

        Dim langs As String() = Vblib.GetSettingsString("uiGeoWikiLangs").Split(",")
        If langs.Count > 1 Then
            For Each slang As String In langs
                Dim langmi As New MenuItem With {.Header = slang}
                _miWikiLinks.Items.Add(langmi)
                Await AddWikiLinks(slang, langmi, oGeo, radius, count)
            Next
        Else
            Await AddWikiLinks(langs(0), _miWikiLinks, oGeo, radius, count)
        End If


    End Sub

    Private Async Function AddWikiLinks(slang As String, langmi As MenuItem, oGeo As BasicGeoposWithRadius, radius As Integer, count As Integer) As Task

        langmi.Items.Clear()

        Dim lista As List(Of BasicGeopos.GeoWikiItem) =
            Await oGeo.GeoWikiGetItemsAsync(slang, radius, count, BasicGeopos.GeoWikiSort.Distance)

        If lista Is Nothing Then Return

        For Each oLink As BasicGeopos.GeoWikiItem In lista
            Dim oNew As New MenuItem
            oNew.Header = oLink.title
            oNew.DataContext = oLink.pageUri
            AddHandler oNew.Click, AddressOf uiOpenWikiThisLink
            langmi.Items.Add(oNew)
        Next


    End Function

    Private Sub uiOpenWikiThisLink(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim linek As Uri = TryCast(oFE.DataContext, Uri)
        If linek Is Nothing Then Return

        linek.OpenBrowser
    End Sub

    Private Async Sub CopyCalled()

        If GetSelectedItems().Count <> 1 Then
            Vblib.MsgBox("Umiem stworzyć link tylko dla jednego zdjęciaa")
            _miPaste.IsEnabled = False
            Return
        End If

        Dim opis As String = Await Vblib.InputBoxAsync("Podaj opis dla linku")
        If String.IsNullOrWhiteSpace(opis) Then Return

        _clip = New OneLink With {.opis = opis, .link = "pic" & GetSelectedItems(0).oPic.FormattedSerNo}
        _miPaste.IsEnabled = True
    End Sub

    Private Sub PasteCalled()
        OneOrMany(Sub(x) x.AddLink(_clip))
    End Sub


    Private Async Sub uiWzajemnie_Click(sender As Object, e As RoutedEventArgs)
        If GetSelectedItems().Count <> 2 Then
            Vblib.MsgBox("Umiem łączyć tylko dwa zdjęcia")
            Return
        End If

        Dim thumb0 = GetSelectedItems(0)
        Dim thumb1 = GetSelectedItems(1)

        Dim name0 As String = thumb0.oPic.InBufferPathName
        Dim name1 As String = thumb1.oPic.InBufferPathName

        ' najpierw przypadek gdy w nazwie któregoś występuje "rewers"

        If name0.ContainsCI("rewers") AndAlso Not name1.ContainsCI("rewers") Then
            PolaczZeSoba(thumb0, "awers", thumb1, "rewers")
            Return
        End If

        If name1.ContainsCI("rewers") AndAlso Not name0.ContainsCI("rewers") Then
            PolaczZeSoba(thumb1, "awers", thumb0, "rewers")
            Return
        End If

        ' nie mozna po nazwie, to po wielkości pliku

        If Not IO.File.Exists(thumb0.oPic.InBufferPathName) OrElse Not IO.File.Exists(thumb1.oPic.InBufferPathName) Then
            Vblib.MsgBox("Oba pliki muszą być dostępne") ' raczej się nie zdarzy, bo tylko z bufora to działa
            Return
        End If

        Dim f0 As FileInfo = New FileInfo(thumb0.oPic.InBufferPathName)
        Dim f1 As FileInfo = New FileInfo(thumb1.oPic.InBufferPathName)

        ' różnica w wielkości musi być wyraźna...
        If f0.Length * 1.5 < f1.Length Then
            PolaczZeSoba(thumb0, "foto", thumb1, "opis")
            Return
        End If

        If f1.Length * 1.5 < f0.Length Then
            PolaczZeSoba(thumb1, "foto", thumb0, "opis")
            Return
        End If

        Dim txts As String = Await Vblib.InputBoxAsync($"Podaj opis zdjęcia {thumb0.oPic.FormattedSerNo} | opis {thumb1.oPic.FormattedSerNo}")
        Dim opisy As String() = txts.Split("|")

        If opisy.Length <> 2 Then
            Vblib.MsgBox("Nie rozumiem opisu")
            Return
        End If

        PolaczZeSoba(thumb0, opisy(1), thumb0, opisy(0))

    End Sub

    ''' <summary>
    ''' uzupełnia linki o #serno, i dodaje do OnePic. Na wejściu linki mają mieć zdefiniowane opisy
    ''' </summary>
    Private Shared Sub PolaczZeSoba(thumb0 As ThumbPicek, dokad0 As String, thumb1 As ThumbPicek, dokad1 As String)
        ' # jest w Formatted
        Dim link0 As New OneLink With {.opis = dokad0, .link = "pic" & thumb1.oPic.FormattedSerNo}
        Dim link1 As New OneLink With {.opis = dokad1, .link = "pic" & thumb0.oPic.FormattedSerNo}

        thumb0.oPic.AddLink(link0)
        thumb1.oPic.AddLink(link1)
    End Sub


    Private Sub uiAddLink_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New AddLink
        If Not oWnd.ShowDialog() Then Return

        OneOrMany(Sub(x) x.AddLink(oWnd.linek))

        EventRaise(PicMenuModifies.Any)
    End Sub

    Private Sub DodajRevLinkizJednego(oPic As OnePic)

        opicserno = oPic.serno

        Dim skadlinki As IEnumerable(Of OnePic) =
            Application.gDbase.GetFirstLoaded.GetAll.Where(AddressOf DodajRevLinkizJednego_Where).
        Concat(Globs.GetBuffer.GetList.Where(AddressOf DodajRevLinkizJednego_Where))

        If Not skadlinki.Any Then Return

        For Each picekFrom As Vblib.OnePic In skadlinki
            Dim linkname As String = "pic#" & picekFrom.serno
            Dim linek As New Vblib.OneLink() With {.opis = linkname, .link = linkname}
            _miOpenReverse.Items.Add(CreateLinkMenuItem(linek))
        Next

    End Sub

    Private opicserno As Integer

    Private Function DodajRevLinkizJednego_Where(x As Vblib.OnePic)
        If x.linki Is Nothing Then Return False
        For Each linek As Vblib.OneLink In x.linki
            If Not linek.link.StartsWithCI("pic#") Then Continue For
            Dim tempno As Integer
            If Not Integer.TryParse(linek.link.AsSpan(4), tempno) Then Continue For
            If opicserno = tempno Then Return True
        Next
        Return False
    End Function

    Private Sub DodajLinkizJednego(oPic As OnePic)
        If oPic.linki IsNot Nothing Then
            For Each linek As OneLink In oPic.linki
                DodajJedenLink(linek)
            Next
        End If

        Dim alldesc As String = oPic.GetSumOfDescriptionsText
        If Not String.IsNullOrWhiteSpace(alldesc) Then

            Dim znajdy = Regex.Matches(alldesc, "http")
            Dim licznik As Integer = 0
            For Each traf As Match In znajdy
                Dim oLink As New OneLink
                oLink.opis = "from descriptions"
                If licznik > 0 Then oLink.opis &= $" ({licznik})"
                licznik += 1
                oLink.link = alldesc.Substring(traf.Index)
                Dim iInd As Integer = oLink.link.IndexOfAny({",", ";", "|", " "})
                If iInd > 0 Then oLink.link = oLink.link.Substring(0, iInd)
                DodajJedenLink(oLink)
            Next

            znajdy = Regex.Matches(alldesc, "pic#[0-9]+")
            For Each traf As Match In znajdy
                Dim oLink As New OneLink
                oLink.opis = traf.Value
                oLink.link = traf.Value
                DodajJedenLink(oLink)
            Next


        End If

    End Sub

    Private Sub DodajJedenLink(linek As OneLink)
        If linek Is Nothing Then Return
        If String.IsNullOrWhiteSpace(linek.opis) Then linek.opis = "xx"

        ' nie chcemy powtórek
        For Each oMI As MenuItem In _itemLinki.Items
            Dim dtx As OneLink = TryCast(oMI.DataContext, OneLink)
            If dtx Is Nothing Then Continue For

            If dtx.opis = linek.opis AndAlso dtx.link = linek.link Then Return
        Next

        _itemLinki.Items.Add(CreateLinkMenuItem(linek))

    End Sub

    Private Function CreateLinkMenuItem(linek As OneLink) As MenuItem
        Dim oNew As New MenuItem()
        oNew.Header = linek.opis
        oNew.DataContext = linek
        oNew.ToolTip = linek
        AddHandler oNew.Click, AddressOf UzyjLinka
        Return oNew
    End Function

    Private Sub UzyjLinka(sender As Object, e As RoutedEventArgs)
        Dim linek = TryCast(TryCast(sender, MenuItem)?.DataContext, OneLink)
        If linek Is Nothing Then Return

        If linek.link.StartsWith("pic#") Then
            ' wywołaj to zdjęcie
            WyszukajZdjecie(linek.link.Replace("pic#", ""))
        Else
            ' wywołaj stronę URL
            Dim url As New Uri(linek.link)
            url.OpenBrowser
        End If

    End Sub

    Private Async Sub WyszukajZdjecie(sernoStr As String)

        Dim serno As Integer
        Try
            serno = sernoStr
        Catch ex As Exception
            Me.MsgBox("serno nie jest liczbą?")
            Return
        End Try

        ' link do zdjęcia w default buffer
        Dim oItem As Vblib.OnePic
        oItem = vblib.GetBuffer.GetList.FirstOrDefault(Function(x) x.serno = serno)
        If oItem IsNot Nothing Then
            ' mamy zdjęcie w buforze, więc możemy pokazać
            Dim oWnd As New ShowBig(oItem, False, False)
            oWnd.Show()
            Return
        End If

        If Not Application.gDbase.IsLoaded Then
            If Not Await Me.DialogBoxYNAsync("Baza nie jest wczytana, wczytać?") Then Return
        End If
        Application.ShowWait(True)
        Application.gDbase.Load()

        Dim query As New Vblib.SearchQuery
        query.ogolne.serno = serno

        Dim listka As List(Of Vblib.OnePic) = Application.gDbase.Search(query).ToList
        Application.ShowWait(False)

        If listka Is Nothing OrElse listka.Count < 1 Then
            Me.MsgBox("Nie znalazłem takiego zdjęcia ani w buforze ani w archiwum")
            Return
        End If

        SearchWindow.PokazBigZarchive(listka(0))

    End Sub


End Class

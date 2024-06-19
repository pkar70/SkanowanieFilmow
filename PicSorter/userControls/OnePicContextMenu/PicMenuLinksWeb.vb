Imports PicSorterNS.ProcessBrowse
Imports Vblib
Imports pkar.UI.Extensions
Imports System.IO.Compression
Imports System.Text.RegularExpressions

Public Class PicMenuLinksWeb
    Inherits PicMenuBase

    Private _itemLinki As MenuItem

    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If _wasApplied Then Return

        MyBase.OnApplyTemplate()

        If Not InitEnableDisable("Pic links", "Przywołane zdjęcia", True) Then Return

        Me.Items.Clear()
        Me.Items.Add(NewMenuItem("Add new", "Dodaj zdjęcie", AddressOf uiAddLink_Click))

        AddHandler Me.SubmenuOpened, AddressOf OpeningMenu ' .ContextMenuOpening
        _itemLinki = New MenuItem With {.Header = "Open"}
        Me.Items.Add(_itemLinki)

        _wasApplied = True
    End Sub

    Private Sub uiAddLink_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New AddLink
        If Not oWnd.ShowDialog() Then Return

        OneOrMany(Sub(x) x.AddLink(oWnd.linek))

        EventRaise(Me)
    End Sub

    Private Sub OpeningMenu(sender As Object, e As RoutedEventArgs)
        ' przetworzenie Menu - dodanie linków
        _itemLinki.Items.Clear()

        OneOrMany(AddressOf DodajLinkizJednego)

        _itemLinki.IsEnabled = _itemLinki.Items.Count > 0

    End Sub

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

    Private Async Sub WyszukajZdjecie(serno As String)

        Dim oItem As Vblib.OnePic
        oItem = Application.GetBuffer.GetList.FirstOrDefault(Function(x) x.serno = serno)
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

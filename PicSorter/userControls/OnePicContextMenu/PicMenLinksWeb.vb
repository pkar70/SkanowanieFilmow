Imports PicSorterNS.ProcessBrowse
Imports Vblib
Imports pkar.UI.Extensions

Public Class PicMenLinksWeb
    Inherits PicMenuBase

    Private _itemLinki As MenuItem

    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If _wasApplied Then Return

        MyBase.OnApplyTemplate()

        If Not InitEnableDisable("Links", "Operacje na linkach", True) Then Return

        Me.Items.Clear()
        Me.Items.Add(NewMenuItem("Add new", "Dodaj link", AddressOf uiAddLink_Click))

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
            Dim iInd As Integer = alldesc.IndexOf("http")
            If iInd > 0 Then
                Dim oLink As New OneLink
                oLink.opis = "from descriptions"
                alldesc = alldesc.Substring(iInd)
                iInd = alldesc.IndexOfAny({",", ";", "|", " "})
                If iInd > 0 Then alldesc = alldesc.Substring(0, iInd)
                oLink.link = alldesc
                DodajJedenLink(oLink)
            End If
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

    Private Shared Function CreateLinkMenuItem(linek As OneLink) As MenuItem
        Dim oNew As New MenuItem()
        oNew.Header = linek.opis
        oNew.DataContext = linek
        AddHandler oNew.Click, AddressOf UzyjLinka
        Return oNew
    End Function

    Private Shared Sub UzyjLinka(sender As Object, e As RoutedEventArgs)
        Dim linek = TryCast(TryCast(sender, MenuItem)?.DataContext, OneLink)
        If linek Is Nothing Then Return

        Dim url As New Uri(linek.link)
        url.OpenBrowser
    End Sub

End Class

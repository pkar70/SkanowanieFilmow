﻿

Imports Vblib

Public NotInheritable Class PicMenuShowWiki
    Inherits PicMenuBase

    Protected Overrides Property _minAktualne As SequenceStages = SequenceStages.Keywords


    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If Not String.IsNullOrWhiteSpace(Me.Header) Then Return

        MyBase.OnApplyTemplate()

        If Not InitEnableDisable("Show wiki", "Otwarcie wikipedii", True) Then Return

        AddMenuItem("for day", "Pokaż co się działo w danym dniu", AddressOf ShowWikiDay)
        AddMenuItem("for month", "Pokaż co się działo w danym miesiącu", AddressOf ShowWikiMonth)

    End Sub


    Private Sub ShowWikiMonth(sender As Object, e As RoutedEventArgs)
        Dim data As Date = GetFromDataContext.GetMostProbablyDate

        ' https://en.wikipedia.org/wiki/January_1970
        Dim sLink As String = data.ToString("MMMM_yyyy", System.Globalization.CultureInfo.InvariantCulture)
        sLink = "https://en.wikipedia.org/wiki/" & sLink

        pkar.OpenBrowser(sLink)
    End Sub

    Private Sub ShowWikiDay(sender As Object, e As RoutedEventArgs)
        Dim data As Date = GetFromDataContext.GetMostProbablyDate

        ' https://en.wikipedia.org/wiki/January_1970
        Dim sLink As String = data.ToString("d_MMMM", System.Globalization.CultureInfo.InvariantCulture)
        sLink = "https://en.wikipedia.org/wiki/" & sLink

        pkar.OpenBrowser(sLink)
    End Sub


End Class

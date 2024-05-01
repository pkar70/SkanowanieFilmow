Imports MetadataExtractor
Imports pkar
Imports pkar.UI.Extensions
Imports Vblib

Public Class PicMenuSearchArchive
    Inherits PicMenuBase

    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If _wasApplied Then Return

        If UseSelectedItems Then Return ' umiemy tylko pojedyńczy picek

        MyBase.OnApplyTemplate()

        ' *TODO* może być Bing, Google
        If Not InitEnableDisable("Search similar", "Wyszukaj własne zdjęcia w archiwum", True) Then Return

        Me.Items.Add(NewMenuItem("Same day", "Wyszukaj zdjęcia z tego samego dnia", AddressOf SearchExactDay))
        Me.Items.Add(NewMenuItem("Date ±7 days", "Wyszukaj zdjęcia o podobnych datach", AddressOf SearchSimilarDate))
        Me.Items.Add(NewMenuItem("Same place", "Wyszukaj zdjęcia zrobione w pobliżu tego zdjęcia (1 km)", AddressOf SearchByGeo))
        Me.Items.Add(NewMenuItem("Same people", "Wyszukaj zdjęcia oznaczone tymi samymi słowami kluczowymi osób ('-')", AddressOf SearchByKwds))

        'AddHandler Me.Click, AddressOf ActionClick

        _wasApplied = True
    End Sub

    Private Sub SearchByKwds(sender As Object, e As RoutedEventArgs)
        Dim oPic As Vblib.OnePic = GetFromDataContext()
        Dim kwds As String = oPic.GetAllKeywords

        Dim arKwds As String() = kwds.Split(" ")
        Dim srchKwds As String = ""
        For Each orgKwd As String In arKwds
            If Not orgKwd.StartsWith("-") Then Continue For
            If orgKwd.StartsWith("-f") Then
                ' jeśli -f1 itp., z WinFace
                If orgKwd.Substring(2, 1) >= "0" AndAlso orgKwd.Substring(2, 1) <= "9" Then
                    Continue For
                End If
            End If
            srchKwds &= orgKwd & " "
        Next

        If srchKwds.Trim.Length < 3 Then
            Me.MsgBox("Ale to zdjęcie nie ma słów kluczowych dotyczących osób...")
            Return
        End If

        Dim qry As New Vblib.SearchQuery
        qry.ogolne.Tags = srchKwds
        Szukaj(qry)

    End Sub

    Private Sub SearchByGeo(sender As Object, e As RoutedEventArgs)
        Dim oPic As Vblib.OnePic = GetFromDataContext()
        Dim geo As BasicGeopos = oPic.GetGeoTag

        If geo Is Nothing Then
            Me.MsgBox("Ale to zdjęcie nie ma zdefiniowanego miejsca...")
            Return
        End If

        Dim qry As New Vblib.SearchQuery
        qry.ogolne.geo.Location = New BasicGeoposWithRadius(geo, 1000)
        qry.ogolne.geo.AlsoEmpty = False
        Szukaj(qry)
    End Sub

    Private Sub SearchSimilarDate(sender As Object, e As RoutedEventArgs)
        StworzQueryDlaDaty(7)
    End Sub

    Private Sub SearchExactDay(sender As Object, e As RoutedEventArgs)
        StworzQueryDlaDaty(0)
    End Sub

    Private Sub StworzQueryDlaDaty(iDaysDelta As Integer)
        Dim oPic As Vblib.OnePic = GetFromDataContext()

        If Not oPic.HasRealDate Then
            Me.MsgBox("Ale to zdjęcie nie ma określonej daty...")
            Return
        End If

        Dim kiedy As Date = oPic.GetMostProbablyDate
        Dim qry As New Vblib.SearchQuery
        qry.ogolne.MinDateCheck = True
        qry.ogolne.MinDate = kiedy.AddHours(-kiedy.Hour).AddMinutes(-kiedy.Hour)
        qry.ogolne.MaxDateCheck = True
        qry.ogolne.MaxDate = kiedy.AddHours(23 - kiedy.Hour).AddMinutes(59 - kiedy.Minute)

        qry.ogolne.MinDate = qry.ogolne.MinDate.AddDays(-iDaysDelta)
        qry.ogolne.MaxDate = qry.ogolne.MaxDate.AddDays(iDaysDelta)

        Szukaj(qry)
    End Sub

    Private Async Sub Szukaj(qry As SearchQuery)

        If Not Application.gDbase.IsLoaded Then
            If Not Await Me.DialogBoxYNAsync("Będę musiał wczytać Archiwum, to trochę zajmie. Kontynuować?") Then
                Return
            End If
        End If

        Dim oWnd As New SearchWindow(Nothing, qry)
        oWnd.Show()
    End Sub

End Class


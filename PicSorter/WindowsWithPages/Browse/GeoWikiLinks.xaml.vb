Imports pkar
Imports pkar.UI.Extensions

Public Class GeoWikiLinks
    Private Async Sub Window_DataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs)

        Await Task.Delay(20)    ' na zmianę po stronie uiPinUnpin
        ' idziemy dalej, bo czasem sam przeładowywuję (bez zmiany DataContext, jedynie przerysowanie full/ograniczone)
        If uiPinUnpin.IsPinned Then Return

        Dim oPic As Vblib.OnePic = GetOnePicFromDataContext()
        If oPic Is Nothing Then
            ' kopia z ShowExifs: to jako jedyne okno daje taki numer, że szybciej jest Window_DataContextChanged niż uiPinUnpin_DataContextChanged ...
            Await Task.Delay(10)
            oPic = GetOnePicFromDataContext()
        End If

        'uiTitle.Text = oPic.sSuggestedFilename
        If oPic Is Nothing Then Return

        UstawLanguagi(oPic)

        uiLista.ItemsSource = Nothing

        ' guzik SET przepisywanie linkow do oPic.Links

    End Sub

    Private Async Function WczytajGeoWikiLinki(oPic As Vblib.OnePic) As Task

        _itemsy.Clear()

        If oPic.sumOfGeo Is Nothing Then Return

        Dim radius As Integer = Vblib.GetSettingsInt("uiGeoWikiRadius")
        Dim maxcount As Integer = Vblib.GetSettingsInt("uiGeoWikiCount")

        Dim langsy As String() = uiCurrLangs.Text.Split(",")
        For Each lang As String In langsy

            Dim linki = Await oPic.sumOfGeo.GeoWikiGetItemsAsync(lang, radius, maxcount, BasicGeopos.GeoWikiSort.Distance)
            If linki Is Nothing Then Continue For

            For Each geowiki As BasicGeopos.GeoWikiItem In linki
                Dim onew As New WikiGeoLink(geowiki)

                ' test czy nie ma już takiego
                If oPic.linki IsNot Nothing AndAlso oPic.linki.Any(Function(x) x.link = onew.pageUri.ToString) Then
                    onew.enabled = False
                End If

                _itemsy.Add(onew)
            Next

        Next

    End Function



    Private Sub UstawLanguagi(oPic As Vblib.OnePic)
        Dim langsy As String = Vblib.GetSettingsString("uiGeoWikiLangs")

        Dim oOSM As Vblib.ExifTag = oPic.GetExifOfType(Vblib.ExifSource.AutoOSM)

        If oOSM Is Nothing Then
            uiCurrLangs.ToolTip = "(języki z Settings, bo brak AutoOSM)"
            uiCurrLangs.Text = langsy
            Return
        End If

        Dim linijki As String() = oOSM.GeoName.Split(vbCrLf)

        If linijki.Length < 2 Then
            uiCurrLangs.ToolTip = "(języki z Settings - AutoOSM wskazuje na Polskę)"
            uiCurrLangs.Text = langsy
            Return
        End If

        Dim currlang As String = KrajNaLang(linijki(1).Substring(0, 2))
        If currlang = "" Then
            uiCurrLangs.ToolTip = "(języki z Settings - nie wiem jaki jest język lokalny)"
            uiCurrLangs.Text = langsy
            Return
        End If

        If langsy.Contains(currlang) Then
            uiCurrLangs.ToolTip = "(języki z Settings - lokalny w nich się zawiera)"
            uiCurrLangs.Text = langsy
            Return
        End If

        uiCurrLangs.ToolTip = "(języki z Settings + lokalny)"
        uiCurrLangs.Text = langsy & "," & currlang

    End Sub

    Public Function KrajNaLang(countrycode As String) As String

        ' ale może próbować lang=country?

        Select Case countrycode
            Case "it", "sk", "bg", "fr", "pt", "ru", "lt", "de", "hu", "ch"
                Return countrycode
            Case "cz"
                Return "cs"
            Case "ua"
                Return "uk"
            Case "gb"
                Return "en"
            Case "by"
                Return "be"
            Case "eg"
                Return "arz"
            Case "mc"
                Return "fr"
            Case "at"
                Return "de"
            Case Else
                Return ""
        End Select

    End Function


    Private Function GetOnePicFromDataContext()
        If uiPinUnpin.EffectiveDatacontext Is Nothing Then Return Nothing
        ' próbujemy czy zadziała casting z ThumbPicek na OnePic - NIE
        If uiPinUnpin.EffectiveDatacontext.GetType Is GetType(Vblib.OnePic) Then
            Return uiPinUnpin.EffectiveDatacontext
        ElseIf uiPinUnpin.EffectiveDatacontext.GetType Is GetType(ProcessBrowse.ThumbPicek) Then
            Return TryCast(uiPinUnpin.EffectiveDatacontext, ProcessBrowse.ThumbPicek)?.oPic
        Else
            ' nieznany typ
            Return Nothing
        End If
    End Function

    Private _itemsy As New List(Of WikiGeoLink)

    Protected Class WikiGeoLink
        Public Property enabled As Boolean
        Public Property lang As String
        Public Property title As String
        Public Property pageUri As Uri

        Public Sub New(wikigeo As BasicGeopos.GeoWikiItem)
            enabled = True
            title = wikigeo.title
            pageUri = wikigeo.pageUri
            lang = wikigeo.lang
        End Sub

    End Class

    Private Async Sub uiCheck_Click(sender As Object, e As RoutedEventArgs)
        Dim oPic As Vblib.OnePic = GetOnePicFromDataContext()

        If oPic Is Nothing Then Return

        Await WczytajGeoWikiLinki(oPic)

        uiLista.ItemsSource = _itemsy

    End Sub

    Private Sub Window_KeyUp(sender As Object, e As KeyEventArgs)
        If e.IsRepeat Then Return
        If e.Key <> Key.Escape Then Return
        Me.Close()
    End Sub

    Private Sub uiOpenLink_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim linek As WikiGeoLink = TryCast(oFE?.DataContext, WikiGeoLink)

        If linek Is Nothing Then Return

        linek.pageUri.OpenBrowser()
    End Sub
End Class

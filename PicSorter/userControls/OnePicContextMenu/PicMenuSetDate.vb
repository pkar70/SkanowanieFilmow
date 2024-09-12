
Imports Vblib
Imports pkar.DotNetExtensions
Imports pkar.UI.Extensions
'Imports System.Windows.Forms.VisualStyles
'Imports PdfSharp.Snippets

Public NotInheritable Class PicMenuSetDate
    Inherits PicMenuBase

    Private Shared _clipMin As Date
    Private Shared _clipMax As Date
    Private Shared _clipOrg As Date

    'Private _itemPaste As MenuItem
    Private _itemInterpolate As MenuItem
    'Private _itemCopy As MenuItem
    Private _itemCalcDiff As MenuItem
    Private _miCopy As MenuItem
    Private _timeDiff As String
    Private _miPaste As MenuItem
    Private _miReelKwds As MenuItem
    Private _miReelKwdsDelta As MenuItem


    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If Not String.IsNullOrWhiteSpace(Me.Header) Then Return

        MyBase.OnApplyTemplate()

        If Not InitEnableDisable("Dates", "Operacje na datach", True) Then Return

        Me.Items.Clear()

        ' dla wielu: date refit (stopniowo od pierwszego do ostatniego zdjęcia)
        ' dla jednej: force date (na konkretną od-do)

        AddMenuItem("Force date taken", "Narzuca datowanie zdjęcia", AddressOf uiForceDateTaken_Click)
        AddMenuItem("Set date range", "Narzuca zakres dat zdjęcia", AddressOf uiForceDateRange_Click)

        _itemInterpolate = AddMenuItem("Interpolate", "Interpoluje datę zdjęcia z sąsiednich", AddressOf uiInterpolateDates_Click)

        _miCopy = AddMenuItem("Copy range", "Kopiuje daty wybranego pliku do lokalnego clipboard", AddressOf CopyCalled)
        _miPaste = AddMenuItem("Paste range", "Narzuca daty zdjęć wedle zapisanych w lokalnym clipboard", AddressOf PasteCalled, False)

        Dim timediff As TimeSpan = Date.Now - Date.UtcNow

        Dim tools As MenuItem = AddMenuItem("Tools", "Narzędzia poprawiania dat")
        tools.Items.Add(CreateMenuItem("To DST (+1)", "Dodaje godzinę", AddressOf uiToDST_Click))
        tools.Items.Add(CreateMenuItem("From DST (-1)", "Odejmuje godzinę", AddressOf uiFromDST_Click))
        tools.Items.Add(CreateMenuItem($"To local ({timediff.Hours})", "Zamienia czas UTC na lokalny", AddressOf uiToLocal_Click))
        tools.Items.Add(CreateMenuItem($"To universal (-{timediff.Hours})", "Zamienia czas lokalny na UTC", AddressOf uiToUTC_Click))
        tools.Items.Add(CreateMenuItem("Adjust offset", "Przesuwa czas o podane minuty", AddressOf uiAdjustOffset_Click))
        tools.Items.Add(New Separator)
        _miReelKwds = CreateMenuItem("Dates from kwds", "Ustawia datę wszystkich zdjęć wedle sumy słów kluczowych (korzysta z aktualnych dat dopisanych do słów kluczowych, może więc zostać użyte do aktualizacji)", AddressOf uiDatesFromKwds_Click)
        tools.Items.Add(_miReelKwds)
        _miReelKwdsDelta = CreateMenuItem("Dates from kwds w/Δ", "Ustawia datę wszystkich zdjęć wedle sumy słów kluczowych z uwzględnieniem marginesu błędu (korzysta z aktualnych dat dopisanych do słów kluczowych, może więc zostać użyte do aktualizacji)", AddressOf uiDatesFromKwdsDelta_Click)
        tools.Items.Add(_miReelKwdsDelta)

        _itemCalcDiff = CreateMenuItem("Calculate diff", "Wylicza różnicę czasu pomiędzy zdjęciami", AddressOf uiCalcDiff_Click)
        tools.Items.Add(_itemCalcDiff)

        MenuOtwieramy()
    End Sub

    Private Async Sub uiDatesFromKwds_Click(sender As Object, e As RoutedEventArgs)
        DatesFromKwds(0)
    End Sub

    Private Sub uiDatesFromKwdsDelta_Click(sender As Object, e As RoutedEventArgs)
        DatesFromKwds(Vblib.GetSettingsInt("uiMiesDatesFromKwds"))
    End Sub

    Private Async Sub DatesFromKwds(delta As Integer)
        If GetSelectedItemsAsPics.Any(Function(x) x.GetExifOfType(Vblib.ExifSource.ManualDate) IsNot Nothing) Then
            If Not Await Vblib.DialogBoxYNAsync("Są zdjęcia z narzuconymi datami, kontynuować?") Then Return
        End If

        Dim kwds As String = ""
        OneOrMany(Sub(x) kwds &= x.sumOfKwds & " ")

        ' sobie policzymy daty (a także GEO...)
        Dim oCalcDate As Vblib.ExifTag = Globs.GetKeywords.CreateManualTagFromKwds(kwds)
        If oCalcDate.DateMin.IsDateValid Then oCalcDate.DateMin = oCalcDate.DateMin.AddMonths(-delta)
        If oCalcDate.DateMax.IsDateValid Then oCalcDate.DateMax = oCalcDate.DateMax.AddMonths(delta)

        Dim oExif As New Vblib.ExifTag(Vblib.ExifSource.ManualDate)
        oExif.DateMin = oCalcDate.DateMin
        oExif.DateMax = oCalcDate.DateMax

        OneOrMany(Sub(x)
                      x.ReplaceOrAddExif(oExif)
                      ' x.RecalcSumsy() a to nie, bo nie ma sumy datemin/max
                  End Sub)

        EventRaise(Me)
    End Sub

    Public Overrides Sub MenuOtwieramy()
        MyBase.MenuOtwieramy()

        If _itemCalcDiff Is Nothing Then Return

        _itemCalcDiff.IsEnabled = UseSelectedItems
        _itemInterpolate.IsEnabled = UseSelectedItems
        _miReelKwds.IsEnabled = UseSelectedItems
    End Sub


    Private Sub AdjustOffset(offset As TimeSpan)
        If UseSelectedItems Then
            For Each oItem As ProcessBrowse.ThumbPicek In GetSelectedItems()
                AdjustOffsetInPic(offset, oItem)
                oItem.dateMin = oItem.oPic.GetMinDate
            Next
        Else
            AdjustOffsetInPic(offset, GetFromDataContext)
        End If

        EventRaise(Me)
    End Sub

#Region "przesuniecia dat"

    ''' <summary>
    ''' Przesuwa min/max/org* o podany offset (org: Forced, or Real)
    ''' </summary>
    ''' <param name="offset"></param>
    ''' <param name="oPic"></param>
    Private Shared Sub AdjustOffsetInPic(offset As TimeSpan, oPic As Vblib.OnePic)
        Dim datemin As DateTime = oPic.GetMinDate + offset
        Dim datemax As DateTime = oPic.GetMaxDate + offset
        Dim dateorg As DateTime = oPic.GetMostProbablyDate + offset

        Vblib.DumpMessage($"Plik: {oPic.sSuggestedFilename}")
        Vblib.DumpMessage($"DtMin/DtMax/MostProb: {oPic.GetMinDate} / {oPic.GetMaxDate} / {oPic.GetMostProbablyDate} ")
        Vblib.DumpMessage($"Offset: {offset}")

        Dim oExif As New Vblib.ExifTag(Vblib.ExifSource.ManualDate)
        If dateorg.IsDateValid Then
            oExif.DateTimeOriginal = dateorg.ToExifString
        Else
            oExif.DateMin = datemin
            oExif.DateMax = datemax
        End If

        oPic.ReplaceOrAddExif(oExif)
    End Sub

    ''' <summary>
    ''' ustaw datemin/datemax/dateorg dla zdjęcia.
    ''' Jeśli dateorg.IsValid to ono, jeśli nie, to bez DateOriginal
    ''' </summary>
    Private Shared Sub ForceInPic(oPic As Vblib.OnePic, dateMin As Date, dateMax As Date, dateOrg As Date)
        Dim oExif As New Vblib.ExifTag(Vblib.ExifSource.ManualDate)
        If dateMin.IsDateValid Then oExif.DateMin = dateMin
        If dateMax.IsDateValid Then oExif.DateMax = dateMax

        Vblib.DumpMessage($"Nowe daty: {dateMin} / {dateMax}")
        If dateOrg.IsDateValid Then
            oExif.DateTimeOriginal = dateOrg.ToExifString
            Vblib.DumpMessage($"OriginalDate: {oExif.DateTimeOriginal} ")
        End If
        oPic.ReplaceOrAddExif(oExif)
    End Sub


    Private Sub uiToUTC_Click(sender As Object, e As RoutedEventArgs)
        Dim timediff As TimeSpan = Date.Now - Date.UtcNow
        AdjustOffset(-timediff)
    End Sub

    Private Sub uiToLocal_Click(sender As Object, e As RoutedEventArgs)
        Dim timediff As TimeSpan = Date.Now - Date.UtcNow
        AdjustOffset(timediff)
    End Sub

    Private Sub uiFromDST_Click(sender As Object, e As RoutedEventArgs)
        AdjustOffset(TimeSpan.FromHours(-1))
    End Sub

    Private Sub uiToDST_Click(sender As Object, e As RoutedEventArgs)
        AdjustOffset(TimeSpan.FromHours(1))
    End Sub
#End Region


#Region "clipboard dat"

    Private Sub PasteCalled(sender As Object, e As RoutedEventArgs)

        If UseSelectedItems Then
            For Each oItem As ProcessBrowse.ThumbPicek In GetSelectedItems()
                ForceInPic(oItem, _clipMin, _clipMax, _clipOrg)
                oItem.dateMin = oItem.oPic.GetMinDate
            Next
        Else
            ForceInPic(GetFromDataContext, _clipMin, _clipMax, _clipOrg)
        End If

        EventRaise(Me)
    End Sub

    Private Sub CopyCalled(sender As Object, e As RoutedEventArgs)
        _clipMin = GetFromDataContext.GetMinDate
        _clipMax = GetFromDataContext.GetMaxDate
        _clipOrg = GetFromDataContext.GetMostProbablyDate
        _miPaste.IsEnabled = True
    End Sub
#End Region


    Private Sub uiInterpolateDates_Click(sender As Object, e As RoutedEventArgs)
        If Not UseSelectedItems Then Return

        Dim lista As List(Of ProcessBrowse.ThumbPicek) = GetSelectedItems()

        If lista.Count < 3 Then
            Vblib.DialogBox("Interpolacja dat działa tylko przy >2 zaznaczonych zdjęciach")
            Return
        End If

        Dim oPic00 As ProcessBrowse.ThumbPicek = lista(0)
        Dim oPic99 As ProcessBrowse.ThumbPicek = lista(lista.Count - 1)

        Dim date00 As Date = oPic00.oPic.GetMostProbablyDate
        Dim date99 As Date = oPic99.oPic.GetMostProbablyDate

        Dim offset As TimeSpan = TimeSpan.FromTicks(date99.Ticks - date00.Ticks)
        If offset.TotalMilliseconds < 0 Then
            Vblib.DialogBox("Jakoby daty malały, dziwna sytuacja - rezygnuję")
            Return
        End If
        If offset.TotalMinutes > 15 Then
            Vblib.DialogBox("Wypada ponad kwadrans na jedno zdjęcie - rezygnuję")
            Return
        End If

        Dim currDate As Date = date00 + offset
        For ind As Integer = 1 To lista.Count - 2
            Dim oNew As New Vblib.ExifTag(Vblib.ExifSource.ManualDate)
            oNew.DateTimeOriginal = currDate.ToExifString
            lista(ind).oPic.ReplaceOrAddExif(oNew)
            currDate += offset
        Next

        EventRaise(Me)

        ' logika poprzednio była inna - tylko na 3 obrazki, i ścinało odstępcę :)
        'If Math.Abs((date1 - date2).TotalHours) < 1 Then
        '    ' czyli date3 jest "za daleko"
        '    Dim dNew As Date = GetDateBetween(date1, date2)
        '    oNew.DateTimeOriginal = dNew.ToExifString
        '    oPic3.oPic.ReplaceOrAddExif(oNew)
        '    oPic3.dateMin = dNew
        'ElseIf Math.Abs((date2 - date3).TotalHours) < 1 Then
        '    ' czyli date1 jest "za daleko"
        '    Dim dNew As Date = GetDateBetween(date2, date3)
        '    oNew.DateTimeOriginal = dNew.ToExifString
        '    oPic1.oPic.ReplaceOrAddExif(oNew)
        '    oPic1.dateMin = dNew
        'ElseIf Math.Abs((date1 - date3).TotalHours) < 1 Then
        '    ' czyli date2 jest "za daleko"
        '    Dim dNew As Date = GetDateBetween(date1, date3)
        '    oNew.DateTimeOriginal = dNew.ToExifString
        '    oPic2.oPic.ReplaceOrAddExif(oNew)
        '    oPic2.dateMin = dNew
        'Else
        '    vb14.DialogBox("Nie ma dwu zdjęć blisko siebie, nie mam jak policzyć średniej")
        '    Return
        'End If

    End Sub

    Private Async Sub uiAdjustOffset_Click(sender As Object, e As RoutedEventArgs)
        Dim diffdef As String = ""
        If Not String.IsNullOrEmpty(_timeDiff) Then diffdef = _timeDiff

        Dim sTime As String = Await Vblib.DialogBoxInputAllDirectAsync("O ile przesunąć daty zdjęć? ([-]HH:MM, HH)", diffdef)
        If String.IsNullOrWhiteSpace(sTime) Then Return

        Dim iInd As Integer = sTime.IndexOf(":")
        Dim hr As Integer = 0
        Dim min As Integer = 0
        If iInd < 1 Then
            If Not Integer.TryParse(sTime, hr) Then
                Vblib.DialogBox("Niepoprawna liczba godzin")
                Return
            End If
        Else
            If Not Integer.TryParse(sTime.AsSpan(0, iInd), hr) Then
                Vblib.DialogBox("Niepoprawna liczba godzin")
                Return
            End If
            If Not Integer.TryParse(sTime.AsSpan(iInd + 1), min) Then
                Vblib.DialogBox("Niepoprawna liczba minut")
                Return
            End If
            If hr < 0 Then min = -min
        End If

        min = hr * 60 + min
        ' przypadek: -0:26, wtedy hr=0 a min=26, i trzeba zmienić
        If sTime.StartsWith("-") AndAlso Math.Sign(min) > 0 Then
            min = -min
        End If

        AdjustOffset(TimeSpan.FromMinutes(min))

    End Sub

    Private Sub uiCalcDiff_Click(sender As Object, e As RoutedEventArgs)
        Dim lista = GetSelectedItems()
        If lista.Count <> 2 Then
            Vblib.DialogBox("Do wyliczenia różnicy dat potrzebuję dwa i tylko dwa zdjęcia")
            Return
        End If

        Dim dt0 As Date = lista(0).oPic.GetMostProbablyDate
        Dim dt1 As Date = lista(1).oPic.GetMostProbablyDate

        Dim dtdiff As TimeSpan = dt1 - dt0
        _timeDiff = $"{dtdiff.TotalHours.Floor}:{dtdiff.Minutes}"
        Vblib.DialogBox("Różnica czasu: " & _timeDiff)

    End Sub


#Region "force date"
    Private _pickWind As Window

    Private Async Function AskCzyNaPewno(maxDays As Integer) As Task(Of Boolean)
        Return Await Vblib.DialogBoxYNAsync($"Przestawić datę o {If(UseSelectedItems, "maksymalnie ", "")}{maxDays.Abs} dni?")
    End Function

    Private Sub PokazWokienku(oStack As StackPanel, wysok As Integer)
        Dim screenPoint As Point = Me.PointToScreen(New Point(0, 0))
        _pickWind = New Window With {.Width = 80, .Height = wysok, .ResizeMode = ResizeMode.NoResize, .Left = screenPoint.X + 10, .Top = screenPoint.Y}
        _pickWind.Content = oStack

        _pickWind.ShowDialog()
    End Sub


#Region "date taken"
    Private _pickerOrig As DateTimePicker

    Private Async Sub PickerTaken_Clicked(sender As Object, e As RoutedEventArgs)
        _pickWind.Close()

        Dim dateOrg As Date = _pickerOrig.DateTime

        ' check odległości między plikiem a datą forsowaną
        Dim maxDaysFromOrg As Integer = 0
        If UseSelectedItems Then
            For Each oItem As ProcessBrowse.ThumbPicek In GetSelectedItems()
                maxDaysFromOrg = Math.Max(maxDaysFromOrg, (oItem.oPic.GetMostProbablyDate - dateOrg).TotalDays.Abs)
            Next
        Else
            maxDaysFromOrg = Math.Max(maxDaysFromOrg, (GetFromDataContext.GetMostProbablyDate - dateOrg).TotalDays.Abs)
        End If

        If Not Await AskCzyNaPewno(maxDaysFromOrg) Then Return

        ' realne przestawianie daty
        If UseSelectedItems Then
            For Each oThumb As ProcessBrowse.ThumbPicek In GetSelectedItems()
                Dim oExif As New Vblib.ExifTag(Vblib.ExifSource.ManualDate)
                oExif.DateTimeOriginal = dateOrg.ToExifString
                oThumb.oPic.ReplaceOrAddExif(oExif)
                oThumb.dateMin = oThumb.oPic.GetMostProbablyDate
            Next
        Else
            Dim oPic As Vblib.OnePic = GetFromDataContext()
            Dim oExif As New Vblib.ExifTag(Vblib.ExifSource.ManualDate)
            oExif.DateTimeOriginal = dateOrg.ToExifString
            oPic.ReplaceOrAddExif(oExif)
        End If

        EventRaise(Me)

    End Sub

    Private Sub uiForceDateTaken_Click(sender As Object, e As RoutedEventArgs)

        _pickerOrig = New DateTimePicker With {.Arrangement = Orientation.Vertical}   ' w new jest ustalenie min i max

        If UseSelectedItems Then
            ' *TODO* środek dat, albo min/max z całej listy
            Dim lista = GetSelectedItemsAsPics()
            If lista IsNot Nothing Then
                _pickerOrig.DateTime = lista(0).GetMostProbablyDate
            End If
        Else
            _pickerOrig.DateTime = GetFromDataContext.GetMostProbablyDate
        End If

        Dim oStack As New StackPanel
        oStack.Children.Add(_pickerOrig)

        Dim oButt As New Button With {.Content = " Set ", .HorizontalAlignment = HorizontalAlignment.Center}
        AddHandler oButt.Click, AddressOf PickerTaken_Clicked
        oStack.Children.Add(oButt)

        PokazWokienku(oStack, 110)
    End Sub





#End Region

#Region "daterange"

    Private _pickerRange As UserDateRange

    Private Sub uiForceDateRange_Click(sender As Object, e As RoutedEventArgs)

        ' pickery
        _pickerRange = New UserDateRange

        If UseSelectedItems Then
            ' *TODO* środek dat, albo min/max z całej listy
            Dim lista = GetSelectedItemsAsPics()
            If lista IsNot Nothing Then
                _pickerRange.MinDate = lista(0).GetMinDate
                _pickerRange.MaxDate = lista(0).GetMaxDate
            End If
        Else
            _pickerRange.MinDate = GetFromDataContext.GetMinDate
            _pickerRange.MaxDate = GetFromDataContext.GetMaxDate
        End If

        Dim oStack As New StackPanel
        oStack.Children.Add(_pickerRange)

        Dim oButt As New Button With {.Content = " Set ", .HorizontalAlignment = HorizontalAlignment.Center, .IsDefault = True}
        'oButt.Content = " Set " ' w With nie działa?
        'Dim oTxt As New TextBlock With {.Text = " Set "}
        'oButt.Content = oTxt
        AddHandler oButt.Click, AddressOf PickerRange_Clicked
        oStack.Children.Add(oButt)

        PokazWokienku(oStack, 190)

    End Sub
    Private Async Sub PickerRange_Clicked(sender As Object, e As RoutedEventArgs)
        _pickWind.Close()

        ' ale chyba zawsze będzie HasValue, bo sam ustawiam uruchamiając okienko :)
        'If Not dataMin.HasValue AndAlso Not dataMax.HasValue Then Return
        If Not _pickerRange.UseMin AndAlso Not _pickerRange.UseMax Then Return

        Dim dateMin As Date = _pickerRange.MinDate
        Dim dateMax As Date = _pickerRange.MaxDate

        ' check odległości między plikiem a datą forsowaną
        Dim maxDaysFromMin As Integer = 0
        Dim maxDaysFromMax As Integer = 0
        If UseSelectedItems Then
            For Each oItem As ProcessBrowse.ThumbPicek In GetSelectedItems()
                maxDaysFromMin = Math.Max(maxDaysFromMin, (oItem.oPic.GetMostProbablyDate - dateMin).TotalDays.Abs)
                maxDaysFromMax = Math.Max(maxDaysFromMax, (oItem.oPic.GetMostProbablyDate - dateMax).TotalDays.Abs)
            Next
        Else
            maxDaysFromMin = Math.Max(maxDaysFromMin, (GetFromDataContext.GetMostProbablyDate - dateMin).TotalDays.Abs)
            maxDaysFromMax = Math.Max(maxDaysFromMax, (GetFromDataContext.GetMostProbablyDate - dateMax).TotalDays.Abs)
        End If

        Dim maxDaysFromAll As Integer = 0
        If _pickerRange.UseMin Then
            maxDaysFromAll = maxDaysFromAll.Max(maxDaysFromMin)
        Else
            dateMin = Date.MinValue
        End If
        If _pickerRange.UseMax Then
            maxDaysFromAll = maxDaysFromAll.Max(maxDaysFromMax)
        Else
            dateMax = Date.MaxValue
        End If

        If Not Await AskCzyNaPewno(maxDaysFromAll) Then Return

        'Dim dateMax As Date = dataMin.Value.AddHours(23).AddMinutes(59)
        Dim emptydate As New Date(1500, 1, 1)

        ' realne przestawianie daty
        If UseSelectedItems Then
            For Each oItem As ProcessBrowse.ThumbPicek In GetSelectedItems()
                ForceInPic(oItem.oPic, dateMin, dateMax, emptydate)
                oItem.dateMin = oItem.oPic.GetMostProbablyDate
            Next
        Else
            ForceInPic(GetFromDataContext, dateMin, dateMax, emptydate)
        End If

        EventRaise(Me)

    End Sub

#End Region

#End Region

End Class

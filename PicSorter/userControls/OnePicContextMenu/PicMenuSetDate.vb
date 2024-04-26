
Imports Vblib
Imports pkar.DotNetExtensions
Imports pkar.UI.Extensions
Imports System.Windows.Forms.VisualStyles

Public NotInheritable Class PicMenuSetDate
    Inherits PicMenuBase

    Private Shared _clipMin As Date
    Private Shared _clipMax As Date
    Private Shared _clipOrg As Date

    Private _itemPaste As New MenuItem
    Private _timeDiff As String

    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If _wasApplied Then Return

        MyBase.OnApplyTemplate()

        If Not InitEnableDisable("Dates", True) Then Return

        Me.Items.Clear()

        ' dla wielu: date refit (stopniowo od pierwszego do ostatniego zdjęcia)
        ' dla jednej: force date (na konkretną od-do)

        Me.Items.Add(NewMenuItem("Force date range", AddressOf uiForceDate_Click))

        If UseSelectedItems Then
            Me.Items.Add(NewMenuItem("Interpolate", AddressOf uiInterpolateDates_Click))
        End If

        Me.Items.Add(NewMenuItem("Copy range", AddressOf uiDatesToClip_Click, Not UseSelectedItems))

        _itemPaste = NewMenuItem("Paste range", AddressOf uiDatesPaste_Click, _clipMin.IsDateValid OrElse _clipMax.IsDateValid)
        Me.Items.Add(_itemPaste)

        Dim timediff As TimeSpan = Date.Now - Date.UtcNow

        Dim tools As MenuItem = NewMenuItem("Tools")
        Me.Items.Add(tools)
        tools.Items.Add(NewMenuItem("To DST (+1)", AddressOf uiToDST_Click))
        tools.Items.Add(NewMenuItem("From DST (-1)", AddressOf uiFromDST_Click))
        tools.Items.Add(NewMenuItem($"To local ({timediff.Hours})", AddressOf uiToLocal_Click))
        tools.Items.Add(NewMenuItem($"To universal (-{timediff.Hours})", AddressOf uiToUTC_Click))
        tools.Items.Add(NewMenuItem("Adjust offset", AddressOf uiAdjustOffset_Click))
        If UseSelectedItems Then
            tools.Items.Add(NewMenuItem("Calculate diff", AddressOf uiCalcDiff_Click))
        End If


        _wasApplied = True
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
        oExif.DateMin = datemin
        oExif.DateMax = datemax
        oExif.DateTimeOriginal = dateorg.ToExifString

        oPic.ReplaceOrAddExif(oExif)
    End Sub

    ''' <summary>
    ''' ustaw datemin/datemax/dateorg dla zdjęcia.
    ''' Jeśli dateorg.IsValid to ono, jeśli nie, to DateOriginal jako mniejsza z tych dwu
    ''' </summary>
    Private Shared Sub ForceInPic(oPic As Vblib.OnePic, dateMin As Date, dateMax As Date, dateOrg As Date)
        Dim oExif As New Vblib.ExifTag(Vblib.ExifSource.ManualDate)
        If dateMin.IsDateValid Then oExif.DateMin = dateMin
        If dateMax.IsDateValid Then oExif.DateMax = dateMax

        Vblib.DumpMessage($"Nowe daty: {dateMin} / {dateMax}")
        If dateOrg.IsDateValid Then
            oExif.DateTimeOriginal = dateOrg
            Vblib.DumpMessage($"OriginalDate: {oExif.DateTimeOriginal} ")
        End If
        'Else
        '    oExif.DateTimeOriginal = If(dateMin < dateMax, dateMin, dateMax).ToExifString
        'End If

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

    Private Sub uiDatesPaste_Click(sender As Object, e As RoutedEventArgs)
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

    Private Sub uiDatesToClip_Click(sender As Object, e As RoutedEventArgs)
        _clipMin = GetFromDataContext.GetMinDate
        _clipMax = GetFromDataContext.GetMaxDate
        _clipOrg = GetFromDataContext.GetMostProbablyDate
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
            oNew.DateTimeOriginal = currDate
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

        AdjustOffset(TimeSpan.FromMinutes(hr * 60 + min))

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

    Private _pickWind As Window
    Private _pickerMin As DatePicker
    Private _pickerMax As DatePicker
    Private _pickerOrig As DateTimePicker
    Private _useMin As CheckBox
    Private _useMax As CheckBox
    Private _useOrg As CheckBox


    Private Sub StworzOkienko()

        ' pickery
        _pickerMin = GetNewDatePicker()
        _pickerMax = GetNewDatePicker()
        _pickerOrig = New DateTimePicker With {.Arrangement = Orientation.Vertical}   ' w new jest ustalenie min i max

        If UseSelectedItems Then
            ' *TODO* środek dat, albo min/max z całej listy
            Dim lista = GetSelectedItemsAsPics()
            If lista IsNot Nothing Then
                _pickerMin.SelectedDate = lista(0).GetMinDate
                _pickerMax.SelectedDate = lista(0).GetMaxDate
                _pickerOrig.DateTime = lista(0).GetMostProbablyDate
            End If
        Else
            _pickerMin.SelectedDate = GetFromDataContext.GetMinDate
            _pickerMax.SelectedDate = GetFromDataContext.GetMaxDate
            _pickerOrig.DateTime = GetFromDataContext.GetMostProbablyDate
        End If

        ' checkboxy używania
        _useMin = New CheckBox With {.IsChecked = True, .Content = "Data min:"}
        _useOrg = New CheckBox With {.IsChecked = True, .Content = "Data foto:"}
        _useMax = New CheckBox With {.IsChecked = True, .Content = "Data max:"}

        Dim oStack As New StackPanel
        oStack.Children.Add(_useMin)
        oStack.Children.Add(_pickerMin)

        oStack.Children.Add(_useOrg)
        oStack.Children.Add(_pickerOrig)

        oStack.Children.Add(_useMax)
        oStack.Children.Add(_pickerMax)

        Dim oButt As New Button With {.Content = " Set ", .HorizontalAlignment = HorizontalAlignment.Center}
        'oButt.Content = " Set " ' w With nie działa?
        'Dim oTxt As New TextBlock With {.Text = " Set "}
        'oButt.Content = oTxt
        AddHandler oButt.Click, AddressOf PickerOk_Clicked
        oStack.Children.Add(oButt)


        Dim screenPoint As Point = Me.PointToScreen(New Point(0, 0))
        _pickWind = New Window With {.Width = 80, .Height = 210, .ResizeMode = ResizeMode.NoResize, .Left = screenPoint.X + 10, .Top = screenPoint.Y}
        _pickWind.Content = oStack

        _pickWind.ShowDialog()
    End Sub

    Private Function GetNewDatePicker() As DatePicker
        Dim picker As New DatePicker
        picker.DisplayDateStart = New Date(1800, 1, 1)
        picker.DisplayDateEnd = Date.Now.AddHours(5)
        Return picker
    End Function

    Private Async Sub PickerOk_Clicked(sender As Object, e As RoutedEventArgs)
        _pickWind.Close()

        Dim dataMin As Date? = _pickerMin.SelectedDate
        Dim dataMax As Date? = _pickerMax.SelectedDate
        ' ale chyba zawsze będzie HasValue, bo sam ustawiam uruchamiając okienko :)
        If Not dataMin.HasValue AndAlso Not dataMax.HasValue Then Return
        If Not _useMin.IsChecked AndAlso Not _useMax.IsChecked AndAlso Not _useOrg.IsChecked Then Return

        Dim dateMin As Date = dataMin.Value
        Dim dateMax As Date = dataMax.Value
        Dim dateOrg As Date = _pickerOrig.DateTime

        ' check odległości między plikiem a datą forsowaną
        Dim maxDaysFromMin As Integer = 0
        Dim maxDaysFromMax As Integer = 0
        Dim maxDaysFromOrg As Integer = 0
        If UseSelectedItems Then
            For Each oItem As ProcessBrowse.ThumbPicek In GetSelectedItems()
                maxDaysFromMin = Math.Max(maxDaysFromMin, (oItem.oPic.GetMostProbablyDate - dateMin).TotalDays.Abs)
                maxDaysFromMax = Math.Max(maxDaysFromMax, (oItem.oPic.GetMostProbablyDate - dateMax).TotalDays.Abs)
                maxDaysFromOrg = Math.Max(maxDaysFromOrg, (oItem.oPic.GetMostProbablyDate - dateOrg).TotalDays.Abs)
            Next
        Else
            maxDaysFromMin = Math.Max(maxDaysFromMin, (GetFromDataContext.GetMostProbablyDate - dateMin).TotalDays.Abs)
            maxDaysFromMax = Math.Max(maxDaysFromMax, (GetFromDataContext.GetMostProbablyDate - dateMax).TotalDays.Abs)
            maxDaysFromOrg = Math.Max(maxDaysFromOrg, (GetFromDataContext.GetMostProbablyDate - dateOrg).TotalDays.Abs)
        End If

        Dim maxDaysFromAll As Integer = 0
        If _useMin.IsChecked Then
            maxDaysFromAll = maxDaysFromAll.Max(maxDaysFromMin)
        Else
            dateMin = Date.MinValue
        End If
        If _useMax.IsChecked Then
            maxDaysFromAll = maxDaysFromAll.Max(maxDaysFromMax)
        Else
            dateMax = Date.MaxValue
        End If
        If _useOrg.IsChecked Then
            maxDaysFromAll = maxDaysFromAll.Max(maxDaysFromOrg)
        Else
            dateOrg = Date.MinValue
        End If

        If Not Await Vblib.DialogBoxYNAsync($"Przestawić datę o maksymalnie {maxDaysFromAll} dni?") Then Return

        'Dim dateMax As Date = dataMin.Value.AddHours(23).AddMinutes(59)

        ' realne przestawianie daty
        If UseSelectedItems Then
            For Each oItem As ProcessBrowse.ThumbPicek In GetSelectedItems()
                ForceInPic(oItem.oPic, dateMin, dateMax, dateOrg)
                oItem.dateMin = oItem.oPic.GetMostProbablyDate
            Next
        Else
            ForceInPic(GetFromDataContext, dateMin, dateMax, dateOrg)
        End If

        EventRaise(Me)

    End Sub

    Private Sub uiForceDate_Click(sender As Object, e As RoutedEventArgs)
        StworzOkienko()
    End Sub
End Class

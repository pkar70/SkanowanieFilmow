
Imports Vblib

Public NotInheritable Class PicMenuSetDate
    Inherits PicMenuBase

    Private Shared _clipMin As Date
    Private Shared _clipMax As Date

    Private _itemPaste As New MenuItem


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

    Private Shared Sub AdjustOffsetInPic(offset As TimeSpan, oPic As Vblib.OnePic)
        Dim datemin As DateTime = oPic.GetMinDate + offset
        Dim datemax As DateTime = oPic.GetMaxDate + offset

        ForceInPic(oPic, datemin, datemax)
    End Sub
    Private Shared Sub ForceInPic(oPic As Vblib.OnePic, dateMin As Date, dateMax As Date)
        Dim oExif As New Vblib.ExifTag(Vblib.ExifSource.ManualDate)
        oExif.DateMin = dateMin
        oExif.DateMax = dateMax
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
                ForceInPic(oItem, _clipMin, _clipMax)
                oItem.dateMin = oItem.oPic.GetMinDate
            Next
        Else
            ForceInPic(GetFromDataContext, _clipMin, _clipMax)
        End If

        EventRaise(Me)
    End Sub

    Private Sub uiDatesToClip_Click(sender As Object, e As RoutedEventArgs)
        _clipMin = GetFromDataContext.GetMinDate
        _clipMax = GetFromDataContext.GetMaxDate
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
        Dim sTime As String = Await Vblib.DialogBoxInputAllDirectAsync("O ile przesunąć daty zdjęć? (HH:MM, HH)")
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

        AdjustOffset(TimeSpan.FromMinutes(hr * 60 + min))

    End Sub

    Private _pickWind As Window
    Private _picker As DatePicker

    Private Sub StworzOkienko()
        _picker = New DatePicker
        _picker.SelectedDate = GetFromDataContext.GetMostProbablyDate
        _picker.DisplayDateStart = New Date(1800, 1, 1)
        _picker.DisplayDateEnd = Date.Now.AddHours(5)

        Dim oStack As New StackPanel
        oStack.Children.Add(New TextBlock With {.Text = "Wybierz datę:"})
        oStack.Children.Add(_picker)

        Dim oButt As New Button With {.Content = " Set ", .HorizontalAlignment = HorizontalAlignment.Center}
        AddHandler oButt.Click, AddressOf PickerOk_Clicked
        oStack.Children.Add(New Button)

        Dim screenPoint As Point = Me.PointToScreen(New Point(0, 0))
        _pickWind = New Window With {.Width = 100, .Height = 90, .ResizeMode = ResizeMode.NoResize, .Left = screenPoint.X + 10, .Top = screenPoint.Y}
        _pickWind.Content = oStack

        _pickWind.ShowDialog()
    End Sub

    Private Async Sub PickerOk_Clicked(sender As Object, e As RoutedEventArgs)
        _pickWind.Close()

        Dim data As Date? = _picker.SelectedDate
        If Not data.HasValue Then Return

        Dim dateMin As Date = data.Value

        ' check odległości między plikiem a datą forsowaną
        Dim maxDays As Integer = 0
        If UseSelectedItems Then
            For Each oItem As ProcessBrowse.ThumbPicek In GetSelectedItems()
                maxDays = Math.Max(maxDays, Math.Abs((GetFromDataContext.GetMostProbablyDate - dateMin).TotalDays))
            Next
        Else
            maxDays = Math.Max(maxDays, Math.Abs((GetFromDataContext.GetMostProbablyDate - dateMin).TotalDays))
        End If

        If Not Await Vblib.DialogBoxYNAsync($"Przestawić datę o maksymalnie {maxDays} dni?") Then Return

        Dim dateMax As Date = data.Value.AddHours(23).AddMinutes(59)

        ' realne przestawianie daty
        If UseSelectedItems Then
            For Each oItem As ProcessBrowse.ThumbPicek In GetSelectedItems()
                ForceInPic(oItem.oPic, dateMin, dateMax)
                oItem.dateMin = oItem.oPic.GetMostProbablyDate
            Next
        Else
            ForceInPic(GetFromDataContext, dateMin, dateMax)
        End If

        EventRaise(Me)

    End Sub

    Private Sub uiForceDate_Click(sender As Object, e As RoutedEventArgs)
        StworzOkienko()
    End Sub
End Class

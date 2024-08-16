

Imports Vblib
Imports vb14 = Vblib.pkarlibmodule14
Imports pkar.DotNetExtensions
Imports pkar.UI.Configs

Public Class BrowseKeywordsWindow

    ' Private _myKeywordsList As New List(Of Vblib.OneKeyword)
    ' Private _oPic As ProcessBrowse.ThumbPicek
    Private _oNewExif As New Vblib.ExifTag(Vblib.ExifSource.ManualTag)
    Private _readonly As Boolean

    Private _recenty As New List(Of Vblib.OneKeyword)

    Private Const RECENT_LABEL As String = "(recent)"

    Public Sub New(bReadOnly As Boolean)
        Me.InitDialogs

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        _readonly = bReadOnly
    End Sub

#Region "UI events"

    Private Async Sub Window_DataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs)

        Await Task.Delay(20)    ' na zmianę po stronie uiPinUnpin

        If uiPinUnpin.IsPinned Then Return

        _oNewExif = New Vblib.ExifTag(Vblib.ExifSource.ManualTag)

        ' bo jak nie ma żadnych tagów, to nie kasował oznaczeń
        Vblib.GetKeywords.ToFlatList.ForEach(Sub(x) x.bChecked = False)

        Dim currentFlatKwds As String = uiPinUnpin.EffectiveDatacontext?.oPic.GetAllKeywords
        uiSelectedKwds.Text = If(currentFlatKwds, "")
        UstalCheckboxy(currentFlatKwds)    ' 50 ms
        ZablokujNiezgodne() ' 200 ms
        RefreshLista()      ' 60 ms

        uiEdit.IsEnabled = Not _readonly
        uiClear.IsEnabled = Not _readonly

    End Sub

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        WypelnCombo()
        uiApply.IsEnabled = Not _readonly
        uiHideKeywords.GetSettingsBool

        If uiPinUnpin.EffectiveDatacontext IsNot Nothing Then Return

        uiEdit.IsEnabled = False
        uiClear.IsEnabled = False

        For Each oItem As Vblib.OneKeyword In vblib.GetKeywords.ToFlatList
            oItem.bEnabled = True
            oItem.bChecked = False
        Next
        RefreshLista()

    End Sub

    Private Shared Sub SetGeoByKeywords(inExif As Vblib.ExifTag, fromKeywords As List(Of Vblib.OneKeyword))

        Dim iMinRadius As Integer = Integer.MaxValue

        For Each oItem As Vblib.OneKeyword In fromKeywords

            ' geo: na najbardziej ograniczony zakres (najmniejszy radius)
            If oItem.iGeoRadius > 0 Then
                If oItem.iGeoRadius > iMinRadius Then Continue For
                inExif.GeoTag = oItem.oGeo
                inExif.GeoName = oItem.sDisplayName
                iMinRadius = oItem.iGeoRadius
            End If

        Next

    End Sub
    Private Shared Sub SetDatesByKeywords(inExif As Vblib.ExifTag, fromKeywords As List(Of Vblib.OneKeyword))

        Dim oMinDate As Date = Date.MaxValue
        Dim oMaxDate As Date = Date.MinValue

        For Each oItem As Vblib.OneKeyword In fromKeywords

            oMinDate = oMinDate.Min(oItem.minDate)
            oMaxDate = oMaxDate.Max(oItem.maxDate)

        Next

        If oMaxDate.IsDateValid Then inExif.DateMax = oMaxDate
        If oMinDate.IsDateValid Then inExif.DateMin = oMinDate

    End Sub

    Private Shared Async Function SetTargetDirByKeywords(forPic As ProcessBrowse.ThumbPicek, fromKeywords As List(Of Vblib.OneKeyword)) As Task

        For Each oItem As Vblib.OneKeyword In fromKeywords
            If Not String.IsNullOrWhiteSpace(oItem.ownDir) Then
                ' podkatalog _kwd\ zapewne, ale to też zależy od Archive przecież
                Dim sTargetDir As String = oItem.ownDir
                If Not String.IsNullOrEmpty(forPic.oPic.TargetDir) Then
                    If Not Await vb14.DialogBoxYNAsync("Dotychczasowy katalog: " & forPic.oPic.TargetDir & vbCrLf & "Z keyword: " & sTargetDir) Then
                        ' teoretycznie mogłoby być Continue, bo może kolejny już tak?
                        Exit For
                    End If
                End If

                forPic.oPic.TargetDir = sTargetDir
            End If
        Next

    End Function

    Public Shared Function GetListOfSelectedKeywords() As List(Of Vblib.OneKeyword)
        Dim lKeys As New List(Of Vblib.OneKeyword)

        For Each oItem As Vblib.OneKeyword In vblib.GetKeywords.ToFlatList
            If oItem.bChecked Then lKeys.Add(oItem)
        Next


        Return lKeys

    End Function


    Public Shared Sub ApplyKeywordsToExif(oExif As Vblib.ExifTag, lKeywords As List(Of Vblib.OneKeyword))
        ' *TODO* do zamiany na KeywordsList.CreateManualTagFromKwds(string aktualnych)
        SetDatesByKeywords(oExif, lKeywords)
        SetGeoByKeywords(oExif, lKeywords)

        oExif.Keywords = ""
        oExif.UserComment = ""

        For Each oItem As OneKeyword In lKeywords
            oExif.Keywords = oExif.Keywords & " " & oItem.sId
            oExif.UserComment = oExif.UserComment & " | " & oItem.sDisplayName
        Next
    End Sub

    Private Sub uiClear_Click(sender As Object, e As RoutedEventArgs)
        For Each oItem As Vblib.OneKeyword In vblib.GetKeywords.ToFlatList
            oItem.bChecked = False
        Next
        uiSelectedKwds.Text = ""

        _oNewExif = New Vblib.ExifTag(Vblib.ExifSource.ManualTag)
        RefreshLista()
    End Sub


    Private Async Sub uiApply_Click(sender As Object, e As RoutedEventArgs)

        ' jeśli nie było obrazka startowego, to zamykamy - i można sobie wziąć listę keywordsów
        If uiPinUnpin.EffectiveDatacontext Is Nothing Then Me.Close()

        Application.ShowWait(True)

        Dim lKeys As List(Of Vblib.OneKeyword) = GetListOfSelectedKeywords()
        ApplyKeywordsToExif(_oNewExif, lKeys)

        Await SetTargetDirByKeywords(uiPinUnpin.EffectiveDatacontext, lKeys)

        ' dodaj do Recent, jeśli tam nie ma (bo .Add daje z duplikatami)
        For Each kwd As Vblib.OneKeyword In lKeys
            If Not _recenty.Contains(kwd) Then _recenty.Add(kwd)
        Next

        Dim oBrowserWnd As ProcessBrowse = Me.Owner
        If oBrowserWnd Is Nothing Then Return
        oBrowserWnd.ChangedKeywords(_oNewExif, uiPinUnpin.EffectiveDatacontext)
        _oNewExif = _oNewExif.Clone

        Application.ShowWait(False)


    End Sub

    Private Sub uiGrupy_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        RefreshLista()
    End Sub


    Private Sub RefreshLista()

        If uiGrupy.SelectedItem Is Nothing Then Return

        Dim sId As String = uiGrupy.SelectedItem.Trim

        If sId = RECENT_LABEL Then
            uiLista.ItemsSource = _recenty
            Return
        End If

        ' step 1: znajdź sId

        Dim iInd As Integer = sId.IndexOfOrdinal(" ")
        If iInd > 0 Then sId = sId.Substring(0, iInd)

        ' step 2: znajdź - ale hierarchicznie!

        If uiHideKeywords.IsChecked Then
            uiLista.ItemsSource = vblib.GetKeywords.GetKeyword(sId).ToFlatList.Where(Function(x) x.bEnabled)
        Else
            uiLista.ItemsSource = vblib.GetKeywords.GetKeyword(sId).ToFlatList
        End If
    End Sub

#End Region


    Private Sub WypelnComboRecursive(oItem As Vblib.OneKeyword, sPrefix As String)

        uiGrupy.Items.Add(sPrefix & oItem.sId & " (" & oItem.sDisplayName & ")")

        If oItem.SubItems IsNot Nothing Then
            For Each oSubItem As Vblib.OneKeyword In oItem.SubItems
                If oSubItem.SubItems Is Nothing Then Continue For
                If oSubItem.SubItems.Count < 1 Then Continue For

                WypelnComboRecursive(oSubItem, sPrefix & "  ")
            Next
        End If

    End Sub

    Private Sub WypelnCombo()
        uiGrupy.Items.Clear()

        uiGrupy.Items.Add(RECENT_LABEL)

        For Each oItem As Vblib.OneKeyword In vblib.GetKeywords
            If oItem.SubItems Is Nothing Then Continue For
            If oItem.SubItems.Count < 1 Then Continue For
            ' zostaje własna rekurencja, bo chodzi o indent w hierarchii
            WypelnComboRecursive(oItem, "")
        Next

        For Each oItem As String In uiGrupy.Items
            If oItem.Trim.StartsWithOrdinal("-RO") Then
                uiGrupy.SelectedItem = oItem
                Exit For
            End If
        Next

    End Sub

    Private Sub ZablokujNiezgodne()
        vb14.DumpCurrMethod()

        vblib.GetKeywords.EnableDisableAll(True)
        ' OdblokujWszystkie() ' 40 ms przed usunięciem DumpCurrMethod z ToFlat, 2 ms po tym
        ZablokujNiezgodneWedlePic() ' 50 ms j.w.
        ZablokujNiezgodneWedleKeywords() ' 50 ms j.w.

    End Sub

    'Private Shared Sub OdblokujWszystkie()
    '    'vb14.DumpCurrMethod()

    '    For Each oItem As Vblib.OneKeyword In Application.GetKeywords.ToFlatList
    '        oItem.bEnabled = True
    '    Next
    'End Sub

    ''' <summary>
    '''  zaznacza Keywords które są w exif.ManualTag.keywords, exif.FileExif.keywords, descriptions
    ''' </summary>
    Private Shared Sub UstalCheckboxy(sUsedTags As String)
        vb14.DumpCurrMethod()

        If String.IsNullOrWhiteSpace(sUsedTags) Then Return

        ' Dim sUsedTags As String = _oPic.oPic.GetAllKeywords ' ""

        'For Each oExifTag As Vblib.ExifTag In _oPic.oPic.Exifs
        '    If Not String.IsNullOrWhiteSpace(oExifTag.Keywords) Then
        '        sUsedTags = sUsedTags & " " & oExifTag.Keywords
        '    End If
        'Next

        'If _oPic.oPic.descriptions IsNot Nothing Then
        '    For Each oDesc As OneDescription In _oPic.oPic.descriptions
        '        sUsedTags = sUsedTags & " " & oDesc.keywords & " "
        '    Next
        'End If

        sUsedTags = sUsedTags.Replace("  ", " ")

        Dim aKwds As String() = sUsedTags.Split(" ")

        ' rekurencyjnie przez wszystkie itemy w Keywords, zaznacz odznacz
        'For Each oItem As Vblib.OneKeyword In Application.GetKeywords.GetList
        '    UstalCheckboxyRecursive(oItem, aKwds)
        'Next

        For Each oItem As Vblib.OneKeyword In vblib.GetKeywords.ToFlatList
            oItem.bChecked = False
            For Each sTag As String In aKwds
                If oItem.sId = sTag Then
                    oItem.bChecked = True
                    Exit For
                End If
            Next
        Next

    End Sub

    Private Sub ZablokujNiezgodneWedlePic()
        vb14.DumpCurrMethod()

        If uiPinUnpin?.EffectiveDatacontext?.oPic Is Nothing Then Return

        Dim minDate, maxdate As Date

        If uiPinUnpin.EffectiveDatacontext.oPic.HasRealDate Then
            minDate = uiPinUnpin.EffectiveDatacontext.oPic.GetMostProbablyDate.adddays(-1)
            maxdate = minDate.AddDays(2)
        Else
            minDate = uiPinUnpin.EffectiveDatacontext.oPic.GetMinDate
            maxdate = uiPinUnpin.EffectiveDatacontext.oPic.GetMaxDate
        End If


        For Each oItem As Vblib.OneKeyword In vblib.GetKeywords.ToFlatList
            ZablokujNiezgodneWedleDat(oItem, minDate, maxdate)
        Next


    End Sub

    Private Sub ZablokujNiezgodneWedleKeywords()
        'vb14.DumpCurrMethod()

        Dim minDate As Date = Date.MaxValue
        Dim maxDate As Date = Date.MinValue

        For Each oItem As Vblib.OneKeyword In vblib.GetKeywords
            minDate = SzukajMinDatyRecursive(oItem, minDate)
        Next

        For Each oItem As Vblib.OneKeyword In vblib.GetKeywords
            maxDate = SzukajMaxDatyRecursive(oItem, maxDate)
        Next
        vb14.DumpMessage($"daty: {minDate} .. {maxDate}")

        For Each oItem As Vblib.OneKeyword In vblib.GetKeywords.ToFlatList
            ZablokujNiezgodneWedleDat(oItem, minDate, maxDate)
        Next

    End Sub

    Private Function SzukajMinDatyRecursive(oItem As Vblib.OneKeyword, minDate As Date) As Date
        'vb14.DumpCurrMethod()

        If oItem.SubItems IsNot Nothing Then
            For Each oSubItem As Vblib.OneKeyword In oItem.SubItems
                Dim currMinDate As Date = SzukajMinDatyRecursive(oSubItem, minDate)
                minDate = minDate.Min(currMinDate)
                'If currMinDate.Year > 1800 Then
                '    If currMinDate < minDate Then minDate = currMinDate
                'End If
            Next
        End If

        If oItem.minDate < minDate Then Return oItem.minDate
        Return minDate
    End Function



    Private Function SzukajMaxDatyRecursive(oItem As Vblib.OneKeyword, maxDate As Date) As Date
        'vb14.DumpCurrMethod()

        If oItem.SubItems IsNot Nothing Then
            For Each oSubItem As Vblib.OneKeyword In oItem.SubItems
                Dim currMaxDate As Date = SzukajMaxDatyRecursive(oSubItem, maxDate)
                maxDate = maxDate.Max(currMaxDate)
            Next
        End If

        If oItem.maxDate < maxDate AndAlso oItem.maxDate.IsDateValid Then Return oItem.maxDate
        If Not maxDate.IsDateValid Then Return Date.MinValue

        Return maxDate
    End Function


    Private Shared Sub ZablokujNiezgodneWedleDat(oItem As Vblib.OneKeyword, minDate As Date, maxDate As Date)
        'vb14.DumpMessage($"oItem: {oItem.sId}, minDate: {minDate}, maxDate: {maxDate}")

        ' step 1: czy węzeł ma limit dat?

        If minDate.IsDateValid Then
            If oItem.maxDate.IsDateValid Then
                If oItem.maxDate < minDate Then oItem.bEnabled = False
            End If
        End If

        If maxDate.IsDateValid Then
            If oItem.minDate.IsDateValid Then
                If oItem.minDate > maxDate Then oItem.bEnabled = False
            End If
        End If

    End Sub

    Private Sub uiEditKeyTree_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New SettingsKeywords
        AddHandler oWnd.Closed, AddressOf ZmianyKiwords

        oWnd.ShowDialog()

        ' po co była ta linia?
        'Me.DataContext = uiPinUnpin.EffectiveDatacontext
        ' InitForPic(_oPic)
    End Sub

    Private Sub ZmianyKiwords(sender As Object, e As EventArgs)
        Window_Loaded(Nothing, Nothing)
        Window_DataContextChanged(Nothing, Nothing)
    End Sub

    Private Sub uiZmianaCheck(sender As Object, e As RoutedEventArgs)
        Dim currentFlatKwds As String = ""
        vblib.GetKeywords.ToFlatList.ForEach(Sub(x) If x.bChecked Then currentFlatKwds &= x.sId & " ")
        uiSelectedKwds.Text = currentFlatKwds
    End Sub

    Private Sub uiHideKeywords_Checked(sender As Object, e As RoutedEventArgs)
        RefreshLista()
    End Sub
End Class

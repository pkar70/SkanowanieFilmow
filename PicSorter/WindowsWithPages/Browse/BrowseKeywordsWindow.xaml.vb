

Imports System.DirectoryServices.ActiveDirectory
Imports System.Security.Cryptography
Imports System.Security.Policy
Imports MetadataExtractor.Formats
Imports Vblib
Imports vb14 = Vblib.pkarlibmodule14


Public Class BrowseKeywordsWindow

    ' Private _myKeywordsList As New List(Of Vblib.OneKeyword)
    Private _oPic As ProcessBrowse.ThumbPicek
    Private _oNewExif As New Vblib.ExifTag(Vblib.ExifSource.ManualTag)


#Region "UI events"

    Public Sub InitForPic(oPic As ProcessBrowse.ThumbPicek)
        If oPic Is Nothing Then Return
        _oPic = oPic
        Me.Title = oPic.oPic.InBufferPathName

        _oNewExif = New Vblib.ExifTag(Vblib.ExifSource.ManualTag)

        UstalCheckboxy()    ' 50 ms
        ZablokujNiezgodne() ' 200 ms
        RefreshLista()      ' 60 ms
    End Sub

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        WypelnCombo()
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

            oMinDate = oMinDate.DateMin(oItem.minDate)
            oMaxDate = oMaxDate.DateMax(oItem.maxDate)

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

    Private Shared Function GetListOfSelectedKeywords() As List(Of Vblib.OneKeyword)
        Dim lKeys As New List(Of Vblib.OneKeyword)

        For Each oItem As Vblib.OneKeyword In Application.GetKeywords.ToFlatList
            If oItem.bChecked Then lKeys.Add(oItem)
        Next


        Return lKeys

    End Function

    Private Async Sub uiApply_Click(sender As Object, e As RoutedEventArgs)

        Dim lKeys As List(Of Vblib.OneKeyword) = GetListOfSelectedKeywords()

        SetDatesByKeywords(_oNewExif, lKeys)
        SetGeoByKeywords(_oNewExif, lKeys)
        Await SetTargetDirByKeywords(_oPic, lKeys)

        _oNewExif.Keywords = ""
        _oNewExif.UserComment = ""

        For Each oItem As OneKeyword In lKeys
            _oNewExif.Keywords = _oNewExif.Keywords & " " & oItem.sId
            _oNewExif.UserComment = _oNewExif.UserComment & " | " & oItem.sDisplayName
        Next


        Dim oBrowserWnd As ProcessBrowse = Me.Owner
            If oBrowserWnd Is Nothing Then Return
        oBrowserWnd.ChangedKeywords(_oNewExif, _oPic)

    End Sub

    Private Sub uiGrupy_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        RefreshLista()
    End Sub


    Private Sub RefreshLista()

        If uiGrupy.SelectedItem Is Nothing Then Return
        ' step 1: znajdź sId
        Dim sId As String = uiGrupy.SelectedItem.Trim
        Dim iInd As Integer = sId.IndexOf(" ")
        If iInd > 0 Then sId = sId.Substring(0, iInd)

        ' step 2: znajdź - ale hierarchicznie!
        uiLista.ItemsSource = Application.GetKeywords.GetKeyword(sId).ToFlatList

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

        For Each oItem As Vblib.OneKeyword In Application.GetKeywords.GetList
            If oItem.SubItems Is Nothing Then Continue For
            If oItem.SubItems.Count < 1 Then Continue For
            ' zostaje własna rekurencja, bo chodzi o indent w hierarchii
            WypelnComboRecursive(oItem, "")
        Next

        For Each oItem As String In uiGrupy.Items
            If oItem.Trim.StartsWith("-RO") Then
                uiGrupy.SelectedItem = oItem
                Exit For
            End If
        Next

    End Sub

    Private Sub ZablokujNiezgodne()
        vb14.DumpCurrMethod()

        Application.GetKeywords.EnableDisableAll(True)
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
    Private Sub UstalCheckboxy()
        vb14.DumpCurrMethod()

        Dim sUsedTags As String = ""

        For Each oExifTag As Vblib.ExifTag In _oPic.oPic.Exifs
            If Not String.IsNullOrWhiteSpace(oExifTag.Keywords) Then
                sUsedTags = sUsedTags & " " & oExifTag.Keywords
            End If
        Next

        If _oPic.oPic.descriptions IsNot Nothing Then
            For Each oDesc As OneDescription In _oPic.oPic.descriptions
                sUsedTags = sUsedTags & " " & oDesc.keywords & " "
            Next
        End If

        sUsedTags = sUsedTags.Replace("  ", " ")

        Dim aKwds As String() = sUsedTags.Split(" ")

        ' rekurencyjnie przez wszystkie itemy w Keywords, zaznacz odznacz
        'For Each oItem As Vblib.OneKeyword In Application.GetKeywords.GetList
        '    UstalCheckboxyRecursive(oItem, aKwds)
        'Next

        For Each oItem As Vblib.OneKeyword In Application.GetKeywords.ToFlatList
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

        Dim minDate As Date = _oPic.oPic.GetMinDate ' Date.MaxValue
        Dim maxDate As Date = _oPic.oPic.GetMaxDate ' Date.MinValue

        For Each oItem As Vblib.OneKeyword In Application.GetKeywords.ToFlatList
            ZablokujNiezgodneWedleDat(oItem, minDate, maxDate)
        Next


    End Sub

    Private Sub ZablokujNiezgodneWedleKeywords()
        'vb14.DumpCurrMethod()

        Dim minDate As Date = Date.MaxValue
        Dim maxDate As Date = Date.MinValue

        For Each oItem As Vblib.OneKeyword In Application.GetKeywords.GetList
            minDate = SzukajMinDatyRecursive(oItem, minDate)
        Next

        For Each oItem As Vblib.OneKeyword In Application.GetKeywords.GetList
            maxDate = SzukajMaxDatyRecursive(oItem, maxDate)
        Next
        vb14.DumpMessage($"daty: {minDate} .. {maxDate}")

        For Each oItem As Vblib.OneKeyword In Application.GetKeywords.ToFlatList
            ZablokujNiezgodneWedleDat(oItem, minDate, maxDate)
        Next

    End Sub

    Private Function SzukajMinDatyRecursive(oItem As Vblib.OneKeyword, minDate As Date) As Date
        'vb14.DumpCurrMethod()

        If oItem.SubItems IsNot Nothing Then
            For Each oSubItem As Vblib.OneKeyword In oItem.SubItems
                Dim currMinDate As Date = SzukajMinDatyRecursive(oSubItem, minDate)
                minDate = minDate.DateMin(currMinDate)
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
                maxDate = maxDate.DateMax(currMaxDate)
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
        oWnd.ShowDialog()
        InitForPic(_oPic)
    End Sub


End Class

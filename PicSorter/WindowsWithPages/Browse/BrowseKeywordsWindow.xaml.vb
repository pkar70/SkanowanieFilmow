

Imports System.DirectoryServices.ActiveDirectory
Imports System.Security.Cryptography
Imports Vblib
Imports vb14 = Vblib.pkarlibmodule14


Public Class BrowseKeywordsWindow

    ' Private _myKeywordsList As New List(Of Vblib.OneKeyword)
    Private _oPic As ProcessBrowse.ThumbPicek
    Private _oNewExif As New Vblib.ExifTag(Vblib.ExifSource.ManualTag)

#Region "UI events"

    Public Sub InitForPic(oPic As ProcessBrowse.ThumbPicek)
        _oPic = oPic
        Me.Title = oPic.oPic.InBufferPathName

        UstalCheckboxy()
        ZablokujNiezgodne()
        RefreshLista()
    End Sub

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        WypelnCombo()
    End Sub

    Private Sub uiApply_Click(sender As Object, e As RoutedEventArgs)

        _oNewExif.Keywords = ""
        _oNewExif.UserComment = ""

        Dim oMinDate As Date = Date.MaxValue
        Dim oMaxDate As Date = Date.MinValue

        For Each oItem As Vblib.OneKeyword In Application.GetKeywords.GetList
            If oItem.bChecked Then
                _oNewExif.Keywords = _oNewExif.Keywords & " " & oItem.sTagId
                _oNewExif.UserComment = _oNewExif.UserComment & " | " & oItem.sDisplayName

                If oItem.minDate.Year > 1800 Then
                    If oItem.minDate < oMinDate Then oMinDate = oItem.minDate
                End If

                If oItem.maxDate.Year > 1800 Then
                    If oItem.maxDate > oMaxDate Then oMaxDate = oItem.maxDate
                End If

            End If
        Next

        If oMaxDate.Year < 1800 Then oMaxDate = Date.Now

        _oNewExif.DateMin = oMinDate
        _oNewExif.DateMax = oMaxDate

        _oPic.oPic.ReplaceOrAddExif(_oNewExif)

        _oPic.oPic.RemoveFromDescriptions(_oNewExif.Keywords, Application.GetKeywords)

        Application.GetBuffer.SaveData()
    End Sub

    Private Sub uiGrupy_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        RefreshLista()
    End Sub


    Private Sub RefreshListaAddRecursive(oNewList As List(Of Vblib.OneKeyword), oItem As Vblib.OneKeyword)

        oNewList.Add(oItem)
        If oItem.SubItems IsNot Nothing Then
            For Each oSubItem As Vblib.OneKeyword In oItem.SubItems
                RefreshListaAddRecursive(oNewList, oSubItem)
            Next
        End If
    End Sub

    Private Function RefreshListaRecursive(sId As String, oItem As Vblib.OneKeyword) As Boolean

        If oItem.sTagId = sId Then
            Dim oNewList As New List(Of Vblib.OneKeyword)
            RefreshListaAddRecursive(oNewList, oItem)
            uiLista.ItemsSource = oNewList
            Return True
        End If

        If oItem.SubItems IsNot Nothing Then
            For Each oSubItem As Vblib.OneKeyword In oItem.SubItems
                If RefreshListaRecursive(sId, oSubItem) Then Return True
            Next
        End If

        Return False
    End Function
    Private Sub RefreshLista()

        ' step 1: znajdź sTagId
        ' Dim oMI As MenuItem = uiGrupy.SelectedItem
        Dim sId As String = uiGrupy.SelectedItem.Trim
        Dim iInd As Integer = sId.IndexOf(" ")
        If iInd > 0 Then sId = sId.Substring(0, iInd)

        ' step 2: znajdź - ale hierarchicznie!
        For Each oItem As Vblib.OneKeyword In Application.GetKeywords.GetList
            If RefreshListaRecursive(sId, oItem) Then Exit For
        Next

    End Sub

#End Region


    Private Sub WypelnComboRecursive(oItem As Vblib.OneKeyword, sPrefix As String)

        uiGrupy.Items.Add(sPrefix & oItem.sTagId & " (" & oItem.sDisplayName & ")")

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

        OdblokujWszystkie()
        ZablokujNiezgodneWedlePic()
        ZablokujNiezgodneWedleKeywords()

    End Sub

    Private Sub OdblokujWszystkie()
        'vb14.DumpCurrMethod()

        For Each oItem As Vblib.OneKeyword In Application.GetKeywords.GetList
            OdblokujWszystkieRecursive(oItem)
        Next

    End Sub

    Private Sub OdblokujWszystkieRecursive(oItem As Vblib.OneKeyword)
        'vb14.DumpCurrMethod()

        If oItem.SubItems IsNot Nothing Then
            For Each oSubItem As Vblib.OneKeyword In oItem.SubItems
                OdblokujWszystkieRecursive(oSubItem)
            Next
        End If

        oItem.bEnabled = True

    End Sub

    Private Sub UstalCheckboxy()
        'vb14.DumpCurrMethod()

        Dim sUsedTags As String = ""

        Dim oExifTag As Vblib.ExifTag = _oPic.oPic.GetExifOfType(Vblib.ExifSource.ManualTag)
        If oExifTag IsNot Nothing Then sUsedTags = oExifTag.Keywords

        oExifTag = _oPic.oPic.GetExifOfType(Vblib.ExifSource.FileExif)
        If oExifTag IsNot Nothing Then sUsedTags = sUsedTags & " " & oExifTag.Keywords

        If _oPic.oPic.descriptions IsNot Nothing Then
            For Each oDesc As OneDescription In _oPic.oPic.descriptions
                sUsedTags = sUsedTags & " " & oDesc.keywords & " "
            Next
        End If

        sUsedTags = sUsedTags.Replace("  ", " ")

        Dim aKwds As String() = sUsedTags.Split(" ")

        ' rekurencyjnie przez wszystkie itemy w Keywords, zaznacz odznacz
        For Each oItem As Vblib.OneKeyword In Application.GetKeywords.GetList
            UstalCheckboxyRecursive(oItem, aKwds)
        Next

    End Sub

    Private Sub UstalCheckboxyRecursive(oItem As Vblib.OneKeyword, aKwds As String())
        'vb14.DumpCurrMethod()

        If oItem.SubItems IsNot Nothing Then
            For Each oSubItem As Vblib.OneKeyword In oItem.SubItems
                UstalCheckboxyRecursive(oSubItem, aKwds)
            Next
        End If

        oItem.bChecked = False
        For Each sTag As String In aKwds
            If oItem.sTagId = sTag Then
                oItem.bChecked = True
                Exit For
            End If
        Next

    End Sub

    Private Sub ZablokujNiezgodneWedlePic()
        vb14.DumpCurrMethod()

        Dim minDate As Date = Date.MaxValue
        Dim maxDate As Date = Date.MinValue

        ' policz dateMin i dateMax dla zdjecia - SOURCE, EXIF, MANUALDATE
        Dim oExifTag As Vblib.ExifTag = _oPic.oPic.GetExifOfType(Vblib.ExifSource.SourceDefault)
        If oExifTag IsNot Nothing Then
            vb14.DumpMessage("mam EXIF SOURCE")
            If oExifTag.DateMin.Year > 1800 Then
                If minDate < oExifTag.DateMin Then minDate = oExifTag.DateMin
            End If
            If oExifTag.DateMax.Year > 1800 AndAlso oExifTag.DateMax.Year < 2100 Then
                If maxDate > oExifTag.DateMax Then maxDate = oExifTag.DateMax
            End If
            vb14.DumpMessage($"daty: {minDate} .. {maxDate}")
        End If

        oExifTag = _oPic.oPic.GetExifOfType(Vblib.ExifSource.FileExif)
        If oExifTag IsNot Nothing Then
            vb14.DumpMessage("mam EXIF EXIF")
            If oExifTag.DateMin.Year > 1800 Then
                If minDate < oExifTag.DateMin Then minDate = oExifTag.DateMin
            End If
            If oExifTag.DateMax.Year > 1800 AndAlso oExifTag.DateMax.Year < 2100 Then
                If maxDate > oExifTag.DateMax Then maxDate = oExifTag.DateMax
            End If
            vb14.DumpMessage($"daty: {minDate} .. {maxDate}")
        End If

        oExifTag = _oPic.oPic.GetExifOfType(Vblib.ExifSource.ManualDate)
        If oExifTag IsNot Nothing Then
            'vb14.DumpMessage("mam EXIF MANUAL")
            If oExifTag.DateMin.Year > 1800 Then
                If minDate < oExifTag.DateMin Then minDate = oExifTag.DateMin
            End If
            If oExifTag.DateMax.Year > 1800 AndAlso oExifTag.DateMax.Year < 2100 Then
                If maxDate > oExifTag.DateMax Then maxDate = oExifTag.DateMax
            End If
            vb14.DumpMessage($"daty: {minDate} .. {maxDate}")
        End If

        ' rekurencyjnie przez wszystkie itemy w Keywords, zaznacz odznacz
        For Each oItem As Vblib.OneKeyword In Application.GetKeywords.GetList
            ZablokujNiezgodneWedleDatRecursive(oItem, minDate, maxDate)
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

        ' rekurencyjnie przez wszystkie itemy w Keywords, zaznacz odznacz
        For Each oItem As Vblib.OneKeyword In Application.GetKeywords.GetList
            ZablokujNiezgodneWedleDatRecursive(oItem, minDate, maxDate)
        Next

    End Sub

    'Private Function SzukajMinDaty(oItem As OneKeyword, minDate As Date)

    '    For Each oItem As Vblib.OneKeyword In Application.GetKeywords.GetList
    '        Dim currMinDate As Date = SzukajMinDatyRecursive(oItem)
    '        If currMinDate.Year > 1800 Then
    '            If currMinDate > minDate Then minDate = currMinDate
    '        End If
    '    Next

    '    Return minDate
    'End Function

    Private Function SzukajMinDatyRecursive(oItem As Vblib.OneKeyword, minDate As Date) As Date
        'vb14.DumpCurrMethod()

        If oItem.SubItems IsNot Nothing Then
            For Each oSubItem As Vblib.OneKeyword In oItem.SubItems
                Dim currMinDate As Date = SzukajMinDatyRecursive(oSubItem, minDate)
                If currMinDate.Year > 1800 Then
                    If currMinDate < minDate Then minDate = currMinDate
                End If
            Next
        End If

        If oItem.minDate < minDate Then Return oItem.minDate
        Return minDate
    End Function

    'Private Function SzukajMaxDaty()
    '    Dim maxDate As Date = Date.MinValue

    '    For Each oItem As Vblib.OneKeyword In Application.GetKeywords.GetList
    '        Dim currMaxDate As Date = SzukajMaxDatyRecursive(oItem)
    '        If currMaxDate.Year > 1800 Then
    '            If currMaxDate > maxDate Then maxDate = currMaxDate
    '        End If
    '    Next

    '    If maxDate.Year > 2100 Then Return Date.MinValue
    '    Return maxDate
    'End Function

    Private Function SzukajMaxDatyRecursive(oItem As Vblib.OneKeyword, maxDate As Date) As Date
        'vb14.DumpCurrMethod()

        'Dim maxDate As Date = Date.MinValue

        If oItem.SubItems IsNot Nothing Then
            For Each oSubItem As Vblib.OneKeyword In oItem.SubItems
                Dim currMaxDate As Date = SzukajMaxDatyRecursive(oSubItem, maxDate)
                If currMaxDate.Year > 1800 AndAlso currMaxDate.Year < 2100 Then
                    If currMaxDate > maxDate Then maxDate = currMaxDate
                End If
            Next
        End If

        If oItem.maxDate < maxDate AndAlso oItem.maxDate.Year > 1800 Then Return oItem.maxDate
        If maxDate.Year > 2100 Then Return Date.MinValue

        Return maxDate
    End Function


    Private Sub ZablokujNiezgodneWedleDatRecursive(oItem As Vblib.OneKeyword, minDate As Date, maxDate As Date)
        'vb14.DumpMessage($"oItem: {oItem.sTagId}, minDate: {minDate}, maxDate: {maxDate}")

        ' step 1: czy węzeł ma limit dat?

        If minDate.Year > 1800 Then
            If oItem.maxDate.Year > 1800 AndAlso oItem.maxDate.Year < 2100 Then
                If oItem.maxDate < minDate Then oItem.bEnabled = False
            End If
        End If

        If maxDate.Year > 1800 AndAlso maxDate.Year < 2100 Then
            If oItem.minDate.Year > 1800 Then
                If oItem.minDate > maxDate Then oItem.bEnabled = False
            End If
        End If

        ' step 2: iterowanie subitemow
        If oItem.SubItems IsNot Nothing Then
            For Each oSubItem As Vblib.OneKeyword In oItem.SubItems
                ZablokujNiezgodneWedleDatRecursive(oSubItem, minDate, maxDate)
            Next
        End If

    End Sub







End Class

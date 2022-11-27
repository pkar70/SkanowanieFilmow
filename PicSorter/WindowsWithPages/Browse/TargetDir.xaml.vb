

' 2022.11.23.sb.A_geo[space]remark

'Imports System.Collections.ObjectModel

Imports System.Security.Policy
Imports Windows.Devices

Public Class TargetDir

    Private _thumbsy As List(Of ProcessBrowse.ThumbPicek)
    Private _selected As List(Of ProcessBrowse.ThumbPicek)

    Public Sub New(wholeList As List(Of ProcessBrowse.ThumbPicek), selectedList As List(Of ProcessBrowse.ThumbPicek))

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.

        _thumbsy = wholeList
        _selected = selectedList

    End Sub

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)

        Dim sFirstItemFilename As String = _selected(0).oPic.InBufferPathName
        Dim iFirstPtr As Integer = -1
        For iLp As Integer = 0 To _thumbsy.Count
            If _thumbsy(iLp).oPic.InBufferPathName = sFirstItemFilename Then
                iFirstPtr = iLp
                Exit For
            End If
        Next

        If iFirstPtr < 0 Then Return ' nie powinno się zdarzyć

        PokazOpcjeCzasowe(iFirstPtr)
        PokazOpcjeGeo(iFirstPtr)
        PokazIstniejaceKatalogi(_selected(0).dateMin, _selected(_selected.Count - 1).dateMin)

    End Sub


    Private Function KatalogiWgDaty(aboutDateOd As Date, aboutDateDo As Date) As List(Of String)

        Dim lLista As New List(Of String) ' poprzez listę pośrednią, bo chodzi o sortowanie

        Dim sDataOd As String = Vblib.OneDir.DateToDirId(aboutDateOd.AddDays(-5))
        Dim sDataDo As String = Vblib.OneDir.DateToDirId(aboutDateDo.AddDays(5))

        ' teraz mozna wedle stringow
        For Each oDir As Vblib.OneDir In Application.GetDirList.GetList
            ' If oDir.sId.StartsWith("_") Then lLista.Add(oDir.ToComboDisplayName)
            If oDir.sId > sDataOd AndAlso oDir.sId < sDataDo Then lLista.Add(oDir.ToComboDisplayName)
        Next

        Return lLista
    End Function

    Private Sub KatalogiWgKeywordRecursive(oKwd As Vblib.OneKeyword, lista As List(Of String))
        If oKwd.SubItems Is Nothing Then Return

        For Each oChild As Vblib.OneKeyword In oKwd.SubItems
            If oChild.hasFolder Then lista.Add(oChild.ToComboDisplayName)
            KatalogiWgKeywordRecursive(oChild, lista)
        Next

    End Sub

    Private Function KatalogiWgKeyword() As List(Of String)
        Dim lLista As New List(Of String) ' poprzez listę pośrednią, bo chodzi o sortowanie

        For Each oKwd As Vblib.OneKeyword In Application.GetKeywords.GetList
            If oKwd.hasFolder Then lLista.Add(oKwd.ToComboDisplayName)
            KatalogiWgKeywordRecursive(oKwd, lLista)
        Next

        Return lLista

    End Function

    Private Sub PokazIstniejaceKatalogi(aboutDateOd As Date, aboutDateDo As Date)

        Dim lLista As List(Of String) = KatalogiWgDaty(aboutDateOd, aboutDateDo)

        For Each sId As String In From c In lLista Order By c
            uiComboExisting.Items.Add(sId)
        Next

        lLista = KatalogiWgKeyword()
        For Each sId As String In From c In lLista Order By c
            uiComboExisting.Items.Add(sId)
        Next

    End Sub

    Private Sub PokazOpcjeCzasowe(iFirstSelected As Integer)

        For iLp As Integer = iFirstSelected To 0 Step -1
            If _thumbsy(iLp).splitBefore = SplitBeforeEnum.czas Then
                ' w ten sposób mamy datę z dniem tygodnia (wspólne dla całego programu)
                _lastCzasDir = Vblib.OneDir.DateToDirId(_thumbsy(iLp).dateMin)
                uiManualDateSplit.Content = _lastCzasDir & " "
                Exit For
            End If
        Next

        uiManualDateSplit.IsChecked = True

        For iLP As Integer = 1 To _selected.Count - 1
            If _selected(iLP).splitBefore = SplitBeforeEnum.czas Then
                uiAutoDateSplit.IsChecked = True
                Exit For
            End If
        Next


    End Sub

    Private Sub uiCzasFolder_Changed(sender As Object, e As TextChangedEventArgs)
        ' jeśli coś ktoś wpisał, to wymusza MANUAL
        If uiManualDateName.Text.Length > 0 Then uiManualDateSplit.IsChecked = True
    End Sub

    Private Function CountSubdirInDate(sData As String) As Char
        Dim iCount As Integer = 65
        For Each oDir As Vblib.OneDir In Application.GetDirList.GetList
            If oDir.sId.StartsWith(sData) Then iCount += 1
        Next

        Return Chr(iCount)
    End Function

    Private Function PicekToGeoName(oPic As Vblib.OnePic) As String
        Dim oExif As Vblib.ExifTag = oPic.GetExifOfType(Vblib.ExifSource.AutoOSM)
        If oExif Is Nothing Then Return ""

        Dim sNazwa As String = Vblib.Auto_OSM_POI.FullGeoNameToFolderName(oExif.GeoName)
        Return sNazwa

    End Function

    Private Sub PokazOpcjeGeo(iFirstSelected As Integer)

        Dim sTaData As String = uiManualDateSplit.Content.ToString.Trim
        Dim sPrefixGeoData As String = CountSubdirInDate(sTaData)
        uiManualGeoSplit.Content = sTaData & "." & sPrefixGeoData & "__"


        For iLp As Integer = iFirstSelected To 0 Step -1
            If _thumbsy(iLp).splitBefore = SplitBeforeEnum.geo Then
                _lastGeoDir = PicekToGeoName(_thumbsy(iLp).oPic)
                If _lastGeoDir <> "" Then uiManualGeoSplit.Content += _lastGeoDir
                Exit For
            End If
        Next

        uiNoGeoSplit.IsChecked = True

        For iLP As Integer = 1 To _selected.Count - 1
            If _selected(iLP).splitBefore = SplitBeforeEnum.geo Then
                uiAutoGeoSplit.IsChecked = True
                Exit For
            End If
        Next

    End Sub


    Private Sub uiGeoFolder_Changed(sender As Object, e As TextChangedEventArgs)
        ' jeśli coś ktoś wpisał, to wymusza MANUAL
        If uiManualGeoName.Text.Length > 0 Then uiManualGeoSplit.IsChecked = True
    End Sub

    Private Sub uiOK_Click(sender As Object, e As RoutedEventArgs)

        DopiszKatalogi()
        ' aplikuj do _selected

        Me.DialogResult = True
        Me.Close()
    End Sub


    Private Sub DopiszKatalogi()

        If _selected.Count = 1 Then
            DopiszKatalog(_selected(0), True)
        Else
            For Each oThPic As ProcessBrowse.ThumbPicek In _selected
                DopiszKatalog(oThPic, False)
            Next
        End If

    End Sub

    Private Sub DopiszKatalog(oPicek As ProcessBrowse.ThumbPicek, forceDir As Boolean)

        ' jeśli już jest wpisany, to zmiana tylko przy FORCE
        If Not String.IsNullOrWhiteSpace(oPicek.oPic.TargetDir) AndAlso Not forceDir Then Return

        ' możliwości:
        ' existing, czas *, geo * - czyli narzucona nazwa
        ' no existing, czas *, manual geo - czyli narzucona nazwa
        ' no existing, manual czas, no geo - czyli narzucona nazwa
        ' no existing, manual czas, auto geo
        ' no existing, auto czas, no geo
        ' no existing, auto czas, auto geo

        ' wybrany z istniejących
        If DopiszKatalogExisting(oPicek) Then Return

        If DopiszKatalogForcedGeo(oPicek) Then Return

        If DopiszKatalogForcedCzasNoGeo(oPicek) Then Return

        If DopiszKatalogForcedCzasAutoGeo(oPicek) Then Return

        If DopiszKatalogAutoCzasNoGeo(oPicek) Then Return

        If DopiszKatalogAutoCzasAutoGeo(oPicek) Then Return

    End Sub

    Private Function DopiszKatalogExisting(oPicek As ProcessBrowse.ThumbPicek) As Boolean

        Dim sExisting As String = uiComboExisting.SelectedItem
        If sExisting Is Nothing Then Return ""

        Dim iInd As Integer = sExisting.IndexOf(" (")
        If iInd > 0 Then sExisting = sExisting.Substring(0, iInd)

        oPicek.oPic.TargetDir = sExisting
        oPicek.ZrobDymek()
        Return True
    End Function

    Private Function DopiszKatalogForcedGeo(oPicek As ProcessBrowse.ThumbPicek) As Boolean

        If Not uiManualGeoSplit.IsChecked Then Return False

        Dim sDirId As String = uiManualGeoSplit.Content.ToString.Replace("__", "_")
        Dim sOpis As String = uiManualGeoName.Text
        Application.GetDirList.TryAddFolder(sDirId, sOpis)

        oPicek.oPic.TargetDir = (sDirId & " " & sOpis).Trim
        oPicek.ZrobDymek()

        Return True
    End Function

    Private Function DopiszKatalogForcedCzasNoGeo(oPicek As ProcessBrowse.ThumbPicek) As Boolean
        If Not uiNoGeoSplit.IsChecked Then Return False
        If Not uiManualDateSplit.IsChecked Then Return False

        Dim sDirId As String = uiManualDateSplit.Content.ToString
        Dim sOpis As String = uiManualDateName.Text
        Application.GetDirList.TryAddFolder(sDirId, sOpis)

        oPicek.oPic.TargetDir = (sDirId & " " & sOpis).Trim

        oPicek.ZrobDymek()
        Return True
    End Function

    Private Function DopiszKatalogAutoCzasNoGeo(oPicek As ProcessBrowse.ThumbPicek) As Boolean
        If Not uiNoGeoSplit.IsChecked Then Return False
        If Not uiAutoDateSplit.IsChecked Then Return False

        Dim sTargetDir As String = KatalogNaCzas(oPicek)
        Application.GetDirList.TryAddFolder(sTargetDir, "")
        oPicek.oPic.TargetDir = sTargetDir

        oPicek.ZrobDymek()
        Return True
    End Function

    Private Function DopiszKatalogForcedCzasAutoGeo(oPicek As ProcessBrowse.ThumbPicek) As Boolean
        If Not uiManualDateSplit.IsChecked Then Return False
        If Not uiAutoGeoSplit.IsChecked Then Return False

        Dim sDirId As String = uiManualDateSplit.Content.ToString
        Dim sOpis As String = uiManualDateName.Text

        Return DopiszKatalogCzasAutoGeo(oPicek, (sDirId & " " & sOpis).Trim)

    End Function

    Private Function DopiszKatalogAutoCzasAutoGeo(oPicek As ProcessBrowse.ThumbPicek) As Boolean
        If Not uiAutoDateSplit.IsChecked Then Return False
        If Not uiAutoGeoSplit.IsChecked Then Return False

        Dim sTargetDir As String = KatalogNaCzas(oPicek)
        Return DopiszKatalogCzasAutoGeo(oPicek, sTargetDir)

    End Function



    Private Function DopiszKatalogCzasAutoGeo(oPicek As ProcessBrowse.ThumbPicek, sFolderCzas As String) As Boolean

        Dim sGeoSufix As String = KatalogNaGeo(oPicek)

        Dim sTargetDir As String = sFolderCzas & "_" & sGeoSufix

        oPicek.oPic.TargetDir = sTargetDir
        oPicek.ZrobDymek()
        Return True

    End Function

    Private _lastGeoDir As String = ""
    Private Function KatalogNaGeo(oPicek As ProcessBrowse.ThumbPicek) As String
        ' dla AUTO geo, bierze geo z serii, zmienia tylko na przedziałku
        ' pierwotne ustawienie _lastCzasDir jest w WindowLoad:PokazOpcjeGeo
        If oPicek.splitBefore = SplitBeforeEnum.geo Then
            _lastGeoDir = PicekToGeoName(oPicek.oPic)
        End If

        Return _lastGeoDir
    End Function

    Private _lastCzasDir As String = ""
    Private Function KatalogNaCzas(oPicek As ProcessBrowse.ThumbPicek) As String
        ' dla AUTO czas, bierze czas z serii, i zmienia tylko na przedziałku
        ' pierwotne ustawienie _lastCzasDir jest w WindowLoad:PokazOpcjeCzasowe
        If oPicek.splitBefore = SplitBeforeEnum.czas Then
            _lastCzasDir = Vblib.OneDir.DateToDirId(oPicek.dateMin)
        End If

        Return _lastCzasDir
    End Function

    '''' <summary>
    '''' podaje ustalony katalog czasowy dla zdjęć, lub "" gdy ma być auto
    '''' </summary>
    '''' <returns></returns>
    'Public Function GetFolderCzas() As String
    '    If uiAutoDateSplit.IsChecked Then Return ""
    '    Return (uiManualDateSplit.Content.ToString & " " & uiManualDateName.Text).Trim
    'End Function

    '''' <summary>
    '''' podaje ustalony katalog geograficzny dla zdjęć, "" gdy ma być auto, lub nothing - gdy bez podziału geo
    '''' </summary>
    '''' <returns></returns>
    'Public Function GetFolderGeo() As String
    '    If uiNoGeoSplit.IsChecked Then Return Nothing

    '    If uiAutoGeoSplit.IsChecked Then Return ""
    '    Return (uiManualGeoSplit.Content.ToString.Replace("__", "_") & " " & uiManualGeoName.Text).Trim
    'End Function

    '''' <summary>
    '''' podaje narzucony (wybrany z istniejących) katalog, lub "", gdy nie ma narzucenia
    '''' </summary>
    '''' <returns></returns>
    'Private Function GetFolderExisting() As String
    '    Dim sRet As String = uiComboExisting.SelectedItem
    '    If sRet Is Nothing Then Return ""

    '    Dim iInd As Integer = sRet.IndexOf(" (")
    '    If iInd > 0 Then sRet = sRet.Substring(0, iInd)

    '    Return sRet
    'End Function

    Private Sub uiComboExisting_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles uiComboExisting.SelectionChanged
        Dim sRet As String = uiComboExisting.SelectedItem
        uiAutoDateSplit.IsEnabled = (String.IsNullOrEmpty(sRet))
        uiManualDateSplit.IsEnabled = (String.IsNullOrEmpty(sRet))
        uiManualDateName.IsEnabled = (String.IsNullOrEmpty(sRet))
        uiNoGeoSplit.IsEnabled = (String.IsNullOrEmpty(sRet))
        uiAutoGeoSplit.IsEnabled = (String.IsNullOrEmpty(sRet))
        uiManualGeoSplit.IsEnabled = (String.IsNullOrEmpty(sRet))
        uiManualGeoName.IsEnabled = (String.IsNullOrEmpty(sRet))
    End Sub

    Private Sub uiManualGeoSplit_Checked(sender As Object, e As RoutedEventArgs)
        uiAutoDateSplit.IsEnabled = Not uiManualGeoSplit.IsChecked
        uiManualDateSplit.IsEnabled = Not uiManualGeoSplit.IsChecked
        uiManualDateName.IsEnabled = Not uiManualGeoSplit.IsChecked
    End Sub
End Class

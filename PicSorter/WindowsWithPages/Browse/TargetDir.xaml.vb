

' 2022.11.23.sb.A_geo[space]remark

'Imports System.Collections.ObjectModel

Imports System.Security.Policy
Imports Vblib
Imports Windows.Devices
Imports vb14 = Vblib.pkarlibmodule14


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

#Region "combo katalogów"

    Private Shared Function KatalogiWgDaty(aboutDateOd As Date, aboutDateDo As Date) As List(Of String)

        Dim lLista As New List(Of String) ' poprzez listę pośrednią, bo chodzi o sortowanie

        'Dim sDataOd As String = Vblib.OneDirFlat.DateToDirId(aboutDateOd.AddDays(-5))
        'Dim sDataDo As String = Vblib.OneDirFlat.DateToDirId(aboutDateDo.AddDays(5))
        Dim sDataOd As String = aboutDateOd.AddDays(-5).ToExifString
        Dim sDataDo As String = aboutDateDo.AddDays(5).ToExifString

        ' teraz mozna wedle stringow
        For Each oDir As Vblib.OneDir In Application.GetDirTree.ToFlatList
            ' If oDir.sId.StartsWith("_") Then lLista.Add(oDir.ToComboDisplayName)
            If Not oDir.IsFromDate Then Continue For
            If oDir.sId > sDataOd AndAlso oDir.sId < sDataDo Then lLista.Add(oDir.ToComboDisplayName)
        Next

        Return lLista
    End Function

    Private Sub PokazIstniejaceKatalogi(aboutDateOd As Date, aboutDateDo As Date)

        ' z sortowaniem według daty
        Dim lLista As List(Of String) = KatalogiWgDaty(aboutDateOd, aboutDateDo)
        For Each sId As String In From c In lLista Order By c
            uiComboExisting.Items.Add(sId)
        Next

        ' a teraz bez sortowania

        ' te ze słów kluczowych
        For Each oKwd As Vblib.OneKeyword In Application.GetKeywords.ToFlatList
            If Not String.IsNullOrWhiteSpace(oKwd.ownDir) Then
                uiComboExisting.Items.Add(oKwd.ToComboDisplayName)
            End If
        Next

        ' a na koniec te normalne, jako drzewko
        SettingsKeywords.FillDirCombo(uiComboExisting, "", True)
    End Sub
#End Region

    Private Sub PokazOpcjeCzasowe(iFirstSelected As Integer)

        For iLp As Integer = iFirstSelected To 0 Step -1
            If _thumbsy(iLp).splitBefore = SplitBeforeEnum.czas Then
                ' w ten sposób mamy datę z dniem tygodnia (wspólne dla całego programu)
                _lastCzasDir = _thumbsy(iLp).dateMin.ExifDateWithWeekDay
                uiManualDateName.Text = _lastCzasDir & " "
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

    Private Sub PokazOpcjeGeo(iFirstSelected As Integer)

        Dim sTaData As String = uiManualDateName.Text.Trim
        Dim sPrefixGeoData As String = CountSubdirInDate(sTaData)
        uiManualGeoName.Text = sTaData & "." & sPrefixGeoData & "_"


        For iLp As Integer = iFirstSelected To 0 Step -1
            If _thumbsy(iLp).splitBefore = SplitBeforeEnum.geo Then
                _lastGeoDir = PicekToGeoName(_thumbsy(iLp).oPic)
                If _lastGeoDir <> "" Then uiManualGeoName.Text += _lastGeoDir
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

    Private Shared Function CountSubdirInDate(sData As String) As Char
        Dim iCount As Integer = 65
        For Each oDir As Vblib.OneDir In Application.GetDirTree.ToFlatList
            If oDir.sId.StartsWith(sData) Then iCount += 1
        Next

        Return Chr(iCount)
    End Function

    Private Shared Function PicekToGeoName(oPic As Vblib.OnePic) As String
        Dim oExif As Vblib.ExifTag = oPic.GetExifOfType(Vblib.ExifSource.AutoOSM)
        If oExif Is Nothing Then Return ""

        Dim sNazwa As String = Vblib.Auto_OSM_POI.FullGeoNameToFolderName(oExif.GeoName)
        Return sNazwa

    End Function




    Private Sub uiGeoFolder_Changed(sender As Object, e As TextChangedEventArgs)
        ' jeśli coś ktoś wpisał, to wymusza MANUAL
        'If uiManualGeoName.Text.Length > 0 Then uiManualGeoSplit.IsChecked = True
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

        For Each oThPic As ProcessBrowse.ThumbPicek In _selected
            oThPic.ZrobDymek()
        Next


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

        Dim oDir As OneDir = GetKatalogExisting()
        oDir = GetSubdirDate(oPicek, oDir)
        oDir = GetSubdirGeo(oPicek, oDir)

        oPicek.oPic.TargetDir = Application.GetDirTree.GetFullPath(oDir)
        oPicek.ZrobDymek()

    End Sub

    Private Function GetKatalogExisting() As OneDir

        Dim oCBI As ComboBoxItem = uiComboExisting.SelectedItem
        Dim oDir As OneDir = oCBI?.DataContext

        Return oDir
    End Function

    Private Function GetSubdirDate(oPicek As ProcessBrowse.ThumbPicek, oParent As OneDir) As OneDir
        If uiNoDateSplit.IsChecked Then Return oParent

        If uiManualDateSplit.IsChecked Then
            Dim sDir As String = uiManualDateName.Text.Replace("__", "_").Trim
            'If Not String.IsNullOrWhiteSpace(uiManualDateName.Text) Then
            '    sDir = sDir & " " & uiManualDateName.Text
            'End If
            Return Application.GetDirTree.TryAddSubdir(oParent, sDir, "")
        End If

        If uiAutoDateSplit.IsChecked Then
            Dim sDir As String = KatalogNaCzas(oPicek)
            Return Application.GetDirTree.TryAddSubdir(oParent, sDir, "")
        End If

        ' a to się nie ma prawa zdarzyć
        Return Nothing

    End Function

    Private Function GetSubdirGeo(oPicek As ProcessBrowse.ThumbPicek, oParent As OneDir) As OneDir
        If uiNoGeoSplit.IsChecked Then Return oParent

        If uiManualGeoSplit.IsChecked Then
            Dim sDir As String = uiManualGeoName.Text.Trim
            'If Not String.IsNullOrWhiteSpace(uiManualGeoName.Text) Then
            '    sDir = sDir & " " & uiManualGeoName.Text
            'End If
            Return Application.GetDirTree.TryAddSubdir(oParent, sDir, "")
        End If

        If uiAutoDateSplit.IsChecked Then
            Dim sDir As String = KatalogNaCzas(oPicek)
            sDir = sDir & "_" & KatalogNaGeo(oPicek)
            Return Application.GetDirTree.TryAddSubdir(oParent, sDir, "")
        End If

        ' a to się nie ma prawa zdarzyć
        Return Nothing

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
            _lastCzasDir = oPicek.dateMin.ExifDateWithWeekDay
        End If

        Return _lastCzasDir
    End Function


    'Private Sub uiComboExisting_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles uiComboExisting.SelectionChanged
    '    Dim sRet As String = uiComboExisting.SelectedItem
    '    uiAutoDateSplit.IsEnabled = (String.IsNullOrEmpty(sRet))
    '    uiManualDateSplit.IsEnabled = (String.IsNullOrEmpty(sRet))
    '    uiManualDateName.IsEnabled = (String.IsNullOrEmpty(sRet))
    '    uiNoGeoSplit.IsEnabled = (String.IsNullOrEmpty(sRet))
    '    uiAutoGeoSplit.IsEnabled = (String.IsNullOrEmpty(sRet))
    '    uiManualGeoSplit.IsEnabled = (String.IsNullOrEmpty(sRet))
    '    uiManualGeoName.IsEnabled = (String.IsNullOrEmpty(sRet))
    'End Sub

    Private Sub uiManualGeoSplit_Checked(sender As Object, e As RoutedEventArgs)
        uiAutoDateSplit.IsEnabled = Not uiManualGeoSplit.IsChecked
        uiManualDateSplit.IsEnabled = Not uiManualGeoSplit.IsChecked
        uiManualDateName.IsEnabled = Not uiManualGeoSplit.IsChecked
    End Sub

    Private Sub uiOpenDirTree_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New SettingsDirTree
        If Not oWnd.ShowDialog Then Return
        Window_Loaded(Nothing, Nothing)
    End Sub

    Private Sub uiDateSplit_Checked(sender As Object, e As RoutedEventArgs)
        If uiManualDateName Is Nothing Then Return

        Try
            uiManualDateName.IsEnabled = uiManualDateSplit.IsChecked
            uiNoGeoSplit.IsEnabled = True
            uiAutoGeoSplit.IsEnabled = True
            uiManualGeoSplit.IsEnabled = True
            uiManualGeoName.IsEnabled = True
        Catch ex As Exception

        End Try
    End Sub

    Private Sub uiNoDateSplit_Checked(sender As Object, e As RoutedEventArgs)
        If uiManualDateName Is Nothing Then Return

        uiManualDateName.IsEnabled = False
        uiNoGeoSplit.IsEnabled = False
        uiAutoGeoSplit.IsEnabled = False
        uiManualGeoSplit.IsEnabled = False
        uiManualGeoName.IsEnabled = False
    End Sub

    Private Async Sub uiSearchTree_Click(sender As Object, e As RoutedEventArgs)

        Dim sQuery As String = Await vb14.DialogBoxInputAllDirectAsync("Czego szukać?")
        If sQuery = "" Then Return
        sQuery = sQuery.ToLowerInvariant

        ' wyszukiwanie w drzewku, ale drzewko musi być chyba raz wczytane i w nim tylko przeglądanie
        For Each oCBItem As ComboBoxItem In uiComboExisting.Items
            oCBItem.Visibility = Visibility.Collapsed
            Dim oDir As OneDir = oCBItem.DataContext
            If oDir.sId.ToLowerInvariant.Contains(sQuery) Then oCBItem.Visibility = Visibility.Visible
        Next

    End Sub
End Class

Partial Public Module Extensions

    <Runtime.CompilerServices.Extension>
    Public Function ExifDateWithWeekDay(ByVal oDate As Date) As String
        Dim sId As String = oDate.ToString("yyyy.MM.dd.")
        Select Case oDate.DayOfWeek
            Case DayOfWeek.Monday
                sId &= "pn"
            Case DayOfWeek.Tuesday
                sId &= "wt"
            Case DayOfWeek.Wednesday
                sId &= "sr"
            Case DayOfWeek.Thursday
                sId &= "cz"
            Case DayOfWeek.Friday
                sId &= "pt"
            Case DayOfWeek.Saturday
                sId &= "sb"
            Case DayOfWeek.Sunday
                sId &= "nd"
        End Select

        Return sId
    End Function
End Module

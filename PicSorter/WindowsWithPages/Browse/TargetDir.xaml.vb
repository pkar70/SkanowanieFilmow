

' 2022.11.23.sb.A_geo[space]remark

'Imports System.Collections.ObjectModel

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

    Private Sub PokazIstniejaceKatalogi(aboutDateOd As Date, aboutDateDo As Date)
        Dim sDataOd As String = Vblib.OneDir.DateToDirId(aboutDateOd.AddDays(-5))
        Dim sDataDo As String = Vblib.OneDir.DateToDirId(aboutDateDo.AddDays(5))

        ' teraz mozna wedle stringow
        Dim lLista As New List(Of String)
        For Each oDir As Vblib.OneDir In Application.GetDirList.GetList
            If oDir.sId > sDataOd AndAlso oDir.sId < sDataDo Then lLista.Add(oDir.sId)
        Next

        For Each sId As String In From c In lLista Order By c
            uiComboExisting.Items.Add(sId)
        Next

    End Sub

    Private Sub PokazOpcjeCzasowe(iFirstSelected As Integer)

        For iLp As Integer = iFirstSelected To 0 Step -1
            If _thumbsy(iLp).splitBefore = SplitBeforeEnum.czas Then
                ' w ten sposób mamy datę z dniem tygodnia (wspólne dla całego programu)
                uiManualDateSplit.Content = Vblib.OneDir.DateToDirId(_thumbsy(iLp).dateMin) & " "
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

    Private Sub PokazOpcjeGeo(iFirstSelected As Integer)

        ' weryfikacja czy już były jakieś z daną date
        Dim iTaData As Integer = 0
        For Each oDir As Vblib.OneDir In Application.GetDirList.GetList
            If oDir.sId.StartsWith(uiManualDateSplit.Content) Then iTaData += 1
        Next

        Dim sPrefixGeoData As String = Chr(65 + iTaData)
        uiManualGeoSplit.Content = uiManualDateSplit.Content & "." & sPrefixGeoData & "__"


        For iLp As Integer = iFirstSelected To 0 Step -1
            If _thumbsy(iLp).splitBefore = SplitBeforeEnum.geo Then
                Dim oExif As Vblib.ExifTag = _thumbsy(iLp).oPic.GetExifOfType(Vblib.ExifSource.AutoOSM)
                If oExif IsNot Nothing Then
                    Dim sNazwa As String = Vblib.Auto_OSM_POI.FullGeoNameToFolderName(oExif.GeoName)
                    uiManualGeoSplit.Content += sNazwa
                End If
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
        Me.DialogResult = True
        Me.Close()
    End Sub

    ''' <summary>
    ''' podaje ustalony katalog czasowy dla zdjęć, lub "" gdy ma być auto
    ''' </summary>
    ''' <returns></returns>
    Public Function GetFolderCzas() As String
        If uiAutoDateSplit.IsChecked Then Return ""
        Return uiManualDateSplit.Content & " " & uiManualDateName.Text
    End Function

    ''' <summary>
    ''' podaje ustalony katalog geograficzny dla zdjęć, "" gdy ma być auto, lub nothing - gdy bez podziału geo
    ''' </summary>
    ''' <returns></returns>
    Public Function GetFolderGeo() As String
        If uiNoGeoSplit.IsChecked Then Return Nothing

        If uiAutoGeoSplit.IsChecked Then Return ""
        Return uiManualGeoSplit.Content.ToString.Replace("__", "_") & " " & uiManualGeoName.Text
    End Function

    ''' <summary>
    ''' podaje narzucony (wybrany z istniejących) katalog, lub "", gdy nie ma narzucenia
    ''' </summary>
    ''' <returns></returns>
    Public Function GetFolderExisting() As String
        Dim sRet As String = uiComboExisting.SelectedItem
        If sRet Is Nothing Then Return ""
        Return sRet
    End Function

End Class

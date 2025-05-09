﻿
' edycja ExifTag - niekoniecznie wszystkie elemeny pokazuje


Imports Vblib
Imports pkar.DotNetExtensions

Public Class EditExifTag

    Private _exifTag As Vblib.ExifTag
    Private _sourceDisplay As String
    Private _scope As EditExifTagScope
    Private _addRemove As Boolean

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="exifTag">tag do edycji</param>
    ''' <param name="sourceDisplayName">tytuł okna do pokazania</param>
    ''' <param name="scope">ile pól ma być pokazane</param>
    ''' <param name="addRemove">czy dodawać w ComboBox "-" (remove tag)</param>
    Public Sub New(exifTag As Vblib.ExifTag, sourceDisplayName As String, scope As EditExifTagScope, addRemove As Boolean)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        _exifTag = exifTag
        _sourceDisplay = sourceDisplayName
        _scope = scope
        _addRemove = addRemove
    End Sub

    Private Sub WypelnComboDeviceType(eTypZrodla As Vblib.FileSourceDeviceTypeEnum)
        WypelnComboDeviceType(uiFileSourceDeviceType, eTypZrodla)
        If _addRemove Then uiFileSourceDeviceType.Items.Add("-")

    End Sub
    Public Shared Sub WypelnComboDeviceType(uiCombo As ComboBox, eTypZrodla As Vblib.FileSourceDeviceTypeEnum)
        uiCombo.Items.Clear()

        uiCombo.Items.Add(" ")

        For iLp = 0 To 6
            If Not WypelnComboDeviceOneDevice(uiCombo, iLp, eTypZrodla) Then Exit For
        Next

        WypelnComboDeviceOneDevice(uiCombo, 13, 0)

        'uiFileSourceDeviceType.SelectedValue = _exifTag.FileSourceDeviceType
    End Sub

    Private Shared Function WypelnComboDeviceOneDevice(uiCombo As ComboBox, iLp As Integer, eTypZrodla As Vblib.FileSourceDeviceTypeEnum) As Boolean
        Dim devType As Vblib.FileSourceDeviceTypeEnum = iLp
        If devType.ToString = iLp.ToString Then Return False
        Dim iInd As Integer = uiCombo.Items.Add(iLp & ": " & devType.ToString)
        If eTypZrodla = iLp Then uiCombo.SelectedIndex = iInd

        Return True
    End Function

    Private Sub WypelnComboPlikiem(oCombo As ComboBox, sFiletitle As String, sCurrent As String)
        oCombo.Items.Clear()

        oCombo.Items.Add(" ")

        Dim sFileName As String = IO.Path.Combine(vblib.GetDataFolder(), sFiletitle & ".txt")
        If IO.File.Exists(sFileName) Then
            Dim fileContent As List(Of String) = IO.File.ReadAllLines(sFileName).ToList
            fileContent.Sort()
            For Each entry As String In fileContent
                oCombo.Items.Add(entry)
            Next
        End If

        If _addRemove Then oCombo.Items.Add("-")

        oCombo.SelectedValue = sCurrent
    End Sub

    Private Sub WypelnDatePickery(dateMin As DateTime, dateMax As DateTime)

        'Dim dateStart As New DateTime(1800, 1, 1)

        'uiDateMax.DisplayDateEnd = DateTime.Now
        'uiDateMin.DisplayDateEnd = DateTime.Now
        'uiDateMax.DisplayDateStart = dateStart
        'uiDateMin.DisplayDateStart = dateStart

        If dateMax.IsDateValid Then uiDateRange.MaxDate = dateMax

        If dateMin.IsDateValid Then uiDateRange.MinDate = dateMin

    End Sub

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)

        uiSource.Text = _sourceDisplay
        WypelnComboDeviceType(_exifTag.FileSourceDeviceType)
        WypelnComboPlikiem(uiAuthor, "authors", _exifTag.Author)
        WypelnComboPlikiem(uiCopyright, "copyrights", _exifTag.Copyright)

        If _scope = EditExifTagScope.LimitedToCloudPublish Then
            uiCopyright.Items.Add("(C) %1 %1")
            uiCopyright.Items.Add("(C) %1 %^3")
        End If

        WypelnComboPlikiem(uiCameraModel, "cameras", _exifTag.CameraModel)

        WypelnDatePickery(_exifTag.DateMin, _exifTag.DateMax)

        uiKeywords.Text = _exifTag.Keywords
        uiUserComment.Text = _exifTag.UserComment

        SchowajZbedne()

    End Sub

    Private Sub SchowajZbedne()

        ' w zaleznosci od _scope
        Select Case _scope
            Case EditExifTagScope.LimitedToSourceDir
                uiDateRange.Visibility = Visibility.Visible
                'uiDateMin.Visibility = Visibility.Visible
                'uiDateMinHdr.Visibility = Visibility.Visible
                'uiDateMaxHdr.Visibility = Visibility.Visible

                uiKeywordsHdr.Visibility = Visibility.Collapsed
                uiKeywords.Visibility = Visibility.Collapsed
                uiUserCommentHdr.Visibility = Visibility.Collapsed
                uiUserComment.Visibility = Visibility.Collapsed

            Case EditExifTagScope.LimitedToCloudPublish
                uiDateRange.Visibility = Visibility.Collapsed
                'uiDateMin.Visibility = Visibility.Collapsed
                'uiDateMinHdr.Visibility = Visibility.Collapsed
                'uiDateMaxHdr.Visibility = Visibility.Collapsed

                uiKeywordsHdr.Visibility = Visibility.Visible
                uiKeywords.Visibility = Visibility.Visible
                uiUserCommentHdr.Visibility = Visibility.Visible
                uiUserComment.Visibility = Visibility.Visible
        End Select

    End Sub

    Private Sub uiOpenAuthors_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New EditEntryHist("Authors", "Dodaj autora (zwykle: imię nazwisko)", "")
        oWnd.ShowDialog()
        WypelnComboPlikiem(uiAuthor, "authors", _exifTag.Author)
    End Sub

    Private Sub uiOpenCopyrights_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New EditEntryHist("Copyrights", "Dodaj właściciela praw", "(c) KTO, All rights reserved.")
        oWnd.ShowDialog()
        WypelnComboPlikiem(uiCopyright, "copyrights", _exifTag.Copyright)
    End Sub

    Private Sub uiOpenCamera_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New EditEntryHist("Cameras", "Dodaj model aparatu/skanera", "")
        oWnd.ShowDialog()
        WypelnComboPlikiem(uiCameraModel, "cameras", _exifTag.CameraModel)
    End Sub

    Private Sub uiOK_Click(sender As Object, e As RoutedEventArgs)

        _exifTag.Author = uiAuthor.SelectedValue
        _exifTag.Copyright = uiCopyright.SelectedValue
        _exifTag.CameraModel = uiCameraModel.SelectedValue
        If uiDateRange.UseMin Then
            _exifTag.DateMin = uiDateRange.MinDate
        Else
            _exifTag.DateMin = DateTime.MinValue
        End If

        If uiDateRange.UseMax Then
            _exifTag.DateMax = uiDateRange.MaxDate
        Else
            _exifTag.DateMax = DateTime.MinValue
        End If

        _exifTag.Keywords = uiKeywords.Text
        _exifTag.UserComment = uiUserComment.Text

        Dim sDevType As String = uiFileSourceDeviceType.SelectedValue
        If String.IsNullOrWhiteSpace(sDevType) Then sDevType = "0"
        _exifTag.FileSourceDeviceType = sDevType.Substring(0, 1)

        DialogResult = True

        Me.Close()
    End Sub

    Private Sub uiAuthor_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles uiAuthor.SelectionChanged
        Dim autor As String = uiAuthor.SelectedValue
        If autor Is Nothing Then Return

        For Each copyr As String In uiCopyright.Items
            If copyr Is Nothing Then Continue For
            If copyr.ContainsCIAI(autor) Then
                uiCopyright.SelectedItem = copyr
            End If
        Next

    End Sub
End Class


Public Enum EditExifTagScope
    LimitedToSourceDir
    LimitedToSourceFilename
    LimitedToSourceExif
    LimitedToAdHocSource
    LimitedToCloudPublish
    Full
End Enum

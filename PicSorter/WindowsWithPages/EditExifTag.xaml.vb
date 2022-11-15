
' edycja ExifTag - niekoniecznie wszystkie elemeny pokazuje


Imports Vblib

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
        uiFileSourceDeviceType.Items.Clear()

        uiFileSourceDeviceType.Items.Add(" ")

        For iLp = 0 To 6
            Dim devType As Vblib.FileSourceDeviceTypeEnum = iLp
            If devType.ToString = iLp.ToString Then Exit For
            Dim iInd As Integer = uiFileSourceDeviceType.Items.Add(iLp & ": " & devType.ToString)
            If eTypZrodla = iLp Then uiFileSourceDeviceType.SelectedIndex = iInd
        Next

        If _addRemove Then uiFileSourceDeviceType.Items.Add("-")

        'uiFileSourceDeviceType.SelectedValue = _exifTag.FileSourceDeviceType
    End Sub

    Private Sub WypelnComboPlikiem(oCombo As ComboBox, sFiletitle As String, sCurrent As String)
        oCombo.Items.Clear()

        oCombo.Items.Add(" ")

        Dim sFileName As String = IO.Path.Combine(App.GetDataFolder(), sFiletitle & ".txt")
        If IO.File.Exists(sFileName) Then
            Dim fileContent As List(Of String) = IO.File.ReadAllLines(sFileName).ToList
            fileContent.Sort()
            For Each entry In fileContent
                oCombo.Items.Add(entry)
            Next
        End If

        If _addRemove Then oCombo.Items.Add("-")

        oCombo.SelectedValue = sCurrent
    End Sub

    Private Sub WypelnDatePickery(dateMin As DateTime, dateMax As DateTime)

        Dim dateStart As New DateTime(1800, 1, 1)

        uiDateMax.DisplayDateEnd = DateTime.Now
        uiDateMin.DisplayDateEnd = DateTime.Now
        uiDateMax.DisplayDateStart = dateStart
        uiDateMin.DisplayDateStart = dateStart

        If dateMax.IsDateValid Then uiDateMax.SelectedDate = dateMax

        If dateMin.IsDateValid Then uiDateMin.SelectedDate = dateMin

    End Sub

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)

        uiSource.Text = _sourceDisplay
        WypelnComboDeviceType(_exifTag.FileSourceDeviceType)
        WypelnComboPlikiem(uiAuthor, "authors", _exifTag.Author)
        WypelnComboPlikiem(uiCopyright, "copyrights", _exifTag.Copyright)
        WypelnComboPlikiem(uiCameraModel, "cameras", _exifTag.CameraModel)

        WypelnDatePickery(_exifTag.DateMin, _exifTag.DateMax)

        SchowajZbedne()

    End Sub

    Private Sub SchowajZbedne()
        ' w zaleznosci od _scope
        ' *TODO* na razie niepotrzebne, bo tylko jeden typ jest używany
    End Sub

    Private Sub uiOpenAuthors_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New EditEntryHist(App.GetDataFolder, "Authors", "Dodaj autora (zwykle: imię nazwisko)", "")
        oWnd.ShowDialog()
        WypelnComboPlikiem(uiAuthor, "authors", _exifTag.Author)
    End Sub

    Private Sub uiOpenCopyrights_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New EditEntryHist(App.GetDataFolder, "Copyrights", "Dodaj właściciela praw", "(c) KTO, All rights reserved.")
        oWnd.ShowDialog()
        WypelnComboPlikiem(uiCopyright, "copyrights", _exifTag.Copyright)
    End Sub

    Private Sub uiOpenCamera_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New EditEntryHist(App.GetDataFolder, "Cameras", "Dodaj model aparatu/skanera", "")
        oWnd.ShowDialog()
        WypelnComboPlikiem(uiCameraModel, "cameras", _exifTag.CameraModel)
    End Sub

    Private Sub uiOK_Click(sender As Object, e As RoutedEventArgs)

        _exifTag.Author = uiAuthor.SelectedValue
        _exifTag.Copyright = uiCopyright.SelectedValue
        _exifTag.CameraModel = uiCameraModel.SelectedValue
        If uiDateMin.SelectedDate.HasValue Then
            _exifTag.DateMin = uiDateMin.SelectedDate.Value
        Else
            _exifTag.DateMin = DateTime.MinValue
        End If

        If uiDateMax.SelectedDate.HasValue Then
            _exifTag.DateMax = uiDateMax.SelectedDate.Value
        Else
            _exifTag.DateMax = DateTime.MinValue
        End If

        Me.Close()
    End Sub
End Class


Public Enum EditExifTagScope
    LimitedToSourceDir
    LimitedToSourceFilename
    LimitedToSourceExif
    LimitedToAdHocSource
    Full
End Enum

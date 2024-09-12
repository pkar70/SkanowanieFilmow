'Imports Auto_WinOCR
'Imports MetadataExtractor.Formats
'Imports Vblib
Imports pkar.DotNetExtensions

Public Class ShowAzureLists
    'Public Property Captions As ListTextWithProbability
    'Public Property Categories As ListTextWithProbability
    'Public Property Tags As ListTextWithProbability
    'Public Property Landmarks As ListTextWithProbability

    'Public Property Brands As ListTextWithProbabAndBox
    'Public Property Objects As ListTextWithProbabAndBox
    'Public Property Celebrities As ListTextWithProbabAndBox
    'Public Property Faces As ListTextWithProbabAndBox

    Private _azurek As Vblib.MojeAzure

    Private Sub Window_KeyUp(sender As Object, e As KeyEventArgs)
        If e.IsRepeat Then Return
        If e.Key <> Key.Escape Then Return
        Me.Close()
    End Sub


    Private Async Sub Window_DataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs)
        Await Task.Delay(20)    ' na zmianę po stronie uiPinUnpin

        Dim picek As Vblib.OnePic = Nothing
        If Not uiPinUnpin.IsPinned Then
            picek = TryCast(DataContext, ProcessBrowse.ThumbPicek)?.oPic
        End If

        _azurek = picek?.GetExifOfType(Vblib.ExifSource.AutoAzure)?.AzureAnalysis

        uiRozpiska.DataContext = _azurek

        If String.IsNullOrWhiteSpace(_azurek?.Wiekowe) Then
            uiAdult.IsChecked = False
            uiGory.IsChecked = False
            uiRacy.IsChecked = False
        Else
            uiAdult.IsChecked = _azurek.Wiekowe.ContainsCI("ADULT")
            uiGory.IsChecked = _azurek.Wiekowe.ContainsCI("GORY")
            uiRacy.IsChecked = _azurek.Wiekowe.ContainsCI("RACY")
        End If

    End Sub

    Private Sub uiOk_Click(sender As Object, e As RoutedEventArgs)

        ' z zakładki ogólne idzie bezpośrednio do AzureAnalysis, więc nic nie trzeba

        If _azurek Is Nothing Then Return

        _azurek.Captions = AzureCapt.GetChanged
        _azurek.Categories = AzureCateg.GetChanged
        _azurek.Tags = AzureTags.GetChanged
        _azurek.Landmarks = AzureLandm.GetChanged
        _azurek.Brands = AzureBrands.GetChanged
        _azurek.Objects = AzureObjs.GetChanged
        _azurek.Celebrities = AzureCelebs.GetChanged
        _azurek.Faces = AzureFaces.GetChanged

        Dim wiek As String = ""
        If uiAdult.IsChecked Then wiek = "ADULTPIC"
        If uiGory.IsChecked Then
            If wiek <> "" Then wiek &= ", "
            wiek &= "GORYPIC"
        End If
        If uiRacy.IsChecked Then
            If wiek <> "" Then wiek &= ", "
            wiek &= "RACYPIC"
        End If

        If wiek = "" Then
            _azurek.Wiekowe = Nothing
        Else
            _azurek.Wiekowe = wiek
        End If
    End Sub
End Class



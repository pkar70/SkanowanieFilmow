﻿Public Class SimpleKeywords

    Private _orgKeywords As String
    Private _readonly As Boolean

    Public Sub New(bReadOnly As Boolean)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        _readonly = bReadOnly
    End Sub


    Private Sub uiApply_Click(sender As Object, e As RoutedEventArgs)

        ' bez zmian
        If _orgKeywords = uiAllKeywords.Text Then Return

        ' zmiana keywordów
        Dim _oNewExif As New Vblib.ExifTag(Vblib.ExifSource.ManualTag)
        Dim listaKwds As List(Of Vblib.OneKeyword) = Application.GetKeywords.GetKeywordsList(uiAllKeywords.Text)

        BrowseKeywordsWindow.ApplyKeywordsToExif(_oNewExif, listaKwds)

        Dim oPicek As ProcessBrowse.ThumbPicek = DataContext
        oPicek.oPic.ReplaceOrAddExif(_oNewExif)

    End Sub

    Private Sub Window_DataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs)
        Dim oPicek As ProcessBrowse.ThumbPicek = DataContext

        uiFileName.Text = oPicek.oPic.sSuggestedFilename

        _orgKeywords = oPicek.oPic.GetAllKeywords
        uiAllKeywords.Text = _orgKeywords

        If _readonly Then uiApply.IsEnabled = False

    End Sub
End Class

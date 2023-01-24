Imports Vblib

Public Class SimpleDescribe
    Private _orgDescribe As String
    Private _readonly As Boolean

    Public Sub New(bReadOnly As Boolean)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        _readonly = bReadOnly
    End Sub

    Private Sub uiApply_Click(sender As Object, e As RoutedEventArgs)

        ' bez zmian
        If _orgDescribe = uiAllDescribe.Text Then Return

        Dim oPicek As ProcessBrowse.ThumbPicek = DataContext
        Dim descr As String = uiAllDescribe.Text
        AddToMenu(descr)
        oPicek.oPic.ReplaceAllDescriptions(descr)

    End Sub

    Private Sub AddToMenu(descr As String)
        uiPastePrev.IsEnabled = True
        For Each oItem As MenuItem In uiPrevMenu.Items
            If oItem.Header = descr Then Return
        Next

        If uiPrevMenu.Items.Count > 5 Then
            uiPrevMenu.Items.RemoveAt(5)
        End If

        Dim oMI As New MenuItem
        oMI.Header = descr
        AddHandler oMI.Click, AddressOf PasteThis

        uiPrevMenu.Items.Insert(0, oMI)

    End Sub

    Private Sub PasteThis(sender As Object, e As RoutedEventArgs)
        uiPrevMenuPopup.IsOpen = False
        Dim oMI As MenuItem = sender
        uiAllDescribe.Text = oMI.Header
    End Sub

    Private Sub Window_DataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs)
        Dim oPicek As ProcessBrowse.ThumbPicek = DataContext

        uiFileName.Text = oPicek.oPic.sSuggestedFilename

        _orgDescribe = oPicek.oPic.GetSumOfDescriptionsText
        uiAllDescribe.Text = _orgDescribe
        uiAllDescribe.IsReadOnly = oPicek.oPic.AreTagsInDescription

        If _readonly Then uiApply.IsEnabled = False

    End Sub

    Private Sub uiPastePrev_Click(sender As Object, e As RoutedEventArgs)
        uiPrevMenuPopup.IsOpen = Not uiPrevMenuPopup.IsOpen
    End Sub
End Class

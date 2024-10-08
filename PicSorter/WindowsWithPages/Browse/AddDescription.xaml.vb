﻿

'Imports Windows.Media.Devices
Imports pkar.UI.Extensions
Imports Vblib

Public Class AddDescription

    Private _picek As Vblib.OnePic
    Private _keywords As String
    Private _comment As String
    Private _editmode As Boolean = False

    ''' <summary>
    ''' jak nie NULL, to z niego do edycji bierze dane?
    ''' </summary>
    ''' <param name="oPic"></param>
    Public Sub New(oPic As Vblib.OnePic)

        ' This call is required by the designer.
        InitializeComponent()

        _picek = oPic
    End Sub

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        Me.InitDialogs

        If _picek IsNot Nothing Then
            uiTitle.DataContext = _picek
            'uiTitle.Text = _picek.sSuggestedFilename
        End If

        If _picek?.descriptions IsNot Nothing Then
            If _picek.descriptions.Count = 1 Then
                _editmode = True
                uiKeywords.Text = _picek.descriptions.ElementAt(0).keywords
                uiDescription.Text = _picek.descriptions.ElementAt(0).comment
            End If
        End If

        WypelnMenuKeywords()

        uiDescription.Focus()
    End Sub

    Private Async Sub uiOK_Click(sender As Object, e As RoutedEventArgs)
        _keywords = uiKeywords.Text
        _comment = uiDescription.Text

        If _keywords.Contains("NO:") AndAlso Not _keywords.Contains("=NO:") Then
            If Await Me.DialogBoxYNAsync("Podejrzewam że chciałeś użyć keyword blokującego AUTOTAG, ale użyłeś złej składni: powinno być =NO:" & vbCrLf & "Chcesz poprawić?") Then Return
        End If

        If _editmode Then
            _picek.descriptions.ElementAt(0).keywords = uiKeywords.Text
            _picek.descriptions.ElementAt(0).comment = uiDescription.Text
            Me.DialogResult = False
        Else
            Me.DialogResult = (_keywords & _comment).Length > 2
        End If
        Me.Close()
    End Sub

    Public Function GetDescription() As Vblib.OneDescription
        If _editmode Then Return Nothing
        If (_keywords & _comment).Length < 3 Then Return Nothing
        Return New Vblib.OneDescription(_comment, _keywords)
    End Function

#Region "menu z keywords"

    Private Sub uiAdd_Click(sender As Object, e As RoutedEventArgs)
        uiAddPopup.IsOpen = Not uiAddPopup.IsOpen
    End Sub

    Private _DefMargin As New Thickness(-10, 0, 0, 0)

    Private Shared Sub DodajSubTree(oMenuItem As MenuItem, oSubTree As List(Of Vblib.OneKeyword), oEventHandler As RoutedEventHandler)
        If oSubTree Is Nothing Then Return
        For Each oItem As Vblib.OneKeyword In oSubTree
            Dim oNew As New MenuItem
            oNew.Header = oItem.sId & " " & oItem.sDisplayName
            oNew.DataContext = oItem
            'oNew.Margin = New Thickness(2)
            DodajSubTree(oNew, oItem.SubItems, oEventHandler)
            AddHandler oNew.Click, oEventHandler
            'oNew.Margin = _DefMargin
            'oNew.Background = New SolidColorBrush(Colors.White)
            oMenuItem.Items.Add(oNew)

            If oItem.sId = "=NO" Then DodajTaggersBlock(oNew, oEventHandler)

        Next

    End Sub

    Private Shared Sub DodajTaggersBlock(oParent As MenuItem, oEventHandler As RoutedEventHandler)

        For Each oTagger In Globs.gAutoTagery

            Dim oItem As New Vblib.OneKeyword With {.sId = oTagger.GetAutoTagDisableKwd}
            Dim oNew As New MenuItem With {.Header = oItem.sId, .DataContext = oItem}
            AddHandler oNew.Click, oEventHandler
            oParent.Items.Add(oNew)
        Next

    End Sub

    Public Shared Function WypelnMenuKeywords(oMenu As Menu, oEventHandler As RoutedEventHandler) As Integer
        oMenu.Items.Clear()

        For Each oItem As Vblib.OneKeyword In Vblib.GetKeywords
            Dim oNew As New MenuItem
            oNew.Header = oItem.sId
            'oNew.Margin = _DefMargin
            DodajSubTree(oNew, oItem.SubItems, oEventHandler)
            ' AddHandler oNew.Click, AddressOf Keyword_Click ' nie można dodawać keywords głównego poziomu (#,-,=)
            oMenu.Items.Add(oNew)
        Next

        Return oMenu.Items.Count
    End Function


    Private Sub WypelnMenuKeywords()

        Dim count As Integer = WypelnMenuKeywords(uiMenuKeywords, AddressOf DodajTenKeyword)

        If count < 1 Then
            uiAdd.IsEnabled = False
            uiAdd.ToolTip = "(nie ma zdefiniowanych słów kluczowych)"
        Else
            uiAdd.IsEnabled = True
            uiAdd.ToolTip = "Dodaj słowa kluczowe"
        End If

    End Sub

    Private Sub DodajTenKeyword(sender As Object, e As RoutedEventArgs)
        uiAddPopup.IsOpen = False

        Dim oMI As MenuItem = sender
        Dim oKeyword As Vblib.OneKeyword = oMI?.DataContext
        If oKeyword Is Nothing Then Return

        uiKeywords.Text = (uiKeywords.Text & " " & oKeyword.sId).Trim & " "
        uiDescription.Text = (uiDescription.Text & vbCrLf & oKeyword.sDisplayName).Trim

    End Sub

#End Region

End Class

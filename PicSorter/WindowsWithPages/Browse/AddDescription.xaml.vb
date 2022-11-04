﻿

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
        If _picek IsNot Nothing Then uiTitle.Text = _picek.sSuggestedFilename

        If _picek?.descriptions IsNot Nothing Then
            If _picek.descriptions.Count = 1 Then
                _editmode = True
                uiKeywords.Text = _picek.descriptions.ElementAt(0).keywords
                uiDescription.Text = _picek.descriptions.ElementAt(0).comment
            End If
        End If
    End Sub

    Private Sub uiOK_Click(sender As Object, e As RoutedEventArgs)
        _keywords = uiKeywords.Text
        _comment = uiDescription.Text
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

End Class

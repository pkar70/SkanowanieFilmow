
Imports Newtonsoft.Json
Imports vb14 = Vblib.pkarlibmodule14


Public Class ShowExifs

    Private _picek As Vblib.OnePic
    Private _fullOpis As String

    Public Sub New(oPic As Vblib.OnePic)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        _picek = oPic

        Me.Height = 400 ' bo zdaje sie pamięta że było okno powiększone...
        Me.Width = 350
    End Sub

    Public Sub SetForPic(oPic As Vblib.OnePic)
        uiTitle.Text = oPic.sSuggestedFilename

        Dim oSerSet As New JsonSerializerSettings
        If vb14.GetSettingsBool("uiFullJSON") Then
            oSerSet.NullValueHandling = NullValueHandling.Include
        Else
            oSerSet.NullValueHandling = NullValueHandling.Ignore
        End If

        Dim sTxt As String = ""
        If oPic.descriptions IsNot Nothing Then
            sTxt = "Descriptions:" & vbCrLf & vbCrLf

            sTxt &= JsonConvert.SerializeObject(oPic.descriptions, Formatting.Indented, oSerSet)
            sTxt = sTxt & vbCrLf & vbCrLf & vbCrLf & "Exifs:" & vbCrLf & vbCrLf
        End If

        sTxt &= JsonConvert.SerializeObject(oPic.Exifs, Formatting.Indented, oSerSet)

        _fullOpis = sTxt.Replace("\r\n", vbCrLf)

        FilterText()
    End Sub

    Private Sub FilterText()
        If uiMask.Text = "" Then
            uiDump.Text = _fullOpis
            Return
        End If

        Dim sTxt As String = ""
        Dim aLines As String() = _fullOpis.Split(vbCrLf)
        Dim sMaska As String = uiMask.Text.ToLowerInvariant

        For Each linia As String In aLines
            If linia.ToLowerInvariant.Contains(uiMask.Text) Then sTxt = sTxt & vbCrLf & linia
        Next

        uiDump.Text = sTxt.Trim
    End Sub

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        SetForPic(_picek)
    End Sub

    Private Sub uiMask_TextChanged(sender As Object, e As TextChangedEventArgs)
        FilterText()
    End Sub
End Class

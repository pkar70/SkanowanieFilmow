﻿
Imports Newtonsoft.Json
Imports vb14 = Vblib.pkarlibmodule14
Imports pkar.DotNetExtensions


Public Class ShowExifs

    'Private _picek As Vblib.OnePic
    Private _fullOpis As String
    Private _bRealExif As String

    Public Sub New(bRealExif As Boolean) '(oPic As Vblib.OnePic)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        '_picek = oPic

        _bRealExif = bRealExif
        Me.Title = If(_bRealExif, "File EXIF", "Metadata")
        If _bRealExif Then uiDump.FontFamily = New FontFamily("Courier New")
        Me.Height = 400 ' bo zdaje sie pamięta że było okno powiększone...
        Me.Width = 350
    End Sub

    'Public Sub SetForPic(oPic As Vblib.OnePic)
    '    uiTitle.Text = oPic.sSuggestedFilename

    '    Dim oSerSet As New JsonSerializerSettings
    '    If vb14.GetSettingsBool("uiFullJSON") Then
    '        oSerSet.NullValueHandling = NullValueHandling.Include
    '    Else
    '        oSerSet.NullValueHandling = NullValueHandling.Ignore
    '    End If

    '    Dim sTxt As String = ""
    '    If oPic.descriptions IsNot Nothing Then
    '        sTxt = "Descriptions:" & vbCrLf & vbCrLf

    '        sTxt &= JsonConvert.SerializeObject(oPic.descriptions, Formatting.Indented, oSerSet)
    '        sTxt = sTxt & vbCrLf & vbCrLf & vbCrLf & "Exifs:" & vbCrLf & vbCrLf
    '    End If

    '    sTxt &= JsonConvert.SerializeObject(oPic.Exifs, Formatting.Indented, oSerSet)

    '    _fullOpis = sTxt.Replace("\r\n", vbCrLf)

    '    FilterText()
    'End Sub

    Private Sub FilterText()
        If uiMask.Text = "" Then
            uiDump.Text = _fullOpis
            Return
        End If

        Dim sTxt As String = ""
        Dim aLines As String() = _fullOpis.Split(vbCrLf)
        Dim sMaska As String = uiMask.Text.ToLowerInvariant

        For Each linia As String In aLines
            If linia.ContainsCI(uiMask.Text) Then sTxt = sTxt & vbCrLf & linia
        Next

        uiDump.Text = sTxt.Trim
    End Sub

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        'SetForPic(_picek)
    End Sub

    Private Sub uiMask_TextChanged(sender As Object, e As TextChangedEventArgs)
        FilterText()
    End Sub

    Private Function GetMetadataDump(oPic As Vblib.OnePic) As String

        Dim oSerSet As New JsonSerializerSettings
        If vb14.GetSettingsBool("uiFullJSON") Then
            oSerSet.NullValueHandling = NullValueHandling.Include
            oSerSet.DefaultValueHandling = DefaultValueHandling.Include
        Else
            oSerSet.NullValueHandling = NullValueHandling.Ignore
            oSerSet.DefaultValueHandling = DefaultValueHandling.Ignore
        End If

        Dim sTxt As String = ""
        If oPic.descriptions IsNot Nothing Then
            sTxt = "Descriptions:" & vbCrLf & vbCrLf

            sTxt &= JsonConvert.SerializeObject(oPic.descriptions, Formatting.Indented, oSerSet)
            sTxt = sTxt & vbCrLf & vbCrLf & vbCrLf & "Exifs:" & vbCrLf & vbCrLf
        End If

        sTxt &= JsonConvert.SerializeObject(oPic.Exifs, Formatting.Indented, oSerSet)

        Return sTxt.Replace("\r\n", vbCrLf)
    End Function

    Private Function GetRealExifDump(oPic As Vblib.OnePic) As String
        Dim sPathname As String = oPic.InBufferPathName
        If Not IO.File.Exists(sPathname) Then Return "Cannot find file " & sPathname

        Return CompactExifLib.FileExif2String.GetString(sPathname)

    End Function

    Private Sub Window_DataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs)
        ' *TODO* być może jakoś trzeba wywołać też dla mybase

        Dim oPic As Vblib.OnePic

        If Me.DataContext.GetType Is GetType(Vblib.OnePic) Then
            oPic = Me.DataContext
        ElseIf Me.DataContext.GetType Is GetType(ProcessBrowse.ThumbPicek) Then
            oPic = TryCast(Me.DataContext, ProcessBrowse.ThumbPicek)?.oPic
        Else
            ' nieznany typ
            Return
        End If

        uiTitle.Text = oPic.sSuggestedFilename


        If _bRealExif Then
            _fullOpis = GetRealExifDump(oPic)
        Else
            _fullOpis = GetMetadataDump(oPic)
        End If
        FilterText()


    End Sub
End Class

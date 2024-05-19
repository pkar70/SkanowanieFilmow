
Imports Newtonsoft.Json
Imports vb14 = Vblib.pkarlibmodule14
Imports pkar.DotNetExtensions


Public Class ShowExifs

    'Private _picek As Vblib.OnePic
    Private _fullOpis As String
    Private _bRealExif As String
    Private _bFullData As Boolean

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

    Private Shared Function GetMetadataDump(oPic As Vblib.OnePic) As String

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

    Private Shared Function GetRealExifDump(oPic As Vblib.OnePic) As String
        Dim sPathname As String = oPic.InBufferPathName
        If Not IO.File.Exists(sPathname) Then Return "Cannot find file " & sPathname

        Return CompactExifLib.FileExif2String.GetString(sPathname)

    End Function

    Private Function GetOnePicFromDataContext()
        If uiPinUnpin.EffectiveDatacontext Is Nothing Then Return Nothing
        ' próbujemy czy zadziała casting z ThumbPicek na OnePic - NIE
        If uiPinUnpin.EffectiveDatacontext.GetType Is GetType(Vblib.OnePic) Then
            Return uiPinUnpin.EffectiveDatacontext
        ElseIf uiPinUnpin.EffectiveDatacontext.GetType Is GetType(ProcessBrowse.ThumbPicek) Then
            Return TryCast(uiPinUnpin.EffectiveDatacontext, ProcessBrowse.ThumbPicek)?.oPic
        Else
            ' nieznany typ
            Return Nothing
        End If
    End Function

    Private Async Sub Window_DataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs)
        ' *TODO* być może jakoś trzeba wywołać też dla mybase

        ' idziemy dalej, bo czasem sam przeładowywuję (bez zmiany DataContext, jedynie przerysowanie full/ograniczone)
        If uiPinUnpin.IsPinned Then Return

        Dim oPic As Vblib.OnePic = GetOnePicFromDataContext()
        If oPic Is Nothing Then
            ' to jako jedyne okno daje taki numer, że szybciej jest Window_DataContextChanged niż uiPinUnpin_DataContextChanged ...
            Await Task.Delay(10)
            oPic = GetOnePicFromDataContext()
        End If
        'uiTitle.Text = oPic.sSuggestedFilename
        If oPic Is Nothing Then Return

        If _bRealExif Then
            _fullOpis = GetRealExifDump(oPic)
        Else
            If _bFullData Then
                _fullOpis = oPic.DumpAsJSON
            Else
                _fullOpis = GetMetadataDump(oPic)
            End If
        End If
        FilterText()

        uiMask.Focus()
    End Sub

    Private Sub uiTitle_MouseDoubleClick(sender As Object, e As MouseButtonEventArgs)
        _bFullData = Not _bFullData
        Window_DataContextChanged(Nothing, Nothing)
    End Sub

    Private Sub Window_KeyUp(sender As Object, e As KeyEventArgs)
        If e.IsRepeat Then Return
        If e.Key <> Key.Escape Then Return
        Me.Close()
    End Sub

End Class

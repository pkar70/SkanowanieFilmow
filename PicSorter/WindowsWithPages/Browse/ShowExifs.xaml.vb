
Imports Newtonsoft.Json
Imports vb14 = Vblib.pkarlibmodule14


Public Class ShowExifs

    Private _picek As Vblib.OnePic

    Public Sub New(oPic As Vblib.OnePic)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        _picek = oPic
    End Sub

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        uiTitle.Text = _picek.sSuggestedFilename

        Dim oSerSet As New JsonSerializerSettings
        If vb14.GetSettingsBool("uiFullJSON") Then
            oSerSet.NullValueHandling = NullValueHandling.Include
        Else
            oSerSet.NullValueHandling = NullValueHandling.Ignore
        End If


        Dim sTxt As String = ""
        If _picek.descriptions IsNot Nothing Then
            sTxt = "Descriptions:" & vbCrLf & vbCrLf

            sTxt &= JsonConvert.SerializeObject(_picek.descriptions, Formatting.Indented, oSerSet)
            sTxt = sTxt & vbCrLf & vbCrLf & vbCrLf & "Exifs:" & vbCrLf & vbCrLf
        End If

        sTxt &= JsonConvert.SerializeObject(_picek.Exifs, Formatting.Indented, oSerSet)

        uiDump.Text = sTxt
    End Sub
End Class

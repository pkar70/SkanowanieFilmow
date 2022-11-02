


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

        Dim sTxt As String = Newtonsoft.Json.JsonConvert.SerializeObject(_picek.Exifs, Newtonsoft.Json.Formatting.Indented)
        uiDump.Text = sTxt
    End Sub
End Class

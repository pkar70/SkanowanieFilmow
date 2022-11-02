Imports PicSorterNS.ProcessBrowse

Public Class ShowBig

    Private _picek As ThumbPicek

    Public Sub New(oPicek As ThumbPicek)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        _picek = oPicek
    End Sub

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        Dim oImage As New Image
        oImage.Source = New BitmapImage(New Uri(_picek.oPic.InBufferPathName))

        Dim oStack As StackPanel = New StackPanel
        oStack.Children.Add(oImage)
        'oStack.Children.Add(oButt)

        Dim oWin As Window = New Window
        oWin.Content = oStack
        oWin.Title = _picek.sDymek
        oWin.Show()
    End Sub
End Class

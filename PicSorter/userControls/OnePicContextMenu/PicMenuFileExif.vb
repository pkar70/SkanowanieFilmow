
Public NotInheritable Class PicMenuFileExif
    Inherits PicMenuBase

    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If _wasApplied Then Return

        MyBase.OnApplyTemplate()

        If Not InitEnableDisable("File EXIF") Then Return

        AddHandler Me.Click, AddressOf ActionClick

        _wasApplied = True
    End Sub

    Private Sub ActionClick(sender As Object, e As RoutedEventArgs)

        Dim oWnd As New ShowExifs(True) '(_picek.oPic)
        oWnd.DataContext = _picek
        If UseOwner Then oWnd.Owner = Window.GetWindow(Me)
        oWnd.Show()

    End Sub


End Class

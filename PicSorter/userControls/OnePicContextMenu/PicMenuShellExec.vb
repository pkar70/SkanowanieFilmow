

Public NotInheritable Class PicMenuShellExec
    Inherits PicMenuBase


    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If _wasApplied Then Return

        If UseSelectedItems Then Return

        MyBase.OnApplyTemplate()

        If Not InitEnableDisable("Shell exec", "Uruchomienie programu domyślnego dla danego typu zdjęcia") Then Return

        AddHandler Me.Click, AddressOf ActionClick

        _wasApplied = True
    End Sub

    Public Sub ActionClick(sender As Object, e As RoutedEventArgs)

        Dim proc As New Process()
        proc.StartInfo.UseShellExecute = True
        proc.StartInfo.FileName = GetFromDataContext.InBufferPathName
        proc.Start()
    End Sub

End Class

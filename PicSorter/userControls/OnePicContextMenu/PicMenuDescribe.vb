

Public NotInheritable Class PicMenuDescribe
    Inherits PicMenuBase

    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If _wasApplied Then Return

        MyBase.OnApplyTemplate()

        If Not InitEnableDisable("Add description", "Dodawanie opisu zdjęcia") Then Return

        AddHandler Me.Click, AddressOf ActionClick

        _wasApplied = True
    End Sub

    Private Sub ActionClick(sender As Object, e As RoutedEventArgs)

        Dim oWnd As New AddDescription(_picek)
        If Not oWnd.ShowDialog Then Return

        Dim oDesc As Vblib.OneDescription = oWnd.GetDescription

        OneOrMany(Sub(x)
                      x.AddDescription(oDesc)
                      If Not String.IsNullOrWhiteSpace(x.sharingFromGuid) Then
                          Application.GetShareDescriptionsOut.AddPicDescForPicLastPeer(x, oDesc.comment)
                      End If

                  End Sub)
        EventRaise(Me)

    End Sub


End Class

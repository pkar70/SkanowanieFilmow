﻿
Public NotInheritable Class PicMenuShowMetadata
    Inherits PicMenuBase


    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If Not String.IsNullOrWhiteSpace(Me.Header) Then Return

        MyBase.OnApplyTemplate()

        If Not InitEnableDisable("Metadata", "Pokazanie opisów zdjęcia") Then Return

        AddHandler Me.Click, AddressOf ActionClick

    End Sub

    Private Sub ActionClick(sender As Object, e As RoutedEventArgs)

        Dim oWnd As New ShowExifs(False) '(oPicek.oPic)
        If UseOwner Then oWnd.Owner = Window.GetWindow(Me)
        oWnd.DataContext = GetFromDataContext()
        oWnd.Show()

    End Sub


End Class

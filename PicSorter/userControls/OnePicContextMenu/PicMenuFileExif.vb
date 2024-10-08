﻿
Public NotInheritable Class PicMenuFileExif
    Inherits PicMenuBase

    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If Not String.IsNullOrWhiteSpace(Me.Header) Then Return

        MyBase.OnApplyTemplate()

        If Not InitEnableDisable("File EXIF", "Pokaż pełny opis zdjęcia (odczytuje dane z pliku)") Then Return

        AddHandler Me.Click, AddressOf ActionClick

    End Sub

    Private Sub ActionClick(sender As Object, e As RoutedEventArgs)

        Dim oWnd As New ShowExifs(True) '(_azurek.oPic)
        oWnd.DataContext = GetFromDataContext()
        If UseOwner Then oWnd.Owner = Window.GetWindow(Me)
        oWnd.Show()

    End Sub


End Class

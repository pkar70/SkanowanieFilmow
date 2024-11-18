Imports PicSorterNS.ProcessBrowse
Imports Vblib

Public Class PicMenuSlideshow
    Inherits PicMenuBase


    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If Not String.IsNullOrWhiteSpace(Me.Header) Then Return

        If Not InitEnableDisable("Slideshow", "Pokaz slajdów z zaznaczonych zdjęć") Then Return

        MyBase.OnApplyTemplate()

        AddHandler Me.Click, AddressOf Runme

    End Sub

    Private Sub Runme(sender As Object, e As RoutedEventArgs)
        If Not UseSelectedItems Then Return

        If GetSelectedItems.Count < 1 Then Return

        Dim oBrowserWnd As ProcessBrowse = TryGetProcessBrowse()
        If oBrowserWnd Is Nothing Then Return

        Dim picek As ProcessBrowse.ThumbPicek = oBrowserWnd.FromBig_Next(Nothing, -100, True)

        Dim oWnd As New ShowBig(picek, True, True)
        oWnd.Owner = oBrowserWnd
        oWnd.Show()

        Task.Delay(100) ' bo czasem focus wraca do Browser i chodzenie nie działa
        oWnd.Focus()

    End Sub
End Class

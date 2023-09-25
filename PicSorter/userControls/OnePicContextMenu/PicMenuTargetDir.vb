

Public Class PicMenuTargetDir
    Inherits PicMenuBase

    Private _itemPaste As New MenuItem
    Private Shared _clipForTargetDir As String

    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If _wasApplied Then Return

        MyBase.OnApplyTemplate()

        If Not InitEnableDisable("Target dir") Then Return

        Me.Items.Clear()

        Dim oNew As MenuItem

        If UseSelectedItems Then
            ' dla pojedyńczego trudno jest ustalić TargetDir (radiobuttony automatycznego podziału)
            oNew = New MenuItem
            oNew.Header = "Set target dir"
            AddHandler oNew.Click, AddressOf uiCreateGeotag_Click
            Me.Items.Add(oNew)
        End If

        If Not UseSelectedItems Then
            oNew = New MenuItem
            oNew.Header = "Copy TargetDir"
            AddHandler oNew.Click, AddressOf uiGeotagToClip_Click
            oNew.IsEnabled = String.IsNullOrWhiteSpace(_picek.TargetDir)
            Me.Items.Add(oNew)
        End If

        'oNew = New MenuItem
        _itemPaste.Header = "Paste TargetDir"
        _itemPaste.IsEnabled = Not String.IsNullOrWhiteSpace(_clipForTargetDir)
        AddHandler _itemPaste.Click, AddressOf uiGeotagPaste_Click
        Me.Items.Add(_itemPaste)

        oNew = New MenuItem
        oNew.Header = "Clear TargetDir"
        AddHandler oNew.Click, AddressOf uiGeotagClear_Click
        oNew.IsEnabled = String.IsNullOrWhiteSpace(_picek.TargetDir)
        Me.Items.Add(oNew)

        _wasApplied = True
    End Sub

    Private Sub uiGeotagClear_Click(sender As Object, e As RoutedEventArgs)
        OneOrMany(Sub(x) x.TargetDir = "")
        EventRaise(Me)
    End Sub


    Private Sub uiGeotagToClip_Click(sender As Object, e As RoutedEventArgs)
        _clipForTargetDir = _picek.TargetDir
    End Sub

    Private Sub uiGeotagPaste_Click(sender As Object, e As RoutedEventArgs)
        OneOrMany(Sub(x) x.TargetDir = _clipForTargetDir)
        EventRaise(Me)
    End Sub

    Private Sub uiCreateGeotag_Click(sender As Object, e As RoutedEventArgs)

        Dim oWnd As New TargetDir(Nothing, Nothing)
        If Not oWnd.ShowDialog Then Return
        ' ale to jest bardzo skomplikowane, bo operuje na całej liście do auto-dzielenia

        EventRaise(Me)

    End Sub



End Class

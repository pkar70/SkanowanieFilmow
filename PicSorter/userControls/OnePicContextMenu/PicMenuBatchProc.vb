﻿
Imports Vblib

Public NotInheritable Class PicMenuBatchProc
    Inherits PicMenuBase

    Public Overrides Property ChangePic As Boolean = True
    Public Overrides Property ChangeMetadata As Boolean = True

    Protected Overrides Property _maxAktualne As SequenceStages = SequenceStages.CloudArch

    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If Not String.IsNullOrWhiteSpace(Me.Header) Then Return

        MyBase.OnApplyTemplate()

        If Not InitEnableDisable("Batch process", "Uruchamianie przetwarzania wsadowego", True) Then Return

        WypelnMenuBatchProcess(Me, AddressOf ApplyBatchProcess)

    End Sub

    Private Shared Sub WypelnMenuBatchProcess(oMenuItem As MenuItem, oEventHandler As RoutedEventHandler)
        oMenuItem.Items.Clear()

        For Each oEngine As Vblib.PostProcBase In Vblib.gPostProcesory
            Dim oNew As New MenuItem
            oNew.Header = oEngine.Nazwa.Replace("_", "__")
            oNew.DataContext = oEngine
            AddHandler oNew.Click, oEventHandler
            oMenuItem.Items.Add(oNew)
        Next

        oMenuItem.IsEnabled = (oMenuItem.Items.Count > 0)

    End Sub

    Private _engine As Vblib.PostProcBase
    Private Async Sub ApplyBatchProcess(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        _engine = oFE?.DataContext
        If _engine Is Nothing Then Return

        Await OneOrManyAsync(AddressOf ApplyBatch)

        EventRaise(PicMenuModifies.Any)
    End Sub

    Private Async Function ApplyBatch(oPic As Vblib.OnePic) As Task
        Await _engine.Apply(oPic, False, "")
    End Function

End Class

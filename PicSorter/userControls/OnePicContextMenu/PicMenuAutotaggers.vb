
Public NotInheritable Class PicMenuAutotaggers
    Inherits PicMenuBase

    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If _wasApplied Then Return

        MyBase.OnApplyTemplate()

        If Not InitEnableDisable("Auto-taggers", "Uruchamianie automatycznych opisywaczy zdjęć", True) Then Return

        WypelnMenuAutotagerami(Me, AddressOf ApplyProcess)

        _wasApplied = True
    End Sub


    Public Shared Sub WypelnMenuAutotagerami(oMenuItem As MenuItem, oEventHandler As RoutedEventHandler)
        oMenuItem.Items.Clear()
        ' _UImenuOnClick = oEventHandler

        For Each oEngine As Vblib.AutotaggerBase In Application.gAutoTagery
            Dim oNew As New MenuItem
            oNew.Header = oEngine.Nazwa.Replace("_", "__")
            oNew.DataContext = oEngine
            AddHandler oNew.Click, oEventHandler
            oMenuItem.Items.Add(oNew)
        Next

        oMenuItem.IsEnabled = (oMenuItem.Items.Count > 0)

    End Sub

    Private _engine As Vblib.AutotaggerBase
    Private Async Sub ApplyProcess(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        _engine = oFE?.DataContext
        If _engine Is Nothing Then Return

        Await OneOrManyAsync(AddressOf ApplyTagger)

        EventRaise(Me)
    End Sub

    Private Async Function ApplyTagger(oPic As Vblib.OnePic) As Task

        If UseSelectedItems Then
            If oPic.GetExifOfType(_engine.Nazwa) IsNot Nothing Then Return
        End If

        Dim oExif As Vblib.ExifTag = Await _engine.GetForFile(oPic)
        If oExif IsNot Nothing Then
            oPic.ReplaceOrAddExif(oExif)
            oPic.TagsChanged = True
        End If
    End Function


End Class

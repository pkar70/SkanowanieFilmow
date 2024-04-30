Imports pkar.DotNetExtensions


Public NotInheritable Class PicMenuCloudArchive
    Inherits PicMenuCloudBase

    Protected Overrides Property IsForCloudArchive As Boolean = True

    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If _wasApplied Then Return

        MyBase.OnApplyTemplate()

        If Not InitEnableDisable("Cloud archive", "Wysyłanie do archiwum w chmurze", True) Then Return

        WypelnMenu(Me, AddressOf ApplyActionSingle, AddressOf ApplyActionMulti)

        _wasApplied = True
    End Sub


    Private _engine As Vblib.CloudArchive

    Private _retMsg As String = ""
    Private Async Function ApplyOnSingle(oPic As Vblib.OnePic) As Task

        If UseSelectedItems Then
            If oPic.IsCloudArchivedIn(_engine.konfiguracja.nazwa) Then Return
        End If
        _retMsg &= Await _engine.SendFile(oPic) & vbCrLf
    End Function

    Protected Overrides Async Sub ApplyActionMulti(sender As Object, e As RoutedEventArgs)
        Dim oFE As MenuItem = sender
        _engine = oFE?.DataContext
        If _engine Is Nothing Then Return

        Application.ShowWait(True)
        Dim sErr As String = Await _engine.Login    ' AnyCloudStorage
        If sErr <> "" Then
            Await Vblib.DialogBoxAsync(sErr)
            Application.ShowWait(False)
            Return
        End If

        _retMsg = ""

        Await OneOrManyAsync(AddressOf ApplyOnSingle)

        If Not String.IsNullOrWhiteSpace(_retMsg) Then Vblib.DialogBox(_retMsg)

        WypelnMenu(Me, AddressOf ApplyActionSingle, AddressOf ApplyActionMulti)

        EventRaise(Me)

    End Sub





End Class

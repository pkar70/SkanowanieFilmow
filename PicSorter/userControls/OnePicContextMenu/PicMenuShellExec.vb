

Imports Vblib

Public NotInheritable Class PicMenuShellExec
    Inherits PicMenuBase


    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If _wasApplied Then Return

        If UseSelectedItems Then Return

        MyBase.OnApplyTemplate()

        If Vblib.GetSettingsBool("uiAdvShellExec") Then
            If Not InitEnableDisable("Shell exec", "Uruchomienie programu domyślnego dla danego typu zdjęcia") Then Return
            AddHandler Me.Click, AddressOf DefaultActionClick
        Else
            If Not InitEnableDisable("Shell exec", "Otworzenie zdjęcia w innym programie") Then Return
        End If

        _wasApplied = True
    End Sub

    Public Overrides Sub MenuOtwieramy()
        MyBase.MenuOtwieramy()

        If Not Vblib.GetSettingsBool("uiAdvShellExec") Then Return

        Me.Items.Clear()

        Me.Items.Add(NewMenuItem("default", "program domyślny", AddressOf DefaultActionClick))
        Me.Items.Add(New Separator)
        ' *TODO* może tutaj być share (systemowe), albo w copyout
        ' Me.Items.Add(New Separator)

        ' *TODO* wejdz do registry, iteruj
        ' *TODO* item.CommandParameter to string
    End Sub

    Public Sub DefaultActionClick(sender As Object, e As RoutedEventArgs)

        Dim proc As New Process()
        proc.StartInfo.UseShellExecute = True
        proc.StartInfo.FileName = GetFromDataContext.InBufferPathName
        proc.Start()
    End Sub

    Public Sub AdvancedActionClick(sender As Object, e As RoutedEventArgs)

        Dim oFE As MenuItem = TryCast(sender, MenuItem)
        If oFE Is Nothing Then Return
        Dim exec As String = oFE.CommandParameter

        exec = exec.Replace("%f", """" & GetFromDataContext()?.InBufferPathName & """")

        Dim proc As New Process()
        proc.StartInfo.UseShellExecute = True
        proc.StartInfo.FileName = "" ' pierwszy - plik
        proc.StartInfo.Arguments = "" ' reszta commandline

        proc.Start()
    End Sub

End Class

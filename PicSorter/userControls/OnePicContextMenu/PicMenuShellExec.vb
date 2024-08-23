

Imports System.IO
Imports Vblib

Public NotInheritable Class PicMenuShellExec
    Inherits PicMenuBase


    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If Not String.IsNullOrWhiteSpace(Me.Header) Then Return

        If UseSelectedItems Then Return

        MyBase.OnApplyTemplate()

        If Vblib.GetSettingsBool("uiAdvShellExec") Then
            If Not InitEnableDisable("Shell exec", "Uruchomienie programu domyślnego dla danego typu zdjęcia") Then Return
            AddHandler Me.Click, AddressOf DefaultActionClick
        Else
            If Not InitEnableDisable("Shell exec", "Otworzenie zdjęcia w innym programie") Then Return
            WypelnMenuExecami()
        End If

    End Sub

    Public Sub WypelnMenuExecami()

        If Not Vblib.GetSettingsBool("uiAdvShellExec") Then Return

        Me.Items.Clear()
        AddMenuItem("default", "program domyślny", AddressOf DefaultActionClick)
        AddSeparator()

        Dim fname As String = Globs.GetDataFile("", "execs", False)
        If String.IsNullOrWhiteSpace(fname) Then Return

        Dim pliki As String() = IO.File.ReadAllLines(fname)

        For Each exec As String In pliki
            Dim oNew As New MenuItem

            If exec.Substring(0, 1) = """" Then
                Dim iInd As Integer = exec.IndexOf("""", 2)
                oNew.DataContext = exec.Substring(1, iInd - 2)
                oNew.CommandParameter = exec.Substring(iInd + 1)
            Else
                Dim iInd As Integer = exec.IndexOf(" ")
                oNew.DataContext = exec.Substring(0, iInd)
                oNew.CommandParameter = exec.Substring(iInd + 1)
            End If

            AddHandler oNew.Click, AddressOf RunThisExec

            Dim fileInfo As FileVersionInfo = FileVersionInfo.GetVersionInfo(oNew.DataContext)
            oNew.Header = fileInfo.ProductName

            Me.Items.Add(oNew)
        Next

        AddSeparator()
        AddMenuItem("Settings:Edit execs", "Zmiana listy EXE", AddressOf RunSettings)

    End Sub

    Private Sub RunSettings(sender As Object, e As RoutedEventArgs)
        SettingListy.uiListExec_Click(sender, e)
    End Sub

    Private Sub RunThisExec(sender As Object, e As RoutedEventArgs)
        Dim oMI As MenuItem = TryCast(sender, MenuItem)
        If oMI Is Nothing Then Return

        Dim proc As New Process()
        proc.StartInfo.UseShellExecute = True
        proc.StartInfo.FileName = oMI.DataContext
        proc.StartInfo.Arguments = oMI.CommandParameter.ToString.Replace("%f", GetFromDataContext.InBufferPathName)
        proc.Start()

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

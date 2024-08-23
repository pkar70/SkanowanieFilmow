
Imports InstagramApiSharp.Classes.Models
Imports pkar
Imports pkar.UI.Extensions

Public NotInheritable Class PicMenuCopyId
    Inherits PicMenuBase


    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If Not String.IsNullOrWhiteSpace(Me.Header) Then Return

        MyBase.OnApplyTemplate()

        If Not InitEnableDisable("Copy ID", "Kopiowanie identyfikatorów do clipboard", True) Then Return

        AddMenuItem("Pic #serial", "Skopiuj do clipboard numer seryjny zdjęcia", AddressOf uiCopyPicSerNo_Click)
        AddMenuItem("Reel", "Skopiuj do clipboard numer oparty o reel", AddressOf uiCopyPicReel_Click)
        AddMenuItem("Current ID", "Skopiuj do clipboard aktualnie używany identyfikator (np. do publikacji)", AddressOf uiCopyCurrentId_Click)
        AddMenuItem("Current raw link", "Skopiuj do clipboard link do zdjęcia (z pomijaniem uprawnień; tak jak do SearchByPic)", AddressOf uiCopyCurrentLink_Click)

    End Sub

    Private Async Sub uiCopyCurrentLink_Click(sender As Object, e As RoutedEventArgs)
        Dim oPic As Vblib.OnePic = GetFromDataContext()
        Dim localUri As String = Await PicMenuSearchWebByPic.GetLocalUriInBuff(oPic)
        localUri.SendToClipboard
    End Sub

    Private Sub uiCopyCurrentId_Click(sender As Object, e As RoutedEventArgs)
        GetFromDataContext.GetImageUniqueId.SendToClipboard
    End Sub

    Private Sub uiCopyPicReel_Click(sender As Object, e As RoutedEventArgs)

        ' wysyła ostatni zdefiniowany ReelName
        For Each oExif As Vblib.ExifTag In GetFromDataContext.Exifs
            If Not String.IsNullOrWhiteSpace(oExif.ReelName) Then
                oExif.ReelName.SendToClipboard
            End If
        Next

    End Sub

    Private Sub uiCopyPicSerNo_Click(sender As Object, e As RoutedEventArgs)
        GetFromDataContext.FormattedSerNo.SendToClipboard
    End Sub

    Private Sub uiCopyPicGUID_Click(sender As Object, e As RoutedEventArgs)
        GetFromDataContext()?.PicGuid?.SendToClipboard
    End Sub
End Class

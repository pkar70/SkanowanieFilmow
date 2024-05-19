Imports System.Drawing

Public Class UserControlPinUnpin
    Inherits TextBlock

    Public Property IsPinned As Boolean
        Get
            Return Text = ChrW(&HF096)
        End Get
        Set(value As Boolean)
            If value Then
                Text = ChrW(&HF096)  ' ChrW(&HE1DD) & ChrW(&HE8D8) & ChrW(&HEA6A) ' E194 e8d8 'ea6a
                ToolTip = "Okno nie będzie reagować na zmianę aktywnego zdjęcia"
            Else
                Text = ChrW(&HEDAB) ' ChrW(&HE1DF) & ChrW(&HEDAB) & ChrW(&HE895) ' E149 'edab 'e895
                ToolTip = "Okno będzie zmieniać zawartość wraz ze zmianą aktywnego zdjęcia"
            End If
        End Set
    End Property

    Public Sub New()
        MyBase.New

        TextAlignment = TextAlignment.Center
        HorizontalAlignment = HorizontalAlignment.Center
        VerticalAlignment = VerticalAlignment.Center
        FontSize = 14
        FontFamily = New System.Windows.Media.FontFamily("Segoe MDL2 Assets")

        Text = ChrW(&HE783)
        ToolTip = "nieustalone pin/unpin"

        AddHandler Me.MouseUp, AddressOf MyszkaUp

    End Sub

    Private Sub MyszkaUp(sender As Object, e As MouseButtonEventArgs)
        IsPinned = Not IsPinned
    End Sub
End Class

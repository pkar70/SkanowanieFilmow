Public Class MenuItemIcon
    Inherits MenuItem

    Public Property Image As String
        Set(value As String)

            Try
                Icon = New System.Windows.Controls.Image With {.Source = New BitmapImage(New Uri(value, UriKind.Relative))}
            Catch ex As Exception
                ' typu nie ma takiego pliku, albo tp. - niemanie obrazka nie jest FAIL, więc bez Exception
                Debug.WriteLine($"MenuItemIcon:Image.Set  FAILED  no such file ({value})")
            End Try

        End Set
        Get
            Return Nothing
        End Get
    End Property

    Private _Symbol As String
    Public Property Symbol As String
        Get
            Return _Symbol
        End Get
        Set(value As String)
            Try
                ' https://stackoverflow.com/questions/14975980/how-to-generate-a-image-with-text-and-images-in-c-sharp
                Dim rozmiar As New Size(28, 28)

                Dim tbl As New TextBlock With {.Text = value}
                tbl.Measure(rozmiar)
                tbl.Arrange(New Rect(rozmiar))

                Dim rtb As New RenderTargetBitmap(tbl.ActualWidth, tbl.ActualHeight, 96, 96, PixelFormats.Pbgra32)
                rtb.Render(tbl)

                Icon = rtb
            Catch ex As Exception
                ' typu nie ma takiego pliku, albo tp. - niemanie obrazka nie jest FAIL, więc bez Exception
                Debug.WriteLine($"MenuItemIcon:Symbol.Set  FAILED  ({ex.Message})")
            End Try
        End Set
    End Property
End Class

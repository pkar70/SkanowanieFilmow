


Public NotInheritable Class PicMenuCopyOut
    Inherits PicMenuBase

    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If Not String.IsNullOrWhiteSpace(Me.Header) Then Return

        MyBase.OnApplyTemplate()

        If Not InitEnableDisable("Copy files", "Kopiowanie plików (takich jak są w buforze, bez pipeline)", True) Then Return

        Me.Items.Clear()

        AddMenuItem("To dir ...", "Kopiowanie do wskazanego katalogu", AddressOf uiCopyOut_Click)
        AddMenuItem("To Clip", "Kopiowanie do clipboard", AddressOf uiCopyClip_Click)

    End Sub

    Public Overrides Sub MenuOtwieramy()
        MyBase.MenuOtwieramy()

        Me.Header = If(UseSelectedItems, "Copy files", "Copy file")
    End Sub

    Private Sub uiCopyClip_Click(sender As Object, e As RoutedEventArgs)
        Clipboard.Clear()
        Dim lista As New Specialized.StringCollection

        If UseSelectedItems Then
            For Each oTB As ProcessBrowse.ThumbPicek In GetSelectedItems()
                lista.Add(oTB.oPic.InBufferPathName)
            Next
        Else
            lista.Add(GetFromDataContext.InBufferPathName)
        End If

        Clipboard.SetFileDropList(lista)

        Vblib.DialogBox("Files in Clipboard")
    End Sub

    Private Sub uiCopyOut_Click(sender As Object, e As RoutedEventArgs)

        Dim sFolder As String = SettingsGlobal.FolderBrowser("", "Gdzie skopiować pliki?")
        If sFolder = "" Then Return
        If Not IO.Directory.Exists(sFolder) Then Return

        Dim iErrCount As Integer = 0

        If UseSelectedItems Then
            For Each oItem As ProcessBrowse.ThumbPicek In GetSelectedItems()
                Try
                    oItem.oPic.FileCopyTo(sFolder, oItem.oPic.sSuggestedFilename)
                Catch ex As Exception
                    iErrCount += 1
                End Try
            Next
        Else
            Try
                GetFromDataContext.FileCopyTo(sFolder, _picek.sSuggestedFilename)
            Catch ex As Exception
                iErrCount += 1
            End Try
        End If

        If iErrCount < 1 Then Return
        Vblib.DialogBox($"{iErrCount} errors while copying")

    End Sub
End Class

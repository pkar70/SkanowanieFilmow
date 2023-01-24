Public Class SimpleTargetDir


    Private Sub Window_DataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs)
        Dim oPicek As ProcessBrowse.ThumbPicek = DataContext

        uiFileName.Text = oPicek.oPic.sSuggestedFilename
        uiAllKeywords.Text = oPicek.oPic.TargetDir
    End Sub
End Class

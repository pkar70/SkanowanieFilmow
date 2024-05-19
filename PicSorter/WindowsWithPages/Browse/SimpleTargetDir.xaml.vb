Public Class SimpleTargetDir


    Private Sub Window_DataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs)

        If uiPinUnpin.IsPinned Then Return

        Dim oPicek As ProcessBrowse.ThumbPicek = uiPinUnpin.EffectiveDatacontext

        'uiFileName.Text = oPicek.oPic.sSuggestedFilename
        uiAllKeywords.Text = oPicek.oPic.TargetDir
    End Sub
End Class

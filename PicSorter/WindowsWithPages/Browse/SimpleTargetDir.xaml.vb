﻿Public Class SimpleTargetDir


    Private Async Sub Window_DataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs)

        Await Task.Delay(20)    ' bo najpierw się musi zmienić w uiPinUnpin

        Dim thumb As ProcessBrowse.ThumbPicek = TryCast(uiPinUnpin.EffectiveDatacontext, ProcessBrowse.ThumbPicek)
        If thumb?.oPic Is Nothing Then Return

        uiPropEditor.DataContext = thumb.oPic

        'Dim oPicek As ProcessBrowse.ThumbPicek = uiPinUnpin.EffectiveDatacontext

        'uiFileName.Text = oPicek.oPic.sSuggestedFilename
        'uiAllKeywords.Text = oPicek.oPic.TargetDir
    End Sub
End Class

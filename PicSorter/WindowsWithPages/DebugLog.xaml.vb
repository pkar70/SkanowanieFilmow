Imports pkar.UI.LogExtensions
Imports pkar

Public Class DebugLog
    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        uiLogViewer.AttachLogTextBox
    End Sub

    Private Sub uiClearLog_Click(sender As Object, e As RoutedEventArgs)
        Log.ClearLog()
    End Sub

    Private Sub uiClose_Click(sender As Object, e As RoutedEventArgs)

    End Sub

    Private Sub uiLogLevel_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double))
        Log.EnableLogging(uiLogLevel.Value)
    End Sub
End Class

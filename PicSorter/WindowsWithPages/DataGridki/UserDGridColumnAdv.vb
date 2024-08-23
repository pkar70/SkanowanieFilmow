Public Class UserDGridColumnAdv
    Inherits DataGridTextColumn

    '  <DataGridTextColumn Binding="{Binding sSourceName, Mode=TwoWay}" IsReadOnly="{Binding serno, Converter={StaticResource ReadOnlyFromMode}}" Foreground="{Binding serno, Converter={StaticResource ForegroundFromMode}}" MaxWidth="350">
    Private Shared brushRW As New SolidColorBrush(Colors.Black)
    Private Shared brushRO As New SolidColorBrush(Colors.DimGray)

    Public Sub New()
        Me.MaxWidth = 350
        Me.IsReadOnly = DataGridWnd._standardMode

        Me.Foreground = If(Me.IsReadOnly, brushRO, brushRW)
    End Sub

End Class

Public Class UserDGridColumnRO
    Inherits DataGridTextColumn

    Private Shared brushRO As New SolidColorBrush(Colors.DimGray)

    Public Sub New()
        Me.MaxWidth = 350
        Me.IsReadOnly = True

        Me.Foreground = brushRO
    End Sub

End Class

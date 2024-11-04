Public Class UserDGridColumnAdv
    Inherits DataGridTextColumn

    '  <DataGridTextColumn Binding="{Binding sSourceName, Mode=TwoWay}" IsReadOnly="{Binding serno, Converter={StaticResource ReadOnlyFromMode}}" Foreground="{Binding serno, Converter={StaticResource ForegroundFromMode}}" MaxWidth="350">
    Public Shared brushRW As New SolidColorBrush(Colors.Black)
    Public Shared brushRO As New SolidColorBrush(Colors.DimGray)

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

'If (factory == null)
'Throw New ArgumentNullException("factory");

'    var frameworkElementFactory = New FrameworkElementFactory(TypeOf (_TemplateGeneratorControl));
'    frameworkElementFactory.SetValue(_TemplateGeneratorControl.FactoryProperty, factory);

'    var dataTemplate = New DataTemplate(TypeOf (DependencyObject));
'    dataTemplate.VisualTree = frameworkElementFactory;
'    Return dataTemplate;
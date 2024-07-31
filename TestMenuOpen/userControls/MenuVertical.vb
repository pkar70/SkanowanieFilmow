Public Class MenuVertical
    Inherits Menu

    Public Overrides Sub OnApplyTemplate()

        Dim xaml As String = $"
<ItemsPanelTemplate
  xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
        <VirtualizingStackPanel Orientation = 'Vertical' />
</ItemsPanelTemplate>"

        Me.ItemsPanel = TryCast(Markup.XamlReader.Parse(xaml), ItemsPanelTemplate)
    End Sub

End Class

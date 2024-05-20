

Imports System.Globalization

Public Class UserPropString
    Inherits Grid

    Private _combo As New UserPropSelector With {.UseStrings = True}
    Private _edit As New TextBox ' With {.IsReadOnly = True}
    Private _buttonSet As New Button With {.Content = " Set "}


    Public Property IsReadOnly As Boolean
        Get
            Return _edit.IsReadOnly
        End Get
        Set(value As Boolean)
            _edit.IsReadOnly = value
            _buttonSet.Visibility = If(value, Visibility.Collapsed, Visibility.Visible)
        End Set
    End Property

    Public Property DefaultSelect As String
        Get
            Return _combo.DefaultSelect
        End Get
        Set(value As String)
            _combo.DefaultSelect = value
        End Set
    End Property

    Public Property SkipNames As String
        Get
            Return _combo.SkipNames
        End Get
        Set(value As String)
            _combo.SkipNames = value
        End Set
    End Property


    Public Sub New()
        ColumnDefinitions.Clear()
        ColumnDefinitions.Add(New ColumnDefinition() With {.Width = New GridLength(1, GridUnitType.Auto)})
        ColumnDefinitions.Add(New ColumnDefinition() With {.Width = New GridLength(1, GridUnitType.Star)})
        ColumnDefinitions.Add(New ColumnDefinition() With {.Width = New GridLength(1, GridUnitType.Auto)})

        Children.Clear()
        Children.Add(_combo)
        Children.Add(_edit)
        Children.Add(_buttonSet)

        Grid.SetColumn(_combo, 0)
        Grid.SetColumn(_edit, 1)
        Grid.SetColumn(_buttonSet, 2)

        AddHandler _combo.SelectionChanged, AddressOf ZmianaPropertyCombo
        AddHandler _buttonSet.Click, AddressOf UstawWartosc
        AddHandler Me.DataContextChanged, AddressOf ZmianaDataContext
    End Sub

    Private Sub UstawWartosc(sender As Object, e As RoutedEventArgs)
        Throw New NotImplementedException()
    End Sub

    Private Async Sub ZmianaDataContext(sender As Object, e As DependencyPropertyChangedEventArgs)
        Await Task.Delay(10)    ' czas na zmienienie się po stronie _combo
        _edit.Text = _combo.GetSelectedPropertyStringValue
    End Sub

    Private Sub ZmianaPropertyCombo(sender As Object, e As SelectionChangedEventArgs)
        '_edit.SetBinding(TextBlock.TextProperty, _combo.GetSelectedProperty)
        _edit.Text = _combo.GetSelectedPropertyStringValue

        'If e.AddedItems Is Nothing Then Return
        'If e.AddedItems.Count < 1 Then Return

        'Dim nameBindingObject As New Binding(_combo.GetSelectedPropertyName)

        '' Configure the binding
        'nameBindingObject.Mode = If(IsReadOnly, BindingMode.OneWay, BindingMode.TwoWay)
        'nameBindingObject.Source = DataContext

        '' Set the binding to a target object. The TextBlock.Name property on the NameBlock UI element
        'BindingOperations.SetBinding(_edit, TextBlock.TextProperty, nameBindingObject)

    End Sub


End Class

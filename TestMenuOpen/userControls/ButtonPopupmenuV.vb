Imports System.Windows.Controls


Public Class ButtonPopupmenuV
    Inherits StackPanel

    Private _PopUp As New Primitives.Popup
    Private _Button As New Button
    Private _Header As New TextBlock
    Private _Menu As Primitives.MenuBase

    Public ReadOnly Property Popup As Primitives.Popup
        Get
            Return _PopUp
        End Get
    End Property

    Public ReadOnly Property Header As TextBlock
        Get
            Return _Header
        End Get
    End Property

    Public ReadOnly Property Button As Button
        Get
            Return _Button
        End Get
    End Property

    Public ReadOnly Property Menu As Primitives.MenuBase
        Get
            Return _Menu
        End Get
    End Property

    Public Property UseContextMenu As Boolean
        Get
            Return GetType(ContextMenu) = _Menu.GetType
        End Get
        Set(value As Boolean)
            If (GetType(ContextMenu) = _Menu.GetType) = value Then Return

            Dim newMenu As Primitives.MenuBase = If(value, New ContextMenu, New Menu)
            For Each oItem In _Menu.Items
                newMenu.Items.Add(oItem)
            Next

            _Menu = newMenu

        End Set
    End Property

    Public Sub New()

        Me.Children.Add(_Header)
        Me.Children.Add(_Button)
        Me.Children.Add(_PopUp)
        _Menu = New Menu
        _PopUp.Child = _Menu

        _Header.Name = "txblck"

        _PopUp.IsOpen = False
        _Button.Content = "guzik"
        '            <Popup PlacementTarget="{Binding ElementName=uiAdd}" >

        AddHandler _Button.Click, AddressOf MojaKlik
    End Sub


    Private Sub MojaKlik(sender As Object, e As RoutedEventArgs)
        _PopUp.IsOpen = Not _PopUp.IsOpen
    End Sub
End Class

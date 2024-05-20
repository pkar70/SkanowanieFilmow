

Imports System.Reflection
Imports pkar
Imports pkar.DotNetExtensions

''' <summary>
''' combobox wypełniony nazwami properties z DataContext
''' </summary>
Public Class UserPropSelector
    Inherits ComboBox

    Public Property UseStrings As Boolean
    Public Property UseInts As Boolean
    Public Property UseDates As Boolean
    Public Property UseObjects As Boolean
    Public Property UseBool As Boolean

    Public Property SkipNames As String = ""
    Public Property DefaultSelect As String

    Private _lastTyp As Type


    Public Sub New()
        AddHandler Me.DataContextChanged, AddressOf WypelnCombo
    End Sub

    Private Sub WypelnCombo(sender As Object, e As DependencyPropertyChangedEventArgs)

        If DataContext IsNot Nothing AndAlso _lastTyp IsNot Nothing Then
            If DataContext.GetType = _lastTyp Then Return
        End If
        _lastTyp = DataContext.GetType

        Me.Items.Clear()

            If DataContext Is Nothing Then Return


            Dim typek As Type = DataContext.GetType
        For Each prop As PropertyInfo In typek.GetProperties.OrderBy(Of String)(Function(x) x.Name)
            If SkipNames.ContainsCI("|" & prop.Name & "|") Then Continue For

            Select Case prop.PropertyType
                Case GetType(String)
                    If UseStrings Then AddItem(prop, "s")
                Case GetType(Integer)
                    If UseInts Then AddItem(prop, "i")
                Case GetType(Date)
                    If UseDates Then AddItem(prop, "t")
                Case GetType(Boolean)
                    If UseBool Then AddItem(prop, "b")
                Case Else
                    If UseObjects Then AddItem(prop, "o")
            End Select
        Next

    End Sub

    Private Sub AddItem(prop As PropertyInfo, sufix As String)
        Dim ni As ComboBoxItem = New ComboBoxItem With {.Content = prop.Name & $" ({sufix}): "}
        If prop.Name.EqualsCI(DefaultSelect) Then ni.IsSelected = True
        Me.Items.Add(ni)
    End Sub

    Public Function GetSelectedPropertyName() As String
        Dim oCBI As ComboBoxItem = Me.SelectedItem
        Dim opis As String = oCBI?.Content
        If opis Is Nothing Then Return ""
        Dim iInd As Integer = opis.IndexOf(" ")
        Return opis.Substring(0, iInd)
    End Function

    Public Function GetSelectedPropertyType() As String
        Dim oCBI As ComboBoxItem = Me.SelectedItem
        Dim opis As String = oCBI?.Content
        If opis Is Nothing Then Return ""

        Dim iInd As Integer = opis.IndexOf(" ")
        Return opis.Substring(iInd + 2, 1)
    End Function

    Public Function GetSelectedProperty() As PropertyInfo
        Dim propName As String = GetSelectedPropertyName()
        If propName = "" Then Return Nothing

        Return DataContext.GetType.GetProperty(GetSelectedPropertyName)
    End Function

    Public Function GetSelectedPropertyStringValue() As String
        Dim wart As Object = GetSelectedProperty()?.GetValue(DataContext)
        If wart Is Nothing Then Return ""

        Select Case GetSelectedPropertyType()
            Case "s"
                Return wart
            Case "i"
                Return CInt(wart).ToString
            Case "t"
                Return CDate(wart).ToExifString
            Case "b"
                Return CBool(wart).ToString
            Case "o"
                Dim mystruct As BaseStruct = TryCast(wart, BaseStruct)
                If mystruct Is Nothing Then Return "ERR: cannot dump"
                Return mystruct.DumpAsJSON
        End Select
    End Function

    Public Sub SetSelectedPropertyStringValue(newVal As String)
        Dim prop As PropertyInfo = GetSelectedProperty()

        Select Case GetSelectedPropertyType()
            Case "s"
                prop.SetValue(DataContext, newVal)
                'Case "i"
                '    Return CInt(wart).ToString
                'Case "t"
                '    Return CDate(wart).ToExifString
                'Case "b"
                '    Return CBool(wart).ToString
                'Case "o"
                '    Dim mystruct As BaseStruct = TryCast(wart, BaseStruct)
                '    If mystruct Is Nothing Then Return "ERR: cannot dump"
                '    Return mystruct.DumpAsJSON
        End Select
    End Sub

End Class

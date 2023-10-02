Imports pkar
Imports System.DirectoryServices.ActiveDirectory
Imports System.Drawing
Imports System.Reflection
Imports System.Windows.Automation
Imports System.Windows.Controls.Primitives
Imports Vblib

Public Class UserControlPolaTxt0123

    Public Property FieldsList As String

    Private Sub uiFieldList_Click(sender As Object, e As RoutedEventArgs)
        ' kliknięcie na dowolnym buttonie - po nazwie do sprawdzenia który

        If DataContext Is Nothing Then Return

        Dim uiButt As Button = sender
        If uiButt Is Nothing Then Return

        ' szukamy czy już taki jest, jeśli tak, to tylko przełączamy jego widoczność
        Dim basename As String = uiButt.Name
        Dim oMenu As Menu = uiGrid.FindName(basename & "_Menu")
        If oMenu.Items.Count < 1 Then
            Dim listapol As List(Of String) = GetListaPol()
            If listapol Is Nothing Then Return

            For Each pole As String In listapol
                Dim oNew As New MenuItem With {.Header = pole, .DataContext = basename}
                AddHandler oNew.Click, AddressOf MenuItemClick
                oMenu.Items.Add(oNew)
            Next
        End If

        Dim oPop As Popup = uiGrid.FindName(basename & "_Popup")
        oPop.IsOpen = Not oPop.IsOpen

    End Sub

    Private Function GetListaPol() As List(Of String)
        Select Case FieldsList.ToLowerInvariant
            Case "azure"
                Return New List(Of String) From {"tekst", "DominantColorBackground", "DominantColorForeground", "DominantColors", "Wiekowe"}
            Case "viscros"
                Return New List(Of String) From {"sunrise", "sunset", "moonrise", "moonset", "description", "conditions", "icon"}
            Case "opad"
                Return New List(Of String) From {"RodzajOpadu", "GatunekSniegu", "RodzajPokrywySnieznej"}
            Case "klimat"
                Return New List(Of String) From {"RodzajOpadu"}
            Case "synop"
                Return New List(Of String) From {"StanGruntu"}
        End Select

        Return Nothing  ' nie znam listy pól
    End Function



    'Private Sub AddItems(pop As Menu, fieldsList As String, basename As String)

    '    Dim obj As Object = Nothing
    '    Select Case fieldsList.ToLowerInvariant
    '        Case "azure"
    '            obj = New MojeAzure ' ale tu raczej to nie zadział, bo tam jest "głęboko w dół"
    '        Case "weatherday"
    '            obj = New AutoWeatherDay
    '        Case "weatherhour"
    '            obj = New AutoWeatherHourSingle
    '    End Select
    '    If obj Is Nothing Then Return

    '    For Each prop As PropertyInfo In obj.GetType.GetRuntimeProperties
    '        If prop.PropertyType = GetType(String) Then
    '            Dim oNew As New MenuItem With {.Header = prop.Name, .DataContext = basename}
    '            AddHandler oNew.Click, AddressOf MenuItemClick
    '        End If
    '    Next

    'End Sub

    Private Sub MenuItemClick(sender As Object, e As RoutedEventArgs)
        ' do pola o nazwie DataContext wpisz Header

        Dim oMI As MenuItem = TryCast(sender, MenuItem)
        Dim basename As String = oMI?.DataContext
        If basename Is Nothing Then Return

        Dim oTBox As TextBox = uiGrid.FindName(basename & "_Name")
        If oTBox Is Nothing Then Return

        oTBox.Text = oMI.Header

        Dim oPop As Popup = uiGrid.FindName(basename & "_Popup")
        oPop.IsOpen = False

    End Sub
End Class

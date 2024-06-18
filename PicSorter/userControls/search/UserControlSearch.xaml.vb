Imports Vblib
Imports vb14 = Vblib.pkarlibmodule14
Imports pkar.DotNetExtensions
'Imports pkar.UI.Extensions

Public Class UserControlSearch

    'Public Event Szukajmy As EventHandler

    Private Sub UserControl_Loaded(sender As Object, e As RoutedEventArgs)
        EditExifTag.WypelnComboDeviceType(uiComboDevType, Vblib.FileSourceDeviceTypeEnum.unknown)
        uiComboDevType.SelectedIndex = 0

        FillQueriesCombo()
    End Sub

    Private Sub FillQueriesCombo()
        uiComboQueries.Items.Clear()

        For Each oItem As Vblib.SearchQuery In Application.GetQueries.OrderBy(Function(x) x.nazwa)
            uiComboQueries.Items.Add(New ComboBoxItem With {.Content = oItem.nazwa, .DataContext = oItem})
        Next
    End Sub

#Region "load/save query"

    'Dim _queries As New BaseList(Of SearchQuery)(Application.GetDataFolder, "queries.json")


    Private Async Sub uiSaveQuery_Click(sender As Object, e As RoutedEventArgs)

        Dim query As Vblib.SearchQuery = Await QueryValidityCheck() ' w tym duże litery dla słów kluczowych
        If query Is Nothing Then Return

        Dim nazwa As String = Await vb14.DialogBoxInputAllDirectAsync("Podaj nazwę kwerendy")
        If String.IsNullOrWhiteSpace(nazwa) Then Return

        For Each oItem As Vblib.SearchQuery In Application.GetQueries
            If oItem.nazwa = nazwa Then
                If Not Await vb14.DialogBoxYNAsync("Taka nazwa już istnieje, zamienić?") Then
                    Return
                End If
                ' podmiana query - czyli tutaj usuwamy oryginał, zaraz go zapiszemy ponownie
                Application.GetQueries.Remove(oItem)
                Exit For
            End If
        Next

        query.nazwa = nazwa
        Application.GetQueries.Add(query)
        Application.GetQueries.Save(True)
        Application.GetShareChannels.ReResolveQueries()

        ' żeby zmiany nie były ciągle w tym samym co właśnie zapisane
        DataContext = query.Clone

        FillQueriesCombo()
        For Each oItem As ComboBoxItem In uiComboQueries.Items
            If TryCast(oItem.Content, String) = nazwa Then
                uiComboQueries.SelectedItem = oItem
                Return
            End If
        Next

    End Sub

    Private Sub uiLoadQuery_Click(sender As Object, e As RoutedEventArgs)
        Dim cbi As ComboBoxItem = TryCast(uiComboQueries.SelectedItem, ComboBoxItem)
        Dim oQuery As Vblib.SearchQuery = TryCast(cbi?.DataContext, Vblib.SearchQuery)
        If oQuery Is Nothing Then Return

        DataContext = oQuery.Clone
        uiLoadQuery.IsEnabled = False
    End Sub

    Private Sub uiComboQueries_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        uiLoadQuery.IsEnabled = True
    End Sub

#End Region

    Private Sub UserControl_DataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs)

        Dim query As Vblib.SearchQuery = DataContext

        If query.source_type < 0 Then Return

        If uiComboDevType.Items Is Nothing Then Return
        If uiComboDevType.Items.Count < 1 Then Return
        Dim testItem = uiComboDevType.Items(0)

        If testItem.GetType Is GetType(String) Then
            For Each oItem As String In uiComboDevType.Items
                If oItem Is Nothing Then Continue For
                If oItem.Substring(0, 1) = query.source_type.ToString Then
                    uiComboDevType.SelectedItem = oItem
                    Exit For
                End If
            Next
        Else
            For Each oItem As ComboBoxItem In uiComboDevType.Items
                Dim cbi As String = TryCast(oItem.Content, String)
                If cbi Is Nothing Then Continue For
                If cbi.Substring(0, 1) = query.source_type.ToString Then
                    uiComboDevType.SelectedItem = oItem
                    Exit For
                End If
            Next

        End If


    End Sub

    Private Sub uiComboDevType_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)

        Dim query As Vblib.SearchQuery = DataContext
        If query Is Nothing Then Return
        query.source_type = -1
        Dim sDevType As String = TryCast(uiComboDevType.SelectedValue, String)
        If Not String.IsNullOrWhiteSpace(sDevType) Then
            query.source_type = sDevType.Substring(0, 1)
        End If

    End Sub
    ''' <summary>
    ''' zwróć Query z ewent. dodatkami z kontroli, lub NULL gdy query nie ma sensu
    ''' </summary>
    ''' <returns></returns>
    Public Async Function QueryValidityCheck() As Task(Of Vblib.SearchQuery)
        Dim query As Vblib.SearchQuery = FromUiToQuery()

        ' robimy tak, bo chcemy update w UI oraz w _query; a Binding nie przeniesie przy zmianie od strony kodu
        If Not String.IsNullOrEmpty(query.ogolne.Tags) AndAlso query.ogolne.Tags.ToLowerInvariant = query.ogolne.Tags Then
            If Await vb14.DialogBoxYNAsync("Keywords ma tylko małe litery, czy zmienić na duże?") Then
                'uiTags.Text = uiTags.Text.ToUpper
                query.ogolne.Tags = query.ogolne.Tags.ToUpperInvariant
            End If
        End If

        Dim fname As String = query.ogolne.adv.Filename
        If Not String.IsNullOrWhiteSpace(fname) Then
            If Not fname.Contains("*") AndAlso Not fname.Contains("?") Then
                If Not Await vb14.DialogBoxYNAsync("Filename nie ma * ani ?, czy tak ma być?") Then
                    Return Nothing
                End If
            End If
        End If

        Return query

    End Function


    Private Function FromUiToQuery() As Vblib.SearchQuery
        Dim query As Vblib.SearchQuery = DataContext

        ' daty - UI ma NULL dla nie-selected, a my chcemy mieć wartości
        If Not query.ogolne.MinDateCheck OrElse Not query.ogolne.MaxDate.IsDateValid Then
            query.ogolne.MaxDate = Date.MaxValue
        Else
            ' na północ PO, czyli razem z tym dniem
            query.ogolne.MaxDate = query.ogolne.MaxDate.AddDays(1)
        End If

        If Not query.ogolne.MinDateCheck OrElse Not query.ogolne.MinDate.IsDateValid Then
            query.ogolne.MinDate = Date.MinValue
        End If

        'If query.ogolne_geo.Location IsNot Nothing Then
        '    ' przeliczając z km na metry
        '    query.ogolne_geo.Location.Radius = uiGeoRadius.Text * 1000
        'End If

        'query.ogolne_adv_Source = TryCast(uiComboSource.SelectedValue, String)?.ToLowerInvariant
        If String.IsNullOrWhiteSpace(query.ogolne.adv.Filename) Then
            query.ogolne.adv.Filename = "*"
        Else
            query.ogolne.adv.Filename = query.ogolne.adv.Filename.ToLowerInvariant
        End If

        ' UserControlOgolne : UserKwdEditButton : TextBox
        query.ogolne.Tags = uiOgolne.uiTags.uiSlowka.Text

        Return query

    End Function

End Class

﻿Imports System.Security.Policy
Imports Microsoft.EntityFrameworkCore.Internal
Imports pkar
Imports Vblib
Imports Windows.UI.Core
Imports vb14 = Vblib.pkarlibmodule14
Imports pkar.DotNetExtensions


Public Class UserControlSearch

    'Public Event Szukajmy As EventHandler

    Private Sub UserControl_Loaded(sender As Object, e As RoutedEventArgs)
        EditExifTag.WypelnComboDeviceType(uiComboDevType, Vblib.FileSourceDeviceTypeEnum.unknown)
        uiComboDevType.SelectedIndex = 0

        FillQueriesCombo()
    End Sub

    Private Sub FillQueriesCombo()
        uiComboQueries.Items.Clear()

        For Each oItem As SearchQuery In Application.GetQueries.OrderBy(Function(x) x.nazwa)
            uiComboQueries.Items.Add(New ComboBoxItem With {.Content = oItem.nazwa})
        Next
    End Sub

#Region "load/save query"

    'Dim _queries As New BaseList(Of SearchQuery)(Application.GetDataFolder, "queries.json")


    Private Async Sub uiSaveQuery_Click(sender As Object, e As RoutedEventArgs)

        Dim query As SearchQuery = Await QueryValidityCheck() ' w tym duże litery dla słów kluczowych

        Dim nazwa As String = Await vb14.DialogBoxInputAllDirectAsync("Podaj nazwę kwerendy")
        If String.IsNullOrWhiteSpace(nazwa) Then Return

        For Each oItem As SearchQuery In Application.GetQueries
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
        Dim currName As String = TryCast(uiComboQueries.SelectedItem, String)
        If currName Is Nothing Then Return

        For Each oItem As SearchQuery In Application.GetQueries
            If oItem.nazwa = currName Then
                ' klon - tak żeby grzebanie w danych nie zmieniło oryginału!
                DataContext = oItem.Clone
                Return
            End If
        Next

        vb14.DialogBox("Coś nie tak, nie mogę znaleźć wybranego query?")

    End Sub


#End Region

    Private Sub UserControl_DataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs)
        Dim query As SearchQuery = DataContext

        If query.source_type < 0 Then Return
        For Each oItem As ComboBoxItem In uiComboDevType.Items
            Dim cbi As String = TryCast(oItem.Content, String)
            If cbi Is Nothing Then Continue For
            If cbi.Substring(0, 1) = query.source_type.ToString Then
                uiComboDevType.SelectedItem = oItem
                Exit For
            End If
        Next

    End Sub

    Private Sub uiComboDevType_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)

        Dim query As SearchQuery = DataContext

        query.source_type = -1
        Dim sDevType As String = TryCast(uiComboDevType.SelectedValue, String)
        If Not String.IsNullOrWhiteSpace(sDevType) Then
            query.source_type = sDevType.Substring(0, 1)
        End If

    End Sub

    Public Async Function QueryValidityCheck() As Task(Of SearchQuery)
        Dim query As SearchQuery = FromUiToQuery()

        ' robimy tak, bo chcemy update w UI oraz w _query; a Binding nie przeniesie przy zmianie od strony kodu
        If Not String.IsNullOrEmpty(query.ogolne.Tags) AndAlso query.ogolne.Tags.ToLowerInvariant = query.ogolne.Tags Then
            If Await vb14.DialogBoxYNAsync("Keywords ma tylko małe litery, czy zmienić na duże?") Then
                'uiTags.Text = uiTags.Text.ToUpper
                query.ogolne.Tags = query.ogolne.Tags.ToUpperInvariant
            End If
        End If

        Return query

    End Function

    'Private Async Sub uiSearch_Click(sender As Object, e As RoutedEventArgs)

    '    ' przeniesienie z UI do _query - większość się zrobi samo, ale daty - nie
    '    Dim query As SearchQuery = FromUiToQuery()

    '    ' robimy tak, bo chcemy update w UI oraz w _query; a Binding nie przeniesie przy zmianie od strony kodu
    '    If Not String.IsNullOrEmpty(query.ogolne.Tags) AndAlso query.ogolne.Tags.ToLowerInvariant = query.ogolne.Tags Then
    '        If Await vb14.DialogBoxYNAsync("Keywords ma tylko małe litery, czy zmienić na duże?") Then
    '            'uiTags.Text = uiTags.Text.ToUpper
    '            query.ogolne.Tags = query.ogolne.Tags.ToUpper
    '        End If
    '    End If

    '    Try
    '        RaiseEvent Szukajmy(Me, Nothing)
    '    Catch ex As Exception

    '    End Try
    'End Sub


    Private Function FromUiToQuery() As SearchQuery
        Dim query As SearchQuery = DataContext

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


        Return query

    End Function
End Class

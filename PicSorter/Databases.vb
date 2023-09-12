Imports Vblib

Public Class Databases

    Private _configDir As String
    Private _bazyDanych As List(Of Vblib.DatabaseInterface)

    Sub New(configDir As String)
        _configDir = configDir
        _bazyDanych = New List(Of DatabaseInterface)
        _bazyDanych.Add(New Vblib.DatabaseJSON(configDir))
    End Sub

    Public Function IsAnyEnabled()
        ' foreach, zwraca TRUE jeśli jakas jest enabled
        ' gdy zwroci FALSE z MainWindow: Msg("włącz jakąś bazę danych!")
    End Function

    Public Function InitDbase()

    End Function

    Public Function IsAnyLoaded() As Boolean
        For Each dbase As Vblib.DatabaseInterface In _bazyDanych
            If dbase.IsLoaded Then Return True
        Next
        Return False
    End Function

    Public Function Count() As Integer
        For Each dbase As Vblib.DatabaseInterface In _bazyDanych
            If dbase.IsLoaded Then Return dbase.Count
        Next
        Return -1
    End Function

    ''' <summary>
    ''' jeśli jakaś jest wczytana, to TRUE; wczytaj IsQuick; wczytaj dowolną
    ''' </summary>
    Public Function Load() As Boolean
        For Each dbase As Vblib.DatabaseInterface In _bazyDanych
            If dbase.IsLoaded Then Return True
        Next

        For Each dbase As Vblib.DatabaseInterface In _bazyDanych
            If dbase.IsQuick Then
                If dbase.Load Then Return True
            End If
        Next

        For Each dbase As Vblib.DatabaseInterface In _bazyDanych
            If dbase.Load Then Return True
        Next

        Return False
    End Function


    ''' <summary>
    ''' Dodaj metadane zdjęć do archiwum
    ''' </summary>
    Function AddFiles(nowe As List(Of OnePic))

        Dim bRet As Boolean = False
        For Each dbase As Vblib.DatabaseInterface In _bazyDanych
            bRet = bRet Or dbase.AddFiles(nowe)
        Next

        Return bRet
    End Function

    Function Read()

    End Function

    ''' <summary>
    ''' Zrobienie backupu bazy do katalogu CONFIG (żeby się mógł zrobić backup)
    ''' </summary>
    Function PreBackup()
        ' foreach dbase.addfiles
    End Function

    Function Search(query As SearchQuery, Optional channel As SearchQuery = Nothing) As IEnumerable(Of OnePic)
        ' foreach if dbase.isavailable then dbase.search
        For Each dbase As Vblib.DatabaseInterface In _bazyDanych
            If dbase.IsLoaded Then Return dbase.Search(query, channel)
        Next

        Return Nothing
    End Function


End Class

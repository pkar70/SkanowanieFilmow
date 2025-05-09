﻿Imports Vblib

' nie w vblib, bo Database_SQL

Public Class Databases
    Implements DatabaseInterface

    Private _configDir As String
    Private _bazyDanych As List(Of Vblib.DatabaseInterface)

    Sub New()
        _configDir = Vblib.GetDataFolder
        _bazyDanych = New List(Of DatabaseInterface)
        _bazyDanych.Add(New Vblib.DatabaseJSON(_configDir))
        _bazyDanych.Add(New Database_SQL.DatabaseSQL(_configDir))
    End Sub

    Public ReadOnly Property IsEnabled As Boolean Implements DatabaseInterface.IsEnabled
        Get
            For Each dbase As DatabaseInterface In _bazyDanych
                If dbase.IsEnabled Then Return True
            Next
            Return False
        End Get
    End Property

    Public ReadOnly Property IsLoaded As Boolean Implements DatabaseInterface.IsLoaded
        Get
            For Each dbase As DatabaseInterface In _bazyDanych
                If dbase.IsLoaded Then Return True
            Next
            Return False
        End Get
    End Property

    Public ReadOnly Property IsEditable As Boolean Implements DatabaseInterface.IsEditable
        Get
            For Each dbase As DatabaseInterface In _bazyDanych
                If dbase.IsEditable Then Return True
            Next
            Return False
        End Get
    End Property

    Public ReadOnly Property IsAllEditable As Boolean
        Get
            For Each dbase As DatabaseInterface In _bazyDanych
                If Not dbase.IsEditable Then Return False
            Next
            Return True
        End Get
    End Property

    Public ReadOnly Property IsQuick As Boolean Implements DatabaseInterface.IsQuick
        Get
            For Each dbase As DatabaseInterface In _bazyDanych
                If dbase.IsQuick Then Return True
            Next
            Return False
        End Get
    End Property

    ReadOnly Property Nazwa As String Implements DatabaseInterface.Nazwa
        Get
            Return "ALL"
        End Get
    End Property


    Public Function Count() As Integer Implements DatabaseInterface.Count
        For Each dbase As Vblib.DatabaseInterface In _bazyDanych
            If dbase.IsLoaded Then Return dbase.Count
        Next
        Return -1
    End Function

    ''' <summary>
    ''' jeśli jakaś jest wczytana, to TRUE; wczytaj IsQuick; wczytaj dowolną
    ''' </summary>
    Public Function Load() As Boolean Implements DatabaseInterface.Load

        For Each dbase As Vblib.DatabaseInterface In _bazyDanych
            If dbase.IsLoaded Then Return True
        Next

        For Each dbase As Vblib.DatabaseInterface In _bazyDanych
            If dbase.IsEnabled AndAlso dbase.IsQuick Then
                If dbase.Load Then Return True
            End If
        Next

        For Each dbase As Vblib.DatabaseInterface In _bazyDanych
            If dbase.IsEnabled AndAlso dbase.Load Then Return True
        Next

        Return False
    End Function

    Public Sub Unload() Implements DatabaseInterface.Unload
        For Each dbase As Vblib.DatabaseInterface In _bazyDanych
            If dbase.IsLoaded Then
                dbase.Unload()
            End If
        Next
    End Sub


    ''' <summary>
    ''' Dodaj metadane zdjęć do archiwum
    ''' </summary>
    Function AddFiles(nowe As IEnumerable(Of OnePic)) As Boolean Implements DatabaseInterface.AddFiles

        Dim bRet As Boolean = False
        For Each dbase As Vblib.DatabaseInterface In _bazyDanych
            If dbase.IsEnabled Then
                bRet = bRet Or dbase.AddFiles(nowe)
            End If
        Next

        Return bRet
    End Function


    ''' <summary>
    ''' Zrobienie backupu bazy do katalogu CONFIG (żeby się mógł zrobić backup)
    ''' </summary>
    Function PreBackup() As Boolean Implements DatabaseInterface.PreBackup

        Dim bRet As Boolean = True
        For Each dbase As Vblib.DatabaseInterface In _bazyDanych
            If Not dbase.PreBackup() Then bRet = False
        Next

        Return bRet
    End Function

    Function Search(query As SearchQuery) As IEnumerable(Of OnePic) Implements DatabaseInterface.Search
        ' foreach if dbase.isavailable then dbase.search
        For Each dbase As Vblib.DatabaseInterface In _bazyDanych
            If dbase.IsLoaded Then Return dbase.Search(query)
        Next

        Return Nothing
    End Function

    Public Function Search(channel As ShareChannel, sinceId As String) As IEnumerable(Of OnePic) Implements DatabaseInterface.Search
        For Each dbase As Vblib.DatabaseInterface In _bazyDanych
            If dbase.IsLoaded Then Return dbase.Search(channel, sinceId)
        Next

        Return Nothing
    End Function

    Public Function Search(shareLogin As ShareLogin, sinceId As String) As IEnumerable(Of OnePic) Implements DatabaseInterface.Search
        For Each dbase As Vblib.DatabaseInterface In _bazyDanych
            If dbase.IsLoaded Then Return dbase.Search(shareLogin, sinceId)
        Next

        Return Nothing
    End Function

    Public Function Connect() As Boolean Implements DatabaseInterface.Connect
        Vblib.DialogBox("na poziomie Databases nie powinno być Connect")
        Return Nothing
    End Function

    Public Function Init() As Boolean Implements DatabaseInterface.Init
        Vblib.DialogBox("na poziomie Databases nie powinno być Init")
        Return Nothing
    End Function

    Public Function ImportFrom(prevDbase As DatabaseInterface) As Integer Implements DatabaseInterface.ImportFrom
        Throw New NotImplementedException()
    End Function

    Public Function AddExif(picek As OnePic, oExif As ExifTag) As Boolean Implements DatabaseInterface.AddExif
        Throw New NotImplementedException()
    End Function

    Public Function AddKeyword(picek As OnePic, oKwd As OneKeyword) As Boolean Implements DatabaseInterface.AddKeyword
        Throw New NotImplementedException()
    End Function

    Public Function AddDescription(picek As OnePic, sDesc As String) As Boolean Implements DatabaseInterface.AddDescription
        Throw New NotImplementedException()
    End Function

    Public Function GetAll() As IEnumerable(Of OnePic) Implements DatabaseInterface.GetAll
        Vblib.pkarlibmodule14.DialogBox("na poziomie Databases nie powinno być GetAll")
        Return Nothing
    End Function

    Public Function GetFirstLoaded() As DatabaseInterface
        For Each dbase As Vblib.DatabaseInterface In _bazyDanych
            If dbase.IsLoaded Then Return dbase
        Next
        Return Nothing
    End Function

    Private Function FindDatabase(nazwa As String) As DatabaseInterface
        For Each dbase As Vblib.DatabaseInterface In _bazyDanych
            If dbase.Nazwa = nazwa Then Return dbase
        Next
        Return Nothing
    End Function

    Public Function CopyDatabase(srcName As String, dstName As String) As Integer

        Dim srcBase As DatabaseInterface = FindDatabase(srcName)
        If srcBase Is Nothing Then Return -1
        If Not srcBase.IsEnabled Then Return -2

        Dim dstBase As DatabaseInterface = FindDatabase(dstName)
        If dstBase Is Nothing Then Return -11

        If Not srcBase.IsLoaded Then srcBase.Load()

        Return dstBase.ImportFrom(srcBase)
    End Function

    Public Function Connect(dbaseName As String) As Boolean

        Dim dbase As DatabaseInterface = FindDatabase(dbaseName)
        If dbase Is Nothing Then Return False

        Return dbase.Connect

    End Function

    Public Function Disconnect(dbaseName As String) As Boolean

        Dim dbase As DatabaseInterface = FindDatabase(dbaseName)
        If dbase Is Nothing Then Return False

        Try
            Return dbase.Disconnect
        Catch ex As Exception
            Return False
        End Try

    End Function

    Public Function Init(dbaseName As String) As Boolean
        Dim dbase As DatabaseInterface = FindDatabase(dbaseName)
        If dbase Is Nothing Then Return False

        Return dbase.Init
    End Function

    Public Function Disconnect() As Boolean Implements DatabaseInterface.Disconnect
        Throw New NotImplementedException()
    End Function

    Public Sub SaveData() Implements DatabaseInterface.SaveData


        If Not IsEditable Then
            Vblib.MsgBox("Nie umiem zapisać baz danych!")
            Return
        End If

        For Each dbase As DatabaseInterface In _bazyDanych
            If dbase.IsEditable Then
                dbase.SaveData()
            End If
        Next

        Vblib.MsgBox("TESTUJEMY sprawdz zapis pliku archindexfull")

    End Sub
End Class

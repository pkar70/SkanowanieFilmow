
'Imports System.Security.Cryptography
'Imports Microsoft.Rest
'Imports Newtonsoft.Json

Imports Newtonsoft.Json
Imports Newtonsoft.Json.Schema

Public Class OneDir
    Inherits pkar.BaseStruct

    Public Property sId As String
    'Public Property sDisplayName As String
    Public Property notes As String
    Public Property denyPublish As Boolean

    Public Property SubItems As List(Of OneDir)
    Public Property sParentId As String

    <JsonIgnore>
    Public Property fullPath As String

    Public Function ToComboDisplayName(Optional bPath As Boolean = False) As String

        Dim sRet As String = If(bPath, fullPath, sId)
        If String.IsNullOrWhiteSpace(notes) Then Return sRet
        sRet &= " ("
        If notes.Length < 24 Then Return sRet & notes & ")"

        Return sRet & notes.Substring(0, 23) & "…)"

    End Function

    Public Function ToFlatList() As List(Of OneDir)
        'DumpCurrMethod(sId)
        Dim lista As New List(Of OneDir)

        lista.Add(Me)

        If SubItems IsNot Nothing Then
            For Each oChild As OneDir In SubItems
                lista = lista.Concat(oChild.ToFlatList).ToList
            Next
        End If

        Return lista
    End Function

    Public Function IsFromDate() As Boolean
        Return IsFromDate(sId)
    End Function

    Public Shared Function IsFromDate(sId As String) As Boolean
        ' daty 1850-2050, format yyyy.MM

        If sId.Length < 22 Then Return False
        If sId.Substring(4, 1) <> "." Then Return False

        Dim temp As Integer
        Try
            temp = Integer.Parse(sId.Substring(0, 4))
        Catch ex As Exception
            Return False
        End Try
        If temp < 1850 Then Return False
        If temp > Date.Now.Year Then Return False   ' zdjęć z przyszlosci nie uznajemy

        Try
            temp = Integer.Parse(sId.Substring(5, 2))
        Catch ex As Exception
            Return False
        End Try

        If temp < 1 Then Return False
        If temp > 12 Then Return False

        Return True

    End Function



    Public Const RootId As String = "(root)"

    Public Function IsRoot() As Boolean
        Return sId = RootId
    End Function


End Class

Public Class DirsList
    Inherits pkar.BaseList(Of OneDir)

    Public Sub New(sFolder As String)
        ' uwaga: nazwa pliku takze w SettingsDirTree
        MyBase.New(sFolder, "dirstree.json")
    End Sub

    Public Overrides Function Load() As Boolean
        Dim bRet As Boolean = MyBase.Load()
        If Count() < 1 Then Return bRet

        CalculateFullPaths()

        Return bRet
    End Function

    Private Sub CalculateFullPaths()

        ' teoretycznie będzie tylko jeden item na tym poziomie: root
        For Each oItem As OneDir In _lista

            If oItem.SubItems Is Nothing Then Return

            ' iterujemy te pod ROOT
            For Each oSubitem As OneDir In oItem.SubItems
                CalculateFullPaths(oSubitem, "")
            Next
        Next
    End Sub

    Private Sub CalculateFullPaths(oItem As OneDir, sParentPath As String)
        If sParentPath = "" Then
            oItem.fullPath = oItem.sId
        Else
            oItem.fullPath = IO.Path.Combine(sParentPath, oItem.sId)
        End If
        If oItem.SubItems Is Nothing Then Return

        For Each oSubitem As OneDir In oItem.SubItems
            CalculateFullPaths(oSubitem, oItem.fullPath)
        Next
    End Sub

    Public Function GetDir(sKey As String) As OneDir
        For Each oItem As OneDir In ToFlatList()
            If oItem.sId = sKey Then Return oItem
        Next

        Return Nothing
    End Function

    'Public Function GetFullPath(sKey As String) As String

    '    Dim oItem As OneDir = GetDir(sKey)
    '    If oItem Is Nothing Then Return ""

    '    Return oItem.fullPath

    'End Function

    'Public Function GetFullPath(oDir As OneDir) As String

    '    If Not String.IsNullOrWhiteSpace(oDir.fullPath) Then Return oDir.fullPath

    '    If String.IsNullOrWhiteSpace(oDir.sParentId) Then Return oDir.sId
    '    If oDir.sParentId = OneDir.RootId Then Return oDir.sId
    '    Return IO.Path.Combine(GetFullPath(oDir.sParentId), oDir.sId)

    'End Function

    Public Function ToFlatList() As List(Of OneDir)
        Dim lista As New List(Of OneDir)

        For Each oItem As OneDir In _lista
            lista = lista.Concat(oItem.ToFlatList).ToList
        Next

        Return lista
    End Function

    Public Function GetFolder(sKey As String) As OneDir
        For Each oItem As OneDir In _lista
            If oItem.sId = sKey Then Return oItem
        Next

        Return Nothing
    End Function

    'Public Function TryAddFolder(sFolderPath As String, sOpis As String) As Boolean
    '    ' trudne, bo musimy iść całą ścieżką
    '    ' *TODO* zrobić może, choc tylko wykorzystane w (Local|Cloud)Archive, gdzie aktualnie jest REM
    '    If GetFolder(sFolderPath) IsNot Nothing Then Return False

    '    Dim oNew As New OneDir
    '    oNew.sId = sFolderPath
    '    If Not String.IsNullOrWhiteSpace(sOpis) Then oNew.notes = sOpis
    '    _lista.Add(New OneDir() With {.sId = sFolderPath})
    '    Return True
    'End Function

    Public Function TryAddSubdir(oItem As OneDir, sId As String, sOpis As String) As OneDir
        If oItem.SubItems IsNot Nothing Then
            For Each oSub As OneDir In oItem.SubItems
                If oSub.sId = sId Then Return oSub
            Next
        End If

        Dim oNew As New OneDir
        'oNew.sParentId = oItem.sId
        oNew.sId = sId
        oNew.notes = sOpis
        oNew.fullPath = IO.Path.Combine(oItem.fullPath, oNew.sId)
        If oItem.SubItems Is Nothing Then oItem.SubItems = New List(Of OneDir)
        oItem.SubItems.Add(oNew)

        Return oNew
    End Function

    Public Sub AddSubfolderTree(oItem As OneDir, sFolder As String)
        DumpCurrMethod(sFolder)

        For Each sDir As String In IO.Directory.EnumerateDirectories(sFolder)
            Dim oNew As New OneDir
            oNew.sId = IO.Path.GetFileName(sDir)
            'oNew.sParentId = IO.Path.GetFileName(sFolder)
            oNew.fullPath = IO.Path.Combine(oItem.fullPath, oNew.sId)
            'Public Property notes As String
            'Public Property denyPublish As Boolean
            ' Public Property SubItems As List(Of OneDir)
            oNew.fullPath = IO.Path.Combine(oItem.fullPath, oNew.sId)
            If oItem.SubItems Is Nothing Then oItem.SubItems = New List(Of OneDir)
            oItem.SubItems.Add(oNew)

            AddSubfolderTree(oNew, sDir)
        Next

    End Sub

    Protected Overrides Sub InsertDefaultContent()

        Dim oRoot As New OneDir With {.sId = OneDir.RootId, .notes = "Główny katalog"}
        oRoot.SubItems = New List(Of OneDir)
        oRoot.SubItems.Add(New OneDir With {.sId = "Imprezy", .notes = "wydarzenia"})
        oRoot.SubItems.Add(New OneDir With {.sId = "Wyjazdy", .notes = "wakacje, itp."})
        oRoot.SubItems.Add(New OneDir With {.sId = "Rodzina", .notes = "inne"})
        oRoot.SubItems.Add(New OneDir With {.sId = "Muzeum", .notes = "na wieczną rzeczy pamiątkę"})

        _lista.Add(oRoot)

    End Sub

End Class


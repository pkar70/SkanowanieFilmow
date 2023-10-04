
'Imports System.Security.Cryptography
'Imports Microsoft.Rest
'Imports Newtonsoft.Json

Imports System.Linq.Expressions
Imports pkar.DotNetExtensions

Public Class OneDir
    Inherits pkar.BaseStruct

    Public Property sId As String
    'Public Property sDisplayName As String
    Public Property notes As String
    Public Property denyPublish As Boolean

    Public Property SubItems As List(Of OneDir)
    Public Property sParentId As String

    <Newtonsoft.Json.JsonIgnore>
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
        Return GetDate(sId).IsDateValid

        'If sId.Length < 10 Then Return False
        'If sId.Substring(4, 1) <> "." Then Return False

        'Dim temp As Integer
        'Try
        '    temp = Integer.Parse(sId.Substring(0, 4))
        'Catch ex As Exception
        '    Return False
        'End Try
        'If temp < 1850 Then Return False
        'If temp > Date.Now.Year Then Return False   ' zdjęć z przyszlosci nie uznajemy

        'Try
        '    temp = Integer.Parse(sId.Substring(5, 2))
        'Catch ex As Exception
        '    Return False
        'End Try

        'If temp < 1 Then Return False
        'If temp > 12 Then Return False

        'Return True

    End Function

    Public Function GetDate() As Date
        Return GetDate(sId)
    End Function


    Public Shared Function GetDate(sId As String) As Date

        Dim invalidDate As Date = New Date(2200, 1, 1)

        If sId.Length < 10 Then Return invalidDate
        If sId.Substring(4, 1) <> "." Then Return invalidDate

        Dim rok As Integer
        Try
            rok = Integer.Parse(sId.Substring(0, 4))
        Catch ex As Exception
            Return invalidDate
        End Try
        If rok < 1850 Then Return invalidDate
        If rok > Date.Now.Year Then Return invalidDate ' zdjęć z przyszlosci nie uznajemy

        Dim month As Integer
        Try
            month = Integer.Parse(sId.Substring(5, 2))
        Catch ex As Exception
            Return invalidDate
        End Try

        If month < 1 Then Return invalidDate
        If month > 12 Then Return invalidDate

        Dim day As Integer
        Try
            day = Integer.Parse(sId.Substring(8, 2))
        Catch ex As Exception
            Return New Date(rok, month, 1)
        End Try

        If day < 1 Then Return invalidDate
        If day > 31 Then Return invalidDate

        Return New Date(rok, month, day)

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
        For Each oItem As OneDir In Me

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

    ''' <summary>
    ''' znajdź OneDir dla podanej ścieżki, lub stwórz taki obiekt
    ''' </summary>
    ''' <param name="sTargetDir">ścieżka docelowa</param>
    ''' <param name="bCreate">True gdy ma tworzyć obiekty</param>
    ''' <returns>OneDir, lub Null = error</returns>
    Public Function GetDirFromTargetDir(sTargetDir As String, Optional bCreate As Boolean = False) As OneDir

        If String.IsNullOrWhiteSpace(sTargetDir) Then Return Nothing

        ' najpierw wedle istniejącej ścieżki, bo może być taka sama nazwa w dwu miejscach
        Dim bFound As Boolean = False

        ' ten pierwszy to root
        Dim oRet As OneDir = ElementAt(0)
        For Each sPath As String In sTargetDir.Split(IO.Path.DirectorySeparatorChar)
            If sPath.Trim.Length < 1 Then Continue For
            If oRet.SubItems Is Nothing Then
                If Not bCreate Then Continue For
                TryAddSubdir(oRet, sPath, "")
            End If

            For Each oSubDir As OneDir In oRet.SubItems
                If oSubDir.sId <> sPath Then Continue For

                oRet = oSubDir
                bFound = True
                Exit For
            Next

            If Not bFound Then Exit For
        Next

        If bFound Then Return oRet

        ' a potem tylko ten ostatni człon szukamy - jakby ktoś przestawił
        Return GetFolder(IO.Path.GetFileName(sTargetDir))
    End Function


    'Public Function GetFullPath(sKey As String) As String

    '    Dim oParent As OneDir = GetDir(sKey)
    '    If oParent Is Nothing Then Return ""

    '    Return oParent.fullPath

    'End Function

    'Public Function GetFullPath(oDir As OneDir) As String

    '    If Not String.IsNullOrWhiteSpace(oDir.fullPath) Then Return oDir.fullPath

    '    If String.IsNullOrWhiteSpace(oDir.sParentId) Then Return oDir.sId
    '    If oDir.sParentId = OneDir.RootId Then Return oDir.sId
    '    Return IO.Path.Combine(GetFullPath(oDir.sParentId), oDir.sId)

    'End Function

    Public Function ToFlatList() As List(Of OneDir)
        Dim lista As New List(Of OneDir)

        ForEach(Sub(x) lista = lista.Concat(x.ToFlatList).ToList)

        Return lista
    End Function

    Public Function GetFolder(sKey As String) As OneDir
        Return Find(Function(x) x.sId = sKey)
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


    Public Function TryAddSubdir(oParent As OneDir, sId As String, sOpis As String) As OneDir
        If oParent.SubItems IsNot Nothing Then
            For Each oSub As OneDir In oParent.SubItems
                If oSub.sId = sId Then Return oSub
            Next
        End If

        Dim oNew As New OneDir
        'oNew.sParentId = oParent.sId
        oNew.sId = sId
        oNew.notes = sOpis
        oNew.fullPath = IO.Path.Combine(oParent.fullPath, oNew.sId)
        If oParent.SubItems Is Nothing Then oParent.SubItems = New List(Of OneDir)
        oParent.SubItems.Add(oNew)

        Save(True)

        Return oNew
    End Function

    Public Sub AddSubfolderTree(oParent As OneDir, sFolder As String)
        DumpCurrMethod(sFolder)

        For Each sDir As String In IO.Directory.EnumerateDirectories(sFolder)
            Dim oNew As New OneDir
            oNew.sId = IO.Path.GetFileName(sDir)
            'oNew.sParentId = IO.Path.GetFileName(sFolder)
            oNew.fullPath = IO.Path.Combine(oParent.fullPath, oNew.sId)
            'Public Property notes As String
            'Public Property denyPublish As Boolean
            ' Public Property SubItems As List(Of OneDir)
            oNew.fullPath = IO.Path.Combine(oParent.fullPath, oNew.sId)
            If oParent.SubItems Is Nothing Then oParent.SubItems = New List(Of OneDir)
            oParent.SubItems.Add(oNew)

            AddSubfolderTree(oNew, sDir)
        Next

    End Sub

    Public Function IdExists(sId As String) As Boolean
        For Each oDir As OneDir In ToFlatList()
            If oDir.sId Is Nothing Then Continue For
            If oDir.sId.EqualsCI(sId) Then Return True
        Next

        Return False
    End Function

    Protected Overrides Sub InsertDefaultContent()

        Dim oRoot As New OneDir With {.sId = OneDir.RootId, .notes = "Główny katalog"}
        oRoot.SubItems = New List(Of OneDir)
        oRoot.SubItems.Add(New OneDir With {.sId = "Imprezy", .notes = "wydarzenia"})
        oRoot.SubItems.Add(New OneDir With {.sId = "Wyjazdy", .notes = "wakacje, itp."})
        oRoot.SubItems.Add(New OneDir With {.sId = "Rodzina", .notes = "inne"})
        oRoot.SubItems.Add(New OneDir With {.sId = "Muzeum", .notes = "na wieczną rzeczy pamiątkę"})

        Add(oRoot)

    End Sub

End Class


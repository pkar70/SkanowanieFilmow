
' editable: jeśli tak, to musi być SAVE, po ktorym trzeba ściąć "]" z końca pliku!


Public Class DatabaseJSON
    Implements DatabaseInterface

    Private _dataFilenameFull As String
    Private _configDir As String

    Sub New(configDir As String)
        ' katalog jest dla PreBackup oraz na to skąd czytać dane konfiguracyjne (jeśli takie kiedyś będą)
        _configDir = configDir

        _dataFilenameFull = IO.Path.Combine(_configDir, "archIndexFull.json")
        _allItems = New pkar.BaseList(Of OnePic)(_configDir, "archIndexFull.json")
    End Sub

    Private _allItems As pkar.BaseList(Of Vblib.OnePic)

    Public ReadOnly Property IsEnabled As Boolean Implements DatabaseInterface.IsEnabled
        Get
            Return GetSettingsBool("dbase.json.enabled", True)
        End Get
    End Property

    Private _IsLoaded As Boolean = False

    Public ReadOnly Property IsLoaded As Boolean Implements DatabaseInterface.IsLoaded
        Get
            Return _IsLoaded
        End Get
    End Property

    Private _IsEditable As Boolean = False

    Public ReadOnly Property IsEditable As Boolean Implements DatabaseInterface.IsEditable
        Get
            Return _IsEditable
        End Get
    End Property

    Public ReadOnly Property IsQuick As Boolean Implements DatabaseInterface.IsQuick
        Get
            Return False
        End Get
    End Property

    Public Function Count() As Integer Implements DatabaseInterface.Count
        If Not IsLoaded Then Return -1
        Return _allItems.Count
    End Function

    Public Function AddFiles(nowe As IEnumerable(Of OnePic)) As Boolean Implements DatabaseInterface.AddFiles
        If Not IsEnabled Then Return False

        Dim sIndexLongJson As String = ""

        For Each oPic As OnePic In nowe
            If sIndexLongJson <> "" Then sIndexLongJson &= ","
            sIndexLongJson &= oPic.DumpAsJSON(True)

            If IsLoaded Then _allItems.Add(oPic)
        Next

        If Not IO.File.Exists(_dataFilenameFull) Then
            IO.File.WriteAllText(_dataFilenameFull, "[")
        Else
            ' skoro już mamy coś w pliku, to teraz dodajemy do tego przecinek - pomiędzy itemami
            sIndexLongJson = "," & vbCrLf & sIndexLongJson
        End If

        IO.File.AppendAllText(_dataFilenameFull, sIndexLongJson)

        Return True
    End Function

    Public Function Connect() As Boolean Implements DatabaseInterface.Connect
        Return True
    End Function

    Public Function PreBackup() As Boolean Implements DatabaseInterface.PreBackup
        Return True
    End Function

    Public Function Search(query As SearchQuery, Optional channel As SearchQuery = Nothing) As IEnumerable(Of OnePic) Implements DatabaseInterface.Search

        If Not IsLoaded Then Return Nothing

        If channel Is Nothing Then
            ' dziwne, ale 5 razy takie wyszło (null) - dwa przecinki pod rząd, pewnie przy dodawaniu do archiwumm
            Return _allItems.GetList.Where(Function(x) If(x?.CheckIfMatchesQuery(query), False))
        Else
            Return _allItems.GetList.Where(Function(x) x.CheckIfMatchesQuery(query)).Where(Function(x) x.CheckIfMatchesQuery(channel))
        End If

    End Function

    Public Function Init() As Boolean Implements DatabaseInterface.Init
        Return True
    End Function

    Public Function ImportFrom(prevDbase As DatabaseInterface) As Integer Implements DatabaseInterface.ImportFrom
        ' tworzy nowy plik JSON - może z pytaniem czy na pewno
        Throw New NotImplementedException()
    End Function

    Public Function AddExif(picek As OnePic, oExif As ExifTag) As Boolean Implements DatabaseInterface.AddExif
        If Not IsEditable Then Return False

        Throw New NotImplementedException()
    End Function

    Public Function AddKeyword(picek As OnePic, oKwd As OneKeyword) As Boolean Implements DatabaseInterface.AddKeyword
        If Not IsEditable Then Return False

        Throw New NotImplementedException()
    End Function

    Public Function AddDescription(picek As OnePic, sDesc As String) As Boolean Implements DatabaseInterface.AddDescription
        If Not IsEditable Then Return False

        Throw New NotImplementedException()
    End Function

    Public Function Load() As Boolean Implements DatabaseInterface.Load
        Dim bRet As Boolean = _allItems.Load()
        If bRet Then _IsLoaded = True
        Return bRet
    End Function

End Class



' editable: jeśli tak, to musi być SAVE, po ktorym trzeba ściąć "]" z końca pliku!


Imports System.IO

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
            Return GetSettingsBool("uiJsonEnabled", True)
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

    ReadOnly Property Nazwa As String Implements DatabaseInterface.Nazwa
        Get
            Return "JSON"
        End Get
    End Property

    Public Function Count() As Integer Implements DatabaseInterface.Count
        If Not IsLoaded Then Return -1
        Return _allItems.Count
    End Function

    Public Function AddFiles(nowe As IEnumerable(Of OnePic)) As Boolean Implements DatabaseInterface.AddFiles
        If nowe Is Nothing Then Return True
        If nowe.Count < 1 Then Return True

        If Not IsEnabled Then Return False

        Dim sIndexLongJson As String = ""

        For Each oPic As OnePic In nowe
            ' czasem są NULLe w archindex (dwa przecinki), może tak się tego pozbędę
            If oPic Is Nothing Then Continue For

            If sIndexLongJson <> "" Then sIndexLongJson &= ","
            sIndexLongJson &= oPic.DumpAsJSON(True)

            ' jeśli mamy wczytany index do pamięci, to trzeba go zaktualizować
            If IsLoaded Then _allItems.Add(oPic)
        Next

        ' ale jeśli lista była pusta, to nic nie dopisujemy, żadnego przecinka :)
        If sIndexLongJson = "" Then Return True

        If Not IO.File.Exists(_dataFilenameFull) Then
            IO.File.WriteAllText(_dataFilenameFull, "[")
        Else
            ' skoro już mamy coś w pliku, to teraz dodajemy do tego przecinek - pomiędzy itemami
            sIndexLongJson = "," & vbCrLf & sIndexLongJson
            TrimEndListFromFile()
        End If

        IO.File.AppendAllText(_dataFilenameFull, sIndexLongJson)

        Return True
    End Function

    Public Function Connect() As Boolean Implements DatabaseInterface.Connect
        Return True
    End Function

    Public Function Disconnect() As Boolean Implements DatabaseInterface.Disconnect
        Return True
    End Function

    Public Function PreBackup() As Boolean Implements DatabaseInterface.PreBackup
        Return True
    End Function

    Public Function Search(query As SearchQuery) As IEnumerable(Of OnePic) Implements DatabaseInterface.Search

        If Not IsLoaded Then Return Nothing

        ' *TODO* optymalizacja query, przez użycie x.SumKwds.Contains(query.tags), i dopiero na koncu to co zostanie idzie wedlug pelnego szukania - pozniej bedzie dobre dla SQLa

        Dim lista As IEnumerable(Of OnePic) = _allItems

        ' etap 1 - słowa kluczowe
        If Not String.IsNullOrWhiteSpace(query.ogolne.Tags) Then
            For Each kwd As String In query.ogolne.Tags.Split(" ")
                If kwd.Substring(0, 1) = "!" Then
                    Dim notkwd As String = kwd.Substring(1)
                    lista = lista.Where(Function(x) Not If(x?.sumOfKwds.Contains(notkwd), False))
                Else
                    lista = lista.Where(Function(x) If(x?.sumOfKwds.Contains(kwd), False))
                End If
            Next
        End If

        ' etap 2 - wedle serno
        If query.ogolne.serno > 0 Then
            lista = lista.Where(Function(x) If(x?.serno, 0) = query.ogolne.serno)
        End If

        ' etap LAST - dalsze szukanie
        ' dziwne, ale 5 razy takie wyszło (null) - dwa przecinki pod rząd, pewnie przy dodawaniu do archiwumm
        Return lista.Where(Function(x) If(x?.CheckIfMatchesQuery(query), False))

    End Function

    Function Search(channel As ShareChannel, sinceId As String) As IEnumerable(Of OnePic) Implements DatabaseInterface.Search
        Dim lista As New List(Of OnePic)
        SearchChannel(lista, channel, sinceId, "")

        Return lista
    End Function

    ' do listy dodaje pasujące do kanału
    Private Sub SearchChannel(lista As List(Of OnePic), channel As ShareChannel, sinceId As String, processing As String)
        Dim bCopy As Boolean = False
        If String.IsNullOrWhiteSpace(sinceId) Then bCopy = True

        For Each oItem As OnePic In _allItems
            If oItem Is Nothing Then Continue For ' pomijamy ewentualne puste

            ' pomijamy przed identyfikatorem
            If Not bCopy Then
                If oItem.PicGuid <> sinceId Then Continue For
                bCopy = True
            End If

            ' czy jest na liście wyjątków?
            If channel.exclusions.Contains(oItem.PicGuid) Then Continue For

            For Each queryDef As ShareQueryProcess In channel.queries
                If oItem.CheckIfMatchesQuery(queryDef.query) Then
                    oItem.toProcessed = queryDef.processing & ";" & channel.processing
                    If Not String.IsNullOrWhiteSpace(processing) Then oItem.toProcessed &= ";" & processing
                    If Not lista.Exists(Function(x) x.sSuggestedFilename = oItem.sSuggestedFilename) Then
                        lista.Add(oItem)
                    End If
                End If
            Next

        Next

    End Sub


    Function Search(shareLogin As ShareLogin, sinceId As String) As IEnumerable(Of OnePic) Implements DatabaseInterface.Search
        Dim lista As New List(Of OnePic)

        For Each channelProc As ShareChannelProcess In shareLogin.channels
            SearchChannel(lista, channelProc.channel, sinceId, channelProc.processing)
        Next

        ' *TODO* exclusions loginu

        For Each oItem As OnePic In lista
            oItem.toProcessed &= shareLogin.processing
        Next

        Return lista

    End Function


    Public Function Init() As Boolean Implements DatabaseInterface.Init
        Return True
    End Function

    Public Function ImportFrom(prevDbase As DatabaseInterface) As Integer Implements DatabaseInterface.ImportFrom
        ' tworzy nowy plik JSON - może z pytaniem czy na pewno

        ' najpierw kasujemy aktualny
        If IO.File.Exists(_dataFilenameFull) Then
            IO.File.Delete(_dataFilenameFull & ".bak")
            IO.File.Move(_dataFilenameFull, _dataFilenameFull & ".bak")
        End If

        Dim iCount As Integer = 0

        Using sr As StreamWriter = IO.File.CreateText(_dataFilenameFull)
            For Each oPic As OnePic In prevDbase.GetAll
                If oPic Is Nothing Then Continue For
                If iCount > 0 Then sr.WriteLine(",")
                sr.Write(oPic.DumpAsJSON)
                iCount += 1
            Next
            sr.Flush()
        End Using

        Return iCount

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
        If bRet Then
            _IsLoaded = True
            ' recalculate sums
            _allItems.ForEach(Sub(x) x.RecalcSumsy())
        End If
        Return bRet
    End Function

    ''' <summary>
    ''' usunięcie końcowego "]" z pliku (jeśli taki jest)
    ''' </summary>
    Private Sub TrimEndListFromFile()
        If Not IO.File.Exists(_dataFilenameFull) Then Return
        Dim fi As New IO.FileInfo(_dataFilenameFull)
        If fi.Length < 1 Then Return

        Dim iInd As Long
        ' wczytaj ostatnie 10 bajtów
        Using sr As StreamReader = IO.File.OpenText(_dataFilenameFull)
            sr.BaseStream.Seek(-10, SeekOrigin.End)
            Dim lastchars As String = sr.ReadToEnd
            If Not lastchars.Trim.EndsWith("]") Then Return

            iInd = lastchars.LastIndexOf("]")
            'sr.BaseStream.SetLength(fi.Length - 10 + iInd)
        End Using

        ' długość pliku powinna być przycięta...
        DialogBox("uwaga! dokładnie sprawdź czy zadziała, po BACKUP! - ucinanie ']'")
        Return
        Using sw As FileStream = IO.File.OpenWrite(_dataFilenameFull)
            sw.SetLength(fi.Length - 10 + iInd)
        End Using

    End Sub

    Public Function GetAll() As IEnumerable(Of OnePic) Implements DatabaseInterface.GetAll
        If Not IsLoaded Then Return Nothing
        Return _allItems
    End Function
End Class


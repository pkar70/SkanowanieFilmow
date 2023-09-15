
' najpierw bezpośrednio klasa DatabaseJSON, potem zamienić na databases?


Public Interface DatabaseInterface

    ReadOnly Property IsEnabled As Boolean
    ReadOnly Property IsLoaded As Boolean
    ReadOnly Property IsEditable As Boolean
    ReadOnly Property IsQuick As Boolean
    ReadOnly Property Nazwa As String

    Function Count() As Integer

    ''' <summary>
    '''  podłączenie - JSON: nic, SQL: login
    ''' </summary>
    ''' <returns>TRUE na OK, FALSE na error</returns>
    Function Connect() As Boolean

    ''' <summary>
    ''' inicjalizacja bazy - JSON: empty file, SQL: create TABLE itp.
    ''' </summary>
    ''' <returns>TRUE na OK</returns>
    Function Init() As Boolean

    ''' <summary>
    ''' wczytanie bazy do pamięci - głównie dla JSON
    ''' </summary>
    ''' <returns>TRUE na OK</returns>
    Function Load() As Boolean

    ''' <summary>
    ''' Dodaj metadane zdjęć do archiwum, JSON: AppendFile, i jeśli wczytane to do _files, SQL: add to DB
    ''' </summary>
    Function AddFiles(nowe As IEnumerable(Of OnePic)) As Boolean

    ''' <summary>
    ''' Zrobienie backupu bazy do katalogu CONFIG (żeby się mógł zrobić backup)
    ''' </summary>
    Function PreBackup() As Boolean

    ''' <summary>
    ''' wyszukanie wedle kwerendy (czasem także filtru kanału push/pull - gdy to remote search)
    ''' </summary>
    Function Search(query As SearchQuery, Optional channel As SearchQuery = Nothing) As IEnumerable(Of OnePic)

    ''' <summary>
    '''  zwraca komplet danych - tylko do celów kopiowania między bazami, inaczej nie używać!
    ''' </summary>
    Function GetAll() As IEnumerable(Of OnePic)

    ''' <summary>
    ''' import danych z podanego źródła
    ''' </summary>
    ''' <returns>liczba rekordów wczytanych, &lt;0 oznacza error</returns>
    Function ImportFrom(prevDbase As DatabaseInterface) As Integer

    Function AddExif(picek As OnePic, oExif As ExifTag) As Boolean

    Function AddKeyword(picek As OnePic, oKwd As OneKeyword) As Boolean

    Function AddDescription(picek As OnePic, sDesc As String) As Boolean

End Interface

'aktualny plik archivefull:
' ArchiveQuerender.lib.vb - dodawanie flat/full
'       Public Sub AddToGlobalJsonIndex(sIndexShortJson As String, sIndexLongJson As String)
' SearchWindow.xaml.vb
'       ReadWholeArchive
'       Private Shared _fullArchive As BaseList(Of Vblib.OnePic) ' pełny plik archiwum, do wyszukiwania
'       .count
'       iCount = Szukaj(_fullArchive.GetList, _query)


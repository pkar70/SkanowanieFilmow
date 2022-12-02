
' https://community.cryptomator.org/t/webdav-urls-of-common-cloud-storage-services/75

Public Interface AnyStorage

    ''' <summary>
    '''  wyślij plik, ewentualnie z EXIFami, opisz jeśli trzeba, i w ogóle wszystko razem
    ''' </summary>
    ''' <param name="oPic"></param>
    ''' <returns>"" gdy OK, lub error message</returns>
    Function SendFile(oPic As OnePic) As String

    ''' <summary>
    '''  sprawdź czy istnieje, 
    ''' </summary>
    ''' <param name="oPic"></param>
    ''' <returns>"" gdy OK, lub error message</returns>
    Function VerifyFileExist(oPic As OnePic) As String

    ''' <summary>
    '''  sprawdź czy istnieje, w razie czego wyślij ponownie z Archiwum
    ''' </summary>
    ''' <param name="oPic"></param>
    ''' <returns>"" gdy OK, lub error message</returns>
    Function VerifyFile(oPic As OnePic, oCopyFromArchive As LocalStorage) As String


    ''' <summary>
    '''  wyślij serię plików (jak SendFile), ale tylko z jednym zapisem do JSON 
    '''  wyśle tylko te zdjęcia, które mają targetDir takie jak pierwsze zdjęcie
    ''' </summary>
    ''' <param name="oPic"></param>
    ''' <returns>"" gdy OK, lub error message</returns>
    Function SendFiles(oPicki As List(Of OnePic)) As String


    ''' <summary>
    ''' pobierz plik - korzystając z Dictionary listy publishów, do oContent - Stream
    ''' </summary>
    ''' <param name="oPic"></param>
    ''' <returns>oPic.oContent ustaw na Stream do wczytywania; "" gdy OK, lub error message</returns>
    Function GetFile(oPic As OnePic) As String



    ''' <summary>
    ''' ile wolnego miejsca jest
    ''' </summary>
    ''' <returns></returns>
    Function GetMBfreeSpace() As Integer


End Interface

Public Interface AnyCloudStorage
    ''' <summary>
    ''' login do serwisu
    ''' </summary>
    ''' <returns>"" gdy OK, lub error message</returns>
    Function Login() As String

    ''' <summary>
    ''' ściągnij zewnętrzne opisy (oPic.ExifTag nowy)
    ''' </summary>
    ''' <param name="oPic"></param>
    ''' <returns>nowy oPic.Exifs; "" gdy OK, lub error message</returns>
    Function GetRemoteTags(oPic As OnePic) As String

    ''' <summary>
    ''' usuń plik, aktualizując oPic.Publish
    ''' </summary>
    ''' <param name="oPic"></param>
    ''' <returns>"" gdy OK, lub error message</returns>
    Function Delete(oPic As OnePic) As String

    ''' <summary>
    ''' link do share per plik
    ''' </summary>
    ''' <param name="oPic"></param>
    ''' <returns>link lub ""</returns>
    Function GetShareLink(oPic As OnePic) As String

    ''' <summary>
    ''' link do share per folder (jeśli da się znaleźć)
    ''' </summary>
    ''' <param name="oOneDir"></param>
    ''' <returns>link lub ""</returns>
    Function GetShareLink(oOneDir As OneDir) As String

    ''' <summary>
    ''' odłączenie się od serwisu
    ''' </summary>
    ''' <returns>"" gdy OK, lub error message</returns>
    Function Logout() As String

    ''' <summary>
    ''' zwróć klasę, jeśli oConfig pasuje do konkretnej klasy, lub NULL, jeśli to nie tej klasy
    ''' </summary>
    ''' <param name="oConfig"></param>
    ''' <returns></returns>
    Function CreateNew(oConfig As CloudConfig) As AnyStorage


End Interface

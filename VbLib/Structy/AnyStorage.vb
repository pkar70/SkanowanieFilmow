
' https://community.cryptomator.org/t/webdav-urls-of-common-cloud-storage-services/75

Public Interface AnyStorage

    ''' <summary>
    ''' login do serwisu
    ''' </summary>
    ''' <returns>"" gdy OK, lub error message</returns>
    Function Login() As String

    ''' <summary>
    '''  wyślij plik, ewentualnie z EXIFami, opisz jeśli trzeba, i w ogóle wszystko razem
    ''' </summary>
    ''' <param name="oPic"></param>
    ''' <returns>"" gdy OK, lub error message</returns>
    Function SendFile(oPic As Vblib.OnePic) As String

    ''' <summary>
    ''' pobierz plik - korzystając z Dictionary listy publishów, do oContent - Stream
    ''' </summary>
    ''' <param name="oPic"></param>
    ''' <returns>oPic.oContent ustaw na Stream do wczytywania; "" gdy OK, lub error message</returns>
    Function GetFile(oPic As Vblib.OnePic) As String

    ''' <summary>
    ''' ściągnij zewnętrzne opisy (oPic.ExifTag nowy)
    ''' </summary>
    ''' <param name="oPic"></param>
    ''' <returns>nowy oPic.Exifs; "" gdy OK, lub error message</returns>
    Function GetRemoteTags(oPic As Vblib.OnePic) As String

    ''' <summary>
    ''' usuń plik, aktualizując oPic.Publish
    ''' </summary>
    ''' <param name="oPic"></param>
    ''' <returns>"" gdy OK, lub error message</returns>
    Function Delete(oPic As Vblib.OnePic) As String

    ''' <summary>
    ''' odłączenie się od serwisu
    ''' </summary>
    ''' <returns>"" gdy OK, lub error message</returns>
    Function Logout() As String
End Interface
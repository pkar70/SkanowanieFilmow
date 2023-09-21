

' NOTE: You can use the "Rename" command on the context menu to change the interface name "IPicSortService" in both code and config file together.


Imports CoreWCF

<ServiceContract()>
Public Interface IPicSortService

    ''' <summary>
    ''' Sprawdzenie dostępności - i tylko tyle
    ''' </summary>
    ''' <param name="loginGuid">GUID loginu</param>
    ''' <returns>"OK", "DISABLED", "UNKNOWN", "FAIL"</returns>
    <OperationContract()>
    Function TryLogin(ByVal loginGuid As Guid) As String

    ''' <summary>
    ''' Lista nowych zdjęć
    ''' </summary>
    ''' <param name="loginGuid">GUID loginu</param>
    ''' <param name="sinceId">które zdjęcie było ostatnio widziane</param>
    ''' <returns>JSON danych (jako tekst) / UNKNOWN / TRYAGAIN</returns>
    <OperationContract()>
    Function GetNewPicsList(ByVal loginGuid As Guid, ByVal sinceId As String) As String

    ''' <summary>
    ''' Pobranie jednego zdjęcia
    ''' </summary>
    ''' <param name="loginGuid">GUID loginu</param>
    ''' <param name="picId">identyfikator zdjęcia (PICGUID, nie serno)</param>
    ''' <returns>picture data (file bytes)</returns>
    <OperationContract()>
    Function GetPic(ByVal loginGuid As Guid, ByVal picId As String) As Byte()

    ''' <summary>
    ''' Odsyłamy do serwera informacje o zdjęciu które z tego serwera zostało pobrane
    ''' </summary>
    ''' <param name="loginGuid">GUID loginu</param>
    ''' <param name="picId">identyfikator zdjęcia (PICGUID, nie serno)</param>
    ''' <param name="picData">JSON z nowymi danymi zdjęcia (OneDescription)</param>
    ''' <returns>OK/false</returns>
    <OperationContract()>
    Function UploadPicDescription(ByVal loginGuid As Guid, ByVal picId As String, picData As String) As Boolean

    ''' <summary>
    ''' sprawdzenie czy dopuszczone jest uploadowanie zdjęć
    ''' </summary>
    ''' <param name="loginGuid">GUID loginu</param>
    <OperationContract()>
    Function CanUpload(ByVal loginGuid As Guid) As Boolean

    ''' <summary>
    ''' wysłanie zdjęcia do serwera
    ''' </summary>
    ''' <param name="loginGuid">GUID loginu</param>
    ''' <param name="picMetadata">JSON z OnePic danego zdjęcia</param>
    ''' <param name="picBytes">picture data (file bytes)</param>
    ''' <returns>OK/false</returns>
    <OperationContract()>
    Function PutPic(ByVal loginGuid As Guid, ByVal picMetadata As String, picBytes As Byte()) As Boolean


    '<OperationContract()>
    'Function GetDataUsingDataContract(ByVal composite As CompositeType) As CompositeType

End Interface

' Use a data contract as illustrated in the sample below to add composite types to service operations.
' You can add XSD files into the project. After building the project, you can directly use the data types defined there, with the namespace "Wcf_Server.ContractType".

'<DataContract()>
'Public Class CompositeType

'    <DataMember()>
'    Public Property BoolValue() As Boolean

'    <DataMember()>
'    Public Property StringValue() As String

'End Class

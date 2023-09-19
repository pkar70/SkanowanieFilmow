
' albo w OnePic, albo w onepic.History, albo w onepic.Description - z jakiego kanału zdjęcie pochodzi
' zarówno "gwoli informacji", jak i by było wiadomo jak zwrócić komentarz

Imports System.Net
Imports Newtonsoft.Json
Imports pkar

#Region "struktury"

' pokazywane w Browser.Pic.ContextMenu:Channels i lista channels które obejmują to zdjęcie - wtedy można dać disable, czyli dopisać do wyjątków kanału
Public Class ShareChannel
    Inherits BaseStruct

    Public Property nazwa As String

    ''' <summary>
    ''' lista kwerend używanych jako OR włączające zdjęcia do kanału, każda z processingiem
    ''' </summary>
    ''' 
    Public Property queries As List(Of ShareQueryProcess)

    ''' <summary>
    ''' lista PicGuid wyłączanych (mimo że pasują do powyższych filtrów)
    ''' </summary>
    Public Property exclusions As List(Of String)

    ''' <summary>
    ''' lista kroków do processing, jak zdjęcie miałoby być obrobione - składane z query.processing 
    ''' </summary>
    Public Property processing As String

End Class

Public Class ShareQueryProcess

    Public Property queryName As String
    <JsonIgnore>
    Public Property query As SearchQuery

    Public Property processing As String
End Class

Public Class ShareLogin
    Inherits BaseStruct

    Public Property login As Guid ' tym się INNY loguje
    Public Property displayName As String ' widać go jako...
    Public Property channels As List(Of ShareChannel) ' może widzieć kanały...

    ''' <summary>
    ''' lista PicGuid wyłączanych (mimo że pasują do powyższych filtrów)
    ''' </summary>
    Public Property exclusions As List(Of String)

    Public Property processing As String ' składane do query.processing i channel.processing
    Public Property allowUpload As Boolean ' czy może ten ktoś robić upload
    Public Property remoteHostName As String ' jego hostname, zgłaszany przez jego PicSort
    Public Property ipAddr As IPAddress    ' albo specjalna klasa adres/maska ' https://github.com/jsakamoto/ipaddressrange/
    ' uwaga: z telefonu via komórkowy internet http widzi jako 5.173.42.227, więc jest OK
    Public Property netmask As IPAddress    ' IPNetwork w asp.net   'usercontrol: https://github.com/mariugul/IPUserControls
    Public Property lastLogin As Date ' kiedy ostatnio się logował
End Class

Public Class ShareServers
    Inherits BaseStruct

    Public Property login As Guid ' mogę się zalogować jako...
    Public Property displayName As String ' widzę to u siebie jako...
    Public Property serverAddress As String ' adres PicSort z którym mam się łączyć
    Public Property filters As List(Of SearchQuery) ' mogę filtrować po swojej stronie
    Public Property lastCheck As Date ' kiedy ostatnio sprawdzałem
    Public Property lastId As Integer ' jaki ID ostatnio widziałem (serno)

End Class

#End Region

#Region "listy"

#End Region
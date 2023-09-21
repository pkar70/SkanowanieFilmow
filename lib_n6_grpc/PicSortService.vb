' NOTE: You can use the "Rename" command on the context menu to change the class name "Service1" in both code and config file together.

' na porcie :PS, czyli 50 53 = 20563 - port jest w app.config

Imports CoreWCF

<ServiceBehavior(UseSynchronizationContext:=False)>
Public Class PicSortService
    Implements IPicSortService

    Private Shared _loginy As pkar.BaseList(Of Vblib.ShareLogin)
    Private Shared _databases As Vblib.DatabaseInterface

    ' to byłoby lepsze w New(), ale nie ma wywołania konstruktora :)
    Public Shared Sub SetData(loginy As pkar.BaseList(Of Vblib.ShareLogin), databases As Vblib.DatabaseInterface)
        _loginy = loginy
        _databases = databases
    End Sub

    Private Function ResolveLogin(loginGuid As Guid) As Vblib.ShareLogin
        For Each oLogin As Vblib.ShareLogin In _loginy.GetList
            If oLogin.login = loginGuid Then Return oLogin
        Next

        Return Nothing
    End Function

    Public Function TryLogin(loginGuid As Guid) As String Implements IPicSortService.TryLogin
        Dim oLogin As Vblib.ShareLogin = ResolveLogin(loginGuid)
        If oLogin Is Nothing Then Return "UNKNOWN"

        If Not oLogin.enabled Then Return "DISABLED"

        Return "OK"

    End Function

    Public Function GetNewPicsList(loginGuid As Guid, sinceId As String) As String Implements IPicSortService.GetNewPicsList
        Dim oLogin As Vblib.ShareLogin = ResolveLogin(loginGuid)
        If oLogin Is Nothing Then Return "UNKNOWN"
        If Not oLogin.enabled Then Return "DISABLED"

        If Not _databases.IsLoaded Then
            'Return "TRYAGAIN"
            _databases.Load() ' to mogłoby pójść w oddzielnym thread
        End If

        Dim lista As List(Of Vblib.OnePic) = _databases.Search(oLogin, sinceId)
        If lista Is Nothing Then Return "FAIL"

        Dim ret As String = ""
        For Each oPic As Vblib.OnePic In lista
            If ret <> "" Then ret &= ","
            ret &= oPic.DumpAsJSON(True)
        Next

        Return "[" & ret & "]"

    End Function

    Public Function GetPic(loginGuid As Guid, picId As String) As Byte() Implements IPicSortService.GetPic
        Dim oLogin As Vblib.ShareLogin = ResolveLogin(loginGuid)
        If oLogin Is Nothing Then Return Nothing
        If Not oLogin.enabled Then Return Nothing

        Throw New NotImplementedException()
    End Function

    Public Function UploadPicDescription(loginGuid As Guid, picId As String, picData As String) As Boolean Implements IPicSortService.UploadPicDescription
        Dim oLogin As Vblib.ShareLogin = ResolveLogin(loginGuid)
        If oLogin Is Nothing Then Return "UNKNOWN"
        If Not oLogin.enabled Then Return "DISABLED"

        Throw New NotImplementedException()
    End Function

    Public Function CanUpload(loginGuid As Guid) As Boolean Implements IPicSortService.CanUpload
        Dim oLogin As Vblib.ShareLogin = ResolveLogin(loginGuid)
        If oLogin Is Nothing Then Return False
        If Not oLogin.enabled Then Return False

        Return oLogin.allowUpload

    End Function

    Public Function PutPic(loginGuid As Guid, picMetadata As String, picBytes() As Byte) As Boolean Implements IPicSortService.PutPic
        Dim oLogin As Vblib.ShareLogin = ResolveLogin(loginGuid)
        If oLogin Is Nothing Then Return "UNKNOWN"
        If Not oLogin.enabled Then Return "DISABLED"

        Throw New NotImplementedException()
    End Function

    'Public Function GetData(ByVal value As Integer) As String Implements IPicSortService.GetData
    '    Return String.Format("You entered: {0}", value)
    'End Function

    'Public Function GetDataUsingDataContract(ByVal composite As CompositeType) As CompositeType Implements IPicSortService.GetDataUsingDataContract
    '    If composite Is Nothing Then
    '        Throw New ArgumentNullException("composite")
    '    End If
    '    If composite.BoolValue Then
    '        composite.StringValue &= "Suffix"
    '    End If
    '    Return composite
    'End Function

End Class

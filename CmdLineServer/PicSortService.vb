Imports System.ServiceModel
' NOTE: You can use the "Rename" command on the context menu to change the class name "Service1" in both code and config file together.

' na porcie :PS, czyli 50 53 = 20563 - port jest w app.config

<ServiceBehavior(UseSynchronizationContext:=False)>
Public Class PicSortService
    Implements IPicSortService


    Private Function ResolveLogin(loginGuid As Guid) As String
        Return "ala"
    End Function

    Public Function TryLogin(loginGuid As Guid) As String Implements IPicSortService.TryLogin

        Return "OK"

    End Function

    Public Function GetNewPicsList(loginGuid As Guid, sinceId As String) As String Implements IPicSortService.GetNewPicsList
        Return "UNKNOWN"

    End Function

    Public Function GetPic(loginGuid As Guid, picId As String) As Byte() Implements IPicSortService.GetPic

        Throw New NotImplementedException()
    End Function

    Public Function UploadPicDescription(loginGuid As Guid, picId As String, picData As String) As Boolean Implements IPicSortService.UploadPicDescription

        Throw New NotImplementedException()
    End Function

    Public Function CanUpload(loginGuid As Guid) As Boolean Implements IPicSortService.CanUpload

        Return True

    End Function

    Public Function PutPic(loginGuid As Guid, picMetadata As String, picBytes() As Byte) As Boolean Implements IPicSortService.PutPic

        Throw New NotImplementedException()
    End Function

End Class

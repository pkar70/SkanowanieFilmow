

Public Class OnePic
    Public Property Archived As New List(Of String)
    Public Property Published As New List(Of String)
    Public Property Exifs As New List(Of ExifTag) ' ExifSource.SourceFile ..., )
    Public Property InBufferPathName As String
    ''' <summary>
    ''' z którego źródła pochodzi plik
    ''' </summary>
    ''' <returns></returns>
    Public Property sSourceName As String
    ''' <summary>
    ''' pełny id w źródle - np. full pathname
    ''' </summary>
    ''' <returns></returns>
    Public Property sInSourceID As String    ' usually pathname
    Public Property sSuggestedFilename As String ' z Source: suggested file

    <Newtonsoft.Json.JsonIgnore>
    Public Property Content As IO.Stream

    Public Function GetExifOfType(sType As String) As ExifTag
        If Exifs Is Nothing Then Return Nothing

        For Each oExif As ExifTag In Exifs
            If oExif.ExifSource.ToLower = sType.ToLower Then Return oExif
        Next

        Return Nothing
    End Function

    Public Sub New(sourceName As String, inSourceId As String, suggestedFilename As String)
        sSourceName = sourceName
        sInSourceID = inSourceId
        sSuggestedFilename = suggestedFilename
    End Sub
End Class

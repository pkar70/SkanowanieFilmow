

Imports Newtonsoft.Json

Public Class OnePic
    Inherits MojaStruct

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

    Public Property descriptions As List(Of OneDescription)
    Public Property TagsChanged As Boolean = False

    <Newtonsoft.Json.JsonIgnore>
    Public Property Content As IO.Stream

    Public Function GetExifOfType(sType As String) As ExifTag
        If Exifs Is Nothing Then Return Nothing

        For Each oExif As ExifTag In Exifs
            If oExif.ExifSource.ToLower = sType.ToLower Then Return oExif
        Next

        Return Nothing
    End Function

    Public Function GetGeoTag() As MyBasicGeoposition
        ' *TEMP* pierwsze ktore znajdzie, potem trzeba jakos inaczej?
        For Each oExif As ExifTag In Exifs
            If oExif.GeoTag IsNot Nothing Then Return oExif.GeoTag
        Next

        Return Nothing
    End Function
    Public Sub New(sourceName As String, inSourceId As String, suggestedFilename As String)
        sSourceName = sourceName
        sInSourceID = inSourceId
        sSuggestedFilename = suggestedFilename
    End Sub

    ''' <summary>
    ''' dodaje jeden description, gdy nie ma daty to go datuje na Now
    ''' </summary>
    ''' <param name="opis"></param>
    Public Sub AddDescription(opis As OneDescription)
        If descriptions Is Nothing Then descriptions = New List(Of OneDescription)
        ' If String.IsNullOrEmpty(opis.data) Then opis.data = Date.Now.ToString("yyyy.MM.dd HH:mm")
        descriptions.Add(opis)

        TagsChanged = True
    End Sub

End Class


Public Class OneDescription
    Public Property data As String
    Public Property comment As String
    Public Property keywords As String

    Public Sub New(sData As String, sComment As String, sKeywords As String)
        data = sData
        comment = sComment
        keywords = sKeywords
    End Sub

    <JsonConstructor>
    Public Sub New(sComment As String, sKeywords As String)
        data = Date.Now.ToString("yyyy.MM.dd HH:mm")
        comment = sComment
        keywords = sKeywords
    End Sub
End Class
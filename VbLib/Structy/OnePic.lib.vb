

Imports Newtonsoft.Json

Public Class OnePic
    Inherits MojaStruct

    Public Property Archived As List(Of String)
    Public Property Published As List(Of String)
    Public Property TargetDir As String
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
    Public Property sSuggestedFilename As String ' miało być że np. scinanie WP_. ale jednak tego nie robię (bo moge posortowac po dacie, albo po nazwach - i w tym drugim przypadku mam rozdział na np. telefon i aparat)

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

    ''' <summary>
    ''' podmień (lub dodaj nowy) ExifTag, wedle ExifTag.Source
    ''' </summary>
    ''' <param name="oExifTag"></param>
    Public Sub ReplaceOrAddExif(oExifTag As ExifTag)
        Dim oOldExif As ExifTag = GetExifOfType(oExifTag.ExifSource)
        If oOldExif IsNot Nothing Then Exifs.Remove(oOldExif)

        Exifs.Add(oExifTag)
    End Sub

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

    ''' <summary>
    ''' usuwa wszystkie Description, które mają tagi podane na wejściu
    ''' </summary>
    ''' <param name="sTagList"></param>
    Public Sub RemoveFromDescriptions(sTagList As String, oKeywords As KeywordsList)
        If descriptions Is Nothing Then Return

        Dim aTags As String() = sTagList.Split(" ")

        Dim lToRemove As New List(Of OneDescription)

        For Each oDescr As OneDescription In descriptions

            For Each sTag As String In aTags

                If oDescr.keywords.Contains(sTag & " ") Then
                    Dim oKwd As OneKeyword = oKeywords.GetKeyword(sTag)
                    If oKwd IsNot Nothing Then
                        oDescr.keywords = oDescr.keywords.Replace(oKwd.sTagId, "")
                        oDescr.comment = oDescr.comment.Replace(oKwd.sDisplayName, "")
                    End If
                End If

            Next

            oDescr.keywords = oDescr.keywords.Replace("  ", " ")
            oDescr.comment = oDescr.comment.Replace("  ", " ")

            If String.IsNullOrWhiteSpace(oDescr.keywords) AndAlso
                    String.IsNullOrWhiteSpace(oDescr.comment) Then
                lToRemove.Add(oDescr)
            End If
        Next

        For Each oItem As OneDescription In lToRemove
            descriptions.Remove(oItem)
        Next

    End Sub

    ''' <summary>
    ''' przygotuj plik źródłowy do edycji, robiąc bak/tmp
    ''' </summary>
    ''' <returns>filepathname pliku źródłowego (do otwierania jako readonly)</returns>
    Public Function InitEdit() As String
        Dim bakFileName As String = InBufferPathName & ".bak"

        If Not IO.File.Exists(bakFileName) Then
            IO.File.Move(InBufferPathName, bakFileName)
            IO.File.SetCreationTime(bakFileName, Date.Now)
            Return bakFileName
        End If

        bakFileName = InBufferPathName & ".tmp"

        If IO.File.Exists(bakFileName) Then IO.File.Delete(bakFileName)

        IO.File.Move(InBufferPathName, bakFileName)
        IO.File.SetCreationTime(bakFileName, Date.Now)

        Return bakFileName

    End Function

    ''' <summary>
    ''' skasuj ewentualny plik tmp (gdy było kilka edycji)
    ''' </summary>
    Public Sub EndEdit()
        Dim bakFileName As String = InBufferPathName & ".tmp"
        If Not IO.File.Exists(bakFileName) Then Return

        IO.File.Delete(bakFileName)
    End Sub

    ''' <summary>
    ''' sprowadza wszystkie EXIFy do jednego - ale dalej jeszcze z dodatkowymi polami!
    ''' </summary>
    ''' <returns></returns>
    Public Function FlattenExifs() As ExifTag


        ' defaulty mamy z FileSourceDeviceType
        Dim oNew As New ExifTag(ExifSource.Flattened)
        Dim oExif_SourceDefault As ExifTag = GetExifOfType(ExifSource.SourceDefault)
        If oExif_SourceDefault IsNot Nothing Then
            oNew = oExif_SourceDefault.Clone
            oNew.ExifSource = ExifSource.Flattened
        Else
            oNew = New ExifTag(ExifSource.Flattened)
        End If

        ' dla nich bierzemy ostatni wpis - pewnie zwykle będzie to ustawiane gdy w loop będzie miał SourceDefault - ale może to nie będzie pierwszy...
        For Each oExif As ExifTag In Exifs
            If oExif.FileSourceDeviceType <> 0 Then oNew.FileSourceDeviceType = oExif.FileSourceDeviceType
            If Not String.IsNullOrWhiteSpace(oExif.Author) Then oNew.Author = oExif.Author
            If Not String.IsNullOrWhiteSpace(oExif.Copyright) Then oNew.Copyright = oExif.Copyright
            If Not String.IsNullOrWhiteSpace(oExif.CameraModel) Then oNew.CameraModel = oExif.CameraModel
            If Not String.IsNullOrWhiteSpace(oExif.DateTimeOriginal) Then oNew.DateTimeOriginal = oExif.DateTimeOriginal
            If Not String.IsNullOrWhiteSpace(oExif.DateTimeScanned) Then oNew.DateTimeScanned = oExif.DateTimeScanned

            If Not String.IsNullOrWhiteSpace(oExif.Restrictions) Then oNew.Restrictions = oExif.Restrictions
            If Not String.IsNullOrWhiteSpace(oExif.PicGuid) Then oNew.PicGuid = oExif.PicGuid

            If Not String.IsNullOrWhiteSpace(oExif.ReelName) Then oNew.ReelName = oExif.ReelName
            If Not String.IsNullOrWhiteSpace(oExif.OriginalRAW) Then oNew.OriginalRAW = oExif.OriginalRAW
            If Not String.IsNullOrWhiteSpace(oExif.PicGuid) Then oNew.PicGuid = oExif.PicGuid
        Next

        ' sklejamy - concatenate, więc musimy najpierw wyciąć to co było z SourceDefault
        oNew.Keywords = ""
        oNew.UserComment = ""
        oNew.GeoName = ""
        For Each oExif As ExifTag In Exifs
            oNew.Keywords = oNew.Keywords.ConcatenateWithComma(oExif.Keywords)
            oNew.UserComment = oNew.UserComment.ConcatenateWithComma(oExif.UserComment)
            oNew.GeoName = oNew.GeoName.ConcatenateWithPipe(oExif.GeoName)
        Next

        ' te przeliczamy (min/max)
        Dim dMax As Date = Date.MaxValue
        Dim dMin As Date = Date.MinValue

        For Each oExif As ExifTag In Exifs
            dMax = dMax.DateMin(oExif.DateMax)
            dMin = dMin.DateMax(oExif.DateMin)
        Next

        oNew.DateMin = dMin
        oNew.DateMax = dMax

        ' dwa przypadki szczególne

        ' geotag ma MANUAL jako override, i wtedy nie sprawdzamy innych, inaczej: ostatni znaleziony
        Dim oExif1 As ExifTag = GetExifOfType(ExifSource.ManualGeo)
        If oExif1 IsNot Nothing Then
            oNew.GeoTag = oExif1.GeoTag
        Else
            For Each oExif As ExifTag In Exifs
                If oExif.GeoTag IsNot Nothing Then
                    If Not oExif.GeoTag.IsEmpty Then oNew.GeoTag = oExif.GeoTag
                End If
            Next
        End If

        ' description z PIC do Exif.UserComment
        If descriptions IsNot Nothing Then
            For Each oDesc As OneDescription In descriptions
                oNew.Keywords = oNew.Keywords.ConcatenateWithPipe(oDesc.keywords)
                oNew.UserComment = oNew.UserComment.ConcatenateWithPipe(oDesc.comment)
            Next
        End If

        ' azure analysis - doklejamy jako usercomment
        For Each oExif As ExifTag In Exifs
            If oExif.AzureAnalysis IsNot Nothing Then
                oNew.UserComment = oNew.UserComment.ConcatenateWithPipe(oExif.AzureAnalysis.DumpAsText)
            End If
        Next

        Return oNew
    End Function

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
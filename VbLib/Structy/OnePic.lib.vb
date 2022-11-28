

Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Threading
Imports Newtonsoft.Json

Public Class OnePic
    Inherits MojaStruct

    Public Property Archived As String
    Public Property CloudArchived As String
    Public Property Published As Dictionary(Of String, String)
    Public Property TargetDir As String ' OneDir.sId
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

    'Public Property sortOrder As String

    <Newtonsoft.Json.JsonIgnore>
    Public Property oContent As IO.Stream

    Public Sub New(sourceName As String, inSourceId As String, suggestedFilename As String)
        DumpCurrMethod()
        sSourceName = sourceName
        sInSourceID = inSourceId
        sSuggestedFilename = suggestedFilename
    End Sub

#Region "operacje na ExifTags"

    Public Function GetExifOfType(sType As String) As ExifTag
        If Exifs Is Nothing Then Return Nothing

        For Each oExif As ExifTag In Exifs
            If oExif.ExifSource.ToLower = sType.ToLower Then Return oExif
        Next

        Return Nothing
    End Function

    ''' <summary>
    ''' usuwa wszystkie zestawy o podanym typie (powinien być tylko jeden, ale...)
    ''' </summary>
    ''' <param name="sType"></param>
    Public Sub RemoveExifOfType(sType As String)
        If Exifs Is Nothing Then Return

        Dim lLista As New List(Of ExifTag)

        For Each oExif As ExifTag In Exifs
            If oExif.ExifSource.ToLower = sType.ToLower Then lLista.Add(oExif)
        Next

        For Each oExif As ExifTag In lLista
            Exifs.Remove(oExif)
        Next

    End Sub


    Public Function GetGeoTag() As MyBasicGeoposition
        ' ważniejsze jest z keywords, potem manual_geo, potem - dowolny

        Dim oExif As ExifTag = GetExifOfType(ExifSource.ManualTag)
        If oExif IsNot Nothing Then Return oExif.GeoTag

        oExif = GetExifOfType(ExifSource.ManualGeo)
        If oExif IsNot Nothing Then Return oExif.GeoTag

        For Each oExif In Exifs
            If oExif.GeoTag IsNot Nothing Then
                If Not oExif.GeoTag.IsEmpty Then Return oExif.GeoTag
            End If
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

    ''' <summary>
    ''' Zwraca datę do sortowania tekstowego w formacie EXIF: 2022.05.06 12:27:47.
    ''' Ważność: FileExif, ManualTag (min ze wszystkich), SourceFile
    ''' </summary>
    ''' <returns></returns>
    Public Function GetMostProbablyDate() As String
        Dim oExif As ExifTag

        oExif = GetExifOfType(ExifSource.FileExif)
        If oExif IsNot Nothing Then
            If Not String.IsNullOrWhiteSpace(oExif.DateTimeOriginal) Then Return oExif.DateTimeOriginal
        End If

        Dim dDateMin As Date = Date.MaxValue
        Dim dDateMax As Date = Date.MinValue

        For Each oItem As ExifTag In Exifs
            If oItem.ExifSource = ExifSource.ManualTag Then
                dDateMin = dDateMin.DateMax(oItem.DateMin)
                dDateMax = dDateMax.DateMin(oItem.DateMax)
            End If
        Next

        If dDateMin.IsDateValid AndAlso Not dDateMax.IsDateValid Then Return dDateMin.ToExifString
        If Not dDateMin.IsDateValid AndAlso dDateMax.IsDateValid Then Return dDateMax.ToExifString
        If dDateMin.IsDateValid AndAlso dDateMax.IsDateValid Then
            Dim oDateDiff As TimeSpan = dDateMax - dDateMin
            Return dDateMin.AddMinutes(oDateDiff.TotalMinutes / 2).ToExifString
        End If

        oExif = GetExifOfType(Vblib.ExifSource.SourceFile)
        If oExif IsNot Nothing Then
            ' to właściwie na pewno jest, bo to data pliku
            Return oExif.DateMin.ToExifString
        End If

        ' jakby co jendak nie było
        Return Date.Now

    End Function

#End Region

#Region "operacje na maskach"
    Public Function MatchesMasks(sIncludeMasks As String, sExcludeMasks As String) As Boolean

        Dim sFilenameNoPath As String = IO.Path.GetFileName(InBufferPathName)   ' dla edycji było GetSourceFilename, ale to poprzednia wersja
        Return MatchesMasks(sFilenameNoPath, sIncludeMasks, sExcludeMasks)
    End Function

    Private Shared Function DOSmask2regExp(dosMask As String) As String
        Dim sRet As String = dosMask.Replace(".", "[.]").Replace("*", ".*").Replace("?", ".")
        sRet = "^" & sRet & "$" ' w ten sposób nie będzie ściągało WP_20221119_10_41_12_Rich.jpg.thumb - ale co to za pliki?
        Return sRet
    End Function


    Public Shared Function MatchesMasks(sFilenameNoPath As String, sIncludeMasks As String, sExcludeMasks As String) As Boolean
        DumpCurrMethod($"({sFilenameNoPath}, {sIncludeMasks}, {sExcludeMasks}")

        ' https://stackoverflow.com/questions/725341/how-to-determine-if-a-file-matches-a-file-mask
        Dim aMaski As String()

        If Not String.IsNullOrWhiteSpace(sExcludeMasks) Then
            aMaski = sExcludeMasks.Split(";")
            For Each maska As String In aMaski
                Dim regExMaska As New Regex(DOSmask2regExp(maska))
                If regExMaska.IsMatch(sFilenameNoPath) Then Return False
            Next
        End If

        If String.IsNullOrWhiteSpace(sIncludeMasks) Then
            aMaski = "*.jpg;*.tif;*.png".Split(";")
        Else
            aMaski = sIncludeMasks.Split(";")
        End If

        Dim bMatch As Boolean = False
        For Each maska As String In aMaski
            Dim regExMaska As New Regex(DOSmask2regExp(maska))
            If regExMaska.IsMatch(sFilenameNoPath) Then
                bMatch = True
                Exit For
            End If
        Next

        Return bMatch
    End Function

#End Region

#Region "descriptionsy"

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
#End Region


#Region "start/end edycji"

    <Newtonsoft.Json.JsonIgnore>
    Private Property _EditPipeline As Boolean = True


#If OLD_EDIT_MODE Then
    <Newtonsoft.Json.JsonIgnore>
    Public Property sFilenameEditSrc As String
    <Newtonsoft.Json.JsonIgnore>
    Public Property sFilenameEditDst As String


    ''' <summary>
    ''' do używania PRZED InitEdit, na potrzeby masek
    ''' </summary>
    ''' <returns></returns>
    Public Function GetSourceFilename()
        If String.IsNullOrWhiteSpace(sFilenameEditSrc) Then Return InBufferPathName
        Return sFilenameEditSrc
    End Function

    Public Sub CancelEdit()

        If _EditPipeline Then

            If IO.File.Exists(sFilenameEditDst) Then IO.File.Delete(sFilenameEditDst)
            sFilenameEditDst = ""

        Else

            If IO.File.Exists(InBufferPathName) Then Return

            If IO.File.Exists(InBufferPathName & ".tmp") Then
                IO.File.Move(InBufferPathName & ".tmp", InBufferPathName)
                Return
            End If

            If IO.File.Exists(InBufferPathName & ".bak") Then
                IO.File.Move(InBufferPathName & ".bak", InBufferPathName)
                Return
            End If

            ' to jest dziwna sytuacja, której nie powinno być - nie ma się z czego wycofać
            Throw New Exception("CancelEdit, ale nie ma ani pliku, ani .tmp, ani .bak")

        End If
    End Sub



    ''' <summary>
    ''' przygotuj plik źródłowy do edycji, robiąc bak/tmp
    ''' </summary>
    Public Sub InitEdit(bPipeline As Boolean)
        _EditPipeline = bPipeline

        If _EditPipeline Then
            InitEditPipeline()
        Else
            InitEditInPlace()
        End If

    End Sub

    Private Sub InitEditPipeline()

        If String.IsNullOrWhiteSpace(sFilenameEditSrc) Then
            sFilenameEditSrc = InBufferPathName
        End If

        sFilenameEditDst = IO.Path.GetTempFileName

    End Sub


    Private Sub InitEditInPlace()

        Dim bakFileName As String = InBufferPathName & ".bak"

        If Not IO.File.Exists(bakFileName) Then
            IO.File.Move(InBufferPathName, bakFileName)
            IO.File.SetCreationTime(bakFileName, Date.Now)
        Else
            bakFileName = InBufferPathName & ".tmp"

            If IO.File.Exists(bakFileName) Then IO.File.Delete(bakFileName)

            IO.File.Move(InBufferPathName, bakFileName)
            IO.File.SetCreationTime(bakFileName, Date.Now)
        End If

        sFilenameEditSrc = bakFileName
        sFilenameEditDst = InBufferPathName

    End Sub

    ''' <summary>
    ''' skasuj ewentualny plik tmp (gdy było kilka edycji) / zmień dst -> src w pipeline
    ''' </summary>
    Public Sub EndEdit()

        If _EditPipeline Then
            If InBufferPathName <> sFilenameEditSrc Then
                IO.File.Delete(sFilenameEditSrc)
            End If

            sFilenameEditSrc = sFilenameEditDst
            sFilenameEditDst = ""

        Else
            Dim bakFileName As String = InBufferPathName & ".tmp"
            If Not IO.File.Exists(bakFileName) Then Return
            IO.File.Delete(bakFileName)

        End If

#Else

    <Newtonsoft.Json.JsonIgnore>
    Public Property _PipelineInput As Stream
    <Newtonsoft.Json.JsonIgnore>
    Public Property _PipelineOutput As Stream

    Public Sub CancelEdit()
        ' empty, bo po nowemu nic nie trzeba robić
    End Sub


    ''' <summary>
    ''' przygotuj Stream do edycji
    ''' </summary>
    Public Sub InitEdit(bPipeline As Boolean)
        _EditPipeline = bPipeline

        If _EditPipeline Then
            If _PipelineOutput.Length > 0 Then
                _PipelineInput.Dispose()
                _PipelineInput = _PipelineOutput
                _PipelineOutput = New MemoryStream
            End If
        Else
            _PipelineInput = IO.File.OpenRead(InBufferPathName)
            _PipelineOutput = New MemoryStream
        End If

        _PipelineInput.Seek(0, SeekOrigin.Begin)
        _PipelineOutput.Seek(0, SeekOrigin.Begin)

    End Sub


    ''' <summary>
    ''' zakończ edycje, ewentualnie zapisując plik (jak nie Pipeline)
    ''' </summary>
    Public Sub EndEdit(bCopyExif As Boolean, bResetOrientation As Boolean)

        If bCopyExif Then
            ' próba przeniesienia kopiowania EXIF tutaj
            Dim oExifLib As New CompactExifLib.ExifData(_PipelineInput)
            If bResetOrientation Then
                ' zarówno w pliku ma być bez obracania, jak i w naszych danych
                oExifLib.SetTagValue(CompactExifLib.ExifTag.Orientation, 1, CompactExifLib.ExifTagType.UShort)
                Dim oExif As ExifTag = GetExifOfType(ExifSource.FileExif)
                If oExif IsNot Nothing Then oExif.Orientation = 1
            End If
            _PipelineOutput.Seek(0, SeekOrigin.Begin)
            Dim tempStream As New MemoryStream
            oExifLib.Save(_PipelineOutput, tempStream, 0) ' (orgFileName)
            _PipelineOutput.Dispose()
            _PipelineOutput = tempStream
        End If

        ' do sprawdzenia - czy zachowywane są EXIFy w pliku/strumieniu
        If _EditPipeline Then
            ' do nothing: w streamOut jest rezultat
        Else
            ' należy zapisać plik, czyli najpierw zwalniamy stream.source
            _PipelineInput.Dispose()

            ' najpierw robimy backup
            Dim bakFileName As String = InBufferPathName & ".bak"

            If Not IO.File.Exists(bakFileName) Then
                IO.File.Move(InBufferPathName, bakFileName)
                IO.File.SetCreationTime(bakFileName, Date.Now)
            End If

            ' bo Write zostawiłby śmieci na końcu (resztę dłuższego pliku)
            If IO.File.Exists(InBufferPathName) Then IO.File.Delete(InBufferPathName)

            Dim oNewFileStream As FileStream = IO.File.OpenWrite(InBufferPathName)
            _PipelineOutput.Seek(0, SeekOrigin.Begin)
            _PipelineOutput.CopyTo(oNewFileStream)
            oNewFileStream.Flush()
            oNewFileStream.Dispose()

        End If



    End Sub




#End If



#End Region

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

        oNew.GeoTag = GetGeoTag()   ' z priorytetem dla MANUAL

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


Imports System.Globalization
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Threading
Imports CompactExifLib
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports XmpCore.Impl

Public Class OnePic
    Inherits pkar.BaseStruct

    Public Property Archived As String
    Public Property CloudArchived As String
    Public Property Published As Dictionary(Of String, String)
    Public Property TargetDir As String ' OneDirFlat.sId
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
    Public Property editHistory As List(Of OneDescription)
    Public Property TagsChanged As Boolean = False

    Public Property fileTypeDiscriminator As String = Nothing   ' tu "|>", "*", które mają być dodawane do miniaturek

    Public Property PicGuid As String = Nothing  ' 0xA420 ImageUniqueID ASCII!

    'Public Property sortOrder As String

    <Newtonsoft.Json.JsonIgnore>
    Public Property oContent As IO.Stream
    <Newtonsoft.Json.JsonIgnore>
    Public Property oOstatniExif As ExifTag

    Public Sub New(sourceName As String, inSourceId As String, suggestedFilename As String)
        DumpCurrMethod()
        sSourceName = sourceName
        sInSourceID = inSourceId
        sSuggestedFilename = suggestedFilename
    End Sub

#Region "operacje na Archived"
    Public Sub AddArchive(sArchName As String)
        If IsArchivedIn(sArchName) Then Return
        If Archived Is Nothing Then Archived = ""
        Archived &= sArchName & ";"
    End Sub

    Public Function IsArchivedIn(sArchName As String) As Boolean
        If Archived Is Nothing Then Return False
        If Archived.Contains(sArchName & ";") Then Return True
        Return False
    End Function

    Public Function ArchivedCount() As Integer
        If Archived Is Nothing Then Return 0
        Dim aArr As String() = Archived.Split(";")
        Return aArr.Count
    End Function

#End Region

#Region "operacje na CloudArchive"
    Public Sub AddCloudArchive(sArchName As String)
        If IsCloudArchivedIn(sArchName) Then Return
        If CloudArchived Is Nothing Then CloudArchived = ""
        CloudArchived &= sArchName & ";"
    End Sub

    Public Function IsCloudArchivedIn(sArchName As String) As Boolean
        If CloudArchived Is Nothing Then Return False
        If CloudArchived.Contains(sArchName & ";") Then Return True
        Return False
    End Function

    Public Function CloudArchivedCount() As Integer
        If CloudArchived Is Nothing Then Return 0
        Dim aArr As String() = CloudArchived.Split(";")
        Return aArr.Count
    End Function
#End Region

#Region "operacje na Cloud Publish"
    Public Sub AddCloudPublished(sPublName As String, sRemoteId As String)
        If IsCloudPublishedIn(sPublName) Then Return
        If Published Is Nothing Then Published = New Dictionary(Of String, String)
        Published.Add(sPublName, sRemoteId)
    End Sub

    Public Sub RemoveCloudPublished(sPublName As String)
        If Published Is Nothing Then Return
        Published.Remove(sPublName)
    End Sub

    Public Function GetCloudPublishedId(sPublName As String) As String
        If Not IsCloudPublishedIn(sPublName) Then Return ""
        Dim sRet As String = ""
        If Not Published.TryGetValue(sPublName, sRet) Then Return ""
        Return sRet
    End Function

    Public Function IsCloudPublishMentioned(sPublName As String) As Boolean
        If Published Is Nothing Then Return False
        If Not Published.ContainsKey(sPublName) Then Return False
        Dim sDir As String = ""
        Return Published.TryGetValue(sPublName, sDir)
    End Function

    Public Function IsCloudPublishedIn(sPublName As String) As Boolean
        If Not IsCloudPublishMentioned(sPublName) Then Return False
        Dim sDir As String = ""
        If Not Published.TryGetValue(sPublName, sDir) Then Return False

        If String.IsNullOrEmpty(sDir) Then Return False

        Return True
    End Function

    Public Function IsCloudPublishScheduledIn(sPublName As String) As Boolean
        If Not IsCloudPublishMentioned(sPublName) Then Return False
        Dim sDir As String = ""
        If Not Published.TryGetValue(sPublName, sDir) Then Return False

        If String.IsNullOrEmpty(sDir) Then Return True

        Return False
    End Function

    Public Function CountPublishingWaiting() As Integer
        If Published Is Nothing Then Return 0
        Dim iCnt As Integer = 0
        For Each oPubl In Published
            ' jeśli value jest nonempty, to znaczy że mamy identyfikator wpisany - czyli wysłany
            If String.IsNullOrWhiteSpace(oPubl.Value) Then iCnt += 1
        Next

        Return iCnt
    End Function

#End Region

    Public Function NoPendingAction(iArchCount As Integer, iCloudArchCount As Integer) As Boolean
        If ArchivedCount() < iArchCount Then Return False
        If CloudArchivedCount() < iCloudArchCount Then Return False
        If CountPublishingWaiting() > 0 Then Return False

        Return True
    End Function

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

    ''' <summary>
    ''' ważniejsze jest z keywords, potem manual_geo, potem - dowolny; NULL jeśli nie znajdzie
    ''' </summary>
    ''' <returns></returns>
    Public Function GetGeoTag() As pkar.BasicGeopos
        DumpCurrMethod($"({InBufferPathName})")
        ' ważniejsze jest z keywords, potem manual_geo, potem - dowolny

        Dim oExif As ExifTag = GetExifOfType(ExifSource.ManualTag)
        If oExif?.GeoTag IsNot Nothing Then
            DumpMessage("not null ExifSource.ManualTag.GeoTag")
            If Not oExif.GeoTag.IsEmpty Then Return oExif.GeoTag
            DumpMessage($"... but IsEmpty, {oExif.GeoTag.Latitude}, {oExif.GeoTag.Longitude}")
        End If

        oExif = GetExifOfType(ExifSource.ManualGeo)
        If oExif?.GeoTag IsNot Nothing Then
            DumpMessage("not null ExifSource.ManualGeo.GeoTag")
            If Not oExif.GeoTag.IsEmpty Then Return oExif.GeoTag
            DumpMessage($"... but IsEmpty, {oExif.GeoTag.Latitude}, {oExif.GeoTag.Longitude}")
        End If

        For Each oExif In Exifs
            If oExif?.GeoTag IsNot Nothing Then
                DumpMessage($"not null GeoTag in {oExif.ExifSource}")
                If Not oExif.GeoTag.IsEmpty Then Return oExif.GeoTag
                DumpMessage($"... but IsEmpty, {oExif.GeoTag.Latitude}, {oExif.GeoTag.Longitude}")
            End If
        Next

        DumpMessage("cannot find geotag")

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
    ''' zwraca minimalną datę zdjęcia, bądź Invalid
    ''' </summary>
    ''' <returns></returns>
    Public Function GetMinDate() As Date
        Dim dDateMin As Date = Date.MinValue

        For Each oItem As ExifTag In Exifs
            'If oItem.ExifSource = ExifSource.ManualTag Then
            dDateMin = dDateMin.DateMax(oItem.DateMin)
            'End If
        Next

        Return dDateMin

    End Function

    Public Function GetMaxDate() As Date
        Dim dDateMax As Date = Date.MaxValue

        For Each oItem As ExifTag In Exifs
            ' If oItem.ExifSource = ExifSource.ManualTag Then
            dDateMax = dDateMax.DateMin(oItem.DateMax)
            'End If
        Next

        Return dDateMax
    End Function

    Public Shared Function ExifDateToDate(sExifDate) As Date
        If String.IsNullOrWhiteSpace(sExifDate) Then Return Date.MaxValue

        Dim retDate As Date
        If Not Date.TryParseExact(sExifDate, "yyyy.MM.dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, retDate) Then Return Date.MaxValue

        Return retDate
    End Function

    Private Function GetRealDate() As Date
        Dim oExif As ExifTag = GetExifOfType(ExifSource.FileExif)
        If oExif Is Nothing Then Return Date.MaxValue

        ' zeskanowanie daty (string->date)
        Dim retDate As Date = ExifDateToDate(oExif.DateTimeOriginal)
        Return retDate
    End Function

    ''' <summary>
    ''' Ważność: FileExif, SourceFile, ManualTag (min ze wszystkich)
    ''' </summary>
    ''' <returns></returns>
    Public Function GetMostProbablyDate(Optional bSkipTags As Boolean = False) As Date

        Dim retDate As Date = GetRealDate()
        If retDate.IsDateValid Then Return retDate

        Dim oExif As ExifTag = GetExifOfType(ExifSource.SourceDefault)
        If oExif IsNot Nothing Then
            If oExif.FileSourceDeviceType = FileSourceDeviceTypeEnum.digital Then
                ' gdy to aparat cyfrowy, przyjmujemy mniejszą datę pliku
                oExif = GetExifOfType(ExifSource.SourceFile)
                If oExif IsNot Nothing Then Return oExif.DateMin
            End If
        End If

        If bSkipTags Then Return Date.MaxValue

        ' nie mamy EXIF w pliku, to spróbujemy wyliczyć
        Dim dDateMin As Date = GetMinDate()
        Dim dDateMax As Date = GetMaxDate()

        If dDateMin.IsDateValid AndAlso Not dDateMax.IsDateValid Then Return dDateMin
        If Not dDateMin.IsDateValid AndAlso dDateMax.IsDateValid Then Return dDateMax
        If dDateMin.IsDateValid AndAlso dDateMax.IsDateValid Then
            Dim oDateDiff As TimeSpan = dDateMax - dDateMin
            Return dDateMin.AddMinutes(oDateDiff.TotalMinutes / 2)
        End If

        ' czyli tu właściwie juz nigdy nie wejdzie, bo ten EXIF już jest uwzględniony w pętli powyżej
        oExif = GetExifOfType(ExifSource.SourceFile)
        If oExif IsNot Nothing Then
            ' to właściwie na pewno jest, bo to data pliku
            Return oExif.DateMin
        End If

        ' jakby co (jednak nie było), to coś  zwracamy
        Return Date.Now

    End Function

    Public Function IsAdultInExifs() As Boolean
        Dim oExif As Vblib.ExifTag = GetExifOfType(Vblib.ExifSource.AutoAzure)
        If String.IsNullOrWhiteSpace(oExif?.AzureAnalysis?.Wiekowe) Then Return False
        Return True
    End Function


#End Region
#Region "operacje na maskach"
    Public Function MatchesMasks(sIncludeMasks As String, Optional sExcludeMasks As String = "") As Boolean

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
        Dim sFilename As String = sFilenameNoPath.ToLowerInvariant

        If Not String.IsNullOrWhiteSpace(sExcludeMasks) Then
            aMaski = sExcludeMasks.ToLowerInvariant.Split(";")
            For Each maska As String In aMaski
                Dim regExMaska As New Regex(DOSmask2regExp(maska))
                If regExMaska.IsMatch(sFilename) Then Return False
            Next
        End If

        If String.IsNullOrWhiteSpace(sIncludeMasks) Then
            aMaski = "*.jpg;*.tif;*.png".Split(";")
        Else
            aMaski = sIncludeMasks.ToLowerInvariant.Split(";")
        End If

        Dim bMatch As Boolean = False
        For Each maska As String In aMaski
            Dim regExMaska As New Regex(DOSmask2regExp(maska))
            If regExMaska.IsMatch(sFilename) Then
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

    Public Function AreTagsInDescription() As Boolean
        If descriptions Is Nothing Then Return False
        For Each oDesc As OneDescription In descriptions
            If Not String.IsNullOrWhiteSpace(oDesc.keywords) Then Return True
        Next
        Return False
    End Function

    Public Sub ReplaceAllDescriptions(sDesc As String)
        descriptions = New List(Of OneDescription)
        AddDescription(New OneDescription(sDesc, ""))
    End Sub

    Public Function GetSumOfDescriptionsText() As String
        If descriptions Is Nothing Then Return ""

        Dim sRet As String = ""
        For Each oDesc As OneDescription In descriptions
            ' pomijamy systemowe opisy
            If oDesc.comment.StartsWith("Cropped to ") Then Continue For
            If oDesc.comment.StartsWith("Rotated ") Then Continue For
            sRet &= oDesc.comment & " "
        Next

        Return sRet
    End Function

    Public Function GetSumOfCommentText() As String
        Dim sRet As String = ""

        For Each oExif As ExifTag In Exifs
            If oExif.ExifSource = ExifSource.AutoAzure Then Continue For
            If oExif.ExifSource = ExifSource.Flattened Then Continue For
            sRet = ConcatenateWithComma(sRet, oExif.UserComment)
        Next

        Return sRet

    End Function

    ''' <summary>
    ''' gdy jest już z tą datą i keywords, nie dodaje (do zastosowania w comments z Cloud)
    ''' </summary>
    ''' <param name="opis"></param>
    Public Sub TryAddDescription(opis As OneDescription)
        If descriptions IsNot Nothing Then
            For Each oDesc As OneDescription In descriptions
                If oDesc.data = opis.data AndAlso oDesc.keywords = opis.keywords Then Return
            Next
        End If
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
                        oDescr.keywords = oDescr.keywords.Replace(oKwd.sId, "")
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

#Region "edit history"
    ''' <summary>
    ''' dodaje jedno entry w historii, datując na Now
    ''' </summary>
    ''' <param name="sHistory"></param>
    Public Sub AddEditHistory(sHistory As String)
        If editHistory Is Nothing Then editHistory = New List(Of OneDescription)

        Dim oNew As New OneDescription(sHistory, Nothing)
        editHistory.Add(oNew)

        TagsChanged = True
    End Sub

#End Region

#Region "start/end edycji"

    <Newtonsoft.Json.JsonIgnore>
    Private Property _EditPipeline As Boolean = True


    <Newtonsoft.Json.JsonIgnore>
    Public Property _PipelineInput As Stream
    <Newtonsoft.Json.JsonIgnore>
    Public Property _PipelineOutput As Stream

    Public Sub ResetPipeline()
        ' kasowanie pipeline, żeby można było zrobić drugą
        _PipelineOutput?.Dispose()
        _PipelineOutput = Nothing
    End Sub

    Public Sub SkipEdit()
        If Not _EditPipeline Then Return

        ' po prostu symulujemy pusty krok - kopiujemy in/out
        _PipelineOutput = New MemoryStream
        _PipelineInput.Seek(0, SeekOrigin.Begin)
        _PipelineInput.CopyTo(_PipelineOutput)

        _PipelineInput.Seek(0, SeekOrigin.Begin)
        _PipelineOutput.Seek(0, SeekOrigin.Begin)

    End Sub


    ''' <summary>
    ''' przygotuj Stream do edycji
    ''' </summary>
    Public Sub InitEdit(bPipeline As Boolean)
        _EditPipeline = bPipeline

        If _EditPipeline Then
            If _PipelineOutput IsNot Nothing AndAlso _PipelineOutput.Length > 0 Then
                _PipelineInput.Dispose()
                _PipelineInput = _PipelineOutput
                _PipelineOutput = New MemoryStream
            Else
                _PipelineInput = IO.File.OpenRead(InBufferPathName)
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
            _PipelineInput.Seek(0, SeekOrigin.Begin)
            Dim oExifLib As New CompactExifLib.ExifData(_PipelineInput)
            If bResetOrientation Then
                ' zarówno w pliku ma być bez obracania, jak i w naszych danych
                oExifLib.SetTagValue(CompactExifLib.ExifTag.Orientation, 1, CompactExifLib.ExifTagType.UShort)

                If Not _EditPipeline Then
                    ' to tylko gdy zmieniamy zdjęcie w buforze
                    Dim oExif As ExifTag = GetExifOfType(ExifSource.FileExif)
                    If oExif IsNot Nothing Then oExif.Orientation = 1
                End If
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

    Public Async Function RunPipeline(sProcessingSteps As String, aPostProcesory As Vblib.PostProcBase()) As Task(Of String)
        DumpCurrMethod()

        If String.IsNullOrEmpty(sProcessingSteps) Then
            ' zrób tak, by w oPic.outputStream było to co do wysłania
            Dim oNewFileStream As FileStream = IO.File.OpenRead(InBufferPathName)
            _PipelineOutput = oNewFileStream
            Return ""
        End If

        Dim aSteps As String() = sProcessingSteps.Split(";")
        For Each sStep As String In aSteps
            ' wykonaj krok
            DumpMessage("step: " & sStep)

            For Each oEngine As Vblib.PostProcBase In aPostProcesory
                If oEngine.Nazwa.ToLowerInvariant = sStep.ToLowerInvariant Then
                    If Not Await oEngine.Apply(Me, True) Then Return "ERROR in step " & sStep
                    Exit For
                End If
            Next

        Next

        Return ""
    End Function

    Public Function CanRunPipeline(sProcessingSteps As String, aPostProcesory As Vblib.PostProcBase()) As String
        DumpCurrMethod()

        Dim sErrors As String = ""

        If String.IsNullOrEmpty(sProcessingSteps) Then
            Return ""
        End If

        Dim aSteps As String() = sProcessingSteps.Split(";")
        For Each sStep As String In aSteps
            For Each oEngine As Vblib.PostProcBase In aPostProcesory
                If oEngine.Nazwa.ToLowerInvariant = sStep.ToLowerInvariant Then
                    If Not oEngine.CanRun(Me) Then sErrors &= oEngine.Nazwa & ";"
                End If
            Next

        Next

        Return sErrors
    End Function


#End Region

    Private Sub MergeTwoExifs(oToExif As ExifTag, oAddExif As ExifTag)

        ' dla nich bierzemy ostatni wpis - pewnie zwykle będzie to ustawiane gdy w loop będzie miał SourceDefault - ale może to nie będzie pierwszy...
        If oAddExif.FileSourceDeviceType <> 0 Then oToExif.FileSourceDeviceType = oAddExif.FileSourceDeviceType
        If Not String.IsNullOrWhiteSpace(oAddExif.Author) Then oToExif.Author = oAddExif.Author

        If Not String.IsNullOrWhiteSpace(oAddExif.Copyright) Then
            If oAddExif.Copyright.Contains("%") Then
                ' special case - na koniec, przy publikacji; %1, %3 itp. jako długość odpowiedniego słowa
                Dim aFull As String() = oToExif.Copyright.Split(" ")
                Dim aShort As String() = oAddExif.Copyright.Split(" ")
                oToExif.Copyright = ""
                For iLp As Integer = 0 To aShort.Length - 1
                    Dim sToken As String = aShort.ElementAt(iLp)
                    If sToken.StartsWith("%") Then
                        Dim sOrgWord As String = aFull.ElementAt(iLp)
                        sToken = sToken.Substring(1)
                        Dim iLen As Integer = 0
                        If sToken.StartsWith("^") Then
                            Integer.TryParse(sToken.Substring(1), iLen)
                            sOrgWord = sOrgWord.ToUpperInvariant
                        Else
                            Integer.TryParse(sToken, iLen)
                        End If
                        If iLen = 0 Then
                            oToExif.Copyright = oToExif.Copyright & sOrgWord
                        Else
                            oToExif.Copyright = oToExif.Copyright & sOrgWord.Substring(0, iLen)
                        End If
                    Else
                        oToExif.Copyright = oToExif.Copyright & sToken & " "
                    End If

                Next
                oToExif.Copyright = oToExif.Copyright.Trim
            Else
                oToExif.Copyright = oAddExif.Copyright
            End If
        End If
        If Not String.IsNullOrWhiteSpace(oAddExif.CameraModel) Then oToExif.CameraModel = oAddExif.CameraModel
        If Not String.IsNullOrWhiteSpace(oAddExif.DateTimeOriginal) Then oToExif.DateTimeOriginal = oAddExif.DateTimeOriginal
        If Not String.IsNullOrWhiteSpace(oAddExif.DateTimeScanned) Then oToExif.DateTimeScanned = oAddExif.DateTimeScanned

        If Not String.IsNullOrWhiteSpace(oAddExif.Restrictions) Then oToExif.Restrictions = oAddExif.Restrictions
        If Not String.IsNullOrWhiteSpace(oAddExif.PicGuid) Then oToExif.PicGuid = oAddExif.PicGuid

        If Not String.IsNullOrWhiteSpace(oAddExif.ReelName) Then oToExif.ReelName = oAddExif.ReelName
        If Not String.IsNullOrWhiteSpace(oAddExif.OriginalRAW) Then oToExif.OriginalRAW = oAddExif.OriginalRAW
        If Not String.IsNullOrWhiteSpace(oAddExif.PicGuid) Then oToExif.PicGuid = oAddExif.PicGuid

        ' te sklejamy
        oToExif.Keywords = oToExif.Keywords.ConcatenateWithComma(oAddExif.Keywords)
        oToExif.UserComment = oToExif.UserComment.ConcatenateWithComma(oAddExif.UserComment)
        oToExif.GeoName = oToExif.GeoName.ConcatenateWithPipe(oAddExif.GeoName)

        ' to przeliczamy
        oToExif.DateMax = oToExif.DateMax.DateMin(oAddExif.DateMax)
        oToExif.DateMin = oToExif.DateMin.DateMax(oAddExif.DateMin)

    End Sub

    Public Function GetSuggestedGuid() As String

        If Exifs Is Nothing Then Return "" ' nie umiemy!

        Dim tempData As DateTime
        Dim oExif As ExifTag

        ' jeśli jakiś ID jest odczytany w EXIFach
        For Each oExif In Exifs
            If String.IsNullOrWhiteSpace(oExif.PicGuid) Then Continue For
            If oExif.PicGuid.Length < 15 Then Continue For
            If oExif.PicGuid.Length > 20 Then Continue For

            ' nasze ID to coś typu:
            ' [typ]yyyyMMddHHmmss[-a]
            '     12345678901234567
            If Not Date.TryParseExact(oExif.PicGuid.Substring(1), "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, tempData) Then Continue For

            ' Samsung Marcina Kurcza powtarzał L12XLLD00SM oraz X10LLLB00AM, więc sprawdzam czy ja mam datę.

            Return oExif.PicGuid
        Next

        Dim realData As Date = GetRealDate()
        If realData.IsDateValid Then
            ' jeśli mamy rzeczywistą datę, to próbujemy wziąć sekundy z filename
            ' (różnią się od real o sekundę, ale być może nie zawsze)
            ' a do szukania "spoza" PicSort lepiej żeby ID był w takich wypadkach taki jak filename

            ' wersja WP_
            Dim sRealDate As String = realData.ToString("yyyyMMdd")
            If sSuggestedFilename.StartsWith($"WP_{sRealDate}_{realData.ToString("HH")}_") Then
                ' telefon		WP_20221117_11_32_45_Pro
                '			0123
                '			...12345678901234567890123456
                '			WP_20221117_11_45_21_Rich
                Return GuidPrefix.DateTaken & sSuggestedFilename.Substring(3, 17).Replace("_", "")
            End If

            ' telefon Marcina, 20220723_170151
            '				 123456789012345
            If sSuggestedFilename.StartsWith($"{sRealDate}_{realData.ToString("HH")}") Then
                Return GuidPrefix.DateTaken & sSuggestedFilename.Substring(0, 15).Replace("_", "")
            End If

            ' telefon Aski 
            ' 01234567890123456789
            ' IMG_20230128_111119345
            ' ....1234567890123456789
            If sSuggestedFilename.StartsWith($"IMG_{sRealDate}_{realData.ToString("HH")}") Then
                Return GuidPrefix.DateTaken & sSuggestedFilename.Substring(4, 15).Replace("_", "")
            End If


            Return GuidPrefix.DateTaken & realData.ToString("yyyyMMddHHmmss")
        End If

        oExif = GetExifOfType(ExifSource.SourceDefault)
        If oExif IsNot Nothing Then
            If oExif.FileSourceDeviceType = FileSourceDeviceTypeEnum.digital Then
                ' gdy to aparat cyfrowy, przyjmujemy mniejszą datę pliku
                oExif = GetExifOfType(ExifSource.SourceFile)
                If oExif IsNot Nothing Then Return GuidPrefix.FileDate & oExif.DateMin.ToString("yyyyMMddHHmmss")
            End If
        End If

        DialogBox("zapomniales ze nie umiem stworzyc ID dla bezdatowych?")
        Return ""
    End Function

    ''' <summary>
    ''' sprowadza wszystkie EXIFy do jednego - ale dalej jeszcze z dodatkowymi polami!
    ''' </summary>
    ''' <returns></returns>
    Public Function FlattenExifs(bWithAzureAsComment As Boolean) As ExifTag
        Dim oNew As ExifTag = GetExifOfType(ExifSource.Flattened)
        If oNew IsNot Nothing Then Return oNew

        ' defaulty mamy z FileSourceDeviceType
        oNew = New ExifTag(ExifSource.Flattened)
        Dim oExif_SourceDefault As ExifTag = GetExifOfType(ExifSource.SourceDefault)
        If oExif_SourceDefault IsNot Nothing Then
            oNew = oExif_SourceDefault.Clone
            oNew.ExifSource = ExifSource.Flattened
        Else
            oNew = New ExifTag(ExifSource.Flattened)
        End If

        ' te sklejamy - concatenate, więc musimy najpierw wyciąć to co było z SourceDefault, bo byłoby dwa razy
        oNew.Keywords = ""
        oNew.UserComment = ""
        oNew.GeoName = ""

        For Each oExif As ExifTag In Exifs
            MergeTwoExifs(oNew, oExif)
        Next

        ' dwa przypadki szczególne
        oNew.GeoTag = GetGeoTag()   ' z priorytetem dla MANUAL

        ' description z PIC do Exif.UserComment
        If descriptions IsNot Nothing Then
            For Each oDesc As OneDescription In descriptions
                oNew.Keywords = oNew.Keywords.ConcatenateWithPipe(oDesc.keywords)
                oNew.UserComment = oNew.UserComment.ConcatenateWithPipe(oDesc.comment)
            Next
        End If

        If bWithAzureAsComment Then
            ' azure analysis - doklejamy jako usercomment
            For Each oExif As ExifTag In Exifs
                If oExif.AzureAnalysis IsNot Nothing Then
                    oNew.UserComment = oNew.UserComment.ConcatenateWithPipe(oExif.AzureAnalysis.DumpAsText)
                End If
            Next
        End If

        If oOstatniExif IsNot Nothing Then MergeTwoExifs(oNew, oOstatniExif)

        Return oNew
    End Function

    'Public Function Clone() As OnePic
    '    Dim sTxt As String = Newtonsoft.Json.JsonConvert.SerializeObject(Me, Newtonsoft.Json.Formatting.Indented)
    '    Dim oNew As OnePic = Newtonsoft.Json.JsonConvert.DeserializeObject(sTxt, GetType(OnePic))
    '    Return oNew
    'End Function

    ''' <summary>
    ''' zrobienie kopii OnePic, ale w wersji skróconej (dla archiwizacji i bazy danych)
    ''' </summary>
    ''' <returns></returns>
    Public Function GetFlatOnePic() As OnePic
        Dim oNew As OnePic = Clone()
        oNew.Exifs = New List(Of ExifTag)
        oNew.Exifs.Add(FlattenExifs(False))
        oNew.InBufferPathName = Nothing
        oNew.editHistory = Nothing
        Return oNew
    End Function

    Public Function GetAllKeywords() As String

        Dim oFlat As ExifTag = FlattenExifs(False)
        Return oFlat.Keywords
    End Function

    Public Function HasKeyword(oKey As OneKeyword) As Boolean
        If Exifs Is Nothing Then Return False

        For Each oExif As ExifTag In Exifs
            If oExif.Keywords.Contains(oKey.sId) Then Return True
        Next

        Return False
    End Function

    Public Function HasKeyword(sKey As String) As Boolean
        If Exifs Is Nothing Then Return False

        For Each oExif As ExifTag In Exifs
            If oExif.Keywords IsNot Nothing AndAlso oExif.Keywords.Contains(sKey) Then Return True
        Next

        Return False
    End Function

    ''' <summary>
    ''' sprawdza czy spełnione są warunki keywords (z ! jako zaprzeczeniem)
    ''' </summary>
    ''' <param name="aTags"></param>
    ''' <returns></returns>
    Public Function MatchesKeywords(aTags As String()) As Boolean
        For Each sTag As String In aTags
            If String.IsNullOrWhiteSpace(sTag) Then Continue For
            If sTag.StartsWith("!") Then
                If HasKeyword(sTag.Substring(1)) Then Return False
            Else
                If Not HasKeyword(sTag.Substring(1)) Then Return False
            End If
        Next
        Return True
    End Function

    Public Function GetDescriptionForCloud() As String

        Dim sRet As String = GetSumOfDescriptionsText()
        If sRet <> "" Then Return sRet

        sRet = GetSumOfCommentText()
        If sRet <> "" Then Return sRet

        Dim oExif As ExifTag = GetExifOfType(ExifSource.AutoAzure)
        If oExif Is Nothing Then Return ""

        Return oExif.AzureAnalysis.Captions?.ToComment("Azure")

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
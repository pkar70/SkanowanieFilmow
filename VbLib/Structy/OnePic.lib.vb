

Imports System.Globalization
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Threading
Imports CompactExifLib
'Imports MetadataExtractor
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports pkar
'Imports XmpCore.Impl
Imports pkar.DotNetExtensions

<Serializable>
Public Class OnePic
    Inherits pkar.BaseStruct

    Public Property Archived As String
    Public Property CloudArchived As String
    Public Property Published As Dictionary(Of String, String)
    Public Property TargetDir As String ' OneDirFlat.sId
    Public Property Exifs As New List(Of ExifTag) ' ExifSource.SourceFile ..., )
    Public Property InBufferPathName As String ' przy Sharing: GUID pliku, tymczasowe przy odbieraniu z upload

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

    Public Property sharingFromGuid As String   ' a'la UseNet Path, tyle że rozdzielana ";"; GUIDy kolejne; wpsywane przez httpserver.lib; prefiksy: "L:" z loginu, "S:" z serwera
    Public Property sharingLockSharing As Boolean

    Public Property allowedPeers As String

    Public Property serno As Integer

    Public Property linki As List(Of OneLink)

    'Public Property sortOrder As String

    <Newtonsoft.Json.JsonIgnore>
    Public Property toProcessed As String

    <Newtonsoft.Json.JsonIgnore>
    Public Property oContent As IO.Stream
    <Newtonsoft.Json.JsonIgnore>
    Public Property oOstatniExif As ExifTag

    <Newtonsoft.Json.JsonIgnore>
    Public Property locked As Boolean = False

    ' w summary kopia - jako ułatwienie przy pisaniu kodu
    ''' <summary>
    ''' "*.jpg;*.tif;*.gif;*.png"
    ''' </summary>
    Public Shared ReadOnly ExtsPic As String = "*.jpg;*.tif;*.gif;*.png;*.jpeg;*.nar;*.raf"
    ''' <summary>
    ''' "*.mov;*.avi;*.mp4;*.m4v;*.mkv"
    ''' </summary>
    Public Shared ReadOnly ExtsMovie As String = "*.mov;*.avi;*.mp4;*.m4v;*.mkv"
    Public Shared ReadOnly ExtsStereo As String = "*.jps;*.stereo.zip"

    Public Sub New(sourceName As String, inSourceId As String, suggestedFilename As String)
        DumpCurrMethod()
        sSourceName = sourceName
        sInSourceID = inSourceId
        sSuggestedFilename = suggestedFilename
    End Sub

    ''' <summary>
    ''' konstruktor dla EntityFramework
    ''' </summary>
    Public Sub New()

    End Sub

#Region "operacje na Archived"
    Public Sub AddArchive(sArchName As String)
        If IsArchivedIn(sArchName) Then Return
        If Archived Is Nothing Then Archived = ""
        Archived &= sArchName & ";"
    End Sub

    Public Function IsArchivedIn(sArchName As String) As Boolean
        If Archived Is Nothing Then Return False
        If Archived.ContainsCI(sArchName & ";") Then Return True
        Return False
    End Function

    Public Function ArchivedCount() As Integer
        If Archived Is Nothing Then Return 0
        Dim aArr As String() = Archived.Split(";")
        Return aArr.Count - 1
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
        If IsCloudPublishMentioned(sPublName) Then Return
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

    ''' <summary>
    ''' sprawdza czy jest taki key w słowniku publishingów
    ''' </summary>
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
    Public Function GetGeoTag() As pkar.BasicGeoposWithRadius
        DumpCurrMethod($"({InBufferPathName})")
        ' ważniejsze jest z keywords, potem manual_geo, potem - dowolny

        Dim oExif As ExifTag = GetExifOfType(ExifSource.ManualTag)
        If oExif?.GeoTag IsNot Nothing Then
            DumpMessage("not null ExifSource.ManualTag.GeoTag")
            If Not oExif.GeoTag.IsEmpty Then Return New pkar.BasicGeoposWithRadius(oExif.GeoTag, oExif.GeoZgrubne)
            DumpMessage($"... but IsEmpty, {oExif.GeoTag.Latitude}, {oExif.GeoTag.Longitude}")
        End If

        oExif = GetExifOfType(ExifSource.ManualGeo)
        If oExif?.GeoTag IsNot Nothing Then
            DumpMessage("not null ExifSource.ManualGeo.GeoTag")
            If Not oExif.GeoTag.IsEmpty Then Return New pkar.BasicGeoposWithRadius(oExif.GeoTag, oExif.GeoZgrubne)
            DumpMessage($"... but IsEmpty, {oExif.GeoTag.Latitude}, {oExif.GeoTag.Longitude}")
        End If

        For Each oExif In Exifs
            If oExif?.GeoTag IsNot Nothing Then
                DumpMessage($"not null GeoTag in {oExif.ExifSource}")
                If Not oExif.GeoTag.IsEmpty Then Return New pkar.BasicGeoposWithRadius(oExif.GeoTag, oExif.GeoZgrubne)
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
            If oItem.DateMin.IsDateValid Then dDateMin = dDateMin.Max(oItem.DateMin)
            'End If
        Next

        Return dDateMin

    End Function

    Public Function GetMaxDate() As Date
        Dim dDateMax As Date = Date.MaxValue

        For Each oItem As ExifTag In Exifs
            ' If oItem.ExifSource = ExifSource.ManualTag Then
            If oItem.DateMax.IsDateValid Then dDateMax = dDateMax.Min(oItem.DateMax)
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

    ''' <summary>
    ''' czy jest data zdjęcia, z EXIF lub z MANUAL - ale dokładna, nie zakres
    ''' </summary>
    ''' <returns></returns>
    Public Function HasRealDate() As Boolean
        Dim oExif As ExifTag = GetExifOfType(ExifSource.ManualDate)
        If Not String.IsNullOrWhiteSpace(oExif?.DateTimeOriginal) Then Return True

        oExif = GetExifOfType(ExifSource.FileExif)
        If Not String.IsNullOrWhiteSpace(oExif?.DateTimeOriginal) Then Return True

        Return False
    End Function


    ''' <summary>
    '''  zwraca rzeczywistą datę - z MANUAL.DateTimeOriginal (Original, lub Max-Min) albo FileExif.DateTimeOriginal
    ''' </summary>
    ''' <returns></returns>
    Private Function GetRealDate() As Date

        Dim oExif As ExifTag = GetExifOfType(ExifSource.ManualDate)
        If oExif IsNot Nothing Then
            If Not String.IsNullOrWhiteSpace(oExif.DateTimeOriginal) Then Return ExifDateToDate(oExif.DateTimeOriginal)

            Dim dtdiff As TimeSpan = oExif.DateMax - oExif.DateMin
            Return oExif.DateMin.AddMinutes(dtdiff.TotalMinutes / 2)
        End If


        oExif = GetExifOfType(ExifSource.FileExif)
        If oExif Is Nothing Then Return Date.MaxValue

        ' zeskanowanie daty (string->date)
        Dim retDate As Date = ExifDateToDate(oExif.DateTimeOriginal)
        Return retDate
    End Function

    ''' <summary>
    ''' Ważność: FileExif, SourceFile, ManualTag (min ze wszystkich)
    ''' </summary>
    ''' <param name="bSkipTags">True, gdy bez wyliczania (tylko real)</param>
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

        Dim sFilenameNoPath As String
        If InBufferPathName IsNot Nothing Then
            sFilenameNoPath = IO.Path.GetFileName(InBufferPathName)   ' dla edycji było GetSourceFilename, ale to poprzednia wersja
        Else
            sFilenameNoPath = IO.Path.GetFileName(sSuggestedFilename)   ' dla edycji było GetSourceFilename, ale to poprzednia wersja
        End If

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
                Dim regExMaska As New Regex(DOSmask2regExp(maska.Trim))
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
            Dim regExMaska As New Regex(DOSmask2regExp(maska.Trim))
            If regExMaska.IsMatch(sFilename) Then
                bMatch = True
                Exit For
            End If
        Next

        Return bMatch
    End Function

#End Region

    Public Function GetFileTypeIcon() As String
        If MatchesMasks("*.nar") Then Return "*"
        If MatchesMasks("*.avi") Then Return "►"
        If MatchesMasks("*.mov") Then Return "►"
        If MatchesMasks("*.mp4") Then Return "►"
        If MatchesMasks("*.mkv") Then Return "►"
        If MatchesMasks("*.jps") Then Return "⧉"
        If InBufferPathName.ContainsCI("stereo.zip") Then Return "⧉"
        Return ""
    End Function

    Public Sub SetDefaultFileTypeDiscriminator()
        fileTypeDiscriminator = GetFileTypeIcon()
    End Sub

#Region "linki"

    ''' <summary>
    ''' dodaj link, sprawdzając unikalność
    ''' </summary>
    ''' <returns>FALSE gdy już taki URL bądź taki opis istnieje</returns>
    Public Function AddLink(linek As OneLink) As Boolean
        If linki Is Nothing Then linki = New List(Of OneLink)
        If linki.Any(Function(x) x.link = linek.link OrElse x.opis = linek.opis) Then Return False
        linki.Add(linek)
        Return True
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

    ''' <summary>
    ''' usuwa wszystkie descriptions, i wstawia nowe - jako "dzisiejsze"
    ''' </summary>
    Public Sub ReplaceAllDescriptions(sDesc As String)
        descriptions = New List(Of OneDescription)
        AddDescription(New OneDescription(sDesc, ""))
    End Sub

    Public Function GetSumOfDescriptionsText(Optional separator As String = " | ") As String
        If descriptions Is Nothing Then Return ""

        Dim sRet As String = ""
        For Each oDesc As OneDescription In descriptions
            ' pomijamy systemowe opisy
            If oDesc.comment.StartsWith("Cropped to ") Then Continue For
            If oDesc.comment.StartsWith("Rotated ") Then Continue For
            If oDesc.comment.Trim.Length < 2 Then Continue For
            If sRet <> "" Then sRet &= separator
            sRet &= oDesc.comment.Trim
        Next

        Return sRet.Trim
    End Function

    Public Function GetSumOfDescriptionsKwds() As String
        If descriptions Is Nothing Then Return ""

        Dim sRet As String = ""
        For Each oDesc As OneDescription In descriptions
            sRet &= " " & oDesc.keywords
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
        AddDescription(opis)

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
                _PipelineInput?.Dispose()
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
            Dim oExifLib As CompactExifLib.ExifData
            Try
                oExifLib = New CompactExifLib.ExifData(_PipelineInput)
            Catch ex As Exception
                DumpMessage("Sorry, source EXIFtag is corrupted")
            End Try

            If oExifLib IsNot Nothing Then
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
                _PipelineOutput?.Dispose()
                _PipelineOutput = tempStream
            Else
                ' błąd w EXIFlib wejściowym - tylko kopiujemy IN na OUT
                Dim tempStream As New MemoryStream
                _PipelineInput.CopyTo(tempStream)
                _PipelineOutput?.Dispose()
                _PipelineOutput = tempStream
            End If
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

                ' ewentualne HIDE bak
                If GetSettingsBool("uiHideThumbs") Then
                    Dim attrs As IO.FileAttributes
                    attrs = IO.File.GetAttributes(bakFileName)
                    attrs = attrs Or IO.FileAttributes.Hidden
                    IO.File.SetAttributes(bakFileName, attrs)
                End If

            End If

            ' bo Write zostawiłby śmieci na końcu (resztę dłuższego pliku)
            If IO.File.Exists(InBufferPathName) Then IO.File.Delete(InBufferPathName)

            Using oNewFileStream As FileStream = IO.File.OpenWrite(InBufferPathName)
                _PipelineOutput.Seek(0, SeekOrigin.Begin)
                _PipelineOutput.CopyTo(oNewFileStream)
                oNewFileStream.Flush()
            End Using

        End If



    End Sub

    ''' <summary>
    ''' Przygotowanie obrazka, po pipeline, do _PipelineOutput
    ''' </summary>
    ''' <param name="sProcessingSteps">NULL/empty oznacza brak kroków</param>
    ''' <param name="aPostProcesory"></param>
    ''' <param name="bPreferAnaglyph"></param>
    ''' <returns></returns>
    Public Async Function RunPipeline(sProcessingSteps As String, aPostProcesory As Vblib.PostProcBase(), bPreferAnaglyph As Boolean) As Task(Of String)
        DumpCurrMethod($"plik: {sSuggestedFilename}, steps: {sProcessingSteps}")

        ' gdy nie ma processingu, wyślij plik źródłowy (nar, zip, i tak dalej)
        If String.IsNullOrEmpty(sProcessingSteps) Then
            Dim oNewFileStream As FileStream = IO.File.OpenRead(InBufferPathName) ' SinglePicFromMulti(bPreferAnaglyph) 'IO.File.OpenRead(InBufferPathName)
            _PipelineOutput = oNewFileStream
            Return ""
        End If

        ' jak jest processing, to zrób tak by w oPic.outputStream było to co na początek
        _PipelineOutput = SinglePicFromMulti(bPreferAnaglyph) ' SinglePicFromMulti(bPreferAnaglyph) 'IO.File.OpenRead(InBufferPathName)

        ' i kolejne kroki
        Dim aSteps As String() = sProcessingSteps.Split(";")
        For Each sStep As String In aSteps
            ' wykonaj krok
            DumpMessage("step: " & sStep)
            Dim sParams As String = ""
            Dim sKrok As String = sStep.ToLowerInvariant
            Dim iInd As Integer = sStep.IndexOf("(")
            If iInd > 0 Then
                sKrok = sKrok.Substring(0, iInd)
                sParams = sKrok.Substring(iInd + 1)
                If sParams.EndsWith(")") Then sParams = sParams.Substring(0, sParams.Length - 1)
            End If

            For Each oEngine As Vblib.PostProcBase In aPostProcesory
                If oEngine.Nazwa.EqualsCI(sKrok) Then
                    If Not Await oEngine.Apply(Me, True, sParams) Then Return "ERROR in step " & sStep
                    Exit For
                End If
            Next

        Next

        Return ""
    End Function

    ''' <summary>
    ''' sprawdza czy wszystkie kroki pipeline dadzą się uruchomić (sprawdza dozwolone maski dla każdego kroku)
    ''' </summary>
    ''' <param name="sProcessingSteps"></param>
    ''' <param name="aPostProcesory"></param>
    ''' <returns></returns>
    Public Function CanRunPipeline(sProcessingSteps As String, aPostProcesory As Vblib.PostProcBase()) As String
        DumpCurrMethod()

        Dim sErrors As String = ""

        If String.IsNullOrEmpty(sProcessingSteps) Then
            Return ""
        End If

        Dim aSteps As String() = sProcessingSteps.Split(";")

        ' 2024.01.02: przecież tylko pierwszy jest ważny, potem idzie w bitmapie :)
        Dim sStep As String = aSteps(0)
        'For Each sStep As String In aSteps
        For Each oEngine As Vblib.PostProcBase In aPostProcesory
            If Not oEngine.Nazwa.EqualsCI(sStep) Then Continue For

            If Me.InBufferPathName.ContainsCI("stereo.zip") Then
                ' dla stereo.zip będziemy sie posługiwać JPGami
                If Not oEngine.CanRun("dummpy.jpg") Then sErrors &= oEngine.Nazwa & ";"
            Else
                If Not oEngine.CanRun(Me.InBufferPathName) Then sErrors &= oEngine.Nazwa & ";"
            End If

            ' już wiemy - bo tylko jeden krok sprawdzamy
            Return sErrors
        Next
        'Next

        Return sErrors
    End Function


#End Region

    Private Sub MergeTwoExifs(oToExif As ExifTag, oAddExif As ExifTag)

        ' dla nich bierzemy ostatni wpis - pewnie zwykle będzie to ustawiane gdy w loop będzie miał SourceDefault - ale może to nie będzie pierwszy...
        If oAddExif.FileSourceDeviceType <> 0 Then oToExif.FileSourceDeviceType = oAddExif.FileSourceDeviceType
        If Not String.IsNullOrWhiteSpace(oAddExif.Author) Then oToExif.Author = oAddExif.Author

        If Not String.IsNullOrWhiteSpace(oAddExif.Copyright) Then
            If oAddExif.Copyright.Contains("%") Then
                If String.IsNullOrWhiteSpace(oToExif.Copyright) Then
                    oToExif.Copyright = ""
                Else
                    ' special case - na koniec, przy publikacji; %1, %3 itp. jako długość odpowiedniego słowa
                    Dim aFull As String() = oToExif.Copyright.Split(" ")
                    Dim aShort As String() = oAddExif.Copyright.Split(" ")
                    oToExif.Copyright = ""
                    For iLp As Integer = 0 To Math.Min(aShort.Length - 1, aFull.Length - 1)
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
                End If
            Else
                oToExif.Copyright = oAddExif.Copyright
            End If
        End If
        If Not String.IsNullOrWhiteSpace(oAddExif.CameraModel) Then oToExif.CameraModel = oAddExif.CameraModel
        If Not String.IsNullOrWhiteSpace(oAddExif.DateTimeOriginal) Then oToExif.DateTimeOriginal = oAddExif.DateTimeOriginal
        If Not String.IsNullOrWhiteSpace(oAddExif.DateTimeScanned) Then oToExif.DateTimeScanned = oAddExif.DateTimeScanned

        If Not String.IsNullOrWhiteSpace(oAddExif.Restrictions) Then oToExif.Restrictions = oAddExif.Restrictions
        'If Not String.IsNullOrWhiteSpace(oAddExif.PicGuid) Then oToExif.PicGuid = oAddExif.PicGuid

        If Not String.IsNullOrWhiteSpace(oAddExif.ReelName) Then oToExif.ReelName = oAddExif.ReelName
        If Not String.IsNullOrWhiteSpace(oAddExif.OriginalRAW) Then oToExif.OriginalRAW = oAddExif.OriginalRAW
        'If Not String.IsNullOrWhiteSpace(oAddExif.PicGuid) Then oToExif.PicGuid = oAddExif.PicGuid

        ' te sklejamy
        oToExif.Keywords = oToExif.Keywords.ConcatenateWithComma(oAddExif.Keywords)
        oToExif.UserComment = oToExif.UserComment.ConcatenateWithComma(oAddExif.UserComment)
        oToExif.GeoName = oToExif.GeoName.ConcatenateWithPipe(oAddExif.GeoName)

        ' to przeliczamy
        oToExif.DateMax = oToExif.DateMax.Min(oAddExif.DateMax)
        oToExif.DateMin = oToExif.DateMin.Max(oAddExif.DateMin)

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

            Dim sRealDate As String = realData.ToString("yyyyMMdd")
            ' wersja WP_
            If sSuggestedFilename.StartsWith($"WP_{sRealDate}_{realData.ToString("HH")}_") Then
                ' telefon		WP_20221117_11_32_45_Pro
                '			0123
                '			...12345678901234567890123456
                '			WP_20221117_11_45_21_Rich
                Return GuidPrefix.DateTaken & sSuggestedFilename.Substring(3, 17).Replace("_", "")
            End If
            ' wersja WIN_
            If sSuggestedFilename.StartsWith($"WIN_{sRealDate}_{realData.ToString("HH")}_") Then
                Return GuidPrefix.DateTaken & sSuggestedFilename.Substring(4, 17).Replace("_", "")
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
            Else
                If oExif.FileSourceDeviceType = FileSourceDeviceTypeEnum.scannerTransparent OrElse
                    oExif.FileSourceDeviceType = FileSourceDeviceTypeEnum.scannerReflex Then
                    ' gdy to skaner

                    oExif = GetExifOfType(ExifSource.FileExif)
                    If Not String.IsNullOrWhiteSpace(oExif?.DateTimeScanned) Then
                        tempData = oExif.DateTimeScanned
                        If Not String.IsNullOrWhiteSpace(tempData) Then
                            Dim retDate As Date = ExifDateToDate(tempData)
                            If retDate.IsDateValid Then Return GuidPrefix.ScannedDate & retDate.ToString("yyyyMMddHHmmss")
                        End If
                    End If

                    ' nie ma daty Scanned, to przyjmujemy mniejszą datę pliku
                    oExif = GetExifOfType(ExifSource.SourceFile)
                    If oExif IsNot Nothing Then Return GuidPrefix.FileDate & oExif.DateMin.ToString("yyyyMMddHHmmss")
                End If

            End If

            If oExif.FileSourceDeviceType = FileSourceDeviceTypeEnum.unknown Then
                DialogBox("niezdefiniowany typ źródła podczas importu, nie wiem co zrobić?" & vbCrLf & InBufferPathName)
                Return ""
            End If
        End If


        DialogBox("zapomniales ze nie umiem stworzyc ID dla bezdatowych?" & vbCrLf & InBufferPathName)
        Return ""
    End Function

    ''' <summary>
    ''' Tworzy ID z SerNo, lub sSuggestedFilename - dla Publish/EXIF
    ''' </summary>
    Public Function GetImageUniqueId() As String
        Dim tempGUID As String = ""

        If Not String.IsNullOrWhiteSpace(PicGuid) Then tempGUID = PicGuid & ";"

        If serno > 0 Then
            tempGUID &= GetFormattedSerNo()
        Else
            tempGUID &= "#" & sSuggestedFilename
        End If

        Return tempGUID
    End Function

    ''' <summary>
    ''' zwraca serno uzupełnione odpowiednio '0', albo #?? jeśli nie ma ustawionego
    ''' </summary>
    ''' <returns></returns>
    Public Function GetFormattedSerNo() As String
        If serno > 0 Then
            Dim tempNum As String = serno
            Dim brakuje As Integer = GetSettingsBool("uiSerNoDigits") - tempNum.Length
            For iLp = 1 To brakuje
                'tempNum = Space(brakuje).Replace(" ", "0") ' Space nie ma w .Net Std 1.4 :)
                tempNum &= "0"
            Next
            Return "#" & tempNum
        Else
            Return "#??"
        End If
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

    ''' <summary>
    ''' pobiera (poprzez Flatten) wszystkie keywords, ale robi na tym uniq
    ''' </summary>
    ''' <returns>string zawierający słowa kluczowe rozdzielone spacjami</returns>
    Public Function GetAllKeywords() As String

        Dim oFlat As ExifTag = FlattenExifs(False)
        Dim temp As String() = oFlat.Keywords.Replace(",", "").Split(" ")

        Dim ret As String = ""
        For Each kwd As String In From c In temp Distinct
            ret = ret & kwd & " "
        Next

        Return ret.Trim
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
                If Not HasKeyword(sTag) Then Return False
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

#Region "sharing"

    ''' <summary>
    ''' zwraca ostatni zapis ze ścieżki sharing, czyli GUID poprzedzony L: / S:, lub ""
    ''' </summary>
    ''' <returns>L:guid / S:guid / "" gdy nie ma</returns>
    Public Function GetLastShareGuid(Optional withSerno As Boolean = False) As String
        If String.IsNullOrWhiteSpace(sharingFromGuid) Then Return ""

        Dim tempGuid As String = sharingFromGuid
        If tempGuid.EndsWith(";") Then tempGuid = tempGuid.Substring(0, tempGuid.Length - 1)
        Dim iInd As Integer = tempGuid.LastIndexOf(";")
        If iInd > 0 Then tempGuid = tempGuid.Substring(iInd + 1)

        If Not withSerno Then
            ' usuwamy ID stamtąd, *TODO* przerobić tak by :serno można było wykorzystać do odsyłania opisów
            iInd = tempGuid.IndexOf(":")
            If iInd > 0 Then tempGuid = tempGuid.Substring(0, iInd)
        End If

        Return tempGuid
    End Function

    'Private Function GetLastShareLogin(lista As ShareLoginsList) As SharePeer
    '    Dim tempGuid As String = GetLastShareGuid()
    '    If tempGuid = "" Then Return Nothing
    '    If tempGuid.StartsWith("L:") Then
    '        Return lista.FindByLogin(tempGuid)
    '    Else
    '        ' to będzie w takim razie z ShareServer, nie ShareLogin
    '        Return Nothing
    '    End If
    'End Function

    'Private Function GetLastShareServer(lista As ShareServerList) As ShareServer
    '    Dim tempGuid As String = GetLastShareGuid()
    '    If tempGuid = "" Then Return Nothing
    '    If tempGuid.StartsWith("L:") Then
    '        Return lista.FindByLogin(tempGuid)
    '    Else
    '        ' to będzie w takim razie z ShareServer, nie ShareLogin
    '        Return Nothing
    '    End If
    'End Function

    ''' <summary>
    ''' Zwraca ShareLogin bądź ShareServer, ostatni który jest na ścieżce; łatwiej użyć z ThumbsPicek
    ''' </summary>
    Public Function GetLastSharePeer(serwery As ShareServerList, loginy As ShareLoginsList) As SharePeer
        Dim tempGuid As String = GetLastShareGuid()
        If tempGuid = "" Then Return Nothing
        If tempGuid.StartsWith("L:") Then
            Return loginy.FindByGuid(tempGuid.Substring(2))
        ElseIf tempGuid.StartsWith("S:") Then
            ' to będzie w takim razie z ShareServer, nie ShareLogin
            Return serwery.FindByGuid(tempGuid.Substring(2))
        Else
            Return Nothing
        End If
    End Function

    Public Function IsPeerAllowed(peer As SharePeer) As Boolean
        If String.IsNullOrWhiteSpace(allowedPeers) Then Return False

        Return allowedPeers.Contains(peer.GetIdForSharing)
    End Function

    Public Sub AllowPeer(peer As SharePeer)
        ' juz jest, nie dodajemy drugi raz
        If IsPeerAllowed(peer) Then Return

        allowedPeers &= peer.GetIdForSharing
    End Sub

    Public Sub DenyPeer(peer As SharePeer)
        If String.IsNullOrWhiteSpace(allowedPeers) Then Return
        If Not IsPeerAllowed(peer) Then Return
        allowedPeers = allowedPeers.Replace(peer.GetIdForSharing, "")
    End Sub

    ''' <summary>
    ''' Zwraca Clone, po usunięciu informacji które nie powinny trafić do obcych
    ''' </summary>
    Public Function StrippedForSharing() As OnePic
        Dim temp As OnePic = Me.Clone
        Archived = ""
        CloudArchived = ""
        'Public Property Published As Dictionary(Of String, String)
        TargetDir = ""
        'Public Property Exifs As New List(Of ExifTag) ' ExifSource.SourceFile ..., )
        InBufferPathName = ""
        'Public Property sSourceName As String
        'Public Property sInSourceID As String    ' usually pathname
        'Public Property sSuggestedFilename As String ' mia┼éo by─ç ┼╝e np. scinanie WP_. ale jednak tego nie robi─Ö (bo moge posortowac po dacie, albo po nazwach - i w tym drugim przypadku mam rozdzia┼é na np. telefon i aparat)
        'Public Property descriptions As List(Of OneDescription)
        'Public Property editHistory As List(Of OneDescription)
        TagsChanged = False
        'Public Property fileTypeDiscriminator As String = Nothing   ' tu "|>", "*", kt├│re maj─ů by─ç dodawane do miniaturek
        PicGuid = Nothing
        'Public Property sharingFromGuid As String   ' a'la UseNet Path, tyle ┼╝e rozdzielana ";"; GUIDy kolejne; wpsywane przez httpserver.lib; prefiksy: "L:" z loginu, "S:" z serwera
        sharingLockSharing = False
        allowedPeers = Nothing

        ' te i tak są Ignored, więc nie przejdą przez Clone
        'Public Property toProcessed As String
        'Public Property oContent As IO.Stream
        'Public Property oOstatniExif As ExifTag
        'Public Property locked As Boolean = False
        'Private Property _EditPipeline As Boolean = True
        'Public Property _PipelineInput As Stream
        'Public Property _PipelineOutput As Stream

        Return temp
    End Function

#End Region


#Region "searching by query"

    Public Function CheckIfMatchesQuery(query As SearchQuery) As Boolean

        Dim oExif As Vblib.ExifTag
        Dim bGdziekolwiekMatch As Boolean = False


#Region "ogólne"

        oExif = GetExifOfType(ExifSource.FileExif)

        'If Not CheckStringContains(oExif.ReelName, query.ogolne.reel) Then Return False

        If query.ogolne.MaxDate.IsDateValid Or query.ogolne.MinDate.IsDateValid Then

            Dim picMinDate, picMaxDate As Date

            oExif = GetExifOfType(ExifSource.FileExif)
            Dim oExifDate As Vblib.ExifTag = GetExifOfType(ExifSource.ManualDate)
            If oExifDate Is Nothing Then oExifDate = oExif

            ' jeśli istnieje data zrobienia zdjęcia, to ją bierzemy, jeśli nie - to zakres ze słów kluczowych itp.
            If oExifDate IsNot Nothing AndAlso oExif.FileSourceDeviceType = FileSourceDeviceTypeEnum.digital Then
                picMaxDate = oExifDate.DateTimeOriginal
                picMinDate = oExifDate.DateTimeOriginal
            Else
                picMinDate = GetMinDate()
                picMaxDate = GetMaxDate()
            End If

            If query.ogolne.IgnoreYear Then
                ' bierzemy daty oDir, i zmieniamy Year na taki jak w UI query
                picMinDate = picMinDate.AddYears(query.ogolne.MinDate.Year - picMinDate.Year)
                picMaxDate = picMaxDate.AddYears(query.ogolne.MaxDate.Year - picMaxDate.Year)
            End If

            If query.ogolne.MaxDateCheck AndAlso query.ogolne.MaxDate.IsDateValid Then
                If picMinDate > query.ogolne.MaxDate Then Return False
            End If

            If query.ogolne.MinDateCheck AndAlso query.ogolne.MinDate.IsDateValid Then
                If picMaxDate < query.ogolne.MinDate Then Return False
            End If

        End If

        ' jeśli podany serno, to wtedy EXACT musi być (i jest to liczba!)
        If query.ogolne.serno > 0 Then
            If serno <> query.ogolne.serno Then Return False
        End If

        If Not CheckStringContains(PicGuid, query.ogolne.GUID) Then Return False

        If Not String.IsNullOrWhiteSpace(query.ogolne.Tags) Then
            ' TAGS może mieć "!"
            Dim kwrds As String = query.ogolne.Tags
            If kwrds = "!" Then
                For Each oExif1 As ExifTag In Exifs
                    If oExif1.Keywords IsNot Nothing Then Return False
                Next
            End If
            ' automatyczne wyłączenie =X, jeśli nie jest podane wprost
            If Not kwrds.Contains("=X") Then kwrds &= " !=X"
            If Not MatchesKeywords(kwrds.Split(" ")) Then Return False
        End If

        Dim descripsy As String = GetSumOfDescriptionsText() & " " & GetSumOfCommentText()
        If query.ogolne.Descriptions = "!" Then
            If descripsy.Trim <> "" Then Return False
        End If
        If Not CheckStringMasks(descripsy, query.ogolne.Descriptions) Then Return False

        ' wspóne - tekst w paru miejscach: Descriptions,  Folder, Filename, OCR, Azure description
        If Not String.IsNullOrEmpty(query.ogolne.Gdziekolwiek) Then
            If Not CheckStringMasksNegative(descripsy, query.ogolne.Gdziekolwiek) Then Return False
            If Not CheckStringMasksNegative(TargetDir, query.ogolne.Gdziekolwiek) Then Return False
            If Not CheckStringMasksNegative(sSuggestedFilename, query.ogolne.Gdziekolwiek) Then Return False

            If CheckStringMasks(descripsy, query.ogolne.Gdziekolwiek) Then bGdziekolwiekMatch = True
            If CheckStringMasks(TargetDir, query.ogolne.Gdziekolwiek) Then bGdziekolwiekMatch = True
            If CheckStringMasks(sSuggestedFilename, query.ogolne.Gdziekolwiek) Then bGdziekolwiekMatch = True
        End If

#Region "ogólne - advanced"

        If query.ogolne.adv.TargetDir = "!" Then If Not String.IsNullOrWhiteSpace(TargetDir) Then Return False
        If Not CheckStringMasks(TargetDir, query.ogolne.adv.TargetDir) Then Return False
        If Not CheckStringContains(sSourceName, query.ogolne.adv.Source) Then Return False

        If Not MatchesMasks(query.ogolne.adv.Filename, "") Then Return False

        If Not query.ogolne.adv.TypePic Then
            If MatchesMasks(OnePic.ExtsPic) Then Return False
        End If
        If Not query.ogolne.adv.TypeMovie Then
            If MatchesMasks(OnePic.ExtsMovie) Then Return False
        End If
        If Not query.ogolne.adv.TypeStereo Then
            If MatchesMasks(OnePic.ExtsStereo) Then Return False
        End If


        ' If Not uiTypeOth.IsChecked Then

        If query.ogolne.adv.CloudArchived = "!" Then If Not String.IsNullOrWhiteSpace(CloudArchived) Then Return False
        If Not CheckStringMasks(CloudArchived, query.ogolne.adv.CloudArchived) Then Return False

        If Not String.IsNullOrWhiteSpace(query.ogolne.adv.Published) Then
            Dim publishy As String = ""
            For Each item In Published
                publishy = publishy & " " & item.Key
            Next

            If query.ogolne.adv.Published = "!" Then If Not String.IsNullOrWhiteSpace(publishy) Then Return False
            If Not CheckStringMasks(publishy, query.ogolne.adv.Published) Then Return False

        End If

        ' *TODO* fileTypeDiscriminator ? As String = Nothing


#End Region

        If Not String.IsNullOrWhiteSpace(query.ogolne.geo.Name) Then
            Dim sGeoName As String = ""
            For Each oExif In Exifs
                If Not String.IsNullOrWhiteSpace(oExif.GeoName) Then
                    If query.ogolne.geo.Name = "!" Then Return False
                    sGeoName = sGeoName & " " & oExif.GeoName
                End If
            Next

            If Not CheckStringMasks(sGeoName, query.ogolne.geo.Name) Then Return False
        End If

        If query.ogolne.geo.Location IsNot Nothing Then
            Dim geotag As BasicGeoposWithRadius = GetGeoTag()

            If geotag Is Nothing Then
                If Not query.ogolne.geo.AlsoEmpty Then Return False
            Else
                If Not geotag.IsInsideCircle(query.ogolne.geo.Location) Then Return False
            End If
        End If

        ' pomijamy: InBufferPathName, sInSourceID, TagsChanged, Archived, editHistory

#End Region


#Region "SourceDefault"

        oExif = GetExifOfType(Vblib.ExifSource.SourceDefault)
        If oExif IsNot Nothing Then
            If query.source_author = "!" Then If Not String.IsNullOrWhiteSpace(oExif.Author) Then Return False
            If Not CheckStringMasks(oExif.Author, query.source_author) Then Return False

            ' było: -1, ale po dodaniu ProgRing w SearchWnd jest 0 jako nieznane?
            If query.source_type > 0 Then
                If oExif.FileSourceDeviceType <> query.source_type Then Return False
            End If
        End If


#End Region

#Region "AutoExif"
        oExif = GetExifOfType(Vblib.ExifSource.FileExif)
        If oExif IsNot Nothing Then
            If query.exif_camera = "!" Then If Not String.IsNullOrWhiteSpace(oExif.CameraModel) Then Return False
            If Not CheckStringMasks(oExif.CameraModel, query.exif_camera) Then Return False
        End If

#End Region

        'Public Const AutoWinOCR As String = "AUTO_WINOCR"
        oExif = GetExifOfType(Vblib.ExifSource.AutoWinOCR)
        If Not String.IsNullOrEmpty(query.ocr) Then
            If oExif?.UserComment Is Nothing Then Return False
            If Not CheckStringMasks(oExif.UserComment, query.ocr) Then Return False
        End If

        If Not String.IsNullOrEmpty(query.ogolne.Gdziekolwiek) AndAlso oExif?.UserComment IsNot Nothing Then
            ' wspóne - tekst w paru miejscach: Descriptions,  Folder, Filename, OCR, Azure description
            If Not CheckStringMasksNegative(oExif.UserComment, query.ogolne.Gdziekolwiek) Then Return False
            If CheckStringMasks(oExif.UserComment, query.ogolne.Gdziekolwiek) Then bGdziekolwiekMatch = True
        End If
#Region "astro"


        If query.astro.MoonCheck Then

            oExif = GetExifOfType(Vblib.ExifSource.AutoAstro)
            If oExif Is Nothing Then oExif = GetExifOfType(Vblib.ExifSource.AutoMoon)
            If oExif Is Nothing Then oExif = GetExifOfType(Vblib.ExifSource.AutoVisCrosWeather)

            If oExif Is Nothing Then
                If Not query.astro.AlsoEmpty Then Return False
            Else
                Dim dFaza As Double = oExif.PogodaAstro.day.moonphase
                If Not query.astro.Moon00 Then If Math.Abs(dFaza) < 10 Then Return False
                If Not query.astro.MoonD25 Then If dFaza.Between(10, 35) Then Return False
                If Not query.astro.MoonD50 Then If dFaza.Between(35, 65) Then Return False
                If Not query.astro.MoonD75 Then If dFaza.Between(65, 90) Then Return False
                If Not query.astro.Moon100 Then If Math.Abs(dFaza) > 90 Then Return False
                If Not query.astro.MoonC75 Then If dFaza.Between(-90, -65) Then Return False
                If Not query.astro.MoonC50 Then If dFaza.Between(-65, -35) Then Return False
                If Not query.astro.MoonC25 Then If dFaza.Between(-35, -10) Then Return False
            End If
        End If

        If query.astro.SunHourMinCheck OrElse query.astro.SunHourMaxCheck Then
            oExif = GetExifOfType(Vblib.ExifSource.AutoAstro)
            If oExif Is Nothing Then oExif = GetExifOfType(Vblib.ExifSource.AutoVisCrosWeather)
            If oExif Is Nothing Then
                If Not query.astro.AlsoEmpty Then Return False
            Else

                If query.astro.SunHourMinCheck Then
                    If query.astro.SunHourMinValue > oExif.PogodaAstro.day.sunhour Then Return False
                End If

                If query.astro.SunHourMaxCheck Then
                    If query.astro.SunHourMaxValue < oExif.PogodaAstro.day.sunhour Then Return False
                End If

            End If
        End If


#End Region

#Region "rozpoznawanie twarzy"

        If query.faces.MinCheck OrElse query.faces.MaxCheck Then
            Dim iFaces As Integer = -1

            oExif = GetExifOfType(Vblib.ExifSource.AutoAzure)
            If oExif?.AzureAnalysis IsNot Nothing Then
                ' wedle Azure
                If oExif.AzureAnalysis.Faces IsNot Nothing Then
                    iFaces = oExif.AzureAnalysis.Faces.GetList.Count
                Else
                    iFaces = 0
                End If
            End If

            If iFaces < 0 Then
                oExif = GetExifOfType(Vblib.ExifSource.AutoWinFace)
                If oExif IsNot Nothing Then
                    If oExif.Keywords.StartsWith("-f") Then
                        iFaces = oExif.Keywords.Substring(2)
                    End If
                End If
            End If

            If iFaces > -1 Then
                If query.faces.MinCheck Then If iFaces < query.faces.MinValue Then Return False
                If query.faces.MaxCheck Then If iFaces > query.faces.MaxValue Then Return False
            End If

        End If


#End Region

#Region "azure"
        oExif = GetExifOfType(Vblib.ExifSource.AutoAzure)
        If oExif?.AzureAnalysis Is Nothing Then
            If Not query.Azure.AlsoEmpty Then Return False
        Else
            Dim sTextDump As String = oExif.AzureAnalysis.ToUserComment
            If Not CheckFieldsTxtValue(sTextDump, query.Azure.FldTxt) Then Return False

            ' wspóne - tekst w paru miejscach: Descriptions,  Folder, Filename, OCR, Azure description
            If Not String.IsNullOrEmpty(query.ogolne.Gdziekolwiek) Then
                If Not CheckStringMasksNegative(sTextDump, query.ogolne.Gdziekolwiek) Then Return False
                If CheckStringMasks(sTextDump, query.ogolne.Gdziekolwiek) Then bGdziekolwiekMatch = True
            End If

        End If
#End Region


#Region "pogoda"


#Region "Visual Cross"
        oExif = GetExifOfType(Vblib.ExifSource.AutoVisCrosWeather)
        If oExif?.PogodaAstro Is Nothing Then
            If Not query.VCross.AlsoEmpty Then Return False
        Else
            Dim sTextDump As String = oExif.PogodaAstro.DumpAsJSON
            If Not CheckFieldsTxtValue(sTextDump, query.VCross.FldTxt) Then Return False
            If Not CheckFieldsNumValue(sTextDump, query.VCross.FldNum) Then Return False
        End If

#End Region

#Region "Opad"
        oExif = GetExifOfType(Vblib.ExifSource.AutoMeteoOpad)
        If oExif?.MeteoOpad Is Nothing Then
            If Not query.ImgwOpad.AlsoEmpty Then Return False
        Else
            Dim sTextDump As String = oExif.MeteoOpad.DumpAsJSON
            If Not CheckFieldsTxtValue(sTextDump, query.ImgwOpad.FldTxt) Then Return False
            If Not CheckFieldsNumValue(sTextDump, query.ImgwOpad.FldNum) Then Return False
        End If

#End Region

#Region "Klimat"
        oExif = GetExifOfType(Vblib.ExifSource.AutoMeteoKlimat)
        If oExif?.MeteoKlimat Is Nothing Then
            If Not query.ImgwKlimat.AlsoEmpty Then Return False
        Else
            Dim sTextDump As String = oExif.MeteoKlimat.DumpAsJSON
            If Not CheckFieldsTxtValue(sTextDump, query.ImgwKlimat.FldTxt) Then Return False
            If Not CheckFieldsNumValue(sTextDump, query.ImgwKlimat.FldNum) Then Return False
        End If

#End Region


#End Region

        If Not String.IsNullOrEmpty(query.ogolne.Gdziekolwiek) Then
            If Not bGdziekolwiekMatch Then Return False
        End If

        Return True
    End Function

    Private Shared Function CheckFieldsTxtValue(textDump As String, fields As QueryPolaTxt4) As Boolean
        If Not CheckFieldValue(textDump, fields.p0) Then Return False
        If Not CheckFieldValue(textDump, fields.p1) Then Return False
        If Not CheckFieldValue(textDump, fields.p2) Then Return False
        If Not CheckFieldValue(textDump, fields.p3) Then Return False

        Return True
    End Function

    Private Shared Function CheckFieldValue(textDump As String, field As QueryPoleTxt) As Boolean
        If String.IsNullOrWhiteSpace(field.Value) Then Return True
        If String.IsNullOrWhiteSpace(field.Name) Then Return CheckStringMasks(textDump, field.Value)

        Dim aDump As String() = textDump.ToLowerInvariant.Split(vbCr)

        Dim bInDay As Boolean = False
        Dim bInCurrent As Boolean = False

        Dim bWantDay As Boolean = False 'fieldName.Substring(0, 2) = "d."
        Dim bWantCurr As Boolean = True ' fieldName.Substring(0, 2) = "c."
        If field.Name.Substring(1, 1) = "." Then
            bWantDay = (field.Name.Substring(0, 2) = "d.")
            bWantCurr = (field.Name.Substring(0, 2) = "c.")
            field.Name = field.Name.Substring(2)
        End If

        For Each sDumpLine As String In aDump

            If sDumpLine.Contains("""day"": {") Then
                bInDay = True
                Continue For
            End If
            If sDumpLine.Contains("""currentConditions"": {") Then
                bInCurrent = True
                Continue For
            End If
            If bInCurrent AndAlso Not bWantCurr Then Continue For
            If bInDay AndAlso Not bWantDay Then Continue For

            Dim iInd As Integer = sDumpLine.IndexOf(":")
            If iInd < 1 Then Continue For

            If Not sDumpLine.Substring(0, iInd).ContainsCI(field.Name) Then Continue For

            Return CheckStringMasks(sDumpLine.Substring(iInd + 1), field.Value)
        Next

        Return True

    End Function

    Private Shared Function CheckFieldsNumValue(textDump As String, fields As QueryPolaNum4) As Boolean
        If Not CheckFieldValueMinMax(textDump, fields.p0) Then Return False
        If Not CheckFieldValueMinMax(textDump, fields.p1) Then Return False
        If Not CheckFieldValueMinMax(textDump, fields.p2) Then Return False
        If Not CheckFieldValueMinMax(textDump, fields.p3) Then Return False

        Return True
    End Function


    Private Shared Function CheckFieldValueMinMax(textDump As String, field As QueryPoleNum) As Boolean

        If String.IsNullOrWhiteSpace(field.Name) Then Return True

        Dim bInDay As Boolean = False
        Dim bInCurrent As Boolean = False

        Dim bWantDay As Boolean = False 'fieldName.Substring(0, 2) = "d."
        Dim bWantCurr As Boolean = True ' fieldName.Substring(0, 2) = "c."
        If field.Name.Substring(1, 1) = "." Then
            bWantDay = (field.Name.Substring(0, 2) = "d.")
            bWantCurr = (field.Name.Substring(0, 2) = "c.")
            field.Name = field.Name.Substring(2)
        End If

        Dim dMinVal As Double = Double.MinValue
        If Not String.IsNullOrWhiteSpace(field.Min) Then dMinVal = field.Min
        Dim dMaxValue As Double = Double.MaxValue
        If Not String.IsNullOrWhiteSpace(field.Max) Then dMaxValue = field.Max

        Dim aDump As String() = textDump.ToLowerInvariant.Split(vbCr)
        field.Name = field.Name.ToLowerInvariant

        For Each sDumpLine As String In aDump
            If sDumpLine.Contains("""day"": {") Then
                bInDay = True
                Continue For
            End If
            If sDumpLine.Contains("""currentConditions"": {") Then
                bInCurrent = True
                Continue For
            End If
            If bInCurrent AndAlso Not bWantCurr Then Continue For
            If bInDay AndAlso Not bWantDay Then Continue For

            Dim iInd As Integer = sDumpLine.IndexOf(":")
            If iInd < 1 Then Continue For
            If sDumpLine.Substring(0, iInd).Replace("""", "").Trim <> field.Name Then Continue For

            Try
                Dim dCurrValue As Double = sDumpLine.Substring(iInd + 1).Replace(",", "").Trim

                If dCurrValue < dMinVal Then Return False
                Return dCurrValue < dMaxValue
            Catch ex As Exception
                ' jakby konwersje na double nie wyszły
            End Try

        Next

        Return True

    End Function


    ''' <summary>
    ''' true/false, jeśli spełnione są "fragments, prefixed with ! for negate"; TRUE gdy maski są empty; Case insensitive
    ''' </summary>
    Private Shared Function CheckStringMasks(sFromPicture As String, sMaskiWord As String) As Boolean
        If String.IsNullOrWhiteSpace(sMaskiWord) Then Return True

        sFromPicture = If(sFromPicture?.ToLowerInvariant, "")

        For Each maska As String In sMaskiWord.Split(" ")
            Dim temp As String = maska.ToLowerInvariant
            If temp.StartsWith("!") Then
                If sFromPicture.Contains(temp.Substring(1)) Then Return False
            Else
                If Not sFromPicture.Contains(temp) Then Return False
            End If
        Next

        Return True
    End Function

    ''' <summary>
    ''' false gdy picture zawiera "fragments prefixed with !", wszędzie indziej TRUE, case insensitive
    ''' </summary>
    Private Shared Function CheckStringMasksNegative(sFromPicture As String, sMaskiWord As String) As Boolean
        If String.IsNullOrWhiteSpace(sMaskiWord) Then Return True
        If String.IsNullOrWhiteSpace(sFromPicture) Then Return True

        sFromPicture = If(sFromPicture?.ToLowerInvariant, "")

        For Each maska As String In sMaskiWord.Split(" ")
            Dim temp As String = maska.ToLowerInvariant
            If temp.StartsWith("!") Then
                If sFromPicture.Contains(temp.Substring(1)) Then Return False
            End If
        Next

        Return True
    End Function


    ''' <summary>
    ''' true/false, jeśli jest contains, lub sMask empty. Case independent
    ''' </summary>
    Private Shared Function CheckStringContains(sFromPicture As String, sMaska As String) As Boolean
        If String.IsNullOrWhiteSpace(sFromPicture) Then Return True
        Return sFromPicture.ContainsCI(sMaska)
    End Function


#End Region

#Region "operacje na pliku"
    Public Function FileCopyTo(newPathname As String) As Boolean
        If File.Exists(newPathname) Then Return False
        IO.File.Copy(InBufferPathName, newPathname)
        Return True
    End Function

    Public Function FileCopyTo(targetDir As String, newname As String) As Boolean
        Return FileCopyTo(IO.Path.Combine(targetDir, newname))
    End Function

    ''' <summary>
    ''' kopiuj do podanego katalogu, używając suggestedfilename lub inbufferfilename
    ''' </summary>
    ''' <param name="newDirname">pathname targer katalogu</param>
    ''' <param name="bInBuffName">TRUE: użyj nazwy pliku z InBufferPathName, FALSE: użyj sSuggestedFilename (może się powtórzyć!)</param>
    ''' <returns>TRUE: ok, FALSE: target file already exists</returns>
    Public Function FileCopyToDir(newDirname As String, bInBuffName As Boolean) As Boolean
        If bInBuffName Then
            Return FileCopyTo(newDirname, GetInBuffName)
        Else
            Return FileCopyTo(newDirname, sSuggestedFilename)
        End If
    End Function

    ''' <summary>
    ''' nazwa pliku w buforze (bez ścieżki)
    ''' </summary>
    Public Function GetInBuffName() As String
        Return IO.Path.GetFileName(InBufferPathName)
    End Function


    ''' <summary>
    ''' usuwa wszystkie pliki tymczasowe i utworzone przez program (thumb, bak, firstFrame...), NIE kasuje samego zdjęcia!
    ''' </summary>
    Public Sub DeleteAllTempFiles()

        Dim folder As String = IO.Path.GetDirectoryName(InBufferPathName)
        Dim mask As String = IO.Path.GetFileName(InBufferPathName) & ".*"    ' musi być coś po nazwie pliku - więc samego zdjęcia NIE skasuje

        ' bez tego crash? może że zmieniona collection w trakcie
        Dim doUsuniecia As String() = IO.Directory.GetFiles(folder, mask)

        For Each plik As String In doUsuniecia
            ' niestety, MASK.* daje też MASK
            If plik = InBufferPathName Then Continue For
            IO.File.Delete(plik)
        Next
    End Sub


#Region "operacje na extensions"

    Public Function IsPic() As Boolean
        Return MatchesMasks(ExtsPic)
    End Function

    Public Function IsMovie() As Boolean
        Return MatchesMasks(ExtsMovie)
    End Function

    Public Function IsStereo() As Boolean
        Return MatchesMasks(ExtsStereo)
    End Function

#End Region

    ''' <summary>
    ''' daje stream albo bezpośrednio z pliku, albo po wyborze z paczki (NAR/ZIP)
    ''' </summary>
    ''' <returns></returns>
    Public Function SinglePicFromMulti(Optional bPreferAnaglyph As Boolean = False) As Stream
        If IO.Path.GetExtension(InBufferPathName).EqualsCI(".nar") Then
            Return SinglePicFromNar()
        ElseIf InBufferPathName.EndsWithCI(".stereo.zip") Then
            Return SinglePicFromZip(bPreferAnaglyph)
        Else
            Return IO.File.OpenRead(InBufferPathName)
        End If
    End Function

    Private Function SinglePicFromNar() As Stream
        Vblib.DumpCurrMethod()
        If Not IO.Path.GetExtension(InBufferPathName).EqualsCI(".nar") Then Return Nothing

        Using oArchive = IO.Compression.ZipFile.OpenRead(InBufferPathName)
            For Each oInArch As IO.Compression.ZipArchiveEntry In oArchive.Entries
                If Not IO.Path.GetExtension(oInArch.Name).EqualsCI(".jpg") Then Continue For
                Return SingePicFromZipEntry(oInArch)
            Next
        End Using

        Return Nothing
    End Function

    ''' <summary>
    ''' Daje MemoryStream z kopią wybranego pliku ze środka (zwykle: pierwszy JPG)
    ''' </summary>
    ''' <param name="bPreferAnaglyph">Gdy TRUE, i uiStereoBigAnaglyph, to daje ze środka (jeśli istnieje) anaglyph* </param>
    ''' <returns></returns>
    Private Function SinglePicFromZip(bPreferAnaglyph As Boolean) As Stream
        ' od SinglePicFromNar odróżnia się pomijaniem plików anaglyph*, ale może kiedyś NAR by wybierał zdefiniowany plik a nie pierwszy lepszy
        Vblib.DumpCurrMethod()
        If Not InBufferPathName.EndsWithCI(".stereo.zip") Then Return Nothing

        If bPreferAnaglyph Then
            Using oArchive = IO.Compression.ZipFile.OpenRead(InBufferPathName)
                For Each oInArch As IO.Compression.ZipArchiveEntry In oArchive.Entries
                    If Not IO.Path.GetExtension(oInArch.Name).EqualsCI(".jpg") Then Continue For
                    If Not oInArch.Name.ContainsCI("stereo.jpg") Then Continue For
                    Return SingePicFromZipEntry(oInArch)
                Next
            End Using
        End If

        Using oArchive = IO.Compression.ZipFile.OpenRead(InBufferPathName)
            For Each oInArch As IO.Compression.ZipArchiveEntry In oArchive.Entries
                If Not IO.Path.GetExtension(oInArch.Name).EqualsCI(".jpg") Then Continue For
                If oInArch.Name.ContainsCI("stereo.jpg") Then Continue For
                Return SingePicFromZipEntry(oInArch)
            Next
        End Using

        Return Nothing
    End Function

    Private Function SingePicFromZipEntry(oInArch As IO.Compression.ZipArchiveEntry) As Stream
        Dim memStream As New MemoryStream
        oInArch.Open.CopyTo(memStream)
        memStream.Flush()
        memStream.Seek(0, SeekOrigin.Begin)

        Return memStream
    End Function

#End Region




End Class

'Public Class BasicGeoposWithRadius
'    Inherits pkar.BasicGeopos

'    Public Property iRadius As Double

'    Public Sub New(geopos As pkar.BasicGeopos, bZgrubne As Boolean)
'        MyBase.New(geopos.Latitude, geopos.Longitude)
'        iRadius = If(bZgrubne, 20000, 100)
'    End Sub
'End Class
Public Class OneDescription
    Inherits BaseStruct

    Public Property data As String
    Public Property comment As String
    Public Property keywords As String
    ''' <summary>
    ''' guid prefiksowany L:, S:
    ''' </summary>
    Public Property PeerGuid As String

    Public Sub New(sData As String, sComment As String, sKeywords As String)
        data = sData
        comment = sComment
        keywords = sKeywords
    End Sub

    ''' <summary>
    ''' Stworzenie OneDescription dla dzisiejszej daty
    ''' </summary>
    <JsonConstructor>
    Public Sub New(sComment As String, sKeywords As String)
        data = Date.Now.ToString("yyyy.MM.dd HH:mm")
        comment = sComment
        keywords = sKeywords
    End Sub
End Class


Public Class OneLink
    Inherits BaseStruct

    Public Property opis As String = ""
    Public Property link As String = ""
End Class


Imports System.IO
Imports System.Runtime.Serialization
Imports System.Runtime.Serialization.Formatters.Binary
Imports System.Security.Policy
Imports System.Windows.Controls.Primitives
Imports System.Windows.Input.Manipulations
Imports FFMpegCore.Enums
Imports MetadataExtractor.Formats
Imports Org.BouncyCastle.Crmf
Imports Org.BouncyCastle.Crypto.Engines
Imports pkar
Imports Vblib
Imports Windows.Storage.Streams
Imports Windows.UI.Core
Imports vb14 = Vblib.pkarlibmodule14

Public Class SearchWindow

    Private Shared _fullArchive As BaseList(Of Vblib.OnePic) ' pełny plik archiwum, do wyszukiwania
    Private _inputList As List(Of Vblib.OnePic) ' aktualnie używany na wejściu
    Private _queryResults As List(Of Vblib.OnePic) ' wynik szukania
    'Private _geoTag As BasicGeopos
    Private _initialCount As Integer

    Private _query As New SearchQuery

    Public Sub New(Optional lista As List(Of Vblib.OnePic) = Nothing)
        ' This call is required by the designer.
        InitializeComponent()

        If lista Is Nothing Then
            ReadWholeArchive()
            _inputList = Nothing
        Else
            _inputList = lista
        End If

    End Sub

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        EditExifTag.WypelnComboDeviceType(uiComboDevType, Vblib.FileSourceDeviceTypeEnum.unknown)
        uiComboDevType.SelectedIndex = 0
        WypelnComboSourceNames()

        uiResultsCount.Text = $"(no query, total {_fullArchive.Count} items)"

        uiKwerenda.DataContext = _query

    End Sub

    Private Sub WypelnComboSourceNames()
        uiComboSource.Items.Clear()

        uiComboSource.Items.Add("")

        For Each oSource In Application.GetSourcesList.GetList
            uiComboSource.Items.Add(oSource.SourceName)
        Next

    End Sub

    Private Sub ReadWholeArchive()
        If _fullArchive IsNot Nothing Then Return

        Application.ShowWait(True)

        Dim sDataFolder As String = Application.GetDataFolder
        Dim sBinFile As String = IO.Path.Combine(sDataFolder, "archIndexFull.bin")
        Dim sTxtFile As String = IO.Path.Combine(sDataFolder, "archIndexFull.json")

        If IO.File.Exists(sBinFile) AndAlso (New IO.FileInfo(sBinFile)).Length > 100 AndAlso IO.File.GetLastWriteTime(sBinFile) > IO.File.GetLastWriteTime(sTxtFile) Then
            ' plik BIN jest nowszy, więc bierzemy go
            Dim fs As New FileStream(sBinFile, FileMode.Open)
            Dim formatter As New BinaryFormatter
            _fullArchive = DirectCast(formatter.Deserialize(fs), BaseList(Of Vblib.OnePic))
        Else
            ' mamy nowszy JSON, to musimy wczytać JSON i go zapisać
            _fullArchive = New BaseList(Of Vblib.OnePic)(Application.GetDataFolder, "archIndexFull.json") ' "archIndex.flat.json"
            _fullArchive.Load()

            ' bin zapis
#If False Then
            ' nie serializujemy, bo "is not marked as serializable" - ale to znaczy że nigdy do tego powyżej IF nie wejdzie, zawsze będzie ELSE
            Dim fs As New FileStream(sBinFile, FileMode.Create)
            Dim formatter As New BinaryFormatter()
            formatter.Serialize(fs, _fullArchive)
#End If
        End If

        _initialCount = _fullArchive.Count

        Application.ShowWait(False)
        ' potem: new ProcessBrowse.New(bufor As Vblib.IBufor, onlyBrowse As Boolean)

        If _initialCount < 1 Then
            vb14.DialogBox("Coś dziwnego, bo jakoby pusty indeks był?")
        End If

    End Sub


    ' Query 1:
    ' „pokaż zdjęcia, na których jestem ja i babcia Z, nie ma babci S;
    ' zdjęcie jest zrobione na zewnątrz,
    ' jest jakaś woda (typu jezioro, rzeka),
    ' widać samochód, ale nie ma roweru,
    ' i jest dużo żółtego (kwiaty);
    ' wykonane między 2010 a 2015 rokiem,
    ' latem,
    ' i w Suchej Beskidzkiej,
    ' gdy temperatura nie przekraczała 18 °C,
    ' między godziną 10:15 a 12:30,
    ' wiał dość silny wiatr północny,
    ' blisko pełni Księżyca”.
    '
    ' Query 2:
    ' „znajdź zdjęcia z parady samochodów zabytkowych, na których to paradach byłem z A, ale bez B”


    Private Function Szukaj(lista As List(Of Vblib.OnePic), query As SearchQuery) As Integer

        _queryResults = New List(Of Vblib.OnePic)
        Dim iCount As Integer = 0

        Application.ShowWait(True)
        For Each oPicek As Vblib.OnePic In lista
            If oPicek Is Nothing Then Continue For  ' a cóż to za dziwny case, że jest NULL?
            If Not CheckIfOnePicMatches(oPicek, query) Then Continue For

            _queryResults.Add(oPicek)
            iCount += 1
        Next
        Application.ShowWait(False)

        Return iCount
    End Function


    Private Shared Function CheckIfOnePicMatches(oPicek As Vblib.OnePic, query As SearchQuery) As Boolean

        Dim oExif As Vblib.ExifTag
        Dim bGdziekolwiekMatch As Boolean = False

#Region "ogólne"
        If query.ogolne_MaxDate.IsDateValid Or query.ogolne_MinDate.IsDateValid Then

            Dim picMinDate, picMaxDate As Date

            oExif = oPicek.GetExifOfType(ExifSource.FileExif)
            Dim oExifDate As Vblib.ExifTag = oPicek.GetExifOfType(ExifSource.ManualDate)
            If oExifDate Is Nothing Then oExifDate = oExif

            ' jeśli istnieje data zrobienia zdjęcia, to ją bierzemy, jeśli nie - to zakres ze słów kluczowych itp.
            If oExifDate IsNot Nothing AndAlso oExif.FileSourceDeviceType = FileSourceDeviceTypeEnum.digital Then
                picMaxDate = oExifDate.DateTimeOriginal
                picMinDate = oExifDate.DateTimeOriginal
            Else
                picMinDate = oPicek.GetMinDate
                picMaxDate = oPicek.GetMaxDate
            End If

            If query.ogolne_IgnoreYear Then
                ' bierzemy daty oDir, i zmieniamy Year na taki jak w UI query
                picMinDate = picMinDate.AddYears(query.ogolne_MinDate.Year - picMinDate.Year)
                picMaxDate = picMaxDate.AddYears(query.ogolne_MaxDate.Year - picMaxDate.Year)
            End If

            If query.ogolne_MaxDate.IsDateValid Then
                If picMinDate > query.ogolne_MaxDate Then Return False
            End If

            If query.ogolne_MinDate.IsDateValid Then
                If picMaxDate < query.ogolne_MinDate Then Return False
            End If
        End If

        If Not CheckStringContains(oPicek.PicGuid, query.ogolne_GUID) Then Return False

        If Not String.IsNullOrWhiteSpace(query.ogolne_Tags) Then
            Dim kwrds As String = query.ogolne_Tags
            ' automatyczne wyłączenie =X, jeśli nie jest podane wprost
            If Not kwrds.Contains("=X") Then kwrds &= " !=X"
            If Not oPicek.MatchesKeywords(kwrds.Split(" ")) Then Return False
        End If

        Dim descripsy As String = oPicek.GetSumOfDescriptionsText & " " & oPicek.GetSumOfCommentText
        If Not CheckStringMasks(descripsy, query.ogolne_Descriptions) Then Return False

        ' wspóne - tekst w paru miejscach: Descriptions,  Folder, Filename, OCR, Azure description
        If Not String.IsNullOrEmpty(query.ogolne_Gdziekolwiek) Then
            If Not CheckStringMasksNegative(descripsy, query.ogolne_Gdziekolwiek) Then Return False
            If Not CheckStringMasksNegative(oPicek.TargetDir, query.ogolne_Gdziekolwiek) Then Return False
            If Not CheckStringMasksNegative(oPicek.sSuggestedFilename, query.ogolne_Gdziekolwiek) Then Return False

            If CheckStringMasks(descripsy, query.ogolne_Gdziekolwiek) Then bGdziekolwiekMatch = True
            If CheckStringMasks(oPicek.TargetDir, query.ogolne_Gdziekolwiek) Then bGdziekolwiekMatch = True
            If CheckStringMasks(oPicek.sSuggestedFilename, query.ogolne_Gdziekolwiek) Then bGdziekolwiekMatch = True
        End If

#Region "ogólne - advanced"

        If Not CheckStringMasks(oPicek.TargetDir, query.ogolne_adv_TargetDir) Then Return False
        If Not CheckStringContains(oPicek.sSourceName, query.ogolne_adv_Source) Then Return False

        If Not oPicek.MatchesMasks(query.ogolne_adv_Filename, "") Then Return False

        If Not query.ogolne_adv_TypePic Then
            If oPicek.MatchesMasks(OnePic.ExtsPic) Then Return False
        End If
        If Not query.ogolne_adv_TypeMovie Then
            If oPicek.MatchesMasks(OnePic.ExtsMovie) Then Return False
        End If
        ' If Not uiTypeOth.IsChecked Then

        If Not CheckStringMasks(oPicek.CloudArchived, query.ogolne_adv_CloudArchived) Then Return False

        If Not String.IsNullOrWhiteSpace(query.ogolne_adv_Published) Then
            Dim publishy As String = ""
            For Each item In oPicek.Published
                publishy = publishy & " " & item.Key
            Next

            If Not CheckStringMasks(publishy, query.ogolne_adv_Published) Then Return False

        End If

        ' *TODO* fileTypeDiscriminator ? As String = Nothing


#End Region

        If Not String.IsNullOrWhiteSpace(query.ogolne_geo_Name) Then
            Dim sGeoName As String = ""
            For Each oExif In oPicek.Exifs
                If Not String.IsNullOrWhiteSpace(oExif.GeoName) Then sGeoName = sGeoName & " " & oExif.GeoName
            Next

            If Not CheckStringMasks(sGeoName, query.ogolne_geo_Name) Then Return False
        End If

        If query.ogolne_geo_Location IsNot Nothing Then
            Dim geotag As BasicGeoposWithRadius = oPicek.GetGeoTag
            If geotag Is Nothing Then
                If Not query.ogolne_geo_AlsoEmpty Then Return False
            Else
                If Not geotag.IsInsideCircle(query.ogolne_geo_Location) Then Return False
            End If
        End If

        ' pomijamy: InBufferPathName, sInSourceID, TagsChanged, Archived, editHistory

#End Region


#Region "SourceDefault"

        oExif = oPicek.GetExifOfType(Vblib.ExifSource.SourceDefault)
        If oExif IsNot Nothing Then
            If Not CheckStringMasks(oExif.Author, query.source_author) Then Return False

            If query.source_type > -1 Then
                If oExif.FileSourceDeviceType <> query.source_type Then Return False
            End If
        End If


#End Region

#Region "AutoExif"
        oExif = oPicek.GetExifOfType(Vblib.ExifSource.FileExif)
        If oExif IsNot Nothing Then
            If Not CheckStringMasks(oExif.CameraModel, query.exif_camera) Then Return False
        End If

#End Region

        'Public Const AutoWinOCR As String = "AUTO_WINOCR"
        oExif = oPicek.GetExifOfType(Vblib.ExifSource.AutoWinOCR)
        If Not String.IsNullOrEmpty(query.ocr) Then
            If oExif Is Nothing Then Return False
            If Not CheckStringMasks(oExif.UserComment, query.ocr) Then Return False
        End If

        If Not String.IsNullOrEmpty(query.ogolne_Gdziekolwiek) Then
            ' wspóne - tekst w paru miejscach: Descriptions,  Folder, Filename, OCR, Azure description
            If Not CheckStringMasksNegative(oExif.UserComment, query.ogolne_Gdziekolwiek) Then Return False
            If CheckStringMasks(oExif.UserComment, query.ogolne_Gdziekolwiek) Then bGdziekolwiekMatch = True
        End If
#Region "astro"


        If query.astro_MoonCheck Then

            oExif = oPicek.GetExifOfType(Vblib.ExifSource.AutoAstro)
            If oExif Is Nothing Then oExif = oPicek.GetExifOfType(Vblib.ExifSource.AutoMoon)
            If oExif Is Nothing Then oExif = oPicek.GetExifOfType(Vblib.ExifSource.AutoVisCrosWeather)

            If oExif Is Nothing Then
                If Not query.astro_AlsoEmpty Then Return False
            Else
                Dim dFaza As Double = oExif.PogodaAstro.day.moonphase
                If Not query.astro_Moon00 Then If Math.Abs(dFaza) < 10 Then Return False
                If Not query.astro_MoonD25 Then If dFaza.Between(10, 35) Then Return False
                If Not query.astro_MoonD50 Then If dFaza.Between(35, 65) Then Return False
                If Not query.astro_MoonD75 Then If dFaza.Between(65, 90) Then Return False
                If Not query.astro_Moon100 Then If Math.Abs(dFaza) > 90 Then Return False
                If Not query.astro_MoonC75 Then If dFaza.Between(-90, -65) Then Return False
                If Not query.astro_MoonC50 Then If dFaza.Between(-65, -35) Then Return False
                If Not query.astro_MoonC25 Then If dFaza.Between(-35, -10) Then Return False
            End If
        End If

        If query.astro_SunHourMinCheck OrElse query.astro_SunHourMaxCheck Then
            oExif = oPicek.GetExifOfType(Vblib.ExifSource.AutoAstro)
            If oExif Is Nothing Then oExif = oPicek.GetExifOfType(Vblib.ExifSource.AutoVisCrosWeather)
            If oExif Is Nothing Then
                If Not query.astro_AlsoEmpty Then Return False
            Else

                If query.astro_SunHourMinCheck Then
                    If query.astro_SunHourMinValue > oExif.PogodaAstro.day.sunhour Then Return False
                End If

                If query.astro_SunHourMaxCheck Then
                    If query.astro_SunHourMaxValue < oExif.PogodaAstro.day.sunhour Then Return False
                End If

            End If
        End If


#End Region

#Region "rozpoznawanie twarzy"

        If query.faces_MinCheck OrElse query.faces_MaxCheck Then
            Dim iFaces As Integer = -1

            oExif = oPicek.GetExifOfType(Vblib.ExifSource.AutoAzure)
            If oExif?.AzureAnalysis IsNot Nothing Then
                ' wedle Azure
                If oExif.AzureAnalysis.Faces IsNot Nothing Then
                    iFaces = oExif.AzureAnalysis.Faces.GetList.Count
                Else
                    iFaces = 0
                End If
            End If

            If iFaces < 0 Then
                oExif = oPicek.GetExifOfType(Vblib.ExifSource.AutoWinFace)
                If oExif IsNot Nothing Then
                    If oExif.Keywords.StartsWith("-f") Then
                        iFaces = oExif.Keywords.Substring(2)
                    End If
                End If
            End If

            If iFaces > -1 Then
                If query.faces_MinCheck Then If iFaces < query.faces_MinValue Then Return False
                If query.faces_MaxCheck Then If iFaces > query.faces_MaxValue Then Return False
            End If

        End If


#End Region

#Region "azure"
        oExif = oPicek.GetExifOfType(Vblib.ExifSource.AutoAzure)
        If oExif?.AzureAnalysis Is Nothing Then
            If Not query.azure_AlsoEmpty Then Return False
        Else
            Dim sTextDump As String = oExif.AzureAnalysis.ToUserComment
            If Not CheckFieldsTxtValue(sTextDump, query.azure_Txt) Then Return False

            ' wspóne - tekst w paru miejscach: Descriptions,  Folder, Filename, OCR, Azure description
            If Not String.IsNullOrEmpty(query.ogolne_Gdziekolwiek) Then
                If Not CheckStringMasksNegative(sTextDump, query.ogolne_Gdziekolwiek) Then Return False
                If CheckStringMasks(sTextDump, query.ogolne_Gdziekolwiek) Then bGdziekolwiekMatch = True
            End If

        End If
#End Region


#Region "pogoda"


#Region "Visual Cross"
        oExif = oPicek.GetExifOfType(Vblib.ExifSource.AutoVisCrosWeather)
        If oExif?.PogodaAstro Is Nothing Then
            If Not query.pog_Vcross_AlsoEmpty Then Return False
        Else
            Dim sTextDump As String = oExif.PogodaAstro.DumpAsJSON
            If Not CheckFieldsTxtValue(sTextDump, query.pog_Vcross_Txt) Then Return False
            If Not CheckFieldsNumValue(sTextDump, query.pog_Vcross_Num) Then Return False
        End If

#End Region

#Region "Opad"
        oExif = oPicek.GetExifOfType(Vblib.ExifSource.AutoMeteoOpad)
        If oExif?.MeteoOpad Is Nothing Then
            If Not query.pog_ImgwOpad_AlsoEmpty Then Return False
        Else
            Dim sTextDump As String = oExif.MeteoOpad.DumpAsJSON
            If Not CheckFieldsTxtValue(sTextDump, query.pog_ImgwOpad_Txt) Then Return False
            If Not CheckFieldsNumValue(sTextDump, query.pog_ImgwOpad_Num) Then Return False
        End If

#End Region

#Region "Klimat"
        oExif = oPicek.GetExifOfType(Vblib.ExifSource.AutoMeteoKlimat)
        If oExif?.MeteoKlimat Is Nothing Then
            If Not query.pog_ImgwKlimat_AlsoEmpty Then Return False
        Else
            Dim sTextDump As String = oExif.MeteoKlimat.DumpAsJSON
            If Not CheckFieldsTxtValue(sTextDump, query.pog_ImgwKlimat_Txt) Then Return False
            If Not CheckFieldsNumValue(sTextDump, query.pog_ImgwKlimat_Num) Then Return False
        End If

#End Region


#End Region

        If Not String.IsNullOrEmpty(query.ogolne_Gdziekolwiek) Then
            If Not bGdziekolwiekMatch Then Return False
        End If


    End Function

    Private Shared Function CheckFieldsTxtValue(textDump As String, fields As PolaTxt4) As Boolean
        If Not CheckFieldValue(textDump, fields.p0) Then Return False
        If Not CheckFieldValue(textDump, fields.p1) Then Return False
        If Not CheckFieldValue(textDump, fields.p2) Then Return False
        If Not CheckFieldValue(textDump, fields.p3) Then Return False

        Return True
    End Function

    Private Shared Function CheckFieldValue(textDump As String, field As PoleTxt) As Boolean
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

            If Not sDumpLine.Substring(0, iInd).Contains(field.Name.ToLowerInvariant) Then Continue For

            Return CheckStringMasks(sDumpLine.Substring(iInd + 1), field.Value)
        Next

        Return True

    End Function

    Private Shared Function CheckFieldsNumValue(textDump As String, fields As PolaNum4) As Boolean
        If Not CheckFieldValueMinMax(textDump, fields.p0) Then Return False
        If Not CheckFieldValueMinMax(textDump, fields.p1) Then Return False
        If Not CheckFieldValueMinMax(textDump, fields.p2) Then Return False
        If Not CheckFieldValueMinMax(textDump, fields.p3) Then Return False

        Return True
    End Function


    Private Shared Function CheckFieldValueMinMax(textDump As String, field As PoleNum) As Boolean

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
    ''' true/false, jeśli spełnione są "fragments, prefixed with ! for negate"; TRUE gdy maski są empty
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
    ''' false gdy picture zawiera "fragments prefixed with !", wszędzie indziej TRUE
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
    ''' true/false, jeśli jest contains
    ''' </summary>
    Private Shared Function CheckStringContains(sFromPicture As String, sMaska As String) As Boolean
        If String.IsNullOrWhiteSpace(sMaska) Then Return True

        sFromPicture = If(sFromPicture?.ToLowerInvariant, "")

        Return sFromPicture.Contains(sMaska)
    End Function


    Private Async Sub uiSearch_Click(sender As Object, e As RoutedEventArgs)

        ' przeniesienie z UI do _query - większość się zrobi samo, ale daty - nie
        Dim query As SearchQuery = FromUiToQuery()

        ' robimy tak, bo chcemy update w UI oraz w _query; a Binding nie przeniesie przy zmianie od strony kodu
        If Not String.IsNullOrEmpty(query.ogolne_Tags) AndAlso query.ogolne_Tags.ToLowerInvariant = query.ogolne_Tags Then
            If Await vb14.DialogBoxYNAsync("Keywords ma tylko małe litery, czy zmienić na duże?") Then
                'uiTags.Text = uiTags.Text.ToUpper
                query.ogolne_Tags = query.ogolne_Tags.ToUpper
            End If
        End If

        Dim iCount As Integer
        If _inputList Is Nothing Then
            iCount = Szukaj(_fullArchive.GetList, query)
        Else
            iCount = Szukaj(_inputList, query)
        End If

        If iCount < 1 Then
            uiLista.ItemsSource = Nothing
            uiListaKatalogow.ItemsSource = Nothing
            uiResultsCount.Text = $"Nic nie znalazłem (w {_initialCount})."
            Return
        End If

        uiResultsCount.Text = $"Found {iCount} items (from {_initialCount})."
        If iCount > 1000 Then
            If Not Await vb14.DialogBoxYNAsync($"{iCount} to dużo elementów, pokazać listę mimo to?") Then Return
        End If



        ' pokazanie rezultatów
        uiLista.ItemsSource = From c In _queryResults

        ' oraz folderów
        Dim listaNazwFolderow As New List(Of String)
        For Each oPicek As Vblib.OnePic In _queryResults
            listaNazwFolderow.Add(oPicek.TargetDir)
        Next

        Dim listaFolderow As New List(Of Vblib.OneDir)
        For Each nazwa As String In From c In listaNazwFolderow Order By c Distinct
            Dim oFolder As Vblib.OneDir = Application.GetDirTree.GetDirFromTargetDir(nazwa)
            If oFolder IsNot Nothing Then listaFolderow.Add(oFolder)
        Next

        uiListaKatalogow.ItemsSource = listaFolderow
    End Sub

    ''' <summary>
    ''' przeniesienie danych z UI do struktury Query - to, co samo się nie przenosi
    ''' </summary>
    Private Function FromUiToQuery() As SearchQuery
        Dim query As SearchQuery = _query

        ' daty - UI ma NULL dla nie-selected, a my chcemy mieć wartości
        If Not uiMinDateCheck.IsChecked OrElse uiMaxDate.SelectedDate Is Nothing Then
            query.ogolne_MaxDate = Date.MaxValue
        Else
            query.ogolne_MaxDate = uiMaxDate.SelectedDate
            ' na północ PO, czyli razem z tym dniem
            query.ogolne_MaxDate = query.ogolne_MaxDate.AddDays(1)
        End If

        If Not uiMinDateCheck.IsChecked OrElse uiMinDate.SelectedDate Is Nothing Then
            query.ogolne_MinDate = Date.MinValue
        Else
            query.ogolne_MaxDate = uiMinDate.SelectedDate
        End If

        If query.ogolne_geo_Location IsNot Nothing Then
            ' przeliczając z km na metry
            query.ogolne_geo_Location.Radius = uiGeoRadius.Text * 1000
        End If

        query.ogolne_adv_Source = TryCast(uiComboSource.SelectedValue, String)?.ToLowerInvariant
        query.ogolne_adv_Filename = uiFilename.Text.ToLowerInvariant
        If String.IsNullOrWhiteSpace(query.ogolne_adv_Filename) Then query.ogolne_adv_Filename = "*"

        query.source_type = -1
        Dim sDevType As String = TryCast(uiComboDevType.SelectedValue, String)
        If Not String.IsNullOrWhiteSpace(sDevType) Then
            query.source_type = sDevType.Substring(0, 1)
        End If

        Return query

    End Function

    Private Sub uiGetGeo_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New EnterGeoTag
        If Not oWnd.ShowDialog Then Return
        _query.ogolne_geo_Location = oWnd.GetGeoPos
        uiLatLon.Text = $"szer. {_query.ogolne_geo_Location.StringLat(3)}, dług. {_query.ogolne_geo_Location.StringLon(3)}"
        uiGeoRadius.Text = If(oWnd.IsZgrubne, 20, 5)
    End Sub

    Private Sub uiSubSearch_Click(sender As Object, e As RoutedEventArgs)
        vb14.DialogBox("jeszcze nie umiem")
    End Sub

    Private Sub uiGoMiniaturki_Click(sender As Object, e As RoutedEventArgs)

        If _queryResults.Count < 1 Then Return

        Dim lista As New Vblib.BufferFromQuery()

        For Each oPic As Vblib.OnePic In _queryResults

            For Each oArch As VbLibCore3_picSource.LocalStorageMiddle In Application.GetArchivesList.GetList
                'vb14.DumpMessage($"trying archive {oArch.StorageName}")
                Dim sRealPath As String = oArch.GetRealPath(oPic.TargetDir, oPic.sSuggestedFilename)
                vb14.DumpMessage($"real path of index file: {sRealPath}")
                If Not String.IsNullOrWhiteSpace(sRealPath) Then
                    Dim oPicNew As Vblib.OnePic = oPic.Clone
                    oPic.InBufferPathName = sRealPath
                    lista.AddFile(oPic)
                    Exit For
                End If
            Next
        Next

        Dim oWnd As New ProcessBrowse(lista, True, "Found")
        oWnd.Show()
        Return


    End Sub

    Private Sub uiOpenBig_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oPic As Vblib.OnePic = oFE.DataContext
        If oPic Is Nothing Then Return

        If String.IsNullOrWhiteSpace(oPic.TargetDir) Then Return

        For Each oArch As VbLibCore3_picSource.LocalStorageMiddle In Application.GetArchivesList.GetList
            'vb14.DumpMessage($"trying archive {oArch.StorageName}")
            Dim sRealPath As String = oArch.GetRealPath(oPic.TargetDir, oPic.sSuggestedFilename)
            vb14.DumpMessage($"real path of index file: {sRealPath}")
            If Not String.IsNullOrWhiteSpace(sRealPath) Then
                Dim oPicNew As Vblib.OnePic = oPic.Clone
                oPic.InBufferPathName = sRealPath
                Dim oWnd As New ShowBig(oPic, True, False)
                oWnd.Show()
                Return
            End If
        Next
        vb14.DialogBox("nie mogę znaleźć pliku w żadnym archiwum")

    End Sub

    Private Sub uiOpenExif_Click(sender As Object, e As RoutedEventArgs)
        Dim oItem As FrameworkElement = sender
        Dim oPicek As Vblib.OnePic = oItem.DataContext

        Dim oWnd As New ShowExifs(False) '(oPicek.oDir)

        ' możemy potem w nim robić zmiany...
        oWnd.Owner = Me
        oWnd.DataContext = oPicek
        oWnd.Show()

    End Sub

    Private Sub RefreshOwnedWindows(oPic As Vblib.OnePic)
        For Each oWnd As Window In Me.OwnedWindows
            oWnd.DataContext = oPic
        Next
    End Sub

    Private Sub uiLista_SelChanged(sender As Object, e As SelectionChangedEventArgs)
        Dim ile As Integer = uiLista.SelectedItems.Count

        If ile = 1 Then
            Dim oItem As Vblib.OnePic = uiLista.SelectedItems(0)
            RefreshOwnedWindows(oItem)
        End If

    End Sub

    Private Sub uiCopyDateMinToMax(sender As Object, e As RoutedEventArgs)
        uiMaxDate.SelectedDate = uiMinDate.SelectedDate
        uiMaxDateCheck.IsChecked = uiMinDateCheck.IsChecked
    End Sub

    Private Sub uiOpenFolder_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oPic As Vblib.OnePic = oFE.DataContext
        If oPic Is Nothing Then Return

        If String.IsNullOrWhiteSpace(oPic.TargetDir) Then Return
        SettingsDirTree.OpenFolderInPicBrowser(oPic.TargetDir)
    End Sub

    Private Sub uiFoldersOpenFolder_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        Dim oDir As Vblib.OneDir = oFE.DataContext
        If oDir Is Nothing Then Return

        ' otwórz folder - ale z listy folderów
        SettingsDirTree.OpenFolderInPicBrowser(oDir.fullPath)
    End Sub
End Class


Public Class SearchQuery
    Inherits BaseStruct

    Public Property ogolne_MinDate As Date
    Public Property ogolne_MaxDate As Date
    Public Property ogolne_IgnoreYear As Boolean
    Public Property ogolne_GUID As String
    Public Property ogolne_Tags As String
    Public Property ogolne_Descriptions As String
    Public Property ogolne_Gdziekolwiek As String

    Public Property ogolne_geo_AlsoEmpty As Boolean = True
    Public Property ogolne_geo_Location As BasicGeoposWithRadius
    Public Property ogolne_geo_Name As String

    Public Property ogolne_adv_Source As String
    Public Property ogolne_adv_TargetDir As String
    Public Property ogolne_adv_Filename As String
    Public Property ogolne_adv_Published As String
    Public Property ogolne_adv_CloudArchived As String
    Public Property ogolne_adv_TypePic As Boolean = True
    Public Property ogolne_adv_TypeMovie As Boolean = True

    Public Property source_type As Integer
    Public Property source_author As String

    Public Property exif_camera As String

    Public Property ocr As String

    Public Property astro_AlsoEmpty As Boolean = True
    Public Property astro_MoonCheck As Boolean
    Public Property astro_Moon00 As Boolean = True
    Public Property astro_MoonD25 As Boolean = True
    Public Property astro_MoonD50 As Boolean = True
    Public Property astro_MoonD75 As Boolean = True
    Public Property astro_Moon100 As Boolean = True
    Public Property astro_MoonC75 As Boolean = True
    Public Property astro_MoonC50 As Boolean = True
    Public Property astro_MoonC25 As Boolean = True

    Public Property astro_SunHourMinCheck As Boolean
    Public Property astro_SunHourMinValue As Integer
    Public Property astro_SunHourMaxCheck As Boolean
    Public Property astro_SunHourMaxValue As Integer


    Public Property faces_MinCheck As Boolean
    Public Property faces_MinValue As Integer
    Public Property faces_MaxCheck As Boolean
    Public Property faces_MaxValue As Integer


    Public Property azure_AlsoEmpty As Boolean = True
    Public Property azure_Txt As New PolaTxt4

    Public Property pog_Vcross_AlsoEmpty As Boolean = True
    Public Property pog_Vcross_Txt As New PolaTxt4
    Public Property pog_Vcross_Num As New PolaNum4


    Public Property pog_ImgwOpad_AlsoEmpty As Boolean = True
    Public Property pog_ImgwOpad_Txt As New PolaTxt4
    Public Property pog_ImgwOpad_Num As New PolaNum4


    Public Property pog_ImgwKlimat_AlsoEmpty As Boolean = True
    Public Property pog_ImgwKlimat_Txt As New PolaTxt4
    Public Property pog_ImgwKlimat_Num As New PolaNum4


    Public Property fullDirs As Boolean

End Class

Public Class PolaTxt4
    Inherits BaseStruct

    Public Property p0 As New PoleTxt
    Public Property p1 As New PoleTxt
    Public Property p2 As New PoleTxt
    Public Property p3 As New PoleTxt
End Class

Public Class PoleTxt
    Inherits BaseStruct

    Public Property Name As String
    Public Property Value As String
End Class

Public Class PolaNum4
    Inherits BaseStruct

    Public Property p0 As New PoleNum
    Public Property p1 As New PoleNum
    Public Property p2 As New PoleNum
    Public Property p3 As New PoleNum

End Class

Public Class PoleNum
    Inherits BaseStruct

    Public Property Name As String
    Public Property Min As String
    Public Property Max As String
End Class
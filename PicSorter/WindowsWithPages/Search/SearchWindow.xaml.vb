

Imports System.IO
Imports System.Runtime.InteropServices.WindowsRuntime
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
    Private _inputList As IEnumerable(Of Vblib.OnePic) ' aktualnie używany na wejściu
    Private _queryResults As IEnumerable(Of Vblib.OnePic) ' wynik szukania
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
        'WypelnComboSourceNames()

        uiResultsCount.Text = $"(no query, total {_initialCount} items)"

        uiKwerenda.DataContext = _query
        'AddHandler uiKwerenda.Szukajmy, AddressOf uiSearch_Click

    End Sub



    Private Sub ReadWholeArchiveOld()
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

    Private Sub ReadWholeArchive()

        If Application.gDbase.IsLoaded Then Return

        Application.ShowWait(True)

        Application.gDbase.Load()
        _initialCount = Application.gDbase.Count

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


    Private Function Szukaj(lista As IEnumerable(Of Vblib.OnePic), query As SearchQuery) As Integer

        _queryResults = New List(Of Vblib.OnePic)
        Dim iCount As Integer = 0

        Application.ShowWait(True)
        'For Each oPicek As Vblib.OnePic In lista
        '    If Not CheckIfOnePicMatches(oPicek, query) Then Continue For

        '    _queryResults.Add(oPicek)
        '    iCount += 1
        'Next

        If lista Is Nothing Then
            ' po pełnym
            _queryResults = Application.gDbase.Search(query)
        Else
            ' po już ograniczonym
            _queryResults = lista.Where(Function(x) x.CheckIfMatchesQuery(query))
        End If
        Application.ShowWait(False)

        Return _queryResults.Count
        'Return iCount
    End Function

#If False Then
    Public Shared Function CheckIfOnePicMatches(oPicek As Vblib.OnePic, query As SearchQuery) As Boolean

        If oPicek Is Nothing Then Return False ' a cóż to za dziwny case, że jest NULL? ale był!

        Dim oExif As Vblib.ExifTag
        Dim bGdziekolwiekMatch As Boolean = False

#Region "ogólne"
        If query.ogolne.MaxDate.IsDateValid Or query.ogolne.MinDate.IsDateValid Then

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

            If query.ogolne.IgnoreYear Then
                ' bierzemy daty oDir, i zmieniamy Year na taki jak w UI query
                picMinDate = picMinDate.AddYears(query.ogolne.MinDate.Year - picMinDate.Year)
                picMaxDate = picMaxDate.AddYears(query.ogolne.MaxDate.Year - picMaxDate.Year)
            End If

            If query.ogolne.MaxDate.IsDateValid Then
                If picMinDate > query.ogolne.MaxDate Then Return False
            End If

            If query.ogolne.MinDate.IsDateValid Then
                If picMaxDate < query.ogolne.MinDate Then Return False
            End If
        End If

        If Not CheckStringContains(oPicek.PicGuid, query.ogolne.GUID) Then Return False

        If Not String.IsNullOrWhiteSpace(query.ogolne.Tags) Then
            Dim kwrds As String = query.ogolne.Tags
            ' automatyczne wyłączenie =X, jeśli nie jest podane wprost
            If Not kwrds.Contains("=X") Then kwrds &= " !=X"
            If Not oPicek.MatchesKeywords(kwrds.Split(" ")) Then Return False
        End If

        Dim descripsy As String = oPicek.GetSumOfDescriptionsText & " " & oPicek.GetSumOfCommentText
        If Not CheckStringMasks(descripsy, query.ogolne.Descriptions) Then Return False

        ' wspóne - tekst w paru miejscach: Descriptions,  Folder, Filename, OCR, Azure description
        If Not String.IsNullOrEmpty(query.ogolne.Gdziekolwiek) Then
            If Not CheckStringMasksNegative(descripsy, query.ogolne.Gdziekolwiek) Then Return False
            If Not CheckStringMasksNegative(oPicek.TargetDir, query.ogolne.Gdziekolwiek) Then Return False
            If Not CheckStringMasksNegative(oPicek.sSuggestedFilename, query.ogolne.Gdziekolwiek) Then Return False

            If CheckStringMasks(descripsy, query.ogolne.Gdziekolwiek) Then bGdziekolwiekMatch = True
            If CheckStringMasks(oPicek.TargetDir, query.ogolne.Gdziekolwiek) Then bGdziekolwiekMatch = True
            If CheckStringMasks(oPicek.sSuggestedFilename, query.ogolne.Gdziekolwiek) Then bGdziekolwiekMatch = True
        End If

#Region "ogólne - advanced"

        If Not CheckStringMasks(oPicek.TargetDir, query.ogolne.adv.TargetDir) Then Return False
        If Not CheckStringContains(oPicek.sSourceName, query.ogolne.adv.Source) Then Return False

        If Not oPicek.MatchesMasks(query.ogolne.adv.Filename, "") Then Return False

        If Not query.ogolne.adv.TypePic Then
            If oPicek.MatchesMasks(OnePic.ExtsPic) Then Return False
        End If
        If Not query.ogolne.adv.TypeMovie Then
            If oPicek.MatchesMasks(OnePic.ExtsMovie) Then Return False
        End If
        ' If Not uiTypeOth.IsChecked Then

        If Not CheckStringMasks(oPicek.CloudArchived, query.ogolne.adv.CloudArchived) Then Return False

        If Not String.IsNullOrWhiteSpace(query.ogolne.adv.Published) Then
            Dim publishy As String = ""
            For Each item In oPicek.Published
                publishy = publishy & " " & item.Key
            Next

            If Not CheckStringMasks(publishy, query.ogolne.adv.Published) Then Return False

        End If

        ' *TODO* fileTypeDiscriminator ? As String = Nothing


#End Region

        If Not String.IsNullOrWhiteSpace(query.ogolne.geo.Name) Then
            Dim sGeoName As String = ""
            For Each oExif In oPicek.Exifs
                If Not String.IsNullOrWhiteSpace(oExif.GeoName) Then sGeoName = sGeoName & " " & oExif.GeoName
            Next

            If Not CheckStringMasks(sGeoName, query.ogolne.geo.Name) Then Return False
        End If

        If query.ogolne.geo.Location IsNot Nothing Then
            Dim geotag As BasicGeoposWithRadius = oPicek.GetGeoTag
            If geotag Is Nothing Then
                If Not query.ogolne.geo.AlsoEmpty Then Return False
            Else
                If Not geotag.IsInsideCircle(query.ogolne.geo.Location) Then Return False
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

            oExif = oPicek.GetExifOfType(Vblib.ExifSource.AutoAstro)
            If oExif Is Nothing Then oExif = oPicek.GetExifOfType(Vblib.ExifSource.AutoMoon)
            If oExif Is Nothing Then oExif = oPicek.GetExifOfType(Vblib.ExifSource.AutoVisCrosWeather)

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
            oExif = oPicek.GetExifOfType(Vblib.ExifSource.AutoAstro)
            If oExif Is Nothing Then oExif = oPicek.GetExifOfType(Vblib.ExifSource.AutoVisCrosWeather)
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
                If query.faces.MinCheck Then If iFaces < query.faces.MinValue Then Return False
                If query.faces.MaxCheck Then If iFaces > query.faces.MaxValue Then Return False
            End If

        End If


#End Region

#Region "azure"
        oExif = oPicek.GetExifOfType(Vblib.ExifSource.AutoAzure)
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
        oExif = oPicek.GetExifOfType(Vblib.ExifSource.AutoVisCrosWeather)
        If oExif?.PogodaAstro Is Nothing Then
            If Not query.VCross.AlsoEmpty Then Return False
        Else
            Dim sTextDump As String = oExif.PogodaAstro.DumpAsJSON
            If Not CheckFieldsTxtValue(sTextDump, query.VCross.FldTxt) Then Return False
            If Not CheckFieldsNumValue(sTextDump, query.VCross.FldNum) Then Return False
        End If

#End Region

#Region "Opad"
        oExif = oPicek.GetExifOfType(Vblib.ExifSource.AutoMeteoOpad)
        If oExif?.MeteoOpad Is Nothing Then
            If Not query.ImgwOpad.AlsoEmpty Then Return False
        Else
            Dim sTextDump As String = oExif.MeteoOpad.DumpAsJSON
            If Not CheckFieldsTxtValue(sTextDump, query.ImgwOpad.FldTxt) Then Return False
            If Not CheckFieldsNumValue(sTextDump, query.ImgwOpad.FldNum) Then Return False
        End If

#End Region

#Region "Klimat"
        oExif = oPicek.GetExifOfType(Vblib.ExifSource.AutoMeteoKlimat)
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

            If Not sDumpLine.Substring(0, iInd).Contains(field.Name.ToLowerInvariant) Then Continue For

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
#End If

    Private Async Sub uiSearch_Click(sender As Object, e As RoutedEventArgs)

        ' clickcli
        _query = Await uiKwerenda.QueryValidityCheck

        ' przeniesienie z UI do _query - większość się zrobi samo, ale daty - nie
        Dim iCount As Integer
        'If _inputList Is Nothing Then
        '    iCount = Szukaj(_fullArchive.GetList, _query)
        'Else
        iCount = Szukaj(_inputList, _query)
        'End If

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
        uiLista.ItemsSource = _queryResults 'From c In _queryResults

        ' oraz folderów
        Dim listaNazwFolderow As New List(Of String)
        For Each oPicek As Vblib.OnePic In _queryResults
            listaNazwFolderow.Add(oPicek.TargetDir)
        Next

        If listaNazwFolderow.Count > 0 Then
            Dim listaFolderow As New List(Of Vblib.OneDir)
            For Each nazwa As String In From c In listaNazwFolderow Order By c Distinct
                Dim oFolder As Vblib.OneDir = Application.GetDirTree.GetDirFromTargetDir(nazwa)
                If oFolder IsNot Nothing Then listaFolderow.Add(oFolder)
            Next

            uiListaKatalogow.ItemsSource = listaFolderow
        Else
            uiListaKatalogow.ItemsSource = Nothing
        End If

    End Sub


    ''' <summary>
    ''' przeniesienie danych z UI do struktury Query - to, co samo się nie przenosi
    ''' </summary>


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


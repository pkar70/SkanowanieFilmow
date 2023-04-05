

Imports System.IO
Imports System.Runtime.Serialization
Imports System.Runtime.Serialization.Formatters.Binary
Imports System.Security.Policy
Imports System.Windows.Controls.Primitives
Imports MetadataExtractor.Formats
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
    Private _geoTag As BasicGeopos
    Private _initialCount As Integer

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


    Private Function Szukaj(lista As List(Of Vblib.OnePic)) As Integer
        _queryResults = New List(Of Vblib.OnePic)

#Region "przygotowanie danych"
#Region "ogolne"

        Dim maxDate As Date
        If Not uiMinDateCheck.IsChecked OrElse uiMaxDate.SelectedDate Is Nothing Then
            maxDate = Date.MaxValue
        Else
            maxDate = uiMaxDate.SelectedDate
            ' na północ PO, czyli razem z tym dniem
            maxDate = maxDate.AddDays(1)
        End If

        Dim minDate As Date
        If Not uiMinDateCheck.IsChecked OrElse uiMinDate.SelectedDate Is Nothing Then
            minDate = Date.MinValue
        Else
            minDate = uiMinDate.SelectedDate
        End If

        'Dim targetDir As String = 
        Dim source As String = TryCast(uiComboSource.SelectedValue, String)?.ToLowerInvariant
        Dim dosMask As String = uiFilename.Text.ToLowerInvariant
        If String.IsNullOrWhiteSpace(dosMask) Then dosMask = "*"

        Dim iDistance As Integer = uiGeoRadius.Text ' inputscope: digits

#End Region
#End Region

        Dim iDeviceType As Integer = -1
        Dim sDevType As String = TryCast(uiComboDevType.SelectedValue, String)
        If Not String.IsNullOrWhiteSpace(sDevType) Then
            iDeviceType = sDevType.Substring(0, 1)
        End If

        Dim iCount As Integer = 0
        Dim oExif As Vblib.ExifTag

        Application.ShowWait(True)
        For Each oPicek As Vblib.OnePic In lista
            If oPicek Is Nothing Then Continue For  ' a cóż to za dziwny case, że jest NULL?

#Region "ogólne"

            If uiIgnoreYear.IsChecked Then
                ' bierzemy daty oDir, i zmieniamy Year na taki jak w UI query
                If maxDate.IsDateValid Then
                    Dim picMinDate As Date = oPicek.GetMinDate
                    picMinDate.AddYears(maxDate.Year - picMinDate.Year)
                    If picMinDate > maxDate Then Continue For
                End If

                If minDate.IsDateValid Then
                    Dim picMaxDate As Date = oPicek.GetMaxDate
                    picMaxDate.AddYears(minDate.Year - picMaxDate.Year)
                    If picMaxDate < minDate Then Continue For
                End If
            Else
                ' zwykłe porównanie
                If oPicek.GetMinDate > maxDate Then Continue For
                If oPicek.GetMaxDate < minDate Then Continue For
            End If

            If Not CheckStringContains(oPicek.PicGuid, uiGuid.Text) Then Continue For

            If Not String.IsNullOrWhiteSpace(uiTags.Text) Then
                If Not oPicek.MatchesKeywords(uiTags.Text.Split(" ")) Then Continue For
            End If

            Dim descripsy As String = oPicek.GetSumOfDescriptionsText & " " & oPicek.GetSumOfCommentText
            If Not CheckStringMasks(descripsy, uiDescriptions.Text) Then Continue For

#Region "ogólne - advanced"

            If Not CheckStringMasks(oPicek.TargetDir, uiTargetDir.Text) Then Continue For
            If Not CheckStringContains(oPicek.sSourceName, source) Then Continue For

            If Not oPicek.MatchesMasks(dosMask, "") Then Continue For

            If Not CheckStringMasks(oPicek.CloudArchived, uiCloudArchived.Text) Then Continue For

            If Not String.IsNullOrWhiteSpace(uiPublished.Text) Then
                Dim publishy As String = ""
                For Each item In oPicek.Published
                    publishy = publishy & " " & item.Key
                Next

                If Not CheckStringMasks(publishy, uiPublished.Text) Then Continue For

            End If

            ' *TODO* fileTypeDiscriminator ? As String = Nothing


#End Region

            If Not String.IsNullOrWhiteSpace(uiGeoName.Text) Then
                Dim sGeoName As String = ""
                For Each oExif In oPicek.Exifs
                    If Not String.IsNullOrWhiteSpace(oExif.GeoName) Then sGeoName = sGeoName & " " & oExif.GeoName
                Next

                If Not CheckStringMasks(sGeoName, uiGeoName.Text) Then Continue For
            End If


            If _geoTag IsNot Nothing Then
                Dim geotag As BasicGeoposWithRadius = oPicek.GetGeoTag
                If geotag Is Nothing Then
                    If Not uiGeoDefault.IsChecked Then Continue For
                Else
                    Dim maxRadius As Integer = iDistance * 1000 ' na metry
                    maxRadius += geotag.iRadius ' dodaj dokładność lokalizacji
                    If Not geotag.IsInsideCircle(_geoTag, maxRadius) Then Continue For
                End If
            End If

            ' pomijamy: InBufferPathName, sInSourceID, TagsChanged, Archived, editHistory

#End Region


#Region "SourceDefault"

            oExif = oPicek.GetExifOfType(Vblib.ExifSource.SourceDefault)
            If oExif IsNot Nothing Then
                If Not CheckStringMasks(oExif.Author, uiAuthor.Text) Then Continue For

                If iDeviceType > -1 Then
                    If oExif.FileSourceDeviceType <> iDeviceType Then Continue For
                End If
            End If



#End Region

#Region "AutoExif"
            oExif = oPicek.GetExifOfType(Vblib.ExifSource.FileExif)
            If oExif IsNot Nothing Then
                If Not CheckStringMasks(oExif.CameraModel, uiCamera.Text) Then Continue For
            End If

#End Region

            'Public Const AutoWinOCR As String = "AUTO_WINOCR"
            oExif = oPicek.GetExifOfType(Vblib.ExifSource.AutoWinOCR)
            If oExif IsNot Nothing Then
                If Not CheckStringMasks(oExif.UserComment, uiOCR.Text) Then Continue For
            End If

#Region "astro"


            If uiMoonPhase.IsChecked Then

                oExif = oPicek.GetExifOfType(Vblib.ExifSource.AutoAstro)
                If oExif Is Nothing Then oExif = oPicek.GetExifOfType(Vblib.ExifSource.AutoMoon)
                If oExif Is Nothing Then oExif = oPicek.GetExifOfType(Vblib.ExifSource.AutoVisCrosWeather)

                If oExif Is Nothing Then
                    If Not uiAstroDefault.IsChecked Then Continue For
                Else
                    Dim dFaza As Double = oExif.PogodaAstro.day.moonphase
                    If Not uiMoon00.IsChecked Then If Math.Abs(dFaza) < 10 Then Continue For
                    If Not uiMoonD25.IsChecked Then If dFaza.Between(10, 35) Then Continue For
                    If Not uiMoonD50.IsChecked Then If dFaza.Between(35, 65) Then Continue For
                    If Not uiMoonD75.IsChecked Then If dFaza.Between(65, 90) Then Continue For
                    If Not uiMoon100.IsChecked Then If Math.Abs(dFaza) > 90 Then Continue For
                    If Not uiMoonC75.IsChecked Then If dFaza.Between(-90, -65) Then Continue For
                    If Not uiMoonC50.IsChecked Then If dFaza.Between(-65, -35) Then Continue For
                    If Not uiMoonC25.IsChecked Then If dFaza.Between(-35, -10) Then Continue For
                End If
            End If

            If uiSunHourMin.IsChecked OrElse uiSunHourMax.IsChecked Then
                oExif = oPicek.GetExifOfType(Vblib.ExifSource.AutoAstro)
                If oExif Is Nothing Then oExif = oPicek.GetExifOfType(Vblib.ExifSource.AutoVisCrosWeather)
                If oExif Is Nothing Then
                    If Not uiAstroDefault.IsChecked Then Continue For
                Else

                    If uiSunHourMin.IsChecked Then
                        If uiSunHourMinSlider.Value > oExif.PogodaAstro.day.sunhour Then Continue For
                    End If

                    If uiSunHourMax.IsChecked Then
                        If uiSunHourMaxSlider.Value < oExif.PogodaAstro.day.sunhour Then Continue For
                    End If

                End If
            End If


#End Region

#Region "rozpoznawanie twarzy"

            If uiFacesMin.IsChecked OrElse uiFacesMax.IsChecked Then
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
                    If uiFacesMin.IsChecked Then If iFaces < uiFacesMinSlider.Value Then Continue For
                    If uiFacesMax.IsChecked Then If iFaces > uiFacesMaxSlider.Value Then Continue For
                End If

            End If


#End Region

#Region "azure"
            oExif = oPicek.GetExifOfType(Vblib.ExifSource.AutoAzure)
            If oExif?.AzureAnalysis Is Nothing Then
                If Not uiAzureDefault.IsChecked Then Continue For
            Else
                Dim sTextDump As String = oExif.AzureAnalysis.ToUserComment
                If Not CheckFieldValue(sTextDump, uiAzureField0.Text, uiAzureValue0.Text) Then Continue For
                If Not CheckFieldValue(sTextDump, uiAzureField1.Text, uiAzureValue1.Text) Then Continue For
                If Not CheckFieldValue(sTextDump, uiAzureField2.Text, uiAzureValue2.Text) Then Continue For
                If Not CheckFieldValue(sTextDump, uiAzureField3.Text, uiAzureValue3.Text) Then Continue For
            End If
#End Region


#Region "pogoda"


#Region "Visual Cross"
            oExif = oPicek.GetExifOfType(Vblib.ExifSource.AutoVisCrosWeather)
            If oExif?.PogodaAstro Is Nothing Then
                If Not uiPogodaDefault.IsChecked Then Continue For
            Else
                Dim sTextDump As String = oExif.PogodaAstro.DumpAsJSON
                If Not CheckFieldValue(sTextDump, uiPogodaField0.Text, uiPogodaValue0.Text) Then Continue For
                If Not CheckFieldValue(sTextDump, uiPogodaField1.Text, uiPogodaValue1.Text) Then Continue For
                If Not CheckFieldValue(sTextDump, uiPogodaField2.Text, uiPogodaValue2.Text) Then Continue For
                If Not CheckFieldValue(sTextDump, uiPogodaField3.Text, uiPogodaValue3.Text) Then Continue For

                If Not CheckFieldValueMinMax(sTextDump, uiPogodaFieldNum0.Text, uiPogodaValueMin0.Text, uiPogodaValueMax0.Text) Then Continue For
                If Not CheckFieldValueMinMax(sTextDump, uiPogodaFieldNum1.Text, uiPogodaValueMin1.Text, uiPogodaValueMax1.Text) Then Continue For
                If Not CheckFieldValueMinMax(sTextDump, uiPogodaFieldNum2.Text, uiPogodaValueMin2.Text, uiPogodaValueMax2.Text) Then Continue For
                If Not CheckFieldValueMinMax(sTextDump, uiPogodaFieldNum3.Text, uiPogodaValueMin3.Text, uiPogodaValueMax3.Text) Then Continue For


            End If

#End Region

#Region "Opad"
            oExif = oPicek.GetExifOfType(Vblib.ExifSource.AutoMeteoOpad)
            If oExif?.MeteoOpad Is Nothing Then
                If Not uiMeteoOpadDefault.IsChecked Then Continue For
            Else
                Dim sTextDump As String = oExif.MeteoOpad.DumpAsJSON
                If Not CheckFieldValue(sTextDump, uiMeteoOpadField0.Text, uiMeteoOpadValue0.Text) Then Continue For
                If Not CheckFieldValue(sTextDump, uiMeteoOpadField1.Text, uiMeteoOpadValue1.Text) Then Continue For
                If Not CheckFieldValue(sTextDump, uiMeteoOpadField2.Text, uiMeteoOpadValue2.Text) Then Continue For

                If Not CheckFieldValueMinMax(sTextDump, uiMeteoOpadFieldNum0.Text, uiMeteoOpadValueMin0.Text, uiMeteoOpadValueMax0.Text) Then Continue For
                If Not CheckFieldValueMinMax(sTextDump, uiMeteoOpadFieldNum1.Text, uiMeteoOpadValueMin1.Text, uiMeteoOpadValueMax1.Text) Then Continue For
                If Not CheckFieldValueMinMax(sTextDump, uiMeteoOpadFieldNum2.Text, uiMeteoOpadValueMin2.Text, uiMeteoOpadValueMax2.Text) Then Continue For
                If Not CheckFieldValueMinMax(sTextDump, uiMeteoOpadFieldNum3.Text, uiMeteoOpadValueMin3.Text, uiMeteoOpadValueMax3.Text) Then Continue For


            End If

#End Region

#Region "Klimat"
            oExif = oPicek.GetExifOfType(Vblib.ExifSource.AutoMeteoKlimat)
            If oExif?.MeteoKlimat Is Nothing Then
                If Not uiMeteoKlimatDefault.IsChecked Then Continue For
            Else
                Dim sTextDump As String = oExif.MeteoKlimat.DumpAsJSON
                If Not CheckFieldValue(sTextDump, uiMeteoKlimatField0.Text, uiMeteoKlimatValue0.Text) Then Continue For
                If Not CheckFieldValue(sTextDump, uiMeteoKlimatField1.Text, uiMeteoKlimatValue1.Text) Then Continue For
                If Not CheckFieldValue(sTextDump, uiMeteoKlimatField2.Text, uiMeteoKlimatValue2.Text) Then Continue For

                If Not CheckFieldValueMinMax(sTextDump, uiMeteoKlimatFieldNum0.Text, uiMeteoKlimatValueMin0.Text, uiMeteoKlimatValueMax0.Text) Then Continue For
                If Not CheckFieldValueMinMax(sTextDump, uiMeteoKlimatFieldNum1.Text, uiMeteoKlimatValueMin1.Text, uiMeteoKlimatValueMax1.Text) Then Continue For
                If Not CheckFieldValueMinMax(sTextDump, uiMeteoKlimatFieldNum2.Text, uiMeteoKlimatValueMin2.Text, uiMeteoKlimatValueMax2.Text) Then Continue For
                If Not CheckFieldValueMinMax(sTextDump, uiMeteoKlimatFieldNum3.Text, uiMeteoKlimatValueMin3.Text, uiMeteoKlimatValueMax3.Text) Then Continue For


            End If

#End Region


#End Region




            _queryResults.Add(oPicek)
            iCount += 1
        Next
        Application.ShowWait(False)

        Return iCount
    End Function


    Private Function CheckFieldValue(textDump As String, fieldName As String, fieldValue As String) As Boolean
        If String.IsNullOrWhiteSpace(fieldValue) Then Return True
        If String.IsNullOrWhiteSpace(fieldName) Then Return CheckStringMasks(textDump, fieldValue)

        Dim aDump As String() = textDump.ToLowerInvariant.Split(vbCr)

        Dim bInDay As Boolean = False
        Dim bInCurrent As Boolean = False

        Dim bWantDay As Boolean = False 'fieldName.Substring(0, 2) = "d."
        Dim bWantCurr As Boolean = True ' fieldName.Substring(0, 2) = "c."
        If fieldName.Substring(1, 1) = "." Then
            bWantDay = (fieldName.Substring(0, 2) = "d.")
            bWantCurr = (fieldName.Substring(0, 2) = "c.")
            fieldName = fieldName.Substring(2)
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

            If Not sDumpLine.Substring(0, iInd).Contains(fieldName.ToLowerInvariant) Then Continue For

            Return CheckStringMasks(sDumpLine.Substring(iInd + 1), fieldValue)
        Next

        Return True

    End Function

    Private Function CheckFieldValueMinMax(textDump As String, fieldName As String, fieldMinValue As String, fieldMaxValue As String) As Boolean

        If String.IsNullOrWhiteSpace(fieldName) Then Return True

        Dim bInDay As Boolean = False
        Dim bInCurrent As Boolean = False

        Dim bWantDay As Boolean = False 'fieldName.Substring(0, 2) = "d."
        Dim bWantCurr As Boolean = True ' fieldName.Substring(0, 2) = "c."
        If fieldName.Substring(1, 1) = "." Then
            bWantDay = (fieldName.Substring(0, 2) = "d.")
            bWantCurr = (fieldName.Substring(0, 2) = "c.")
            fieldName = fieldName.Substring(2)
        End If

        Dim dMinVal As Double = Double.MinValue
        If Not String.IsNullOrWhiteSpace(fieldMinValue) Then dMinVal = fieldMinValue
        Dim dMaxValue As Double = Double.MaxValue
        If Not String.IsNullOrWhiteSpace(fieldMaxValue) Then dMaxValue = fieldMaxValue

        Dim aDump As String() = textDump.ToLowerInvariant.Split(vbCr)
        fieldName = fieldName.ToLowerInvariant

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
            If sDumpLine.Substring(0, iInd).Replace("""", "").Trim <> fieldName Then Continue For

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
    ''' true/false, jeśli spełnione są "fragments, prefixed with ! for negate"
    ''' </summary>
    Private Function CheckStringMasks(sFromPicture As String, sMaskiWord As String) As Boolean
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
    ''' true/false, jeśli jest contains
    ''' </summary>
    Private Function CheckStringContains(sFromPicture As String, sMaska As String) As Boolean
        If String.IsNullOrWhiteSpace(sMaska) Then Return True

        sFromPicture = If(sFromPicture?.ToLowerInvariant, "")

        Return sFromPicture.Contains(sMaska)
    End Function


    Private Async Sub uiSearch_Click(sender As Object, e As RoutedEventArgs)

        If uiTags.Text.Length > 0 AndAlso uiTags.Text.ToLowerInvariant = uiTags.Text Then
            If Await vb14.DialogBoxYNAsync("Keywords ma tylko małe litery, czy zmienić na duże?") Then
                uiTags.Text = uiTags.Text.ToUpper
            End If
        End If


        Dim iCount As Integer
        If _inputList Is Nothing Then
            iCount = Szukaj(_fullArchive.GetList)
        Else
            iCount = Szukaj(_inputList)
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

    Private Sub uiGetGeo_Click(sender As Object, e As RoutedEventArgs)
        Dim oWnd As New EnterGeoTag
        If Not oWnd.ShowDialog Then Return
        _geoTag = oWnd.GetGeoPos
        uiLatLon.Text = $"szer. {_geoTag.StringLat(3)}, dług. {_geoTag.StringLon(3)}"
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

        Dim oWnd As New ProcessBrowse(lista, True)
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

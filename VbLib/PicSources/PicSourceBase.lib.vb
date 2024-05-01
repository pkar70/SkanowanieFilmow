
' Ewentualnie: JSON config, moze %computername%.picsource.json jako konfiguracja (kopia) w miejscu, przy podłączaniu source szuka takich i pyta czy zaimportować (defaulttags), ale też może byc dla roznych instacji - roznie

Imports System.Text
Imports System.Text.RegularExpressions
Imports pkar.DotNetExtensions

Public MustInherit Class PicSourceBase
	Inherits pkar.BaseStruct

	Public Property Typ As PicSourceType  ' MTP, FOLDER, ADHOC, ONEDRIVE
	Public Property SourceName As String  ' c:\xxxx, MTP\Lumia435, MTP\Lumia650 - per instance
	Public Property enabled As Boolean = True
	Public Property Path As String  ' znaczenie zmienne w zależności od Type
	Public Property VolLabel As String  ' device MTP, albo vollabel dysku do sprawdzania
	Public Property Recursive As Boolean
	Public Property sourcePurgeDelay As TimeSpan = TimeSpan.FromDays(7)
	Public Property defaultPublish As List(Of String)   ' lista IDs
	Public Property includeMask As String = OnePic.ExtsPic & OnePic.ExtsMovie & ";*.nar;*.jpg.thumb;*.raf" ' maski regexp
	Public Property excludeMask As String  ' maski regexp
	Public Property lastDownload As DateTime
	Public Property defaultExif As ExifTag

	<Newtonsoft.Json.JsonIgnore>
	Public Property currentExif As ExifTag  ' default + aktualne zmiany

	Public Sub ExifDefaultToCurrent()
		If currentExif IsNot Nothing Then Return
		If defaultExif Is Nothing Then Return

		currentExif = defaultExif.Clone
	End Sub


	'<Newtonsoft.Json.JsonIgnore>
	'Public Shared Property _dataFolder As String    ' dla JSON, żeby mógł wczytać

	''' <summary>
	''' datafolder potrzebny na plik PURGE, może być NULL
	''' </summary>
	Public Sub New(typSource As PicSourceType, sDataFolder As String)
		If Not String.IsNullOrWhiteSpace(sDataFolder) Then
			_purgeFile = IO.Path.Combine(sDataFolder, $"purge.{SourceName}.txt")
		End If
		Typ = typSource
	End Sub

	Protected MustOverride Function IsPresent_Main() As Boolean

	''' <summary>
	''' sprawdzenie czy katalog (z VolLabel) jest dostępny - dysk lub device
	''' </summary>
	''' <returns></returns>
	Public Function IsPresent() As Boolean
		DumpCurrMethod()
		If Not enabled Then Return False
		Return IsPresent_Main()
	End Function

#Region "listing / file iterations"

	Protected MustOverride Function ReadDirectory_Main() As Integer
	''' <summary>
	''' wczytanie katalogu plików (pełny listing), dodając tagi z nazw plików
	''' </summary>
	''' <param name="lKeywords">Lista słów kluczowych, które są wyciągane z nazwy pliku</param>
	''' <returns>count(files), gdy = 0 wtedy nie ma sensu iterować</returns>
	Public Function ReadDirectory(lKeywords As List(Of OneKeyword)) As Integer
		DumpCurrMethod()
		If Not IsPresent_Main() Then Return -1

		' ale tego nie powinno być potrzebne, bo przed startem z UI powinno być modyfikowanie
		Dim bNoCurrentExif As Boolean = False
		If currentExif Is Nothing Then
			bNoCurrentExif = True
			currentExif = defaultExif.Clone
		End If

		Dim iRet As Integer = ReadDirectory_Main()
		UseTagsFromFilename(_listaPlikow, lKeywords)
		' jesli sobie przełączyliśmy, to teraz przełączamy na powrot
		If bNoCurrentExif Then currentExif = Nothing

		DumpMessage($"po readdirmain,ret={iRet}")
		Return iRet

	End Function

	<Newtonsoft.Json.JsonIgnore>
	Protected _listaPlikow As List(Of OnePic)

	''' <summary>
	''' "import" wcześniej używanych tagów, tych istniejących
	''' </summary>
	''' <param name="lPicki"></param>
	''' <param name="lKeys"></param>
	Public Shared Sub UseTagsFromFilename(lPicki As List(Of OnePic), lKeys As List(Of OneKeyword))

		For Each oPic As OnePic In lPicki

			Dim oExif As ExifTag = oPic.GetExifOfType(ExifSource.SourceFile)
			' można się zabezpieczyć i dawać nowy, ale ten i tak zawsze będzie
			If oExif Is Nothing Then Continue For

			Dim sFilename As String = IO.Path.GetFileNameWithoutExtension(oPic.sSuggestedFilename)

			' etap 1: przepisanie TAGów z filename do Keywords
			' for each kwd, use it
			For Each oKey As OneKeyword In lKeys
				If oKey.IsRoot Then Continue For

				If sFilename.Contains(oKey.sId) Then
					oExif.Keywords = oExif.Keywords & " " & oKey.sId
					oExif.UserComment = oExif.UserComment & " | " & oKey.sDisplayName

					sFilename = sFilename.Replace(oKey.sId, "")  ' kasujemy istniejące
				End If
			Next

			' wycięcie pierwszego sklejenia
			If Not String.IsNullOrEmpty(oExif.Keywords) Then
				oExif.Keywords = oExif.Keywords.Trim
				If oExif.UserComment.StartsWith(" | ") Then oExif.UserComment = oExif.UserComment.Substring(3)
			End If

			' *TODO* reszta -do userdescription
			' WP_20[0-9_]*_Rich, WP_20[0-9_]*_Pro, 

			' telefon Marcina, 20220723_170151
			'				 0123456789012345
			If Regex.IsMatch(sFilename, "^20[0-9][0-9][0-1][0-9][0-3][0-9]_[0-2][0-9][0-5][0-9][0-6][0-9]") Then sFilename = sFilename.Substring(15)

			' Fuji,	DSCF0494
			'		0123456789012345
			If Regex.IsMatch(sFilename, "^DSCF[0-9][0-9][0-9][0-9]") Then sFilename = sFilename.Substring(8)


			' Motorola Aśki, IMG_20230128_111119345
			'			   012345678901234567
			' to pierwsze jest błędne, ale nie wiem czemu
			' pierwsze IMG_2yyymmdd_hhmmss
			'		  0123456789012345678
			' drugie:  IMG_2yyymmdd_hhmm
			'		  01234567890123456
			If Regex.IsMatch(sFilename, "^IMG_2[0-1][0-9][0-9][0-2][0-9][0-3][0-9]_[0-2][0-9][0-5][0-9][0-5][0-9]") Then sFilename = sFilename.Substring(19)
			' If Regex.IsMatch(sFilename, "^IMG_2[0-1][0-9][0-9][0-2][0-9][0-3][0-9]_[0-2][0-9][0-5][0-9]") Then sFilename = sFilename.Substring(17)

			' iPhone	IMG_E5513.JPG
			'		0123456789012345
			If Regex.IsMatch(sFilename, "^IMG_E[0-9][0-9][0-9][0-9]") Then sFilename = sFilename.Substring(9)
			' iPhone	IMG_5513.JPG
			'		0123456789012345
			If Regex.IsMatch(sFilename, "^IMG_[0-9][0-9][0-9][0-9]") Then sFilename = sFilename.Substring(8)

			' telefon		WP_20221117_11_32_45_Pro
			'			WP_20221117_11_45_21_Rich
			'			012345678901234567890123456
			If Regex.IsMatch(sFilename, "^WP_20[0-9][0-9][0-1][0-9][0-3][0-9]_[0-2][0-9]_[0-5][0-9]") Then
				sFilename = sFilename.Substring(20)
				sFilename = sFilename.Replace("_Pro", "")
				sFilename = sFilename.Replace("_Rich", "")
			End If

			' desktop WIN_20220609_09_51_08_Pro
			'		 012345678901234567890123456
			If Regex.IsMatch(sFilename, "^WIN_20[0-9][0-9][0-1][0-9][0-3][0-9]_[0-2][0-9]_[0-5][0-9]") Then
				sFilename = sFilename.Substring(21)
				sFilename = sFilename.Replace("_Pro", "")
				sFilename = sFilename.Replace("_Rich", "")
			End If

			' 2024.01.02: jak zostaną same cyfry, to je usuwamy
			If Regex.IsMatch(sFilename, "^[0-9]+$") Then sFilename = ""

			'If sFilename.Length > 4 Then sFilename = sFilename.Substring(0, sFilename.Length - 4)
			If sFilename.Length > 2 Then
				oExif.UserComment = sFilename & " | " & oExif.UserComment
			End If
		Next

		' *TODO* ewentualnie tak podmień, by suggested filename (a więc w buforze i dalej) było już bez tego
	End Sub


	''' <summary>
	''' returns count of files newer than sinceDate (excluding)
	''' </summary>
	''' <param name="sinceDate"></param>
	''' <returns></returns>
	Public Function Count(Optional sinceDate As DateTime = Nothing) As Integer
		If _listaPlikow Is Nothing Then Return -1

		If Not sinceDate.IsDateValid Then sinceDate = lastDownload

		Dim iCnt As Integer = 0
		For Each oFile As OnePic In _listaPlikow
			Dim oExif As ExifTag = oFile.GetExifOfType(ExifSource.SourceFile)
			If oExif Is Nothing Then Continue For

			If oExif.DateMax > sinceDate Then iCnt += 1
		Next

		Return iCnt
	End Function

	Protected MustOverride Function OpenFile(oPic As OnePic) As Boolean

#Region "descript.ion file"

	<Newtonsoft.Json.JsonIgnore>
	Private _sDescriptIonContent As String()
	<Newtonsoft.Json.JsonIgnore>
	Private _sDescriptIonName As String

	Private Sub TryDescrptIon(oFile As OnePic)
		' dla MTP nic nie zrobimy, za dużo roboty (a i tak bez sensu, bo skądby tam ten plik?)
		If Typ = PicSourceType.MTP Then Return

		' pathname dla descript.ion
		Dim sPicDir As String = IO.Path.GetDirectoryName(oFile.sInSourceID)
		Dim sPicFile As String = IO.Path.GetFileName(oFile.sInSourceID)
		Dim sDescr As String = IO.Path.Combine(sPicDir, "descript.ion")


		' if inne niz _sDescriptIonName - wczytaj
		If sDescr <> _sDescriptIonName Then
			_sDescriptIonName = sDescr
			If Not IO.File.Exists(sDescr) Then Return
			Try
				' jak nie ma Nuget System.Text.Encoding.CodePages to bedzie fail przy nie .Net Framework
				' https://stackoverflow.com/questions/50858209/system-notsupportedexception-no-data-is-available-for-encoding-1252
				_sDescriptIonContent = IO.File.ReadAllLines(sDescr, Encoding.GetEncoding(1250))
			Catch ex As Exception
				_sDescriptIonContent = IO.File.ReadAllLines(sDescr)
			End Try
		End If

		If _sDescriptIonContent Is Nothing Then Return

		' sprawdz czy mamy
		' filename.jpg<space>comment<0x04>bindata
		' "filename ze spacja.jpg"<space>comment<0x04>bindata
		For Each sLine As String In _sDescriptIonContent

			Dim iInd As Integer = sLine.IndexOf(sPicFile)
			If iInd < 0 Then Continue For
			If iInd > 1 Then Continue For

			If iInd = 0 Then
				iInd = sLine.IndexOf(" ")
			Else
				' omijamy problem z plikami sWP... (mniejsze wersje)
				If Not sLine.StartsWith("""") Then Continue For
				iInd = sLine.IndexOf(""" ") + 1
			End If

			Dim sComment As String = sLine.Substring(iInd + 1)
			iInd = sComment.IndexOf(ChrW(4))
			If iInd > -1 Then sComment = sComment.Substring(0, iInd).Trim

			If Not String.IsNullOrWhiteSpace(sComment) Then
				' jesli tak, to stworz nowego Exifa
				Dim oNew As New ExifTag(ExifSource.SourceDescriptIon)
				oNew.UserComment = sComment
				oFile.Exifs.Add(oNew)
				oFile.AddDescription(New OneDescription(sComment, ""))
			End If
		Next

	End Sub

#End Region

	<Newtonsoft.Json.JsonIgnore>
	Private _sinceDate As DateTime

	''' <summary>
	''' get first file to download 
	''' </summary>
	''' <param name="sinceDate"></param>
	''' <returns></returns>
	Public Function GetFirst(Optional sinceDate As DateTime = Nothing) As OnePic
		If _listaPlikow Is Nothing Then Return Nothing

		If Not SourceName.ContainsCI("adhoc") Then
			If Not sinceDate.IsDateValid Then sinceDate = lastDownload
		End If

		_sinceDate = sinceDate

		For iLp = 0 To _listaPlikow.Count - 1
			Dim oFile As OnePic = _listaPlikow.ElementAt(iLp)
			Dim oExif As ExifTag = oFile.GetExifOfType(ExifSource.SourceFile)
			If oExif Is Nothing Then Continue For

			If oExif.DateMax > sinceDate Then
				_lastIDreturned = iLp
				OpenFile(oFile)
				TryDescrptIon(oFile)
				Return oFile
			End If
		Next

		Return Nothing

	End Function

	<Newtonsoft.Json.JsonIgnore>
	Private Property _lastIDreturned As Integer = -1

	''' <summary>
	''' get next file to download
	''' </summary>
	''' <param name="sinceDate"></param>
	''' <returns></returns>
	Public Function GetNext() As OnePic
		If _lastIDreturned = -1 Then Return Nothing
		If _lastIDreturned > -1 Then _listaPlikow.ElementAt(_lastIDreturned)?.oContent?.Dispose()


		For iLp = _lastIDreturned + 1 To _listaPlikow.Count - 1
			Dim oFile As OnePic = _listaPlikow.ElementAt(iLp)
			Dim oExif As ExifTag = oFile.GetExifOfType(ExifSource.SourceFile)
			If oExif Is Nothing Then Continue For

			If oExif.DateMax > _sinceDate Then
				_lastIDreturned = iLp
				OpenFile(oFile)
				TryDescrptIon(oFile)
				Return oFile
			End If
		Next

		'_lastIDreturned += 1
		'If _lastIDreturned > _listaPlikow.Count - 1 Then
		'	_lastIDreturned = -1
		'	Return Nothing
		'End If

		'Dim oFile As OnePic = _listaPlikow.ElementAt(_lastIDreturned)
		'OpenFile(oFile)
		'TryDescrptIon(oFile)

		'Return oFile

		Return Nothing
	End Function

	''' <summary>
	''' get date of oldest file in source
	''' </summary>
	''' <param name="bAll"></param>
	''' <returns></returns>
	Public Function GetDateMin(Optional bAll As Boolean = False) As DateTime
		If _listaPlikow Is Nothing Then Return Nothing
		If _listaPlikow.Count < 1 Then Return Nothing

		If bAll Then Return _listaPlikow.ElementAt(0).GetExifOfType(ExifSource.SourceFile)?.DateMax

		For Each oFile As OnePic In _listaPlikow
			Dim oExif As ExifTag = oFile.GetExifOfType(ExifSource.SourceFile)
			If oExif Is Nothing Then Continue For

			If oExif.DateMax > lastDownload Then Return oExif.DateMax
		Next

		Return Nothing
	End Function

	Public Function GetDateMax() As DateTime
		If _listaPlikow Is Nothing Then Return Nothing
		If _listaPlikow.Count < 1 Then Return Nothing

		Return _listaPlikow.ElementAt(_listaPlikow.Count - 1).GetExifOfType(ExifSource.SourceFile)?.DateMax
	End Function
#End Region

#If TUTAJ_MASKI Then
	Public Shared Function MatchesMasks(sFilenameNoPath As String, sIncludeMasks As String, sExcludeMasks As String) As Boolean

		' https://stackoverflow.com/questions/725341/how-to-determine-if-a-file-matches-a-file-mask
		Dim aMaski As String()

		If Not String.IsNullOrWhiteSpace(sExcludeMasks) Then
			aMaski = sExcludeMasks.Split(";")
			For Each maska As String In aMaski
				Dim regExMaska As Regex = New Regex(maska.Replace(".", "[.]").Replace("*", ".*").Replace("?", "."))
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
			Dim regExMaska As Regex = New Regex(maska.Replace(".", "[.]").Replace("*", ".*").Replace("?", "."))
			If regExMaska.IsMatch(sFilenameNoPath) Then
				bMatch = True
				Exit For
			End If
		Next

		Return bMatch
	End Function

	Protected Function MatchesMasks(sFilenameNoPath As String) As Boolean
		Return MatchesMasks(sFilenameNoPath, includeMask, excludeMask)
	End Function
#End If

#Region "Purging"

	<Newtonsoft.Json.JsonIgnore>
	Private Property _purgeFile As String

	Public Sub InitDataDirectory(sDataFolder As String)
		_purgeFile = IO.Path.Combine(sDataFolder, $"purge.{SourceName}.txt")
	End Sub


	''' <summary>
	''' usunięcie jednego pliku - dla Purge
	''' </summary>
	''' <param name="sId"></param>
	''' <returns></returns>
	Protected MustOverride Function DeleteFile(sId As String) As Boolean

	''' <summary>
	''' dodanie pliku do listy plików do usunięcia
	''' </summary>
	''' <param name="sId"></param>
	Public Sub AddToPurgeList(sId As String)
		If Typ = PicSourceType.AdHOC Then Return
		IO.File.AppendAllText(_purgeFile, Date.Now.ToString("yyyyMMdd.HHmm") & vbTab & sId & vbCrLf)
	End Sub

	''' <summary>
	''' purge all files from purge list (bRealPurge = true), lub policz pliki podpadające pod usuwanie (false)
	''' </summary>
	''' <param name="bRealPurge">TRUE: kasuj, FALSE: tylko policz</param>
	''' <returns></returns>
	Public Function Purge(bRealPurge As Boolean, everyTick As Action) As Integer
		DumpCurrMethod()
		If Typ = PicSourceType.AdHOC Then Return 0

		If Not IO.File.Exists(_purgeFile) Then Return 0
		Dim sContent As String() = IO.File.ReadAllLines(_purgeFile)
		Dim sNewContent As New List(Of String)
		Dim iCnt As Integer = 0
		' file + delay < today
		' file < today-delay

		Dim sPurgeDate As String = Date.Now.Add(-sourcePurgeDelay).ToString("yyyyMMdd.HHmm")

		For Each sFile As String In sContent
			If sFile < sPurgeDate Then
				Dim iInd As Integer = sFile.IndexOf(vbTab)
				If iInd < 2 Then
					DialogBox("Błędny plik PURGE!")
					Return -iCnt
				End If
				If bRealPurge Then
					If everyTick IsNot Nothing Then everyTick()
					DeleteFile(sFile.Substring(iInd + 1))   ' tu można byłoby uwzględniac ret=FALSE (nieudane)
				End If
				iCnt += 1
			Else
				If bRealPurge Then sNewContent.Add(sFile)
			End If
		Next

		If Not bRealPurge Then Return iCnt

		If sNewContent.Count = 0 Then
			IO.File.Delete(_purgeFile)
		Else
			IO.File.WriteAllLines(_purgeFile, sNewContent)
		End If

		Return iCnt
	End Function
#End Region




End Class


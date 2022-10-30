

Public MustInherit Class PicSourceBase
	Inherits MojaStruct

	Public MustOverride ReadOnly Property Typ As String  ' MTP, folder
	Public Property Name As String  ' c:\xxxx, MTP\Lumia435, MTP\Lumia650 - per instance
	Public Property enabled As Boolean = True
	Public Property Path As String  ' znaczenie zmienne w zależności od Type
	Public Property VolLabel As String  ' device MTP, albo vollabel dysku do sprawdzania
	Public Property Recursive As Boolean
	Public Property sourceRemoveDelay As TimeSpan
	Public Property defaultPublish As List(Of String)   ' lista IDs
	Public Property include As List(Of String)  ' maski regexp
	Public Property exclude As List(Of String)  ' maski regexp
	Public Property lastDownload As DateTime
	Public Property defaultExif As ExifTag


	<Newtonsoft.Json.JsonIgnore>
	Public Property currentExif As ExifTag  ' default + aktualne zmiany


	Public Sub New(sDataFolder As String)
		_purgeFile = IO.Path.Combine(sDataFolder, "sources", Name & ".purge.txt")
	End Sub

	Protected MustOverride Function IsPresent_Main() As Boolean

	''' <summary>
	''' sprawdzenie czy katalog (z VolLabel) jest dostępny - dysk lub device
	''' </summary>
	''' <returns></returns>
	Public Function IsPresent() As Boolean
		If Not enabled Then Return False
		Return IsPresent_Main()
	End Function

#Region "listing / file iterations"

	''' <summary>
	''' wczytanie katalogu plików (pełny listing)
	''' </summary>
	''' <returns>count(files), gdy = 0 wtedy nie ma sensu iterować</returns>
	Public MustOverride Function ReadDirectory() As Integer

	<Newtonsoft.Json.JsonIgnore>
	Protected _listaPlikow As List(Of OneSourcePic)

	''' <summary>
	''' returns count of files newer than sinceDate (excluding)
	''' </summary>
	''' <param name="sinceDate"></param>
	''' <returns></returns>
	Public Function Count(Optional sinceDate As DateTime = Nothing) As Integer
		If _listaPlikow Is Nothing Then Return -1

		If sinceDate < New Date(1800, 1, 1) Then sinceDate = lastDownload

		Dim iCnt As Integer = 0
		For Each oFile As OneSourcePic In _listaPlikow
			' *TODO* sprawdzenie daty w EXIFach, wedle EXIFS typ SOURCE_FILE
		Next

		Return iCnt
	End Function

	''' <summary>
	''' get first file to download 
	''' </summary>
	''' <param name="sinceDate"></param>
	''' <returns></returns>
	Public Function GetFirst(Optional sinceDate As DateTime = Nothing) As OneSourcePic
		If _listaPlikow Is Nothing Then Return Nothing

		If sinceDate < New Date(1800, 1, 1) Then sinceDate = lastDownload

		For Each oFile As OneSourcePic In _listaPlikow
			' *TODO* sprawdzenie daty w EXIFach, wedle EXIFS typ SOURCE_FILE
			'	_lastIDreturned = New OneSourcePic
			' return ..
		Next

		Return Nothing

	End Function

	<Newtonsoft.Json.JsonIgnore>
	Private Property _lastIDreturned As OneSourcePic

	''' <summary>
	''' get next file to download
	''' </summary>
	''' <param name="sinceDate"></param>
	''' <returns></returns>
	Public Function GetNext() As OneSourcePic
		For i = 0 To _listaPlikow.Count - 2
			If _listaPlikow.ElementAt(i).ID = _lastIDreturned.ID Then
				_lastIDreturned = _listaPlikow.ElementAt(i + 1)
				Return _lastIDreturned
			End If
		Next

		Return Nothing
	End Function

	''' <summary>
	''' get date of oldest file in source
	''' </summary>
	''' <param name="bAll"></param>
	''' <returns></returns>
	Public Function GetDateMin(Optional bAll As Boolean = False) As DateTime
		' *TODO* wedlug _listaPlikow.ElementAt(...).exifs.SOURCE_FILENAME
	End Function

	Public Function GetDateMax() As DateTime
		' *TODO* wedlug _listaPlikow.ElementAt(_listaPlikow.Count -1).exifs.SOURCE_FILENAME
	End Function
#End Region

#Region "Purging"

	<Newtonsoft.Json.JsonIgnore>
	Private Property _purgeFile As String

	''' <summary>
	''' usunięcie jednego pliku - dla Purge
	''' </summary>
	''' <param name="sId"></param>
	''' <returns></returns>
	Protected MustOverride Function DeleteFile(sId As String)

	''' <summary>
	''' dodanie pliku do listy plików do usunięcia
	''' </summary>
	''' <param name="sId"></param>
	Public Sub AddToPurgeList(sId As String)
		IO.File.AppendAllText(_purgeFile, sId & vbCrLf)
	End Sub

	''' <summary>
	''' purge all files from purge list
	''' </summary>
	''' <param name="olderThan"></param>
	''' <returns></returns>
	Public Sub Purge()
		Dim sContent As String() = IO.File.ReadAllLines(_purgeFile)
		For Each sFile As String In sContent
			DeleteFile(sFile)
		Next
	End Sub
#End Region

	'IMPLEMENT: vblib, JSON na config, moze %computername%.picsource.json jako konfiguracja (kopia) w miejscu, przy podłączaniu source szuka takich i pyta czy zaimportować (defaulttags), ale też może byc dla roznych instacji - roznie
	'	reguły rename? np. dir\file z karty SD na dir_file?, DSCF1234 na yymmddhhmmss? dla latwiejszego uniqID




End Class

'*) SOURCE  FOLDER|MTP, To będą albo karta z aparatu (FOLD/subf+?/file), albo telefon (MTP/CameraRoll), albo zrzucone skądś (FOLD/subf*/file - browser za każdym razem), albo folder z OneDrive
'	typ/ name
'	path(internal data)
'	recursive yes/no
'	sourceRemove yes/no, delayed? TimeSpan
'	Default tags
'Default publish?
'include: List(Of String), exclude: List(Of String) -maski regexp
'	IMPLEMENT: vblib, JSON na config, moze %computername%.picsource.json jako konfiguracja (kopia) w miejscu, przy podłączaniu source szuka takich i pyta czy zaimportować (defaulttags), ale też może byc dla roznych instacji - roznie
'	UI: dodaj(moze byc wielokrotne), visible yes/no, tagRules ?
'	METHODS: Dir(allFiles = False - od ostatniego, albo calosc), GetFile(filepath), GetTags(filepath),  COnfigCreate, COnfigSave
'	Count(allFiles = False, since = yyyymmddhhmmss), First, Next, Purge(olderThanDate), DateMin, DateMax,
'	Moze STRUCT SrcFile path, name, tags, publishers, stream.
'	reguły rename? np. dir\file z karty SD na dir_file?, DSCF1234 na yymmddhhmmss? dla latwiejszego uniqID
'	FUNKCJONALNOSC: Purge, Download
'Tu moze BUFFER, z plikami sciagnietym z zewnątrz, wraz z tagami dopisanymi w pliku
'może też różne buffer dla różnych sources?
'Albo tagi dopisywac automatycznie podczas kopiowania pliku? tyle że To zepsuje daty.

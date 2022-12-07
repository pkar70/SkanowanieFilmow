

Imports System.IO

Public MustInherit Class LocalStorage
	Implements AnyStorage

	Public Property StorageName As String
	Public Property enabled As Boolean = True
	Public Property Path As String
	Public Property VolLabel As String
	Public Property includeMask As String = "*.jpg;*.tif;*.png" ' maski regexp
	Public Property excludeMask As String  ' maski regexp

	Public Property lastSave As DateTime

	Public Property jsonInDir As Boolean = True
	'	Public Property saveToExif As Boolean = True

	Public Property tree0Dekada As Boolean = False
	Public Property tree1Rok As Boolean = True
	Public Property tree2Miesiac As Boolean = True
	Public Property tree3Dzien As Boolean
	Public Property tree3DzienWeekDay As Boolean = True
	Public Property tree4Geo As Boolean = True

#Region "to co musi być w vblib 2.0"
	Public MustOverride Function IsPresent() As Boolean
	Protected MustOverride Function GetConvertedPathForVol(sVolLabel As String, sPath As String) As String
#End Region

#Region "folder ID na real"

	''' <summary>
	''' robi domyślną nazwę do zapisu
	''' </summary>
	''' <param name="oOneDir">do jakiego katalogu</param>
	''' <param name="sGeo">string potrzebny przy dzieleniu wedle GEO</param>
	''' <returns></returns>
	Protected Function GetWriteFolder(sDirId As String) As String
		Return GetFolder(sDirId, False)
	End Function

	''' <summary>
	''' znalezienie w archiwum katalogu - gdziekolwiek by on nie był (niezależnie od struktury i rename)
	''' </summary>
	''' <param name="oItem"></param>
	''' <returns></returns>
	Protected Function FindRealFolder(sDirId As String) As String
		Return GetFolder(sDirId, True)
	End Function


	''' <summary>
	''' robi domyślną nazwę do zapisu
	''' </summary>
	''' <param name="oOneDir">do jakiego katalogu</param>
	''' <param name="sGeo">string potrzebny przy dzieleniu wedle GEO</param>
	''' <returns></returns>
	Private Function GetFolder(sDirId As String, bForRead As Boolean) As String

		Dim sPath As String = GetConvertedPathForVol(VolLabel, Path)
		If sPath = "" Then Return ""

		If OneDir.IsFromKeyword(sDirId) Then Return GetFolderForKeyword(sPath, sDirId, bForRead)

		' 1981.01.23.sb_geo -> 198x
		If tree0Dekada Or bForRead Then
			sPath = FindCreateRealDir(sPath, sDirId.Substring(0, 3) & "x", Not bForRead)
		End If

		' 1981.01.23.sb_geo -> 1981
		If tree1Rok Or bForRead Then
			sPath = FindCreateRealDir(sPath, sDirId.Substring(0, 4), Not bForRead)
		End If

		' 1981.01.23.sb_geo -> 1981.01
		If tree2Miesiac Or bForRead Then
			sPath = FindCreateRealDir(sPath, sDirId.Substring(0, 7), Not bForRead)
		End If

		' 1981.01.23.sb_geo -> 1981.01.23
		If tree3Dzien Or bForRead Then
			sPath = FindCreateRealDir(sPath, sDirId.Substring(0, 10), Not bForRead)
		End If

		' 1981.01.23.sb_geo -> 1981.01.23.sb
		If tree3DzienWeekDay Or bForRead Then
			sPath = FindCreateRealDir(sPath, sDirId.Substring(0, 13), Not bForRead)
		End If

		' 1981.01.23.sb_geo -> 1981.01.23.sb_geo
		If tree4Geo Or bForRead Then
			sPath = FindCreateRealDir(sPath, sDirId, Not bForRead)
		End If

		Return sPath
	End Function

	Private Function GetFolderForKeyword(sPath As String, sDirId As String, bForRead As Boolean) As String
		sPath = FindCreateRealDir(sPath, "_kwd", True)  ' _kwd musi istnieć
		'sPath = FindCreateRealDir(sPath, sDirId.Substring(0, 1), False) ' podkatalog typu może istnieć, ale nie musi (i program nigdy go nie stworzy)
		sPath = FindCreateRealDir(sPath, sDirId, Not bForRead) ' a konkretny - może
		Return sPath
	End Function

	''' <summary>
	''' znajdź w InFolder podkatalog którego nazwa rozpoczyna się od SubFolder, jak go nie ma, to utwórz (lub zwróć "")
	''' </summary>
	''' <param name="sInFolder"></param>
	''' <param name="sSubfolderPrefix"></param>
	''' <returns>folder pathname, albo "" gdy nie ma (i nie mamy bCreate)</returns>
	Private Function FindCreateRealDir(sInFolder As String, sSubfolderPrefix As String, bCreate As Boolean) As String
		If Not IO.Directory.Exists(sInFolder) Then Return ""

		For Each sFolder As String In IO.Directory.GetDirectories(sInFolder)
			If IO.Path.GetFileName(sFolder).StartsWith(sSubfolderPrefix) Then
				Return IO.Path.Combine(sInFolder, sSubfolderPrefix)
			End If
		Next

		If Not bCreate Then Return sInFolder

		Dim sSubfolder As String = IO.Path.Combine(sInFolder, sSubfolderPrefix)
		IO.Directory.CreateDirectory(sSubfolder)
		Return sSubfolder
	End Function
#End Region

#Region "implementacja interface"

#Region "przerzucenie do vblib 2.0"
	Public MustOverride Async Function GetMBfreeSpace() As Task(Of Integer) Implements AnyStorage.GetMBfreeSpace

#End Region

#Region "realne funkcje"

	Private Const NO_MATCH_MASK As String = "nomatch"

	Public Async Function SendFile(oPic As OnePic) As Task(Of String) Implements AnyStorage.SendFileMain
		If Not IsPresent() Then Return "ERROR: archiwum aktualnie jest niewidoczne"

		' zapisz plik, gdy błąd - wróć od razu
		Dim sErr As String = SendPhoto(oPic)
		If sErr = NO_MATCH_MASK Then Return ""    ' nie ma błędu, bo po prostu plik spoza maski jest
		If sErr <> "" Then Return sErr  ' błąd

		Dim sJsonContent As String = oPic.DumpAsJSON
		AddToJsonIndex(oPic.TargetDir, sJsonContent)

		Return ""

	End Function

	Public Async Function SendFiles(oPicki As List(Of OnePic)) As Task(Of String) Implements AnyStorage.SendFiles
		If Not IsPresent() Then Return "ERROR: archiwum aktualnie jest niewidoczne"

		If oPicki Is Nothing Then Return ""
		If oPicki.Count < 1 Then Return ""

		Dim sTargetDir As String = oPicki(0).TargetDir

		Dim sJsonContent As String = ""
		Dim sError As String = ""
		For Each oPic As OnePic In oPicki

			If oPic.TargetDir <> sTargetDir Then Continue For

			Dim temperr As String = SendPhoto(oPic)
			If temperr = NO_MATCH_MASK Then Continue For  ' nie ma błędu, bo po prostu plik spoza maski jest

			If temperr = "" Then
				' zapis OK
				If sJsonContent <> "" Then sJsonContent = sJsonContent & "," & vbCrLf
				sJsonContent = sJsonContent & oPic.DumpAsJSON
			Else
				sError = sError & vbCrLf & temperr
			End If

		Next

		If sJsonContent <> "" Then AddToJsonIndex(sTargetDir, sJsonContent)

		Return sError

	End Function

	Public Async Function VerifyFileExist(oPic As OnePic) As Task(Of String) Implements AnyStorage.VerifyFileExist
		If Not IsPresent() Then Return "ERROR: archiwum aktualnie jest niewidoczne"

		Dim sFolder As String = FindRealFolder(oPic.TargetDir)
		If String.IsNullOrEmpty(sFolder) Then Return "ERROR: cannot get folder for file"

		Dim sTargetFile As String = IO.Path.Combine(sFolder, oPic.sSuggestedFilename)

		If IO.File.Exists(sTargetFile) Then Return ""

		Return "no file"
	End Function

	Public Async Function VerifyFile(oPic As OnePic, oFromArchive As LocalStorage) As Task(Of String) Implements AnyStorage.VerifyFile
		If Not IsPresent() Then Return "ERROR: archiwum aktualnie jest niewidoczne"

		Dim sFolder As String = FindRealFolder(oPic.TargetDir)
		If String.IsNullOrEmpty(sFolder) Then Return "ERROR: cannot get folder for file"

		Dim sTargetFile As String = IO.Path.Combine(sFolder, oPic.sSuggestedFilename)

		If IO.File.Exists(sTargetFile) Then Return ""

		' *TODO* jeśli nie, to spróbuj skopiować z oArchive
		Throw New NotImplementedException()
	End Function

	Public Async Function GetFile(oPic As OnePic) As Task(Of String) Implements AnyStorage.GetFile
		If Not IsPresent() Then Return "ERROR: archiwum aktualnie jest niewidoczne"

		Dim sFolder As String = FindRealFolder(oPic.TargetDir)
		If String.IsNullOrEmpty(sFolder) Then Return "ERROR: cannot get folder for file"

		Dim sTargetFile As String = IO.Path.Combine(sFolder, oPic.sSuggestedFilename)

		If Not IO.File.Exists(sTargetFile) Then Return $"ERROR: non existent file {sTargetFile}"

		oPic.oContent = IO.File.Open(sTargetFile, FileMode.Open)

		Return ""
	End Function

	Public Shared Sub AddToJsonIndexMain(sIndexFilename As String, sContent As String)

		If Not IO.File.Exists(sIndexFilename) Then
			IO.File.WriteAllText(sIndexFilename, "[")
		Else
			' skoro już mamy coś w pliku, to teraz dodajemy do tego przecinek - pomiędzy itemami
			sContent = "," & vbCrLf & sContent
		End If

		IO.File.AppendAllText(sIndexFilename, sContent)

	End Sub

	Private Sub AddToJsonIndex(sDirId As String, sContent As String)
		If Not jsonInDir Then Return

		Dim sFolder As String = FindRealFolder(sDirId)
		If String.IsNullOrEmpty(sFolder) Then Return

		Dim sJsonFile As String = IO.Path.Combine(sFolder, "picsort.arch.json")

		AddToJsonIndexMain(sJsonFile, sContent)

	End Sub

	''' <summary>
	''' wysyła plik oPic do oOneDir, dba o daty pliku w archiwum, odnotowuje archiwizację w oPic.Archived
	''' </summary>
	''' <param name="oPic"></param>
	''' <param name="oOneDir"></param>
	''' <returns>errmessage lub ""</returns>
	Private Function SendPhoto(oPic As OnePic) As String

		If Not OnePic.MatchesMasks(oPic.sSuggestedFilename, includeMask, excludeMask) Then Return NO_MATCH_MASK

		Dim sFolder As String = GetWriteFolder(oPic.TargetDir)
		If String.IsNullOrEmpty(sFolder) Then Return "ERROR: SendPhoto, cannot get folder for write"

		Dim sTargetFile As String = IO.Path.Combine(sFolder, oPic.sSuggestedFilename)
		If IO.File.Exists(sTargetFile) Then Return $"ERROR: file {sTargetFile} already exist!"

		IO.File.Copy(oPic.InBufferPathName, sTargetFile)

		Try
			IO.File.SetCreationTime(sTargetFile, IO.File.GetCreationTime(oPic.InBufferPathName))
			IO.File.SetLastWriteTime(sTargetFile, IO.File.GetLastWriteTime(oPic.InBufferPathName))
		Catch ex As Exception
			' to nie jest takie istotne
		End Try

		oPic.AddArchive(StorageName)

		Return ""
	End Function

#End Region

#End Region
End Class


'*) LOCALSTORAGE  INTERNAL|EXTERNAL (podłącz dysk...) 
'	typ/ name
'	root-path(dla EXT - wewnatrz EXT, bo litera dysku nieznana)
'	vollabel: (wazne dla EXT, bo po tym rozpoznaje istnienie)
'	include: List(Of String), exclude: List(Of String)(np.tylko JPG)
'delayed: List(Of picture) ?
'	IMPLEMENT: vblib, JSON na config, kopia w root-path
'	UI: dodaj, in_use yes/no
'	FUNKCJONALNOSC: permanent storage, Save, ApplyTags, Find/Search, GetById (Id na pathname - w srodku STORAGE, app tego nie widzi), GetTagsById, GetPathForId (Do AcdSee itp.)
' na razie bez wyszukiwania, itp., tylko
' * samo buffer->storage,
' * aktualizacja pliku json w katalogu,
' * aktualizacja JSON bufora ("plik przekazany do archiwum"), co daje Purge potem
' * jesli OnePic.IsExifed = false, to pyta czy zapisac tagi do niego


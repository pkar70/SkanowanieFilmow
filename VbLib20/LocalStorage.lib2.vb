

Public Class LocalStorage
	Public Property StorageName As String
	Public Property enabled As Boolean = True
	Public Property Path As String
	Public Property VolLabel As String
	Public Property includeMask As String = "*.jpg;*.tif;*.png" ' maski regexp
	Public Property excludeMask As String  ' maski regexp

	Public Property lastSave As DateTime

	Public Property jsonInDir As Boolean = True
	Public Property saveToExif As Boolean = True

	Public Property tree0Dekada As Boolean = False
	Public Property tree1Rok As Boolean = True
	Public Property tree2Miesiac As Boolean = True
	Public Property tree3Dzien As Boolean
	Public Property tree3DzienWeekDay As Boolean = True
	Public Property tree4Geo As Boolean = True


	''' <summary>
	''' robi domyślną nazwę do zapisu
	''' </summary>
	''' <param name="oOneDir">do jakiego katalogu</param>
	''' <param name="sGeo">string potrzebny przy dzieleniu wedle GEO</param>
	''' <returns></returns>
	Public Function GetWriteFolder(oOneDir As Vblib.OneDir) As String
		Return GetFolder(oOneDir, False)
	End Function

	''' <summary>
	''' znalezienie w archiwum katalogu - gdziekolwiek by on nie był (niezależnie od struktury i rename)
	''' </summary>
	''' <param name="oItem"></param>
	''' <returns></returns>
	Public Function FindRealFolder(oOneDir As Vblib.OneDir) As String
		Return GetFolder(oOneDir, True)
	End Function


	''' <summary>
	''' robi domyślną nazwę do zapisu
	''' </summary>
	''' <param name="oOneDir">do jakiego katalogu</param>
	''' <param name="sGeo">string potrzebny przy dzieleniu wedle GEO</param>
	''' <returns></returns>
	Private Function GetFolder(oOneDir As Vblib.OneDir, bForRead As Boolean) As String

		Dim sPath As String = PicSourceImplement.GetConvertedPathForVol_Folder(VolLabel, Path)
		If sPath = "" Then Return ""

		' 1981.01.23.sb_geo -> 198x
		If tree0Dekada Or bForRead Then
			sPath = FindCreateRealDir(sPath, oOneDir.sId.Substring(0, 3) & "x", Not bForRead)
		End If

		' 1981.01.23.sb_geo -> 1981
		If tree1Rok Or bForRead Then
			sPath = FindCreateRealDir(sPath, oOneDir.sId.Substring(0, 4), Not bForRead)
		End If

		' 1981.01.23.sb_geo -> 1981.01
		If tree2Miesiac Or bForRead Then
			sPath = FindCreateRealDir(sPath, oOneDir.sId.Substring(0, 7), Not bForRead)
		End If

		' 1981.01.23.sb_geo -> 1981.01.23
		If tree3Dzien Or bForRead Then
			sPath = FindCreateRealDir(sPath, oOneDir.sId.Substring(0, 10), Not bForRead)
		End If

		' 1981.01.23.sb_geo -> 1981.01.23.sb
		If tree3DzienWeekDay Or bForRead Then
			sPath = FindCreateRealDir(sPath, oOneDir.sId.Substring(0, 13), Not bForRead)
		End If

		' 1981.01.23.sb_geo -> 1981.01.23.sb_geo
		If tree4Geo Or bForRead Then
			sPath = FindCreateRealDir(sPath, oOneDir.sId, Not bForRead)
		End If

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
			If IO.Path.GetFileName(sFolder).StartsWith(sSubfolderPrefix) Then Return sFolder
		Next

		If Not bCreate Then Return sInFolder

		Dim sSubfolder As String = IO.Path.Combine(sInFolder, sSubfolderPrefix)
		IO.Directory.CreateDirectory(sSubfolder)
		Return sSubfolder
	End Function

	Public Function IsPresent() As Boolean

		Dim sPath As String = PicSourceImplement.GetConvertedPathForVol_Folder(VolLabel, Path)
		If sPath = "" Then Return False ' "nie ma takiego Vollabel"

		Dim oDrive As IO.DriveInfo = New IO.DriveInfo(sPath) ' .Net 2.0
		If Not oDrive.IsReady Then Return False

		Return True
	End Function

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


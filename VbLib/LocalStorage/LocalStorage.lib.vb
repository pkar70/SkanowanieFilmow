﻿

Imports System.IO

Public MustInherit Class LocalStorage
	Implements AnyStorage

	Public Property StorageName As String
	Public Property enabled As Boolean = True
	Public Property Path As String
	Public Property VolLabel As String
	Public Property includeMask As String = OnePic.ExtsPic & OnePic.ExtsMovie & ";*.nar;*jpg.thumb" ' maski regexp
	Public Property excludeMask As String  ' maski regexp

	Public Property lastSave As DateTime

	Public Property jsonInDir As Boolean = True
	'	Public Property saveToExif As Boolean = True

#If False Then
	' tego jednak nie wykorzystujemy, teraz jest inna struktura katalogów
	Public Property tree0Dekada As Boolean = False
	Public Property tree1Rok As Boolean = True
	Public Property tree2Miesiac As Boolean = True
	Public Property tree3Dzien As Boolean
	Public Property tree3DzienWeekDay As Boolean = True
	Public Property tree4Geo As Boolean = True
#End If

#Region "to co musi być w vblib 2.0"
	Public MustOverride Function IsPresent() As Boolean
	Protected MustOverride Function GetConvertedPathForVol(sVolLabel As String, sPath As String) As String
#End Region

#Region "implementacja interface"

#Region "przerzucenie do vblib 2.0"
	Public MustOverride Async Function GetMBfreeSpace() As Task(Of Integer) Implements AnyStorage.GetMBfreeSpace

#End Region

#Region "realne funkcje"

	Private Const NO_MATCH_MASK As String = "nomatch"

	Public Async Function SendFile(oPic As OnePic) As Task(Of String) Implements AnyStorage.SendFileMain
		If oPic.locked Then Return ""
		If Not IsPresent() Then Return "ERROR: archiwum aktualnie jest niewidoczne"

		' zapisz plik, gdy błąd - wróć od razu
		Dim sErr As String = Await SendPhoto(oPic)
		If sErr = NO_MATCH_MASK Then Return ""    ' nie ma błędu, bo po prostu plik spoza maski jest
		If sErr <> "" Then Return sErr  ' błąd

		Dim sJsonContent As String = oPic.DumpAsJSON
		AddToJsonIndex(oPic.TargetDir, sJsonContent)

		Return ""

	End Function

	Public Async Function SendFiles(oPicki As List(Of OnePic), oNextPic As JedenWiecejPlik) As Task(Of String) Implements AnyStorage.SendFiles
		If Not IsPresent() Then Return "ERROR: archiwum aktualnie jest niewidoczne"

		If oPicki Is Nothing Then Return ""
		If oPicki.Count < 1 Then Return ""

		Dim sTargetDir As String = oPicki(0).TargetDir

		Dim sJsonContent As String = ""
		Dim sError As String = ""
		For Each oPic As OnePic In oPicki
			If oPic.locked Then Continue For
			If oPic.TargetDir <> sTargetDir Then Continue For

			Dim temperr As String = Await SendPhoto(oPic)
			oNextPic()
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

	Public Function VerifyFileExists(sTargetDir As String, sFilename As String) As String
		If Not IsPresent() Then Return "ERROR: archiwum aktualnie jest niewidoczne"

		Dim sFolder As String = GetConvertedPathForVol(VolLabel, sTargetDir)

		If String.IsNullOrEmpty(sFolder) Then Return "ERROR: cannot get folder for file"

		Dim sTargetFile As String = IO.Path.Combine(sFolder, sFilename)

		If IO.File.Exists(sTargetFile) Then Return ""

		Return "no file"
	End Function

	Public Function GetRealPath(sTargetDir As String, sFilename As String) As String
		If Not IsPresent() Then Return ""

		Dim sFolder As String = GetConvertedPathForVol(VolLabel, sTargetDir)
		If String.IsNullOrEmpty(sFolder) Then Return ""
		Dim sTargetFile As String = IO.Path.Combine(sFolder, sFilename)
		If Not IO.File.Exists(sTargetFile) Then Return ""
		Return sTargetFile
	End Function


#Disable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
	Public Async Function VerifyFileExist(oPic As OnePic) As Task(Of String) Implements AnyStorage.VerifyFileExist

		Return VerifyFileExists(oPic.TargetDir, oPic.sSuggestedFilename)
	End Function


	Public Async Function VerifyFile(oPic As OnePic, oFromArchive As LocalStorage) As Task(Of String) Implements AnyStorage.VerifyFile
		If Not IsPresent() Then Return "ERROR: archiwum aktualnie jest niewidoczne"

		'Dim sFolder As String = FindRealFolder(oPic.TargetDir)
		Dim sFolder As String = GetConvertedPathForVol(VolLabel, oPic.TargetDir)

		If String.IsNullOrEmpty(sFolder) Then Return "ERROR: cannot get folder for file"

		Dim sTargetFile As String = IO.Path.Combine(sFolder, oPic.sSuggestedFilename)

		If IO.File.Exists(sTargetFile) Then Return ""

		' *TODO* jeśli nie, to spróbuj skopiować z oArchive
		Throw New NotImplementedException()
	End Function

	Public Async Function GetFile(oPic As OnePic) As Task(Of String) Implements AnyStorage.GetFile
#Enable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
		If Not IsPresent() Then Return "ERROR: archiwum aktualnie jest niewidoczne"

		'Dim sFolder As String = FindRealFolder(oPic.TargetDir)
		Dim sFolder As String = GetConvertedPathForVol(VolLabel, oPic.TargetDir)

		If String.IsNullOrEmpty(sFolder) Then Return "ERROR: cannot get folder for file"

		Dim sTargetFile As String = IO.Path.Combine(sFolder, oPic.sSuggestedFilename)

		If Not IO.File.Exists(sTargetFile) Then Return $"ERROR: non existent file {sTargetFile}"

		oPic.oContent = IO.File.Open(sTargetFile, FileMode.Open)

		Return ""
	End Function

	' w archivequerender
	'Public Shared Sub AddToJsonIndexMain(sIndexFilename As String, sContent As String)

	'	If Not IO.File.Exists(sIndexFilename) Then
	'		IO.File.WriteAllText(sIndexFilename, "[")
	'	Else
	'		' skoro już mamy coś w pliku, to teraz dodajemy do tego przecinek - pomiędzy itemami
	'		sContent = "," & vbCrLf & sContent
	'	End If

	'	IO.File.AppendAllText(sIndexFilename, sContent)

	'End Sub

	Private Sub AddToJsonIndex(sDirId As String, sContent As String)
		If Not jsonInDir Then Return

		Dim sFolder As String = GetConvertedPathForVol(VolLabel, sDirId)
		If String.IsNullOrEmpty(sFolder) Then Return
		ArchiveIndex.AddToFolderJsonIndex(sFolder, sContent)

	End Sub

#Disable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
	''' <summary>
	''' wysyła plik oPic do oOneDir, dba o daty pliku w archiwum, odnotowuje archiwizację w oPic.Archived
	''' </summary>
	''' <param name="oPic"></param>
	''' <param name="oOneDir"></param>
	''' <returns>errmessage lub ""</returns>
	Private Async Function SendPhoto(oPic As OnePic) As Task(Of String)
#Enable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously

		If Not OnePic.MatchesMasks(oPic.sSuggestedFilename, includeMask, excludeMask) Then Return NO_MATCH_MASK

		'Dim sFolder As String = FindRealFolder(oPic.TargetDir)
		Dim sFolder As String = GetConvertedPathForVol(VolLabel, oPic.TargetDir)

		If String.IsNullOrEmpty(sFolder) Then Return "ERROR: SendPhoto, cannot get folder for write"

		IO.Directory.CreateDirectory(sFolder)

		Dim sTargetFile As String = IO.Path.Combine(sFolder, oPic.sSuggestedFilename)
		If IO.File.Exists(sTargetFile) Then Return $"ERROR: file {sTargetFile} already exist!"

		oPic.FileCopyTo(sTargetFile)
		' IO.File.Copy(oPic.InBufferPathName, sTargetFile)

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


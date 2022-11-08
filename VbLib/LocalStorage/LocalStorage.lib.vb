Public Class LocalStorage
	Public Property StorageName As String
	Public Property enabled As Boolean = True
	Public Property Path As String  ' znaczenie zmienne w zależności od Type
	Public Property VolLabel As String  ' device MTP, albo vollabel dysku do sprawdzania
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


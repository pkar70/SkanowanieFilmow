Public Class LocalStorage

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


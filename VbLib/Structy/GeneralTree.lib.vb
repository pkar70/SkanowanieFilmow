Public Class GeneralTreeNode
    Public Property Name As String
    Public Property Childs As List(Of GeneralTreeNode)


End Class

Public Class GeneralTree
    Public Property root As GeneralTreeNode

    Private _sFilename As String

    Public Function ReadFromDisk(sFilename As String)
        ' tak, mozna wczytać tylko fragment
    End Function

    Public Function SaveToDisk()

    End Function
End Class


'*) DIRTREE
'	subpath
'ID  ' IDcomponent? przy tworzeniu ID zdjecia moglby robic skladanke z IDów, ale trzeba tez zapewnic move w ditree i bez problemow potem zjandywanie
'defaulttags
'IMPLEMENT: vblib, JSON na config
'	UI: prosty edytor, dodaj TU, oraz open In notepad/VStudio i reload
'	może być dir przestawiany w drzewie, ale ID się nie zmienia? i w filesystem cos jak IDIDIDID_nazwa, nazwa[IDIDIDID], czyli inaczej NODE inaczej LEAF?
'	ID moze byc datą (parowozjada np., czy inny Event), ale moze byc kilka z tą samą datą, więc nie tylko data. Przy skanach data nie zadziała (nie jest znana).
'	historia zmian katalogów - zeby byo latwiej dosjc Do tego skąd ID itp

'*) TAGTREE
'	level0: grupa(osoby, miejsca...) zwierzeta, transport, pora roku (snieg, morze, ...)
'	level1 i w głąb: np.Kraków -Widok - mieszkanie, rodzina - aska, 8a-wz
'	id -JA #WID
'	name Piotr Karocki, Widok
'	IMPLEMENT: vblib, JSON na config
'	UI: prosty edytor, usun/dodaj TU, oraz open In notepad/VStudio i reload
'	STRUCT z używanymi TAGami, i LoadFromFile(filename), SaveToFIle(filename), zmiana tagów To również reapply tagów w serwisach! resave pliku Do storage (external disk)

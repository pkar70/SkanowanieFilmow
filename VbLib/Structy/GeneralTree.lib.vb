
'Public Class OgolneTree
'    Inherits MojaStruct

'    Public Property sId As String
'    'Public Property sDisplayName As String
'    Public Property notes As String
'    Public Property denyPublish As Boolean

'    Public Property SubItems As List(Of OgolneTree)
'    Public Property sParentId As String

'    Public Function ToComboDisplayName() As String
'        If String.IsNullOrWhiteSpace(notes) Then Return sId
'        Dim sRet As String = sId & " ("
'        If notes.Length < 24 Then Return sRet & notes & ")"

'        Return sRet & notes.Substring(0, 23) & "…)"

'    End Function

'    Public Function ToFlatList() As List(Of OneDir)
'        DumpCurrMethod(sId)
'        Dim lista As New List(Of OneDir)

'        lista.Add(Me)

'        If SubItems IsNot Nothing Then
'            For Each oChild As OneDir In SubItems
'                lista = lista.Concat(oChild.ToFlatList).ToList
'            Next
'        End If

'        Return lista
'    End Function

'    Public Function IsFromDate() As Boolean
'        Return IsFromDate(sId)
'    End Function

'    Public Shared Function IsFromDate(sId As String) As Boolean
'        ' daty 1850-2050, format yyyy.MM

'        If sId.Length < 22 Then Return False
'        If sId.Substring(4, 1) <> "." Then Return False

'        Dim temp As Integer
'        Try
'            temp = Integer.Parse(sId.Substring(0, 4))
'        Catch ex As Exception
'            Return False
'        End Try
'        If temp < 1850 Then Return False
'        If temp > Date.Now.Year Then Return False   ' zdjęć z przyszlosci nie uznajemy

'        Try
'            temp = Integer.Parse(sId.Substring(5, 2))
'        Catch ex As Exception
'            Return False
'        End Try

'        If temp < 1 Then Return False
'        If temp > 12 Then Return False

'        Return True

'    End Function



'    Public Const RootId As String = "(root)"

'    Public Function IsRoot() As Boolean
'        Return sId = RootId
'    End Function


'End Class


''Public Class GeneralTreeNode
''    Public Property Name As String
''    Public Property Childs As List(Of GeneralTreeNode)

''End Class

''Public Class GeneralTree
''    Public Property root As GeneralTreeNode

''    Private _sFilename As String

''    Public Function ReadFromDisk(sFilename As String)
''        ' tak, mozna wczytać tylko fragment
''    End Function

''    Public Function SaveToDisk()

''    End Function
''End Class


'''*) DIRTREE
'''	subpath
'''ID  ' IDcomponent? przy tworzeniu ID zdjecia moglby robic skladanke z IDów, ale trzeba tez zapewnic move w ditree i bez problemow potem zjandywanie
'''defaulttags
'''IMPLEMENT: vblib, JSON na config
'''	UI: prosty edytor, dodaj TU, oraz open In notepad/VStudio i reload
'''	może być dir przestawiany w drzewie, ale ID się nie zmienia? i w filesystem cos jak IDIDIDID_nazwa, nazwa[IDIDIDID], czyli inaczej NODE inaczej LEAF?
'''	ID moze byc datą (parowozjada np., czy inny Event), ale moze byc kilka z tą samą datą, więc nie tylko data. Przy skanach data nie zadziała (nie jest znana).
'''	historia zmian katalogów - zeby byo latwiej dosjc Do tego skąd ID itp

'''*) TAGTREE
'''	level0: grupa(osoby, miejsca...) zwierzeta, transport, pora roku (snieg, morze, ...)
'''	level1 i w głąb: np.Kraków -Widok - mieszkanie, rodzina - aska, 8a-wz
'''	id -JA #WID
'''	name Piotr Karocki, Widok
'''	IMPLEMENT: vblib, JSON na config
'''	UI: prosty edytor, usun/dodaj TU, oraz open In notepad/VStudio i reload
'''	STRUCT z używanymi TAGami, i LoadFromFile(filename), SaveToFIle(filename), zmiana tagów To również reapply tagów w serwisach! resave pliku Do storage (external disk)

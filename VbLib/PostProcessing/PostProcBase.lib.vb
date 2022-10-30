' postprocessing
' znak wodny
' podpis widoczny na zdjęciu - narzucony, bądź z (c) brany (z EXIF)
' może tu dać zmniejszanie

Public MustInherit Class PostProcBase
	Public Property Name As String  ' c:\xxxx, MTP\Lumia435, MTP\Lumia650 - per instance
	'Public Property Path As String  ' znaczenie zmienne w zależności od Type
	'Public Property Recursive As Boolean
	'Public Property sourceRemoveDelay As TimeSpan
	'Public Property defaultTags As ExifTag
	'Public Property defaultPublish As List(Of String)   ' lista IDs
	'Public Property include As List(Of String)  ' maski regexp
	'Public Property exclude As List(Of String)  ' maski regexp
	'Public Property lastDownload As DateTime

End Class

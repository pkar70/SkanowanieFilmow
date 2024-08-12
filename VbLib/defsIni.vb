
' ponieważ NIE DZIAŁA pod Uno.Android wczytywanie pliku (apk nie jest rozpakowany?),
' to w ten sposób przekazywanie zawartości pliku INI
' wychodzi na to samo, edycja pliku defaults.ini albo defsIni.lib.vb

Public Class IniLikeDefaults

    Public Const sIniContent As String = "
[main]
uiBakDelayDays=7
uiJpgQuality=90
uiBakDelayDays=7
uiGeoGapInt=20
uiHourGapInt=36
uiGeoGapOn=True
uiHourGapOn=True
uiMaxThumbs=500
uiTree0Dekada=True
uiTree2Miesiac=True
uiCacheThumbs=True
uiSlideShowSeconds=5
uiSerNoDigits=6

uiAzureMaxBatch=500
uiVisualCrossMaxBatch=400
uiBigPicSize=90

uiJsonEnabled=True
uiSqlInstance=(localdb)\MSSQLLocalDB

uiWinFaceMaxAge=90
uiAstroNotWhenWether=True

uiSpellCheck=True

uiWebBuffPicLimit=99

uiTitleSerno=True
uiAutoCrop=True

uiWinFaceMinSize=2

# remark
' remark
; remark
// remark

[debug]
key=value # remark

[app]
; lista z app (bez ustawiania)


[libs]
; lista z pkarmodule
remoteSystemDisabled=false
appFailData=
offline=false
lastPolnocnyTry=
lastPolnocnyOk=

"

End Class

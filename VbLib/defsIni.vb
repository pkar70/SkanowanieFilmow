﻿
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
uiMaxThumb=100

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

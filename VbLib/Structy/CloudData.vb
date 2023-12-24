

' to są dane, które można wczytywać jako lista

Public Class CloudConfig
    Inherits pkar.BaseStruct

    Public Property eTyp As CloudTyp    ' archiwum/publish
    Public Property sProvider As String '   e.g. "Instagram"
    Public Property nazwa As String '   e.g. "Insta imienne", "Insta ukryte"

    Public Property defaultPostprocess As String    ' ";" separated nazwy
    Public Property defaultExif As ExifTag    ' "-" jako kasowanik, reszta - jako override/doklejanie (np. minus jako usunięcie, i to co dalej - doklejane)

    Public Property deleteAfterDays As Integer

    Public Property afterTagChangeBehaviour As AfterChangeBehaviour = AfterChangeBehaviour.ignore
    Public Property afterPicChangeBehaviour As AfterChangeBehaviour = AfterChangeBehaviour.ignore

    Public Property sUsername As String
    Public Property sPswd As String

    Public Property enabled As Boolean = True
    Public Property includeMask As String = "*.jpg;*.tif;*.png" ' maski regexp
    Public Property excludeMask As String  ' maski regexp

    Public Property stereoAnaglyph As Boolean

    Public Property additInfo As String

    Public Property processLikes As Boolean
    Public Property lastSave As DateTime

End Class

Public Enum AfterChangeBehaviour
    ignore = 0
    sendPhoto = 1
    sendMetadata = 2
    sendBoth = 3
End Enum

Public Enum CloudTyp
    archiwum = 0
    publish = 1
End Enum
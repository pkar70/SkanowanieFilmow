
Imports pkar

Public Class SearchQuery
    Inherits BaseStruct

    Public Property ogolne_MinDate As Date
    Public Property ogolne_MaxDate As Date
    Public Property ogolne_IgnoreYear As Boolean
    Public Property ogolne_GUID As String
    Public Property ogolne_Tags As String
    Public Property ogolne_Descriptions As String
    Public Property ogolne_Gdziekolwiek As String

    Public Property ogolne_geo As QueryGeo

    Public Property ogolne_adv_Source As String
    Public Property ogolne_adv_TargetDir As String
    Public Property ogolne_adv_Filename As String
    Public Property ogolne_adv_Published As String
    Public Property ogolne_adv_CloudArchived As String
    Public Property ogolne_adv_TypePic As Boolean = True
    Public Property ogolne_adv_TypeMovie As Boolean = True

    Public Property source_type As Integer
    Public Property source_author As String

    Public Property exif_camera As String

    Public Property ocr As String

    Public Property astro As New QueryAstro

    Public Property faces As New QueryFaces
    Public Property Azure As New QueryTxtNum

    Public Property VCross As New QueryTxtNum

    Public Property ImgwOpad As New QueryTxtNum

    Public Property ImgwKlimat As New QueryTxtNum

    Public Property fullDirs As Boolean

End Class

Public Class QueryGeo
    Public Property AlsoEmpty As Boolean = True
    Public Property Location As BasicGeoposWithRadius
    Public Property Name As String
End Class


Public Class QueryFaces
    Public Property MinCheck As Boolean
    Public Property MinValue As Integer
    Public Property MaxCheck As Boolean
    Public Property MaxValue As Integer

End Class

Public Class QueryTxtNum
    Public Property AlsoEmpty As Boolean = True
    Public Property FldTxt As New QueryPolaTxt4
    'Public Property _HasNum As Boolean
    Public Property FldNum As New QueryPolaNum4

    'Sub New(hasNumFlds As Boolean)
    '    _HasNum = hasNumFlds
    'End Sub

End Class

Public Class QueryAstro
    Inherits BaseStruct

    Public Property AlsoEmpty As Boolean = True
    Public Property MoonCheck As Boolean
    Public Property Moon00 As Boolean = True
    Public Property MoonD25 As Boolean = True
    Public Property MoonD50 As Boolean = True
    Public Property MoonD75 As Boolean = True
    Public Property Moon100 As Boolean = True
    Public Property MoonC75 As Boolean = True
    Public Property MoonC50 As Boolean = True
    Public Property MoonC25 As Boolean = True

    Public Property SunHourMinCheck As Boolean
    Public Property SunHourMinValue As Integer
    Public Property SunHourMaxCheck As Boolean
    Public Property SunHourMaxValue As Integer
End Class

Public Class QueryPolaTxt4
    Inherits BaseStruct

    Public Property p0 As New QueryPoleTxt
    Public Property p1 As New QueryPoleTxt
    Public Property p2 As New QueryPoleTxt
    Public Property p3 As New QueryPoleTxt
End Class

Public Class QueryPoleTxt
    Inherits BaseStruct

    Public Property Name As String
    Public Property Value As String
End Class

Public Class QueryPolaNum4
    Inherits BaseStruct

    Public Property p0 As New QueryPoleNum
    Public Property p1 As New QueryPoleNum
    Public Property p2 As New QueryPoleNum
    Public Property p3 As New QueryPoleNum

End Class

Public Class QueryPoleNum
    Inherits BaseStruct

    Public Property Name As String
    Public Property Min As String
    Public Property Max As String
End Class
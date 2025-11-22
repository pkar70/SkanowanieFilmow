
Imports Newtonsoft.Json
Imports pkar

#Region "query definition"


Public Class SearchQuery
    Inherits BaseStruct

    Public Property nazwa As String
    Public Property ogolne As New QueryOgolne

    Public Property source_type As Integer
    Public Property source_author As String = ""

    Public Property exif_camera As String = ""

    Public Property ocr As String = ""

    Public Property astro As New QueryAstro

    Public Property faces As New QueryFaces
    Public Property Azure As New QueryAzure

    Public Property VCross As New QueryTxtNum

    Public Property ImgwOpad As New QueryTxtNum

    Public Property ImgwKlimat As New QueryTxtNum

    Public Property fullDirs As Boolean

    <JsonIgnore>
    Public ReadOnly Property AsDymek As String
        Get
            Dim txtMe As String = Me.DumpAsJSON(True)
            Dim txtDef As String = (New SearchQuery).DumpAsJSON(True)

            Dim ret As String = ""
            For Each linia As String In txtMe.Split(vbCrLf)
                If txtDef.Contains(linia) Then Continue For
                'If linia.Contains(": """"") Then Continue For
                ret = ret & vbCrLf & linia.Replace(vbCr, "").Replace(vbLf, "")
            Next
            Return ret
        End Get
    End Property


End Class

#Region "pomocnicze sub-query"

Public Class QueryOgolne
    Public Property MinDateCheck As Boolean
    Public Property MinDate As Date
    Public Property MaxDateCheck As Boolean
    Public Property MaxDate As Date
    Public Property IgnoreYear As Boolean
    Public Property MaxDaysRange As Integer
    Public Property GUID As String = ""
    Public Property serno As Integer
    Public Property Reel As String
    Public Property Tags As String = ""
    <JsonIgnore>
    Public Property AllSubTags As List(Of String)

    Public Property Descriptions As String = ""
    Public Property Gdziekolwiek As String = ""

    Public Property geo As New QueryGeo

    Public Property adv As New QueryOgolneAdvanced

End Class

Public Class QueryOgolneAdvanced
    Public Property Source As String = ""
    Public Property TargetDir As String = ""
    Public Property Filename As String = ""
    Public Property AllowedPeers As String = ""
    Public Property Published As String = ""
    Public Property CloudArchived As String = ""
    Public Property TypePic As Boolean = True
    Public Property TypeMovie As Boolean = True
    Public Property TypeStereo As Boolean = True
End Class

Public Class QueryGeo
    Public Property AlsoEmpty As Boolean = True
    Public Property Location As BasicGeoposWithRadius
    Public Property Name As String = ""
    Public Property OnlyExact As Boolean
End Class


Public Class QueryFaces
    Public Property AlsoEmpty As Boolean
    Public Property MinCheck As Boolean
    Public Property MinValue As Integer
    Public Property MaxCheck As Boolean
    Public Property MaxValue As Integer

End Class

Public Class QueryAzure
    Public Property AlsoEmpty As Boolean = True
    Public Property Brands As String = ""
    Public Property Categories As String = ""
    Public Property Objects As String = ""
    Public Property Landmarks As String = ""
    Public Property Tags As String = ""
    Public Property Celebrities As String = ""
    Public Property Captions As String = ""
    Public Property DominantColorBackground As String = ""
    Public Property DominantColorForeground As String = ""
    Public Property DominantColors As String = ""
    Public Property Wiekowe As String = ""

    Public Property Anywhere As String = ""

    Public Property SkipBW As Boolean = False
    Public Property SkipColor As Boolean = False

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

    Public Property Name As String = ""
    Public Property Value As String = ""
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

    Public Property Name As String = ""
    Public Property Min As String = ""
    Public Property Max As String = ""
End Class

#End Region
#End Region


'Public Class QueryList
'    Inherits pkar.BaseList(Of OneKeyword)

'    Public Sub New(sFolder As String)
'        MyBase.New(sFolder, "queries.json")
'    End Sub

'End Class

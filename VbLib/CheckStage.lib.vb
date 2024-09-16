
' sprawdzanie kolejnych etapów

Public MustInherit Class SequenceStageBase

    Public MustOverride ReadOnly Property Nazwa As String
    Public Overridable ReadOnly Property Dymek As String = ""
    Public Overridable ReadOnly Property AutoCheck As Boolean = False
    Public MustOverride ReadOnly Property StageNo As Integer
    Public MustOverride ReadOnly Property Icon As String

    Public Property IsRequired As Boolean

    Public MustOverride Function Check(picek As OnePic) As Boolean

    Public Overridable Function Check(picki As List(Of OnePic)) As Boolean
        Return picki.All(Function(x) Check(x))
    End Function

End Class


Public Class SequenceStage_AutoExif
    Inherits SequenceStageBase

    Public Overrides ReadOnly Property Nazwa As String = "Run AutoExif"
    Public Overrides ReadOnly Property AutoCheck As Boolean = True
    Public Overrides ReadOnly Property StageNo As Integer = SequenceStages.AutoExif
    Public Overrides ReadOnly Property Icon As String = "A"

    Public Overrides ReadOnly Property Dymek As String = "Potrzebne do Crop/Rotate (obrót zapisany w JFIF), tak samo wyciągnięcie Geo"


    Public Overrides Function Check(picek As OnePic) As Boolean
        If picek.GetExifOfType(ExifSource.FileExif) IsNot Nothing Then Return True

        Dim procek As Vblib.AutotaggerBase = Vblib.GetTagger(ExifSource.FileExif)
        Return Not procek.CanTag(picek)

    End Function

    Public Overrides Function Check(picki As List(Of OnePic)) As Boolean
        Dim bez As IEnumerable(Of OnePic) = picki.Where(Function(x) x.GetExifOfType(ExifSource.FileExif) Is Nothing)

        If bez.Count < 1 Then Return True

        For Each picek As OnePic In bez
            If Not Check(picek) Then Return False
        Next

        Return True
    End Function

End Class

Public Class SequenceStage_CropRotate
    Inherits SequenceStageBase

    Public Overrides ReadOnly Property Nazwa As String = "Crop & Rotate"
    Public Overrides ReadOnly Property StageNo As Integer = SequenceStages.CropRotate
    Public Overrides ReadOnly Property Icon As String = "↻"

    Public Overrides Function Check(picek As OnePic) As Boolean
        Return False
    End Function

    Public Overrides Function Check(picki As List(Of OnePic)) As Boolean
        Return False
    End Function

End Class

Public Class SequenceStage_Keywords
    Inherits SequenceStageBase

    Public Overrides ReadOnly Property Nazwa As String = "Add keywords"
    Public Overrides ReadOnly Property StageNo As Integer = SequenceStages.Keywords
    Public Overrides ReadOnly Property Dymek As String = "Przed Autotag, bo z Kwd jest np. Geo (do pogody)"
    Public Overrides ReadOnly Property Icon As String = "#"

    Public Overrides Function Check(picek As OnePic) As Boolean
        Return Not String.IsNullOrWhiteSpace(picek.sumOfKwds)
    End Function
    Public Overrides Function Check(picki As List(Of OnePic)) As Boolean
        Return Not picki.Any(Function(x) String.IsNullOrWhiteSpace(x.sumOfKwds))
    End Function
End Class

Public Class SequenceStage_Dates
    ' (ale ja tu false)
    Inherits SequenceStageBase

    Public Overrides ReadOnly Property Nazwa As String = "Set dates"
    Public Overrides ReadOnly Property StageNo As Integer = SequenceStages.Dates
    Public Overrides ReadOnly Property Dymek As String = "Przed Autotag, bo data jest potrzebna np. do pogody"
    Public Overrides ReadOnly Property Icon As String = "📆"

    Public Overrides Function Check(picek As OnePic) As Boolean
        If picek.HasRealDate Then Return True
        If picek.GetExifOfType(ExifSource.ManualDate) IsNot Nothing Then Return True

        Return False
    End Function
    'Public Overrides Function Check(picki As List(Of OnePic)) As Boolean
    '    Return False
    'End Function
End Class

Public Class SequenceStage_Geotags
    ' (ale ja tu false)
    Inherits SequenceStageBase

    Public Overrides ReadOnly Property Nazwa As String = "Add geotags"
    Public Overrides ReadOnly Property StageNo As Integer = SequenceStages.Geotags
    Public Overrides ReadOnly Property Dymek As String = "Po Kwd lepiej, bo część geo pójdzie z Kwd i nie trzeba ustawiać"
    Public Overrides ReadOnly Property AutoCheck As Boolean = True
    Public Overrides ReadOnly Property Icon As String = "🚩"

    Public Overrides Function Check(picek As OnePic) As Boolean
        Return picek.sumOfGeo IsNot Nothing
    End Function
    Public Overrides Function Check(picki As List(Of OnePic)) As Boolean
        Return Not picki.Any(Function(x) x.sumOfGeo Is Nothing)
    End Function
End Class

Public Class SequenceStage_AutoTaggers
    Inherits SequenceStageBase

    Public Overrides ReadOnly Property Nazwa As String = "Run AutoTaggers"
    Public Overrides ReadOnly Property StageNo As Integer = SequenceStages.AutoTaggers
    Public Overrides ReadOnly Property Icon As String = "A"

    Public Overrides Function Check(picek As OnePic) As Boolean
        Dim autoSelect As String = Vblib.GetSettingsString("uiRequiredAutoTags")

        Dim nazwy As String() = autoSelect.Split("|")

        For Each nazwa As String In nazwy
            If picek.GetExifOfType(nazwa) Is Nothing Then Return False
        Next

        Return True

    End Function
    'Public Overrides Function Check(picki As List(Of OnePic)) As Boolean
    '    Return False
    'End Function
End Class

Public Class SequenceStage_Descriptions
    Inherits SequenceStageBase

    Public Overrides ReadOnly Property Nazwa As String = "Add descriptions"
    Public Overrides ReadOnly Property StageNo As Integer = SequenceStages.Descriptions
    Public Overrides ReadOnly Property Icon As String = "💬"

    Public Overrides Function Check(picek As OnePic) As Boolean
        Return Not String.IsNullOrWhiteSpace(picek.sumOfDescr)
    End Function
    Public Overrides Function Check(picki As List(Of OnePic)) As Boolean
        Return Not picki.Any(Function(x) String.IsNullOrWhiteSpace(x.sumOfDescr))
    End Function
End Class

Public Class SequenceStage_TargetDir
    Inherits SequenceStageBase

    Public Overrides ReadOnly Property Nazwa As String = "Set TargetDir"
    Public Overrides ReadOnly Property AutoCheck As Boolean = True
    Public Overrides ReadOnly Property StageNo As Integer = SequenceStages.TargetDir
    Public Overrides ReadOnly Property Icon As String = "📂"

    Public Overrides Function Check(picek As OnePic) As Boolean
        Return Not String.IsNullOrWhiteSpace(picek.TargetDir)
    End Function
    Public Overrides Function Check(picki As List(Of OnePic)) As Boolean
        Return Not picki.Any(Function(x) String.IsNullOrWhiteSpace(x.TargetDir))
    End Function
End Class

Public Class SequenceStage_Publish
    Inherits SequenceStageBase

    Public Overrides ReadOnly Property Nazwa As String = "Publish"
    Public Overrides ReadOnly Property StageNo As Integer = SequenceStages.Publish
    Public Overrides ReadOnly Property Icon As String = "🏛"

    Public Overrides Function Check(picek As OnePic) As Boolean
        Return False ' bo niby co może tu zrobić? wszak mogą być wielokrotne...
    End Function
    Public Overrides Function Check(picki As List(Of OnePic)) As Boolean
        Return False
    End Function
End Class

Public Class SequenceStage_CloudArch
    Inherits SequenceStageBase

    Public Overrides ReadOnly Property Nazwa As String = "Cloud archive"
    Public Overrides ReadOnly Property AutoCheck As Boolean = True
    Public Overrides ReadOnly Property StageNo As Integer = SequenceStages.CloudArch
    Public Overrides ReadOnly Property Icon As String = "☁"

    Public Overrides Function Check(picek As OnePic) As Boolean
        Dim ileArchiwow As Integer = Vblib.LibgCloudArchives.Count
        If ileArchiwow < 1 Then Return True

        If String.IsNullOrWhiteSpace(picek.CloudArchived) Then Return False
        Return picek.CloudArchived.Split(";").Count >= ileArchiwow

    End Function
    'Public Overrides Function Check(picki As List(Of OnePic)) As Boolean
    '    Return False
    'End Function
End Class

Public Class SequenceStage_LocalArch
    Inherits SequenceStageBase

    Public Overrides ReadOnly Property Nazwa As String = "Local archive"
    Public Overrides ReadOnly Property AutoCheck As Boolean = True
    Public Overrides ReadOnly Property StageNo As Integer = SequenceStages.LocalArch
    Public Overrides ReadOnly Property Dymek As String = "Na końcu, bo wtedy w metadanych jest zapis do cloud i publish"
    Public Overrides ReadOnly Property Icon As String = "💾"

    Public Overrides Function Check(picek As OnePic) As Boolean
        Return False
        'Dim ileArchiwow As Integer = 0 ' ile ma być archiwów *TODO*
        'If ileArchiwow < 1 Then Return True

        'If String.IsNullOrWhiteSpace(picek.Archived) Then Return False
        'Return picek.Archived.Split(";").Count >= ileArchiwow
    End Function
    Public Overrides Function Check(picki As List(Of OnePic)) As Boolean
        Return False
    End Function
End Class


Public Class SequenceStageAzureCheck
    Inherits SequenceStageBase

    Public Overrides ReadOnly Property Nazwa As String = "Check Azure data"
    Public Overrides ReadOnly Property StageNo As Integer = SequenceStages.AzureCheck
    Public Overrides ReadOnly Property Dymek As String = "Po Azure - sprawdzić rozpoznanie Brands, Landmarks, Celebrities, GoryPic; skopiować ADULT do kwd=X"
    Public Overrides ReadOnly Property Icon As String = "🗷"

    Public Overrides Function Check(picek As OnePic) As Boolean
        Return False
        'Dim ileArchiwow As Integer = 0 ' ile ma być archiwów *TODO*
        'If ileArchiwow < 1 Then Return True

        'If String.IsNullOrWhiteSpace(picek.Archived) Then Return False
        'Return picek.Archived.Split(";").Count >= ileArchiwow
    End Function
    Public Overrides Function Check(picki As List(Of OnePic)) As Boolean
        Return False
    End Function
End Class

' nie da się zrobić Shared z MustOverride, a chcę wykorzystywac levele symbolicznie a nie numerycznie później
Public Enum SequenceStages
    AutoExif = 10
    CropRotate = 20
    Keywords = 30
    Dates = 40
    Geotags = 50
    AutoTaggers = 60
    AzureCheck = 65
    Descriptions = 70
    TargetDir = 80
    Publish = 90
    CloudArch = 100
    LocalArch = 110
End Enum
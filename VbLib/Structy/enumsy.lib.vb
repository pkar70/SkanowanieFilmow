<Flags>
Public Enum PipelineEnum
    unknown = 0
    changed = 1
    buffer = 256
    hastags = 512
    archived = 1024
    webarchived = 2048
    published = 4096
End Enum

Public Enum FileSourceDeviceTypeEnum
    unknown = 0
    scannerTransparent = 1
    scannerReflex = 2
    digital = 3
    internet = 11
End Enum

Public Class RestrictionsEnum
    Public Const forAll As String = "" ' unrestricted (=U)
    Public Const family As String = "c" ' confidential
    Public Const onlyAuthor As String = "s" ' secret
End Class

' gdzie jest (0,0)
Public Enum OrientationEnum
    topLeft = 1
    topRight = 2
    bottomRight = 3
    bottomLeft = 4
    leftTop = 5
    rightTop = 6
    rightBottom = 7
    leftBottom = 8
End Enum

Public Enum PicSourceType
    NONE = 0
    FOLDER = 1
    MTP = 2
    AdHOC = 3
    PeerSrv = 10
    Inet = 11
End Enum

Public Class ExifSource
    Public Const SourceDefault As String = "SOURCE_DEFAULT"
    Public Const SourceFile As String = "SOURCE_FILEATTR"
    Public Const SourceDescriptIon As String = "SOURCE_DESCRIPT.ION"
    Public Const FileExif As String = "AUTO_EXIF"
    Public Const FullExif As String = "AUTO_FULLEXIF"
    Public Const AutoWinFace As String = "AUTO_WINFACE"
    Public Const AutoOSM As String = "AUTO_OSM_POI"
    Public Const AutoImgw As String = "AUTO_GEONAME_PL"
    Public Const ManualGeo As String = "MANUAL_GEO" ' kopiowanie GeoTag pomiędzy zdjęciami
    Public Const ManualTag As String = "MANUAL_TAG"
    Public Const ManualDate As String = "MANUAL_DATE"
    Public Const AutoAzure As String = "AUTO_AZURE"
    ' Public Const ManualRotate As String = "MANUAL_ROTATE"
    Public Const Flattened As String = "INTERNAL_FLATTENED"
    Public Const CloudPublish As String = "CLOUD_PUBLISH"
    Public Const FilenameToTags As String = "AUTO_TAG_FROM_NAME"
    Public Const AutoWinOCR As String = "AUTO_WINOCR"
    Public Const AutoGuid As String = "AUTO_GUID"
    Public Const AutoVisCrosWeather As String = "AUTO_WEATHER"
    Public Const AutoAstro As String = "AUTO_ASTRO"
    Public Const AutoMoon As String = "AUTO_MOONPHASE"
    Public Const AutoHydro As String = "AUTO_HYDRO"
    Public Const AutoMeteoOpad As String = "AUTO_METEO_OPAD"
    Public Const AutoMeteoKlimat As String = "AUTO_METEO_KLIMAT"

End Class

Public Enum AutoTaggerType
    Local = 1
    WebPublic = 2
    WebAccount = 3
End Enum

Public Class GuidPrefix
    Public Const DateTaken As String = "t"
    Public Const FileDate As String = "f"
    Public Const ScannedDate As String = "s"
End Class


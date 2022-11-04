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
End Enum

Public Class ExifSource
    Public Const SourceDefault As String = "SOURCE_DEFAULT"
    Public Const SourceFile As String = "SOURCE_FILEATTR"
    Public Const SourceDescriptIon As String = "SOURCE_DESCRIPT.ION"
    Public Const FileExif As String = "FILE_EXIF"
End Class

Public Enum AutoTaggerType
    Local = 1
    WebPublic = 2
    WebAccount = 3
End Enum

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

Public Enum RestrictionsEnum
    forAll = 0
    family = 1
    onlyAuthor = 255
End Enum

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



Public Class PublishMetadataOptions
    Inherits pkar.BaseStruct

    Public Property PicLimit As Integer

    Public Property PrintKwd As Boolean
    Public Property PrintDescr As Boolean
    Public Property PrintGeo As Boolean
    Public Property PrintGeoName As Boolean
    Public Property PrintFilename As Boolean
    Public Property PrintSerno As Boolean
    Public Property PrintReel As Boolean
    Public Property PrintDates As Boolean
    Public Property PrintOCR As Boolean

    Public Property AllLinks As Boolean

    Public Property noHttpLog As Boolean


    Public Sub SaveAsDefaults()
        SetSettingsInt("PublishMetadataPicLimit", PicLimit)

        SetSettingsBool("PublishMetadataPrintKwd", PrintKwd)
        SetSettingsBool("PublishMetadataPrintDescr", PrintDescr)
        SetSettingsBool("PublishMetadataPrintGeo", PrintGeo)
        SetSettingsBool("PublishMetadataPrintFilename", PrintFilename)
        SetSettingsBool("PublishMetadataPrintSerno", PrintSerno)
        SetSettingsBool("PublishMetadataPrintReel", PrintReel)
        SetSettingsBool("PublishMetadataPrintDates", PrintDates)
        SetSettingsBool("PublishMetadataPrintOCR", PrintOCR)
        SetSettingsBool("PublishMetadataAllLinks ", AllLinks)
        SetSettingsBool("PublishMetadatanoHttpLog", noHttpLog)
    End Sub

    Public Sub LoadDefaults()
        PicLimit = GetSettingsInt("PublishMetadataPicLimit")

        PrintDescr = GetSettingsBool("PublishMetadataPrintDescr")
        PrintGeo = GetSettingsBool("PublishMetadataPrintGeo")
        PrintFilename = GetSettingsBool("PublishMetadataPrintFilename")
        PrintSerno = GetSettingsBool("PublishMetadataPrintSerno")
        PrintReel = GetSettingsBool("PublishMetadataPrintReel")
        PrintDates = GetSettingsBool("PublishMetadataPrintDates")
        PrintOCR = GetSettingsBool("PublishMetadataPrintOCR")
        AllLinks = GetSettingsBool("PublishMetadataAllLinks ")
        noHttpLog = GetSettingsBool("PublishMetadatanoHttpLog")
    End Sub

    Public Shared Function GetDefault() As PublishMetadataOptions
        Dim ret As New PublishMetadataOptions
        ret.LoadDefaults()
        Return ret
    End Function


End Class

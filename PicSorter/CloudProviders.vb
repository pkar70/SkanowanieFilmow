
Imports System.Reflection
Imports pkar
Imports Vblib

Public Class CloudPublishersList
    ' Inherits Vblib.MojaLista(Of CloudConfig)

    ' New CloudPubl_std14_SSC.Cloud_Skyscraper,

    Private gCloudProviders As Vblib.CloudPublish() = {
        New Vblib.Publish_AdHoc,
        New Publish_Email,
        New Cloud_Skyscraper,
        New Publish_Instagram.Publish_Instagram,
        New lib_n6_publishPDF.Publish_PDF,
        New Publish_ZIP,
        New Publish_CBZ
        }

    'New Publish_std2_Facebook.Publish_Facebook_Post,
    'New Publish_std2_Facebook.Publish_Facebook_Album,
    ' PowerPointa i tak nie używam, a jest to 40 MB w .exe
    'New lib_n6_PowerPoint.Publish_PowerPoint
    '}

    Private gCloudPublishers As List(Of Vblib.CloudPublish)

    Private gCloudConfigs As New BaseList(Of Vblib.CloudConfig)(vblib.GetDataFolder, "cloudPublishers.json")

    Private _DataDir As String

    Public Sub New()
        _DataDir = Vblib.GetDataFolder
    End Sub

    Public Function Load() As Boolean

        gCloudConfigs.Load()

        gCloudPublishers = New List(Of Vblib.CloudPublish)

        Dim bMamAdHoc As Boolean = False
        For Each oItem As Vblib.CloudConfig In gCloudConfigs
            Dim oNew As Vblib.CloudPublish = GetCloudPublishInstantion(oItem, _DataDir)
            If oNew IsNot Nothing Then
                gCloudPublishers.Add(oNew)
                If oNew.konfiguracja.sProvider.ContainsCI("adhoc") Then bMamAdHoc = True
            End If
        Next

        'mamy jakieś AdHoc, więc go nie musimy dodawać
        If bMamAdHoc Then Return True

        ' ale ono zawsze powinno być, więc dodajemy
        Dim oNewPubl As Vblib.CloudPublish = New Vblib.Publish_AdHoc
        oNewPubl.konfiguracja = Vblib.Publish_AdHoc.DefaultConfig
        gCloudPublishers.Add(oNewPubl)
        oNewPubl = New Vblib.Publish_ZIP
        oNewPubl.konfiguracja = Vblib.Publish_ZIP.DefaultConfig
        gCloudPublishers.Add(oNewPubl)
        oNewPubl = New Vblib.Publish_CBZ
        oNewPubl.konfiguracja = Vblib.Publish_CBZ.DefaultConfig
        gCloudPublishers.Add(oNewPubl)

        Return True
    End Function

    Public Function Save(Optional bIgnoreNulls As Boolean = False) As Boolean
        gCloudConfigs.Clear()

        For Each oItem As Vblib.CloudPublish In gCloudPublishers
            gCloudConfigs.Add(oItem.konfiguracja)
        Next

        Return gCloudConfigs.Save(bIgnoreNulls)
    End Function

    Private Function GetCloudPublishInstantion(oConfig As Vblib.CloudConfig, sDataDir As String) As Vblib.CloudPublish

        For Each oProvider As Vblib.CloudPublish In gCloudProviders
            If oProvider.sProvider = oConfig.sProvider Then
                Return oProvider.CreateNew(oConfig, Vblib.gPostProcesory, sDataDir)
            End If
        Next

        Return Nothing
    End Function

    Public Function GetList() As List(Of Vblib.CloudPublish)
        Return gCloudPublishers
    End Function

    Public Function GetProvidersList() As List(Of Vblib.CloudPublish)
        Return gCloudProviders.ToList
    End Function


    Public Sub Remove(oItem As Vblib.CloudPublish)
        gCloudPublishers.Remove(oItem)
    End Sub

    Public Sub Add(oNewConfig As Vblib.CloudConfig)
        gCloudPublishers.Add(GetCloudPublishInstantion(oNewConfig, _DataDir))
    End Sub

End Class



Public Class CloudArchivesList
    ' Inherits Vblib.MojaLista(Of CloudConfig)

    Private gCloudProviders As Vblib.CloudArchive() = {
            New CloudArch_std14_Chomikuj.Cloud_Chomikuj
           }
    'New CloudArch_std14_Degoo.Cloud_Degoo,
    'New CloudArch_std14_Shutterfly.Cloud_Shutterfly
    ' }

    'Private gCloudArchives As List(Of Vblib.CloudArchive)

    Private Const CLOUDARCH_FILENAME As String = "cloudArchives.json"
    Private gCloudConfigs As New BaseList(Of Vblib.CloudConfig)(vblib.GetDataFolder, CLOUDARCH_FILENAME)

    Private _DataDir As String

    Public Sub New()
        _DataDir = Vblib.GetDataFolder
    End Sub

    Public Function Load() As Boolean

        gCloudConfigs.Load()

        Vblib.LibgCloudArchives = New List(Of Vblib.CloudArchive)

        For Each oItem As Vblib.CloudConfig In gCloudConfigs
            Dim oNew As Vblib.CloudArchive = GetCloudInstantion(oItem)
            If oNew IsNot Nothing Then
                Vblib.LibgCloudArchives.Add(oNew)
            End If
        Next

        Return True
    End Function

    Private Sub CopyToOneDrive(sSourceFileName As String, sSettName As String)
        If Not Vblib.GetSettingsBool(sSettName) Then Return

        Dim srcFile As String = Globs.GetDataFile("", sSourceFileName, False)
        If Not IO.File.Exists(srcFile) Then Return

        Dim dstFile As String = IO.Path.Combine(Globs.GetOneDriveFolder, sSourceFileName)

        IO.File.Copy(srcFile, dstFile, True)
    End Sub

    Public Function Save(Optional bIgnoreNulls As Boolean = False) As Boolean
        gCloudConfigs.Clear()

        For Each oItem As Vblib.CloudArchive In Vblib.LibgCloudArchives
            gCloudConfigs.Add(oItem.konfiguracja)
        Next

        Dim bRet As Boolean = gCloudConfigs.Save(bIgnoreNulls)
        CopyToOneDrive(CLOUDARCH_FILENAME, "uiUseOneDrive")
        Return bRet
    End Function


    Private Function GetCloudInstantion(oConfig As Vblib.CloudConfig) As Vblib.CloudArchive

        For Each oProvider As Vblib.CloudArchive In gCloudProviders
            If oProvider.sProvider = oConfig.sProvider Then
                Return oProvider.CreateNew(oConfig, Vblib.gPostProcesory, _DataDir)
            End If
        Next

        Return Nothing
    End Function

    Public Function GetList() As List(Of Vblib.CloudArchive)
        Return Vblib.LibgCloudArchives
    End Function

    Public Function GetProvidersList() As List(Of Vblib.CloudArchive)
        Return gCloudProviders.ToList
    End Function

    Public Sub Remove(oItem As Vblib.CloudArchive)
        Vblib.LibgCloudArchives.Remove(oItem)
    End Sub
    Public Sub Add(oNewConfig As Vblib.CloudConfig)
        Vblib.LibgCloudArchives.Add(GetCloudInstantion(oNewConfig))
    End Sub

End Class



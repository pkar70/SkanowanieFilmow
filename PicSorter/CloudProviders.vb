﻿
Imports System.Reflection
Imports pkar
'Imports Vblib


Public Class CloudPublishersList
    ' Inherits Vblib.MojaLista(Of CloudConfig)

    Private gCloudProviders As Vblib.CloudPublish() = {
        New Vblib.Publish_AdHoc,
        New Publish_Email,
        New CloudPubl_std14_SSC.Cloud_Skyscraper,
        New Publish_Instagram.Publish_Instagram,
        New Publish_std2_Facebook.Publish_Facebook_Post,
        New Publish_std2_Facebook.Publish_Facebook_Album
        }

    Private gCloudPublishers As List(Of Vblib.CloudPublish)

    Private gCloudConfigs As New BaseList(Of Vblib.CloudConfig)(Application.GetDataFolder, "cloudPublishers.json")

    Private _DataDir As String

    Public Sub New(sDataDir As String)
        _DataDir = sDataDir
    End Sub

    Public Function Load() As Boolean

        gCloudConfigs.Load()

        gCloudPublishers = New List(Of Vblib.CloudPublish)

        Dim bMamAdHoc As Boolean = False
        For Each oItem As Vblib.CloudConfig In gCloudConfigs.GetList
            Dim oNew As Vblib.CloudPublish = GetCloudPublishInstantion(oItem, _DataDir)
            If oNew IsNot Nothing Then
                gCloudPublishers.Add(oNew)
                If oNew.konfiguracja.sProvider.ToLowerInvariant.Contains("adhoc") Then bMamAdHoc = True
            End If
        Next

        'mamy jakieś AdHoc, więc go nie musimy dodawać
        If bMamAdHoc Then Return True

        ' ale ono zawsze powinno być, więc dodajemy
        Dim oNewAdHoc As New Vblib.Publish_AdHoc
        oNewAdHoc.konfiguracja = Vblib.Publish_AdHoc.DefaultConfig
        gCloudPublishers.Add(oNewAdHoc)

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
                Return oProvider.CreateNew(oConfig, Application.gPostProcesory, sDataDir)
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

    Private gCloudArchives As List(Of Vblib.CloudArchive)

    Private Const CLOUDARCH_FILENAME As String = "cloudArchives.json"
    Private gCloudConfigs As New BaseList(Of Vblib.CloudConfig)(Application.GetDataFolder, CLOUDARCH_FILENAME)

    Private _DataDir As String

    Public Sub New(sDataDir As String)
        _DataDir = sDataDir
    End Sub

    Public Function Load() As Boolean

        gCloudConfigs.Load()

        gCloudArchives = New List(Of Vblib.CloudArchive)

        For Each oItem As Vblib.CloudConfig In gCloudConfigs.GetList
            Dim oNew As Vblib.CloudArchive = GetCloudInstantion(oItem)
            If oNew IsNot Nothing Then
                gCloudArchives.Add(oNew)
            End If
        Next

        Return True
    End Function

    Public Shared Sub CopyToOneDrive(sSourceFileName As String, sSettName As String)
        If Not Vblib.GetSettingsBool(sSettName) Then Return

        Dim srcFile As String = IO.Path.Combine(Application.GetDataFolder, sSourceFileName)
        If Not IO.File.Exists(srcFile) Then Return

        Dim dstFile As String = IO.Path.Combine(Application.GetOneDrivePath, sSourceFileName)

        IO.File.Copy(srcFile, dstFile, True)
    End Sub

    Public Function Save(Optional bIgnoreNulls As Boolean = False) As Boolean
        gCloudConfigs.Clear()

        For Each oItem As Vblib.CloudArchive In gCloudArchives
            gCloudConfigs.Add(oItem.konfiguracja)
        Next

        Dim bRet As Boolean = gCloudConfigs.Save(bIgnoreNulls)
        CopyToOneDrive(CLOUDARCH_FILENAME, "uiUseOneDrive")
        Return bRet
    End Function


    Private Function GetCloudInstantion(oConfig As Vblib.CloudConfig) As Vblib.CloudArchive

        For Each oProvider As Vblib.CloudArchive In gCloudProviders
            If oProvider.sProvider = oConfig.sProvider Then
                Return oProvider.CreateNew(oConfig, Application.gPostProcesory, _DataDir)
            End If
        Next

        Return Nothing
    End Function

    Public Function GetList() As List(Of Vblib.CloudArchive)
        Return gCloudArchives
    End Function

    Public Function GetProvidersList() As List(Of Vblib.CloudArchive)
        Return gCloudProviders.ToList
    End Function

    Public Sub Remove(oItem As Vblib.CloudArchive)
        gCloudArchives.Remove(oItem)
    End Sub
    Public Sub Add(oNewConfig As Vblib.CloudConfig)
        gCloudArchives.Add(GetCloudInstantion(oNewConfig))
    End Sub

End Class



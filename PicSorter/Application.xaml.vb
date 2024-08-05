
Imports vb14 = Vblib.pkarlibmodule14
Imports pkar
Imports Vblib
Imports System.Reflection


Partial Class Application

    ' Application-level events, such as Startup, Exit, and DispatcherUnhandledException
    ' can be handled in this file.

#Disable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
    Public Async Function AppServiceLocalCommand(sCommand As String) As Task(Of String)
#Enable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
        Return ""
    End Function

    Public Shared Sub ShowWait(bShow As Boolean)
        If bShow Then
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait
        Else
            Mouse.OverrideCursor = Nothing
        End If
    End Sub


    Public Shared Function GetOneDrivePath() As String
        Dim sOneDrivePath As String = Environment.GetEnvironmentVariable("OneDriveConsumer")
        If sOneDrivePath Is Nothing Then Return ""

        If Not IO.Directory.Exists(sOneDrivePath) Then Return ""

        sOneDrivePath = IO.Path.Combine(sOneDrivePath, "Apps")
        If Not IO.Directory.Exists(sOneDrivePath) Then IO.Directory.CreateDirectory(sOneDrivePath)

        Dim appName As String = GetAppName()

        sOneDrivePath = IO.Path.Combine(sOneDrivePath, appName)
        If Not IO.Directory.Exists(sOneDrivePath) Then IO.Directory.CreateDirectory(sOneDrivePath)

        Return sOneDrivePath
    End Function


    Private Shared gSourcesList As lib_PicSource.PicSourceList

    Public Shared Function GetSourcesList() As lib_PicSource.PicSourceList

        If gSourcesList Is Nothing Then
            'Vblib.PicSourceBase._dataFolder = Application.GetDataFolder ' żeby JSON mógł wczytać sources
            gSourcesList = New lib_PicSource.PicSourceList
            gSourcesList.Load()
            gSourcesList.InitDataDirectory()
        End If
        Return gSourcesList
    End Function


    Private Shared gArchiveList As BaseList(Of lib_PicSource.LocalStorageMiddle)
    Public Shared Function GetArchivesList() As BaseList(Of lib_PicSource.LocalStorageMiddle)

        If gArchiveList Is Nothing OrElse gArchiveList.Count < 1 Then
            gArchiveList = New BaseList(Of lib_PicSource.LocalStorageMiddle)(Vblib.GetDataFolder, "archives.json")
            gArchiveList.Load()
        End If
        Return gArchiveList
    End Function


#Region "Cloud publish/archives"

    Private Shared gCloudPublishers As CloudPublishersList

    Public Shared Function GetCloudPublishers() As CloudPublishersList
        If gCloudPublishers Is Nothing Then
            gCloudPublishers = New CloudPublishersList
            gCloudPublishers.Load()
        End If
        Return gCloudPublishers
    End Function


    Private Shared gCloudArchives As CloudArchivesList

    Public Shared Function GetCloudArchives() As CloudArchivesList
        If gCloudArchives Is Nothing Then
            gCloudArchives = New CloudArchivesList
            gCloudArchives.Load()
        End If
        Return gCloudArchives
    End Function
#End Region



    Public Shared gDbase As New Databases()

    Public Shared gWcfServer As lib_sharingNetwork.ServerWrapper

End Class

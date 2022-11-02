
' ponieważ nie może być hierarchia PicSourceBase + dziedziczone z niej PicSource* dla różnych typów (bo JSON nie potrafi wczytać, bo nie wie którą klasę ma instancję zrobić...)


Imports Vblib

Public Class PicSourceImplement
    Inherits Vblib.PicSourceBase

    Public Sub New()
        MyBase.New(Vblib.PicSourceType.NONE, Vblib.PicSourceBase._dataFolder)
    End Sub

    Public Sub New(typSource As Vblib.PicSourceType, sDataFolder As String)
        MyBase.New(typSource, sDataFolder)
    End Sub

    Protected Overrides Function IsPresent_Main() As Boolean
        Select Case Typ
            Case Vblib.PicSourceType.MTP
                Return IsPresent_MTP()
            Case Else
                Return IsPresent_Folder()
        End Select
    End Function

    Protected Overrides Function ReadDirectory_Main() As Integer
        Select Case Typ
            Case Vblib.PicSourceType.MTP
                Return ReadDirectory_MTP()
            Case Else
                Return ReadDirectory_Folder()
        End Select
    End Function

    Protected Overrides Function DeleteFile(sId As String) As Boolean
        Select Case Typ
            Case Vblib.PicSourceType.MTP
                Return DeleteFile_MTP(sId)
            Case Else
                Return DeleteFile_Folder(sId)
        End Select
    End Function

    Protected Overrides Function OpenFile(oPic As OnePic) As Boolean
        Select Case Typ
            Case Vblib.PicSourceType.MTP
                Dim oFI As MediaDevices.MediaFileInfo = _MediaDevice.GetFileInfo(oPic.sInSourceID)
                oPic.Content = oFI.OpenRead()
                Return True
            Case Else
                If Not IO.File.Exists(oPic.sInSourceID) Then Return False
                oPic.Content = IO.File.Open(oPic.sInSourceID, IO.FileMode.Open, IO.FileAccess.Read)
                Return True
        End Select
    End Function

#Region "Folder"
    Private Function ReadDirectory_Folder() As Integer
        If Not IsPresent_Main() Then Return -1
        If Not IO.Directory.Exists(Path) Then Return -1

        Dim iCnt As Integer = 0
        _listaPlikow.Clear()

        Dim dateMin As Date = currentExif.DateMin

        Dim allfiles As String() = IO.Directory.GetFiles(Path, "*", IO.SearchOption.AllDirectories)
        For Each sFilePathName As String In allfiles
            ' uwzględnianie masek include oraz exclude
            Dim sFileName As String = IO.Path.GetFileName(sFilePathName)
            If MatchesMasks(sFileName) Then
                Dim oNew As New Vblib.OnePic(SourceName, sFilePathName, sFileName)
                oNew.Exifs.Add(currentExif)

                Dim oExif As New Vblib.ExifTag(Vblib.ExifSource.SourceFile)
                Dim createDate As Date = IO.File.GetCreationTime(sFilePathName)
                If createDate < dateMin Then createDate = dateMin
                Dim writeDate As Date = IO.File.GetLastWriteTime(sFilePathName)
                If writeDate < dateMin Then writeDate = dateMin

                If createDate < writeDate Then
                    oExif.DateMax = writeDate
                    oExif.DateMin = createDate
                Else
                    oExif.DateMin = writeDate
                    oExif.DateMax = createDate
                End If
                dateMin = oExif.DateMin

                oNew.Exifs.Add(oExif)

                _listaPlikow.Add(oNew)
            End If
        Next

        Return _listaPlikow.Count
    End Function

    Private Function DeleteFile_Folder(sId As String) As Boolean
        If Not IsPresent_Main() Then Return False
        If Not sId.ToLower.StartsWith(Path.ToLower) Then Return False
        If Not IO.Directory.Exists(Path) Then Return False
        IO.File.Delete(sId)

        Return True
    End Function


    Public Shared Function GetConvertedPathForVol_Folder(sVolLabel As String, sPath As String) As String
        sVolLabel = sVolLabel.ToLowerInvariant

        Dim oDrives = IO.DriveInfo.GetDrives()
        For Each oDrive As IO.DriveInfo In oDrives
            If oDrive.IsReady AndAlso oDrive.VolumeLabel.ToLowerInvariant = sVolLabel Then
                If String.IsNullOrWhiteSpace(sPath) Then Return oDrive.RootDirectory.Name
                Return oDrive.RootDirectory.Name.Substring(0, 1) & sPath.Substring(1)
            End If
        Next

        Return ""
    End Function

    ''' <summary>
    ''' w me.PATH ustawia drive:\ na to gdzie jest aktualnie wymagany VolLabel
    ''' </summary>
    ''' <returns></returns>
    Private Function PoprawPathWedleVolLabel_Folder(sVolLabel As String) As Boolean
        sVolLabel = sVolLabel.ToLowerInvariant

        Dim sPath As String = GetConvertedPathForVol_Folder(sVolLabel, Path)
        If sPath = "" Then Return False

        Path = sPath
        Return True
    End Function


    Private Function IsPresent_Folder() As Boolean

        If Not PoprawPathWedleVolLabel_Folder(VolLabel) Then Return False  ' "nie ma takiego Vollabel"

        Dim oDrive As IO.DriveInfo = New IO.DriveInfo(Path) ' .Net 2.0
        If Not oDrive.IsReady Then Return False

        Return True
    End Function

#End Region

#Region "MTP"
    Private Function ReadDirectory_MTP_Recursion(sSrcPath As String) As Boolean

        If _MediaDevice Is Nothing Then Return False    ' nie powinno się zdarzyć, chyba że będzie błąd w programowaniu

        For Each sDir As String In _MediaDevice.EnumerateDirectories(Path)
            If Not ReadDirectory_MTP_Recursion(sDir) Then Return False
        Next

        For Each sFilePathName As String In _MediaDevice.EnumerateFiles(sSrcPath)
            Dim sFileName As String = IO.Path.GetFileName(sFilePathName)

            If MatchesMasks(sFileName) Then

                Dim oNew As New Vblib.OnePic(SourceName, sFilePathName, sFileName)
                oNew.Exifs.Add(currentExif)

                ' i teraz daty sprobuj sciagnac
                Dim oFI As MediaDevices.MediaFileInfo = _MediaDevice.GetFileInfo(sFilePathName)

                Dim oExif As New Vblib.ExifTag(Vblib.ExifSource.SourceFile)
                Dim createDate = oFI.CreationTime
                Dim writeDate = oFI.LastWriteTime
                If createDate < writeDate Then
                    oExif.DateMax = writeDate
                    oExif.DateMin = createDate
                Else
                    oExif.DateMin = writeDate
                    oExif.DateMax = createDate
                End If
                oNew.Exifs.Add(oExif)

                _listaPlikow.Add(oNew)
            End If

        Next

        Return True

    End Function


    Private Function ReadDirectory_MTP() As Integer
        If Not IsPresent_MTP() Then Return -1

        Dim iCnt As Integer = 0
        If _listaPlikow Is Nothing Then
            _listaPlikow = New List(Of OnePic)
        Else
            _listaPlikow.Clear()
        End If

        If Not ReadDirectory_MTP_Recursion(Path) Then Return -1

        '_MediaDevice.Disconnect()
        '_MediaDevice.Dispose()
        '_MediaDevice = Nothing

        Return _listaPlikow.Count
    End Function

    Private Function DeleteFile_MTP(sId As String) As Boolean
        If _MediaDevice Is Nothing Then Return False    ' nie powinno się zdarzyć, chyba że będzie błąd w programowaniu

        _MediaDevice.DeleteFile(sId)
        Return True
    End Function


    <Newtonsoft.Json.JsonIgnore>
    Private _MediaDevice As MediaDevices.MediaDevice

    Public Shared Function GetDeviceFromLabel_MTP(sVolLabel As String) As MediaDevices.MediaDevice
        sVolLabel = sVolLabel.ToLowerInvariant
        If String.IsNullOrWhiteSpace(sVolLabel) Then Return Nothing
        Dim iInd As Integer = sVolLabel.IndexOf("(")
        If iInd > 0 Then sVolLabel = sVolLabel.Substring(0, iInd - 1).Trim

        For Each oMD As MediaDevices.MediaDevice In MediaDevices.MediaDevice.GetDevices
            If oMD.FriendlyName.ToLowerInvariant = sVolLabel Then Return oMD
        Next

        Return Nothing
    End Function

    Private Function CheckDeviceFromVolLabel_MTP(sVolLabel As String) As Boolean

        Dim oMD As MediaDevices.MediaDevice = GetDeviceFromLabel_MTP(sVolLabel)
        If oMD Is Nothing Then Return False
        _MediaDevice = oMD
        oMD.Connect(MediaDevices.MediaDeviceAccess.GenericRead Or MediaDevices.MediaDeviceAccess.GenericWrite)

        Return True
    End Function

    Private Function IsPresent_MTP() As Boolean
        Return CheckDeviceFromVolLabel_MTP(VolLabel)
    End Function



#End Region

End Class

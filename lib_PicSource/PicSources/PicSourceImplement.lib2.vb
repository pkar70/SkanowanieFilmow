
' ponieważ nie może być hierarchia PicSourceBase + dziedziczone z niej PicSource* dla różnych typów (bo JSON nie potrafi wczytać, bo nie wie którą klasę ma instancję zrobić...)


Imports Vblib
Imports MediaHelper = Lib_mediaDevices

Public Class PicSourceImplement
    Inherits Vblib.PicSourceBase

    'Public Sub New()
    '    MyBase.New(Vblib.PicSourceType.NONE, Vblib.PicSourceBase._dataFolder)
    'End Sub

    ''' <summary>
    ''' datafolder potrzebny na plik PURGE, może być NULL
    ''' </summary>
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

    Private Function GetDownloadFolder() As String
        Dim path As String = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        Return IO.Path.Combine(path, "Downloads")
    End Function

    Protected Overrides Function DeleteFile(sId As String) As Boolean
        Vblib.pkarlibmodule14.DumpCurrMethod(sId)
        Select Case Typ
            Case PicSourceType.Inet
                Dim path As String = GetDownloadFolder()
                If Not IO.Directory.Exists(path) Then Return False
                path = IO.Path.Combine(path, sId)
                IO.File.Delete(path)
                Return True
            Case Vblib.PicSourceType.MTP
                Return DeleteFile_MTP(sId)
            Case Else
                Return DeleteFile_Folder(sId)
        End Select
    End Function

    Protected Overrides Function OpenFile(oPic As OnePic) As Boolean
        Select Case Typ
            Case Vblib.PicSourceType.MTP
                oPic.oContent = _MediaDeviceHelper.GetStream(oPic.sInSourceID)
                Return True
            Case Else
                If Not IO.File.Exists(oPic.sInSourceID) Then Return False
                oPic.oContent = IO.File.Open(oPic.sInSourceID, IO.FileMode.Open, IO.FileAccess.Read)
                Return True
        End Select
    End Function

#Region "Folder oraz Inet"
    Private Function ReadDirectory_Folder() As Integer
        DumpCurrMethod()
        If Not IsPresent_Main() Then Return -1
        If Typ <> PicSourceType.Inet Then
            If Not IO.Directory.Exists(Path) Then Return -2
        End If

        Dim iCnt As Integer = 0
        _listaPlikow?.Clear()
        _listaPlikow = New List(Of OnePic)

        Dim dateMin As Date = currentExif.DateMin

        Dim srchopts As IO.SearchOption = IO.SearchOption.TopDirectoryOnly
        If Typ <> PicSourceType.Inet AndAlso Recursive Then srchopts = IO.SearchOption.AllDirectories

        Dim allfiles As String()
        If Typ = PicSourceType.Inet Then
            allfiles = IO.Directory.GetFiles(GetDownloadFolder)
        Else
            allfiles = IO.Directory.GetFiles(Path, "*", srchopts)
        End If

        DumpMessage("got allfiles array, starting to iterate")

        For Each sFilePathName As String In allfiles
            ' uwzględnianie masek include oraz exclude
            Dim sFileName As String = IO.Path.GetFileName(sFilePathName)
            DumpMessage("got file: " & sFileName)

            If OnePic.MatchesMasks(sFileName, includeMask, excludeMask) Then

                Dim oNew As New Vblib.OnePic(SourceName, sFilePathName, sFileName)
                oNew.Exifs.Add(currentExif.Clone) ' clone, bo jakby potem gdzieś zmieniać (jak było w InetDownld), to by się zmieniało wszędzie

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

        Try
            IO.File.Delete(sId)
            Return True
        Catch ex As Exception

        End Try

        Return False
    End Function


    Public Shared Function GetConvertedPathForVol_Folder(sVolLabel As String, sPath As String, sTargetDir As String) As String
        sVolLabel = sVolLabel.ToLowerInvariant
        Dim iInd As Integer = sVolLabel.IndexOf("(")
        If iInd > 0 Then sVolLabel = sVolLabel.Substring(0, iInd).Trim

        Dim oDrives = IO.DriveInfo.GetDrives() 'DriveInfo: .Net Std 2.0
        For Each oDrive As IO.DriveInfo In oDrives
            If oDrive.IsReady AndAlso oDrive.VolumeLabel.ToLowerInvariant = sVolLabel Then
                If String.IsNullOrWhiteSpace(sPath) Then Return oDrive.RootDirectory.Name
                Return IO.Path.Combine(oDrive.RootDirectory.Name.Substring(0, 1) & sPath.Substring(1), sTargetDir)
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

        Dim sPath As String = GetConvertedPathForVol_Folder(sVolLabel, Path, "")
        If sPath = "" Then Return False

        Path = sPath
        Return True
    End Function


    Private Function IsPresent_Folder() As Boolean
        If Typ = PicSourceType.Inet Then Return True
        If Not PoprawPathWedleVolLabel_Folder(VolLabel) Then Return False  ' "nie ma takiego Vollabel"

        Dim oDrive As IO.DriveInfo = New IO.DriveInfo(Path) ' .Net 2.0
        If Not oDrive.IsReady Then Return False

        Return True
    End Function

#End Region

#Region "MTP"

    ' ale to właściwie tylko proxy do ClassLib Std 2.0 z nugetem, żeby ten nuget był tylko tam

    Private Function ReadDirectory_MTP() As Integer
        DumpCurrMethod()
        Dim temp As Boolean = IsPresent_MTP()
        DumpMessage($"zwrot z present: {temp}")
        If Not temp Then Return -1

        DumpMessage("mam MTP")
        _listaPlikow = _MediaDeviceHelper.ReadDirectory(Path, SourceName, includeMask, excludeMask, currentExif)
        If _listaPlikow Is Nothing Then Return -2

        Return _listaPlikow.Count
    End Function

    Private Function DeleteFile_MTP(sId As String) As Boolean
        If Not sId.Contains("\") Then
            ' mappedsource tak robi - bez ścieżki w pliku purge
            sId = Path & "\" & sId
        End If
        Return _MediaDeviceHelper.Delete(sId)
    End Function


    <Newtonsoft.Json.JsonIgnore>
    Private _MediaDeviceHelper As MediaHelper.Helper
    Private _MediaVolLabel As String


    Private Function CheckDeviceFromVolLabel_MTP(sVolLabel As String) As Boolean
        If _MediaDeviceHelper IsNot Nothing Then
            If sVolLabel = _MediaVolLabel Then Return True
            ' inny VolLabel - trzeba rozłączyć i poszukać nowego
            _MediaDeviceHelper.Disconnect()
            _MediaDeviceHelper = Nothing
        End If

        Try
            DumpCurrMethod(sVolLabel)
            Dim oMD As New MediaHelper.Helper(sVolLabel)
            If Not oMD.IsValid Then Return False
            _MediaDeviceHelper = oMD
            _MediaVolLabel = sVolLabel
            DumpMessage("tuż przed Connect")
            _MediaDeviceHelper.Connect()
            DumpMessage("już po Connect")

            Return True
        Catch ex As Exception
            DumpMessage($"catch error {ex.Message}")
        End Try
        Return False
    End Function

    Private Function IsPresent_MTP() As Boolean
        DumpCurrMethod()
        Dim temp As Boolean = CheckDeviceFromVolLabel_MTP(VolLabel)
        DumpMessage("powrocilem")
        DumpMessage($"ret from CheckDev: {temp}")
        Return temp
    End Function



#End Region

End Class


' docelowo: wszystko co ma zwi¹zek z MediaDevices (¿eby ten nuget by³ tylko w tym jednym Project)


Imports System.IO
Imports MediaDevices

Public Class Helper

    Public Shared Function GetDevicesList() As List(Of String)
        Dim lRet As New List(Of String)

        For Each oMD As MediaDevices.MediaDevice In MediaDevices.MediaDevice.GetDevices
            If oMD.DeviceId.ToLower.StartsWith("\\?\usb#") Then
                lRet.Add(oMD.FriendlyName & " (" & oMD.Description & ")")
            End If  ' if(po USB)
        Next

        Return lRet
    End Function

    Private _oMD As MediaDevices.MediaDevice

    Public Sub New(sVolLabel As String)
        _oMD = GetDeviceFromLabel_MTP(sVolLabel)
    End Sub

    Public Function IsValid() As Boolean
        Return _oMD IsNot Nothing
    End Function

    Public Sub Connect()
        _oMD?.Connect(MediaDevices.MediaDeviceAccess.GenericRead Or MediaDevices.MediaDeviceAccess.GenericWrite)
    End Sub

    Public Function Delete(sPathname As String)
        If _oMD Is Nothing Then Return False
        _oMD.DeleteFile(sPathname)
        Return True
    End Function

    Public Function GetStream(sPathname As String) As IO.Stream
        Dim oFI As MediaDevices.MediaFileInfo = _oMD?.GetFileInfo(sPathname)
        If oFI Is Nothing Then Return Nothing
        Return oFI.OpenRead()
    End Function

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

    Public Function GetDirList(sForDir As String) As List(Of String)
        If _oMD Is Nothing Then Return Nothing

        Dim lista As New List(Of String)

        _oMD.Connect()

        If sForDir = "\" Then
            ' z MTPget , bo inaczej pokazuje np. \Apps i tak dalej
            Dim oDrives As MediaDevices.MediaDriveInfo() = _oMD.GetDrives()
            For Each oDrive As MediaDevices.MediaDriveInfo In oDrives
                lista.Add(IO.Path.GetFileName(oDrive.Name))
            Next

        Else
            For Each sDir As String In _oMD.EnumerateDirectories(sForDir)
                lista.Add(IO.Path.GetFileName(sDir))
            Next
        End If

        _oMD.Disconnect()

        Return lista
    End Function

    Private _listaPlikow As List(Of Vblib.OnePic)
    Public Function ReadDirectory(sInitialPath As String, sSourceName As String, sIncludeMask As String, sExcludeMask As String, oCurrentExif As Vblib.ExifTag) As List(Of Vblib.OnePic)
        _listaPlikow = New List(Of Vblib.OnePic)

        If Not ReadDirectory_Recursion(sInitialPath, sSourceName, sIncludeMask, sExcludeMask, oCurrentExif) Then Return Nothing

        Return _listaPlikow
    End Function

    Private Function ReadDirectory_Recursion(sSrcPath As String, sSourceName As String, sIncludeMask As String, sExcludeMask As String, oCurrentExif As Vblib.ExifTag) As Boolean

        If _oMD Is Nothing Then Return Nothing    ' nie powinno siê zdarzyæ, chyba ¿e bêdzie b³¹d w programowaniu

        For Each sDir As String In _oMD.EnumerateDirectories(sSrcPath)
            If Not ReadDirectory_Recursion(sDir, sSourceName, sIncludeMask, sExcludeMask, oCurrentExif) Then Return Nothing
        Next

        For Each sFilePathName As String In _oMD.EnumerateFiles(sSrcPath)
            Dim sFileName As String = IO.Path.GetFileName(sFilePathName)

            If Vblib.PicSourceBase.MatchesMasks(sFileName, sIncludeMask, sExcludeMask) Then

                Dim oNew As New Vblib.OnePic(sSourceName, sFilePathName, sFileName)
                oNew.Exifs.Add(oCurrentExif)

                ' i teraz daty sprobuj sciagnac
                Dim oFI As MediaDevices.MediaFileInfo = _oMD.GetFileInfo(sFilePathName)

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

End Class

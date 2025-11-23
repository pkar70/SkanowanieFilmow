
' docelowo: wszystko co ma zwi¹zek z MediaDevices (¿eby ten nuget by³ tylko w tym jednym Project)

Imports System.IO
Imports System.Reflection.Metadata
Imports MediaDevices
Imports vb14 = Vblib.pkarlibmodule14
Imports pkar.DotNetExtensions

Public Class Helper

    Public Shared Function GetDevicesList() As List(Of String)
        Dim lRet As New List(Of String)

        For Each oMD As MediaDevices.MediaDevice In MediaDevices.MediaDevice.GetDevices
            If oMD.DeviceId.StartsWithCI("\\?\usb#") Then
                lRet.Add(oMD.FriendlyName & " (" & oMD.Description & ")")
            End If  ' if(po USB)
        Next

        Return lRet
    End Function

    Private _oMD As MediaDevices.MediaDevice

    Public Sub New(sVolLabel As String)
        vb14.DumpCurrMethod(sVolLabel)
        _oMD = GetDeviceFromLabel_MTP(sVolLabel)
    End Sub

    Public Function IsValid() As Boolean
        Return _oMD IsNot Nothing
    End Function

    Public Sub Connect()
        vb14.DumpCurrMethod()
        If _oMD Is Nothing Then
            vb14.DumpMessage("Nie mogê CONNECT bo _oMD jest Nothing")
            Exit Sub
        End If

        If _oMD.IsConnected Then
            vb14.DumpMessage("Ju¿ jestem po³¹czony")
            Exit Sub
        End If

        _oMD?.Connect(MediaDevices.MediaDeviceAccess.GenericRead Or MediaDevices.MediaDeviceAccess.GenericWrite)
        '_oMD?.Connect(MediaDevices.MediaDeviceAccess.GenericRead)
    End Sub

    Public Sub Disconnect()
        vb14.DumpCurrMethod()
        If _oMD Is Nothing Then
            vb14.DumpMessage("Nie mogê DISCONNECT bo _oMD jest Nothing")
            Exit Sub
        End If
        If Not _oMD.IsConnected Then
            vb14.DumpMessage("Ju¿ jestem roz³¹czony")
            Exit Sub
        End If
        _oMD?.Disconnect()
    End Sub


    Public Function Delete(sPathname As String)
        If _oMD Is Nothing Then Return False
        Try
            _oMD.DeleteFile(sPathname)
            Return True
        Catch ex As Exception

        End Try
        Return False
    End Function

    Public Function GetStream(sPathname As String) As IO.Stream
        vb14.DumpCurrMethod()
        Dim oFI As MediaDevices.MediaFileInfo
        Try
            ' 2023.07.28, doda³em TRY bo wyskakiwa³
            ' Exception thrown: 'System.Runtime.InteropServices.COMException' in MediaDevices.dll 0x80042003
            ' Lumia 950XL WP_20230724_12_02_35_Rich.jpg
            oFI = _oMD?.GetFileInfo(sPathname)
        Catch ex As Exception
            oFI = Nothing
        End Try

        If oFI Is Nothing Then Return Nothing

        Try
            Return oFI.OpenRead()
        Catch ex As Exception
            Return Nothing
        End Try

    End Function

    Public Shared Function GetDeviceFromLabel_MTP(sVolLabel As String) As MediaDevices.MediaDevice
        vb14.DumpCurrMethod(sVolLabel)
        sVolLabel = sVolLabel.ToLowerInvariant
        If String.IsNullOrWhiteSpace(sVolLabel) Then Return Nothing

        ' przypadek szczególny: moto G(50)
        If sVolLabel.StartsWithCI("moto g(50)") Then
            sVolLabel = "moto g(50)"
        Else
            Dim iInd As Integer = sVolLabel.IndexOf("(")
            If iInd > 0 Then sVolLabel = sVolLabel.Substring(0, iInd - 1).Trim
        End If

        vb14.DumpMessage("searching For '" & sVolLabel & "'")

        For Each oMD As MediaDevices.MediaDevice In MediaDevices.MediaDevice.GetDevices
            vb14.DumpMessage($"device: {oMD.FriendlyName.ToLowerInvariant}")
            If oMD.FriendlyName.EqualsCI(sVolLabel) Then Return oMD
        Next

        vb14.DumpMessage("nie znalaz³em!")
        Return Nothing
    End Function

    ''' <summary>
    ''' Zwraca listê katalogów w danym katalogu na urz¹dzeniu MTP
    ''' </summary>
    Public Function GetDirList(sForDir As String) As List(Of String)
        vb14.DumpCurrMethod(sForDir)
        If _oMD Is Nothing Then Return Nothing

        Dim lista As New List(Of String)

        Connect()

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
        Return lista.OrderBy(Of String)(Function(s) s).ToList()
    End Function

    Private _listaPlikow As List(Of Vblib.OnePic)
    Public Function ReadDirectory(sInitialPath As String, sSourceName As String, sIncludeMask As String, sExcludeMask As String, oCurrentExif As Vblib.ExifTag) As List(Of Vblib.OnePic)
        vb14.DumpCurrMethod(sInitialPath)
        _listaPlikow = New List(Of Vblib.OnePic)

        If Not ReadDirectory_Recursion(sInitialPath, sSourceName, sIncludeMask, sExcludeMask, oCurrentExif) Then Return Nothing

        Return _listaPlikow.ToList()
    End Function

    Private Function ReadDirectory_Recursion(sSrcPath As String, sSourceName As String, sIncludeMask As String, sExcludeMask As String, oCurrentExif As Vblib.ExifTag) As Boolean

        If _oMD Is Nothing Then Return False    ' nie powinno siê zdarzyæ, chyba ¿e bêdzie b³¹d w programowaniu

        Try

            For Each sDir As String In _oMD.EnumerateDirectories(sSrcPath)
                If Not ReadDirectory_Recursion(sDir, sSourceName, sIncludeMask, sExcludeMask, oCurrentExif) Then Return Nothing
            Next

            For Each sFilePathName As String In _oMD.EnumerateFiles(sSrcPath).OrderBy(Of String)(Function(s) s)
                Dim sFileName As String = IO.Path.GetFileName(sFilePathName)

                If Vblib.OnePic.MatchesMasks(sFileName, sIncludeMask, sExcludeMask) Then

                    Dim oNew As New Vblib.OnePic(sSourceName, sFilePathName, sFileName)
                    oNew.Exifs.Add(oCurrentExif)

                    ' i teraz daty sprobuj sciagnac
                    Dim oFI As MediaDevices.MediaFileInfo = _oMD.GetFileInfo(sFilePathName)
                    Dim oExif As New Vblib.ExifTag(Vblib.ExifSource.SourceFile)
                    Dim createDate As Date? = oFI.CreationTime
                    Dim writeDate As Date? = oFI.LastWriteTime

                    If createDate.HasValue AndAlso writeDate.HasValue Then

                        If createDate < writeDate Then
                            oExif.DateMax = writeDate.Value
                            oExif.DateMin = createDate.Value
                        Else
                            oExif.DateMin = writeDate.Value
                            oExif.DateMax = createDate.Value
                        End If
                        oNew.Exifs.Add(oExif)
                    Else
                        ' czyli albo tylko jedno, albo ¿adno nie ma daty
                        If createDate.HasValue Then
                            oExif.DateMax = createDate.Value
                            oExif.DateMin = createDate.Value
                        End If

                        If writeDate.HasValue Then
                            oExif.DateMax = writeDate.Value
                            oExif.DateMin = writeDate.Value
                        End If
                    End If

                    _listaPlikow.Add(oNew)
                End If

            Next

            Return True

        Catch
            Return False
        End Try

    End Function

End Class

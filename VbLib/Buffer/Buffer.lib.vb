



Imports System.ComponentModel
Imports System.IO

Public Class Buffer
    Private _RootDataPath As String
    Private _rootPictures As String
    Private _pliki As FilesInBuffer
    Public Sub New(sRootDataPath As String)
        _RootDataPath = sRootDataPath
        _pliki = New FilesInBuffer(_RootDataPath)
        _pliki.Load()
        ' AddSortBy
        'AddTyp3()
        _rootPictures = GetSettingsString("uiFolderBuffer")
    End Sub

#If False Then
    ' *TODO* to później można wyłączyć, bo to uzupełnia to co się powinno zrobić wcześniej
    Private Sub AddSortBy()
        Dim bBylyZmiany As Boolean = False
        For Each oItem As OnePic In _pliki.GetList
            If Not String.IsNullOrWhiteSpace(oItem.sortOrder) Then Continue For

            bBylyZmiany = True
            Dim oExif As ExifTag = oItem.GetExifOfType(ExifSource.FileExif)
            If oExif IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(oExif.DateTimeOriginal) Then
                ' 2022.05.06 12:27:47
                oItem.sortOrder = oExif.DateTimeOriginal
            Else
                oExif = oItem.GetExifOfType(ExifSource.SourceFile)
                ' 2022-05-06T12:27:48
                If oExif IsNot Nothing AndAlso oExif.DateMax.IsDateValid Then
                    oItem.sortOrder = oExif.DateMax.ToExifString 'ToString("yyyy.MM.dd HH.mm.ss")
                End If
            End If
        Next

        If bBylyZmiany Then SaveData()
    End Sub
#End If

    Private Sub AddTyp3()
        For Each oItem As OnePic In _pliki.GetList

            Dim oExif As ExifTag = oItem.GetExifOfType(ExifSource.SourceDefault)
            oExif.FileSourceDeviceType = FileSourceDeviceTypeEnum.digital
        Next

        SaveData()
    End Sub



    Public Sub SaveData()
        _pliki.Save(True)
    End Sub

    Public Function Count() As Integer
        Return _pliki.Count
    End Function

    Public Function GetList() As List(Of OnePic)
        Return _pliki.GetList
    End Function

    Public Function BakDelete(iDays As Integer, bRealDelete As Boolean) As Boolean
        Dim oDate As Date = Date.Now.AddDays(-iDays)

        Dim aFiles As String() = IO.Directory.GetFiles(_rootPictures, "*.bak")

        Dim iOutdatedCnt As Integer = 0
        For Each sFile As String In aFiles
            If IO.File.GetCreationTime(sFile) < oDate Then
                iOutdatedCnt += 1
                If bRealDelete Then IO.File.Delete(sFile)
            End If
        Next

        Return iOutdatedCnt
    End Function

    ''' <summary>
    ''' tu nie ma Save - bo jeśli kasujemy serię, to zapis lepiej zrobić tylko raz
    ''' nie usuwa też ze źródła (AddToPurge), gdyż VbLib nie widzi SourceList - użyj PicSourceList.AddToPurge
    ''' </summary>
    ''' <param name="oPic"></param>
    Public Function DeleteFile(oPic As OnePic) As Boolean
        Try
            IO.File.Delete(oPic.InBufferPathName)
            _pliki.Remove(oPic)
            Return True
        Catch ex As Exception

        End Try
        Return False
    End Function


    Private Async Function IsSameFileContents(oStream1 As Stream, oStream2 As Stream) As Task(Of Boolean)
        ' This is not merely an optimization, as incrementing one stream's position
        ' should Not affect the position of the other.
        If oStream1.Equals(oStream2) Then Return True

        If oStream1.Length <> oStream2.Length Then Return False

        Dim oBuf1 As Byte() = New Byte(4100) {}
        Dim oBuf2 As Byte() = New Byte(4100) {}

        Do
            Dim iBytes1 As Integer = Await oStream1.ReadAsync(oBuf1, 0, 4096)
            Dim iBytes2 As Integer = Await oStream2.ReadAsync(oBuf2, 0, 4096)

            If iBytes1 = 0 Then Return True

            For iLp As Integer = 0 To iBytes1
                If oBuf1(iLp) <> oBuf2(iLp) Then Return False
            Next

        Loop

        Return True
    End Function

    ''' <summary>
    ''' zabierz plik (skorzystaj z OnePic.oContent - stream do pliku)
    ''' </summary>
    ''' <param name="pic">tu jest wazne suggestedfileName oraz oContent</param>
    ''' <returns>OnePic uzupelniony o BufferFileName, FALSE: error (lub ten plik z tą zawartością już jest)</returns>
    Public Async Function AddFile(oPic As OnePic) As Task(Of Boolean)
        DumpCurrMethod($"({oPic.sSuggestedFilename}")

        Dim sDstPathName As String = IO.Path.Combine(_rootPictures, oPic.sSuggestedFilename)

        If IO.File.Exists(sDstPathName) Then
            ' na MTP nie ma Seek, wiec musimy to zrobić przez plik pośredni

            Dim sTempName As String = sDstPathName & ".tmp"
            Dim oTempStream = IO.File.Create(sTempName, 1024 * 1024)
            Await oPic.oContent.CopyToAsync(oTempStream, 1024 * 1024)

            Dim oExistingStream As Stream = IO.File.OpenRead(sDstPathName)
            oTempStream.Seek(0, SeekOrigin.Begin)

            Dim bSame As Boolean = Await IsSameFileContents(oTempStream, oExistingStream)
            oExistingStream.Dispose()

            If bSame Then
                DumpMessage("File already exists, same content")
                Return False
            End If
            DumpMessage("File already exists, but another content - renaming")

            Dim sDstTmp As String
            Dim iInd As Integer
            iInd = sDstPathName.LastIndexOf(".")
            sDstTmp = sDstPathName.Substring(0, iInd) & Date.Now.ToString("yyMMdd") & sDstPathName.Substring(iInd)
            If IO.File.Exists(sDstTmp) Then
                sDstTmp = sDstPathName.Substring(0, iInd) & Date.Now.ToString("yyMMddHHmmss") & sDstPathName.Substring(iInd)
                If IO.File.Exists(sDstTmp) Then
                    Return False ' file already exist, i nawet z doklejaniem daty - nic sie nie da zrobic
                End If
            End If
            sDstPathName = sDstTmp

            IO.File.Move(sTempName, sDstPathName)
        Else
            Dim oWriteStream = IO.File.Create(sDstPathName, 1024 * 1024)
            Await oPic.oContent.CopyToAsync(oWriteStream, 1024 * 1024)

            Await oWriteStream.FlushAsync
            oWriteStream.Dispose()
        End If


        Dim oExif As ExifTag = oPic.GetExifOfType(ExifSource.SourceFile)
        IO.File.SetCreationTime(sDstPathName, oExif.DateMin)
        IO.File.SetLastWriteTime(sDstPathName, oExif.DateMax)

        oPic.InBufferPathName = sDstPathName
        ' oPic.sortOrder = oExif.DateMax.ToExifString 'ToString("yyyy.MM.dd HH.mm.ss")


        If oPic.MatchesMasks("*.nar") Then
            oPic.fileTypeDiscriminator = "*"
        End If

        If oPic.MatchesMasks("*.avi") Then
            oPic.fileTypeDiscriminator = "►"
        End If
        If oPic.MatchesMasks("*.mov") Then
            oPic.fileTypeDiscriminator = "►"
        End If
        If oPic.MatchesMasks("*.mp4") Then
            oPic.fileTypeDiscriminator = "►"
        End If


        _pliki.Add(oPic)

        Return True

    End Function


    Public Sub ResetPipelines()
        For Each oItem As OnePic In _pliki.GetList
            oItem.ResetPipeline()
        Next
    End Sub

    Public Class FilesInBuffer
        Inherits Vblib.MojaLista(Of OnePic)

        Public Sub New(sFolder As String)
            MyBase.New(sFolder, "buffer.json")
        End Sub

    End Class


End Class




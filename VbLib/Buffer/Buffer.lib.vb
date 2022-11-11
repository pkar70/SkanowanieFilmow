



Public Class Buffer
    Private _RootDataPath As String
    Private _rootPictures As String
    Private _pliki As FilesInBuffer
    Public Sub New(sRootDataPath As String)
        _RootDataPath = sRootDataPath
        _pliki = New FilesInBuffer(_RootDataPath)
        _pliki.Load()
        _rootPictures = GetSettingsString("uiFolderBuffer")
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


    ''' <summary>
    ''' zabierz plik (skorzystaj z OnePic.Content - stream do pliku)
    ''' </summary>
    ''' <param name="pic">tu jest wazne suggestedfileName oraz Content</param>
    ''' <returns>OnePic uzupelniony o BufferFileName, FALSE: error</returns>
    Public Async Function AddFile(oPic As OnePic) As Task(Of Boolean)

        Dim sDstPathName As String = IO.Path.Combine(_rootPictures, oPic.sSuggestedFilename)

        If IO.File.Exists(sDstPathName) Then
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
        End If

        Dim oWriteStream = IO.File.Create(sDstPathName, 1024 * 1024)
        Await oPic.Content.CopyToAsync(oWriteStream, 1024 * 1024)

        Await oWriteStream.FlushAsync
        oWriteStream.Dispose()


        IO.File.SetCreationTime(sDstPathName, oPic.GetExifOfType(ExifSource.SourceFile).DateMin)
        IO.File.SetLastWriteTime(sDstPathName, oPic.GetExifOfType(ExifSource.SourceFile).DateMax)

        oPic.InBufferPathName = sDstPathName

        _pliki.Add(oPic)

        Return True

    End Function


End Class

Public Class FilesInBuffer
    Inherits Vblib.MojaLista(Of OnePic)

    Public Sub New(sFolder As String)
        MyBase.New(sFolder, "buffer.json")
    End Sub

End Class



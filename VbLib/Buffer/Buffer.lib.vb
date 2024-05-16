
'Imports System.ComponentModel
Imports System.IO
'Imports pkar
Imports pkar.DotNetExtensions

Imports Vblib.BufferSortowania

Public Interface IBufor
    Sub SaveData()
    Function Count() As Integer

    Function GetList() As List(Of OnePic)

    Function BakDelete(iDays As Integer, bRealDelete As Boolean) As Boolean

    ''' <summary>
    ''' tu nie ma Save - bo jeśli kasujemy serię, to zapis lepiej zrobić tylko raz
    ''' nie usuwa też ze źródła (AddToPurge), gdyż VbLib nie widzi SourceList - użyj PicSourceList.AddToPurge
    ''' </summary>
    Function DeleteFile(oPic As OnePic) As Boolean

    ''' <summary>
    ''' BuforSortowania: skopiuj plik (skorzystaj z OnePic.oContent - stream do pliku)
    ''' BuforFromQuery: po prostu dodaj do listy
    ''' </summary>
    ''' <param name="pic">BuforSortowania: tu jest wazne suggestedfileName oraz oContent</param>
    ''' <returns>OnePic uzupelniony o BufferFileName, FALSE: error (lub ten plik z tą zawartością już jest)</returns>
    Function AddFile(oPic As OnePic) As Task(Of Boolean)

    Sub ResetPipelines()

    Function GetMinDate() As Date
    Function GetMaxDate() As Date

End Interface


Public Class BufferSortowania
    Implements IBufor

    Private _RootDataPath As String
    Private _rootPictures As String
    Private _pliki As FilesInBuffer

    Public Sub New(sRootDataPath As String)
        _RootDataPath = sRootDataPath
        _pliki = New FilesInBuffer(_RootDataPath)
        _pliki.Load()
        _pliki.RecalcSumsy()
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

    Private Sub AddTyp3()
        For Each oItem As OnePic In _pliki.GetList

            Dim oExif As ExifTag = oItem.GetExifOfType(ExifSource.SourceDefault)
            oExif.FileSourceDeviceType = FileSourceDeviceTypeEnum.digital
        Next

        SaveData()
    End Sub

#End If

    Public Sub SaveData() Implements IBufor.SaveData
        _pliki.Save(True)
    End Sub

    Public Function Count() As Integer Implements IBufor.Count
        Return _pliki.Count
    End Function

    Public Function GetList() As List(Of OnePic) Implements IBufor.GetList
        Return _pliki
    End Function

    Public Function BakDelete(iDays As Integer, bRealDelete As Boolean) As Boolean Implements IBufor.BakDelete
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
    Public Function DeleteFile(oPic As OnePic) As Boolean Implements IBufor.DeleteFile
        Try
            IO.File.Delete(oPic.InBufferPathName)
            _pliki.Remove(oPic)
            Return True
        Catch ex As Exception

        End Try
        Return False
    End Function


    ''' <summary>
    ''' zabierz plik (skorzystaj z OnePic.oContent - stream do pliku)
    ''' </summary>
    ''' <param name="pic">tu jest wazne suggestedfileName oraz oContent</param>
    ''' <returns>TRUE i OnePic uzupelniony o BufferFileName, FALSE: error (lub ten plik z tą zawartością już jest)</returns>
    Public Async Function AddFile(oPic As OnePic) As Task(Of Boolean) Implements IBufor.AddFile
        DumpCurrMethod($"({oPic.sSuggestedFilename}")

        Dim sDstPathName As String = IO.Path.Combine(_rootPictures, oPic.sSuggestedFilename)

        If IO.File.Exists(sDstPathName) Then
            ' na MTP nie ma Seek, wiec musimy to zrobić przez plik pośredni

            Dim sTempName As String = sDstPathName & ".tmp"

            If File.Exists(sTempName) Then
                DumpMessage("Temp file already exists! " & sTempName)
                Await Vblib.DialogBoxAsync("Temp file already exists! " & sTempName)
                Return False
            End If

            Dim bSame As Boolean = False

            Using oTempStream = IO.File.Create(sTempName, 1024 * 1024)
                Await oPic.oContent.CopyToAsync(oTempStream, 1024 * 1024)

                Using oExistingStream As Stream = IO.File.OpenRead(sDstPathName)
                    oTempStream.Seek(0, SeekOrigin.Begin)

                    bSame = Await oTempStream.IsSameStreamContent(oExistingStream)
                End Using
            End Using

            If bSame Then
                DumpMessage("File already exists, same content")
                Try
                    IO.File.Delete(sTempName)
                Catch ex As Exception

                End Try

                If _pliki.Find(Function(x) x.sSuggestedFilename = sDstPathName) IsNot Nothing Then
                    DumpMessage("i wiem o tym pliku, więc pomijam go")
                    Return False
                End If

                DumpMessage("ale chyba z nieudanego IMPORT, bo jakobym nic o nim nie wiedział")

            Else

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

                Dim bErr As Boolean = False
                Try
                    IO.File.Move(sTempName, sDstPathName)
                Catch ex As Exception
                    bErr = True
                End Try

                If bErr Then
                    Await Task.Delay(250) '  próba opóźnienia, może antywir trzyma... mi się zdarzyło na dużym mp4
                    IO.File.Move(sTempName, sDstPathName)
                End If
            End If
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

        oPic.SetDefaultFileTypeDiscriminator()

        _pliki.Add(oPic)

        Return True

    End Function


    Public Sub ResetPipelines() Implements IBufor.ResetPipelines
        _pliki.ForEach(Sub(x) x.ResetPipeline())
    End Sub

    Public Function GetMinDate() As Date Implements IBufor.GetMinDate
        Dim minPicDate As Date = New Date(2200, 1, 1)

        _pliki.ForEach(Sub(x) If x.GetMinDate < minPicDate Then minPicDate = x.GetMinDate)

        Return minPicDate
    End Function

    Public Function GetMaxDate() As Date Implements IBufor.GetMaxDate
        Dim maxPicDate As Date = New Date(1750, 1, 1)
        _pliki.ForEach(Sub(x) If x.GetMaxDate > maxPicDate Then maxPicDate = x.GetMaxDate)
        Return maxPicDate
    End Function

    Public Class FilesInBuffer
        Inherits pkar.BaseList(Of OnePic)

        Public Sub New(sFolder As String)
            MyBase.New(sFolder, "buffer.json")
        End Sub

        Public Sub RecalcSumsy()
            ForEach(Sub(x) x.RecalcSumsy())
        End Sub

    End Class


End Class


Public Class BufferFromQuery
    Implements IBufor

    Private _pliki As List(Of OnePic)

    Public Sub New()
        _pliki = New List(Of OnePic)
    End Sub

    Public Sub New(sFilepathname As String)
        DumpCurrMethod()

        Dim sFolder As String = IO.Path.GetDirectoryName(sFilepathname)
        Dim lista As New pkar.BaseList(Of OnePic)(sFolder, IO.Path.GetFileName(sFilepathname))

        lista.Load()
        ' recalc sumsy
        For Each oItem As OnePic In _pliki
            oItem.sumOfDescr = oItem.GetSumOfDescriptionsText
        Next

        _pliki = New List(Of OnePic)

        For Each oPic As OnePic In lista
            oPic.InBufferPathName = IO.Path.Combine(sFolder, oPic.sSuggestedFilename)
            _pliki.Add(oPic)
        Next

    End Sub

    Public Sub SaveData() Implements IBufor.SaveData
        ' empty - nie zapisujemy NIC
        ' *TODO* zapisywanie zmian do pliku archive i folder\picsort
    End Sub

    Public Sub ResetPipelines() Implements IBufor.ResetPipelines
        For Each oItem As OnePic In _pliki
            oItem.ResetPipeline()
        Next
    End Sub

    Public Function Count() As Integer Implements IBufor.Count
        Return _pliki.Count
    End Function

    Public Function GetList() As List(Of OnePic) Implements IBufor.GetList
        Return _pliki
    End Function

    Public Function BakDelete(iDays As Integer, bRealDelete As Boolean) As Boolean Implements IBufor.BakDelete
        Return True
    End Function

    Public Function DeleteFile(oPic As OnePic) As Boolean Implements IBufor.DeleteFile
        Return False
    End Function

#Disable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
    ''' <summary>
    ''' po prostu dodaj do listy, nie tykając danych; ważne żeby InBufferPathName było poprawne
    ''' </summary>
    Public Async Function AddFile(oPic As OnePic) As Task(Of Boolean) Implements IBufor.AddFile
#Enable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
        _pliki.Add(oPic)
        Return True
    End Function

    Public Function GetMinDate() As Date Implements IBufor.GetMinDate
        Dim minPicDate As Date = New Date(2200, 1, 1)
        For Each oPic As OnePic In _pliki
            If oPic.GetMinDate < minPicDate Then minPicDate = oPic.GetMinDate
        Next

        Return minPicDate
    End Function

    Public Function GetMaxDate() As Date Implements IBufor.GetMaxDate
        Dim maxPicDate As Date = New Date(1750, 1, 1)
        For Each oPic As OnePic In _pliki
            If oPic.GetMaxDate > maxPicDate Then maxPicDate = oPic.GetMaxDate
        Next

        Return maxPicDate
    End Function

End Class


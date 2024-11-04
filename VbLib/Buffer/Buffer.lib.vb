
'Imports System.ComponentModel
Imports System.IO
'Imports pkar
Imports pkar.DotNetExtensions

'Imports Vblib.BufferSortowania

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

    Function GetIsReadonly() As Boolean

    Function GetBufferName() As String

    Sub SetStagesSettings(listaCheckow As String)

    Function RunAutoExif() As Task


End Interface


Public Class BufferSortowania
    Implements IBufor

    'Private _RootDataPath As String
    Private _rootPictures As String
    Private _pliki As FilesInBuffer
    Private _nazwa As String

    Public Const DEFAULT_BUFFER_NAME As String = "Buffer"

    ''' <summary>
    ''' wczytuje indeks zdjęć z pliku .json
    ''' </summary>
    ''' <param name="sRootDataPath">ścieżka do katalogu z plikami .json</param>
    Public Sub New(sRootDataPath As String)
        '_RootDataPath = sRootDataPath
        _pliki = New FilesInBuffer(sRootDataPath)
        _pliki.Load()
        _pliki.RecalcSumsy(Vblib.GetKeywords)
        ' AddSortBy
        'AddTyp3()
        _rootPictures = GetSettingsString("uiFolderBuffer")
        _nazwa = DEFAULT_BUFFER_NAME
    End Sub

    ''' <summary>
    ''' wczytuje indeks zdjęć z pliku JSON
    ''' </summary>
    ''' <param name="sRootDataPath">ścieżka do katalogu z plikami .json</param>
    ''' <param name="bufferFolder">nazwa bufora (podkatalogu ze zdjęciami)</param>
    ''' <param name="slowka">lista słów kluczowych (potrzebna w .lib)</param>
    Public Sub New(sRootDataPath As String, bufferFolder As String)
        '_RootDataPath = sRootDataPath
        _pliki = New FilesInBuffer(sRootDataPath, "u." & bufferFolder & ".json")
        _pliki.Load()
        _pliki.RecalcSumsy(Vblib.GetKeywords)

        _rootPictures = IO.Path.Combine(GetSettingsString("uiFolderBuffer"), bufferFolder)
        _nazwa = bufferFolder
    End Sub

    Public Function GetBufferName() As String Implements IBufor.GetBufferName
        Return _nazwa
    End Function

    Public Function IsDefaultBuffer() As Boolean
        Return String.IsNullOrWhiteSpace(_nazwa)
    End Function

    Private Function GetStagesSettName() As String
        Return $"stages_{_nazwa}"
    End Function

    Public Function GetStagesSettings() As String
        Return Vblib.GetSettingsString(GetStagesSettName)
    End Function

    Public Sub SetStagesSettings(listaCheckow As String) Implements IBufor.SetStagesSettings
        Vblib.SetSettingsString(GetStagesSettName, listaCheckow)
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
    ''' usuwa wszystkie pliki z katalogu - używać ostrożnie!
    ''' </summary>
    Public Sub RemoveAllFiles()
        Dim pliki As String() = IO.Directory.GetFiles(_rootPictures)
        For Each plik As String In pliki
            IO.File.Delete(plik)
        Next

        _pliki.Clear()
        _pliki.Save()
    End Sub


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
        ' z MappedSource nie ma DateMin, bo nie ma dat plików (zalożenie: data jest w EXIFie)
        If oExif.DateMin.IsDateValid Then IO.File.SetCreationTime(sDstPathName, oExif.DateMin)
        IO.File.SetLastWriteTime(sDstPathName, oExif.DateMax)

        oPic.InBufferPathName = sDstPathName
        ' oPic.sortOrder = oExif.DateMax.ToExifString 'ToString("yyyy.MM.dd HH.mm.ss")

        oPic.SetDefaultFileTypeDiscriminator()
        oPic.RecalcSumsy()

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

    ''' <summary>
    ''' sprawdza czy wszystkie pliki mają SERNO, TRUE: OK, FALSE: jakiś nie ma
    ''' </summary>
    ''' <returns></returns>
    Public Function CheckSerNo() As String

        For Each oPic As OnePic In _pliki
            If String.IsNullOrEmpty(oPic.TargetDir) Then Continue For
            If oPic.serno < 1 Then Return oPic.sSuggestedFilename
        Next
        Return ""
    End Function

    ''' <summary>
    ''' policz pliki które mają zdefiniowany TargetDir
    ''' </summary>
    Public Function CountWithTargetDir() As Integer

        Dim iCnt As Integer = 0
        For Each oPic As Vblib.OnePic In _pliki
            If String.IsNullOrWhiteSpace(oPic.TargetDir) Then Continue For
            iCnt += 1
        Next
        Return iCnt
    End Function

    ''' <summary>
    ''' policz pliki do archiwizacji
    ''' </summary>
    ''' <param name="currentArchs">lista archiwów</param>
    Public Function CountDoArchiwizacji(currentArchs As List(Of String)) As Integer

        If currentArchs.Count < 1 Then Return 0

        Dim iCnt As Integer = 0
        For Each oPic As Vblib.OnePic In _pliki
            If String.IsNullOrWhiteSpace(oPic.TargetDir) Then Continue For

            If oPic.Archived Is Nothing Then
                iCnt += 1
            Else
                Dim sArchiwa As String = oPic.Archived.ToLowerInvariant
                For Each sArch As String In currentArchs
                    If Not oPic.IsArchivedIn(sArch) Then
                        iCnt += 1
                        Exit For
                    End If
                Next
            End If

        Next

        Return iCnt

    End Function

    Public Function CountDoCloudArchiwizacji(cloudArchs As List(Of Vblib.CloudArchive)) As Integer

        Dim currentArchs As New List(Of String)
        For Each oArch As Vblib.CloudArchive In cloudArchs
            If oArch.konfiguracja.enabled Then currentArchs.Add(oArch.konfiguracja.nazwa.ToLowerInvariant)
        Next

        If currentArchs.Count < 1 Then Return 0

        Dim iCnt As Integer = 0
        For Each oPic As Vblib.OnePic In _pliki
            If String.IsNullOrWhiteSpace(oPic.TargetDir) Then Continue For

            If oPic.CloudArchived Is Nothing Then
                iCnt += 1
            Else
                Dim sArchiwa As String = oPic.CloudArchived.ToLowerInvariant
                For Each sArch As String In currentArchs
                    If Not sArchiwa.Contains(sArch) Then
                        iCnt += 1
                        Exit For
                    End If
                Next
            End If

        Next

        Return iCnt

    End Function

    Public Function GetIsReadonly() As Boolean Implements IBufor.GetIsReadonly
        Return False
    End Function

    Public Async Function RunAutoExif() As Task Implements IBufor.RunAutoExif
        Dim oEngine As AutotaggerBase = Vblib.gAutoTagery.Where(Function(x) x.Nazwa = Vblib.ExifSource.FileExif).ElementAt(0)
        ' się nie powinno zdarzyć, no ale cóż...
        If oEngine Is Nothing Then Return

        Dim iSerNo As Integer = Vblib.GetSettingsInt("lastSerNo")

        For Each oItem As Vblib.OnePic In _pliki

            ' najpierw Serial Number zrobimy - obojętnie co dalej...
            If oItem.serno < 1 Then
                iSerNo += 1
                oItem.serno = iSerNo
            End If

            If Not IO.File.Exists(oItem.InBufferPathName) Then Continue For   ' zabezpieczenie przed samoznikaniem

            ' niby nie ma prawa być, chyba że to Peer - albo mappedSource
            If oItem.GetExifOfType(oEngine.Nazwa) Is Nothing Then
                Try
                    Dim oExif As Vblib.ExifTag = Await oEngine.GetForFile(oItem)
                    If oExif IsNot Nothing Then
                        oItem.ReplaceOrAddExif(oExif)
                        oItem.TagsChanged = True
                    End If
                    'Await Task.Delay(1) ' na wszelki wypadek, żeby był czas na przerysowanie progbar, nawet jak tworzenie EXIFa jest empty
                Catch ex As Exception
                    ' zabezpieczenie, żeby mi na pewno nie zrobił crash przy nadawaniu serno!
                End Try
            End If
        Next

        Vblib.SetSettingsInt("lastSerNo", iSerNo)

    End Function

    Public Class FilesInBuffer
        Inherits pkar.BaseList(Of OnePic)

        Public Sub New(sFolder As String, Optional folderfile As String = "buffer.json")
            MyBase.New(sFolder, folderfile)
        End Sub

        Public Sub RecalcSumsy(slowka As KeywordsList)
            ForEach(Sub(x) x.RecalcSumsy(slowka))
        End Sub

    End Class


End Class


Public Class BufferFromQuery
    Implements IBufor

    Private _pliki As List(Of OnePic)
    Private _dbase As DatabaseInterface = Nothing

    ''' <summary>
    ''' tworzy nowy IBuffer; który umie zapisać zmiany
    ''' </summary>
    ''' <param name="dbase">Baza danych (do niej będzie próbowało zrobic Save)</param>
    Public Sub New(dbase As DatabaseInterface)
        _pliki = New List(Of OnePic)
        _dbase = dbase
    End Sub

    ''' <summary>
    ''' tworzy nowy IBuffer; który NIE umie zapisać zmian
    ''' </summary>
    Public Sub New()
        _pliki = New List(Of OnePic)
        _dbase = Nothing
    End Sub



    Public Sub New(sFilepathname As String)
        DumpCurrMethod()

        Dim sFolder As String = IO.Path.GetDirectoryName(sFilepathname)
        Dim lista As New pkar.BaseList(Of OnePic)(sFolder, IO.Path.GetFileName(sFilepathname))

        lista.Load()
        ' recalc sumsy
        'For Each oItem As OnePic In _pliki
        '    oItem.sumOfDescr = oItem.GetSumOfDescriptionsText
        'Next

        _pliki = New List(Of OnePic)

        For Each oPic As OnePic In lista
            oPic.InBufferPathName = IO.Path.Combine(sFolder, oPic.sSuggestedFilename)
            _pliki.Add(oPic)
        Next

        _dbase = Nothing

    End Sub

    Public Sub SaveData() Implements IBufor.SaveData
        If _dbase Is Nothing Then Return

        _dbase.SaveData()
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

    Public Function GetIsReadonly() As Boolean Implements IBufor.GetIsReadonly
        Return (_dbase Is Nothing)
    End Function

    Public Function GetBufferName() As String Implements IBufor.GetBufferName
        Return "Query"
    End Function

#Disable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
    Public Sub SetStagesSettings(listaCheckow As String) Implements IBufor.SetStagesSettings
        ' empty
    End Sub

    Public Async Function RunAutoExif() As Task Implements IBufor.RunAutoExif
        ' empty dla query
    End Function
#Enable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously


End Class


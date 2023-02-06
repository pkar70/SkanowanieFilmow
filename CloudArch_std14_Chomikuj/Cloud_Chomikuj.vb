
Imports System.Net.Mime
Imports System.Runtime.CompilerServices
Imports System.Text
Imports Vblib
Imports vb14 = Vblib.pkarlibmodule14

' chomikuj: https://github.com/brogowski/ChomikujApi

' Std 2.0 dla MimeType


Public Class Cloud_Chomikuj
    Inherits Vblib.CloudArchive

    Public Const PROVIDERNAME As String = "Chomikuj"
    Private _DataDir As String

    Private _nugetClient As New Chomikuj.ChomikujClient
    Private _loggedIn As Boolean = False

    Public Overrides Property sProvider As String = PROVIDERNAME

    Public Overrides Async Function SendFileMain(oPic As Vblib.OnePic) As Task(Of String)

        If Not _loggedIn Then Await Login()
        If Not _loggedIn Then Return "ERROR cannot login"

        Dim oDir As Chomikuj.ChomikujDirectory = TryCreateDirectoryTree(GetFolderForFile(oPic))
        If oDir Is Nothing Then Return "ERROR: cannot get/create dirs path"

        Dim oFile As New Chomikuj.NewFileRequest
        oFile.FileName = oPic.sSuggestedFilename
        oPic._PipelineOutput.Seek(0, IO.SeekOrigin.Begin)
        oFile.FileStream = oPic._PipelineOutput
        ' oFile.ContentType = "image/" ' jpeg, png, tiff, avi, itp.
        oFile.ContentType = MimeTypes.MimeTypeMap.GetMimeType(IO.Path.GetExtension(oPic.InBufferPathName))

        oDir.UploadFile(oFile)

        Dim oRemoteFile As Chomikuj.ChomikujFile = oDir.GetFile(oPic.sSuggestedFilename)
        If oRemoteFile Is Nothing Then Return "ERROR: unsuccessfull upload"

        Dim sCaption As String = oPic.GetDescriptionForCloud
        If Not String.IsNullOrWhiteSpace(sCaption) Then oRemoteFile.AddComment(sCaption)

        oPic.AddCloudArchive(konfiguracja.nazwa)

        Return ""
    End Function

    Public Overrides Function CreateNew(oConfig As Vblib.CloudConfig, oPostProcs As Vblib.PostProcBase(), sDataDir As String) As Vblib.AnyStorage
        If oConfig.sProvider <> sProvider Then Return Nothing

        Dim oNew As New Cloud_Chomikuj
        oNew.konfiguracja = oConfig
        oNew._PostProcs = oPostProcs
        oNew._DataDir = sDataDir
        Return oNew
    End Function

#Disable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously. Consider using the 'Await' operator to await non-blocking API calls, or 'Await Task.Run(...)' to do CPU-bound work on a background thread.
    Public Overrides Async Function GetMBfreeSpace() As Task(Of Integer)
#Enable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously. Consider using the 'Await' operator to await non-blocking API calls, or 'Await Task.Run(...)' to do CPU-bound work on a background thread.
        Return Integer.MaxValue ' no limits
    End Function

    Public Overrides Async Function SendFiles(oPicki As List(Of Vblib.OnePic), oNextPic As JedenWiecejPlik) As Task(Of String)
        ' tu jest proste - zwyk³e wywo³anie SendFile dla kolejnych
        For Each oPicek As Vblib.OnePic In oPicki
            Dim sRet As String = Await SendFile(oPicek)
            If sRet <> "" Then Return $"When sending {oPicek.sSuggestedFilename}: " & sRet
            oNextPic()
        Next

        Return ""
    End Function
    Public Overrides Async Function GetFile(oPic As Vblib.OnePic) As Task(Of String)
        Dim sLink As String = Await GetShareLink(oPic)
        If sLink = "" Then Return "ERROR: nie mam zapisanego ID pliku"

        ' *TODO* na razie i tak nie bêdzie wykorzystywane, podobnie jak w LocalStorage
        ' *TODO* raczej bedzie konieczny LOGIN
        Throw New NotImplementedException()
    End Function


    Public Overrides Async Function GetRemoteTagsMain(oPic As Vblib.OnePic) As Task(Of String)

        If Not oPic.IsCloudArchivedIn(konfiguracja.nazwa) Then Return "ERROR: plik nie zarchiwizowany w " & konfiguracja.nazwa

        If Not _loggedIn Then Await Login()
        If Not _loggedIn Then Return "ERROR: login problem"

        Dim oDir As Chomikuj.ChomikujDirectory = TryCreateDirectoryTree(GetFolderForFile(oPic))
        If oDir Is Nothing Then Return "ERROR brak katalogu z plikiem"

        Dim oFile As Chomikuj.ChomikujFile = oDir.GetFile(oPic.sSuggestedFilename)
        If oFile Is Nothing Then Return "ERROR: no file"

        Dim oComms As List(Of Chomikuj.ChomikComment) = oFile.GetComments
        If oComms Is Nothing Then Return ""

        For Each oComment As Chomikuj.ChomikComment In oComms
            ' tylko "nieswoje"
            If oComment.User <> konfiguracja.sUsername Then
                Dim sData As String = oComment.When
                Dim sComm As String = oComment.Tekst
                oPic.TryAddDescription(New Vblib.OneDescription(sData, sComm))
            End If
        Next

        Return ""
    End Function

    Public Overrides Async Function Delete(oPic As Vblib.OnePic) As Task(Of String)
        If Not oPic.IsArchivedIn(konfiguracja.nazwa) Then Return "ERROR: plik nie zarchiwizowany w " & konfiguracja.nazwa

        If Not _loggedIn Then Await Login()
        If Not _loggedIn Then Return "ERROR: login problem"

        Dim oDir As Chomikuj.ChomikujDirectory = TryCreateDirectoryTree(GetFolderForFile(oPic))
        If oDir Is Nothing Then Return "ERROR brak katalogu z plikiem"

        Dim oFile As Chomikuj.ChomikujFile = oDir.GetFile(oPic.sSuggestedFilename)
        If oFile Is Nothing Then Return "ERROR: no file"

        oDir.RemoveFile(oFile)

        Return ""
    End Function
    Private Function GetFolderForFile(oPic As Vblib.OnePic) As String
        Dim sLink As String = konfiguracja.additInfo
        If sLink Is Nothing Then
            sLink = ""
        Else
            sLink &= "/"
        End If

        sLink = sLink & oPic.TargetDir

        Return sLink.Replace("\", "/").Replace("//", "/")
    End Function

#Disable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
    Public Overrides Async Function GetShareLink(oPic As Vblib.OnePic) As Task(Of String)

        Dim sTemp As String = $"/{GetFolderForFile(oPic)}/{oPic.sSuggestedFilename}"
        Return "https://" & ("chomikuj.pl/" & konfiguracja.sUsername & sTemp).Replace("//", "/")
    End Function

    Public Overrides Async Function Login() As Task(Of String)
        Return ChomikLogin()
    End Function

#Enable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously

#Disable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously

    Public Overrides Async Function VerifyFileExist(oPic As Vblib.OnePic) As Task(Of String)

        If Not oPic.IsArchivedIn(konfiguracja.nazwa) Then Return "ERROR: plik nie zarchiwizowany w " & konfiguracja.nazwa

        If Not _loggedIn Then Await Login()
        If Not _loggedIn Then Return "ERROR: login problem"

        Dim oDir As Chomikuj.ChomikujDirectory = TryCreateDirectoryTree(GetFolderForFile(oPic))
        If oDir Is Nothing Then Return "ERROR brak katalogu z plikiem"

        Dim oFile As Chomikuj.ChomikujFile = oDir.GetFile(oPic.sSuggestedFilename)
        If oFile Is Nothing Then Return "ERROR: no file"

        Return ""
    End Function

    Public Overrides Async Function VerifyFile(oPic As Vblib.OnePic, oCopyFromArchive As Vblib.LocalStorage) As Task(Of String)
        Dim sRet As String = Await VerifyFileExist(oPic)
        If sRet <> "NO FILE" Then Return sRet

        ' nie ma, b¹dŸ error

        ' *TODO* na razie i tak nie bêdzie wykorzystywane, podobnie jak w LocalStorage
        Throw New NotImplementedException()
    End Function

    Public Overrides Async Function GetShareLink(oOneDir As Vblib.OneDir) As Task(Of String)
        'Dim sLink As String = $"{konfiguracja.sUsername}/"
        'sLink &= $"{konfiguracja.additInfo}/{oOneDir.Pic.TargetDir}/{oPic.InBufferPathName}"

        'Return "https://chomikuj.pl/" & sLink.Replace("//", "/")
        ' *TODO* na razie i tak nie jest wykorzystywane chyba
        Throw New NotImplementedException()

    End Function

    Public Overrides Async Function Logout() As Task(Of String)
        Return ""
    End Function

#Enable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously

#Region "bezpoœredni dostêp do Chomika"

    Private Function ChomikLogin() As String

        _loggedIn = False

        If String.IsNullOrWhiteSpace(konfiguracja.sUsername) Then Return "ERROR: username cannot be null"
        If String.IsNullOrWhiteSpace(konfiguracja.sPswd) Then Return "ERROR: password cannot be null"

        If Not _nugetClient.Login(konfiguracja.sUsername, konfiguracja.sPswd) Then
            Return "ERROR: bad login"
        End If

        _loggedIn = True

        Return ""

    End Function

    Private Function TryCreateDirectoryTree(sDirPath As String) As Chomikuj.ChomikujDirectory

        If String.IsNullOrWhiteSpace(sDirPath) Then Return Nothing
        Dim aDirs As String() = sDirPath.Replace("\", "/").Split("/")

        Dim oDir As Chomikuj.ChomikujDirectory = _nugetClient.HomeDirectory
        Dim bFirst As Boolean = True
        For Each sDir As String In aDirs
            If sDir.Length < 1 Then Continue For ' jakby by³y //, albo "/aaa" (pierwszy slash)

            Dim oDir1 As Chomikuj.ChomikujDirectory = oDir.GetDirectory(sDir)
            If oDir1 Is Nothing Then
                Dim oNew As New Chomikuj.NewFolderRequest
                If bFirst Then
                    oNew.AdultContent = True
                    oNew.Password = konfiguracja.sPswd
                    oNew.PasswordSecured = True
                End If

                oNew.Name = sDir
                oDir.CreateSubDirectory(oNew)
                oDir1 = oDir.GetDirectory(sDir)
                If oDir1 Is Nothing Then Return Nothing ' nieudane stworzenie katalogu
            End If

            oDir = oDir1

            bFirst = False
        Next

        Return oDir
    End Function

#End Region
End Class

Module Extensions
    <Runtime.CompilerServices.Extension>
    Public Function GetDirectory(ByVal oDir As Chomikuj.ChomikujDirectory, sSubDir As String) As Chomikuj.ChomikujDirectory
        vb14.DumpCurrMethod($"(oDir={oDir.Title}, {sSubDir}")
        For Each oSubDir As Chomikuj.ChomikujDirectory In oDir.GetDirectories
            If oSubDir.Title.ToLower = sSubDir.ToLower Then Return oSubDir
        Next

        Return Nothing
    End Function

    <Runtime.CompilerServices.Extension>
    Public Function GetFile(ByVal oDir As Chomikuj.ChomikujDirectory, sFilename As String) As Chomikuj.ChomikujFile
        vb14.DumpCurrMethod($"(oDir={oDir.Title}, {sFilename}")
        For Each oFile As Chomikuj.ChomikujFile In oDir.GetFiles
            If oFile.Title.ToLower = sFilename.ToLower Then Return oFile
        Next

        Return Nothing
    End Function

End Module

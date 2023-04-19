Imports Vblib
Imports vb14 = Vblib.pkarlibmodule14

Public Class chomikowanie

    Private _konfig As Vblib.CloudConfig
    Private _nugetClient As New Chomikuj.ChomikujClient
    Private _loggedIn As Boolean = False


    Public Sub New(konfig As Vblib.CloudConfig)
        _konfig = konfig
    End Sub

    Public Function Login() As Boolean
        Dim sErr As String = ChomikujLogin()
        If sErr = "" Then Return True

        vb14.DialogBox(sErr)
        Return False
    End Function

    Private _chomikDir As Chomikuj.ChomikujDirectory
    Private _chomikFilesList As List(Of Chomikuj.ChomikujFile)
    Private _chomikCurrFile As Integer

    Public Async Function SetDir(sPathname As String) As Task(Of Boolean)

        sPathname = IO.Path.Combine(_konfig.additInfo, sPathname)
        _chomikDir = TryGetDirectoryTree(sPathname)
        If _chomikDir Is Nothing Then Return False

        _chomikFilesList = _chomikDir.GetFiles
        _chomikCurrFile = -1
        Return True

    End Function

    ''' <summary>
    ''' zwraca Stream do pliku next/prev, ale tylko dla Image, i ewentualnie Video (Archive itp - pomija)
    ''' </summary>
    ''' <param name="bPrev"></param>
    ''' <param name="bAllowVideo"></param>
    ''' <returns>NULL jeśli się nie udało</returns>
    Public Function GetNextFile(bPrev As Boolean, bAllowVideo As Boolean) As Stream

        If _chomikFilesList.Count < 1 Then Return Nothing

        _chomikCurrFile += If(bPrev, -1, 1)
        If _chomikCurrFile < 0 Then Return Nothing
        If _chomikCurrFile > _chomikFilesList.Count - 1 Then Return Nothing

        Dim oCurrFile As Chomikuj.ChomikujFile = _chomikFilesList.ElementAt(_chomikCurrFile)

        If oCurrFile.FileType = Chomikuj.ChomikujFileType.Image Then Return oCurrFile.GetStream
        If bAllowVideo AndAlso oCurrFile.FileType = Chomikuj.ChomikujFileType.Video Then Return oCurrFile.GetStream

        Return GetNextFile(bPrev, bAllowVideo)
    End Function

    Private Function ChomikujLogin() As String

        _loggedIn = False

        If String.IsNullOrWhiteSpace(_konfig.sUsername) Then Return "ERROR: username cannot be null"
        If String.IsNullOrWhiteSpace(_konfig.sPswd) Then Return "ERROR: password cannot be null"

        If Not _nugetClient.Login(_konfig.sUsername, _konfig.sPswd) Then
            Return "ERROR: bad login"
        End If

        _loggedIn = True

        Return ""

    End Function

    Private Function TryGetDirectoryTree(sDirPath As String) As Chomikuj.ChomikujDirectory

        If String.IsNullOrWhiteSpace(sDirPath) Then Return Nothing
        Dim aDirs As String() = sDirPath.Replace("\", "/").Split("/")

        Dim oDir As Chomikuj.ChomikujDirectory = _nugetClient.HomeDirectory
        For Each sDir As String In aDirs
            If sDir.Length < 1 Then Continue For ' jakby były //, albo "/aaa" (pierwszy slash)

            Dim oDir1 As Chomikuj.ChomikujDirectory = oDir.GetDirectory(sDir)
            If oDir1 Is Nothing Then Return Nothing

            oDir = oDir1
        Next

        Return oDir
    End Function


End Class

Partial Module Extensions
    <Runtime.CompilerServices.Extension>
    Public Function GetDirectory(ByVal oDir As Chomikuj.ChomikujDirectory, sSubDir As String) As Chomikuj.ChomikujDirectory
        vb14.DumpCurrMethod($"(oDir={oDir.Title}, {sSubDir}")
        Dim dirsy = oDir.GetDirectories
        For Each oSubDir As Chomikuj.ChomikujDirectory In dirsy
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

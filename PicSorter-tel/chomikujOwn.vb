
' własna obsługa chomika, jako że Nugetowa coś nie działa po próbie zmiany z Restowego nugeta,
' a do telefonu potrzeba tylko niewiele funkcji


Imports System.Net.Http
Imports Windows.UI.Composition

Public Class chomikujOwn

    ''' <summary>
    ''' login, zrób wszystko co trzeba i zapisz sobie w wewnętrznych danych
    ''' </summary>
    Public Async Function Login(sUsername As String, sPassword As String) As Task(Of Boolean)

    End Function

    ''' <summary>
    ''' zwraca listę Uri do plików poszczególnych w podanym katalogu
    ''' </summary>
    ''' <returns>Lista URI do plików lub NULL gdy błąd</returns>
    Public Async Function GetDirectoryListing(sDirpath As String) As Task(Of List(Of String))

    End Function

    ''' <summary>
    ''' zwraca Stream konkretnego pliku (sFileUri - jeden ze zwróconych z GetDirectoryListing
    ''' </summary>
    Public Async Function GetFileStream(sFileUri As String) As Task(Of Stream)

    End Function

    Private Const MainAddress As String = "http://chomikuj.pl/"
    Private Const LoginUrl As String = "action/Login/TopBarLogin"
    Private Const NewFolderUrl As String = "action/FolderOptions/NewFolderAction"
    Private Const GetPageWithFilesUrl As String = "action/Files/FilesList"
    Private Const GetUrlToFileUrl As String = "action/License/DownloadContext"

    Private mHttpClient As New HttpClient
End Class


' https://www.cs.hmc.edu/~ben/c4p_API_v2.html
' https://www.shutterfly.com/documentation/OflyBasicXml.sfly?esch=1
' https://stackoverflow.com/questions/275130/shutterfly-order-api

Imports Vblib

Public Class Cloud_Shutterfly
    Inherits Vblib.CloudArchive

    Public Const PROVIDERNAME As String = "Shutterfly"

    Public Overrides Property sProvider As String = PROVIDERNAME
    Private _DataDir As String

    Public Overrides Async Function SendFileMain(oPic As Vblib.OnePic) As Task(Of String)
        ' *TODO*

    End Function

    Public Overrides Function CreateNew(oConfig As Vblib.CloudConfig, oPostProcs As Vblib.PostProcBase(), sDataDir As String) As Vblib.AnyStorage
        If oConfig.sProvider <> sProvider Then Return Nothing

        Dim oNew As New Cloud_Shutterfly
        oNew.konfiguracja = oConfig
        oNew._PostProcs = oPostProcs
        oNew._DataDir = sDataDir
        Return oNew
    End Function

    Private _token As String    ' wa¿ny 60 dni, ale raczej tak d³ugo nikt nie bêdize mia³ otwartego PicSorta

    Protected Async Function SendFileMainFB(oPic As Vblib.OnePic, sAlbumName As String) As Task(Of String)
        ' *TODO* wyœlij plik, "" gdy bezpoœrednio, Name gdy do albumu
    End Function

    Public Overrides Async Function GetMBfreeSpace() As Task(Of Integer)
        Return Integer.MaxValue ' no limits
    End Function

    Public Overrides Async Function SendFiles(oPicki As List(Of Vblib.OnePic), oNextPic As JedenWiecejPlik) As Task(Of String)
        ' *TODO* na razie i tak nie bêdzie wykorzystywane, podobnie jak w LocalStorage
        Throw New NotImplementedException()
    End Function

    Public Overrides Async Function GetFile(oPic As Vblib.OnePic) As Task(Of String)
        Dim sLink As String = Await GetShareLink(oPic)
        If sLink = "" Then Return "ERROR: nie mam zapisanego ID pliku"

        ' *TODO* na razie i tak nie bêdzie wykorzystywane, podobnie jak w LocalStorage
        ' *TODO* raczej bedzie konieczny LOGIN
        Throw New NotImplementedException()
    End Function


    Public Overrides Async Function GetRemoteTagsMain(oPic As Vblib.OnePic) As Task(Of String)
        Dim sLink As String = Await GetShareLink(oPic)
        If sLink = "" Then Return "ERROR: nie mam zapisanego ID pliku"

        ' If mInstaApi Is Nothing Then Return "ERROR: przed GetRemoteTags musi byæ LOGIN"

        ' *TODO* na razie i tak nie bêdzie wykorzystywane, podobnie jak w LocalStorage
        Throw New NotImplementedException()
    End Function

    Public Overrides Async Function Delete(oPic As Vblib.OnePic) As Task(Of String)
        Dim sLink As String = Await GetShareLink(oPic)
        If sLink = "" Then Return "ERROR: nie mam zapisanego ID pliku"

        ' If mInstaApi Is Nothing Then Return "ERROR: przed Delete musi byæ LOGIN"

        ' *TODO* na razie i tak nie bêdzie wykorzystywane, podobnie jak w LocalStorage
        Throw New NotImplementedException()
    End Function

    Public Overrides Async Function GetShareLink(oPic As Vblib.OnePic) As Task(Of String)
        Dim sId As String = oPic.GetCloudPublishedId(konfiguracja.nazwa)
        If sId = "" Then Return ""

        Return "https://www.instagram.com/p/" & sId & "/"
    End Function

    Public Overrides Async Function Login() As Task(Of String)
        If Await LoginMainAsync() Then Return ""
        Return "ERROR: incorrect login"
    End Function

#Disable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously

    Public Overrides Async Function VerifyFileExist(oPic As Vblib.OnePic) As Task(Of String)
        Dim sLink As String = Await GetShareLink(oPic)
        If sLink = "" Then Return "ERROR: nie mam zapisanego ID pliku"

        Dim sPage As String = Await Vblib.HttpPageAsync(sLink)
        If sPage.Contains("<title>Instagram</title>") Then Return "NO FILE"
        ' gdy jest, to <title>XXXXXX on Instagram: &quot;DESCRIPTION&quot;</title>
        Return ""

    End Function

    Public Overrides Async Function VerifyFile(oPic As Vblib.OnePic, oCopyFromArchive As Vblib.LocalStorage) As Task(Of String)
        Dim sRet As String = Await VerifyFileExist(oPic)
        If sRet <> "NO FILE" Then Return sRet

        ' If mInstaApi Is Nothing Then Return "ERROR: przed VerifyFile:Resend musi byæ LOGIN"

        ' *TODO* na razie i tak nie bêdzie wykorzystywane, podobnie jak w LocalStorage
        Throw New NotImplementedException()
    End Function

    Public Overrides Async Function GetShareLink(oOneDir As Vblib.OneDir) As Task(Of String)
        Return ""
    End Function

    Public Overrides Async Function Logout() As Task(Of String)
        Return ""
    End Function

#Enable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously

#Region "bezpoœredni dostêp do Chomika"

    Private Async Function LoginMainAsync() As Task(Of String)

        If Not String.IsNullOrWhiteSpace(_token) Then Return ""

        If String.IsNullOrWhiteSpace(konfiguracja.sUsername) Then Return "ERROR: use username for App ID"
        If String.IsNullOrWhiteSpace(konfiguracja.sPswd) Then Return "ERROR: use password for App Secret"


        Dim sPage As String = Await Vblib.HttpPageAsync("https://accounts.shutterfly.com/")



    End Function

#End Region
End Class


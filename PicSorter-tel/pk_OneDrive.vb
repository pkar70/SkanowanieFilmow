' Nuget:
' "Microsoft.OneDriveSDK" 2.0.7
' "Microsoft.OneDriveSDK.Authentication" 1.0.10

' u¿ywanie:
' 1) rejestracja app z dostêpem do Azure: https://docs.microsoft.com/en-us/archive/msdn-magazine/2016/february/windows-10-implementing-a-uwp-app-with-the-official-onedrive-sdk
' albo tu: https://docs.microsoft.com/en-us/onedrive/developer/rest-api/getting-started/msa-oauth?view=odsp-graph-online
' ale chyba to dla Android jest potrzebne, bo dla Windows - nie. Dla Win: dodaj Permission dla Internet :)
' 2) z UI: ODfolder = ODclient.GetRootAsync(true/false) w zale¿noœci od tego czy w ramach \Apps\ czy w ogóle root
' 3) ODfolder.GetFolderAsync, ODfolder.GetFileAsync
' 4) ODfile.ReadContentAsync, WriteContentAsync

' "Accounts in any organizational directory (Any Azure AD directory - Multitenant) and personal Microsoft accounts (e.g. Skype, Xbox)"
' RedirectUri: public client, https://login.microsoftonline.com/common/oauth2/nativeclient
' Application (client) ID jest potem wykorzystywane w wersji dla Android (see: Andro2UWP, shared, app.xaml.cs)

Imports vb14 = Vblib.pkarlibmodule14

''' <summary>
''' To jest w³asciwie tylko do wykorzystywania jako new ODclient, await ODclient.GetRoot
''' </summary>
Public Class ODclient
    Public Shared _oOneDriveClnt As Microsoft.OneDrive.Sdk.IOneDriveClient

    Public Shared Function IsOneDriveOpened() As Boolean
        Return _oOneDriveClnt IsNot Nothing
    End Function

    ''' <summary>
    ''' Zwraca ROOT folder, albo w ramach OD:\apps\[app] albo OD:\
    ''' </summary>
    ''' <param name="bInApp">default=true, w ramach \apps\; false: generalny root</param>
    ''' <param name="bCanUseUI">default=true, mo¿na pokazaæ okienka; false: dla pracy w tle</param>
    ''' <returns>ODfolder</returns>
    Public Shared Async Function GetRootAsync(Optional bInApp As Boolean = True, Optional bCanUseUI As Boolean = True) As Task(Of ODfolder)
        If Not IsOneDriveOpened() Then
            If Not NetIsIPavailable(False) Then Return Nothing
            If Not Await OpenOneDriveAsync(bCanUseUI) Then Return Nothing
        End If

        If bInApp Then
            Return New ODfolder(_oOneDriveClnt.Drive.Special.AppRoot)
        Else
            Return New ODfolder(_oOneDriveClnt.Drive.Root)
        End If

    End Function

    Private Shared Async Function OpenOneDriveAsync(bCanUseUI As Boolean) As Task(Of Boolean)
        ' https://github.com/OneDrive/onedrive-sample-photobrowser-uwp/blob/master/OneDrivePhotoBrowser/AccountSelection.xaml.cs
        ' dla PC tu bedzie error, wiec zwróci FALSE

        Dim sScopes As String() = {"onedrive.readwrite", "offline_access"}
        Const oneDriveConsumerBaseUrl As String = "https://api.onedrive.com/v1.0"

        Try

            Dim onlineIdAuthProvider As New Microsoft.OneDrive.Sdk.OnlineIdAuthenticationProvider(sScopes)
            Dim authTask As Task
            If bCanUseUI Then
                authTask = onlineIdAuthProvider.RestoreMostRecentFromCacheOrAuthenticateUserAsync()
            Else
                authTask = onlineIdAuthProvider.RestoreMostRecentFromCacheAsync()
            End If
            'Await authTask

            _oOneDriveClnt = New Microsoft.OneDrive.Sdk.OneDriveClient(oneDriveConsumerBaseUrl, onlineIdAuthProvider)
            Await authTask     ' tu jest w samplu - po moOneDriveClnt

            Return True
        Catch ex As Exception
            _oOneDriveClnt = Nothing
            Return False
        End Try

    End Function

End Class

Public Class ODfolder
    Private ReadOnly _oBuilder As Microsoft.OneDrive.Sdk.ItemRequestBuilder
    Public Sub New(oBuilder As Microsoft.OneDrive.Sdk.ItemRequestBuilder)
        If oBuilder Is Nothing Then Throw New ArgumentNullException("cannot create ODfolder from NULL")
        _oBuilder = oBuilder
    End Sub

    Public Async Function GetFolderAsync(sName As String, bCreate As Boolean) As Task(Of ODfolder)
        If sName = "" Then Return Nothing

        If Not NetIsIPavailable(False) Then Return Nothing

        Try
            Dim oReq As Microsoft.OneDrive.Sdk.ItemRequest = _oBuilder.ItemWithPath(sName).Request
            Dim oItem = Await oReq.GetAsync()
            Return New ODfolder(_oBuilder.ItemWithPath(sName))
        Catch ex As Exception

        End Try

        ' jak tu jestem, znaczy ¿e nie ma folderu
        If Not bCreate Then Return Nothing

        ' proba utworzenia katalogu
        Dim oNew As New Microsoft.OneDrive.Sdk.Item
        oNew.Name = sName
        oNew.Folder = New Microsoft.OneDrive.Sdk.Folder

        Dim oFolder As Microsoft.OneDrive.Sdk.Item
        oFolder = Await _oBuilder.Children.Request().AddAsync(oNew)

        Return New ODfolder(_oBuilder.ItemWithPath(sName))

    End Function

    Public Async Function GetItemsAsStringsAsync(bFolders As Boolean, bFiles As Boolean) As Task(Of List(Of String))

        Dim lNames As New List(Of String)

        Dim oItems As List(Of ODfile) = Await GetItemsAsItemsAsync(bFolders, bFiles)
        If oItems Is Nothing Then Return lNames

        For Each oItem As ODfile In oItems
            lNames.Add(oItem.GetName)
        Next

        Return lNames

    End Function

    Public Async Function GetItemsAsItemsAsync(bFolders As Boolean, bFiles As Boolean) As Task(Of List(Of ODfile))

        Dim oItems As New List(Of ODfile)

        Try
            Dim oPicLista As Microsoft.OneDrive.Sdk.Item =
                Await _oBuilder.Request().Expand("children").GetAsync
            For Each oPicItem As Microsoft.OneDrive.Sdk.Item In oPicLista.Children.CurrentPage
                If bFolders AndAlso oPicItem.Folder IsNot Nothing Then oItems.Add(New ODfile(oPicItem))
                If bFiles AndAlso oPicItem.File IsNot Nothing Then oItems.Add(New ODfile(oPicItem))
            Next

            If oPicLista.Children.NextPageRequest Is Nothing Then Return oItems

            Dim oPicNew As Microsoft.OneDrive.Sdk.ItemChildrenCollectionPage =
            Await oPicLista.Children.NextPageRequest.GetAsync

            For iGuard As Integer = 1 To 12000 / 200   ' itemow moze byc, przez itemów na stronê
                For Each oPicItem As Microsoft.OneDrive.Sdk.Item In oPicNew.CurrentPage
                    If bFolders AndAlso oPicItem.Folder IsNot Nothing Then oItems.Add(New ODfile(oPicItem))
                    If bFiles AndAlso oPicItem.File IsNot Nothing Then oItems.Add(New ODfile(oPicItem))
                Next
                If oPicNew.NextPageRequest Is Nothing Then Return oItems
                oPicNew = Await oPicNew.NextPageRequest.GetAsync
            Next

            Return oItems

        Catch ex As Exception
            CrashMessageExit("@OneDriveGetAllChildsSDK", ex.Message)
        End Try

        Return Nothing

    End Function

    Public Async Function GetFileAsync(sFilename As String, Optional bCreate As Boolean = False) As Task(Of ODfile)

        Dim oItem As Microsoft.OneDrive.Sdk.Item

        Try
            Dim req = _oBuilder.ItemWithPath(sFilename).Request()
            oItem = Await req.GetAsync
            If oItem IsNot Nothing Then Return New ODfile(oItem)
        Catch
        End Try

        If Not bCreate Then Return Nothing

        ' stwórz plik

        Using oStream As New MemoryStream
            Using oWrtr = New StreamWriter(oStream)
                oWrtr.WriteLine("")
                oWrtr.Flush()
                oStream.Seek(0, SeekOrigin.Begin)

                Try
                    ' utworzy plik dwubajtowy
                    oItem = Await _oBuilder.ItemWithPath(sFilename).Content.Request.PutAsync(Of Microsoft.OneDrive.Sdk.Item)(oStream)
                Catch ex As Exception
                    Return Nothing
                End Try
            End Using
        End Using

        Return New ODfile(oItem)

    End Function

    Private mbInUsunPlikiOneDrive As Boolean = False

    Public Async Function RemoveFileAsync(sFilename As String) As Task

        ' gdy nie ma sieci, przerwij - na wypadek jakby trwa³o Del, a zacz¹³ robiæ fotkê i by³ error powoduj¹cy reset WiFi
        If Not NetIsIPavailable(False) Then Return

        Try

            Await _oBuilder.ItemWithPath(sFilename).Request.DeleteAsync
        Catch ex As Exception
            ' pliku moze nie byc
        End Try

    End Function

    Public Async Function RemoveFilesAsync(lFilesToDel As List(Of String)) As Task

        If mbInUsunPlikiOneDrive Then Exit Function
        mbInUsunPlikiOneDrive = True

        For Each sFileName As String In lFilesToDel
            Await RemoveFileAsync(sFileName)
        Next

        mbInUsunPlikiOneDrive = False
    End Function

    <Obsolete("nieprzetestowane")>
    Public Async Function CopyFileToOneDriveAsync(oFile As Windows.Storage.StorageFile) As Task(Of ODfile)

        Try

            Dim oStream = Await oFile.OpenStreamForReadAsync()
            Dim oItem As Microsoft.OneDrive.Sdk.Item = Nothing
            Dim bError As Boolean = False

            Try
                oItem = Await _oBuilder.ItemWithPath(oFile.Name).
                    Content.Request.PutAsync(Of Microsoft.OneDrive.Sdk.Item)(oStream)   ' (oRdr.BaseStream)
                oItem.LastModifiedDateTime = (Await oFile.GetBasicPropertiesAsync).DateModified
            Catch ex As Exception
                vb14.CrashMessageAdd("@CopyFileToOneDrive while trying to copy file", ex.Message)
                Return Nothing
            End Try

            Return New ODfile(Await _oBuilder.ItemWithPath(oFile.Name).Request.GetAsync)

        Catch ex As Exception
        End Try

        Return Nothing
    End Function

    Public Async Function FileReadStringAsync(sFilename As String) As Task(Of String)
        Dim oFile As ODfile = Await GetFileAsync(sFilename, False)
        If oFile Is Nothing Then Return ""

        Return Await oFile.ReadContentAsync()
    End Function

    ''' <summary>
    ''' Zapisuje plik (overwrite), zwraca false gdy zapis nieudany
    ''' </summary>
    ''' <param name="sFilename"></param>
    ''' <param name="sContent"></param>
    ''' <returns></returns>
    Public Async Function FileWriteStringAsync(sFilename As String, sContent As String) As Task(Of Boolean)
        Dim oFile As ODfile = Await GetFileAsync(sFilename, True)
        If oFile Is Nothing Then Return False

        Return Await oFile.WriteContentAsync(sContent)
    End Function


End Class

Public Class ODfile
    Private ReadOnly _oSDKitem As Microsoft.OneDrive.Sdk.Item

    Public Function GetName() As String
        Dim sRet As String = _oSDKitem.Name
        Dim iInd As Integer = sRet.LastIndexOf("/")
        If iInd < 0 Then Return sRet
        Return sRet.Substring(iInd + 1)
    End Function

    Public Sub New(oItem As Microsoft.OneDrive.Sdk.Item)
        If oItem Is Nothing Then Throw New ArgumentNullException("cannot create ODfile with NULL item")
        _oSDKitem = oItem
    End Sub

    Public Function GetLastModDate() As DateTimeOffset
        Dim oData As DateTimeOffset? = _oSDKitem.LastModifiedDateTime
        If oData Is Nothing Then Return New DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.FromSeconds(0))
        Return oData
    End Function

    Public Async Function GetStreamAsync() As Task(Of Stream)
        'https://msdn.microsoft.com/en-us/magazine/mt632271.aspx

        Dim oItemReq As Microsoft.OneDrive.Sdk.ItemRequestBuilder
        oItemReq = ODclient._oOneDriveClnt.Drive.Items(_oSDKitem.Id)

        Try
            Dim oStream As Stream = Await oItemReq.Content.Request.GetAsync

            Return oStream
        Catch ex As Exception
            vb14.CrashMessageAdd("@GetOneDriveFileStream", ex.Message)
            Return Nothing
        End Try

    End Function

    Public Async Function WriteContentAsync(sTresc As String) As Task(Of Boolean)

        Using oStream As New MemoryStream
            Using oWrtr = New StreamWriter(oStream)
                oWrtr.WriteLine(sTresc)
                oWrtr.Flush()

                oStream.Seek(0, SeekOrigin.Begin)

                Dim oItemReq As Microsoft.OneDrive.Sdk.ItemRequestBuilder
                oItemReq = ODclient._oOneDriveClnt.Drive.Items(_oSDKitem.Id)

                Try
                    Await oItemReq.Content.Request.PutAsync(Of Microsoft.OneDrive.Sdk.Item)(oStream)   ' (oRdr.BaseStream)
                Catch ex As Exception
                    Return False
                End Try
            End Using
        End Using

        Return True
    End Function

    Public Async Function ReadContentAsync() As Task(Of String)
        Dim oStream As Stream = Await GetStreamAsync()
        If oStream Is Nothing Then Return ""

        Dim oRdr = New StreamReader(oStream)
        Dim sTxt As String = oRdr.ReadToEnd()
        oRdr.Dispose()
        oStream.Dispose()

        Return sTxt

    End Function

    <Obsolete("nieprzetestowane")>
    Public Async Function GetLinkAsync() As Task(Of String)
        Dim sLink = Await ODclient._oOneDriveClnt.Drive.Items(_oSDKitem.Id).CreateLink("view").Request().PostAsync()
        Return sLink.Link.ToString
    End Function

End Class


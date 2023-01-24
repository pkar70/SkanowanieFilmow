Imports System.IO

Public Class Publish_AdHoc
    Inherits Vblib.CloudPublish

    Public Const PROVIDERNAME As String = "AdHoc"

    Public Overrides Property sProvider As String = PROVIDERNAME

    Public Overrides Async Function SendFileMain(oPic As Vblib.OnePic) As Task(Of String)

        If String.IsNullOrEmpty(sZmienneZnaczenie) Then Return "ERROR: Publish_AdHoc, folderForFiles is not set"

        ' jesteśmy po pipeline, które jest "piętro wyżej"

        ' zapisz w katalogu docelowym
        Dim sOutFilename As String = IO.Path.Combine(sZmienneZnaczenie, oPic.sSuggestedFilename)
        If IO.File.Exists(sOutFilename) Then IO.File.Delete(sOutFilename)

        Dim oNewFileStream As FileStream = IO.File.OpenWrite(sOutFilename)
        oPic._PipelineOutput.Seek(0, SeekOrigin.Begin)
        Await oPic._PipelineOutput.CopyToAsync(oNewFileStream)
        oNewFileStream.Flush(True)  ' True: próba by widać było przybywające pliki (bez tego jakby dopiero po chwili, i wszystkie razem?)
        oNewFileStream.Dispose()

        ' w innych Publish: uzupelnij info w oPic o publishingu
        ' oPic.AddCloudPublished(konfiguracja.nazwa, "")

        Return ""
    End Function

    Public Overrides Async Function SendFilesMain(oPicki As List(Of Vblib.OnePic), oNextPic As JedenWiecejPlik) As Task(Of String)
        For Each oPic As Vblib.OnePic In oPicki
            Dim sErr As String = Await SendFileMain(oPic)
            If sErr <> "" Then Return sErr
            oNextPic()
        Next
        Return ""
    End Function

#Disable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
    Public Overrides Async Function GetMBfreeSpace() As Task(Of Integer)
        ' *TODO* jak w localstorage
        Throw New NotImplementedException()
    End Function
#Enable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously

    Public Overrides Function CreateNew(oConfig As Vblib.CloudConfig, oPostProcs As PostProcBase(), sDataName As String) As Vblib.AnyStorage
        If oConfig.sProvider <> sProvider Then Return Nothing

        Dim oNew As New Publish_AdHoc
        oNew.konfiguracja = oConfig
        oNew._PostProcs = oPostProcs

        Return oNew
    End Function

    Public Shared DefaultConfig As CloudConfig = New CloudConfig With
    {
        .eTyp = CloudTyp.publish,
    .sProvider = PROVIDERNAME,
    .nazwa = "Default AdHoc provider",
    .enabled = True,
.includeMask = "*.*",
    .defaultExif = New Vblib.ExifTag(Vblib.ExifSource.CloudPublish)
    }

    ' po dodaniu parametru - listy procesorów, może być w OnePic

#Region "bez znaczenia dla Publish typu Ad Hoc"

#Disable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
    Public Overrides Async Function VerifyFileExist(oPic As Vblib.OnePic) As Task(Of String)
        Return "Should not be run for Ad-Hoc"
    End Function

    Public Overrides Async Function VerifyFile(oPic As Vblib.OnePic, oCopyFromArchive As Vblib.LocalStorage) As Task(Of String)
        Return "Should not be run for Ad-Hoc"
    End Function

    Public Overrides Async Function GetFile(oPic As Vblib.OnePic) As Task(Of String)
        Return "Should not be run for Ad-Hoc"
    End Function

    Public Overrides Async Function Login() As Task(Of String)
        Return ""
    End Function

    Protected Overrides Async Function GetRemoteTagsMain(oPic As Vblib.OnePic) As Task(Of String)
        Return "Should not be run for Ad-Hoc"
    End Function

    Public Overrides Async Function Delete(oPic As Vblib.OnePic) As Task(Of String)
        Return "Should not be run for Ad-Hoc"
    End Function

    Public Overrides Async Function GetShareLink(oPic As Vblib.OnePic) As Task(Of String)
        Return "Should not be run for Ad-Hoc"
    End Function

    Public Overrides Async Function GetShareLink(oOneDir As Vblib.OneDir) As Task(Of String)
        Return "Should not be run for Ad-Hoc"
    End Function

    Public Overrides Async Function Logout() As Task(Of String)
        Return ""
    End Function

#Enable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously

#End Region

End Class

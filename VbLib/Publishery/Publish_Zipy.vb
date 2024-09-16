Imports System.IO
Imports System.IO.Compression
Imports System.Security.Cryptography

Public Class Publish_ZIP
    Inherits Vblib.Publish_ZipyBase

    Public Const PROVIDERNAME As String = "ZIP"

    Public Overrides Property sProvider As String = PROVIDERNAME

    Public Shared DefaultConfig As CloudConfig = New CloudConfig With
    {
        .eTyp = CloudTyp.publish,
    .sProvider = PROVIDERNAME,
    .nazwa = "Default Zip provider",
    .enabled = True,
.includeMask = "*.*",
    .defaultExif = New Vblib.ExifTag(Vblib.ExifSource.CloudPublish)
    }

#Disable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
    Public Overrides Async Function SendFilesMain(oPicki As List(Of Vblib.OnePic), oNextPic As JedenWiecejPlik) As Task(Of String)
#Enable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously

        If String.IsNullOrEmpty(sZmienneZnaczenie) Then Return "ERROR: Publish_ZIP, folderForFiles is not set"

        ' jesteśmy po pipeline, które jest "piętro wyżej"

        Dim oZip As ZipArchive = CreateZipArchive()

        Dim indexJson As String = ""

        For Each oPic As Vblib.OnePic In oPicki
            AppendFileToZip(oZip, oPic, oPic.sSuggestedFilename)
            indexJson = indexJson & "," & vbCrLf & oPic.DumpAsJSON

            If oNextPic IsNot Nothing Then oNextPic()   ' zmiana progressBara
        Next

        Dim oNew As ZipArchiveEntry
        indexJson = "[" & vbCrLf & indexJson.Substring(1) & vbCrLf & "]"
        oNew = oZip.CreateEntry("index.json", CompressionLevel.Optimal)
        Using writer As StreamWriter = New StreamWriter(oNew.Open())
            writer.Write(indexJson)
        End Using

        oZip.Dispose()

        Return ""
        ' w innych Publish: uzupelnij info w oPic o publishingu
        ' oPic.AddCloudPublished(konfiguracja.nazwa, "")

    End Function

    Public Overrides Function CreateNew(oConfig As Vblib.CloudConfig, oPostProcs As PostProcBase(), sDataName As String) As Vblib.AnyStorage
        If oConfig.sProvider <> sProvider Then Return Nothing

        Dim oNew As New Publish_ZIP
        oNew.konfiguracja = oConfig
        oNew._PostProcs = oPostProcs

        Return oNew
    End Function

End Class

Public Class Publish_CBZ
    Inherits Vblib.Publish_ZipyBase

    Public Const PROVIDERNAME As String = "CBZ"

    Public Overrides Property sProvider As String = PROVIDERNAME

    Public Shared DefaultConfig As CloudConfig = New CloudConfig With
    {
        .eTyp = CloudTyp.publish,
    .sProvider = PROVIDERNAME,
    .nazwa = "Default CBZ provider",
    .enabled = True,
.includeMask = "*.*",
.defaultPostprocess = "Resize1024;",
    .defaultExif = New Vblib.ExifTag(Vblib.ExifSource.CloudPublish)
    }

#Disable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
    Public Overrides Async Function SendFilesMain(oPicki As List(Of Vblib.OnePic), oNextPic As JedenWiecejPlik) As Task(Of String)
#Enable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously

        If String.IsNullOrEmpty(sZmienneZnaczenie) Then Return "ERROR: Publish_ZIP, folderForFiles is not set"

        ' jesteśmy po pipeline, które jest "piętro wyżej"

        Dim oZip As ZipArchive = CreateZipArchive()

        Dim indexJson As String = ""

        Dim iCnt As Integer = 1

        For Each oPic As Vblib.OnePic In From c In oPicki Order By c.GetMostProbablyDate
            ' dodaj entry
            Dim targetFilename As String = iCnt.ToString("000") & "_" & oPic.sSuggestedFilename
            AppendFileToZip(oZip, oPic, targetFilename)
            indexJson = indexJson & "," & vbCrLf & oPic.DumpAsJSON

            If oNextPic IsNot Nothing Then oNextPic()   ' zmiana progressBara
        Next

        oZip.Dispose()

        Return ""
        ' w innych Publish: uzupelnij info w oPic o publishingu
        ' oPic.AddCloudPublished(konfiguracja.nazwa, "")

    End Function

    Public Overrides Function CreateNew(oConfig As Vblib.CloudConfig, oPostProcs As PostProcBase(), sDataName As String) As Vblib.AnyStorage
        If oConfig.sProvider <> sProvider Then Return Nothing

        Dim oNew As New Publish_CBZ
        oNew.konfiguracja = oConfig
        oNew._PostProcs = oPostProcs

        Return oNew
    End Function
End Class



Public MustInherit Class Publish_ZipyBase
    Inherits Vblib.CloudPublish

    Public Overrides Async Function SendFileMain(oPic As Vblib.OnePic) As Task(Of String)

        Dim lista As New List(Of Vblib.OnePic)
        lista.Add(oPic)
        Return Await SendFilesMain(lista, Nothing)

    End Function

    Protected Function CreateZipArchive() As ZipArchive
        Dim zipMode As ZipArchiveMode
        If IO.File.Exists(sZmienneZnaczenie) Then
            zipMode = ZipArchiveMode.Update
        Else
            zipMode = ZipArchiveMode.Create
        End If

        Dim oZip As ZipArchive = Compression.ZipFile.Open(sZmienneZnaczenie, zipMode)
        Return oZip
    End Function

    Protected Shared Sub AppendFileToZip(oZip As ZipArchive, oPic As OnePic, sSuggestedFilename As String)
        Dim oNew As ZipArchiveEntry
        oPic._PipelineOutput.Seek(0, SeekOrigin.Begin)
        oNew = oZip.CreateEntry(sSuggestedFilename, CompressionLevel.NoCompression)   ' bez kompresji, bo na JPG przecież i tak prawie jej nie ma, a szkoda czasu
        Using writerek As Stream = oNew.Open
            oPic._PipelineOutput.CopyTo(writerek)
        End Using

        oPic._PipelineOutput.Seek(0, SeekOrigin.Begin)  ' przywracamy pointer na przyszłe operacje

    End Sub

#Disable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
    Public Overrides Async Function GetMBfreeSpace() As Task(Of Integer)
        ' *TODO* jak w localstorage
        Throw New NotImplementedException()
    End Function
#Enable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously

    ' po dodaniu parametru - listy procesorów, może być w OnePic

#Region "bez znaczenia dla Publish typu Ad Hoc"

#Disable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
    Public Overrides Async Function VerifyFileExist(oPic As Vblib.OnePic) As Task(Of String)
        Return "Should not be run for ZIPs"
    End Function

    Public Overrides Async Function VerifyFile(oPic As Vblib.OnePic, oCopyFromArchive As Vblib.LocalStorage) As Task(Of String)
        Return "Should not be run for ZIPs"
    End Function

    Public Overrides Async Function GetFile(oPic As Vblib.OnePic) As Task(Of String)
        Return "Should not be run for ZIPs"
    End Function

    Public Overrides Async Function Login() As Task(Of String)
        Return ""
    End Function

    Protected Overrides Async Function GetRemoteTagsMain(oPic As Vblib.OnePic) As Task(Of String)
        Return "Should not be run for ZIPs"
    End Function

    Public Overrides Async Function Delete(oPic As Vblib.OnePic) As Task(Of String)
        Return "Should not be run for ZIPs"
    End Function

    Public Overrides Async Function GetShareLink(oPic As Vblib.OnePic) As Task(Of String)
        Return "Should not be run for ZIPs"
    End Function

    Public Overrides Async Function GetShareLink(oOneDir As Vblib.OneDir) As Task(Of String)
        Return "Should not be run for ZIPs"
    End Function

    Public Overrides Async Function Logout() As Task(Of String)
        Return ""
    End Function

#Enable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously

#End Region

End Class

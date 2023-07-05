Imports System.IO
Imports System.Runtime.InteropServices.WindowsRuntime
Imports AsNuget_UseMapi.SendFileTo
Imports Vblib


Public Class Publish_Email
    Inherits Vblib.CloudPublish

    Public Const PROVIDERNAME As String = "Email"

    Public Overrides Property sProvider As String = PROVIDERNAME

    Public Overrides Async Function SendFileMain(oPic As Vblib.OnePic) As Task(Of String)
        ' jesteśmy po pipeline, które jest "piętro wyżej"

        Dim email As AsNuget_UseMapi.SendFileTo.MAPI = GetEmailObj()

        Dim attchs As New Dictionary(Of String, Stream)
        attchs.Add(oPic.sSuggestedFilename, oPic._PipelineOutput)

        Return Await SendEmail(email, attchs)

    End Function

    Public Overrides Async Function SendFilesMain(oPicki As List(Of Vblib.OnePic), oNextPic As JedenWiecejPlik) As Task(Of String)
        ' jesteśmy po pipeline, które jest "piętro wyżej"

        Dim email As AsNuget_UseMapi.SendFileTo.MAPI = GetEmailObj()

        Dim attchs As New Dictionary(Of String, Stream)
        For Each oPic As Vblib.OnePic In oPicki
            attchs.Add(oPic.sSuggestedFilename, oPic._PipelineOutput)
        Next

        Return Await SendEmail(email, attchs)

    End Function

    Private Function GetEmailObj() As AsNuget_UseMapi.SendFileTo.MAPI
        Dim email As New AsNuget_UseMapi.SendFileTo.MAPI
        If Not String.IsNullOrEmpty(konfiguracja.sUsername) Then email.AddRecipientTo(konfiguracja.sUsername)
        Return email
    End Function

    Private Async Function SendEmail(email As AsNuget_UseMapi.SendFileTo.MAPI, attchs As Dictionary(Of String, Stream)) As Task(Of String)

        ' tworzymy pliki tymczasowe
        Dim tempsdir As String = IO.Path.Combine(IO.Path.GetTempPath, "PicSort")
        IO.Directory.CreateDirectory(tempsdir)

        Dim tempsy As New List(Of String)

        For Each oAtt As KeyValuePair(Of String, Stream) In attchs
            Dim sOutFilename As String = IO.Path.Combine(tempsdir, oAtt.Key)
            If IO.File.Exists(sOutFilename) Then IO.File.Delete(sOutFilename)
            tempsy.Add(sOutFilename)

            Using oNewFileStream As FileStream = IO.File.OpenWrite(sOutFilename)
                oAtt.Value.Seek(0, SeekOrigin.Begin)
                Await oAtt.Value.CopyToAsync(oNewFileStream)
            End Using

            email.AddAttachment(sOutFilename)
        Next

        Dim sSubj As String = "Zdjęcia"
        If Not String.IsNullOrEmpty(konfiguracja.sPswd) Then sSubj = konfiguracja.sPswd

        email.SendMailPopup(sSubj, "Przesyłam zdjęcia")

        ' kasujemy pliki tymczasowe
        For Each filename As String In tempsy
            IO.File.Delete(filename)
        Next

        Return ""
    End Function

#Disable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
    Public Overrides Async Function GetMBfreeSpace() As Task(Of Integer)
        Dim freeSpaceMB As Integer = VbLibCore3_picSource.LocalStorageMiddle.GetMBfreeSpaceForPath(IO.Path.GetTempPath)
        Return freeSpaceMB

    End Function
#Enable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously

    Public Overrides Function CreateNew(oConfig As Vblib.CloudConfig, oPostProcs As PostProcBase(), sDataName As String) As Vblib.AnyStorage
            If oConfig.sProvider <> sProvider Then Return Nothing

        Dim oNew As New Publish_Email
        oNew.konfiguracja = oConfig
            oNew._PostProcs = oPostProcs

            Return oNew
        End Function

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


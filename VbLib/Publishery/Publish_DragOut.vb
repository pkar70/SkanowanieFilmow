Imports System.IO

Public Class Publish_DragOut
    Inherits Vblib.CloudPublish

    Public Const PROVIDERNAME As String = "DragOut"

    Public Overrides Property sProvider As String = PROVIDERNAME

#Disable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
    Public Overrides Async Function SendFileMain(oPic As Vblib.OnePic) As Task(Of String)
        Return "ERROR: should not be called directly!"
    End Function

    Public Overrides Async Function SendFilesMain(oPicki As List(Of Vblib.OnePic), oNextPic As JedenWiecejPlik) As Task(Of String)
        Return "ERROR: should not be called directly!"
    End Function

    Public Overrides Async Function GetMBfreeSpace() As Task(Of Integer)
        ' *TODO* jak w localstorage
        Throw New NotImplementedException()
    End Function
#Enable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously

    Public Overrides Function CreateNew(oConfig As Vblib.CloudConfig, oPostProcs As PostProcBase(), sDataName As String) As Vblib.AnyStorage
        If oConfig.sProvider <> sProvider Then Return Nothing

        Dim oNew As New Publish_DragOut
        oNew.konfiguracja = oConfig
        oNew._PostProcs = oPostProcs

        Return oNew
    End Function

    Public Shared DefaultConfig As CloudConfig = New CloudConfig With
    {
        .eTyp = CloudTyp.publish,
    .sProvider = PROVIDERNAME,
    .nazwa = "Default DragOut provider",
    .enabled = True,
.includeMask = "*.*",
    .defaultExif = New Vblib.ExifTag(Vblib.ExifSource.CloudPublish)
    }

    ' po dodaniu parametru - listy procesorów, może być w OnePic

#Region "bez znaczenia dla Publish typu DragOut"

#Disable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
    Public Overrides Async Function VerifyFileExist(oPic As Vblib.OnePic) As Task(Of String)
        Return "Should not be run for DragOut"
    End Function

    Public Overrides Async Function VerifyFile(oPic As Vblib.OnePic, oCopyFromArchive As Vblib.LocalStorage) As Task(Of String)
        Return "Should not be run for DragOut"
    End Function

    Public Overrides Async Function GetFile(oPic As Vblib.OnePic) As Task(Of String)
        Return "Should not be run for DragOut"
    End Function

    Public Overrides Async Function Login() As Task(Of String)
        Return ""
    End Function

    Protected Overrides Async Function GetRemoteTagsMain(oPic As Vblib.OnePic) As Task(Of String)
        Return "Should not be run for DragOut"
    End Function

    Public Overrides Async Function Delete(oPic As Vblib.OnePic) As Task(Of String)
        Return "Should not be run for DragOut"
    End Function

    Public Overrides Async Function GetShareLink(oPic As Vblib.OnePic) As Task(Of String)
        Return "Should not be run for DragOut"
    End Function

    Public Overrides Async Function GetShareLink(oOneDir As Vblib.OneDir) As Task(Of String)
        Return "Should not be run for DragOut"
    End Function

    Public Overrides Async Function Logout() As Task(Of String)
        Return ""
    End Function

#Enable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously

#End Region

End Class

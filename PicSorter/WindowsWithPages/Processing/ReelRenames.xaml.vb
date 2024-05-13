
Imports pkar
Imports pkar.UI.Extensions

Public Class ReelRenames
    Private Property _RenameDirList As New ObservableList(Of OneRenameDir)


    Public Sub New(filesIn As IReadOnlyList(Of Vblib.OnePic), rootDir As String)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        If filesIn Is Nothing Then Return
        For Each oPic As Vblib.OnePic In filesIn
            Dim juzMam As OneRenameDir = _RenameDirList.Find(Function(x) x.fromDir = ".\" & IO.Path.GetDirectoryName(oPic.sInSourceID).Replace(rootDir, ""))
            If juzMam IsNot Nothing Then
                juzMam.counter += 1
                If juzMam.counter < 5 Then
                    juzMam.dymek = juzMam.dymek & vbCrLf & oPic.sSuggestedFilename
                End If
            Else
                Dim oNew As New OneRenameDir
                oNew.dymek = oPic.sSuggestedFilename
                oNew.toDir = IO.Path.GetDirectoryName(oPic.sInSourceID).Replace(rootDir, "")
                oNew.fromDir = ".\" & oNew.toDir
                _RenameDirList.Add(oNew)
            End If
        Next
    End Sub

    ''' <summary>
    ''' Podmień nazwy w OnePic: sSuggestedFilename, TargetDir; ustaw Reel
    ''' </summary>
    ''' <param name="picek"></param>
    ''' <returns></returns>
    Public Function RenamesInOnePic(oPic As Vblib.OnePic) As Boolean

        Dim juzMam As OneRenameDir = _RenameDirList.Find(Function(x) x.fromDir = IO.Path.GetDirectoryName(oPic.sInSourceID))
        If juzMam Is Nothing Then Return False

        oPic.TargetDir = "reel\" & juzMam.toDir.Replace(".", "\").Replace("_", "\")

        Dim oExif As Vblib.ExifTag = oPic.GetExifOfType(Vblib.ExifSource.SourceDefault)
        oExif.ReelName = juzMam.toDir.Replace("\", "_")

        Select Case juzMam.renameMode.ToLowerInvariant
            Case "prefix"
                oPic.sSuggestedFilename = oExif.ReelName & "_" & oPic.sSuggestedFilename
            Case "prefix + ASC"
                oPic.sSuggestedFilename = oExif.ReelName & "_" & JustifiedFrameNumber(juzMam, False)
            Case "prefix + DESC"
                oPic.sSuggestedFilename = oExif.ReelName & "_" & JustifiedFrameNumber(juzMam, True)
        End Select

        Return True

    End Function

    Private Function JustifiedFrameNumber(renDir As OneRenameDir, bDown As Boolean) As String
        Dim maxNumLen As Integer = renDir.counter.ToString.Length

        Dim currnum = If(bDown, renDir.counter - renDir.currNum + 1, renDir.currNum)
        renDir.currNum += 1

        Dim ret As String = currnum.ToString
        While ret.Length < maxNumLen
            ret = "0" & ret
        End While

        Return ret
    End Function


    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        uiLista.ItemsSource = _RenameDirList
    End Sub

    Private Sub uiCommPrefix_TextChanged(sender As Object, e As TextChangedEventArgs) Handles uiCommPrefix.TextChanged
        Dim prefix As String = uiCommPrefix.Text.Replace(".", "\").Replace("_", "\")
        For Each oDir As OneRenameDir In _RenameDirList
            oDir.toDir = prefix & "\" & oDir.fromDir.Substring(2)
        Next
        uiLista.ItemsSource = Nothing
        uiLista.ItemsSource = _RenameDirList
        Mouse.OverrideCursor = Nothing  ' już nie będzie klepsydry (z poprzdniego okna0
    End Sub

    Private Sub pButton_Click(sender As Object, e As RoutedEventArgs)
        Me.DialogResult = True
        Me.Close()
    End Sub


    Protected Class OneRenameDir
        Public Property fromDir As String
        Public Property toDir As String
        Public Property counter As Integer = 1
        Public Property dymek As String
        Public Property renameMode As String = "prefix"
        Public Property currNum As Integer = 1
    End Class

End Class


Imports System.ComponentModel
Imports pkar
Imports pkar.UI.Extensions

Public Class ReelRenames
    Private Property _RenameDirList As New ObservableList(Of OneRenameDir)

    Private Const NAZW_W_DYMKU As Integer = 8
    Private _rootDir As String

    Public Sub New(filesIn As IReadOnlyList(Of Vblib.OnePic), rootDir As String)

        ' This call is required by the designer.
        InitializeComponent()

        _rootDir = rootDir

        ' Add any initialization after the InitializeComponent() call.
        If filesIn Is Nothing Then Return
        For Each oPic As Vblib.OnePic In filesIn
            Dim juzMam As OneRenameDir = _RenameDirList.Find(Function(x) x.toDir = StworzToDir(oPic.sInSourceID, rootDir))
            If juzMam IsNot Nothing Then
                juzMam.counter += 1
                If juzMam.counter < NAZW_W_DYMKU Then
                    juzMam.dymek = juzMam.dymek & vbCrLf & oPic.sSuggestedFilename
                Else
                    If juzMam.counter = NAZW_W_DYMKU Then
                        juzMam.dymek = juzMam.dymek & vbCrLf & "..."
                    End If
                End If
            Else
                Dim oNew As New OneRenameDir
                oNew.dymek = oPic.sSuggestedFilename
                ' bez początkowego "\", zaczynamy od nazwy
                oNew.toDir = StworzToDir(oPic.sInSourceID, rootDir)
                oNew.fromDir = ".\" & oNew.toDir
                _RenameDirList.Add(oNew)
            End If
        Next
    End Sub

    Private Function StworzToDir(sourceId As String, rootDir As String)
        Dim ret As String = IO.Path.GetDirectoryName(sourceId).Replace(rootDir, "")
        If ret.StartsWith("\") Then ret = ret.Substring(1)
        Return ret
    End Function


    ''' <summary>
    ''' Podmień nazwy w OnePic: sSuggestedFilename, TargetDir; ustaw Reel
    ''' </summary>
    ''' <param name="picek"></param>
    ''' <returns></returns>
    Public Function RenamesInOnePic(oPic As Vblib.OnePic) As Boolean

        Dim juzMam As OneRenameDir = _RenameDirList.Find(Function(x) x.fromDir = ".\" & StworzToDir(oPic.sInSourceID, _rootDir))
        If juzMam Is Nothing Then Return False

        oPic.TargetDir = "reel\" & juzMam.toDir.Replace(".", "\").Replace("_", "\")
        If oPic.TargetDir.EndsWith("\") Then oPic.TargetDir = oPic.TargetDir.Substring(0, oPic.TargetDir.Length - 1)

        Dim oExif As Vblib.ExifTag = oPic.GetExifOfType(Vblib.ExifSource.SourceDefault)
        oExif.ReelName = juzMam.toDir.Replace("\", "_")
        If oExif.ReelName.EndsWith("_") Then oExif.ReelName = oExif.ReelName.Substring(0, oExif.ReelName.Length - 1)

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
            oDir.NotifyPropChange("toDir")
        Next

        Mouse.OverrideCursor = Nothing  ' już nie będzie klepsydry (z poprzdniego okna)
    End Sub

    Private Sub pButton_Click(sender As Object, e As RoutedEventArgs)
        Me.DialogResult = True
        Me.Close()
    End Sub


    Protected Class OneRenameDir
        Implements INotifyPropertyChanged

        Public Property fromDir As String
        Public Property toDir As String
        Public Property counter As Integer = 1
        Public Property dymek As String
        Public Property renameMode As String = "prefix"
        Public Property currNum As Integer = 1

        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

        Public Sub NotifyPropChange(propertyName As String)
            ' ale do niektórych to onepic się zmienia, więc niby rekurencyjnie powinno być :)
            Dim evChProp As New PropertyChangedEventArgs(propertyName)
            RaiseEvent PropertyChanged(Me, evChProp)
        End Sub

    End Class

End Class

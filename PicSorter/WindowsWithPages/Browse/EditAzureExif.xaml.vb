Public Class EditOneExif
    Private _orgExifJSON As String
    Private _ExifSource As String
    Private _readonly As Boolean

    Public Sub New(sExifSource As String, bReadOnly As Boolean)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        _ExifSource = sExifSource
        _readonly = bReadOnly
    End Sub

    Private Sub uiApply_Click(sender As Object, e As RoutedEventArgs)

        ' bez zmian
        If _orgExifJSON = uiExif.Text Then Return

        Dim oPicek As ProcessBrowse.ThumbPicek = DataContext

        Dim _oNewExif As Vblib.ExifTag

        Try
            _oNewExif = Newtonsoft.Json.JsonConvert.DeserializeObject(uiExif.Text, GetType(Vblib.ExifTag))
        Catch ex As Exception
            Return
        End Try

        _oNewExif.ExifSource = _ExifSource
        If _ExifSource = Vblib.ExifSource.AutoAzure Then
            _oNewExif.UserComment = _oNewExif.AzureAnalysis.ToUserComment
        End If

        oPicek.oPic.ReplaceOrAddExif(_oNewExif)

    End Sub

    Private Sub Window_DataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs)
        Dim oPicek As ProcessBrowse.ThumbPicek = DataContext
        Me.Title = "EXIF: " & _ExifSource
        uiFileName.Text = oPicek.oPic.sSuggestedFilename

        Dim oExif As Vblib.ExifTag = oPicek.oPic.GetExifOfType(_ExifSource)?.Clone ' żeby UserComment się nie wyzerowało w oryginale

        If oExif Is Nothing AndAlso _ExifSource = Vblib.ExifSource.Flattened Then
            oExif = oPicek.oPic.FlattenExifs(False)
        End If

        If _ExifSource = Vblib.ExifSource.AutoAzure Then
            oExif.UserComment = Nothing ' nie chcemy pokazywać konwersji na TXT, ona się zrobi podczas SAVE
        End If

        If oExif Is Nothing Then
            uiExif.IsEnabled = False
            Return
        End If
        uiExif.IsEnabled = True

        _orgExifJSON = oExif.DumpAsJSON
        uiExif.Text = _orgExifJSON

        uiApply.IsEnabled = Not _readonly
    End Sub

End Class

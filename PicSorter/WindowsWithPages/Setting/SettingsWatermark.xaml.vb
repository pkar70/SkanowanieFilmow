Imports System.IO
Imports System.Net.WebRequestMethods
Imports HiddenWatermark


Class SettingsWatermark
    Private Sub uiVerify_Click(sender As Object, e As RoutedEventArgs)

        Dim sFile As String = BrowseFile("Wskaż plik zdjęcia w którym mam sprawdzić watermark")
        If sFile = "" Then Return

        Dim fileBytes As Byte() = IO.File.ReadAllBytes(sFile)
        Dim result As WatermarkResult = Watermark.Default.RetrieveWatermark(fileBytes)

        uiImage.Source = BajtyNaBitmape(result.RecoveredWatermark)

    End Sub

    Private Sub uiBrowse_Click(sender As Object, e As RoutedEventArgs)
        Dim sFile As String = BrowseFile("Wskaż plik watermark (32×32 px)")
        If sFile = "" Then Return

        Dim fileBytes As Byte() = IO.File.ReadAllBytes(sFile)
        Dim bitmapa As BitmapImage = BajtyNaBitmape(fileBytes)
        If bitmapa.Width <> 32 Or bitmapa.Height <> 32 Then
            Vblib.DialogBox("Obrazek musi być 32×32 piksele")
            Return
        End If

        uiImage.Source = bitmapa

        ' skopiowanie do DataFolder
        IO.File.Copy(sFile, Application.GetDataFile("", "watermark.jpg", False))

    End Sub

    Private Sub uiGenerate_Click(sender As Object, e As RoutedEventArgs)

        If uiWatermarkText1.Text.Length > 3 Or uiWatermarkText2.Text.Length > 3 Then
            Vblib.DialogBox("Tekst może mieć maks. 3 znaki")
            Return
        End If

        Dim sTargetFilename As String = Application.GetDataFile("", "watermark.jpg", False)

        Process_Signature.WatermarkCreate.StworzWatermarkFile(sTargetFilename, uiWatermarkText1.Text, uiWatermarkText2.Text)

        Dim fileBytes As Byte() = IO.File.ReadAllBytes(sTargetFilename)
        Dim bitmapa As BitmapImage = BajtyNaBitmape(fileBytes)
        uiImage.Source = bitmapa
    End Sub

    Public Shared Function BrowseFile(sTitle) As String
        Dim oPicker As New Microsoft.Win32.OpenFileDialog
        oPicker.Title = sTitle
        oPicker.CheckPathExists = True
        oPicker.InitialDirectory = Application.GetDataFolder

        ' Show open file dialog box
        Dim result? As Boolean = oPicker.ShowDialog()

        ' Process open file dialog box results
        If result <> True Then Return ""

        Return oPicker.FileName

    End Function


    Private Shared Function BajtyNaBitmape(aBajty As Byte()) As BitmapImage

        Dim bitmapa As New BitmapImage()

        Using oStream As New MemoryStream(aBajty)
            oStream.Seek(0, SeekOrigin.Begin)
            bitmapa.BeginInit()
            bitmapa.StreamSource = oStream
            bitmapa.CacheOption = BitmapCacheOption.OnLoad
            bitmapa.EndInit()
        End Using

        Return bitmapa
    End Function

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)

        Dim sFile As String = Application.GetDataFile("", "watermark.jpg", False)
        If Not IO.File.Exists(sFile) Then Return

        Dim fileBytes As Byte() = IO.File.ReadAllBytes(sFile)

        Dim bitmapa As BitmapImage = BajtyNaBitmape(fileBytes)
        If bitmapa.Width <> 32 Or bitmapa.Height <> 32 Then
            Vblib.DialogBox("Obrazek musi być 32×32 piksele")
            Return
        End If

        uiImage.Source = bitmapa

    End Sub
End Class

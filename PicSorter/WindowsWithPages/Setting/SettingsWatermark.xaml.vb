'Imports System.IO
'Imports System.Net.WebRequestMethods
Imports System.IO
Imports Chomikuj
Imports HiddenWatermark
Imports pkar.UI.Extensions

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
            Me.MsgBox("Obrazek musi być 32×32 piksele")
            Return
        End If

        uiImage.Source = bitmapa

        ' skopiowanie do DataFolder
        IO.File.Copy(sFile, Vblib.GetDataFile("", "watermark.jpg", False))

    End Sub

    Private Sub uiGenerate_Click(sender As Object, e As RoutedEventArgs)

        If uiWatermarkText1.Text.Length > 3 Or uiWatermarkText2.Text.Length > 3 Then
            Me.MsgBox("Tekst może mieć maks. 3 znaki")
            Return
        End If

        Dim sTargetFilename As String = Vblib.GetDataFile("", "watermark.jpg", False)

        Process_Signature.WatermarkCreate.StworzWatermarkFile(sTargetFilename, uiWatermarkText1.Text, uiWatermarkText2.Text)

        Dim fileBytes As Byte() = IO.File.ReadAllBytes(sTargetFilename)
        Dim bitmapa As BitmapImage = BajtyNaBitmape(fileBytes)
        uiImage.Source = bitmapa
    End Sub

    Public Shared Function BrowseFile(sTitle As String) As String
        Dim oPicker As New Microsoft.Win32.OpenFileDialog
        oPicker.Title = sTitle
        oPicker.CheckPathExists = True
        oPicker.InitialDirectory = Vblib.GetDataFolder

        ' Show open file dialog box
        Dim result? As Boolean = oPicker.ShowDialog()

        ' Process open file dialog box results
        If result <> True Then Return ""

        Return oPicker.FileName

    End Function


    Private Shared Function BajtyNaBitmape(aBajty As Byte()) As BitmapImage

        Dim bitmapa As New BitmapImage()

        Using oStream As New IO.MemoryStream(aBajty)
            oStream.Seek(0, IO.SeekOrigin.Begin)
            bitmapa.BeginInit()
            bitmapa.StreamSource = oStream
            bitmapa.CacheOption = BitmapCacheOption.OnLoad
            bitmapa.EndInit()
        End Using

        Return bitmapa
    End Function

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        Me.InitDialogs

        Dim sFile As String = Vblib.GetDataFile("", "watermark.jpg", False)
        If Not IO.File.Exists(sFile) Then Return

        Dim fileBytes As Byte() = IO.File.ReadAllBytes(sFile)

        Dim bitmapa As BitmapImage = BajtyNaBitmape(fileBytes)
        If bitmapa.Width <> 32 Or bitmapa.Height <> 32 Then
            Me.MsgBox("Obrazek musi być 32×32 piksele")
            Return
        End If

        uiImage.Source = bitmapa

    End Sub

    Private Sub uiEmbed_Click(sender As Object, e As RoutedEventArgs)

        Dim oPicker As Microsoft.Win32.FileDialog
        oPicker = New Microsoft.Win32.OpenFileDialog
        oPicker.Title = "Wskaż źródłowy plik ze zdjęciem"
        oPicker.CheckPathExists = True
        Dim result? As Boolean = oPicker.ShowDialog()
        If result <> True Then Return
        Dim srcJpg As String = oPicker.FileName

        oPicker = New Microsoft.Win32.SaveFileDialog
        oPicker.Title = "Wskaż docelowy plik (zdjęcie + watermark)"
        oPicker.InitialDirectory = IO.Path.GetDirectoryName(srcJpg)
        result = oPicker.ShowDialog()
        If result <> True Then Return

        Dim dstJpg As String = oPicker.FileName

        ' zmienione EnsureWatermarkData z picsort/postprocess/process_watermark
        Dim sWatermarkFile As String = Vblib.GetDataFile("", "watermark.jpg")
        If Not IO.File.Exists(sWatermarkFile) Then
            Me.MsgBox("... ale niestety nie mam ustawionego pliku watermark")
            Return
        End If

        Dim watermarkBytes As Byte() = File.ReadAllBytes(sWatermarkFile)

        Dim watermark As New HiddenWatermark.Watermark(watermarkBytes, True)

        Dim fileBytes As Byte() = IO.File.ReadAllBytes(srcJpg)
        Dim newFileBytes As Byte() = watermark.EmbedWatermark(fileBytes, Vblib.GetSettingsInt("uiJpgQuality"))

        IO.File.WriteAllBytes(dstJpg, newFileBytes)

    End Sub
End Class

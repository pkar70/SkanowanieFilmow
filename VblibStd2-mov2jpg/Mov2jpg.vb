
Imports FFMpegCore

Public Class Mov2jpg

    Public Shared Async Function ExtractFirstFrameToTemp(sMovieFilename As String) As Task(Of String)
        Dim sFilenameForFrame As String = IO.Path.Combine(IO.Path.GetTempPath, sMovieFilename & ".png")
        IO.File.Delete(sFilenameForFrame)

        Try
            ' bêdzie z dodatkiem PNG
            If Not Await FFMpeg.SnapshotAsync(sMovieFilename, sFilenameForFrame) Then Return ""
        Catch ex As Exception
            Return ""    ' zapewne: brak FFMPEG
        End Try

        Return sFilenameForFrame
    End Function


    Public Shared Async Function ExtractFirstFrame(sMovieFilename As String, sFilenameForFrame As String) As Task(Of Boolean)
        ' FFMpeg umie tylko PNG zrobiæ
        Try
            ' bêdzie z dodatkiem PNG
            If Not Await FFMpeg.SnapshotAsync(sMovieFilename, sFilenameForFrame) Then Return False
        Catch ex As Exception
            Return False    ' zapewne: brak FFMPEG
        End Try

        Return True

        '' konwertujemy PNG na JPG
        'Dim bRet As Boolean = Await KonwersjaObrazka(sFilenameForFrame & ".png", sFilenameForFrame, iMaxSize)
        'IO.File.Delete(sFilenameForFrame & ".png")

        'Return bRet
    End Function

    Public Async Function KonwersjaObrazka(sFromPicture As String, sToPicture As String, iMaxSize As Integer) As Task(Of Boolean)

        'Dim oSoftBitmap As wingraph.SoftwareBitmap

        'Using oInputStream As Stream = IO.File.OpenRead(sFromPicture)

        '    Dim oDec As BitmapDecoder = Await BitmapDecoder.CreateAsync(oInputStream)
        '    oSoftBitmap = Await oDec.GetSoftwareBitmapAsync()
        '    If oSoftBitmap Is Nothing Then Return False
        'End Using

        '' teraz zapis jako sToPicture
        'Using oTempStream As New InMemoryRandomAccessStream

        '    Dim oEncoder As wingraph.BitmapEncoder = Await Process_AutoRotate.GetJpgEncoderAsync(oTempStream)

        '    oEncoder.SetSoftwareBitmap(oSoftBitmap)

        '    ' gdy to robiê na zwyklym AsRandomAccessStream to siê wiesza
        '    Await oEncoder.FlushAsync()

        '    Using oOutputStream As Stream = IO.File.Open(sToPicture, FileMode.Create)
        '        oTempStream.AsStream.CopyTo(oOutputStream)
        '    End Using

        'End Using
        Return True
    End Function

    Public Async Function IsFfmpegInstalled() As Task(Of Boolean)
        ' ffmpeg po sciezce szukamy
    End Function

    Public Async Function TryInstallFfmpeg() As Task(Of Boolean)
        ' linki do instalki
    End Function
End Class

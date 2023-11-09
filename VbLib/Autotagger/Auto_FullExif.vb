
' https://www.codeproject.com/Articles/5251929/CompactExifLib-Access-to-EXIF-Tags-in-JPEG-TIFF-an



Imports System.Dynamic
Imports System.Globalization
Imports System.IO
Imports pkar.DotNetExtensions

Public Class AutoTag_FullEXIF
    Inherits AutotaggerBase

    Public Overrides ReadOnly Property Typek As Vblib.AutoTaggerType = Vblib.AutoTaggerType.Local
    Public Overrides ReadOnly Property Nazwa As String = "AUTO_FULLEXIF"
    Public Overrides ReadOnly Property MinWinVersion As String = "7.0"
    Public Overrides ReadOnly Property DymekAbout As String = "Wczytuje pełny dump EXIF z pliku zdjęcia (~ 10 KiB na zdjęcie)"
    Public Overrides ReadOnly Property includeMask As String = "*.jpg;*.jpg.thumb;*.jpeg;"

    ' *TODO* dla NAR (Lumia950), MP4 (Lumia*), AVI (Fuji), MOV (iPhone) są specjalne obsługi


#Disable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
    Public Overrides Async Function GetForFile(oFile As Vblib.OnePic) As Task(Of Vblib.ExifTag)
#Enable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
        If Not oFile.MatchesMasks(includeMask) Then Return Nothing

        ' najpierw to, co umie CompactExif
        If oFile.MatchesMasks("*.jpg;*.jpg.thumb;*.tif;*.tiff;*.png") Then Return GetForFileCompact(oFile)

        ' teraz NAR - wyciągnięcie pliku ze środka

        ' filmy: mov, mp4

        ' filmy: avi
        'If oFile.MatchesMasks("*.avi", "") Then Return GetForAviFile(oFile)
        ' AVI title, subtitle, contributing artist, year, media created, copyright, parenting rating

        Return Nothing  ' nie umiemy jeszcze, ale chcemy umieć (bo w includeMask jest że umiemy)
    End Function

#Region "compact EXIF"

    Public Function GetForFileCompact(oFile As Vblib.OnePic) As Vblib.ExifTag
        Try
            Dim oNewExif As New Vblib.ExifTag(Nazwa)
            oNewExif.UserComment = CompactExifLib.FileExif2String.GetString(oFile.InBufferPathName)
            Return oNewExif
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

#End Region



End Class



Imports Vblib

Public Class Auto_MoonPhase
    Inherits Vblib.AutotaggerBase

    Public Overrides ReadOnly Property Typek As Vblib.AutoTaggerType = Vblib.AutoTaggerType.Local
    Public Overrides ReadOnly Property Nazwa As String = Vblib.ExifSource.AutoMoon
    Public Overrides ReadOnly Property MinWinVersion As String = "7.0"
    Public Overrides ReadOnly Property DymekAbout As String = "Wylicza (dla daty zdjęcia) fazę Księżyca"
    Public Overrides ReadOnly Property includeMask As String = "*.*"
    Public Overrides ReadOnly Property RequireDate As Boolean = True

    Public Overrides Function CanTag(oFile As OnePic) As Boolean
        If Not MyBase.CanTag(oFile) Then Return False

        If Vblib.GetSettingsBool("uiAstroNotWhenWether") Then
            If oFile.GetExifOfType(Vblib.ExifSource.AutoVisCrosWeather) IsNot Nothing Then Return Nothing
            If oFile.GetExifOfType(Vblib.ExifSource.AutoAstro) IsNot Nothing Then Return Nothing
        End If

        If oFile.HasRealDate Then Return True

        ' muszę mieć datę dzienną - ale skany mogą mieć zakres OD - DO ten sam dzień
        Dim dataMax As Date = oFile.GetMaxDate
        Dim dataMin As Date = oFile.GetMinDate

        If dataMin.Day <> dataMax.Day Then Return False
        If dataMin.Month <> dataMax.Month Then Return False
        If dataMin.Year <> dataMax.Year Then Return False

        Return True
    End Function

#Disable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
    Public Overrides Async Function GetForFile(oFile As Vblib.OnePic) As Task(Of Vblib.ExifTag)
#Enable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously

        If Not CanTag(oFile) Then Return Nothing

        ' spawdź datę zdjęcia
        Dim data As Date

        If oFile.HasRealDate Then
            data = oFile.GetMostProbablyDate
        Else
            ' muszę mieć datę dzienną - ale skany mogą mieć zakres OD - DO ten sam dzień
            Dim dataMin As Date = oFile.GetMinDate
            data = oFile.GetMaxDate

            If dataMin.Day <> data.Day Then Return Nothing
            If dataMin.Month <> data.Month Then Return Nothing
            If dataMin.Year <> data.Year Then Return Nothing
        End If

        Dim oAstro As New Vblib.AutoWeatherDay
        oAstro.moonphase = Auto_Astro.GetMoonPhase(data)
        Dim oPogoda As New Vblib.CacheAutoWeather_Item
        oPogoda.day = oAstro

        Dim oExif As New Vblib.ExifTag(Vblib.ExifSource.AutoMoon)
        oExif.PogodaAstro = oPogoda

        Return oExif

    End Function


End Class

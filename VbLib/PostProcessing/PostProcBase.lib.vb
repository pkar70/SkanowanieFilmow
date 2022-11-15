' postprocessing:
' *) znak wodny - https://github.com/mchall/HiddenWatermark , https://www.nuget.org/packages/HiddenWatermark/
' *) podpis widoczny na zdjęciu - narzucony, bądź z (c) brany (z EXIF)
' *) scaling
' *) rozmywanie twarzy

' Juz zrobione:
' *) autorotate
' *) embedExif



Public MustInherit Class PostProcBase
    Public MustOverride Property Nazwa As String

    Public MustOverride Property dymekAbout As String
    Public Overridable Property include As String = "*.jpg"

    'Public Property defaultTags As ExifTag
    'Public Property defaultPublish As List(Of String)   ' lista IDs
    'Public Property exclude As List(Of String)  ' maski regexp
    'Public Property lastDownload As DateTime

    Protected MustOverride Async Function ApplyMain(oPic As OnePic, sNewName As String) As Task(Of Boolean)
    Protected MustOverride Async Function ApplyMain(oPic As OnePic, oExif As ExifTag, sNewName As String) As Task(Of Boolean)


    ''' <summary>
    ''' przetwórz plik, na ten sam bądź do nowego pliku
    ''' </summary>
    ''' <param name="oPic"></param>
    ''' <param name="sNewName"></param>
    ''' <returns></returns>
    Public Async Function Apply(oPic As OnePic, Optional sNewName As String = "") As Task(Of Boolean)
        If Not Vblib.PicSourceBase.MatchesMasks(oPic.InBufferPathName, include, "") Then Return False

        Return Await ApplyMain(oPic, sNewName)
    End Function
    ''' <summary>
    ''' przetwórz plik, na ten sam bądź do nowego pliku
    ''' </summary>
    ''' <param name="oPic"></param>
    ''' <param name="sNewName"></param>
    ''' <returns></returns>
    Public Async Function Apply(oPic As OnePic, oExif As ExifTag, Optional sNewName As String = "") As Task(Of Boolean)
        If Not Vblib.PicSourceBase.MatchesMasks(oPic.InBufferPathName, include, "") Then Return False

        Return Await ApplyMain(oPic, oExif, sNewName)
    End Function

    Public Async Function Apply(oLista As List(Of OnePic)) As Task(Of Boolean)
        Dim bAllOk As Boolean = True

        For Each oPicek As Vblib.OnePic In oLista
            If Not Await Apply(oPicek) Then bAllOk = False
        Next

        Return bAllOk

    End Function

    Public Async Function Apply(oLista As List(Of OnePic), oExif As ExifTag) As Task(Of Boolean)
        Dim bAllOk As Boolean = True

        For Each oPicek As Vblib.OnePic In oLista
            If Not Await Apply(oPicek, oExif) Then bAllOk = False
        Next

        Return bAllOk
    End Function



End Class

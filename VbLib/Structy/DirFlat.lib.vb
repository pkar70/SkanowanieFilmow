


Public Class OneDirFlat1
    Inherits pkar.BaseStruct

    Public Property sId As String   ' 1981.01.23.sb_geo
    Public Property notes As String ' wyjazd do Wieliczki z dziadkami, pociągiem i psem

    'Public Sub New(data As Date, sGeo As String, opis As String)
    '    notes = opis

    '    sId = DateToDirId(data)

    '    If sGeo <> "" Then sId = sId & "_" & sGeo

    'End Sub


    Public Sub New1(sKey As String, opis As String)
        notes = opis
        sId = sKey
    End Sub


    ''' <summary>
    ''' wydzielone żeby utrzymać spójność formatowania dat
    ''' </summary>
    ''' <param name="oDate"></param>
    ''' <returns></returns>
    Public Shared Function DateToDirId1(oDate As Date) As String
        Dim sId As String = oDate.ToString("yyyy.MM.dd.")
        Select Case oDate.DayOfWeek
            Case DayOfWeek.Monday
                sId &= "pn"
            Case DayOfWeek.Tuesday
                sId &= "wt"
            Case DayOfWeek.Wednesday
                sId &= "sr"
            Case DayOfWeek.Thursday
                sId &= "cz"
            Case DayOfWeek.Friday
                sId &= "pt"
            Case DayOfWeek.Saturday
                sId &= "sb"
            Case DayOfWeek.Sunday
                sId &= "nd"
        End Select

        Return sId
    End Function

    Public Function ToComboDisplayName1() As String
        If String.IsNullOrWhiteSpace(notes) Then Return sId
        Dim sRet As String = sId & " ("
        If notes.Length < 24 Then Return sRet & notes & ")"

        Return sRet & notes.Substring(0, 23) & "…)"

    End Function

    ''' <summary>
    ''' zwraca True jeśli to jest DIR z keywordów
    ''' </summary>
    ''' <returns></returns>
    Public Function IsFromKeyword1() As Boolean
        Return IsFromKeyword1(sId)
    End Function

    Public Shared Function IsFromKeyword1(sId As String)
        Return (sId.IndexOfAny({"-", "#", "="}) = 0)
    End Function

    Public Function IsFromDate1() As Boolean
        Return IsFromDate1(sId)
    End Function

    Public Shared Function IsFromDate1(sId As String) As Boolean
        ' daty 1850-2050, format yyyy.MM

        If sId.Length < 22 Then Return False
        If sId.Substring(4, 1) <> "." Then Return False

        Dim temp As Integer
        Try
            temp = Integer.Parse(sId.Substring(0, 4))
        Catch ex As Exception
            Return False
        End Try
        If temp < 1850 Then Return False
        If temp > Date.Now.Year Then Return False   ' zdjęć z przyszlosci nie uznajemy

        Try
            temp = Integer.Parse(sId.Substring(5, 2))
        Catch ex As Exception
            Return False
        End Try

        If temp < 1 Then Return False
        If temp > 12 Then Return False

        Return True

        ' regexy są mniej dokładne, bo 19 jako miesiąc też by przeszło
        'If Regex.IsMatch(sId, "^18[5-9][0-9]\.[0-1][0-9]") Then Return True
        'If Regex.IsMatch(sId, "^19[0-9][0-9]\.[0-1][0-9]") Then Return True
        'If Regex.IsMatch(sId, "^20[0-5][0-9]\.[0-1][0-9]") Then Return True


    End Function

End Class

'Public Class DirsListFlat
'    Inherits MojaLista(Of OneDirFlat)

'    Public Sub New(sFolder As String)
'        MyBase.New(sFolder, "dirslist.json")
'    End Sub

'    Public Function GetFolder(sKey As String) As OneDirFlat
'        For Each oItem As OneDirFlat In _lista
'            If oItem.sId = sKey Then Return oItem
'        Next

'        Return Nothing
'    End Function

'    Public Sub TryAddFolder(sKey As String, sOpis As String)
'        If GetFolder(sKey) IsNot Nothing Then Return

'        _lista.Add(New OneDirFlat(sKey, sOpis))
'    End Sub

'End Class




﻿



Public Class OneDir
    Inherits MojaStruct

    Public Property sId As String   ' 1981.01.23.sb
    Public Property notes As String ' wyjazd do Wieliczki z dziadkami, pociągiem i psem

    Public Sub New(data As Date, sGeo As String, opis As String)
        notes = opis

        sId = data.ToString("yyyy.MM.dd.")
        Select Case data.DayOfWeek
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

        If sGeo <> "" Then sId = sId & "_" & sGeo

    End Sub

#If False Then
    ' [datemin, datemax, geomin, geomax] [keywords - wspólne dla wszystkich pic? ustalane potem jakimiś sprawdzaniami, np wlasnie mingeo/maxgeo na geoname]

    '' może być aktualizowane podczas wstawiania zdjęć do katalogu
    'Public Property minDate As DateTime
    'Public Property maxDate As DateTime

    '' może być aktualizowane podczas wstawiania zdjęć do katalogu
    'Public Property oGeo As MyBasicGeoposition
    'Public Property iGeoRadius As Integer

    '' jak publikować / gdzie publikować
    'Public Property defaultPublish As String
    'Public Property denyPublish As String

    'Public Property SubItems As List(Of OneDir)

#Region "na potrzeby pokazywania do wyboru przy oznaczaniu itp."

    <JsonIgnore>
    Public Property bEnabled As Boolean
    <JsonIgnore>
    Public Property bChecked As Boolean
#End Region
#End If

End Class

Public Class DirsList
    Inherits MojaLista(Of OneDir)

    Public Sub New(sFolder As String)
        MyBase.New(sFolder, "dirslist.json")
    End Sub

    Public Function GetFolder(sKey As String) As OneDir
        For Each oItem As OneDir In _lista
            If oItem.sId = sKey Then Return oItem
        Next

        Return Nothing
    End Function


End Class




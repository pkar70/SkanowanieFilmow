﻿
Imports System.Text.RegularExpressions
Imports Vblib
Imports vb14 = Vblib.pkarlibmodule14
Imports pkar
Imports pkar.DotNetExtensions

Class SettingsMapsy


    Public Shared Sub WypelnMenuMapami(oMenuItem As MenuItem, oEvent As RoutedEventHandler)
        vb14.DumpCurrMethod()
        initLista()

        oMenuItem.Items.Clear()

        For Each oEngine As Vblib.JednaMapa In _lista
            Dim oNew As New MenuItem
            oNew.Header = oEngine.nazwa
            oNew.DataContext = oEngine
            AddHandler oNew.Click, oEvent
            oMenuItem.Items.Add(oNew)
        Next

        oMenuItem.IsEnabled = (oMenuItem.Items.Count > 0)

    End Sub

    Private Shared _lista As Vblib.Mapy

    Private Shared Sub initLista()
        If _lista IsNot Nothing Then Return

        _lista = New Vblib.Mapy(Application.GetDataFolder)
        _lista.Load()
    End Sub


    Private Sub uiOk_Click(sender As Object, e As RoutedEventArgs)
        _lista.Save()
    End Sub

    Private Shared Function LinkToName(sLink As String) As String
        ' *TODO* wczytaj nazwę mapy

        Dim sNazwa As String = sLink
        Dim iInd As Integer = sNazwa.IndexOfOrdinal("://")
        sNazwa = sNazwa.Substring(iInd + 3)
        iInd = sNazwa.IndexOf(":")
        sNazwa = sNazwa.Substring(0, iInd)
        sNazwa = sNazwa.Replace(".pl", "")
        sNazwa = sNazwa.Replace(".com", "")
        sNazwa = sNazwa.Replace(".org", "")
        sNazwa = sNazwa.Replace("www.", "")
        sNazwa = sNazwa.Replace("mapa.", "")

        Return sNazwa
    End Function

    Private Async Sub uiAddMapa_Click(sender As Object, e As RoutedEventArgs)

        Dim sLink As String = Await vb14.DialogBoxInputAllDirectAsync("Podaj link do nowej mapy" & vbCrLf & "(użyj %lat i %lon)")
        If sLink = "" Then Return

        If Not sLink.ContainsCS("%lat") Or Not sLink.ContainsCS("%lon") Then
            vb14.DialogBox("Link powinien zawierac %lat i %long")
            Return
        End If

        Dim oNew As New Vblib.JednaMapa(LinkToName(sLink), sLink)
        _lista.Add(oNew)

        uiLista.ItemsSource = _lista

        ' bez Save - to będzie na [OK]

    End Sub

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        initLista()
        uiLista.ItemsSource = _lista.GetList
    End Sub

    ''' <summary>
    ''' zamień link do mapy na współrzędne
    ''' </summary>
    ''' <param name="sLink"></param>
    ''' <returns></returns>
    Public Shared Function Link2Geo(sLink As String) As BasicGeopos
        initLista()

        If sLink.Length < 15 Then Return BasicGeopos.Empty

        ' https://www.openstreetmap.org/way/830020459#map=18/50.01990/19.97866
        Dim iInd As Integer = sLink.IndexOf("/", 10)
        Dim sPrefix As String = sLink.Substring(0, iInd).ToLowerInvariant

        For Each oMapService As Vblib.JednaMapa In _lista
            If Not oMapService.link.StartsWithCIAI(sPrefix) Then Continue For

#If NOT_GEONUGET_121 Then
            Dim iLat As Integer = oMapService.link.IndexOf("%lat")
            Dim iLon As Integer = oMapService.link.IndexOf("%lon")

            Dim sRegMask As String = oMapService.link.Replace("%lon", "([\.0-9]*)").
            Replace("%lat", "([\.0-9]*)").
            Replace("%zoom", "[0-9]*")

            Dim result As Match = Regex.Match(sLink, sRegMask, RegexOptions.IgnoreCase)

            If Not result.Success Then Return BasicGeopos.Empty

            Try
                If iLat < iLon Then
                    Return New BasicGeopos(result.Groups(1).Value, result.Groups(2).Value)
                Else
                    Return New BasicGeopos(result.Groups(2).Value, result.Groups(1).Value)
                End If

            Catch ex As Exception
                Return BasicGeopos.Empty
            End Try

            ' ok, to już mamy
            ' https://www.openstreetmap.org/#map=16/%lat/%lon
            '  no właśnie, jest inaczej :)

#Else
            ' nuget 1.2.1
            Dim oGeo As BasicGeopos = BasicGeopos.FromLink(oMapService.link, sLink)
            If oGeo Is Nothing Then Return BasicGeopos.Empty
            Return oGeo
#End If
        Next

        Return BasicGeopos.Empty

    End Function
End Class

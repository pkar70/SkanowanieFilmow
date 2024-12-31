
'Imports System.Text.RegularExpressions
Imports Vblib
Imports vb14 = Vblib.pkarlibmodule14
Imports pkar
'Imports pkar.DotNetExtensions
Imports pkar.UI.Extensions

Class SettingsMapsy

    Private Shared _lista As New BaseList(Of JednaMapa)(Vblib.GetDataFolder, "mapy.json")

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        Me.InitDialogs

        uiLista.ItemsSource = _lista.OrderBy(Of String)(Function(x) x.nazwa)
    End Sub

    Public Shared Function GetListaMap() As BaseList(Of JednaMapa)
        Return _lista
    End Function

    Public Shared Sub DodajMapyDoNugeta()
        ' wywoływany z main
        _lista.Load()

        ' dodaj do Nugeta mapy zdefiniowane lokalnie
        For Each oMapa As JednaMapa In _lista
            If Not BasicGeopos.MapServices.ContainsKey(oMapa.nazwa) Then
                BasicGeopos.MapServices.Add(oMapa.nazwa, oMapa.link)
            End If
        Next

        ' dodaj mapy z nugeta
        For Each oMapa In BasicGeopos.MapServices
            _lista.Add(New JednaMapa(oMapa.Value) With {.nazwa = oMapa.Key})
        Next

        Dim mainy As String = Vblib.GetSettingsString("mainmapsy")

        For Each oMapa As JednaMapa In _lista
            oMapa.isMain = mainy.ContainsCI(oMapa.nazwa)
        Next

    End Sub


    Private Sub uiOk_Click(sender As Object, e As RoutedEventArgs)
        '_lista.Save()
        'DodajMapyDoNugeta() ' po zapisaniu - dopisz do Nugeta

        Dim sett As String = ""

        For Each oMapa As JednaMapa In _lista
            If oMapa.isMain Then sett &= "|" & oMapa.nazwa
        Next
        Vblib.SetSettingsString("mainmapsy", sett)
        Me.GoBack
    End Sub



    Private Async Sub uiAddMapa_Click(sender As Object, e As RoutedEventArgs)

        Dim sLink As String = Await Me.InputBoxAsync("Podaj link do nowej mapy" & vbCrLf & "(użyj %lat i %lon)")
        If sLink = "" Then Return

        If Not sLink.ContainsCS("%lat") Or Not sLink.ContainsCS("%lon") Then
            Me.MsgBox("Link powinien zawierac %lat i %long")
            Return
        End If

        _lista.Add(New JednaMapa(sLink))
        'uiLista.ItemsSource = Nothing ' to jest IObservableList, nie trzeba
        'uiLista.ItemsSource = BasicGeopos.MapServices

    End Sub


    Public Class JednaMapa
        Inherits BaseStruct

        Public Property nazwa As String
        Public Property link As String
        Public Property isMain As Boolean

        Public Sub New(link As String)

            Me.link = link
            Me.nazwa = "(newlink)"

            Try
                Dim sNazwa As String = link
                Dim iInd As Integer = sNazwa.IndexOfOrdinal("://")
                sNazwa = sNazwa.Substring(iInd + 3)
                iInd = sNazwa.IndexOf("?")
                If iInd > 0 Then sNazwa = sNazwa.Substring(0, iInd)
                iInd = sNazwa.IndexOf("#")
                If iInd > 0 Then sNazwa = sNazwa.Substring(0, iInd)

                sNazwa = sNazwa.Replace(".pl", "")
                sNazwa = sNazwa.Replace(".com", "")
                sNazwa = sNazwa.Replace(".org", "")
                sNazwa = sNazwa.Replace("www.", "")
                sNazwa = sNazwa.Replace("mapa.", "")

                Me.nazwa = sNazwa
            Catch ex As Exception
            End Try

        End Sub

    End Class

End Class

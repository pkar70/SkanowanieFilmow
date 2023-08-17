

Imports System.Globalization

Public Class Mapy
    Inherits pkar.BaseList(Of JednaMapa)

    Public Sub New(sFolder As String)
        MyBase.New(sFolder, "mapy.json")
    End Sub

    Protected Overrides Sub InsertDefaultContent()

        _list.Add(New JednaMapa("OpenStretMap", "https://www.openstreetmap.org/#map=16/%lat/%lon"))
        _list.Add(New JednaMapa("Bing", "https://bing.com/maps/default.aspx?lvl=16&cp=%lat~%lon"))
        _list.Add(New JednaMapa("Google", "https://www.google.pl/maps/@%lat,%lon,16z"))
        _list.Add(New JednaMapa("WirtSzlaki", "https://mapa.wirtualneszlaki.pl/#16/%lat/%lon"))
        _list.Add(New JednaMapa("ArcGIS", "https://www.arcgis.com/home/webmap/viewer.html?center=%lon,%lat&level=10"))
    End Sub


End Class

Public Class JednaMapa
    Inherits pkar.BaseStruct

    Public Property nazwa As String
    Public Property link As String

    Public Sub New(sNazwa As String, sLink As String)
        nazwa = sNazwa
        link = sLink
    End Sub

    Public Function LinkForGeo(oGeo As pkar.BasicGeopos) As String
        If oGeo Is Nothing Then Return ""

        Dim sLink As String = link
#If HERE_FORMATLINK Then
        sLink = sLink.Replace("%lat", oGeo.Latitude)
        sLink = sLink.Replace("%lon", oGeo.Longitude.ToString(CultureInfo.InvariantCulture))
        sLink = sLink.Replace("%zoom", "16")
        Return sLink
#Else
        ' w Nuget 1.2.1
        Return oGeo.FormatLink(link, 16)
#End If
    End Function

    Public Function UriForGeo(oGeo As pkar.BasicGeopos) As Uri
        Return New Uri(LinkForGeo(oGeo))
    End Function

End Class
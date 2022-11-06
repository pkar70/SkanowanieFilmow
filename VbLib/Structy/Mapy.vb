

Imports System.Globalization

Public Class Mapy
    Inherits MojaLista(Of JednaMapa)

    Public Sub New(sFolder As String)
        MyBase.New(sFolder, "mapy.json")
    End Sub

    Public Overrides Function Load() As Boolean

        If MyBase.Load() Then Return True

        ' inicjalizacja defaultami

        _lista.Add(New JednaMapa("OpenStretMap", "https://www.openstreetmap.org/#map=16/%lat/%lon"))
        _lista.Add(New JednaMapa("Bing", "https://bing.com/maps/default.aspx?lvl=16&cp=%lat~%lon"))
        _lista.Add(New JednaMapa("Google", "https://www.google.pl/maps/@%lat,%lon,16z"))
        _lista.Add(New JednaMapa("WirtSzlaki", "https://mapa.wirtualneszlaki.pl/#16/%lat/%lon"))
        _lista.Add(New JednaMapa("ArcGIS", "https://www.arcgis.com/home/webmap/viewer.html?center=%lon,%lat&level=10"))
        Return True
    End Function


End Class

Public Class JednaMapa
    Inherits MojaStruct

    Public Property nazwa As String
    Public Property link As String

    Public Sub New(sNazwa As String, sLink As String)
        nazwa = sNazwa
        link = sLink
    End Sub

    Public Function LinkForGeo(oGeo As MyBasicGeoposition) As String
        If oGeo Is Nothing Then Return ""

        Dim sLink As String = link
        sLink = sLink.Replace("%lat", oGeo.Latitude)
        sLink = sLink.Replace("%lon", oGeo.Longitude.ToString(CultureInfo.InvariantCulture))

        Return sLink
    End Function

    Public Function UriForGeo(oGeo As MyBasicGeoposition) As Uri
        Return New Uri(LinkForGeo(oGeo))
    End Function

End Class
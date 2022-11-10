
Imports Newtonsoft.Json

Public Class OneKeyword
    Inherits MojaStruct

    Public Property sTagId As String
    Public Property sDisplayName As String
    Public Property notes As String
    ' w dwie strony:
    ' a) prezentacja dostępnych tagów dla zdjęcia o znanej dacie
    ' b) datowanie zdjęcia wedle zapisanych tagów
    Public Property minDate As DateTime
    Public Property maxDate As DateTime

    ' tag może mieć współrzędne geograficzne
    Public Property oGeo As MyBasicGeoposition
    Public Property iGeoRadius As Integer

    ' jak publikować / gdzie publikować
    Public Property defaultPublish As String
    Public Property denyPublish As String
    ' *TODO* upload behaviour ' można, nie można, skasować - o co to chodziło? :)
    ' *TODO* można dodać Exif narzucany (chyba)

    Public Property SubItems As List(Of OneKeyword)

#Region "na potrzeby pokazywania do wyboru przy oznaczaniu"

    <JsonIgnore>
    Public Property bEnabled As Boolean
    <JsonIgnore>
    Public Property bChecked As Boolean
#End Region
End Class

Public Class KeywordsList
    Inherits MojaLista(Of OneKeyword)

    Public Sub New(sFolder As String)
        MyBase.New(sFolder, "keywords.json")
    End Sub

    Public Overloads Function Load() As Boolean
        If MyBase.Load() Then
            CalculateMinMaxDateTree
            Return True
        End If

        _lista.Add(New OneKeyword With {.sTagId = "-", .sDisplayName = "osoby"})
        _lista.Add(New OneKeyword With {.sTagId = "#", .sDisplayName = "miejsca"})
        _lista.Add(New OneKeyword With {.sTagId = "=", .sDisplayName = "inne"})

        Return False

    End Function

    Public Function GetKeyword(sKey As String) As OneKeyword
        For Each oItem As OneKeyword In _lista
            If oItem.sTagId = sKey Then Return oItem
        Next

        Return Nothing
    End Function

    ''' <summary>
    '''  w drzewku ustawian dateMin i dateMax hierarchicznie, całość, idąc od dołu (parent zależy od dat w child)
    ''' </summary>
    Public Sub CalculateMinMaxDateTree()

        For Each oItem As OneKeyword In _lista
            CalculateMinMaxDateTree(oItem)
        Next
    End Sub

    ''' <summary>
    '''  w drzewku ustawian dateMin i dateMax hierarchicznie, od tego węzła, idąc od dołu (parent zależy od dat w child)
    ''' </summary>
    Public Sub CalculateMinMaxDateTree(oItem As OneKeyword)

        If oItem.SubItems Is Nothing Then Return

        ' rekurencja najpierw
        For Each oSubItem As OneKeyword In oItem.SubItems
            CalculateMinMaxDateTree(oSubItem)
        Next


        oItem.minDate = CalculateMinDate(oItem)
        oItem.maxDate = CalculateMaxDate(oItem)

    End Sub

    Private Function CalculateMinDate(oItem As OneKeyword) As Date

        Dim oRet As Date = Date.MaxValue

        For Each oSubItem As OneKeyword In oItem.SubItems
            If oSubItem.minDate.Year < 1800 Then Return Date.MinValue

            If oRet > oSubItem.minDate Then oRet = oSubItem.minDate
        Next

        Return oRet
    End Function

    Private Function CalculateMaxDate(oItem As OneKeyword) As Date

        Dim oRet As Date = Date.MinValue

        For Each oSubItem As OneKeyword In oItem.SubItems
            If oSubItem.maxDate.Year < 1800 Then Return Date.MaxValue

            If oRet < oSubItem.maxDate Then oRet = oSubItem.maxDate
        Next

        Return oRet
    End Function



End Class



Imports Newtonsoft.Json
Imports pkar.DotNetExtensions

Public Class OneKeyword
    Inherits pkar.BaseStruct

    Public Property sId As String
    Public Property sDisplayName As String
    Public Property notes As String
    ' w dwie strony:
    ' a) prezentacja dostępnych tagów dla zdjęcia o znanej dacie
    ' b) datowanie zdjęcia wedle zapisanych tagów
    Public Property minDate As DateTime
    Public Property maxDate As DateTime

    ' tag może mieć współrzędne geograficzne
    Public Property oGeo As pkar.BasicGeopos
    Public Property iGeoRadius As Integer

    '' jak publikować / gdzie publikować
    'Public Property defaultPublish As String
    Public Property denyPublish As Boolean
    ' *TODO* upload behaviour ' można, nie można, skasować - o co to chodziło? :)
    ' *TODO* można dodać Exif narzucany (chyba)

    Public Property ownDir As String

    Public Property SubItems As List(Of OneKeyword)

#Region "na potrzeby pokazywania do wyboru przy oznaczaniu"

    <JsonIgnore>
    Public Property bEnabled As Boolean
    <JsonIgnore>
    Public Property bChecked As Boolean

#End Region

    Public Function ToComboDisplayName() As String
        If String.IsNullOrWhiteSpace(sDisplayName) Then Return sId
        Dim sRet As String = sId & " ("
        If sDisplayName.Length < 24 Then Return sRet & sDisplayName & ")"

        Return sRet & sDisplayName.Substring(0, 23) & "…)"

    End Function

    Public Function ToFlatList() As List(Of OneKeyword)
        ' DumpCurrMethod(sId)
        Dim lista As New List(Of OneKeyword)

        lista.Add(Me)

        If SubItems IsNot Nothing Then
            For Each oChild As OneKeyword In SubItems
                lista = lista.Concat(oChild.ToFlatList).ToList
            Next
        End If

        Return lista
    End Function

    Public Function IsRoot() As Boolean
        Return sId.Length = 1
    End Function

End Class

Public Class KeywordsList
    Inherits pkar.BaseList(Of OneKeyword)

    Public Sub New(sFolder As String)
        MyBase.New(sFolder, "keywords.json")
    End Sub

    Public Overloads Function Load() As Boolean
        If Not MyBase.Load() Then Return False
        CalculateMinMaxDateTree()
        Return True
    End Function

    Protected Overrides Sub InsertDefaultContent()
        Add(New OneKeyword With {.sId = "-", .sDisplayName = "osoby"})
        Add(New OneKeyword With {.sId = "#", .sDisplayName = "miejsca"})
        Add(New OneKeyword With {.sId = "=", .sDisplayName = "inne"})
    End Sub

    Public Function GetKeyword(sKey As String) As OneKeyword
        Return ToFlatList.Find(Function(x) x.sId = sKey)
    End Function

    Public Function GetKeywordsList(sKeys As String) As List(Of OneKeyword)
        Dim sSlowka As String = sKeys.Replace("-", ",-").Replace("#", ",#").Replace("=", ",=")
        sSlowka = sSlowka.Replace(",,", ",")
        Dim aKwds As String() = sSlowka.Split(",")

        Dim lista As New List(Of OneKeyword)
        For Each sKwd As String In aKwds
            Dim oNew As OneKeyword = GetKeyword(sKwd)
            If oNew IsNot Nothing Then lista.Add(oNew)
        Next

        Return lista
    End Function

    Public Function IsAdultInAnyKeyword(sKeys As String) As Boolean
        Dim lista As List(Of OneKeyword) = GetKeywordsList(sKeys)
        If lista Is Nothing Then Return False
        For Each kwd As OneKeyword In lista
            If kwd.denyPublish Then Return True
        Next
        Return False
    End Function


    Public Function ToFlatList() As List(Of OneKeyword)
        Dim lista As New List(Of OneKeyword)

        Me.ForEach(Sub(x) lista = lista.Concat(x.ToFlatList).ToList)

        Return lista
    End Function

    Public Sub EnableDisableAll(bEnable As Boolean)
        ToFlatList.ForEach(Sub(x) x.bEnabled = True)
    End Sub


#Region "przeliczanie dat w drzewku"

    ''' <summary>
    '''  w drzewku ustawian dateMin i dateMax hierarchicznie, całość, idąc od dołu (parent zależy od dat w child)
    ''' </summary>
    Public Sub CalculateMinMaxDateTree()
        ForEach(Sub(x) CalculateMinMaxDateTree(x))
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
            If Not oSubItem.minDate.IsDateValid Then Return Date.MaxValue
            oRet = oRet.Min(oSubItem.minDate)
        Next

        Return oRet
    End Function

    Private Function CalculateMaxDate(oItem As OneKeyword) As Date

        Dim oRet As Date = Date.MinValue

        For Each oSubItem As OneKeyword In oItem.SubItems
            If Not oSubItem.maxDate.IsDateValid Then Return Date.MinValue
            oRet = oRet.Max(oSubItem.maxDate)
            'If oSubItem.maxDate.Year < 1800 Then Return Date.MaxValue

            'If oRet < oSubItem.maxDate Then oRet = oSubItem.maxDate
        Next

        Return oRet
    End Function

#End Region

#Region "dla query - szukanie parentów oraz childów"

    '''' <summary>
    '''' dla podanego KEYWORD znajdź wszystkie parenty, aż do root, np. -JA => -JA -RO -R, -DW => -DW -ROK -RO -R
    '''' </summary>
    'Public Function GetAllParents(keyword As String) As String

    'End Function

    ''' <summary>
    ''' dla podanego KEYWORD znajdź wszystkie childy, np. -ROK => -ROK -DW -BS ...
    ''' </summary>
    Public Function GetAllChilds(keyword As String) As String
        Return GetAllChilds(GetKeyword(keyword))
    End Function

    ''' <summary>
    ''' dla podanego KEYWORD znajdź wszystkie childy, np. -ROK => -ROK -DW -BS ...
    ''' </summary>
    Public Function GetAllChilds(keyword As OneKeyword) As String
        Dim sRet As String = keyword.sId
        If keyword?.SubItems Is Nothing Then Return sRet
        For Each subkey As OneKeyword In keyword.SubItems
            sRet &= GetAllChilds(subkey)
        Next

        Return sRet
    End Function


#End Region

    Public Function CreateManualTagFromKwds(keywords As String) As ExifTag
        If String.IsNullOrWhiteSpace(keywords) Then Return Nothing

        Dim oExif As New ExifTag(ExifSource.ManualTag)

        Dim kwds As String() = keywords.Split(" ")

        ' mozna byloby GetKeyword(id), ale to za każdym razem by robiło ToFlatList, może nie ma sensu...
        Dim flatka As List(Of OneKeyword) = ToFlatList()

        oExif.Keywords = keywords ' razem z nieznanymi...
        oExif.UserComment = ""

        Dim iMinRadius As Integer = Integer.MaxValue

        Dim oMinDate As Date = Date.MinValue
        Dim oMaxDate As Date = Date.MaxValue


        For Each kwd As String In kwds

            ' keywords
            Dim oKey As OneKeyword = flatka.Find(Function(x) x.sId = kwd)
            If oKey Is Nothing Then Continue For

            oExif.UserComment = oExif.UserComment & " | " & oKey.sDisplayName


            ' z nich geotag
            If oKey.iGeoRadius > 0 Then
                If oKey.iGeoRadius > iMinRadius Then Continue For
                oExif.GeoTag = oKey.oGeo
                oExif.GeoName = oKey.sDisplayName
                iMinRadius = oKey.iGeoRadius
            End If

            ' oraz daty z nich
            oMinDate = oMinDate.Max(oKey.minDate)
            oMaxDate = oMaxDate.Min(oKey.maxDate)

        Next


        If oMaxDate.IsDateValid Then oExif.DateMax = oMaxDate
        If oMinDate.IsDateValid Then oExif.DateMin = oMinDate

        Return oExif

    End Function


End Class


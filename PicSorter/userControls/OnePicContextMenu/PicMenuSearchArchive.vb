Imports MetadataExtractor
Imports pkar
Imports pkar.UI.Extensions
Imports Vblib

Public Class PicMenuSearchArchive
    Inherits PicMenuBase

    Protected Overrides Property _minAktualne As SequenceStages = SequenceStages.Keywords


    Private Shared _ByAzureTags As MenuItem
    Private Shared _ByAzureObjects As MenuItem
    Private Shared _ByKeywords As MenuItem
    Private Shared _ByGeo As MenuItem
    Private Shared _ByFolder As MenuItem
    Private Shared _ByReel As MenuItem

    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If Not String.IsNullOrWhiteSpace(Me.Header) Then Return

        If UseSelectedItems Then Return ' umiemy tylko pojedyńczy picek

        MyBase.OnApplyTemplate()

        ' *TODO* może być Bing, Google
        If Not InitEnableDisable("Search similar", "Wyszukaj własne zdjęcia w archiwum", True) Then Return

        AddMenuItem("Same day", "Wyszukaj zdjęcia z tego samego dnia", AddressOf SearchExactDay)
        AddMenuItem("Date ±7 days", "Wyszukaj zdjęcia o podobnych datach", AddressOf SearchSimilarDate)

        _ByGeo = AddMenuItem("Same place", "Wyszukaj zdjęcia zrobione w pobliżu tego zdjęcia (1 km)", AddressOf SearchByGeo)
        _ByKeywords = AddMenuItem("Same people", "Wyszukaj zdjęcia oznaczone tymi samymi słowami kluczowymi osób ('-')", AddressOf SearchByKwds)
        _ByFolder = AddMenuItem("Same TargetDir", "Wyszukaj zdjęcia w tym folderze", AddressOf SearchByTargetDir)
        _ByReel = AddMenuItem("Same reel", "Wyszukaj zdjęcia z tej samej reel", AddressOf SearchByReel)
        _ByAzureTags = AddMenuItem("Similar (Tags)", "Wyszukaj zdjęcia oznaczone przez Azure tymi samymi Tags", AddressOf SearchByAzureTags)
        _ByAzureObjects = AddMenuItem("Similar (Object)", "Wyszukaj zdjęcia oznaczone przez Azure tymi samymi Tags", AddressOf SearchByAzureObjects)

        MenuOtwieramy()
    End Sub

    Public Overrides Sub MenuOtwieramy()
        MyBase.MenuOtwieramy()

        If _ByFolder Is Nothing Then Return
        _ByFolder.IsEnabled = If(String.IsNullOrWhiteSpace(GetFromDataContext()?.TargetDir), False, True)
        Dim oExif As Vblib.ExifTag = GetFromDataContext()?.GetExifOfType(Vblib.ExifSource.SourceDefault)
        _ByReel.IsEnabled = If(String.IsNullOrWhiteSpace(oExif?.ReelName), False, True)

        ' fallback przy błędach
        _ByAzureTags.IsEnabled = False
        _ByAzureObjects.IsEnabled = False
        _ByKeywords.IsEnabled = False
        _ByGeo.IsEnabled = False

        Dim oPic As Vblib.OnePic = GetFromDataContext()
        If oPic Is Nothing Then Return

        Dim azurek As Vblib.MojeAzure = oPic.GetExifOfType(Vblib.ExifSource.AutoAzure)?.AzureAnalysis
        UstawWidocznoscMenuItemTagi(azurek)
        UstawWidocznoscMenuItemObjects(azurek)
        UstawWidocznoscMenuItemKeywords(oPic)
        UstawWidocznoscMenuItemGeo(oPic)
    End Sub


    Private Shared Sub UstawWidocznoscMenuItemGeo(oPic As OnePic)

        _ByGeo.DataContext = oPic?.GetGeoTag
        If _ByGeo.DataContext Is Nothing Then
            _ByGeo.ToolTip = "Zdjęcie nie ma informacji o geolokalizacji"
        Else
            _ByGeo.IsEnabled = True
            _ByGeo.ToolTip = "Wyszukaj zdjęcia zrobione w pobliżu tego zdjęcia (500 m)"
        End If

    End Sub

    Private Shared Sub UstawWidocznoscMenuItemKeywords(oPic As OnePic)
        Dim lista As String = ""

        Try
            Dim kwds As String = oPic.GetAllKeywords
            Dim arKwds As String() = kwds.Split(" ")
            For Each orgKwd As String In arKwds
                If Not orgKwd.StartsWith("-") Then Continue For
                If orgKwd.StartsWith("-f") Then
                    ' jeśli -f1 itp., z WinFace
                    If orgKwd.Substring(2, 1) >= "0" AndAlso orgKwd.Substring(2, 1) <= "9" Then
                        Continue For
                    End If
                End If
                lista &= orgKwd & " "
            Next

            If lista.Trim.Length < 3 Then
                _ByKeywords.ToolTip = "Zdjęcie nie ma słów kluczowych dotyczących osób"
                _ByKeywords.DataContext = ""
            Else
                _ByKeywords.IsEnabled = True
                _ByKeywords.ToolTip = "Wyszukaj zdjęcia oznaczone tymi samymi osobami:" & vbCrLf & lista
                _ByKeywords.DataContext = lista.Trim
            End If
        Catch ex As Exception
            _ByKeywords.IsEnabled = False
            _ByKeywords.ToolTip = "ERROR"
        End Try
    End Sub

    Private Shared Sub UstawWidocznoscMenuItemObjects(azurek As MojeAzure)
        Dim lista As String = ""

        Try
            If azurek?.Objects Is Nothing Then
                _ByAzureObjects.IsEnabled = False
                _ByAzureObjects.ToolTip = "Wybrane zdjęcie nie ma Azure.Objects"
            Else
                For Each azureTag As TextWithProbAndBox In azurek.Objects.GetList
                    If azureTag.probability < 0.1 Then Continue For
                    If lista <> "" Then lista = lista & vbCrLf
                    lista &= azureTag.tekst
                Next

                If String.IsNullOrWhiteSpace(lista) Then
                    _ByAzureObjects.ToolTip = "Zdjęcie nie ma żadnych Objects"
                    _ByAzureObjects.DataContext = ""
                Else
                    _ByAzureObjects.IsEnabled = True
                    _ByAzureObjects.ToolTip = "Wyszukaj zdjęcia oznaczone przez Azure tymi samymi Objects:" & vbCrLf & lista
                    _ByAzureObjects.DataContext = lista.Trim
                End If
            End If
        Catch ex As Exception
            _ByAzureObjects.IsEnabled = False
            _ByAzureObjects.ToolTip = "ERROR"
        End Try
    End Sub

    Private Shared Sub UstawWidocznoscMenuItemTagi(azurek As MojeAzure)
        Dim lista As String = ""

        Try
            If azurek?.Tags Is Nothing Then
                _ByAzureTags.IsEnabled = False
                _ByAzureTags.ToolTip = "Wybrane zdjęcie nie ma Azure.Tags"
            Else
                For Each azureTag As TextWithProbability In azurek.Tags.GetList
                    If azureTag.probability < 0.1 Then Continue For
                    If lista <> "" Then lista = lista & vbCrLf
                    lista &= azureTag.tekst
                Next

                If String.IsNullOrWhiteSpace(lista) Then
                    _ByAzureTags.ToolTip = "Zdjęcie nie ma żadnych Tags"
                    _ByAzureTags.DataContext = ""
                Else
                    _ByAzureTags.IsEnabled = True
                    _ByAzureTags.ToolTip = "Wyszukaj zdjęcia oznaczone przez Azure tymi samymi Tags:" & vbCrLf & lista
                    _ByAzureTags.DataContext = lista.Trim
                End If
            End If
        Catch ex As Exception
            _ByAzureTags.IsEnabled = False
            _ByAzureTags.ToolTip = "ERROR"
        End Try

    End Sub

    Private Sub SearchByAzureTags(sender As Object, e As RoutedEventArgs)

        Dim oMI As MenuItem = TryCast(sender, MenuItem)
        Dim tagi As String = TryCast(oMI?.DataContext, String)
        If String.IsNullOrWhiteSpace(tagi) Then Return

        Dim qry As New Vblib.SearchQuery
        qry.Azure.Tags = tagi.Replace(vbCrLf, " ")
        qry.Azure.AlsoEmpty = False

        Szukaj(qry)
    End Sub

    Private Sub SearchByAzureObjects(sender As Object, e As RoutedEventArgs)

        Dim oMI As MenuItem = TryCast(sender, MenuItem)
        Dim tagi As String = TryCast(oMI?.DataContext, String)
        If String.IsNullOrWhiteSpace(tagi) Then Return

        Dim qry As New Vblib.SearchQuery
        qry.Azure.Objects = tagi.Replace(vbCrLf, " ")
        qry.Azure.AlsoEmpty = False

        Szukaj(qry)
    End Sub


    Private Sub SearchByKwds(sender As Object, e As RoutedEventArgs)
        Dim oMI As MenuItem = TryCast(sender, MenuItem)
        Dim tagi As String = TryCast(oMI?.DataContext, String)
        If String.IsNullOrWhiteSpace(tagi) Then Return

        Dim qry As New Vblib.SearchQuery
        qry.ogolne.Tags = tagi
        Szukaj(qry)

    End Sub

    Private Sub SearchByGeo(sender As Object, e As RoutedEventArgs)
        Dim oMI As MenuItem = TryCast(sender, MenuItem)
        Dim geo As BasicGeoposWithRadius = TryCast(oMI?.DataContext, BasicGeoposWithRadius)
        If geo Is Nothing Then Return

        Dim qry As New Vblib.SearchQuery
        qry.ogolne.geo.Location = New BasicGeoposWithRadius(geo, 500)
        qry.ogolne.geo.AlsoEmpty = False
        qry.ogolne.geo.OnlyExact = True
        'qry.ogolne.geo.
        Szukaj(qry)
    End Sub

    Private Sub SearchSimilarDate(sender As Object, e As RoutedEventArgs)
        StworzQueryDlaDaty(7)
    End Sub

    Private Sub SearchExactDay(sender As Object, e As RoutedEventArgs)
        StworzQueryDlaDaty(0)
    End Sub

    Private Sub StworzQueryDlaDaty(iDaysDelta As Integer)
        Dim oPic As Vblib.OnePic = GetFromDataContext()

        If Not oPic.HasRealDate Then
            Me.MsgBox("Ale to zdjęcie nie ma określonej daty...")
            Return
        End If

        Dim kiedy As Date = oPic.GetMostProbablyDate
        Dim qry As New Vblib.SearchQuery
        qry.ogolne.MinDateCheck = True
        qry.ogolne.MinDate = kiedy.AddHours(-kiedy.Hour).AddMinutes(-kiedy.Hour)
        qry.ogolne.MaxDateCheck = True
        qry.ogolne.MaxDate = kiedy.AddHours(23 - kiedy.Hour).AddMinutes(59 - kiedy.Minute)

        qry.ogolne.MinDate = qry.ogolne.MinDate.AddDays(-iDaysDelta)
        qry.ogolne.MaxDate = qry.ogolne.MaxDate.AddDays(iDaysDelta)

        Szukaj(qry)
    End Sub


    Private Sub SearchByTargetDir(sender As Object, e As RoutedEventArgs)

        Dim qry As New Vblib.SearchQuery
        qry.ogolne.adv.TargetDir = GetFromDataContext()?.TargetDir
        Szukaj(qry)

    End Sub

    Private Sub SearchByReel(sender As Object, e As RoutedEventArgs)
        Dim oMI As MenuItem = TryCast(sender, MenuItem)
        Dim tagi As String = TryCast(oMI?.DataContext, String)
        If String.IsNullOrWhiteSpace(tagi) Then Return

        Dim qry As New Vblib.SearchQuery
        Dim oExif As Vblib.ExifTag = GetFromDataContext()?.GetExifOfType(Vblib.ExifSource.SourceDefault)
        qry.ogolne.Reel = oExif?.ReelName

        Szukaj(qry)

    End Sub



    Private Async Sub Szukaj(qry As SearchQuery)

        If Not Application.gDbase.IsLoaded Then
            If Not Await Me.DialogBoxYNAsync("Będę musiał wczytać Archiwum, to trochę zajmie. Kontynuować?") Then
                Return
            End If
        End If

        Dim oWnd As New SearchWindow(Nothing, qry)
        oWnd.Show()
    End Sub

End Class


Imports System.IO
Imports Org.BouncyCastle.Math
Imports pkar
Imports pkar.UI.Extensions

Public Class DataGridWnd

    Private _picki As Vblib.IBufor

    Public Sub New(picki As Vblib.IBufor)

        ' This call is required by the designer.
        InitializeComponent()

        _picki = picki

    End Sub

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        Me.InitDialogs

        uiGridek.ItemsSource = _picki.GetList
        uiGridek.IsReadOnly = _picki.GetIsReadonly ' ale nawet jak r/w, to i tak niektóre kolumny będą r/o
    End Sub

    Public Shared _standardMode As Boolean = True

    Private Sub uiGridek_AutoGeneratingColumn(sender As Object, e As DataGridAutoGeneratingColumnEventArgs) Handles uiGridek.AutoGeneratingColumn
        ' ewentualne ukrycie kolumny: e.Column.Visibility = Visibility.Collapsed
        ' kolumna zawsza read-only: e.Column.IsReadOnly = True
        ' kolumna read-only gdy zwykły tryb:  e.Column.IsReadOnly = _standardMode

        'Dim cellYN As New Style
        'cellYN.Triggers.Add(New Trigger() With {.[Property] = "", .Value = "True", .s})
        '        <DataGrid.CellStyle>
        '    <Style TargetType = "DataGridCell" >
        '        <Style.Triggers>
        '            <Trigger Property="IsSelected" Value="True">
        '                <Setter Property="Background" Value="SeaGreen"/>
        '            </Trigger>
        '        </Style.Triggers>
        '            </Style>
        '</DataGrid.CellStyle>

        e.Column.MaxWidth = 350

        Select Case e.Column.Header.ToString()
            'Case "Archived" ' String
            '    e.Column.IsReadOnly = _standardMode
            Case "CloudArchived" ' String
                e.Column.IsReadOnly = _standardMode
            'Case "Published" ' Dictionary(Of String, String)
            '    e.Column.IsReadOnly = True
            'Case "TargetDir" ' String ' OneDirFlat.sId
            Case "Exifs" ' New List(Of ExifTag) ' ExifSource.SourceFile ..., )
                e.Column.IsReadOnly = True
            'Case "InBufferPathName" ' String ' przy Sharing: GUID pliku, tymczasowe przy odbieraniu z upload
            '    e.Column.IsReadOnly = True
            'Case "sSourceName" ' String
            '    e.Column.IsReadOnly = _standardMode
            'Case "sInSourceID" ' String    ' usually pathname
            '    e.Column.IsReadOnly = _standardMode
            'Case "sSuggestedFilename" ' String ' mia┼éo by─ç ┼╝e np. scinanie WP_. ale jednak tego nie robi─Ö (bo moge posortowac po dacie, albo po nazwach - i w tym drugim przypadku mam rozdzia┼é na np. telefon i aparat)
            '    e.Column.IsReadOnly = _standardMode
            Case "descriptions" ' List(Of OneDescription)
                e.Column.IsReadOnly = True
                'Case "editHistory" ' List(Of OneDescription)
                '    e.Column.IsReadOnly = True
                'Case "TagsChanged" ' Boolean = False
                'Case "fileTypeDiscriminator" ' String = Nothing   ' tu "|>", "*", kt├│re maj─ů by─ç dodawane do miniaturek
                '    e.Column.IsReadOnly = _standardMode
                'Case "PicGuid" ' String = Nothing  ' 0xA420 ImageUniqueID ASCII!

                'Case "sharingFromGuid" ' String   ' a'la UseNet Path, tyle ┼╝e rozdzielana ";"; GUIDy kolejne; wpsywane przez httpserver.lib; prefiksy: "L:" z loginu, "S:" z serwera
                '    e.Column.IsReadOnly = _standardMode
                'Case "sharingLockSharing" ' Boolean
                '    e.Column.IsReadOnly = _standardMode
                'Case "allowedPeers" ' String
                '    e.Column.IsReadOnly = _standardMode
                'Case "deniedPeers" ' String
                '    e.Column.IsReadOnly = _standardMode

                'Case "serno" ' Integer
                '    e.Column.IsReadOnly = True
                '    ' próba przestawienia
                '    e.Column.DisplayIndex = 1
                'Case "linki" ' List(Of OneLink)
                '    e.Column.IsReadOnly = True
                'Case "locked" ' Boolean = False
                '    e.Column.IsReadOnly = _standardMode

                'Case "sumOfKwds" ' String
                '    e.Column.IsReadOnly = True
                'Case "sumOfDescr" ' String
                '    e.Column.IsReadOnly = True
                'Case "sumOfUserComment" ' String
                '    e.Column.IsReadOnly = True
                'Case "sumOfGeo" ' BasicGeoposWithRadius
                '    e.Column.IsReadOnly = True
                'Case "FormattedSerNo"
                '    e.Column.DisplayIndex = 0
                '    e.Column.IsReadOnly = True
                'Case "_EditPipeline"
                '    e.Column.Visibility = Visibility.Collapsed
                'Case "_PipelineInput"
                '    e.Column.Visibility = Visibility.Collapsed
                'Case "_PipelineOutput"
                '    e.Column.Visibility = Visibility.Collapsed
        End Select
    End Sub


    Private _wasEdited As Boolean = False

    Private Sub uiGridek_CellEditEnding(sender As Object, e As DataGridCellEditEndingEventArgs) Handles uiGridek.CellEditEnding
        _wasEdited = True
    End Sub

    Private Sub Window_Closing(sender As Object, e As ComponentModel.CancelEventArgs)
        If _wasEdited Then
            _picki.SaveData()
        End If
    End Sub

    Private Async Sub uiGoAdvanced_Click(sender As Object, e As MouseButtonEventArgs)
        If Not Await Me.DialogBoxYNAsync("Na pewno przełączyć w tryb zaawansowany?") Then Return

        _standardMode = False

        'uiGridek.ItemsSource = Nothing
        '' zakładam że przejdzie jeszcze raz przez autogencolumn - nie przechodzi...
        'uiGridek.ItemsSource = _picki.GetList

        ' przełączenie kolumn r/o r/w
        For Each kol As DataGridColumn In uiGridek.Columns
            Dim kolAdv As UserDGridColumnAdv = TryCast(kol, UserDGridColumnAdv)
            If kolAdv IsNot Nothing Then
                kolAdv.IsReadOnly = DataGridWnd._standardMode
                kolAdv.Foreground = If(kol.IsReadOnly, UserDGridColumnAdv.brushRO, UserDGridColumnAdv.brushRW)
            End If
        Next

    End Sub

    Private Sub uiGridek_MouseDoubleClick(sender As Object, e As MouseButtonEventArgs) Handles uiGridek.MouseDoubleClick
        ' ewentualnie tu sprawdzić gdzie kliknięcie, i wywołanie okna...
    End Sub

    Private Sub uiFiltr_TextChanged(sender As Object, e As TextChangedEventArgs)
        Dim oTB As TextBox = sender
        If oTB Is Nothing Then Return

        Dim propname As String = oTB.Name.Replace("uiFiltr_", "")
        Dim fragm As String = oTB.Text

        uiFiltr_Changed(propname, fragm)
    End Sub

    Private _filtry As New Dictionary(Of String, String)

    Private Sub uiFiltr_Changed(propname As String, query As String)

        Try
            _filtry.Remove(propname)
            If Not String.IsNullOrWhiteSpace(query) Then _filtry.Add(propname, query)

            If _filtry.Count > 0 Then
                uiGridek.Items.Filter = AddressOf FiltrowanieCallback
            Else
                uiGridek.Items.Filter = Nothing
            End If

            uiGridek.Items.Refresh()
        Catch ex As Exception
            ' Exception Info: System.InvalidOperationException: 'Filter' is not allowed during an AddNew or EditItem transaction.
        End Try

    End Sub

    Private Function FiltrowanieCallback(obj As Object) As Boolean
        Dim opic As Vblib.OnePic = TryCast(obj, Vblib.OnePic)
        If opic Is Nothing Then Return True

        For Each fragm In _filtry

            Dim picPole As Reflection.PropertyInfo = opic.GetType.GetProperty(fragm.Key)
            If picPole.PropertyType IsNot GetType(String) Then Continue For

            Dim picVal As String = picPole.GetValue(opic)
            If fragm.Value = "!" Then Return String.IsNullOrWhiteSpace(picVal)

            If Not Vblib.OnePic.CheckStringMasks(picVal, fragm.Value) Then Return False

        Next

        Return True

    End Function

    'Private Function JestCosNieEmpty() As Boolean

    '    For Each kol As DataGridColumn In uiGridek.Columns

    '        Dim kolAdv As DataGridTextColumn = TryCast(kol, DataGridTextColumn)
    '        If kolAdv Is Nothing Then Continue For

    '        Dim pole As Binding = kolAdv.Binding

    '        Dim nazwa As String = "uiFiltr_" & pole.Path.Path

    '        Dim frmEl As FrameworkElement = Me.FindName(nazwa)
    '        Dim txtbox As TextBox = TryCast(frmEl, TextBox)
    '        If txtbox Is Nothing Then Continue For

    '        If Not String.IsNullOrWhiteSpace(txtbox.Text) Then
    '            Return True
    '        End If

    '    Next

    '    Return False

    'End Function

    Private Sub uiFiltrTyp_SelChanged(sender As Object, e As SelectionChangedEventArgs)
    End Sub

    Private Sub DataGrid_AutoGeneratingColumn(sender As Object, e As DataGridAutoGeneratingColumnEventArgs)

    End Sub

    ' można zrobić nie tak, że w kodzie, tylko zwykły binding w XAML, wtedy dodać konwertery
    ' dałoby się wtedy zrobić combobox z checkboxem do usuwania (np. published)
    ' oraz przeskok do datagrid EXIFów - jako expander?
    ' https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/how-to-add-row-details-to-a-datagrid-control?view=netframeworkdesktop-4.8
    ' jakiś filtr?
    ' weryfikacja danych
    ' https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/how-to-implement-validation-with-the-datagrid-control?view=netframeworkdesktop-4.8
    ' no i własną kolejność

    ' kontrolka ComboWithCheck z property DisplayPropertyName - które ma pokazywać, a pamięta całość
    ' więc List(Of Object)

    ' contextmenu na polach można zrobić
    ' openedit otwiera comboboxa z checkboxami - DataContext to pełny item, IsChecked, Header
    ' contextmenu na header: sort, clear, remove subitem (np. konkretny publish), itp.
    ' można pozmieniać nazwy kolumn (na krótsze,szczególnie przy checkbox), i dymek na pełną nazwę

    Private Sub Exifs_AutoGeneratingColumn(sender As Object, e As DataGridAutoGeneratingColumnEventArgs) Handles uiGridek.AutoGeneratingColumn
        ' ewentualne ukrycie kolumny: e.Column.Visibility = Visibility.Collapsed
        ' kolumna zawsza read-only: e.Column.IsReadOnly = True
        ' kolumna read-only gdy zwykły tryb:  e.Column.IsReadOnly = _standardMode

        e.Column.MaxWidth = 350

        Select Case e.Column.Header.ToString()
            Case "ExifSource" ' String ' ExifSource.SourceFile, ...
                e.Column.IsReadOnly = True
            Case "FileSourceDeviceType" ' FileSourceDeviceTypeEnum
                e.Column.IsReadOnly = _standardMode
                '            Case "Author" ' String
                '            Case "Copyright" ' String
                '    ' Public Property CameraMaker" ' String
                '            Case "CameraModel" ' String
                '            Case "DateMin" ' DateTime     ' min i max data, je┼Ťli nie mamy pe┼énej daty (np. "na pewno po 1943, bo jest tata, i na pewno przed 1955, bo wtedy most odbudowano)
                '            Case "DateMax" ' DateTime
                '            Case "DateTimeOriginal" ' String
                '            Case "DateTimeScanned" ' String
                '            Case "Keywords" ' String  ' ImageDescription (only ASCII)
                '            Case "UserComment" ' String  ' UserComment, 9286
                '            Case "Restrictions" ' String ' 0x9blic 212 SecurityClassification string ExifIFD (C/R/S/T/U), do "tajne" :) (ale jest tez non-writable, 0xa212)
                '            Case "Orientation" ' OrientationEnum  ' do usuwania z pliku, bo jego rotate podczas import?
                ''            Case "PicGuid" ' String   ' 0xA420 ImageUniqueID ASCII!
                '            Case "ReelName" ' String   ' 0xc789       ReelName        string  IFD0
                '            Case "GeoTag" ' pkar.BasicGeopos    ' 0x87b1      GeoTiffAsciiParams IFD0 (string)
                '            Case "GeoName" ' String ' GeoTiffAsciiParams
                '            Case "GeoZgrubne" ' Boolean = False
                '            Case "OriginalRAW" ' String   ' Tag 0xc68b (9 bytes, string[9])
                '    'Public Property AlienTags" ' List(Of String)    ' importowane z r├│┼╝nych miejsc, autorozpoznawanie -> ExifSource
                '            Case "AzureAnalysis" ' MojeAzure
                '            Case "PogodaAstro" ' CacheAutoWeather_Item
                '            Case "MeteoOpad" ' Meteo_Opad
                '            Case "MeteoKlimat" ' Meteo_Klimat
                '            Case "MeteoSynop" ' Meteo_Synop
                '            Case "x" ' Integer
                '            Case "y" ' Integer 
        End Select
        ' zmiany stąd nie będą uwzględniane w sumOf!
    End Sub


    Private Sub Descr_AutoGeneratingColumn(sender As Object, e As DataGridAutoGeneratingColumnEventArgs) Handles uiGridek.AutoGeneratingColumn
        ' ewentualne ukrycie kolumny: e.Column.Visibility = Visibility.Collapsed
        ' kolumna zawsza read-only: e.Column.IsReadOnly = True
        ' kolumna read-only gdy zwykły tryb:  e.Column.IsReadOnly = _standardMode

        '    e.Column.MaxWidth = 350

        Select Case e.Column.Header.ToString()
            Case "PeerGUID"
                e.Column.IsReadOnly = True
        End Select
    End Sub

End Class


Public Class ReadOnlyFromMode
    Inherits ValueConverterOneWaySimple

    Protected Overrides Function Convert(value As Object) As Object
        Return DataGridWnd._standardMode
    End Function
End Class

Public Class ForegroundFromMode
    Inherits ValueConverterOneWaySimple

    Dim brushRW As New SolidColorBrush(Colors.Black)
    Dim brushRO As New SolidColorBrush(Colors.DimGray)

    Protected Overrides Function Convert(value As Object) As Object
        Return If(DataGridWnd._standardMode, brushRO, brushRW)
    End Function
End Class

Public Class KonwerterGeo
    Inherits ValueConverterOneWaySimple

    Protected Overrides Function Convert(value As Object) As Object
        If value Is Nothing Then Return ""
        Dim oGeo As BasicGeopos = TryCast(value, BasicGeopos)
        If oGeo Is Nothing Then Return "??"
        Return $"{oGeo.StringLatDM}, {oGeo.StringLon}"
    End Function
End Class

Imports System.Linq
Imports Vblib
Imports pkar.DotNetExtensions

''' <summary>
''' klasa załatwiająca wspólną strukturę dla wszystkich PicMenu*
''' </summary>
Public MustInherit Class PicMenuBase
    Inherits MenuItem

    ''' <summary>
    ''' czy należy używać owner na otwieranych oknach (tak by zmiana selected pic zmieniała zawartość)
    ''' </summary>
    Public Property UseOwner As Boolean

    ''' <summary>
    ''' TRUE: ma wyszukać listę SelectedItems, FALSE: ma używać DataContext (OnePic/ThumbPic)
    ''' </summary>
    Public Property UseSelectedItems As Boolean

    Public Property UseProgBar As Boolean

    Public Shared ReadOnly IsReadOnlyProperty As DependencyProperty =
DependencyProperty.Register("IsReadOnly", GetType(Boolean),
GetType(PicMenuBase), New FrameworkPropertyMetadata(False))

    Public Overridable Property ChangePic As Boolean = False
    Public Overridable Property ChangeMetadata As Boolean = False


    Public Property IsReadOnly As Boolean
        Get
            Return GetValue(IsReadOnlyProperty)
        End Get
        Set
            SetValue(IsReadOnlyProperty, Value)
        End Set
    End Property


    ''' <summary>
    ''' wywoływany po zmianie metadanych (dla listy: dopiero po całej serii)
    ''' </summary>
    Public Event MetadataChanged As MetadataChangedHandler
    Public Delegate Sub MetadataChangedHandler(sender As Object, zmieniam As PicMenuModifies)

    ' musi być OnePic, bo potrzebny jest InBufferPathName i tak dalej, ale może być NULL gdy ma używać listy
    Protected _picek As Vblib.OnePic


    Private _progBar As ProgressBar

    ''' <summary>
    ''' Ustawienie _azurek oraz IsEnabled, ret FALSE oznacza 'zakończ'
    ''' </summary>
    ''' <param name="header">Defaultowy Header dla MenuItem</param>
    ''' <returns>TRUE gdy jest enabled</returns>
    Protected Function InitEnableDisable(header As String, dymek As String, Optional bSubItems As Boolean = False)

        If String.IsNullOrWhiteSpace(Me.Header) Then
            Me.Header = header & If(UseSelectedItems AndAlso bSubItems, " »", "")
        End If
        ToolTip = dymek

        If UseSelectedItems Then
            _picek = Nothing
        Else
            _picek = GetFromDataContext()
            If _picek Is Nothing Then
                Me.IsEnabled = False
                Return False
            End If
        End If


        AddHandler Me.SubmenuOpened, AddressOf OtwieramToSubmenu

        Me.IsEnabled = True
        Return True

    End Function


    Protected Function GetFromDataContext() As Vblib.OnePic
        Dim picek As Vblib.OnePic = TryCast(DataContext, Vblib.OnePic)
        If picek Is Nothing Then
            picek = TryCast(DataContext, ProcessBrowse.ThumbPicek)?.oPic
        End If
        Return picek
    End Function

    Protected Function AddMenuItem(header As String, dymek As String, Optional handler As RoutedEventHandler = Nothing, Optional isenabled As Boolean = True) As MenuItem
        Dim oNew As MenuItem = CreateMenuItem(header, dymek, handler, isenabled)
        Me.Items.Add(oNew)
        Return oNew
    End Function

    Protected Sub AddSeparator()
        Me.Items.Add(New Separator)
    End Sub

    Protected Shared Function CreateMenuItem(header As String, dymek As String, Optional handler As RoutedEventHandler = Nothing, Optional isenabled As Boolean = True) As MenuItem
        Dim oNew As New MenuItem
        oNew.Header = header
        oNew.IsEnabled = isenabled
        oNew.ToolTip = dymek
        If handler IsNot Nothing Then AddHandler oNew.Click, handler
        Return oNew
    End Function

    Protected Sub EventRaise(zmiana As PicMenuModifies)
        RaiseEvent MetadataChanged(Me, zmiana)
    End Sub


    Protected Function TryGetProcessBrowse() As ProcessBrowse
        Dim wnd As Window = Window.GetWindow(Me)
        If wnd Is Nothing Then
            wnd = Window.GetWindow(TryCast(FindUiElement("uiPicList"), ListView))
        End If

        Return TryCast(wnd, ProcessBrowse)
    End Function

    Protected Function GetSelectedItems() As List(Of ProcessBrowse.ThumbPicek)
        Dim wnd As ProcessBrowse = TryGetProcessBrowse()
        Return wnd?.GetSelectedThumbs
    End Function
    Protected Function GetSelectedItemsAsPics() As List(Of Vblib.OnePic)
        'Dim fe As ListView = TryCast(FindUiElement("uiPicList"), ListView)
        'If fe?.SelectedItems Is Nothing Then Return Nothing

        Dim ret As New List(Of Vblib.OnePic)

        For Each oThumb As ProcessBrowse.ThumbPicek In GetSelectedItems()
            ret.Add(oThumb.oPic)
        Next

        Return ret

    End Function

    Protected Function GetFullLista() As List(Of ProcessBrowse.ThumbPicek)

        Return TryCast(Window.GetWindow(Me), ProcessBrowse)?.GetAllThumbs

        'Dim fe As ListView = TryCast(FindUiElement("uiPicList"), ListView)

        'If fe?.ItemsSource Is Nothing Then Return Nothing

        'Dim ret As New List(Of ProcessBrowse.ThumbPicek)
        'For Each oThumb As ProcessBrowse.ThumbPicek In fe.ItemsSource
        '    ret.Add(oThumb)
        'Next

        'Return ret


    End Function

    Protected Function FindUiElement(name As String) As FrameworkElement
        Dim wnd As Window = Window.GetWindow(Me)
        Return wnd?.FindName(name)
    End Function

    ''' <summary>
    ''' próbuje znaleźć ProgressBar w oknie (i przygotować się do jego użycia)
    ''' </summary>
    Protected Sub TryUseProgBar()
        _progBar = Nothing
        If Not UseProgBar Then Return
        _progBar = TryCast(FindUiElement("uiProgBar"), ProgressBar)
    End Sub

    Protected Delegate Sub DoAction(oPic As Vblib.OnePic)
    Protected Delegate Function DoActionAsync(oPic As Vblib.OnePic)

    ''' <summary>
    ''' wykonaj AKCJA na jednym bądź liście zdjęć
    ''' </summary>
    Protected Sub OneOrMany(akcja As DoAction)
        If UseSelectedItems Then
            For Each oItem As ProcessBrowse.ThumbPicek In GetSelectedItems()
                akcja(oItem.oPic)
            Next
        Else
            Dim picek As Vblib.OnePic = GetFromDataContext()
            If picek IsNot Nothing Then akcja(picek)
        End If
    End Sub

    ''' <summary>
    ''' wykonaj AKCJA na jednym bądź liście zdjęć (OnePic), używając znalezionego ProgressBar (uiProgBar)
    ''' </summary>
    ''' <returns></returns>
    Protected Async Function OneOrManyAsync(akcja As DoActionAsync) As Task

        Application.ShowWait(True)

        If UseSelectedItems Then
            TryUseProgBar()

            If _progBar IsNot Nothing Then
                _progBar.Value = 0
                _progBar.Maximum = GetSelectedItems.Count
                _progBar.Visibility = Visibility.Visible
            End If

            For Each oItem As ProcessBrowse.ThumbPicek In GetSelectedItems()
                Await akcja(oItem.oPic)
                Await ProgBarInc()
            Next

            If _progBar IsNot Nothing Then _progBar.Visibility = Visibility.Collapsed
        Else
            Await akcja(GetFromDataContext)
        End If

        Application.ShowWait(False)

    End Function

    Protected Async Function ProgBarInc() As Task
        If _progBar IsNot Nothing Then
            _progBar.Value += 1
            Await Task.Delay(1) ' żeby był czas na przerysowanie progbar, nawet jak tworzenie EXIFa jest empty
        End If
    End Function

    ' na potrzeby Cloud Publish/Archive
    Protected Shared Function NewMenuCloudOperation(sDisplay As String, oEngine As Object, oEventHandler As RoutedEventHandler) As MenuItem
        Dim oNew As New MenuItem
        oNew.Header = sDisplay.Replace("_", "__")
        oNew.DataContext = oEngine

        If oEventHandler IsNot Nothing Then AddHandler oNew.Click, oEventHandler

        Return oNew
    End Function

    Protected Shared Function NewMenuCloudOperation(oEngine As Object) As MenuItem
        Return NewMenuCloudOperation(oEngine.konfiguracja.nazwa, oEngine, Nothing)
    End Function

    ''' <summary>
    ''' otwieramy to konkretne submenu (złożone z tego MenuItem.Items)
    ''' </summary>
    Protected Overridable Sub OtwieramToSubmenu()

    End Sub


    Protected Overridable Property _minAktualne As SequenceStages = SequenceStages.None
    Protected Overridable Property _maxAktualne As SequenceStages = SequenceStages.LocalArch

    ''' <summary>
    ''' wywoływane przy otwieraniu menu piętro wyżej (czyli gdy TO MenuItem się pojawia jako pozycja w menu)
    ''' </summary>
    ''' <param name="whichmenu">-1: to już było, 0: aktualne, 1: na przyszłość</param>
    ''' <param name="stage">według tego stanu</param>
    Public Sub OtwieramMenuWyzej(whichmenu As Integer, stage As SequenceStages)
        If whichmenu < 0 Then
            Me.Visibility = If(stage > _minAktualne, Visibility.Visible, Visibility.Collapsed)
        End If
        If whichmenu > 0 Then
            Me.Visibility = If(_maxAktualne > stage, Visibility.Visible, Visibility.Collapsed)
        End If
        If whichmenu = 0 Then
            Me.Visibility = Visibility.Visible
            If stage < _minAktualne Then Me.Visibility = Visibility.Collapsed
            If stage > _maxAktualne Then Me.Visibility = Visibility.Collapsed
        End If
    End Sub


    ''' <summary>
    ''' do override, reakcja na otwieranie menu - wywoływane dla każdego w menu, z ProcessBrowse
    ''' </summary>
    Public Overridable Async Sub MenuOtwieramy()
        Await Task.Delay(10) ' czas na wypełnienie DataContext
        'If _miCopy IsNot Nothing Then _miCopy.IsEnabled = Not UseSelectedItems
        Me.IsEnabled = CheckNieMaBlokerow()
    End Sub

    ''' <summary>
    ''' sprawdź czy wszystkie są tylko w buforze, żaden nie jest zarchiwizowany
    ''' </summary>
    Private Function IsAllNotArch() As Boolean
        If UseSelectedItems Then
            For Each oItem As ProcessBrowse.ThumbPicek In GetSelectedItems()
                If Not String.IsNullOrWhiteSpace(oItem.oPic.Archived) Then Return False
                If Not String.IsNullOrWhiteSpace(oItem.oPic.CloudArchived) Then Return False
            Next
        Else
            Dim oPic As Vblib.OnePic = GetFromDataContext()
            If oPic Is Nothing Then Debug.WriteLine("OPIC NOTHING OPIC NOTHINGOPIC NOTHINGOPIC NOTHING")
            If Not String.IsNullOrWhiteSpace(oPic?.Archived) Then Return False
            If Not String.IsNullOrWhiteSpace(oPic?.CloudArchived) Then Return False
        End If

        Return True
    End Function

    ''' <summary>
    ''' sprawdź czy wszystkie są zarchiwizowane, żaden nie z bufora, a wszystkie archiwa - editable
    ''' </summary>
    Private Function IsAllArchivedAndEditable() As Boolean

        Dim bufForder As String = Vblib.GetSettingsString("uiFolderBuffer")

        If UseSelectedItems Then
            For Each oItem As ProcessBrowse.ThumbPicek In GetSelectedItems()
                If String.IsNullOrWhiteSpace(oItem.oPic.Archived) Then Return False
                If String.IsNullOrWhiteSpace(oItem.oPic.CloudArchived) Then Return False
                If oItem.oPic.InBufferPathName.StartsWithCI(bufForder) Then Return False
            Next
        Else
            Dim oPic As Vblib.OnePic = GetFromDataContext()
            If String.IsNullOrWhiteSpace(oPic.Archived) Then Return False
            If String.IsNullOrWhiteSpace(oPic.CloudArchived) Then Return False
            If oPic.InBufferPathName.StartsWithCI(bufForder) Then Return False
        End If

        Return Application.gDbase.IsAllEditable

    End Function

    ''' <summary>
    ''' TRUE gdy można edytować
    ''' </summary>
    Protected Function CheckNieMaBlokerow()

        If Not ChangePic AndAlso Not ChangeMetadata() Then Return True

        If ChangePic Then
            Return IsAllNotArch()
        End If

        ' czyli edycja metadanych
        If IsAllNotArch() Then Return True
        If IsAllArchivedAndEditable() Then Return True

        Return False

    End Function



End Class


<Flags>
Public Enum PicMenuModifies
    None = 0
    Any = 1
    Geo = 2
    Azure = 4
    Descript = 8
    Kwds = 16
    Target = 32
    Lock = 64
    Peers = 128

    Data = 1024
End Enum
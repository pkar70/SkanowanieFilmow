Imports System.Linq

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
    Public Delegate Sub MetadataChangedHandler(sender As Object, data As EventArgs)

    ' musi być OnePic, bo potrzebny jest InBufferPathName i tak dalej, ale może być NULL gdy ma używać listy
    Protected _picek As Vblib.OnePic

    Protected Shared _wasApplied As Boolean

    Private _progBar As ProgressBar

    ''' <summary>
    ''' Ustawienie _picek oraz IsEnabled, ret FALSE oznacza 'zakończ'
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

    Protected Shared Function NewMenuItem(header As String, dymek As String, Optional handler As RoutedEventHandler = Nothing, Optional isenabled As Boolean = True) As MenuItem
        Dim oNew As New MenuItem
        oNew.Header = header
        oNew.IsEnabled = isenabled
        oNew.ToolTip = dymek
        If handler IsNot Nothing Then AddHandler oNew.Click, handler
        Return oNew
    End Function


    Protected Sub EventRaise(sender As Object, Optional data As EventArgs = Nothing)
        RaiseEvent MetadataChanged(sender, data)
    End Sub

    Protected Function GetSelectedItems() As List(Of ProcessBrowse.ThumbPicek)

        Dim wnd As Window = Window.GetWindow(Me)
        If wnd Is Nothing Then
            'Dim ctxmenu As FrameworkElement = Me
            'While ctxmenu.GetType <> GetType(ContextMenu)
            '    ctxmenu = ctxmenu.Parent
            'End While

            'wnd = Window.GetWindow(ctxmenu)

            'If wnd Is Nothing Then
            '    wnd = Window.GetWindow(ctxmenu.Parent) ' popup
            'End If


            wnd = Window.GetWindow(TryCast(FindUiElement("uiPicList"), ListView))
        End If

            Return TryCast(wnd, ProcessBrowse)?.GetSelectedThumbs

        'Dim cbox As CheckBox = TryCast(FindUiElement("uiPodpisCheckbox"), CheckBox)
        'If cbox IsNot Nothing AndAlso cbox.IsChecked Then
        '    Dim wnd As ProcessBrowse = Window.GetWindow(Me)
        '    ' *TODO* ma zwrócić listę z IsChecked wzietą!
        '    Return
        'End If

        'Dim fe As ListView = TryCast(FindUiElement("uiPicList"), ListView)

        'If fe?.SelectedItems Is Nothing Then Return Nothing

        'Dim ret As New List(Of ProcessBrowse.ThumbPicek)
        'For Each oThumb As ProcessBrowse.ThumbPicek In fe.SelectedItems
        '    ret.Add(oThumb)
        'Next

        'Return ret
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
            akcja(GetFromDataContext)
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
    ''' do override, reakcja na otwieranie menu - wywoływane dla każdego w menu
    ''' </summary>
    Public Overridable Sub MenuOtwieramy()
        If _miCopy IsNot Nothing Then _miCopy.IsEnabled = Not UseSelectedItems
    End Sub

    Protected Shared _miPaste As MenuItem
    Protected Shared _miCopy As MenuItem

    Protected Sub AddCopyMenu(header As String, dymek As String)
        _miCopy = NewMenuItem(header, dymek, Sub()
                                                 ' najpierw True, bo można w ten sposób dać False w CopyCalled
                                                 _miPaste.IsEnabled = True
                                                 CopyCalled()
                                             End Sub)
        Me.Items.Add(_miCopy)
    End Sub

    Protected Sub AddPasteMenu(header As String, dymek As String)
        _miPaste = NewMenuItem(header, dymek, Sub() PasteCalled(), False)
        Me.Items.Add(_miPaste)
    End Sub

    Protected Overridable Sub CopyCalled()

    End Sub

    Protected Overridable Sub PasteCalled()

    End Sub

End Class

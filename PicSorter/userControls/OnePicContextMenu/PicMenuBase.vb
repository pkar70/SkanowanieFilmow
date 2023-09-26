

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

    Protected _wasApplied As Boolean

    Private _progBar As ProgressBar

    ''' <summary>
    ''' Ustawienie _picek oraz IsEnabled, ret FALSE oznacza 'zakończ'
    ''' </summary>
    ''' <param name="header">Defaultowy Header dla MenuItem</param>
    ''' <returns>TRUE gdy jest enabled</returns>
    Protected Function InitEnableDisable(header As String, Optional bSubItems As Boolean = False)

        If String.IsNullOrWhiteSpace(Me.Header) Then
            Me.Header = header & If(UseSelectedItems AndAlso bSubItems, " »", "")
        End If

        If UseSelectedItems Then
            _picek = Nothing
        Else
            _picek = TryCast(DataContext, Vblib.OnePic)
            If _picek Is Nothing Then
                _picek = TryCast(DataContext, ProcessBrowse.ThumbPicek)?.oPic
                If _picek Is Nothing Then
                    Me.IsEnabled = False
                    Return False
                End If
            End If
        End If

        Me.IsEnabled = True
        Return True

    End Function

    Protected Function NewMenuItem(header As String, Optional handler As RoutedEventHandler = Nothing, Optional isenabled As Boolean = True) As MenuItem
        Dim oNew As New MenuItem
        oNew.Header = header
        oNew.IsEnabled = isenabled
        If handler IsNot Nothing Then AddHandler oNew.Click, handler
        Return oNew
    End Function


    Protected Sub EventRaise(sender As Object, Optional data As EventArgs = Nothing)
        RaiseEvent MetadataChanged(sender, data)
    End Sub

    Protected Function GetSelectedItems() As IList
        Dim fe As ListView = TryCast(FindUiElement("uiPicList"), ListView)
        Return fe?.SelectedItems
    End Function
    Protected Function GetFullLista() As IList
        Dim fe As ListView = TryCast(FindUiElement("uiPicList"), ListView)
        Return fe?.Items
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
            akcja(_picek)
        End If
    End Sub

    ''' <summary>
    ''' wykonaj AKCJA na jednym bądź liście zdjęć, używając znalezionego ProgressBar (uiProgBar)
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

            _progBar.Visibility = Visibility.Collapsed
        Else
            Await akcja(_picek)
        End If

        Application.ShowWait(False)

    End Function

    Public Async Function ProgBarInc() As Task
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


End Class

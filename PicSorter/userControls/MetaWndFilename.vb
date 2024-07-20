
Public Class MetaWndFilename
    Inherits Grid

    Private _filenameBox As New TextBox With
        {.IsReadOnly = True,
        .BorderThickness = New Thickness(0),
        .HorizontalAlignment = HorizontalAlignment.Center,
        .FontWeight = FontWeights.Bold}
    Private _pinunpin As New UserControlPinUnpin With
        {.HorizontalAlignment = HorizontalAlignment.Right}


    ''' <summary>
    ''' czy ignorować zmianę DataContext
    ''' </summary>
    ''' <returns>TRUE gdy już jest DataContext i ustawienie mówi żeby go ignotować</returns>
    Public Property IsPinned As Boolean
        Get
            Return _pinunpin.IsPinned AndAlso DataContext IsNot Nothing
        End Get
        Set(value As Boolean)
            _pinunpin.IsPinned = value
            RaiseEvent PinStateChanged(Me, value)
        End Set
    End Property

    Private _EffectiveDatacontext As Object

    Public ReadOnly Property EffectiveDatacontext As Object
        Get
            Return _EffectiveDatacontext
        End Get
    End Property

    Public Event PinStateChanged As PinStateChangedHandler
    Public Delegate Sub PinStateChangedHandler(sender As Object, state As Boolean)

    Public Event MouseDoubleClick As MouseButtonEventHandler
    'Public Delegate Sub MyszkaDwuklik(sender As Object, e As MouseButtonEventArgs)

    Public Sub New()
        'ColumnDefinitions.Clear()
        'ColumnDefinitions.Add(New ColumnDefinition() With {.Width = New GridLength(1, GridUnitType.Star)})
        'ColumnDefinitions.Add(New ColumnDefinition() With {.Width = New GridLength(1, GridUnitType.Auto)})

        Children.Clear()
        Children.Add(_filenameBox)
        Children.Add(_pinunpin)

        'Grid.SetColumn(_filenameBox, 0)
        'Grid.SetColumn(_pinunpin, 1)

        AddHandler Me.DataContextChanged, AddressOf DataContext_Changed
        AddHandler _filenameBox.MouseDoubleClick, AddressOf Myszka_Dwumlask
    End Sub

    Private Sub Myszka_Dwumlask(sender As Object, e As MouseButtonEventArgs)
        RaiseEvent MouseDoubleClick(Me, e)
    End Sub

    Public Shared Function GetHeaderText(picek As Vblib.OnePic) As String
        Dim ret As String = ""
        If Vblib.GetSettingsBool("uiTitleSerno") Then ret = picek.FormattedSerNo
        If Vblib.GetSettingsBool("uiTitleFilename") Then
            If ret <> "" Then
                ret = ret & " (" & picek.sSuggestedFilename & ")"
            Else
                ret = picek.sSuggestedFilename
            End If
        End If

        If ret = "" Then ret = picek.FormattedSerNo
        Return ret
    End Function

    Private Sub DataContext_Changed(sender As Object, e As DependencyPropertyChangedEventArgs)

        If _EffectiveDatacontext IsNot Nothing AndAlso IsPinned Then Return

        If _EffectiveDatacontext Is Nothing OrElse Not IsPinned Then _EffectiveDatacontext = DataContext

        Dim thumb As ProcessBrowse.ThumbPicek = TryCast(DataContext, ProcessBrowse.ThumbPicek)
        If thumb IsNot Nothing Then
            _filenameBox.Text = GetHeaderText(thumb.oPic)
            _filenameBox.ToolTip = thumb.sDymek
            Return
        End If

        Dim picek As Vblib.OnePic = TryCast(DataContext, Vblib.OnePic)
        If picek Is Nothing Then Return
        _filenameBox.Text = GetHeaderText(picek)
        _filenameBox.ToolTip = ""

    End Sub
End Class

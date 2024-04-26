
' domyślnie: 1 I 1800 do Now+5 hr


Public Class DateTimePicker

    Public Property UseSeconds As Boolean
        Get
            Return uiCzas.UseSeconds
        End Get
        Set(value As Boolean)
            uiCzas.UseSeconds = value
        End Set
    End Property

    Public Property SelectedDate As Date?
        Get
            Return uiData.SelectedDate
        End Get
        Set(value As Date?)
            uiData.SelectedDate = value
        End Set
    End Property

    Public Property DisplayDateStart As Date?
        Get
            Return uiData.DisplayDateStart
        End Get
        Set(value As Date?)
            uiData.DisplayDateStart = value
        End Set
    End Property

    Public Property DisplayDateEnd As Date?
        Get
            Return uiData.DisplayDateEnd
        End Get
        Set(value As Date?)
            uiData.DisplayDateEnd = value
        End Set
    End Property

    Public Property DateTime As DateTime
        Get
            If uiData.SelectedDate Is Nothing Then Return Nothing
            Return uiData.SelectedDate + uiCzas.Time
        End Get
        Set(value As DateTime)
            uiData.SelectedDate = value
            uiCzas.Time = New TimeSpan(value.Hour, value.Minute, value.Second)
        End Set
    End Property

    Public Property Arrangement As Orientation
        Get
            Return uiStack.Orientation
        End Get
        Set(value As Orientation)
            uiStack.Orientation = value
        End Set
    End Property

    Public Property Header As String
        Get
            Return uiHeader.Text
        End Get
        Set(value As String)
            uiHeader.Text = value
            If String.IsNullOrWhiteSpace(value) Then
                uiHeader.Visibility = Visibility.Collapsed
            Else
                uiHeader.Visibility = Visibility.Visible
            End If
        End Set
    End Property

    Private Sub Grid_Loaded(sender As Object, e As RoutedEventArgs)
        DisplayDateEnd = Date.Now.AddHours(5)
        DisplayDateStart = New Date(1800, 1, 1)
    End Sub
End Class

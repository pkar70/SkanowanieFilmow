
Public Class UserKwdEditButton

    Public Property UseCheckmarks As Boolean
        Get
            Return uiButton.UseCheckmarks
        End Get
        Set(value As Boolean)
            uiButton.UseCheckmarks = value
        End Set
    End Property
    Public ReadOnly Property IsChanged As Boolean
        Get
            Return uiButton.IsChanged
        End Get
    End Property


    'Public Property Keywords As String
    '    Get
    '        Return GetValue(KeywordsProperty)
    '    End Get
    '    Set(value As String)
    '        SetValue(KeywordsProperty, value)
    '    End Set
    'End Property

    'Public KeywordsProperty As DependencyProperty = DependencyProperty.Register("Keywords", GetType(String), Me.GetType)

    Public Event MetadataChanged As MetadataChangedHandler
    Public Delegate Sub MetadataChangedHandler(sender As Object, data As EventArgs)

    Public Function GetManualTag()
        Return vblib.GetKeywords.CreateManualTagFromKwds(uiSlowka.Text)
    End Function


    Private Sub UserControl_Loaded(sender As Object, e As RoutedEventArgs)
        uiButton.DataContext = uiSlowka
        AddHandler uiButton.MetadataChanged, AddressOf ZmianaZbuttona
    End Sub

    Private Sub ZmianaZbuttona(sender As Object, data As EventArgs)
        ' zrób coś po tej stronie (np. apply do tutejszego datacontext
        RaiseEvent MetadataChanged(Me, data)
    End Sub
End Class

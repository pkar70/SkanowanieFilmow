Public Class UserControlMetadataOptions

    Public Property ShowHttpLogOption As Boolean
        Get
            Return uiNoHttpLog.Visibility = Visibility.Visible
        End Get
        Set(value As Boolean)
            uiNoHttpLog.Visibility = If(value, Visibility.Visible, Visibility.Collapsed)
        End Set
    End Property

    Public Property ShowPicLimitOption As Boolean
        Get
            Return uiPicLimitOption.Visibility = Visibility.Visible
        End Get
        Set(value As Boolean)
            uiPicLimitOption.Visibility = If(value, Visibility.Visible, Visibility.Collapsed)
        End Set
    End Property


End Class

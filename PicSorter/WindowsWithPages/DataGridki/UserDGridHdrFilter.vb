Imports pkar.DotNetExtensions

Public Class UserDGridHdrFilter
    Inherits StackPanel

    Public Property Field As String
        Get
            Return _qryTbox.Name.Replace("uiFiltr_", "")
        End Get
        Set(value As String)
            _qryTbox.Name = "uiFiltr_" & value
        End Set
    End Property
    Public Property Header As String
        Get
            Return _hdrTblock.Text
        End Get
        Set(value As String)
            _hdrTblock.Text = value
        End Set
    End Property

    Public Property FontWeight As FontWeight
        Get
            Return _hdrTblock.FontWeight
        End Get
        Set(value As FontWeight)
            _hdrTblock.FontWeight = value
        End Set
    End Property

    Public Event QueryChanged As FiltQueryChanged
    Public Delegate Sub FiltQueryChanged(propname As String, query As String)

    'Public Function CheckPicMatches(picek As Vblib.OnePic) As Boolean

    '    If String.IsNullOrWhiteSpace(_qryTbox.Text) Then Return True

    '    Dim picProp As Reflection.PropertyInfo = picek.GetType.GetProperty(Field)
    '    If picProp.GetType IsNot GetType(String) Then Return True

    '    Dim picVal As String = picProp.GetValue(picek)

    '    If _qryTbox.Text = "!" Then Return String.IsNullOrWhiteSpace(picVal)

    '    If _qryTbox.Text.StartsWith("!") Then
    '        Return Not picVal.ContainsCI(_qryTbox.Text.Substring(1))
    '    Else
    '        Return picVal.ContainsCI(_qryTbox.Text)
    '    End If

    'End Function


    Private _hdrTblock As TextBlock
    Private _qryTbox As TextBox

    Public Sub New()
        Me.Children.Clear()

        _hdrTblock = New TextBlock With {.HorizontalAlignment = HorizontalAlignment.Center}
        Me.Children.Add(_hdrTblock)

        _qryTbox = New TextBox With
            {.ToolTip = "filtr typu 'contains'",
            .HorizontalAlignment = HorizontalAlignment.Stretch
            }

        AddHandler _qryTbox.TextChanged, Sub(sender As Object, e As TextChangedEventArgs)
                                             Dim oTB As TextBox = sender
                                             If oTB Is Nothing Then Return

                                             Dim propname As String = oTB.Name.Replace("uiFiltr_", "")
                                             Dim fragm As String = oTB.Text

                                             RaiseEvent QueryChanged(propname, fragm)
                                         End Sub

        Me.Children.Add(_qryTbox)

    End Sub

End Class

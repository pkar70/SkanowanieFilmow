Imports Vblib

Public Class UserAzureListViewWithBox

    Private _mylist As List(Of AzureListBoxedWithCheck)

    Private Sub UserControl_DataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs)

        _mylist = New List(Of AzureListBoxedWithCheck)
        Dim fromList As ListTextWithProbabAndBox = TryCast(DataContext, ListTextWithProbabAndBox)
        If fromList Is Nothing Then
            uiLista.ItemsSource = Nothing
            Return
        End If

        For Each oItem As TextWithProbAndBox In fromList.GetList
            _mylist.Add(New AzureListBoxedWithCheck(oItem))
        Next

        uiLista.ItemsSource = _mylist
    End Sub

    Public Function IsChanged() As Boolean
        If _mylist Is Nothing Then Return False
        Return _mylist.Any(Function(x) Not x.check)
    End Function

    Public Function GetChanged() As ListTextWithProbabAndBox
        If _mylist Is Nothing Then Return Nothing
        Dim ret As New ListTextWithProbabAndBox

        Dim bylo As Boolean = False
        For Each oItem As AzureListBoxedWithCheck In _mylist.Where(Function(x) x.check)
            ret.Add(oItem.item)
            bylo = True
        Next

        ' jeśli nie był nic, to nie empty list, a null, żeby nie było w .json
        If Not bylo Then Return Nothing

        Return ret
    End Function

    Protected Class AzureListBoxedWithCheck
        Public Property check As Boolean
        Public Property item As TextWithProbAndBox

        Public Sub New(basedOn As TextWithProbAndBox)
            check = True
            item = basedOn
        End Sub

        Public ReadOnly Property label As String
            Get
                Return item.tekst.Replace("_", "__")
            End Get
        End Property

        Public ReadOnly Property boxdata As String
            Get
                Return $"({item.X}, {item.Y}), ({item.Width}×{item.Height})"
            End Get
        End Property

    End Class
End Class

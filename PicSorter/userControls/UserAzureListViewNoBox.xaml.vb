Imports Vblib

Public Class UserAzureListViewNoBox

    Private _mylist As List(Of AzureListWithCheck)

    Private Sub UserControl_DataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs)

        _mylist = New List(Of AzureListWithCheck)
        Dim fromList As ListTextWithProbability = TryCast(DataContext, ListTextWithProbability)
        If fromList Is Nothing Then Return

        For Each oItem As TextWithProbability In fromList.GetList
            _mylist.Add(New AzureListWithCheck(oItem))
        Next

        uiLista.ItemsSource = _mylist
    End Sub

    Public Function IsChanged() As Boolean
        If _mylist Is Nothing Then Return False
        Return _mylist.Any(Function(x) Not x.check)
    End Function

    Public Function GetChanged() As ListTextWithProbability
        If _mylist Is Nothing Then Return Nothing

        Dim ret As New ListTextWithProbability

        Dim bylo As Boolean = False
        For Each oItem As AzureListWithCheck In _mylist.Where(Function(x) x.check)
            ret.Add(oItem.item)
            bylo = True
        Next

        ' jeśli nie był nic, to nie empty list, a null, żeby nie było w .json
        If Not bylo Then Return Nothing

        Return ret
    End Function

    Protected Class AzureListWithCheck
        Public Property check As Boolean
        Public Property item As TextWithProbability

        Public Sub New(basedOn As TextWithProbability)
            check = True
            item = basedOn
        End Sub

        Public ReadOnly Property label As String
            Get
                Return item.tekst.Replace("_", "__")
            End Get
        End Property
    End Class
End Class

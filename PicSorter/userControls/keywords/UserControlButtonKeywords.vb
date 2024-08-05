
Public Class UserControlButtonKeywords
    Inherits Button

    Public Property UseCheckmarks As Boolean
    Public Property IsChanged As Boolean

    Public Event MetadataChanged As MetadataChangedHandler
    Public Delegate Sub MetadataChangedHandler(sender As Object, data As EventArgs)

#Region "Inicjalizacja"
    Private Sub UserControl_Loaded(sender As Object, e As RoutedEventArgs)
        IsChanged = False


        Me.ContextMenu = New ContextMenu
        Me.ContextMenu.Items.Clear()

        ' root level: ma tylko trzy pozycje, które nie mogą być same z siebie zaznaczone :)
        For Each oItem As Vblib.OneKeyword In vblib.GetKeywords
            Dim oNew As New MenuItem
            oNew.Header = oItem.sId
            DodajSubTree(oNew, oItem.SubItems)
            Me.ContextMenu.Items.Add(oNew)
        Next

        AddHandler Me.ContextMenu.Closed, AddressOf ZamykamyMenu

        AddHandler Me.Click, AddressOf KliknietoMnie

    End Sub

    Public Overrides Sub OnApplyTemplate()
        MyBase.OnApplyTemplate()
        UserControl_Loaded(Nothing, Nothing)
    End Sub


    Private Sub DodajSubTree(oMenuItem As MenuItem, oSubTree As List(Of Vblib.OneKeyword))
        If oSubTree Is Nothing Then Return
        For Each oItem As Vblib.OneKeyword In oSubTree
            Dim oNew As New MenuItem
            oNew.Header = oItem.sId & " " & oItem.sDisplayName
            If oItem.oGeo IsNot Nothing Then
                oNew.Header &= " " + Vblib.AutotaggerBase.IconGeo
            End If

            If UseCheckmarks Then
                oNew.IsCheckable = UseCheckmarks
                Dim oPic As Vblib.OnePic = TryCast(DataContext, Vblib.OnePic)
                If oPic IsNot Nothing Then
                    If oPic.HasKeyword(oItem.sId) Then oNew.IsChecked = True
                End If
                AddHandler oNew.Checked, AddressOf Keyword_Checkmark
                AddHandler oNew.Unchecked, AddressOf Keyword_Checkmark
            End If

            oNew.DataContext = oItem
            'oNew.Margin = New Thickness(2)
            DodajSubTree(oNew, oItem.SubItems)
            AddHandler oNew.Click, AddressOf Keyword_Click
            'oNew.Margin = _DefMargin
            'oNew.Background = New SolidColorBrush(Colors.White)
            oMenuItem.Items.Add(oNew)
        Next

    End Sub
#End Region

    Private Sub KliknietoMnie(sender As Object, e As RoutedEventArgs)
        Me.ContextMenu.IsOpen = Not Me.ContextMenu.IsOpen
    End Sub

    Private Sub ZamykamyMenu(sender As Object, e As RoutedEventArgs)
        ' zapisuje zmiany - bo zamykamy guzik
        If IsChanged Then RaiseEvent MetadataChanged(sender, Nothing)
    End Sub

    Private Sub AktualizujTextBox(oKey As Vblib.OneKeyword, bAdd As Boolean)
        Dim oEdit As TextBox = TryCast(DataContext, TextBox)
        If oEdit Is Nothing Then Return

        Dim kwdtxt As String = oKey.sId & " "
        Dim aktKwds As String = oEdit.Text & " "

        If Not bAdd Then
            If Not aktKwds.Contains(kwdtxt) Then Return

            oEdit.Text = oEdit.Text.Replace(oKey.sId, "").Replace("  ", " ")
            DataContext = oEdit.Text
            IsChanged = True
        Else
            If aktKwds.Contains(kwdtxt) Then Return

            oEdit.Text = (oEdit.Text & " " & kwdtxt).Trim
            DataContext = oEdit.Text
            IsChanged = True
        End If

    End Sub


    Private Shared Function SenderToKey(sender As Object) As Vblib.OneKeyword
        Return TryCast(SenderToMenuItem(sender)?.DataContext, Vblib.OneKeyword)
    End Function

    Private Shared Function SenderToMenuItem(sender As Object) As MenuItem
        Return TryCast(sender, MenuItem)
    End Function


    Private Sub Keyword_Checkmark(sender As Object, e As RoutedEventArgs)
        If Not UseCheckmarks Then Return

        Dim oKey As Vblib.OneKeyword = SenderToKey(sender)
        If oKey Is Nothing Then Return

        Dim oPic As Vblib.OnePic = TryCast(DataContext, Vblib.OnePic)
        If oPic IsNot Nothing Then
            Vblib.MsgBox("Jeszcze nie umiem: kwdCheckmark/OnePic")
            Return
        End If

        AktualizujTextBox(oKey, SenderToMenuItem(sender).IsChecked)

    End Sub


    Private Sub Keyword_Click(sender As Object, e As RoutedEventArgs)

        If UseCheckmarks Then Return

        Dim oKey As Vblib.OneKeyword = SenderToKey(sender)
        If oKey Is Nothing Then Return

        If oKey.SubItems IsNot Nothing Then Return

        Dim oPic As Vblib.OnePic = TryCast(DataContext, Vblib.OnePic)
        If oPic IsNot Nothing Then
            Vblib.MsgBox("Jeszcze nie umiem: kwdBrowse/OnePic")
            Return
        End If

        AktualizujTextBox(oKey, True)


        Me.ContextMenu.IsOpen = False



        '_oNewExif = New Vblib.ExifTag(Vblib.ExifSource.ManualTag)
        ' tagi mają tez TargetDir :)

        'If _lastAdd.AddSeconds(0.5) > Date.Now Then Return
        '_lastAdd = Date.Now

        'Dim oMI As MenuItem = sender
        'Dim oKeyword As Vblib.OneKeyword = oMI?.DataContext
        'If oKeyword Is Nothing Then Return

        'uiKeywords.Text = (uiKeywords.Text & " " & oKeyword.sId).Trim & " "

    End Sub
End Class



Public Class UserControlPostProcessPipeline

    Public Property AllowDuplicates As Boolean = False
    Public Property Pipeline As String
        Get
            Return uiPostprocess.Text
        End Get
        Set(value As String)
            uiPostprocess.Text = value
        End Set
    End Property

    Private Sub UserControl_Loaded(sender As Object, e As RoutedEventArgs)
        WypelnMenuPostprocesory()
    End Sub

    Private Sub WypelnMenuPostprocesory()
        uiMenuPostProcessors.Items.Clear()

        For Each oItem As Vblib.PostProcBase In Application.gPostProcesory
            uiMenuPostProcessors.Items.Add(StworzMenuItemPostProcesora(oItem))
        Next

    End Sub

    Private Function StworzMenuItemPostProcesora(oEngine As Vblib.PostProcBase) As MenuItem
        Dim oNew As New MenuItem
        oNew.Header = oEngine.Nazwa
        oNew.Margin = New Thickness(2)
        oNew.DataContext = oEngine

        AddHandler oNew.Click, AddressOf DodajTenPostproc

        Return oNew
    End Function

    Private Sub DodajTenPostproc(sender As Object, e As RoutedEventArgs)
        Dim oMI As MenuItem = sender
        If oMI Is Nothing Then Return

        Dim postproc As String = oMI.Header

        If Not AllowDuplicates Then
            If uiPostprocess.Text.Contains(postproc) Then Return
        End If

        If uiPostprocess.Text <> "" Then uiPostprocess.Text &= ";"
        uiPostprocess.Text &= postproc
    End Sub

    Private Sub uiAddPostproc_Click(sender As Object, e As RoutedEventArgs)
        uiAddPostprocPopup.IsOpen = Not uiAddPostprocPopup.IsOpen
    End Sub
End Class

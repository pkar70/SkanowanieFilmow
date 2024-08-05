

'Imports Windows.ApplicationModel.Email.DataProvider

Public Class UserControlPostProcessPipeline

    Public Property AllowDuplicates As Boolean = False

    ' https://stackoverflow.com/questions/18461660/wpf-user-control-bind-data-to-user-control-property

    Public Shared ReadOnly PipelineProperty As DependencyProperty =
DependencyProperty.Register("Pipeline", GetType(String),
GetType(UserControlPostProcessPipeline), New FrameworkPropertyMetadata(String.Empty))

    Public Property Pipeline As String
        Get
            Return GetValue(PipelineProperty)?.ToString()
        End Get
        Set
            SetValue(PipelineProperty, Value)
        End Set
    End Property

    '    Public Property Pipeline As DependencyProperty

    Private Sub UserControl_Loaded(sender As Object, e As RoutedEventArgs)
        WypelnMenuPostprocesory()
    End Sub

    Private Sub WypelnMenuPostprocesory()
        uiMenuPostProcessors.Items.Clear()

        For Each oItem As Vblib.PostProcBase In Vblib.gPostProcesory
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

        Dim sPipeline As String = uiPostprocess.Text

        If Not AllowDuplicates Then
            If sPipeline.Contains(postproc) Then Return
        End If

        If sPipeline <> "" Then sPipeline &= ";"
        sPipeline &= postproc

        Pipeline = sPipeline
    End Sub

    Private Sub uiAddPostproc_Click(sender As Object, e As RoutedEventArgs)
        uiAddPostprocPopup.IsOpen = Not uiAddPostprocPopup.IsOpen
    End Sub
End Class

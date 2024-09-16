Imports pkar
Imports pkar.UI.Extensions
Imports Vblib

' nazwy checkboxów mają uiSequence*, bo żeby się nie powtórzyło z żadnym innym Settingsem

Public Class SequenceHelperList
    'Inherits ProcessWnd_Base

    Private _loading As Boolean = True


    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        _loading = True
        Me.ProgRingInit(True, False)

        Await LoadLista()

        Dim buf As BufferSortowania = ProcessPic.GetBuffer(Me)
        uiTitle.Visibility = If(buf.IsDefaultBuffer, Visibility.Collapsed, Visibility.Visible)
        Me.Height = If(buf.IsDefaultBuffer, 480, 500)
        uiTitle.Text = "buffer: " & buf.GetBufferName

    End Sub

#Disable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
    Private Async Function LoadLista() As Task
#Enable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously

        Dim checkboxy As String = ProcessPic.GetBuffer(Me).GetStagesSettings

        uiLista.Children.Clear()

        For Each oStage As Vblib.SequenceStageBase In Vblib.SequenceCheckers.OrderBy(Of Integer)(Function(x) x.StageNo)
            Dim oNew As New CheckBox
            oNew.Content = oStage.Nazwa
            If Not String.IsNullOrWhiteSpace(oStage.Dymek) Then oNew.ToolTip = oStage.Dymek
            If oStage.AutoCheck Then
                oNew.IsEnabled = False
                oNew.IsChecked = oStage.Check(ProcessPic.GetBuffer(Me).GetList)
            Else
                AddHandler oNew.Checked, AddressOf CheckUncheck
                AddHandler oNew.Unchecked, AddressOf CheckUncheck
                oNew.IsChecked = checkboxy.Contains(oStage.Nazwa)
            End If
            oNew.Margin = New Thickness(5, 10, 5, 5)
            oNew.FontSize = 18

            uiLista.Children.Add(oNew)
        Next

        _loading = False

        ' CheckGeoTag() ISENABLED,ale liczy
        ' targetDir - brak wyłącza publuish, cloudarch, archive
        ' AUTOCHECK: autoexif, targetdir, cloudarch, localarch
    End Function


    Private Sub CheckUncheck(sender As Object, e As RoutedEventArgs)
        If _loading Then Return

        Dim suma As String = ""
        For Each oCB As CheckBox In uiLista.Children
            If oCB.IsEnabled AndAlso oCB.IsChecked Then suma &= oCB.Content.ToString
        Next

        ProcessPic.GetBuffer(Me).SetStagesSettings(suma)
    End Sub


End Class




Imports Org.BouncyCastle.Utilities
Imports pkar

Class MainWindow
    Inherits Window

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)

        InitLib(Nothing)
        Me.Content = New MainPage

        'Dim latD = 49.59
        'Dim lonD = 18.42

        'Dim latDegree As Double = Math.Floor(latD)
        'Dim latMin As Double = 100 * (latD - latDegree) * 100 / 60
        'Dim lonDegree As Double = Math.Floor(lonD)
        'Dim lonMin As Double = 100 * (lonD - lonDegree) * 100 / 60


        'Dim r1 = p1 + 1 / 60 * p2 + 1 / 3600 * 0
        'Dim r2 = p3 + 1 / 60 * p4 + 1 / 3600 * 0

        ' *TODO* to tylko czasowo
        'Dim sChcemy As String = "Degoo"
        ' Dim sChcemy As String = "Shutterfly"
        'Dim sChcemy As String = "NIC"
        'For Each oItem In Application.GetCloudArchives.GetList
        '    'If oItem.sProvider = "Degoo" Then
        '    If oItem.sProvider = sChcemy Then Await oItem.Login
        'Next

        'Dim proba As New probaKlasy
        'proba.cosik = "alamakota"
        'proba.ogps = Vblib.pkar.BasicGeopos.GetDomekGeopos

        'Dim probaClone As probaKlasy = proba.Clone1


    End Sub

    Private Sub Window_Closing(sender As Object, e As ComponentModel.CancelEventArgs)
        If Application.Current.Windows.Count > 2 Then
            Dim sAppName As String = Application.Current.MainWindow.GetType().Assembly.GetName.Name
            Dim iRet As MessageBoxResult = MessageBox.Show("Zamknąć program?", sAppName, MessageBoxButton.YesNo)
            If iRet = MessageBoxResult.Yes Then Application.Current.Shutdown()
        End If
    End Sub
End Class

'Public Class probaKlasy
'    Inherits pkar.BaseStruct

'    Public Property cosik As String
'    Public Property ogps As Vblib.pkar.BasicGeopos
'End Class
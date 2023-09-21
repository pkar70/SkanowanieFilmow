

Public Class BrowseMtpDevice


    Public currentPath As String
    Private _oMDH As Lib_mediaDevices.Helper

    Public Sub New(sVolLabel As String, sDefaultPath As String)

        ' This call is required by the designer.
        InitializeComponent()

        _oMDH = New Lib_mediaDevices.Helper(sVolLabel)

        currentPath = sDefaultPath
    End Sub

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        PokazDir(currentPath)
    End Sub

    Private Sub PokazDir(sPath As String)

        If _oMDH Is Nothing Then Return ' nie powinno się zdarzyć, chyba że będzie błąd w programowaniu

        If String.IsNullOrWhiteSpace(sPath) Then sPath = "\"
        If sPath.Substring(0, 1) <> "\" Then sPath = "\" & sPath
        uiCurrentDir.Text = sPath

        Dim lista As New List(Of OneMTPdirToShow)


        If sPath.Length > 2 Then
            Dim oNew As New OneMTPdirToShow("..")
            lista.Add(oNew)
        End If

        Dim namesList As List(Of String) = _oMDH.GetDirList(sPath)

        For Each sName As String In namesList
            lista.Add(New OneMTPdirToShow(sName))
        Next

        uiLista.ItemsSource = lista
    End Sub
    Private Sub uiOpenFolder_Click(sender As Object, e As MouseButtonEventArgs)
        Dim OFE As FrameworkElement = sender
        Dim oneDir As OneMTPdirToShow = OFE?.DataContext
        If oneDir Is Nothing Then Return

        Dim sPath As String = uiCurrentDir.Text
        If oneDir.dirname = ".." Then
            sPath = IO.Path.GetDirectoryName(sPath)
            'Dim sSep As String = IO.Path.DirectorySeparatorChar
            'Dim iInd As Integer = sPath.LastIndexOf(sSep)
            'If iInd > 0 Then sPath = sPath.Substring(0, iInd)
        Else
            sPath = IO.Path.Combine(sPath, oneDir.dirname)
        End If
        PokazDir(sPath)
    End Sub


    Private Sub uiOk_Click(sender As Object, e As RoutedEventArgs)
        currentPath = uiCurrentDir.Text
        Me.Close()
    End Sub

    Private Class OneMTPdirToShow
        Public Property dirname As String
        Public Sub New(dir As String)
            dirname = dir
        End Sub
    End Class

End Class





Imports PicSorterNS.ProcessBrowse
Imports Vblib

Public NotInheritable Class PicMenuTargetDir
    Inherits PicMenuBase

    Protected Overrides Property _minAktualne As SequenceStages = SequenceStages.Descriptions
    Protected Overrides Property _maxAktualne As SequenceStages = SequenceStages.CloudArch



    Private Shared _clipForTargetDir As String
    Private Shared _itemSet As MenuItem
    Private Shared _itemClear As MenuItem
    Private Shared _itemMakeSame As MenuItem
    Private Shared _miCopy As MenuItem
    Private Shared _miPaste As MenuItem


    Public Overrides Property ChangeMetadata As Boolean = True

    Public Overrides Sub OnApplyTemplate()
        Vblib.DumpCurrMethod()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If Not String.IsNullOrWhiteSpace(Me.Header) Then Return

        MyBase.OnApplyTemplate()

        If Not InitEnableDisable("Target dir", "Operacje na wskazanym katalogu", True) Then Return

        Me.Items.Clear()

        _itemSet = AddMenuItem("Set target dir", "Ustawianie katalogu docelowego", AddressOf uiCreateTargetDir_Click)
        _itemMakeSame = AddMenuItem("Make same", "Skopiowanie katalogu docelowego między zdjęciami", AddressOf uiTargetMakeSame_Click)
        _miCopy = AddMenuItem("Copy TargetDir", "Skopiowanie katalogu docelowego do lokalnego schowka", AddressOf CopyCalled)
        _miPaste = AddMenuItem("Paste TargetDir", "Narzucenie zdjęciom katalogu docelowego wg lokalnego schowka", AddressOf PasteCalled, False)
        _itemClear = AddMenuItem("Clear TargetDir", "Usunięcie wskazania katalogu docelowego", AddressOf uiTargetClear_Click)

        MenuOtwieramy()
    End Sub


    Public Overrides Sub MenuOtwieramy()
        Vblib.DumpCurrMethod()

        MyBase.MenuOtwieramy()

        'If Not String.IsNullOrWhiteSpace(Me.Header) Then Return

        If _miCopy Is Nothing Then Return
        _miCopy.IsEnabled = Not UseSelectedItems AndAlso Not String.IsNullOrWhiteSpace(GetFromDataContext()?.TargetDir)
        _itemSet.IsEnabled = UseSelectedItems
        _itemClear.IsEnabled = UseSelectedItems OrElse Not String.IsNullOrWhiteSpace(GetFromDataContext()?.TargetDir)
        _itemMakeSame.IsEnabled = UseSelectedItems AndAlso GetSelectedItems()?.Count > 1

    End Sub

    Private Sub uiTargetMakeSame_Click(sender As Object, e As RoutedEventArgs)
        If GetSelectedItems.Count < 2 Then
            Vblib.DialogBox("Funkcja kopiowania TargetDir wymaga zaznaczenia przynajmniej dwu zdjęć")
            Return
        End If

        Dim sTarget As String = ""

        ' ustalenie katalogu, i sprawdzenie czy nie ma różnych
        For Each oItem As ProcessBrowse.ThumbPicek In GetSelectedItems()

            If sTarget = "" Then
                ' jeszcze nie było
                If Not String.IsNullOrWhiteSpace(oItem.oPic.TargetDir) Then sTarget = oItem.oPic.TargetDir
            Else
                If String.IsNullOrWhiteSpace(oItem.oPic.TargetDir) Then Continue For

                If sTarget <> oItem.oPic.TargetDir Then
                    Vblib.DialogBox("Są ustalone różne TargetDir dla zaznaczonych plików, więc nic nie robię" & vbCrLf & sTarget & vbCrLf & oItem.oPic.TargetDir)
                    Return
                End If
            End If
        Next

        If String.IsNullOrWhiteSpace(sTarget) Then
            Vblib.DialogBox("Nie znalazłem żadnego TargetDir")
            Return
        End If


        ' uzupełniamy tam gdzie nie ma ustalonego
        For Each oItem As ProcessBrowse.ThumbPicek In GetSelectedItems()
            If String.IsNullOrEmpty(oItem.oPic.TargetDir) Then
                oItem.oPic.TargetDir = sTarget
            End If
        Next

        EventRaise(PicMenuModifies.Target)
    End Sub

    Private Sub uiTargetClear_Click(sender As Object, e As RoutedEventArgs)
        OneOrMany(Sub(x) x.TargetDir = "")
        EventRaise(PicMenuModifies.Target)
    End Sub

    Private Sub CopyCalled(sender As Object, e As RoutedEventArgs)
        _clipForTargetDir = GetFromDataContext.TargetDir
        _miPaste.IsEnabled = True
    End Sub

    Private Sub PasteCalled(sender As Object, e As RoutedEventArgs)
        OneOrMany(Sub(x) If String.IsNullOrWhiteSpace(x.TargetDir) Then x.TargetDir = _clipForTargetDir)
        EventRaise(PicMenuModifies.Target)
    End Sub

    Private Sub uiCreateTargetDir_Click(sender As Object, e As RoutedEventArgs)

        If Not UseSelectedItems Then Return ' działa tylko na wielu zdjęciach
        Dim listaSelected As List(Of ProcessBrowse.ThumbPicek) = GetSelectedItems()
        Dim listaFull As List(Of ProcessBrowse.ThumbPicek) = GetFullLista()

        Dim oWnd As New TargetDir(listaFull, listaSelected)
        If Not oWnd.ShowDialog Then Return
        ' ale to jest bardzo skomplikowane, bo operuje na całej liście do auto-dzielenia

        EventRaise(PicMenuModifies.Target)

    End Sub



End Class

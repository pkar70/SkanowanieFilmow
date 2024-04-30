


Imports PicSorterNS.ProcessBrowse

Public NotInheritable Class PicMenuTargetDir
    Inherits PicMenuBase

    Private _itemPaste As New MenuItem
    Private Shared _clipForTargetDir As String

    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If _wasApplied Then Return

        MyBase.OnApplyTemplate()

        If Not InitEnableDisable("Target dir", "Operacje na wskazanym katalogu", True) Then Return

        Me.Items.Clear()

        If UseSelectedItems Then
            ' dla pojedyńczego trudno jest ustalić TargetDir (radiobuttony automatycznego podziału)
            Me.Items.Add(NewMenuItem("Set target dir", "Ustawianie katalogu docelowego", AddressOf uiCreateTargetDir_Click))
        End If

        If UseSelectedItems Then
            Me.Items.Add(NewMenuItem("Make same", "Skopiowanie katalogu docelowego między zdjęciami", AddressOf uiTargetMakeSame_Click))
        End If

        If Not UseSelectedItems Then
            Me.Items.Add(NewMenuItem("Copy TargetDir", "Skopiowanie katalogu docelowego do lokalnego schowka", AddressOf uiTargetToClip_Click, String.IsNullOrWhiteSpace(GetFromDataContext.TargetDir)))
        End If

        'oNew = New MenuItem
        _itemPaste = NewMenuItem("Paste TargetDir", "Narzucenie zdjęciom katalogu docelowego wg lokalnego schowka", AddressOf uiTargetPaste_Click, Not String.IsNullOrWhiteSpace(_clipForTargetDir))
        Me.Items.Add(_itemPaste)

        Me.Items.Add(NewMenuItem("Clear TargetDir", "Usunięcie wskazania katalogu docelowego", AddressOf uiTargetClear_Click, UseSelectedItems OrElse String.IsNullOrWhiteSpace(GetFromDataContext.TargetDir)))

        _wasApplied = True
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

    End Sub

    Private Sub uiTargetClear_Click(sender As Object, e As RoutedEventArgs)
        OneOrMany(Sub(x) x.TargetDir = "")
        EventRaise(Me)
    End Sub


    Private Sub uiTargetToClip_Click(sender As Object, e As RoutedEventArgs)
        _clipForTargetDir = GetFromDataContext.TargetDir
    End Sub

    Private Sub uiTargetPaste_Click(sender As Object, e As RoutedEventArgs)
        OneOrMany(Sub(x) If String.IsNullOrWhiteSpace(x.TargetDir) Then x.TargetDir = _clipForTargetDir)
        EventRaise(Me)
    End Sub

    Private Sub uiCreateTargetDir_Click(sender As Object, e As RoutedEventArgs)

        If Not UseSelectedItems Then Return ' działa tylko na wielu zdjęciach
        Dim listaSelected As List(Of ProcessBrowse.ThumbPicek) = GetSelectedItems()
        Dim listaFull As List(Of ProcessBrowse.ThumbPicek) = GetFullLista()

        Dim oWnd As New TargetDir(listaFull, listaSelected)
        If Not oWnd.ShowDialog Then Return
        ' ale to jest bardzo skomplikowane, bo operuje na całej liście do auto-dzielenia

        EventRaise(Me)

    End Sub



End Class

Imports Org.BouncyCastle.Utilities.Collections
Imports pkar


Public Class PicMenuKwds
    Inherits PicMenuBase

    Private _itemRemove As MenuItem
    Private _itemPaste As MenuItem
    Private _itemForce As MenuItem


    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If _wasApplied Then Return

        MyBase.OnApplyTemplate()

        If Not InitEnableDisable("keywords", "Operacje na słowach kluczowych", True) Then Return

        Me.Items.Clear()

        'Me.Items.Add(NewMenuItem("Add kwd", "Dodanie słowa kluczowego", AddressOf uiAddKwd_Click))
        _itemRemove = NewMenuItem("Remove kwd", "Usunięcie słowa kluczowego", Nothing)
        Me.Items.Add(_itemRemove)
        _itemRemove.Items.Add("(no entries)")
        Me.Items.Add(NewMenuItem("Copy kwds", "Skopiuj słowa kluczowe do lokalnego schowka (ze wszystkich zdjęć)", AddressOf uiCopyKwds_Click))
        _itemPaste = NewMenuItem("Paste kwds", "Dodaj słowa kluczowe wedle lokalnego schowka", AddressOf uiKwdsPaste_Click, False)
        Me.Items.Add(_itemPaste)
        _itemForce = NewMenuItem("Force kwds", "Narzuć słowa kluczowe lokalnego schowka", AddressOf uiKwdsForce_Click, False)
        Me.Items.Add(_itemForce)

        Me.Items.Add(NewMenuItem("Reset kwds", "Usuń wszystkie słowa kluczowe", AddressOf uiRemoveAll_Click))

        AddHandler Me.SubmenuOpened, AddressOf wypelnKwdsIstniejace

        _wasApplied = True
    End Sub

    Private Sub wypelnKwdsIstniejace(sender As Object, e As System.Windows.RoutedEventArgs)
        Dim sumKwd As String = JakieSaKwds()
        Dim temp As String() = sumKwd.Replace(",", "").Replace("|", "").Split(" ")

        _itemRemove.Items.Clear()

        For Each kwd As String In From c In temp Distinct
            If String.IsNullOrWhiteSpace(kwd) Then Continue For
            _itemRemove.Items.Add(NewMenuItem(kwd, Nothing, AddressOf UsunTenJeden_Click))
        Next

        If _itemRemove.Items.Count < 1 Then
            _itemRemove.Items.Add("(no entries)")
        End If

    End Sub

    ''' <summary>
    ''' zwraca wszystkie słowa kluczowe, ze wszystkich zaznaczonych zdjęć
    ''' </summary>
    Private Function JakieSaKwds() As String
        Dim sumKwd As String = ""

        OneOrMany(
         Sub(x)
             sumKwd &= "|" & x.GetAllKeywords()
         End Sub
        )
        Return sumKwd
    End Function

    Private Sub UsunTenJeden_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As MenuItem = TryCast(sender, MenuItem)
        If oFE Is Nothing Then Return

        Dim kwd As String = oFE.Header

        OneOrMany(Sub(x)
                      x.RemoveKeyword(kwd, Application.GetKeywords)
                  End Sub
        )

        EventRaise(Me)

    End Sub

    Private _clipKwds As String

    Private Sub uiCopyKwds_Click(sender As Object, e As RoutedEventArgs)
        _clipKwds = JakieSaKwds()
        _itemForce.IsEnabled = True
        _itemPaste.IsEnabled = True
    End Sub


    Private Sub uiKwdsPaste_Click(sender As Object, e As RoutedEventArgs)

        OneOrMany(Sub(x)
                      x.ReplaceOrAddExif(Application.GetKeywords.CreateManualTagFromKwds(_clipKwds & " " & x.GetAllKeywords))
                      x.sumOfKwds = x.GetAllKeywords & " "
                  End Sub
        )

        EventRaise(Me)
    End Sub

    Private Sub uiRemoveAll_Click(sender As Object, e As RoutedEventArgs)
        ' to jest trochę ryzykowne!
        OneOrMany(Sub(x)
                      x.RemoveExifOfType(Vblib.ExifSource.ManualTag)
                      x.sumOfKwds = x.GetAllKeywords & " "
                  End Sub
        )

        EventRaise(Me)
    End Sub

    Private Sub uiKwdsForce_Click(sender As Object, e As RoutedEventArgs)
        ' to jest trochę ryzykowne!
        OneOrMany(Sub(x)
                      x.ReplaceOrAddExif(Application.GetKeywords.CreateManualTagFromKwds(_clipKwds))
                      x.sumOfKwds = x.GetAllKeywords & " "
                  End Sub
        )

        EventRaise(Me)
    End Sub


    'Private Sub uiAddKwd_Click(sender As Object, e As RoutedEventArgs)
    '    Throw New NotImplementedException()
    'End Sub



End Class

Imports Org.BouncyCastle.Utilities.Collections
Imports pkar
Imports Vblib


Public Class PicMenuKwds
    Inherits PicMenuBase

    Protected Overrides Property _minAktualne As SequenceStages = SequenceStages.CropRotate
    Protected Overrides Property _maxAktualne As SequenceStages = SequenceStages.LocalArch


    Private Shared _itemRemove As MenuItem
    Private Shared _itemForce As MenuItem
    Private Shared _miCopy As MenuItem
    Private Shared _miPaste As MenuItem


    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If Not String.IsNullOrWhiteSpace(Me.Header) Then Return

        MyBase.OnApplyTemplate()

        If Not InitEnableDisable("keywords", "Operacje na słowach kluczowych", True) Then Return

        Me.Items.Clear()

        'Me.Items.Add(AddMenuItem("Add kwd", "Dodanie słowa kluczowego", AddressOf uiAddKwd_Click))
        _itemRemove = AddMenuItem("Remove kwd", "Usunięcie słowa kluczowego", Nothing)
        _itemRemove.Items.Add("(no entries)")
        _miCopy = AddMenuItem("Copy kwds", "Skopiuj słowa kluczowe do lokalnego schowka (ze wszystkich zdjęć)", AddressOf CopyCalled)
        _miPaste = AddMenuItem("Paste kwds", "Dodaj słowa kluczowe wedle lokalnego schowka", AddressOf Pastecalled, False)
        _itemForce = AddMenuItem("Force kwds", "Narzuć słowa kluczowe lokalnego schowka", AddressOf uiKwdsForce_Click, False)

        AddMenuItem("Reset kwds", "Usuń wszystkie słowa kluczowe", AddressOf uiRemoveAll_Click)

        AddHandler Me.SubmenuOpened, AddressOf wypelnKwdsIstniejace

    End Sub

    Private Sub wypelnKwdsIstniejace(sender As Object, e As System.Windows.RoutedEventArgs)
        Dim sumKwd As String = JakieSaKwds()
        Dim temp As String() = sumKwd.Replace(",", "").Replace("|", "").Split(" ")

        _itemRemove.Items.Clear()

        For Each kwd As String In From c In temp Distinct
            If String.IsNullOrWhiteSpace(kwd) Then Continue For
            '  tu się dzieje: System.InvalidOperationException: Element already has a logical parent. It must be detached from the old parent before it is attached to a new one.
            _itemRemove.Items.Add(AddMenuItem(kwd, Nothing, AddressOf UsunTenJeden_Click))
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
                      x.RemoveKeyword(kwd, vblib.GetKeywords)
                  End Sub
        )

        EventRaise(PicMenuModifies.Kwds)

    End Sub

    Private Shared _clip As String

    Private Sub CopyCalled(sender As Object, e As RoutedEventArgs)
        _clip = JakieSaKwds()
        _miPaste.IsEnabled = True
        _itemForce.IsEnabled = True
    End Sub


    Private Sub Pastecalled(sender As Object, e As RoutedEventArgs)

        OneOrMany(Sub(x)
                      x.ReplaceOrAddExif(Vblib.GetKeywords.CreateManualTagFromKwds(_clip & " " & x.GetAllKeywords))
                      x.sumOfKwds = x.GetAllKeywords & " "
                  End Sub
        )

        EventRaise(PicMenuModifies.Kwds)
    End Sub

    Private Sub uiRemoveAll_Click(sender As Object, e As RoutedEventArgs)
        ' to jest trochę ryzykowne!
        OneOrMany(Sub(x)
                      x.RemoveExifOfType(Vblib.ExifSource.ManualTag)
                      x.sumOfKwds = x.GetAllKeywords & " "
                  End Sub
        )

        EventRaise(PicMenuModifies.Kwds)
    End Sub

    Private Sub uiKwdsForce_Click(sender As Object, e As RoutedEventArgs)
        ' to jest trochę ryzykowne!
        OneOrMany(Sub(x)
                      x.ReplaceOrAddExif(Vblib.GetKeywords.CreateManualTagFromKwds(_clip))
                      x.sumOfKwds = x.GetAllKeywords & " "
                  End Sub
        )

        EventRaise(PicMenuModifies.Kwds)
    End Sub


    'Private Sub uiAddKwd_Click(sender As Object, e As RoutedEventArgs)
    '    Throw New NotImplementedException()
    'End Sub



End Class

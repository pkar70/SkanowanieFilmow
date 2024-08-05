Imports System.Windows.Media.Converters
Imports Org.BouncyCastle.Crypto
Imports pkar.UI.Configs

Class SettingsAutoTagsDef

    Private _lista As New List(Of JedenAutoTagDefault)

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        uiAutotagsExact.GetSettingsBool

        'Dim autoSelect As String = Vblib.GetSettingsString("uiDefaultAutoTags")
        '_lista.Clear()

        'For Each oEngine As Vblib.AutotaggerBase In Vblib.gAutoTagery
        '    Dim oNew As New JedenAutoTagDefault
        '    oNew.nazwa = oEngine.Nazwa.Replace("_", "__")
        '    oNew.checked = autoSelect.Contains(oEngine.Nazwa & "|")
        '    _lista.Add(oNew)
        'Next

        'uiLista.ItemsSource = _lista

        uiDefaultAutoTags.SetItems(Vblib.gAutoTagery.Select(Of String)(Function(x) x.Nazwa).ToArray)

    End Sub

    Private Sub uiOk_Click(sender As Object, e As RoutedEventArgs)
        uiAutotagsExact.SetSettingsBool

        'Dim defaulty As String = ""
        'For Each oItem As JedenAutoTagDefault In _lista
        '    If oItem.checked Then defaulty &= oItem.nazwa.Replace("__", "_") & "|"
        'Next

        'Vblib.SetSettingsString("uiDefaultAutoTags", defaulty)
        uiDefaultAutoTags.SetSettingsString()
        Me.NavigationService.GoBack()
    End Sub


    Public Class JedenAutoTagDefault
        Public Property checked As Boolean
        Public Property nazwa As String

    End Class

End Class



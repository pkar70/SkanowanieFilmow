﻿
Imports Vblib

Public Class RemoteDescr

    Private _inArch As Boolean
    Private _thumb As ProcessBrowse.ThumbPicek
    Public Sub New(inarch As Boolean)

        ' This call is required by the designer.
        InitializeComponent()

        _inArch = inarch

        'Grid_DataContextChanged(Nothing, Nothing)
    End Sub

    Private Async Sub Grid_DataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs)
        ' thumb.oImageSrc mamy obrazek size 400 - ale chyba nie warto tu go pokazywać... od tego jest ShowBig

        Await Task.Delay(20)    ' na zmianę po stronie uiPinUnpin
        If uiPinUnpin.IsPinned Then Return

        _thumb = uiPinUnpin.EffectiveDatacontext
        Vblib.DumpCurrMethod($"(pic={_thumb.oPic.sSuggestedFilename}")

        'uiFileName.Text = _thumb.oPic.sSuggestedFilename   

        RefreshLista()
    End Sub

    Private Sub RefreshLista()
        uiLista.ItemsSource = Nothing
        uiLista.ItemsSource = vblib.GetShareDescriptionsIn.FindAllForPic(_thumb.oPic)
    End Sub

    Private Sub uiDel_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        If oFE?.DataContext Is Nothing Then Return
        Dim oDesc As Vblib.ShareDescription = oFE.DataContext

        Usun(oDesc)
    End Sub

    Private Sub uiUse_Click(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        If oFE?.DataContext Is Nothing Then Return
        Dim oDesc As Vblib.ShareDescription = oFE.DataContext

        _thumb.oPic.AddDescription(oDesc.descr)
        Usun(oDesc)

    End Sub

    Private Sub Usun(oDesc As ShareDescription)
        vblib.GetShareDescriptionsIn.Remove(oDesc)
        RefreshLista()
    End Sub

    Private Sub Window_Closing(sender As Object, e As ComponentModel.CancelEventArgs)
        ' na zamykaniu okna robimy Save zmienionych danych (bufor.Save jest na zamykaniu ProcessBrowse)
        vblib.GetShareDescriptionsIn.Save(True)
    End Sub
End Class

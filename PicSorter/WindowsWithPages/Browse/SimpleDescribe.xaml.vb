﻿Imports System.IO.Compression
Imports lib_sharingNetwork
Imports Vblib

Imports vb14 = Vblib.pkarlibmodule14

Public Class SimpleDescribe
    Private _orgDescribe As String
    Private _readonly As Boolean

    Public Sub New(bReadOnly As Boolean)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        _readonly = bReadOnly
    End Sub

    Private Sub uiApply_Click(sender As Object, e As RoutedEventArgs)
        ZmianaOpisu(False)
    End Sub

    Private Sub uiAdd_Click(sender As Object, e As RoutedEventArgs)
        ZmianaOpisu(True)
    End Sub

    ''' <summary>
    ''' zmień pic.Descriptions
    ''' </summary>
    ''' <param name="bAdd">TRUE: nowy tekst dodaj jako nowy opis; FALSE: zamień wszystkie descriptions na nowy</param>
    Private Async Sub ZmianaOpisu(bAdd As Boolean)
        ' bez zmian
        If _orgDescribe <> uiAllDescribe.Text Then

            Dim oPicek As ProcessBrowse.ThumbPicek = DataContext
            Dim descr As String = uiAllDescribe.Text.Trim
            AddToMenu(descr)

            If bAdd Then
                oPicek.oPic.AddDescription(New OneDescription(descr, ""))
                descr = oPicek.oPic.GetSumOfDescriptionsText
            Else
                oPicek.oPic.ReplaceAllDescriptions(descr)
            End If

            ' podmieniamy do pokazywania w ThumbsBrowse, a nóż zmieni się na ekranie :)
            oPicek.SumOfDescriptionsText = descr

            If Not String.IsNullOrWhiteSpace(oPicek.oPic.sharingFromGuid) Then
                ' to jest 'obce' zdjęcie, i description można temu loginowi wysłać

                Dim oNew As Vblib.ShareDescription = ShareDescription.GetForPic(oPicek, descr)
                Application.GetShareDescriptionsOut.Add(oNew)

                Dim peer = oPicek.GetLastSharePeer
                If peer IsNot Nothing Then
                    If peer.GetType Is GetType(ShareServer) Then
                        ' zdjęcie jest z serwera, więc jest mu jak wysłać komentarz
                        If Vblib.GetSettingsBool("uiSharingAutoUploadComment") OrElse Await Vblib.DialogBoxYNAsync("Zdjęcie przysłane - spróbować odesłać komentarz?") Then
                            Await lib14_httpClnt.httpKlient.UploadPicDescriptions(Application.GetShareDescriptionsOut, oNew.descr.PeerGuid, peer)
                        End If
                    Else
                        ' mamy do czynienia z loginem, czyli ktoś nam wrzucił - nie mamy jak mu wysłać komentarza, on sobie musi odebrać
                    End If
                End If
            End If

        End If

        If uiDescribeSetAndNext.IsChecked Then GoNextPic()

        uiAllDescribe.Focus()
    End Sub

    Private Sub AddToMenu(descr As String)
        uiPastePrev.IsEnabled = True
        For Each oItem As MenuItem In uiPrevMenu.Items
            If oItem.Header = descr Then Return
        Next

        If uiPrevMenu.Items.Count > 5 Then
            uiPrevMenu.Items.RemoveAt(5)
        End If

        Dim oMI As New MenuItem
        oMI.Header = descr
        AddHandler oMI.Click, AddressOf PasteThis

        uiPrevMenu.Items.Insert(0, oMI)

    End Sub

    Private Sub PasteThis(sender As Object, e As RoutedEventArgs)
        uiPrevMenuPopup.IsOpen = False
        Dim oMI As MenuItem = sender
        uiAllDescribe.Text = oMI.Header
        uiAllDescribe.Focus()
    End Sub

    Private Sub Window_DataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs)
        Dim oPicek As ProcessBrowse.ThumbPicek = DataContext
        vb14.DumpCurrMethod($"(pic={oPicek.oPic.sSuggestedFilename}")

        uiFileName.Text = oPicek.oPic.sSuggestedFilename

        _orgDescribe = oPicek.oPic.GetSumOfDescriptionsText
        uiAllDescribe.Text = _orgDescribe
        uiAllDescribe.IsReadOnly = oPicek.oPic.AreTagsInDescription
        uiAllDescribe.ToolTip = "W description są słowa kluczowe, więc nie można tu tego zmieniać"

        uiAdd.Visibility = If(_orgDescribe.Contains(" | "), Visibility.Visible, Visibility.Collapsed)

        If _readonly Then uiApply.IsEnabled = False

    End Sub

    Private Sub uiPastePrev_Click(sender As Object, e As RoutedEventArgs)
        uiPrevMenuPopup.IsOpen = Not uiPrevMenuPopup.IsOpen
    End Sub

    Private Sub GoNextPic()
        Dim oBrowserWnd As ProcessBrowse = Me.Owner
        If oBrowserWnd Is Nothing Then Return

        Dim picek As ProcessBrowse.ThumbPicek = oBrowserWnd.FromBig_Next(DataContext, False, False)
        If picek Is Nothing Then
            Me.Close()  ' koniec obrazków
        Else
            Me.DataContext = picek
            ' Window_DataContextChanged chyba się samo odpali?
        End If

    End Sub

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        uiAllDescribe.Focus()
    End Sub

    Private Sub Window_KeyUp(sender As Object, e As KeyEventArgs)
        If e.IsRepeat Then Return
        If e.Key <> Key.Escape Then Return
        Me.Close()
    End Sub
End Class

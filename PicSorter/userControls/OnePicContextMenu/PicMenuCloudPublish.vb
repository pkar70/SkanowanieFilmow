'Imports Vblib
Imports PicSorterNS.ProcessBrowse
Imports pkar.DotNetExtensions

Public Class PicMenuCloudPublish
    Inherits PicMenuCloudBase

    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If _wasApplied Then Return

        MyBase.OnApplyTemplate()

        If Not InitEnableDisable("Cloud publish") Then Return

        WypelnMenu(Me, AddressOf ApplyActionSingle, AddressOf ApplyActionMulti)

        _wasApplied = True
    End Sub


    ' istotna różnica między Publish i Archive: _engine.sZmienneZnaczenie
    Private _engine As Vblib.CloudPublish

    Private _retMsg As String = ""
    Private Async Function ApplyOnSingle(oPic As Vblib.OnePic) As Task
        _retMsg &= Await _engine.SendFile(oPic) & vbCrLf
    End Function

    Private Async Function NoPublishBoAdulty() As Task(Of Boolean)
        ' test na adultpice
        Dim iCnt As Integer = 0
        Dim sNames As String = ""
        For Each oItem As ThumbPicek In GetSelectedItems()
            If oItem.oPic.IsAdultInExifs OrElse Application.GetKeywords.IsAdultInAnyKeyword(oItem.oPic.GetAllKeywords) Then
                iCnt += 1
                sNames = sNames & vbCrLf & oItem.oPic.sSuggestedFilename
            End If
        Next
        If iCnt > 0 Then
            Dim sMsg As String = "plików zawiera"
            If iCnt = 1 Then sMsg = "plik zawiera"
            If iCnt > 1 AndAlso iCnt < 5 Then sMsg = "pliki zawierają"

            If Not Await Vblib.DialogBoxYNAsync($"{iCnt} {sMsg} ograniczenia wiekowe, kontynuować? ") Then
                Vblib.ClipPut(sNames)
                Vblib.DialogBox("Lista plików - w clipboard")
                Return True
            End If
        End If

        Return False

    End Function

    Protected Overrides Async Sub ApplyActionMulti(sender As Object, e As RoutedEventArgs)
        Dim oFE As MenuItem = sender
        _engine = oFE?.DataContext
        If _engine Is Nothing Then Return

        If Await NoPublishBoAdulty() Then Return

        Dim bSendNow As Boolean = True

        If _engine.sProvider = Vblib.Publish_AdHoc.PROVIDERNAME Then
            Dim sFolder As String = SettingsGlobal.FolderBrowser("", "Gdzie wysłać pliki?")
            If sFolder = "" Then Return
            _engine.sZmienneZnaczenie = sFolder
        Else
            bSendNow = Await Vblib.DialogBoxYNAsync("Wysłać teraz? Bo mogę tylko zaznaczyć do wysłania")
        End If


        If bSendNow Then
            Application.ShowWait(True)
            Dim sErr As String = Await _engine.Login
            If sErr <> "" Then
                Await Vblib.DialogBoxAsync(sErr)
                Application.ShowWait(False)
                Return
            End If

            _retMsg = ""

            ' to pozwala robić dwie publikacje po kolei
            OneOrMany(Sub(x) x.ResetPipeline())

            sErr = Await _engine.SendFiles(GetSelectedItems, AddressOf ProgBarInc)

            If Not String.IsNullOrWhiteSpace(_retMsg) Then Vblib.DialogBox(_retMsg)

            WypelnMenu(Me, AddressOf ApplyActionSingle, AddressOf ApplyActionMulti)

            EventRaise(Me)
        Else
            OneOrMany(Sub(x) x.AddCloudPublished(_engine.konfiguracja.nazwa, ""))
            EventRaise(Me)
        End If
    End Sub



End Class

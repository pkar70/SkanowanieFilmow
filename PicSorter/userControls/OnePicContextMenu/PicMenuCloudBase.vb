Imports System.Reflection.Metadata.Ecma335
Imports pkar.DotNetExtensions



Public MustInherit Class PicMenuCloudBase
    Inherits PicMenuBase

    Public Property IsForCloudArchive As Boolean

    ''' <summary>
    ''' HandlerSingle - operacje na remote; HandlerMulti - wysyłanie
    ''' </summary>
    ''' <param name="oMenuItem"></param>
    ''' <param name="oEventHandlerSingle"></param>
    ''' <param name="oEventHandlerMulti"></param>
    Protected Sub WypelnMenu(oMenuItem As MenuItem, oEventHandlerSingle As RoutedEventHandler, oEventHandlerMulti As RoutedEventHandler)
        oMenuItem.Items.Clear()

        Dim lista As IEnumerable(Of Vblib.AnyStorage)

        If IsForCloudArchive Then
            lista = Application.GetCloudPublishers.GetList
        Else
            lista = Application.GetCloudArchives.GetList
        End If

        For Each oEngine As Vblib.CloudArchPublBase In lista
            Dim oNew As MenuItem = NewMenuCloudOperation(oEngine)
            oNew.IsCheckable = False    ' aczkolwiek to jest default, więc pewnie nie będzie więcej miejsca od tego

            If SendOrOperateSwitch(_picek, oEngine) Then
                AddHandler oNew.Click, oEventHandlerMulti
            Else
                oNew.Items.Add(NewMenuCloudOperation("Open", oEngine, oEventHandlerSingle))
                oNew.Items.Add(NewMenuCloudOperation("Share link", oEngine, oEventHandlerSingle))
                oNew.Items.Add(NewMenuCloudOperation("Get tags", oEngine, oEventHandlerSingle))
                If Not IsForCloudArchive Then
                    ' w archiwum nie ma usuwania :)
                    oNew.Items.Add(New Separator)
                    oNew.Items.Add(NewMenuCloudOperation("Delete", oEngine, oEventHandlerSingle))
                End If
            End If

            oMenuItem.Items.Add(oNew)

        Next

        oMenuItem.IsEnabled = (oMenuItem.Items.Count > 0)

    End Sub

    Private Function SendOrOperateSwitch(picek As Vblib.OnePic, oEngine As Vblib.CloudArchPublBase) As Boolean
        If picek Is Nothing Then Return True

        If IsForCloudArchive Then
            Return Not _picek.IsCloudArchivedIn(oEngine.konfiguracja.nazwa)
        End If

        Return Not _picek.IsCloudPublishedIn(oEngine.konfiguracja.nazwa)

    End Function


    Protected MustOverride Async Sub ApplyActionMulti(sender As Object, e As RoutedEventArgs)

    Protected Async Sub ApplyActionSingle(sender As Object, e As RoutedEventArgs)
        Dim oFE As MenuItem = sender
        Dim engine As Vblib.AnyCloudStorage = oFE?.DataContext
        If engine Is Nothing Then Return

        ' albo zwykłe wysłanie, albo konkretna akcja (jak już jest tam wysłane)
        Select Case oFE.Header.ToString.ToLowerInvariant
            Case "open"
                Dim sLink As String = Await engine.GetShareLink(_picek)
                If sLink.StartsWithCI("http") Then
                    pkar.OpenBrowser(sLink)
                Else
                    If sLink = "" Then sLink = "ERROR getting sharing link"
                    Vblib.DialogBox(sLink)   ' error message
                End If

            Case "delete"
                Await engine.Delete(_picek)
                WypelnMenu(Me, AddressOf ApplyActionSingle, AddressOf ApplyActionMulti)
                EventRaise(Me) ' zmieniono metadane

            Case "get tags"
                Dim newTags As String = Await engine.GetRemoteTags(_picek)
                ' *TODO* coś z tym zrób
                EventRaise(Me) ' zmieniono metadane

            Case "share link"
                Dim sLink As String = Await engine.GetShareLink(_picek)
                If sLink.StartsWithCI("http") Then
                    Vblib.ClipPut(sLink)
                    Vblib.DialogBox("Link in ClipBoard")
                Else
                    If sLink = "" Then sLink = "ERROR getting sharing link"
                    Vblib.DialogBox(sLink)   ' error message
                End If
        End Select

    End Sub


End Class

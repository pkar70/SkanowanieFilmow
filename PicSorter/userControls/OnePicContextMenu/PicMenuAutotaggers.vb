﻿
Imports Vblib

Public NotInheritable Class PicMenuAutotaggers
    Inherits PicMenuBase

    Protected Overrides Property _minAktualne As SequenceStages = SequenceStages.Dates
    Protected Overrides Property _maxAktualne As SequenceStages = SequenceStages.AutoTaggers

    Public Overrides Sub OnApplyTemplate()
        If Not String.IsNullOrWhiteSpace(Me.Header) Then Return
        MyBase.OnApplyTemplate()

        If Not InitEnableDisable("Auto-taggers", "Uruchamianie automatycznych opisywaczy zdjęć", True) Then Return

        WypelnMenuAutotagerami(Me, AddressOf ApplyProcess)
    End Sub



    Private Shared Sub WypelnMenuAutotagerami(oMenuItem As MenuItem, oEventHandler As RoutedEventHandler)
        oMenuItem.Items.Clear()
        ' _UImenuOnClick = oEventHandler

        For Each oEngine As Vblib.AutotaggerBase In Vblib.gAutoTagery
            Dim oNew As New MenuItem
            oNew.Header = oEngine.Nazwa.Replace("_", "__")

            Dim ikony As String = oEngine.Ikony
            If ikony <> "" Then oNew.Header &= $" ({ikony})"
            oNew.DataContext = oEngine
            oNew.ToolTip = oEngine.DymekAbout
            AddHandler oNew.Click, oEventHandler
            oMenuItem.Items.Add(oNew)
        Next

        oMenuItem.IsEnabled = (oMenuItem.Items.Count > 0)

    End Sub

    Private _engine As Vblib.AutotaggerBase
    Private Async Sub ApplyProcess(sender As Object, e As RoutedEventArgs)
        Dim oFE As FrameworkElement = sender
        _engine = oFE?.DataContext
        If _engine Is Nothing Then Return

        Await OneOrManyAsync(AddressOf ApplyTagger)

        EventRaise(PicMenuModifies.Any)
    End Sub

    Private Async Function ApplyTagger(oPic As Vblib.OnePic) As Task

        If UseSelectedItems Then
            If oPic.GetExifOfType(_engine.Nazwa) IsNot Nothing Then Return
        End If

        Dim oExif As Vblib.ExifTag = Await _engine.GetForFile(oPic)
        If oExif IsNot Nothing Then
            oPic.ReplaceOrAddExif(oExif)
            oPic.TagsChanged = True
        End If
    End Function


End Class

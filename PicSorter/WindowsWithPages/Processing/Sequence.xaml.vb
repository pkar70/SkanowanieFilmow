Imports vb14 = Vblib.pkarlibmodule14
Imports pkar.WPF.Configs.Extensions

' nazwy checkboxów mają uiSequence*, bo żeby się nie powtórzyło z żadnym innym Settingsem

Public Class Sequence

    Private _loading As Boolean = True

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)

        _loading = True

        ' ustalenie checkboxów
        CheckBoxesFromSettings()

        _loading = False

        CheckAutoExif()
        CheckGeoTag()
        CheckTargetDir()
        CheckGUID()
        CheckCloudArch()
        CheckLocalArch()

    End Sub

    Private Function CheckLocalArch() As Boolean

        Dim counter As Integer = LocalArchive.CountDoArchiwizacji()
        vb14.DumpMessage("Plików do archiwizacji: " & counter)
        Dim bAllOk As Boolean = counter < 1

        uiSequenceArchive.IsChecked = bAllOk

        Return bAllOk
    End Function

    Private Function CheckCloudArch() As Boolean

        Dim counter As Integer = ProcessPic.CountDoCloudArchiwizacji()
        vb14.DumpMessage("Plików do cloud archiwizacji: " & counter)

        Dim bAllOk As Boolean = counter < 1

        uiSequenceCloudArch.IsChecked = bAllOk

        Return bAllOk
    End Function


    Private Function CheckTargetDir() As Boolean
        Dim bAllOk As Boolean = True
        If App.GetBuffer.Count > 1 Then
            For Each oFile As Vblib.OnePic In App.GetBuffer.GetList
                If String.IsNullOrEmpty(oFile.TargetDir) Then
                    vb14.DumpMessage("Plik bez TargetDir: " & oFile.sSuggestedFilename)

                    bAllOk = False
                    Exit For
                End If
            Next
        End If

        uiSequenceAddFolder.IsChecked = bAllOk

        If bAllOk Then Return True

        uiSequencePublish.IsChecked = False
        uiSequenceCloudArch.IsChecked = False
        uiSequenceArchive.IsChecked = False

        Return False
    End Function

    Private Function CheckGUID() As Boolean
        Dim bAllOk As Boolean = True
        If App.GetBuffer.Count > 1 Then
            For Each oFile As Vblib.OnePic In App.GetBuffer.GetList
                If String.IsNullOrEmpty(oFile.PicGuid) Then
                    vb14.DumpMessage("Plik bez GUID: " & oFile.sSuggestedFilename)

                    bAllOk = False
                    Exit For
                End If
            Next
        End If

        uiSequenceGUID.IsChecked = bAllOk

        Return bAllOk
    End Function

    Private Function CheckGeoTag() As Boolean
        Dim bAllOk As Boolean = True
        If App.GetBuffer.Count > 1 Then
            For Each oFile As Vblib.OnePic In App.GetBuffer.GetList
                If oFile.GetGeoTag Is Nothing Then
                    vb14.DumpMessage("Plik bez Geotag: " & oFile.sSuggestedFilename)
                    bAllOk = False
                    Exit For
                End If
            Next
        End If

        If bAllOk Then uiSequenceAddGeoTag.IsChecked = bAllOk

        'If bAllOk Then Return True

        'uiSequenceRunTaggers.IsChecked = False

        Return bAllOk
    End Function

    Private Function CheckAutoExif() As Boolean
        Dim bAllOk As Boolean = True
        If App.GetBuffer.Count > 1 Then
            For Each oFile As Vblib.OnePic In App.GetBuffer.GetList
                If oFile.GetExifOfType(Vblib.ExifSource.FileExif) Is Nothing Then
                    vb14.DumpMessage("Plik bez EXIF: " & oFile.sSuggestedFilename)

                    If Vblib.AutoTag_EXIF.CanInterpret(oFile) Then
                        bAllOk = False
                        Exit For
                    End If
                End If
            Next
        Else
            uiSequenceRetrieve.IsChecked = False
        End If

        'If bAllOk Then Return True

        If bAllOk Then uiSequenceRunAutoExif.IsChecked = bAllOk
        'uiSequenceCropRotate.IsChecked = False
        'uiSequenceAddGeoTag.IsChecked = False
        'uiSequenceRunTaggers.IsChecked = False
        'uiSequenceAddKeywords.IsChecked = False
        'uiSequenceAddDescriptions.IsChecked = False
        'uiSequenceAddFolder.IsChecked = False
        'uiSequenceGUID.IsChecked = False
        'uiSequencePublish.IsChecked = False
        'uiSequenceCloudArch.IsChecked = False
        'uiSequenceArchive.IsChecked = False

        Return bAllOk
    End Function

    Private Sub CheckBoxesFromSettings()
        uiSequenceRetrieve.GetSettingsBool
        'uiSequenceRunAutoExif.GetSettingsBool
        uiSequenceCropRotate.GetSettingsBool
        uiSequenceAddGeoTag.GetSettingsBool
        uiSequenceRunTaggers.GetSettingsBool
        uiSequenceAddKeywords.GetSettingsBool
        uiSequenceAddDescriptions.GetSettingsBool
        'uiSequenceAddFolder.GetSettingsBool
        'uiSequenceGUID.GetSettingsBool
        uiSequencePublish.GetSettingsBool
        'uiSequenceCloudArch.GetSettingsBool
        'uiSequenceArchive.GetSettingsBool
    End Sub


    ''' <summary>
    ''' usunięcie wszystkich checkboxów, do wywoływania po Retrieve
    ''' </summary>
    Public Shared Sub ResetPoRetrieve()
        vb14.SetSettingsBool("uiSequenceRetrieve", False)
        vb14.SetSettingsBool("uiSequenceRunAutoExif", False)
        vb14.SetSettingsBool("uiSequenceCropRotate", False)
        vb14.SetSettingsBool("uiSequenceAddGeoTag", False)
        vb14.SetSettingsBool("uiSequenceRunTaggers", False)
        vb14.SetSettingsBool("uiSequenceAddKeywords", False)
        vb14.SetSettingsBool("uiSequenceAddDescriptions", False)
        vb14.SetSettingsBool("uiSequenceGUID", False)
        vb14.SetSettingsBool("uiSequenceAddFolder", False)
        vb14.SetSettingsBool("uiSequencePublish", False)
        vb14.SetSettingsBool("uiSequenceCloudArch", False)
        vb14.SetSettingsBool("uiSequenceArchive", False)

    End Sub

    Private Sub uiStage_Checked(sender As Object, e As RoutedEventArgs)
        If _loading Then Return

        Dim oCB As CheckBox = sender
        oCB.SetSettingsBool()
    End Sub
End Class

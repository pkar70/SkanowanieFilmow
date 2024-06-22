Imports Vblib
Imports pkar.DotNetExtensions

Public Class DatesSummary

    Private Async Sub Window_DataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs)

        Await Task.Delay(20)    ' na zmianę po stronie uiPinUnpin
        If uiPinUnpin.IsPinned Then Return

        Dim oPicek As ProcessBrowse.ThumbPicek = DataContext

        Dim daty As New List(Of JednoDatowanie)

        ' zakładam że jakiś EXIF będzie, choćby SOURCE_DEFAULT
        For Each oExif As Vblib.ExifTag In oPicek.oPic.Exifs

            Dim bylExif As Boolean
            If oExif.DateMax.IsDateValid OrElse oExif.DateMin.IsDateValid Then
                daty.Add(New JednoDatowanie(oExif))
                bylExif = True
            End If


            If String.IsNullOrWhiteSpace(oExif.Keywords) Then Continue For

            For Each kwd As String In oExif.Keywords.Split(" ")
                Dim oKey As Vblib.OneKeyword = Application.GetKeywords.GetKeyword(kwd)
                If oKey Is Nothing Then Continue For ' np. -f1

                If oKey.maxDate.IsDateValid OrElse oKey.minDate.IsDateValid Then
                    If Not bylExif Then
                        daty.Add(New JednoDatowanie(oExif))
                        bylExif = True
                    End If
                    daty.Add(New JednoDatowanie(oKey))
                End If
            Next
        Next

        daty.Add(New JednoDatowanie With {.boldowatosc = FontWeights.Bold, 
        .opis = "Wynik",
        .minval = oPicek.oPic.GetMinDate.ToString("yyyy.MM.dd"),
        .maxval = oPicek.oPic.GetMaxDate.ToString("yyyy.MM.dd")
        })


        uiLista.ItemsSource = daty

    End Sub

    Private Sub Window_KeyUp(sender As Object, e As KeyEventArgs)
        If e.IsRepeat Then Return
        If e.Key <> Key.Escape Then Return
        Me.Close()
    End Sub

    Protected Class JednoDatowanie
        Public Property opis As String
        Public Property minval As String
        Public Property maxval As String
        Public Property boldowatosc As FontWeight = FontWeights.Normal

        Public Sub New(oExif As Vblib.ExifTag)
            opis = oExif.ExifSource.Replace("AUTO_", "")
            minval = If(oExif.DateMin.IsDateValid, oExif.DateMin.ToString("yyyy.MM.dd"), "")
            maxval = If(oExif.DateMax.IsDateValid, oExif.DateMax.ToString("yyyy.MM.dd"), "")
        End Sub

        Public Sub New(oKey As Vblib.OneKeyword)
            opis = oKey.sId
            minval = If(oKey.minDate.IsDateValid, oKey.minDate.ToString("yyyy.MM.dd"), "")
            maxval = If(oKey.maxDate.IsDateValid, oKey.maxDate.ToString("yyyy.MM.dd"), "")
        End Sub

        Public Sub New()

        End Sub

    End Class

End Class

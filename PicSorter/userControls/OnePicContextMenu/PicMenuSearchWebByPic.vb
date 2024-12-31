

Imports pkar.UI.Extensions
Imports Vblib

Public NotInheritable Class PicMenuSearchWebByPic
    Inherits PicMenuBase

    Protected Overrides Property _minAktualne As SequenceStages = SequenceStages.CropRotate

    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If Not String.IsNullOrWhiteSpace(Me.Header) Then Return

        If UseSelectedItems Then Return ' umiemy tylko pojedyńczy picek

        MyBase.OnApplyTemplate()

        ' *TODO* może być Bing, Google
        If Not InitEnableDisable("Web search by pic", "Przeszukiwanie sieci według zdjęcia", True) Then Return

        AddMenuItem("Google", "Wyszukaj w Google", AddressOf SearchGoogle)
        AddMenuItem("BING(?)", "Wyszukaj w BING (chwilowo nie działa", AddressOf SearchBing)

        'AddHandler Me.Click, AddressOf ActionClick

    End Sub

    Private Sub SearchBing(sender As Object, e As RoutedEventArgs)
        ' https://www.bing.com/images/search?view=detailv2&iss=sbi&FORM=SBIIRP&sbisrc=UrlPaste&q=imgurl:http%3A%2F%2Fspisek.karoccy.name%2Fbufpic%2FDSCN0894.JPG&idpbck=1
        SearchAny("https://www.bing.com/images/search?view=detailv2&iss=sbi&FORM=SBIIRP&sbisrc=UrlPaste&idpbck=1&q=imgurl:")
    End Sub

    Private Sub SearchGoogle(sender As Object, e As RoutedEventArgs)
        'https://lens.google.com/uploadbyurl?url=https%3A%2F%2Fwww.google.com%2Fimages%2Fbranding%2Fgooglelogo%2F1x%2Fgooglelogo_white_background_color_272x92dp.png&hl=pl&re=df&st=1714127463305&vpw=1272&vph=620&ep=gsbubu
        SearchAny("https://lens.google.com/uploadbyurl?hl=pl&re=df&st=1714127463305&vpw=1272&vph=620&ep=gsbubu&url=")
    End Sub

    Private Async Sub SearchAny(baselink As String)
        If UseSelectedItems Then Return ' umiemy tylko pojedyńczy picek

        Dim oPic As Vblib.OnePic = GetFromDataContext()
        Dim localUri As String = Await GetLocalUriInBuff(oPic)
        Dim oUri As New Uri(baselink & localUri)
        oUri.OpenBrowser
    End Sub

    Public Shared Async Function GetLocalUriInBuff(oPic As Vblib.OnePic) As Task(Of String)
        Dim currMe As String = Await SettingsShareLogins.GetCurrentMeAsWeb & ":" & Vblib.Globs.APP_HTTP_PORT
        Return "http://" & currMe & "/bufpic/" & IO.Path.GetFileName(oPic.InBufferPathName)
    End Function

End Class



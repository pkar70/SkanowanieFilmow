Imports System.Runtime.Intrinsics

Class MainWindow
    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)

        'Dim oFbConfig As New Vblib.CloudConfig

        'oFbConfig.sUsername = ""
        'oFbConfig.sPswd = ""

        'Dim oFB As New Publish_std2_Facebook.Publish_Facebook_Post
        'oFB.konfiguracja = oFbConfig

        'oFB.Login()
        Dim fb = New Facebook.FacebookClient(AppToken)
        Dim resp = fb.Get("me?fields=id,name")
        '{
        '  "id" "8540905445942550",
        '  "name": "Piotr Karocki"
        '}

        ' https://developers.facebook.com/docs/development/register
        ' https://developers.facebook.com/tools/accesstoken
        ' https://artt.dev/en/blog/2021/facebook-token/

        ' You can publish posts by using the /{user-id}/feed, /{page-id}/feed, /{event-id}/feed, or /{group-id}/feed
        Dim resp1 = fb.Get("me/albums")
        '  {"data": [
        '    {
        '      "created_time": "2024-08-15T13:05:39+0000",
        '      "name": "Analogi",
        '      "id": "8488219767877785"
        '    },
        '    ...
        '            ],
        '  "paging": {
        '    "cursors": {
        '      "before": "ODQ4ODIxOTc2Nzg3Nzc4NQZDZD",
        '      "after": "NTcyMjM2NDYxNDQ2MzMyOAZDZD"
        '    },
        '    "next": "https://graph.facebook.com/v15.0/8540905445942550/albums?access_token=EAAJcht0ef1IBOw6JELuAYw1oHmQbwPq72d6Af2FTcywrPNjuyHcEaUKZAHuOJ7MQNnE3VtyI4oZAI9jHXkF7TsayiRmZAfHep02VmxHVVWrYZAHQkuJoKlwge4mWe768LzjwYwdvZAZC7shiiqxNZAwh8C2jvG3EWAEx3EZCkmF4E9nImKyho5y1bMI9Tsnt0OZCOZCw4gMe8RSZAptuIM73U0ZD&pretty=0&limit=25&after=NTcyMjM2NDYxNDQ2MzMyOAZDZD"
        '  }
        '}


        Dim resp2 = fb.Get("me/groups")

        Dim resp3 = fb.Get("8488219767877785")
        Dim resp4 = fb.Get("8488219767877785/photos")
        '        {

        '  "data" [
        '    {
        '      "created_time": "2024-08-15T13:05:41+0000",
        '      "id": "8488215871211508"
        '    }, ...
        '      ],
        '  "paging": {
        '    "cursors": {
        '      "before": "ODQ4ODIxNTg3MTIxMTUwOAZDZD",
        '      "after": "ODQ4ODIxODQyNDU0NDU4NgZDZD"
        '    }
        '  }
        '}
        Dim resp5 = fb.Get("me/accounts?fields=access_token")
        Dim pagetoken As String = ""

        Dim fbalb = New Facebook.FacebookClient(pagetoken)

        Dim mobj As New Facebook.FacebookMediaObject


        Dim param As New Dictionary(Of String, Object)
        ' param.Add("attachment", }};
        param.Add("caption", "takiesobietestowe")
        param.Add("url", "http://spisek.karoccy.name/dW.jpg")
        param.Add("no_story", "true")

        Dim resp6 = fbalb.Post("8488219767877785/photos", param)
        ' https://developers.facebook.com/docs/graph-api/reference/photo
        ' caption
        ' url - wersja 1, do już postniętego, albo 
        ' The URL of a photo that is already uploaded to the Internet. You must specify this or a file attachment
        ' This endpoint supports read-after-write and will read the node represented by id in the return type.
        '        Struct {
        '        id: numeric String,
        '        post_id: token with structure: Post ID,
        '}


        '  "error" {
        '    "message": "(#200) This endpoint is deprecated since the required permission publish_actions is deprecated",
        '    "type": "OAuthException",
        '    "code": 200,
        '    "fbtrace_id": "AZlJA1gk6S5ZD4aJEF8g5pi"
        '  }
        '}

    End Sub
End Class

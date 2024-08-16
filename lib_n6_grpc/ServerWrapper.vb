

Imports Microsoft.AspNetCore.Builder
Imports Microsoft.AspNetCore.Hosting
Imports Microsoft.Extensions.DependencyInjection

' https://stackoverflow.com/questions/74151894/grpc-server-within-a-wpf-desktop-application
' zrobi³o siê czerwone jak doda³em Grpc.Tools oraz Google.Protobuf
' odj¹³em je - i dalej jest czerwone

Public Class ServerWrapper

    Private _app As Microsoft.AspNetCore.Builder.WebApplication

    Public Sub StartSvc()
        If _app Is Nothing Then
            ' w ten sposób serwer jest w oddzielnym w¹tku
            Task.Run(Sub() InitService())
        End If
    End Sub

    Public Async Function StopSvc() As Task
        If _app IsNot Nothing Then Await _app.StopAsync
    End Function

    Private Sub InitService()
        Dim builder As WebApplicationBuilder = WebApplication.CreateBuilder()

        ' Despite adding a "launchSettings.json" file to the project, I couldn't find a way 
        ' for the builder to pick it up, so had to configure the URLs here
        builder.WebHost.UseUrls({"http://localhost:20563/"}) APP_HTTP_PORT
        builder.Services.AddGrpc

        _app = builder.Build()
        'ale NIE! to uruchamianie gRPC, a nie WCF :)
        _app.MapGrpcService(Of PicSortService)()
        _app.Run()

    End Sub


End Class

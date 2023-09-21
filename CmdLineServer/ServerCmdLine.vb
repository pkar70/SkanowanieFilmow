Imports System.ServiceModel

Module ServerCmdLine

    Private _host As ServiceHost

    Sub Main()

        Dim iPort As Integer = &H5053

        If My.Application.CommandLineArgs.Count = 1 Then
            If Not Integer.TryParse(My.Application.CommandLineArgs(0), iPort) Then
                iPort = &H5053
                Console.WriteLine("Then one and only parameter can be only TCP port number! So using default " & iPort)
            End If
        End If

        Dim sUri As String = $"http://localhost:{iPort}/PicSort"
        Console.WriteLine("Using URI: " & sUri)

        _host = New ServiceHost(GetType(PicSortService), New Uri(sUri))

        _host.AddServiceEndpoint(GetType(IPicSortService), New WSHttpBinding(), "PicSort")

        ' tego nie ma defaultowego, więc dodajemy (tu lub w app.config)
        Dim smb As New Description.ServiceMetadataBehavior() With {.HttpGetEnabled = True, .HttpsGetEnabled = True}
        _host.Description.Behaviors.Add(smb)

        ' to i tak jest defaultowo, niezależnie od app.config - więc znaleźć i zmienić
        Dim sdb As Description.ServiceDebugBehavior = _host.Description.Behaviors.Find(Of Description.ServiceDebugBehavior)()
        sdb.IncludeExceptionDetailInFaults = True

        _host.Open()

        Console.WriteLine("service is started - host opened")

        Console.WriteLine("press <enter> to stop")

        Dim ret As String = Console.ReadLine

        _host.Close()

    End Sub

End Module

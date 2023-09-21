Imports CmdLineClient.Wcf_Server

Module ClientCmdLine

    Private _client As PicSortServiceClient

    Sub Main()
        Dim iPort As Integer = &H5053

        If My.Application.CommandLineArgs.Count = 1 Then
            If Not Integer.TryParse(My.Application.CommandLineArgs(0), iPort) Then
                iPort = &H5053
                Console.WriteLine("Then one and only parameter can be only TCP port number! So using default " & iPort)
            End If
        End If

        HelpPage()

        _client = New PicSortServiceClient
        _client.Open()

        Do
            Console.Write("Picsort:> ")
            Dim request As String = Console.ReadLine
            If Not MakeCommand(request) Then Exit Do
        Loop

        Console.WriteLine("Bye.")
        _client.Close()

    End Sub

    Private Sub HelpPage()
        Console.WriteLine("Commands:")
        Console.WriteLine("help")
        Console.WriteLine("exit")
        Console.WriteLine("trylogin GUID, e.g. {030B4A82-1B7C-11CF-9D53-00AA003C9CB6}")
    End Sub

    Private Function MakeCommand(request As String) As Boolean

        If request = "exit" Then Return False

        If request = "help" Then HelpPage()

        Dim aArgs As String() = request.Split(" ")

        If aArgs(0) = "trylogin" Then Console.WriteLine(_client.TryLogin(New Guid(aArgs(1))))

        Return True

    End Function


    ' Unhandled Exception :
    ' System.InvalidOperationException:
    ' Could Not find default endpoint element that references contract 'IPicSortService'
    ' in the ServiceModel client configuration section.
    ' This might be because no configuration file was found for your application,
    ' or because no endpoint element matching this contract could be found in the client element.

    'at System.ServiceModel.Description.ConfigLoader.LoadChannelBehaviors(ServiceEndpoint serviceEndpoint, String configurationName)
    'at System.ServiceModel.ChannelFactory.ApplyConfiguration(String configurationName, Configuration configuration)
    'at System.ServiceModel.ChannelFactory.ApplyConfiguration(String configurationName)
    'at System.ServiceModel.ChannelFactory.InitializeEndpoint(String configurationName, EndpointAddress address)
    'at System.ServiceModel.ChannelFactory`1..ctor(String endpointConfigurationName, EndpointAddress remoteAddress)
    'at System.ServiceModel.ConfigurationEndpointTrait`1.CreateSimplexFactory()
    'at System.ServiceModel.ConfigurationEndpointTrait`1.CreateChannelFactory()
    'at System.ServiceModel.ClientBase`1.CreateChannelFactoryRef(EndpointTrait`1 endpointTrait)
    'at System.ServiceModel.ClientBase`1.InitializeChannelFactoryRef()
    'at System.ServiceModel.ClientBase`1..ctor()
    'at CmdLineClient.PicSortServiceClient..ctor() In H:\Home\PIOTR\VStudio\_Vs2017\SkanowanieFilmow\CmdLineClient\PicSortService.vb:line 51
    'at CmdLineClient.ClientCmdLine.Main() In H:\Home\PIOTR\VStudio\_Vs2017\SkanowanieFilmow\CmdLineClient\ClientCmdLine.vb:line 20
End Module

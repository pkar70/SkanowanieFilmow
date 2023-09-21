' https://stackoverflow.com/questions/19599837/proper-way-to-close-dispose-of-a-servicehost-thread-in-c

Imports System.ServiceModel

Public Class ServerWrapper

    Private _host As ServiceHost

    Public Sub New(loginy As pkar.BaseList(Of Vblib.ShareLogin), databases As Vblib.DatabaseInterface)
        PicSortService.SetData(loginy, databases)
    End Sub

    Public Sub StartSvc()
        If _host Is Nothing Then

#If STD_WCF Then

            _host = New ServiceHost(GetType(PicSortService), New Uri("http://localhost:20563/HelloWCF"))

            _host.AddServiceEndpoint(GetType(IPicSortService), New WSHttpBinding(), "PicSort")

            '// Enable metadata exchange
            Dim smb As New Description.ServiceMetadataBehavior() With {.HttpGetEnabled = True, .HttpsGetEnabled = True}
            _host.Description.Behaviors.Add(smb)

            '// Enable exeption details
            Dim sdb As Description.ServiceDebugBehavior = _host.Description.Behaviors.Find(Of Description.ServiceDebugBehavior)()
            sdb.IncludeExceptionDetailInFaults = True
#Else

            Dim baseUri As New Uri("http://localhost:20563/PicSort")
            Dim contract As Type = GetType(IPicSortService)
            Dim host As New ServiceHost(GetType(PicSortService), {baseUri})

            host.AddServiceEndpoint(contract, New BasicHttpBinding(BasicHttpSecurityMode.None), "/basichttp")
            host.AddServiceEndpoint(contract, New BasicHttpsBinding(BasicHttpsSecurityMode.Transport), "/basichttp")
            'host.AddServiceEndpoint(contract, New WSHttpBinding(SecurityMode.None), "/wsHttp")
            'host.AddServiceEndpoint(contract, New WSHttpBinding(SecurityMode.Transport), "/wsHttp")
            'host.AddServiceEndpoint(contract, New NetTcpBinding(), "/nettcp")
#End If

        End If

        _host.Open()
    End Sub

    Public Sub StopSvc()
        If _host Is Nothing Then Return

        _host.Close()

    End Sub



End Class

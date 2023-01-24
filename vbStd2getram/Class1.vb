Imports System.Management

Public Class GetRam

    Public Shared Function GetGBram() As Integer
        Dim wql As New ObjectQuery("SELECT * FROM Win32_OperatingSystem")
        Dim searcher As New ManagementObjectSearcher(wql)
        Dim results As ManagementObjectCollection = searcher.Get()

        For Each result As ManagementObject In results
            Dim res As Double = Convert.ToDouble(result.Item("TotalVisibleMemorySize"))
            Return Math.Round((res / (1024 * 1024)))
        Next

        Return 0
    End Function

End Class

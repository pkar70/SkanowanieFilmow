

' wydzielony, bo w samym PicSort dawało błąd kompilacji na module pkar w WPF module (zły constructor module?)

Imports System.Management
Imports Microsoft.VisualBasic.Devices

Public Module accessRam

    'Public Function GetGBram() As Integer
    '    Return (New ComputerInfo).TotalPhysicalMemory >> 30
    'End Function
    Public Function GetGBram() As Integer
        Dim wql As New ObjectQuery("SELECT * FROM Win32_OperatingSystem")
        Dim searcher As New ManagementObjectSearcher(wql)
        Dim results As ManagementObjectCollection = searcher.Get()

        For Each result As ManagementObject In results
            Dim res As Double = Convert.ToDouble(result.Item("TotalVisibleMemorySize"))
            Return Math.Round((res / (1024 * 1024)))
        Next

        Return 0
    End Function
End Module

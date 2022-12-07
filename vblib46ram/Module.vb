

' wydzielony, bo w samym PicSort dawało błąd kompilacji na module pkar w WPF module (zły constructor module?)

Imports Microsoft.VisualBasic.Devices

Public Module accessRam

    Public Function GetGBram() As Integer
        Return (New ComputerInfo).TotalPhysicalMemory >> 30
    End Function

End Module

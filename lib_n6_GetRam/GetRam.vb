
Imports Microsoft.VisualBasic.Devices

Public Class GetRam

    Public Shared Function GetGB()

        Dim totalGBRam As Integer = Convert.ToInt32((New ComputerInfo().TotalPhysicalMemory / (Math.Pow(1024, 3))) + 0.5)

        Return totalGBRam

    End Function


End Class

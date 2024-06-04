' mam PowerPoint 2003: 11.0
' 12.0 to PPT 2007


Public Class Class1
    Public Sub alamakota()
        Dim ppt As New Microsoft.Office.Interop.PowerPoint.Application
        'ppt.Visible = True
        ppt.Presentations.Open("c:\My Documents\ex_a2a.ppt")
    End Sub
End Class

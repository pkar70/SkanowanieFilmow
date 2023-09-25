Public MustInherit Class CloudArchPublBase

    Public Property konfiguracja As CloudConfig

    Public MustOverride Property sProvider As String

    Public MustOverride Async Function SendFile(oPic As OnePic) As Task(Of String)

End Class

Public Class OneSourcePic
    Public Property ID As String    ' usually pathname
    Public Property filename As String  ' bez pathname, może być po konwersji
    Public Property Exifs As List(Of ExifTag)   ' SOURCE_DIR, SOURCE_FILENAME, SOURCE_EXIF)
    Public Property Content As IO.Stream
End Class

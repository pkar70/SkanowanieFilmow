Public Class Buffer

    Public Property sRootPath As String

    Public Function AddFile(pic As OneSourcePic) As OnePic

    End Function

	''' <summary>
	''' returns count of files newer than sinceDate (excluding)
	''' </summary>
	''' <param name="sinceDate"></param>
	''' <returns></returns>
	Public Function Count(Optional sinceDate As DateTime = Nothing) As Integer

	End Function

	''' <summary>
	''' purge all files older than given date (excluding)
	''' </summary>
	''' <param name="olderThan"></param>
	''' <returns></returns>
	Public Function Purge(Optional olderThan As DateTime = Nothing) As Boolean

	End Function

	''' <summary>
	''' get first file to download 
	''' </summary>
	''' <param name="sinceDate"></param>
	''' <returns></returns>
	Public Function GetFirst(Optional sinceDate As DateTime = Nothing) As OneSourcePic
		_lastIDreturned = New OneSourcePic
	End Function

	Private Property _lastIDreturned As OneSourcePic
	''' <summary>
	''' get next file to download
	''' </summary>
	''' <param name="sinceDate"></param>
	''' <returns></returns>
	Public Function GetNext() As OneSourcePic
		' after 		_lastIDreturned = 
	End Function

	''' <summary>
	''' get list of files inside source
	''' </summary>
	''' <param name="sinceDate"></param>
	''' <returns></returns>
	Public Function Dir(Optional sinceDate As DateTime = Nothing) As List(Of String)

	End Function

	''' <summary>
	''' get date of oldest file in source
	''' </summary>
	''' <param name="bAll"></param>
	''' <returns></returns>
	Public Function GetDateMin(Optional bAll As Boolean = False) As DateTime

	End Function

	Public Function GetDateMax() As DateTime

	End Function

End Class


'*) DELAYEDCOPY
'	ptr-localstorage bez kopii
'	ptr-localstorage z kopią
'	ptr-picture
'	- albo inaczej, w prostszej wersji, jakoś; tu także zmienione tagi do send?

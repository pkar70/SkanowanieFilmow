
' wzi¹³em "jak leci" z mojego checkIP.vbs

Imports System.Net
Imports System.Net.Http

Public Class GetMyIP

	Public Shared Async Function GetIP() As Task(Of IPAddress)

		Dim sAddr As String = Await GetIPString()
		If sAddr Is Nothing Then Return Nothing

		Dim aBytes As String() = sAddr.Split(".")

		Try
			Return New IPAddress({aBytes(0), aBytes(1), aBytes(2), aBytes(3)})
		Catch ex As Exception
			Return Nothing
		End Try

	End Function

	Public Shared Async Function GetIPString() As Task(Of String)

		Dim oHttp As New HttpClient
		Dim sPage As String = Await oHttp.GetStringAsync("https://www.myip.com/")

		Dim iInd As Integer = sPage.IndexOf("<span id=""ip""")
		If iInd < 5 Then Return Nothing

		sPage = sPage.Substring(iInd, 50)
		iInd = sPage.IndexOf(">")
		sPage = sPage.Substring(iInd + 1, 20)
		iInd = sPage.IndexOf("<")
		sPage = sPage.Substring(0, iInd)

		Dim aBytes As String() = sPage.Split(".")
		If aBytes.Length <> 4 Then Return Nothing

		Return sPage

	End Function


End Class

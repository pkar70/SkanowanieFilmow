

Imports Vblib

Public Class LocalStorageMiddle
	Inherits Vblib.LocalStorage

	Protected Overrides Function GetConvertedPathForVol(sVolLabel As String, sPath As String) As String
		Return PicSourceImplement.GetConvertedPathForVol_Folder(VolLabel, Path)
	End Function


	Public Overrides Function IsPresent() As Boolean

		Dim sPath As String = PicSourceImplement.GetConvertedPathForVol_Folder(VolLabel, Path)
		If sPath = "" Then Return False ' "nie ma takiego Vollabel"

		Dim oDrive As IO.DriveInfo = New IO.DriveInfo(sPath) ' .Net 2.0
		If Not oDrive.IsReady Then Return False

		Return True
	End Function

	Public Overrides Function Login() As String
		Return ""   ' zawsze OK
	End Function

	Public Overrides Function SendFile(oPic As OnePic) As String
		Throw New NotImplementedException()
	End Function

	Public Overrides Function GetFile(oPic As OnePic) As String
		Throw New NotImplementedException()
	End Function

	Public Overrides Function GetRemoteTags(oPic As OnePic) As String
		Throw New NotImplementedException()
	End Function

	Public Overrides Function Delete(oPic As OnePic) As String
		Throw New NotImplementedException()
	End Function

	Public Overrides Function Logout() As String
		Return ""
	End Function
End Class


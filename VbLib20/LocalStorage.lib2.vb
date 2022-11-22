

Public Class LocalStorageMiddle
	Inherits Vblib.LocalStorage

	Public Overrides Function GetConvertedPathForVol(sVolLabel As String, sPath As String) As String
		Return PicSourceImplement.GetConvertedPathForVol_Folder(VolLabel, Path)
	End Function


	Public Overrides Function IsPresent() As Boolean

		Dim sPath As String = PicSourceImplement.GetConvertedPathForVol_Folder(VolLabel, Path)
		If sPath = "" Then Return False ' "nie ma takiego Vollabel"

		Dim oDrive As IO.DriveInfo = New IO.DriveInfo(sPath) ' .Net 2.0
		If Not oDrive.IsReady Then Return False

		Return True
	End Function

End Class




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

	Public Overrides Function GetMBfreeSpace() As Integer
		Dim sPath As String = PicSourceImplement.GetConvertedPathForVol_Folder(VolLabel, Path)
		If sPath = "" Then Return -1

		Dim oDrive As IO.DriveInfo = New IO.DriveInfo(sPath) ' .Net 2.0
		If Not oDrive.IsReady Then Return -1
		Return (oDrive.AvailableFreeSpace / 1024) / 1024
	End Function


End Class


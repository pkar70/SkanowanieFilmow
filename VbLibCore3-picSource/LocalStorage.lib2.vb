﻿

Imports Vblib

Public Class LocalStorageMiddle
	Inherits Vblib.LocalStorage

	Protected Overrides Function GetConvertedPathForVol(sVolLabel As String, sPath As String) As String
		Return PicSourceImplement.GetConvertedPathForVol_Folder(VolLabel, Path, sPath)
	End Function


	Public Overrides Function IsPresent() As Boolean

		Dim sPath As String = PicSourceImplement.GetConvertedPathForVol_Folder(VolLabel, Path, "")
		If sPath = "" Then Return False ' "nie ma takiego Vollabel"

		Dim oDrive As IO.DriveInfo = New IO.DriveInfo(sPath) ' .Net 2.0
		If Not oDrive.IsReady Then Return False

		Return True
	End Function

#Disable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
	Public Overrides Async Function GetMBfreeSpace() As Task(Of Integer)
#Enable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
		Dim sPath As String = PicSourceImplement.GetConvertedPathForVol_Folder(VolLabel, Path, "")

		'zmiana 2023.07.02
		Return GetMBfreeSpaceForPath(sPath)
		'If sPath = "" Then Return -1

		'Dim oDrive As IO.DriveInfo = New IO.DriveInfo(sPath) ' .Net 2.0
		'If Not oDrive.IsReady Then Return -1
		'Return (oDrive.AvailableFreeSpace / 1024) / 1024
	End Function

	Public Shared Function GetMBfreeSpaceForPath(forPath As String) As Integer
		If String.IsNullOrWhiteSpace(forPath) Then Return -1

		Dim oDrive As IO.DriveInfo = New IO.DriveInfo(forPath) ' .Net 2.0
		If Not oDrive.IsReady Then Return -1
		Return (oDrive.AvailableFreeSpace / 1024) / 1024
	End Function

End Class


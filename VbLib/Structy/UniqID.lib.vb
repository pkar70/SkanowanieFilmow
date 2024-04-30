
Imports pkar.DotNetExtensions

Public Class UniqID

    Private _engines As New List(Of UniqIdOneType)

    Public Sub New(datafolder As String)
        _engines.Add(New UniqIdOneType(datafolder, GuidPrefix.DateTaken))
        _engines.Add(New UniqIdOneType(datafolder, GuidPrefix.FileDate))
        _engines.Add(New UniqIdOneType(datafolder, GuidPrefix.ScannedDate))
    End Sub

    ''' <summary>
    ''' tworzy ID dla zdjęcia, ale nie zapisuje go na dysk (tylko w pamięci)
    ''' </summary>
    ''' <param name="oPic"></param>
    ''' <returns></returns>
    Public Function SetGUIDforPic(oPic As OnePic) As Boolean

        Dim sTempId As String = oPic.GetSuggestedGuid

        For Each oEngine As UniqIdOneType In _engines
            Dim sGuid As String = oEngine.TryMadeUniq(sTempId)
            If sGuid = "" Then Continue For

            oPic.PicGuid = sGuid
            Return True
        Next

        Return False
    End Function

    ''' <summary>
    ''' podczas archiwizacji - zapisanie ID do listy (jak już targetDir się nie będzie zmieniał, i faktycznie archiwizujemy)
    ''' potrzebny jest TargetDir oraz filename w archiwum
    ''' </summary>
    ''' <param name="oPic"></param>
    Public Sub StoreGUID(oPic As OnePic)

        If oPic.ArchivedCount > 0 Then Return ' skoro już był archiwizowany, to na pewno mamy IDa zapisanego

        If String.IsNullOrWhiteSpace(oPic.PicGuid) Then
            SetGUIDforPic(oPic)
        End If

        For Each oEngine As UniqIdOneType In _engines
            oEngine.StoreGUID(oPic.PicGuid, oPic.TargetDir, oPic.sSuggestedFilename)
        Next

    End Sub


    Protected Class UniqIdOneType
        Private _dataFilename As String
        Private _prefix As String
        Private _idlist As List(Of String)

        Public Sub New(datafolder As String, idPrefix As String)
            _dataFilename = IO.Path.Combine(datafolder, "uniqID" & idPrefix & ".txt")
            _prefix = idPrefix

            If Not IO.File.Exists(_dataFilename) Then
                _idlist = New List(Of String)
            Else
                _idlist = IO.File.ReadAllLines(_dataFilename).ToList
            End If

        End Sub

        Public Function TryMadeUniq(sBaseID As String) As String
            If Not sBaseID.StartsWithOrdinal(_prefix) Then Return ""

            Dim iCount As Integer = 0
            For Each sId As String In _idlist
                If Not sId.StartsWith(sBaseID) Then Continue For
                iCount += 1
            Next

            If iCount > 0 Then sBaseID = sBaseID & "-" & iCount

            _idlist.Add(sBaseID)

            Return sBaseID

        End Function

        Public Sub StoreGUID(sUniqId As String, sFolder As String, sFile As String)
            If Not sUniqId.StartsWithOrdinal(_prefix) Then Return

            IO.File.AppendAllText(_dataFilename, sUniqId & vbTab & sFolder & vbTab & sFile & vbCrLf)
        End Sub

    End Class

End Class


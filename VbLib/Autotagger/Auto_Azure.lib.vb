﻿


' dokumentacja jest pomieszana, są różne rzeczy w różnych rzeczach napisane
'https://github.com/Azure-Samples/cognitive-services-quickstart-code/blob/master/dotnet/ComputerVision/ImageAnalysisQuickstart.cs

' OCR tutaj nie ma!


Imports System.ComponentModel
Imports System.IO
Imports MetadataExtractor.Formats.Jpeg
Imports Microsoft.Azure.CognitiveServices.Vision
Imports Microsoft.Azure.CognitiveServices.Vision.ComputerVision.ComputerVisionClientExtensions
Imports pkar.DotNetExtensions

Public Class Auto_AzureTest
    Inherits Vblib.AutotaggerBase

    Public Overrides ReadOnly Property Typek As Vblib.AutoTaggerType = Vblib.AutoTaggerType.WebAccount
    Public Overrides ReadOnly Property Nazwa As String = ExifSource.AutoAzure
    Public Overrides ReadOnly Property MinWinVersion As String = "7.0"
    Public Overrides ReadOnly Property DymekAbout As String = "Próba co można wyciągnąć, 20 na minutę"
    Public Overrides ReadOnly Property MaxSize As Integer = 3800
    Public Overrides ReadOnly Property includeMask As String = "*.jpg;*.jpg.thumb;*.nar;*.stereo.zip"
    Public Overrides ReadOnly Property IsWeb As Boolean = True

    Private _oClient As ComputerVision.ComputerVisionClient
    Private _resizeEngine As Vblib.PostProcBase

    Public Sub New(resizeEngine As Vblib.PostProcBase)
        _resizeEngine = resizeEngine
    End Sub


    Private Function EnsureClient() As Boolean
        If _oClient IsNot Nothing Then Return True

        Dim sEndPoint As String = Vblib.GetSettingsString("uiAzureEndpoint")
        Dim sSubscriptionKey As String = Vblib.GetSettingsString("uiAzureSubscriptionKey")

        _oClient = AzureLogin(sEndPoint, sSubscriptionKey)
        If _oClient Is Nothing Then Return False

        Return True
    End Function

    Private Async Function GetStreamForSending(oFile As Vblib.OnePic) As Task(Of Stream)

        If oFile.MatchesMasks("*.stereo.zip") Then
            ' tylko tymczasowo tak - NAR jest obsługa gdzie indziej, tu więc jest tylko JPG z Lumia650, okolo 2 MB - więc w zakresie
            Dim strumyk As Stream = oFile.SinglePicFromMulti
            If strumyk.Length < MaxSize * 1024 Then Return strumyk
            Return Nothing
        Else
            Dim sFilename As String = oFile.InBufferPathName

            ' zabezpieczenie wielkościowe (limit Azure)
            Dim oFileInfo As IO.FileInfo = New IO.FileInfo(sFilename)
            If oFileInfo.Length < MaxSize * 1024 Then Return IO.File.OpenRead(oFile.InBufferPathName)

            ' przeskalujemy
            If Not Await _resizeEngine.Apply(oFile, True, "") Then Return Nothing
            If oFile._PipelineOutput.Length > MaxSize * 1024 Then Return Nothing
            oFile._PipelineOutput.Seek(0, SeekOrigin.Begin)

            Return oFile._PipelineOutput

        End If

    End Function

    'Private Async Function GetFileFromZip(oFile As Vblib.OnePic) As Task(Of String)

    '    Dim sTempFileName As String = IO.Path.GetTempFileName
    '    Await Nar2Jpg(oFile.InBufferPathName, sTempFileName)
    '    Return sTempFileName
    'End Function

    ''' <summary>
    ''' set (or clear) HIDDEN attribute on given file
    ''' </summary>
    ''' <param name="filename"></param>
    ''' <param name="hidden"></param>
    Private Shared Sub FileAttrHidden(filename As String, hide As Boolean)
        Dim attrs As IO.FileAttributes
        attrs = IO.File.GetAttributes(filename)

        If hide Then
            If attrs And IO.FileAttributes.Hidden Then Return
            attrs = attrs Or IO.FileAttributes.Hidden
        Else
            If attrs And IO.FileAttributes.Hidden <> IO.FileAttributes.Hidden Then Return
            attrs = attrs Xor IO.FileAttributes.Hidden
        End If

        IO.File.SetAttributes(filename, attrs)
    End Sub

    Public Shared Async Function Nar2Jpg(picek As OnePic) As Task(Of String)
        Vblib.DumpCurrMethod(picek.InBufferPathName)

        Dim sJpgFileName As String = IO.Path.GetTempFileName

        Using memStream As Stream = picek.SinglePicFromMulti
            If memStream Is Nothing Then Return ""

            Using oWrite As Stream = IO.File.Create(sJpgFileName)
                Await memStream.CopyToAsync(oWrite)
                Await oWrite.FlushAsync()
                'oWrite.Dispose()
            End Using
        End Using

        Return sJpgFileName

    End Function

    Public Overrides Async Function GetForFile(oFile As Vblib.OnePic) As Task(Of Vblib.ExifTag)

        If Not CanTag(oFile) Then Return Nothing

        If oFile.MatchesMasks("*.nar") Then
            Dim sTempfilename As String = Await Nar2Jpg(oFile)

            ' tworzymy nowy oPic, dla wypakowanego pliku
            Dim oPic As New OnePic("TempForAzure", "", sTempfilename)
            oPic.InBufferPathName = sTempfilename
            Dim oAzureTag As ExifTag = Await GetForFile(oPic)

            oPic._PipelineInput?.Dispose()
            oPic._PipelineInput = Nothing
            oPic._PipelineOutput?.Dispose()
            oPic._PipelineOutput = Nothing

            IO.File.Delete(sTempfilename)
            Return oAzureTag
        End If

        Dim oStream As Stream = Await GetStreamForSending(oFile)
        If oStream Is Nothing Then Return Nothing

        If Not EnsureClient() Then Return Nothing

        If Not Vblib.GetSettingsBool("uiAzurePaid") Then Await Task.Delay(3000)  ' 20/min, 20/60, raz na 3 sekundy; 2024.04.09, zmiana z 3000 na 3200, potem 3500

        Dim oNew As New Vblib.ExifTag(Nazwa)
        oNew.AzureAnalysis = Await AnalyzeImageLocal(oStream)
        oStream.Dispose()

        If oNew.AzureAnalysis Is Nothing Then Return Nothing
        ' jednak nie potrzebujemy mieć wersji jednolinijkowej, bo szukamy w Query dokładniej
        ' oNew.UserComment = oNew.AzureAnalysis.ToUserComment

        Return oNew

        ' Detect objects in an image.
        'Await DetectObjectsLocal(oClient, oFile.InBufferPathName)

        '' Detect domain-specific content in both a URL image And a local image.
        'Await DetectDomainSpecific(oClient, oFile.InBufferPathName)

        ' Microsoft.Azure.CognitiveServices.Vision.ComputerVision.ComputerVisionClientExtensions.ReadAsync

    End Function

    Private Shared Function AzureLogin(endpoint As String, key As String) As ComputerVision.ComputerVisionClient
        If String.IsNullOrWhiteSpace(endpoint) Then Return Nothing
        If String.IsNullOrWhiteSpace(key) Then Return Nothing

        Dim client As New ComputerVision.ComputerVisionClient(
            New ComputerVision.ApiKeyServiceClientCredentials(key))

        client.Endpoint = endpoint
        Return client
    End Function

    Public Shared _AzureExceptionsGuard As Integer
    Public Shared _AzureExceptionMsg As String

    Private Async Function AnalyzeImageLocal(oStream As Stream) As Task(Of MojeAzure)

        Dim features As New List(Of ComputerVision.Models.VisualFeatureTypes?)() From {
            ComputerVision.Models.VisualFeatureTypes.Categories,
            ComputerVision.Models.VisualFeatureTypes.Description,
            ComputerVision.Models.VisualFeatureTypes.Faces,
            ComputerVision.Models.VisualFeatureTypes.ImageType,
            ComputerVision.Models.VisualFeatureTypes.Tags,
            ComputerVision.Models.VisualFeatureTypes.Adult,
            ComputerVision.Models.VisualFeatureTypes.Color,
            ComputerVision.Models.VisualFeatureTypes.Brands,
            ComputerVision.Models.VisualFeatureTypes.Objects
        }

        Dim results As ComputerVision.Models.ImageAnalysis
        Try
            results = Await _oClient.AnalyzeImageInStreamAsync(oStream, features)
            'Catch ex As Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.ComputerVisionErrorResponseException
            '    _AzureExceptionsGuard = -1
            '    If ex.Response.Content.ContainsCI("Out of call volume quota") Then
            '        _AzureExceptionMsg &= ex.Response.Content & vbCrLf
            '    End If
        Catch ex As Exception
            _AzureExceptionsGuard -= 1

            If ex.GetType Is GetType(Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.ComputerVisionErrorResponseException) Then
                Dim ex1 = TryCast(ex, Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.ComputerVisionErrorResponseException)
                _AzureExceptionsGuard = -1
                If ex1.Response.Content.ContainsCI("Out of call volume quota") Then
                    _AzureExceptionMsg &= ex1.Response.Content & vbCrLf
                End If
            End If

            If ex.Message.ContainsCI("Forbidden") Then
                ' ex.Response.Content
                ' {"error"{"code":"403","message": "Out of call volume quota for ComputerVision F0 pricing tier. Please retry after 5 days. To increase your call volume switch to a paid tier."}}
                ' ex.Response.
                ' ReasonPhrase    "Quota Exceeded"	String
            End If

            _AzureExceptionMsg &= ex.Message & vbCrLf

            Return Nothing
        End Try

        If results Is Nothing Then Return Nothing

        Return New MojeAzure(results)

    End Function

End Class


Partial Public Module Extensions

    <Runtime.CompilerServices.Extension>
    Public Function ToPercentString(ByVal wart As Double)
        Return wart.ToPercent.ToString() & "%"
    End Function

    <Runtime.CompilerServices.Extension>
    Public Function ToPercent(ByVal wart As Double) As Integer
        Return Math.Round(wart * 100)
    End Function


End Module

Public Class AzureColor
    'Public Property AccentColor As String
    Public Property DominantColorBackground As String
    Public Property DominantColorForeground As String
    Public Property DominantColors As String


    <Newtonsoft.Json.JsonConstructor>
    Public Sub New()

    End Sub
    Public Sub New(oColors As ComputerVision.Models.ColorInfo)
        'AccentColor = oColors.AccentColor
        DominantColorBackground = oColors.DominantColorBackground
        DominantColorForeground = oColors.DominantColorForeground
        DominantColors = String.Join(", ", oColors.DominantColors)
    End Sub
    Public Function ToDisplay() As String
        ' $"Colors: accent {AccentColor}, " &
        Dim sOut As String = $"Colors: dominant {DominantColorForeground} on {DominantColorBackground}"
        If String.IsNullOrWhiteSpace(DominantColors) Then Return sOut

        Return sOut & $", others: {DominantColors}"
    End Function

End Class

Public Class TextWithProbability
    Public Property tekst As String
    Public Property probability As Double

    Public Sub New(oCat As ComputerVision.Models.Category)
        tekst = oCat.Name
        probability = oCat.Score
    End Sub

    Public Sub New(oCat As ComputerVision.Models.ImageCaption)
        tekst = oCat.Text
        probability = oCat.Confidence
    End Sub

    Public Sub New(oCat As ComputerVision.Models.ImageTag)
        tekst = oCat.Name
        probability = oCat.Confidence
    End Sub

    Public Sub New(oItem As ComputerVision.Models.LandmarksModel)
        tekst = oItem.Name
        probability = oItem.Confidence
    End Sub

    Public Sub New(sTekst As String, dProbabil As Double)
        tekst = sTekst
        probability = dProbabil
    End Sub

    <Newtonsoft.Json.JsonConstructor>
    Public Sub New()

    End Sub

    Public Overridable Function ToDisplay() As String
        If probability = 1 Then Return tekst
        Return $"{tekst} ({probability.ToString("P")})"	' bylo: ToPercentString
    End Function

End Class

Public Class ListTextWithProbability

    Public Property lista As New List(Of TextWithProbability)

    Public Function ToComment(sHeader As String) As String
        If lista.Count < 1 Then Return ""
        Dim sOut As String = ""
        For Each oItem As TextWithProbability In lista
            sOut = sOut & ", " & oItem.ToDisplay
        Next

        Return sHeader & ": " & sOut.Substring(2) & vbCrLf  ' sub(2): pomijam pierwszy ", "

    End Function

    Public Sub Add(oNew As TextWithProbability)
        lista.Add(oNew)
    End Sub

    Public Function GetList() As List(Of TextWithProbability)
        Return lista
    End Function
End Class

Public Class TextWithProbAndBox
    Inherits TextWithProbability

    Public Property X As Integer
    Public Property Y As Integer
    Public Property Width As Integer
    Public Property Height As Integer

    Private Sub SetXY(oRect As ComputerVision.Models.BoundingRect, oMeta As ComputerVision.Models.ImageMetadata)
        X = 100.0 * oRect.X / oMeta.Width
        Y = 100.0 * oRect.Y / oMeta.Height
        Width = 100.0 * oRect.W / oMeta.Width
        Height = 100.0 * oRect.H / oMeta.Height
    End Sub
    Private Sub SetXY(oRect As ComputerVision.Models.FaceRectangle, oMeta As ComputerVision.Models.ImageMetadata)
        X = 100.0 * oRect.Left / oMeta.Width
        Y = 100.0 * oRect.Top / oMeta.Height
        Width = 100.0 * oRect.Width / oMeta.Width
        Height = 100.0 * oRect.Height / oMeta.Height
    End Sub

    Public Overloads Function ToDisplay() As String
        Return MyBase.ToDisplay() & $" @[{X}..{X + Width}%, {Y}..{Y + Height}%]"
    End Function

    Public Sub New(oItem As ComputerVision.Models.DetectedBrand, oMeta As ComputerVision.Models.ImageMetadata)
        MyBase.New(oItem.Name, oItem.Confidence)

        SetXY(oItem.Rectangle, oMeta)
    End Sub

    Public Sub New(oItem As ComputerVision.Models.DetectedObject, oMeta As ComputerVision.Models.ImageMetadata)
        MyBase.New(oItem.ObjectProperty, oItem.Confidence)

        SetXY(oItem.Rectangle, oMeta)
    End Sub

    Public Sub New(oItem As ComputerVision.Models.CelebritiesModel, oMeta As ComputerVision.Models.ImageMetadata)
        MyBase.New(oItem.Name, oItem.Confidence)

        SetXY(oItem.FaceRectangle, oMeta)
    End Sub

    <Newtonsoft.Json.JsonConstructor>
    Public Sub New()

    End Sub


    Private Shared Function Gender2Text(plec As ComputerVision.Models.Gender?) As String
        If Not plec.HasValue Then Return "Twarz"

        If plec.Value = ComputerVision.Models.Gender.Female Then Return "Kobieta"
        Return "Mężczyzna"
    End Function

    Private Shared Function Age2Text(age As Integer) As String
        ' We have retired facial analysis capabilities that purport to infer emotional states and identity attributes, such as gender, age, smile, facial hair, hair and makeup
        Return ""

        Dim sOut As String = age.ToString & " "

        Select Case age Mod 10
            Case 2, 3, 4
                Return sOut & "lata"
            Case Else
                Return sOut & "lat"
        End Select
    End Function

    Public Sub New(oItem As ComputerVision.Models.FaceDescription, oMeta As ComputerVision.Models.ImageMetadata)
        ' We have retired facial analysis capabilities that purport to infer emotional states and identity attributes, such as gender, age, smile, facial hair, hair and makeup
        ' MyBase.New($"{Gender2Text(oItem.Gender)} {Age2Text(oItem.Age)}", 1)
        MyBase.New("", 1)

        SetXY(oItem.FaceRectangle, oMeta)
    End Sub

End Class

Public Class ListTextWithProbabAndBox

    Public Property lista As New List(Of TextWithProbAndBox)

    Public Function ToComment(sHeader As String) As String
        If lista.Count < 1 Then Return ""
        Dim sOut As String = ""
        For Each oItem As TextWithProbAndBox In lista
            ' *TODO*
            sOut = sOut & ", " & oItem.ToDisplay
        Next

        Return sHeader & ": " & sOut.Substring(2) & vbCrLf  ' sub(2): pomijam pierwszy ", "

    End Function

    Public Sub Add(oNew As TextWithProbAndBox)
        lista.Add(oNew)
    End Sub

    Public Function GetList() As List(Of TextWithProbAndBox)
        Return lista
    End Function
End Class


Public Class MojeAzure
    Inherits pkar.BaseStruct

    Public Property Captions As ListTextWithProbability
    Public Property Categories As ListTextWithProbability
    Public Property Tags As ListTextWithProbability
    Public Property Landmarks As ListTextWithProbability

    Public Property Brands As ListTextWithProbabAndBox
    Public Property Objects As ListTextWithProbabAndBox
    Public Property Celebrities As ListTextWithProbabAndBox
    Public Property Faces As ListTextWithProbabAndBox

    Public Property IsBW As Boolean
    Public Property Colors As AzureColor

    Public Property Wiekowe As String


    <Newtonsoft.Json.JsonConstructor>
    Public Sub New()

    End Sub

    Public Sub New(analysis As ComputerVision.Models.ImageAnalysis, Optional iMinProbabil As Integer = 0)
        Dim dMinProb As Double = iMinProbabil / 100.0

        ' konwersja ADULTa
        Dim sAdult As String = ""
        If analysis.Adult.IsAdultContent Then sAdult &= " ADULTPIC"
        If analysis.Adult.IsGoryContent Then sAdult &= " GORYPIC"
        If analysis.Adult.IsRacyContent Then sAdult &= " RACYPIC"
        If sAdult.Trim <> "" Then Wiekowe = sAdult


        ' pola z Prawdop
        If analysis.Description?.Captions IsNot Nothing Then
            Captions = New ListTextWithProbability
            For Each oItem As ComputerVision.Models.ImageCaption In analysis.Description?.Captions
                If oItem.Confidence > iMinProbabil Then Captions.Add(New TextWithProbability(oItem))
            Next
        End If

        If analysis.Categories IsNot Nothing AndAlso analysis.Categories.Count > 0 Then
            Categories = New ListTextWithProbability
            For Each oItem As ComputerVision.Models.Category In analysis.Categories
                If oItem.Score > iMinProbabil Then Categories.Add(New TextWithProbability(oItem))
            Next
        End If

        If analysis.Tags IsNot Nothing AndAlso analysis.Tags.Count > 0 Then
            Tags = New ListTextWithProbability
            For Each oItem As ComputerVision.Models.ImageTag In analysis.Tags
                If oItem.Confidence > iMinProbabil Then Tags.Add(New TextWithProbability(oItem))
            Next
        End If

        If analysis.Categories IsNot Nothing Then
            For Each category As ComputerVision.Models.Category In analysis.Categories
                If category.Detail?.Landmarks IsNot Nothing AndAlso category.Detail.Landmarks.Count > 0 Then
                    If Landmarks Is Nothing Then Landmarks = New ListTextWithProbability
                    For Each oItem As ComputerVision.Models.LandmarksModel In category.Detail.Landmarks
                        If oItem.Confidence > iMinProbabil Then Landmarks.Add(New TextWithProbability(oItem))
                    Next
                End If
            Next
        End If

        ' pola razem z BOX
        If analysis.Brands IsNot Nothing AndAlso analysis.Brands.Count > 0 Then
            Brands = New ListTextWithProbabAndBox
            For Each oItem As ComputerVision.Models.DetectedBrand In analysis.Brands
                If oItem.Confidence > iMinProbabil Then Brands.Add(New TextWithProbAndBox(oItem, analysis.Metadata))
            Next
        End If

        If analysis.Objects IsNot Nothing AndAlso analysis.Objects.Count > 0 Then
            Objects = New ListTextWithProbabAndBox
            For Each oItem As ComputerVision.Models.DetectedObject In analysis.Objects
                If oItem.Confidence > iMinProbabil Then Objects.Add(New TextWithProbAndBox(oItem, analysis.Metadata))
            Next
        End If

        If analysis.Categories IsNot Nothing Then
            For Each category As ComputerVision.Models.Category In analysis.Categories
                If category.Detail?.Celebrities IsNot Nothing Then
                    If Celebrities Is Nothing Then Celebrities = New ListTextWithProbabAndBox
                    For Each oItem As ComputerVision.Models.CelebritiesModel In category.Detail.Celebrities
                        If oItem.Confidence > iMinProbabil Then Celebrities.Add(New TextWithProbAndBox(oItem, analysis.Metadata))
                    Next
                End If
            Next
        End If

        If analysis.Faces IsNot Nothing AndAlso analysis.Faces.Count > 0 Then
            Faces = New ListTextWithProbabAndBox
            For Each face As ComputerVision.Models.FaceDescription In analysis.Faces
                Faces.Add(New TextWithProbAndBox(face, analysis.Metadata))
            Next
        End If

        If analysis.Color IsNot Nothing Then
            IsBW = analysis.Color.IsBWImg
            If Not IsBW Then Colors = New AzureColor(analysis.Color)
        End If

    End Sub

    Public Function ToUserComment() As String
        Dim sOutput As String = ""

        If Captions IsNot Nothing Then sOutput &= Captions.ToComment("Summary")
        If Tags IsNot Nothing Then sOutput &= Tags.ToComment("Tags")

        ' to chyba do przeróbki
        If Categories IsNot Nothing Then sOutput &= Categories.ToComment("Categories")

        If Objects IsNot Nothing Then sOutput &= Objects.ToComment("Objects")

        If Faces IsNot Nothing Then sOutput &= Faces.ToComment("Faces")

        If Brands IsNot Nothing Then sOutput &= Brands.ToComment("Brands")

        If Celebrities IsNot Nothing Then sOutput &= Celebrities.ToComment("Celebrities")
        If Landmarks IsNot Nothing Then sOutput &= Landmarks.ToComment("Landmarks")

        If IsBW Then
            sOutput &= "(black/white)"
        Else
            sOutput &= Colors.ToDisplay
        End If

        If Not String.IsNullOrWhiteSpace(Wiekowe) Then sOutput &= Wiekowe

        Return sOutput

    End Function

    ' *TODO* czy Azure ma osobno 5000 OCRów? jeśli tak, to można spróbować
End Class
'Imports System.IO
'Imports System.Net.Http
'Imports Newtonsoft.Json.Linq

'Public Class Auto_Azure2
'    Imports System.Net.Http
'    Imports System.Net.Http.Headers
'    Imports System.Text
'    Imports Newtonsoft.Json.Linq

'    Private Async Function AnalyzeImageLocal(oStream As Stream) As Task(Of MojeAzure)
'        Dim endpoint As String = "https://YOUR_REGION.api.cognitive.microsoft.com"
'        Dim apiKey As String = "YOUR_API_KEY"
'        Dim url As String = $"{endpoint}/content-understanding/image:analyze?api-version=2023-10-01-preview"

'        Dim client As New HttpClient()
'        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey)

'        ' Definicja zadań
'        Dim tasksJson As String = "
'    {
'        ""tasks"": [
'            {""taskType"": ""caption""},
'            {""taskType"": ""objectDetection""},
'            {""taskType"": ""faceDetection""}
'        ]
'    }"

'        Dim content As New MultipartFormDataContent()
'        content.Add(New StreamContent(oStream), "image", "image.jpg")
'        content.Add(New StringContent(tasksJson, Encoding.UTF8, "application/json"), "features")

'        Dim response As HttpResponseMessage
'        Try
'            response = Await client.PostAsync(url, content)
'            If Not response.IsSuccessStatusCode Then
'                _AzureExceptionsGuard -= 1
'                _AzureExceptionMsg &= $"HTTP {response.StatusCode}: {Await response.Content.ReadAsStringAsync()}" & vbCrLf
'                Return Nothing
'            End If
'        Catch ex As Exception
'            _AzureExceptionsGuard -= 1
'            _AzureExceptionMsg &= ex.Message & vbCrLf
'            Return Nothing
'        End Try

'        Dim jsonString As String = Await response.Content.ReadAsStringAsync()
'        Dim jsonResult As JObject = JObject.Parse(jsonString)

'        Return New MojeAzure(jsonResult)
'    End Function

'    Private Function IsBlackAndWhite(oStream As Stream) As Boolean
'    Try
'        Using bmp As New Bitmap(oStream)
'            Dim bwPixelCount As Integer = 0
'            Dim totalPixelCount As Integer = bmp.Width * bmp.Height

'            For y As Integer = 0 To bmp.Height - 1
'                For x As Integer = 0 To bmp.Width - 1
'                    Dim pixel As Color = bmp.GetPixel(x, y)
'                    Dim diffRG = Math.Abs(pixel.R - pixel.G)
'                    Dim diffGB = Math.Abs(pixel.G - pixel.B)
'                    Dim diffBR = Math.Abs(pixel.B - pixel.R)

'                    ' Jeśli różnice między kanałami są małe, traktujemy jako BW
'                    If diffRG < 10 AndAlso diffGB < 10 AndAlso diffBR < 10 Then
'                        bwPixelCount += 1
'                    End If
'                Next
'            Next

'            Dim bwRatio As Double = bwPixelCount / totalPixelCount
'            Return bwRatio > 0.95 ' np. 95% pikseli spełnia warunek
'        End Using
'    Catch ex As Exception
'        _AzureExceptionMsg &= "Błąd analizy BW: " & ex.Message & vbCrLf
'        Return False
'    End Try
'End Function


'    Private Function GetDominantColors(oStream As Stream) As (Background As String, Foreground As String, Colors As String)
'    Try
'        Using bmp As New Bitmap(oStream)
'            Dim colorCounts As New Dictionary(Of String, Integer)
'            Dim bgSamples As New List(Of Color)
'            Dim fgSamples As New List(Of Color)

'            Dim w = bmp.Width
'            Dim h = bmp.Height

'            ' Tło: narożniki
'            bgSamples.Add(bmp.GetPixel(0, 0))
'            bgSamples.Add(bmp.GetPixel(w - 1, 0))
'            bgSamples.Add(bmp.GetPixel(0, h - 1))
'            bgSamples.Add(bmp.GetPixel(w - 1, h - 1))

'            ' Pierwszy plan: środek
'            fgSamples.Add(bmp.GetPixel(w \ 2, h \ 2))
'            fgSamples.Add(bmp.GetPixel(w \ 2 - 10, h \ 2))
'            fgSamples.Add(bmp.GetPixel(w \ 2 + 10, h \ 2))

'            ' Cały obraz – uproszczone zliczanie
'            For y = 0 To h - 1 Step 10
'                For x = 0 To w - 1 Step 10
'                    Dim pixel = bmp.GetPixel(x, y)
'                    Dim key = $"{pixel.R \ 16 * 16},{pixel.G \ 16 * 16},{pixel.B \ 16 * 16}"
'                    If colorCounts.ContainsKey(key) Then
'                        colorCounts(key) += 1
'                    Else
'                        colorCounts(key) = 1
'                    End If
'                Next
'            Next

'            ' Najczęstsze kolory
'            Dim topColors = colorCounts.OrderByDescending(Function(kvp) kvp.Value).Take(5).Select(Function(kvp) kvp.Key).ToList()

'            ' Konwersja do hex
'            Dim bgColor = bgSamples.GroupBy(Function(c) c).OrderByDescending(Function(g) g.Count()).First().Key
'            Dim fgColor = fgSamples.GroupBy(Function(c) c).OrderByDescending(Function(g) g.Count()).First().Key

'            Dim bgHex = $"#{bgColor.R:X2}{bgColor.G:X2}{bgColor.B:X2}"
'            Dim fgHex = $"#{fgColor.R:X2}{fgColor.G:X2}{fgColor.B:X2}"
'            Dim topHex = String.Join(",", topColors.Select(Function(c)
'                            Dim parts = c.Split(","c)
'                            Return $"#{Integer.Parse(parts(0)):X2}{Integer.Parse(parts(1)):X2}{Integer.Parse(parts(2)):X2}"
'                        End Function))

'            Return (bgHex, fgHex, topHex)
'        End Using
'    Catch ex As Exception
'        _AzureExceptionMsg &= "Błąd analizy kolorów: " & ex.Message & vbCrLf
'        Return ("#000000", "#000000", "")
'    End Try
'End Function

'    Imports System.Drawing
'Imports System.Drawing.Imaging

'Public Function IsBlackAndWhiteFast(jpgStream As Stream) As Boolean
'    Try
'        Using bmp As New Bitmap(jpgStream)
'            Dim w = bmp.Width
'            Dim h = bmp.Height
'            Dim sampleStep = Math.Max(1, Math.Min(w, h) \ 100) ' adaptacyjne próbkowanie

'            Dim threshold = 10 ' maksymalna różnica między kanałami RGB
'            Dim bwCount = 0
'            Dim totalCount = 0

'            For y = 0 To h - 1 Step sampleStep
'                For x = 0 To w - 1 Step sampleStep
'                    Dim pixel = bmp.GetPixel(x, y)
'                    Dim r = pixel.R
'                    Dim g = pixel.G
'                    Dim b = pixel.B

'                    If Math.Abs(r - g) < threshold AndAlso Math.Abs(g - b) < threshold AndAlso Math.Abs(b - r) < threshold Then
'                        bwCount += 1
'                    End If
'                    totalCount += 1
'                Next
'            Next

'            Dim ratio = bwCount / totalCount
'            Return ratio > 0.95 ' np. 95% próbek spełnia warunek BW
'        End Using
'    Catch ex As Exception
'        ' Obsługa błędów (np. niepoprawny format pliku)
'        Return False
'    End Try
'End Function



'End Class




'VisualFeatureType	Content Understanding – odpowiednik	Status wsparcia	Uwagi techniczne
'Categories	classification lub caption	✅ Wspierane	Kategorie są bardziej opisowe, np. „scena miejska”, „biuro”
'Description	caption	✅ Wspierane	Generatywny opis obrazu, często z groundingiem
'Faces	faceDetection	✅ Wspierane	Zwraca bounding boxy i liczbę twarzy; brak atrybutów demograficznych
'ImageType	brak dedykowanego zadania	⚠️ Częściowe	Można wywnioskować z caption/classification, ale brak jawnego pola isBWImg
'Tags	objectDetection + caption	✅ Wspierane	Zwraca etykiety obiektów i scen; brak jawnej listy tagów
'Adult	contentModeration	✅ Wspierane	Wykrywa treści nieodpowiednie (adult, racy) z poziomem pewności
'Color	brak dedykowanego zadania	❌ Niewspierane	Nie zwraca kolorów dominujących; wymaga lokalnej analizy (np. OpenCV)
'Brands	objectDetection	⚠️ Częściowe	Rozpoznaje niektóre logotypy, ale nie zawsze zwraca nazwę marki
'Objects	objectDetection	✅ Wspierane	Zwraca obiekty z bounding boxami i confidence score


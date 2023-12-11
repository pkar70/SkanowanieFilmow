Imports System.Drawing
Imports System.IO
Imports Spire.Presentation
Imports Spire.Presentation.Drawing

' freespire ma tylko 10 slides! a wersja p³atna to $800...
' https://www.e-iceblue.com/Introduce/presentation-for-net-introduce.html


Public Class Publish_PowerPoint
    Inherits Vblib.CloudPublish

    Public Const PROVIDERNAME As String = "PowerPoint"

    Public Overrides Property sProvider As String = PROVIDERNAME

    Public Shared DefaultConfig As Vblib.CloudConfig = New Vblib.CloudConfig With
    {
        .eTyp = Vblib.CloudTyp.archiwum.publish,
    .sProvider = PROVIDERNAME,
    .nazwa = "Default PPS provider",
    .enabled = True,
.includeMask = "*.*",
    .defaultExif = New Vblib.ExifTag(Vblib.ExifSource.CloudPublish)
    }


    Public Overrides Async Function SendFilesMain(oPicki As List(Of Vblib.OnePic), oNextPic As Vblib.JedenWiecejPlik) As Task(Of String)

        If String.IsNullOrEmpty(sZmienneZnaczenie) Then Return "ERROR: Publish_PPS, target PPS file is not set"

        ' jesteœmy po pipeline, które jest "piêtro wy¿ej"

        ' https://www.e-iceblue.com/Tutorials/NET/Spire.Presentation/Program-Guide/Conversion/C-/VB.NET-Convert-Images-PNG-JPG-BMP-etc.-to-PowerPoint.html

        Dim oPPS As New Spire.Presentation.Presentation
        Dim iPicCnt As Integer = 0

        For Each oPic As Vblib.OnePic In oPicki

            If iPicCnt = 0 Then
                oPPS.SlideSize.Type = Spire.Presentation.SlideSizeType.Screen4x3
                oPPS.Slides.RemoveAt(0) ' Remove the default slide
            End If

            Dim slide As Spire.Presentation.ISlide = oPPS.Slides.Append

            oPic._PipelineOutput.Seek(0, SeekOrigin.Begin)
            Dim picekBmp = System.Drawing.Bitmap.FromStream(oPic._PipelineOutput)

            Vblib.DumpMessage($"Mam zmiescic obrazek {picekBmp.Width}×{picekBmp.Height} na planszy {oPPS.SlideSize.Size.Width}×{oPPS.SlideSize.Size.Height}")

            Dim skalaX As Double = oPPS.SlideSize.Size.Width / picekBmp.Width
            Dim skalaY As Double = oPPS.SlideSize.Size.Height / picekBmp.Height
            Dim skalowanie As Double = Math.Min(skalaX, skalaY)
            Vblib.DumpMessage($"U¿ywam skalowania {skalaX}, {skalaY} => {skalowanie}")

            'Append it to the image collection 
            Dim imageData = oPPS.Images.Append(picekBmp)

            Dim picFill As PictureFillFormat

#If Not PPT_AS_SHAPE Then

            Dim myWidth As Double = picekBmp.Width * skalowanie
            Dim myHeight As Double = picekBmp.Height * skalowanie
            Vblib.DumpMessage($"skalujemy do rozmiaru {myWidth}×{myHeight}")
            Dim startPoint As New PointF((oPPS.SlideSize.Size.Width - myWidth) / 2, (oPPS.SlideSize.Size.Height - myHeight) / 2)
            Dim endPoint As New PointF(oPPS.SlideSize.Size.Width - startPoint.X, oPPS.SlideSize.Size.Height - startPoint.Y)

            Vblib.DumpMessage($"chce zawrzec okno w {startPoint.X}..{endPoint.X}, {startPoint.Y}..{endPoint.Y}")

            Dim szejp = slide.Shapes.AppendShape(Spire.Presentation.ShapeType.Rectangle, startPoint, endPoint)
            'Fill the shape with image
            szejp.Line.FillType = FillFormatType.None
            szejp.Fill.FillType = FillFormatType.Picture
            picFill = szejp.Fill.PictureFill
#Else

            '//Set the image as the background image of the slide
            slide.SlideBackground.Type = BackgroundType.Custom
            slide.SlideBackground.Fill.FillType = FillFormatType.Picture
            picFill = slide.SlideBackground.Fill.PictureFill
#End If
            picFill.FillType = PictureFillType.Stretch ' stretch: rozci¹ga i psuje proporcje; tile: powiela tak by by³o ca³e zajête
            picFill.ScaleX = skalowanie
            picFill.ScaleY = skalowanie
            picFill.Picture.EmbedImage = imageData

            slide.SlideShowTransition.AdvanceAfterTime = 5000   ' ms
            ' slide.SlideShowTransition.Duration = 6000
            slide.SlideShowTransition.SelectedAdvanceAfterTime = True
            slide.SlideShowTransition.Type = Transition.TransitionType.Random

            If oNextPic IsNot Nothing Then oNextPic()   ' zmiana progressBara

            iPicCnt += 1
            If iPicCnt > 9 Then
                ' bo ograniczenie jest do 10 slajdów - rozdzielamy na kolejne pliki
                oPPS.SaveToFile(sZmienneZnaczenie, Spire.Presentation.FileFormat.PPS)
                ' *TODO* bardziej inteligentne nazewnictwo plików
                sZmienneZnaczenie = sZmienneZnaczenie.Replace(".pps", ".1.pps")
                iPicCnt = 0
            End If

        Next

        ' oraz resetujemy pointery
        For Each oPic As Vblib.OnePic In oPicki
            oPic._PipelineOutput.Seek(0, SeekOrigin.Begin)
        Next

        Return ""
        ' w innych Publish: uzupelnij info w oPic o publishingu
        ' oPic.AddCloudPublished(konfiguracja.nazwa, "")

    End Function

    Public Overrides Function CreateNew(oConfig As Vblib.CloudConfig, oPostProcs() As Vblib.PostProcBase, sDataDir As String) As Vblib.AnyStorage
        If oConfig.sProvider <> sProvider Then Return Nothing
        Dim oNew As New Publish_PowerPoint
        oNew.konfiguracja = oConfig
        oNew._PostProcs = oPostProcs

        Return oNew
    End Function


    Public Overrides Async Function SendFileMain(oPic As Vblib.OnePic) As Task(Of String)

        Dim lista As New List(Of Vblib.OnePic)
        lista.Add(oPic)
        Return Await SendFilesMain(lista, Nothing)

    End Function

#Disable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
    Public Overrides Async Function GetMBfreeSpace() As Task(Of Integer)
        ' *TODO* jak w localstorage
        Throw New NotImplementedException()
    End Function

#Enable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously

    ' po dodaniu parametru - listy procesorów, mo¿e byæ w OnePic

#Region "bez znaczenia dla Publish typu Ad Hoc"
#Disable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
    Public Overrides Async Function VerifyFileExist(oPic As Vblib.OnePic) As Task(Of String)
        Return "Should not be run for PPSs"
    End Function

    Public Overrides Async Function VerifyFile(oPic As Vblib.OnePic, oCopyFromArchive As Vblib.LocalStorage) As Task(Of String)
        Return "Should not be run for PPSs"
    End Function
    Public Overrides Async Function GetFile(oPic As Vblib.OnePic) As Task(Of String)
        Return "Should not be run for PPSs"
    End Function

    Public Overrides Async Function Login() As Task(Of String)
        Return ""
    End Function

    Protected Overrides Async Function GetRemoteTagsMain(oPic As Vblib.OnePic) As Task(Of String)
        Return "Should not be run for PPSs"
    End Function

    Public Overrides Async Function Delete(oPic As Vblib.OnePic) As Task(Of String)
        Return "Should not be run for PPSs"
    End Function

    Public Overrides Async Function GetShareLink(oPic As Vblib.OnePic) As Task(Of String)
        Return "Should not be run for PPSs"
    End Function

    Public Overrides Async Function GetShareLink(oOneDir As Vblib.OneDir) As Task(Of String)
        Return "Should not be run for PPSs"
    End Function

    Public Overrides Async Function Logout() As Task(Of String)
        Return ""
    End Function




#Enable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously

#End Region
End Class


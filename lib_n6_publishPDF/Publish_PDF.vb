Imports System.IO
Imports System.IO.Compression
Imports PdfSharp.Drawing
Imports PdfSharp.Pdf


Public Class Publish_PDF
    Inherits Vblib.CloudPublish

    Public Const PROVIDERNAME As String = "PDF"

    Public Overrides Property sProvider As String = PROVIDERNAME

    Public Shared DefaultConfig As Vblib.CloudConfig = New Vblib.CloudConfig With
    {
        .eTyp = Vblib.CloudTyp.publish,
    .sProvider = PROVIDERNAME,
    .nazwa = "Default PDF provider",
    .enabled = True,
.includeMask = "*.*",
    .defaultExif = New Vblib.ExifTag(Vblib.ExifSource.CloudPublish)
    }


    Public Overrides Function CreateNew(oConfig As Vblib.CloudConfig, oPostProcs As Vblib.PostProcBase(), sDataName As String) As Vblib.AnyStorage
        If oConfig.sProvider <> sProvider Then Return Nothing

        Dim oNew As New Publish_PDF
        oNew.konfiguracja = oConfig
        oNew._PostProcs = oPostProcs

        Return oNew
    End Function


    Public Overrides Async Function SendFileMain(oPic As Vblib.OnePic) As Task(Of String)
        Return Await SendFilesMain(New List(Of Vblib.OnePic) From {oPic}, Nothing)
    End Function

    Public Overrides Async Function SendFilesMain(oPicki As List(Of Vblib.OnePic), oNextPic As Vblib.JedenWiecejPlik) As Task(Of String)

        If String.IsNullOrEmpty(sZmienneZnaczenie) Then Return "ERROR: Publish_PDF, PDF path is not set!"

        ' jesteœmy po pipeline, które jest "piêtro wy¿ej"



        Dim oPdf As New PdfDocument

        For Each oPic As Vblib.OnePic In oPicki

            Dim oPage As PdfPage = oPdf.AddPage
            oPage.Orientation = PdfSharp.PageOrientation.Portrait

            Dim oExif As Vblib.ExifTag = oPic.GetExifOfType(Vblib.ExifSource.FileExif)
            If oExif IsNot Nothing Then
                If oExif.Orientation = Vblib.OrientationEnum.topLeft OrElse oExif.Orientation = Vblib.OrientationEnum.bottomRight Then
                    oPage.Orientation = PdfSharp.PageOrientation.Landscape
                End If
            End If

            Dim gfx As XGraphics = XGraphics.FromPdfPage(oPage, XPageDirection.Downwards)

            ' https://stackoverflow.com/questions/18854935/overlay-image-onto-pdf-using-pdfsharp
            Dim image As XImage = XImage.FromFile(oPic.InBufferPathName)

            ' page: 595×842 pt
            ' pic np. 290×412

            Dim x, width, height As Double ' w pointach
            If oPage.Width.Point > image.PointWidth Then
                x = (oPage.Width.Point - image.PointWidth) / 2
                width = image.PointWidth
                height = image.PointHeight
            Else
                x = 0
                width = oPage.Width.Point
                height = image.PointHeight * (oPage.Width.Point / image.PointWidth)
            End If

            ' 150, 10, 290, 412
            'gfx.DrawImage(image,
            '              New XRect(x, 10, width, height),
            '              New XRect(0, 0, image.PointWidth, image.PointHeight), XGraphicsUnit.Point)

            ' 150, 10, 290, 412
            gfx.DrawImage(image, New XPoint(10, 10))

            ' opisy
            gfx.DrawString(oPic.FormattedSerNo,
                New XFont("Verdana", 12, XFontStyleEx.Bold), XBrushes.Black,
                New XRect(0, height + 10 + 10, oPage.Width.Point, oPage.Height.Point - height - 10 - 10),
                XStringFormats.TopLeft)



            gfx.DrawString(oPic.GetDescriptionForCloud,
                New XFont("Verdana", 12), XBrushes.Black,
                New XRect(0, height + 10 + 20, oPage.Width.Point, oPage.Height.Point - height - 10 - 20),
                XStringFormats.TopLeft)


            If oNextPic IsNot Nothing Then oNextPic()   ' zmiana progressBara
        Next

        oPdf.Save(sZmienneZnaczenie)

        Return ""

    End Function

#Region "bez znaczenia dla Publish typu Ad Hoc"

#Disable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously
    Public Overrides Async Function GetMBfreeSpace() As Task(Of Integer)
        ' *TODO* jak w localstorage
        Throw New NotImplementedException()
    End Function

    Public Overrides Async Function VerifyFileExist(oPic As Vblib.OnePic) As Task(Of String)
        Return "Should not be run for PDFs"
    End Function

    Public Overrides Async Function VerifyFile(oPic As Vblib.OnePic, oCopyFromArchive As Vblib.LocalStorage) As Task(Of String)
        Return "Should not be run for PDFs"
    End Function

    Public Overrides Async Function GetFile(oPic As Vblib.OnePic) As Task(Of String)
        Return "Should not be run for PDFs"
    End Function

    Public Overrides Async Function Login() As Task(Of String)
        Return ""
    End Function

    Protected Overrides Async Function GetRemoteTagsMain(oPic As Vblib.OnePic) As Task(Of String)
        Return "Should not be run for PDFs"
    End Function

    Public Overrides Async Function Delete(oPic As Vblib.OnePic) As Task(Of String)
        Return "Should not be run for PDFs"
    End Function

    Public Overrides Async Function GetShareLink(oPic As Vblib.OnePic) As Task(Of String)
        Return "Should not be run for PDFs"
    End Function

    Public Overrides Async Function GetShareLink(oOneDir As Vblib.OneDir) As Task(Of String)
        Return "Should not be run for PDFs"
    End Function

    Public Overrides Async Function Logout() As Task(Of String)
        Return ""
    End Function


#Enable Warning BC42356 ' This async method lacks 'Await' operators and so will run synchronously

#End Region

End Class

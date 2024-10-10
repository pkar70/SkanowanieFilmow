Imports QRCoder

Public Class QRgen

    Public Shared Function GenerateQR(tekst As String, bokPikseli As Integer) As Drawing.Bitmap

        Dim oCodeGen As New QRCodeGenerator
        Dim oCodeData As QRCodeData = oCodeGen.CreateQrCode(tekst, QRCodeGenerator.ECCLevel.M)
        Dim oCode As New QRCode(oCodeData)

        Dim modulow As Integer = oCodeData.ModuleMatrix.Count ' bok liczony w modu³ach

        Dim reqSize As Integer = Math.Max(bokPikseli / modulow, 3)

        Return oCode.GetGraphic(reqSize)

    End Function

End Class


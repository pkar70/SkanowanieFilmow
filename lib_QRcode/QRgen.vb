
Public Class QRgen

    Public Function GenerateQR(tekst As String, bokPikseli As Integer) As 

        Dim oBarcode As ZXing.BarcodeWriter
        oBarcode.Format = ZXing.BarcodeFormat.QR_CODE
        ' ograniczenie na 300 dobre dla kodów paskowych
        oBarcode.Options = New ZXing.Common.EncodingOptions With
                {
                    .Height = bokPikseli,
                    .Width = bokPikseli
                    }

        Dim oRes = oBarcode.Write(tekst)
        If oRes Is Nothing Then Return Nothing

        Return oRes
    End Function

End Class

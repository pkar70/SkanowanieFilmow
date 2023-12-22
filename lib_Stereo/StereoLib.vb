

' operacje stereoskopowe

Imports System.Collections.Specialized
Imports System.Runtime.CompilerServices

Public Class StereoLib

    ' zrób anaglyph z right/left
    ' zrób anaglyph z JPS

    Public Shared Function AnaglyphFromLRpic(leftPic As Drawing.Bitmap, rightPic As Drawing.Bitmap) As Drawing.Bitmap

        If Math.Abs(leftPic.Width - rightPic.Width) > 50 Then Return Nothing
        If Math.Abs(leftPic.Height - rightPic.Height) > 50 Then Return Nothing

        Dim grayLeft = MakeGrayscale(leftPic)
        Dim grayRight = MakeGrayscale(rightPic)

        Dim anaPic As New Drawing.Bitmap(leftPic.Width, leftPic.Height)

        For iRow As Integer = 0 To leftPic.Height - 1
            For iCol As Integer = 0 To leftPic.Width - 1

                Dim pikselL = leftPic.GetPixel(iCol, iRow).R
                Dim pikselR = rightPic.GetPixel(iCol, iRow).R

                anaPic.SetPixel(iCol, iRow, Drawing.Color.FromArgb(pikselL, pikselR, pikselR))
            Next
        Next


        Return anaPic

        'Dim pixData = piksy.LockBits
        ' https://www.codeproject.com/articles/617613/fast-pixel-operations-in-net-with-and-without-unsa

    End Function

    Public Shared Function AnaglyphFromSBS(inputPic As Drawing.Bitmap) As Drawing.Bitmap

        Dim half As Integer = inputPic.Width / 2
        Dim anaPic As New Drawing.Bitmap(half, inputPic.Height)

        For iRow As Integer = 0 To inputPic.Height - 1
            For iCol As Integer = 0 To half - 1

                Dim pikselL = inputPic.GetPixel(iCol, iRow).GetGrayscale
                Dim pikselR = inputPic.GetPixel(half + iCol, iRow).GetGrayscale

                anaPic.SetPixel(iCol, iRow, Drawing.Color.FromArgb(pikselL, pikselR, pikselR))
            Next
        Next


        Return anaPic

        'Dim pixData = piksy.LockBits
        ' https://www.codeproject.com/articles/617613/fast-pixel-operations-in-net-with-and-without-unsa

    End Function




    Private Shared Function MakeGrayscale(obrazek As Drawing.Bitmap) As Drawing.Bitmap

        Dim newPic As New Drawing.Bitmap(obrazek.Width, obrazek.Height)

        For iRow As Integer = 0 To obrazek.Height - 1
            For iCol As Integer = 0 To obrazek.Width - 1

                Dim pikselL = obrazek.GetPixel(iCol, iRow)
                newPic.SetPixel(iCol, iRow, pikselL.ToGrayscale)
            Next
        Next

        Return newPic
        'Return obrazek.Clone(New Drawing.Rectangle(0, 0, obrazek.Width, obrazek.Height), Drawing.Imaging.PixelFormat.Format16bppGrayScale)
    End Function

End Class


Public Module extensions

    <Extension>
    Public Function ToGrayscale(piksel As Drawing.Color) As Drawing.Color
        Dim szarak As Integer = piksel.GetGrayscale
        Return Drawing.Color.FromArgb(szarak, szarak, szarak)
    End Function

    <Extension>
    Public Function GetGrayscale(piksel As Drawing.Color) As Integer
        Return piksel.R * 0.3 + piksel.G * 0.59 + piksel.B * 0.11
    End Function
End Module
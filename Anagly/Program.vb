Imports System

Module Program
    Sub Main(args As String())

        'If args.Count < 2 Then
        '    Console.Error.WriteLine("Bad number of paramsy")
        '    Return
        'End If

        Dim leftName As String = args(0)

        If Not IO.File.Exists(leftName) Then
            Console.Error.WriteLine("Not exists file " & leftName)
            Return
        End If

        Dim rightName As String = Nothing

        Dim iParOut As Integer = 1

        If IO.Path.GetExtension(leftName).ToLowerInvariant <> ".jps" Then
            rightName = args(1)

            If Not IO.File.Exists(rightName) Then
                Console.Error.WriteLine("Not exists file " & rightName)
                Return
            End If
            iParOut = 2
        End If

        Dim anaglName As String = args(iParOut)

        If IO.File.Exists(anaglName) Then
            Console.Error.WriteLine("But output file exists: " & anaglName)
            Return
        End If

        Dim leftBitmap As New Drawing.Bitmap(leftName)
        Dim anaBitmap As Drawing.Bitmap

        If String.IsNullOrEmpty(rightName) Then
            Console.WriteLine($"Converting SBS file {leftName} to anaglyph {anaglName}")
            anaBitmap = lib_Stereo.StereoLib.AnaglyphFromSBS(leftBitmap)
        Else
            Console.WriteLine($"Converting files {leftName} and {rightName} to anaglyph {anaglName}")
            Dim rightBitmap As New Drawing.Bitmap(rightName)
            anaBitmap = lib_Stereo.StereoLib.AnaglyphFromLRpic(leftBitmap, rightBitmap)
        End If

        anaBitmap.Save(anaglName, Drawing.Imaging.ImageFormat.Jpeg)

        Console.WriteLine("Chiba jest")

    End Sub
End Module

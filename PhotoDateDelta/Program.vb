Imports System
Imports System.IO
Imports System.Net.NetworkInformation

Module Program
    Sub Main(args As String())

        ' 1) skan command line
        Dim oDateDiff As TimeSpan = ScanCmdLine(args)

        ' 2) wczytanie listy istniej¹cych plikow
        Dim filesy As List(Of jedenFile) = ReadDirectory(".")
        If filesy.Count < 1 Then Return

        ' 3) rename ka¿dego z nich do listy
        If TryToRename(filesy, oDateDiff) Then
            ' jeœli pliki maj¹ nazwy do zmiany (np nie zmieniamy IMG)

            ' 4) sprawdzenie czy kazda src name jest rozna od kazda dst name
            If RenamingGivesDublets(filesy) Then Return

        End If

        DoRename(filesy)
        FixJFIF(filesy, oDateDiff)

    End Sub


    ' zamieñ command line na offset daty (o ile trzeba przesun¹æ)
    Private Function ScanCmdLine(args() As String) As TimeSpan
        Console.WriteLine("Na razie zmiana daty jest robiona na sztywno - wkompiluj dane jakby co")
        Return New TimeSpan(2, -1, -12, 0) ' D H M S
    End Function

    Private Function ReadDirectory(srcDir As String) As List(Of jedenFile)

        Dim oDir As New List(Of jedenFile)

        For Each sFile As String In IO.Directory.EnumerateFiles(srcDir, "*.jpg")
            oDir.Add(New jedenFile(sFile))
        Next

        If oDir.Count < 1 Then Console.Error.WriteLine("No files in directory")

        Return oDir

    End Function

    Private Function TryToRename(filesy As List(Of jedenFile), oDateDiff As TimeSpan) As Boolean

        Dim bWas As Boolean = False

        For Each oFile As jedenFile In filesy

            Dim oldname As String = IO.Path.GetFileName(oFile.oldName)

            If Not oldname.StartsWith("WP_2") Then Continue For

            ' ok, to jest nazwa pliku do zmiany
            Dim dFileDate As Date = GetDateFromFilename(oldname)

            ' WP_20230612_19_48_15
            ' 012345678901234567890

            oFile.newName = IO.Path.Combine(
                                IO.Path.GetDirectoryName(oFile.oldName),
                                "WP_" & (dFileDate + oDateDiff).ToString("yyyyMMdd_HH_mm_ss") & oldname.Substring(20))

            bWas = True


        Next

        Return bWas

    End Function

    Private Function GetDateFromFilename(oldname As String) As Date

        If Not oldname.StartsWith("WP_2") Then
            Throw New ArgumentException("Takich plikow jeszcze nie umiem - ale tez test ju¿ by³ raz zrobiony")
        End If

        ' 0123
        ' WP_20230612_19_48_15
        '    12345678901234567890

        Dim sDate As String = oldname.Substring(3, 17)

        Return Date.ParseExact(sDate, "yyyyMMdd_HH_mm_ss", Nothing)

    End Function

    Private Function RenamingGivesDublets(filesy As List(Of jedenFile)) As Boolean

        Dim bErr As Boolean = False

        For Each oDst As jedenFile In filesy
            For Each oSrc As jedenFile In filesy

                If oSrc.oldName = oDst.newName Then
                    Console.Error.WriteLine($"Colision in rename {IO.Path.GetFileName(oSrc.oldName)} to {IO.Path.GetFileName(oDst.newName)}")
                    bErr = True
                End If

            Next
        Next

        Return bErr
    End Function

    Private Sub DoRename(filesy As List(Of jedenFile))
        For Each oFile As jedenFile In filesy
            If String.IsNullOrWhiteSpace(oFile.newName) Then Continue For
            IO.File.Move(oFile.oldName, oFile.newName)
            Console.WriteLine($"{IO.Path.GetFileName(oFile.oldName)} »» {IO.Path.GetFileName(oFile.newName)} ")
        Next
    End Sub


    Private Sub FixJFIF(filesy As List(Of jedenFile), oDateDiff As TimeSpan)

        For Each oFile As jedenFile In filesy

            Dim srcfile As String = oFile.newName
            If String.IsNullOrWhiteSpace(srcfile) Then srcfile = oFile.oldName

            ' rename to BAK
            Dim bakfile As String = IO.Path.ChangeExtension(srcfile, "bak")
            IO.File.Copy(srcfile, bakfile)

            ' odczytaj JFIF z pliku
            Dim oExifLib As New CompactExifLib.ExifData(bakfile)

            ' bierzemy datê - ale jak siê nie uda, to ignorujemy ten plik
            Dim dataFotki As Date
            If Not oExifLib.GetDateTaken(dataFotki) Then
                Console.Error.WriteLine($"Cannot read DateTaken from {IO.Path.GetFileName(srcfile)}")
                IO.File.Delete(bakfile)
                Continue For
            End If

            dataFotki += oDateDiff
            oExifLib.SetDateTaken(dataFotki)

            Console.WriteLine($"Corrected DateTaken in {IO.Path.GetFileName(srcfile)}")

            oExifLib.Save(srcfile)

        Next
    End Sub

End Module

Public Class jedenFile
    Public Property oldName As String
    Public Property newName As String

    Public Sub New(filename As String)
        oldName = filename
    End Sub

End Class
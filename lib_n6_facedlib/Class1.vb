
Imports System.Drawing
Imports DlibDotNet



Public Class Class1

    ' ale trzeba mieæ modele zrobione, a to mo¿e byæ trudne :)
    ' https://github.com/takuya-takeuchi/FaceRecognitionDotNet


    ' dla p³ci: https://susanqq.github.io/UTKFace/ 1.3 GB do trenowania , nie da sie sciagnac automatem (gogiel drive, 3 fragmenty)
    ' dla emocji: https://www.kaggle.com/datasets/sudarshanvaidya/corrective-reannotation-of-fer-ck-kdef
    ' dla wieku: https://talhassner.github.io/home/projects/Adience/Adience-data.html
    ' Age classes are (0, 2), (4, 6), (8, 13), (15, 20), (25, 32), (38, 43), (48, 53) and (60, 100).
    ' dopiero te pliki pozwoli³yby na skorzystanie

    ' dok³adnoœci: p³eæ 90 %, wiek 76 %, emocje 68 %


    Public Sub ala()

        Dim dirmodelfiles As String = "C:\temp\modelfiles"

        Dim enc As FaceRecognitionDotNet.FaceRecognition = FaceRecognitionDotNet.FaceRecognition.Create(dirmodelfiles)

        'Dim obrazek As FaceRecognitionDotNet.Image =  FaceRecognitionDotNet.FaceRecognition.LoadImage(bitmap)
        Dim obrazek As FaceRecognitionDotNet.Image = FaceRecognitionDotNet.FaceRecognition.LoadImageFile("c:\temp\testplik")

        Dim lokalcje = enc.FaceLocations(obrazek)

        For Each lokacja In lokalcje

            Dim gend = enc.PredictGender(obrazek, lokacja)
            If gend = FaceRecognitionDotNet.Gender.Female Then
            End If

            Dim gendList = enc.PredictProbabilityGender(obrazek, lokacja)

            Dim emocja = enc.PredictEmotion(obrazek, lokacja)
            If emocja = "aka" Then

            End If
            Dim emocjaL = enc.PredictProbabilityEmotion(obrazek, lokacja)

            Dim age = enc.PredictAge(obrazek, lokacja)
            If age = 10 Then

            End If

            Dim ageL = enc.PredictProbabilityAge(obrazek, lokacja)

        Next

    End Sub





End Class

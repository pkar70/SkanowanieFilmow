Public Interface AutotaggerInterface
    Property Typek As AutoTaggerType    ' lokalny, web, web z autoryzacją
    Property Nazwa As String
    Property MinWinVersion As String
    Function ApplyToFile(oFile As OnePic) As Boolean

End Interface


' https://learn.microsoft.com/en-us/azure/cognitive-services/computer-vision/quickstarts-sdk/image-analysis-client-library?tabs=visual-studio%2C3-2&pivots=programming-language-csharp
'grass 0.99575436115264893
'dog 0.99391579627990723
'mammal 0.9928356409072876
'animal 0.99180018901824951
'dog breed 0.9890419244766235
'pet 0.974603533744812
'outdoor 0.969241738319397
'companion dog 0.906731367111206
'small greek domestic dog 0.8965123891830444
'golden retriever 0.8877675533294678
'labrador retriever 0.8746421337127686
'puppy 0.872604250907898
'ancient dog breeds 0.8508287668228149
'field 0.80177485942840576
'retriever 0.6837497353553772
'brown 0.6581960916519165
' czyli definiowany próg pewności? albo wszystko, i dopiero podczas opisywania slider ile ma być
' https://learn.microsoft.com/en-us/dotnet/api/microsoft.azure.cognitiveservices.vision.computervision.computervisionclient?view=azure-dotnet
' https://learn.microsoft.com/en-us/dotnet/api/microsoft.azure.cognitiveservices.vision.computervision.computervisionclientextensions.describeimageasync?view=azure-dotnet
' https://learn.microsoft.com/en-us/dotnet/api/microsoft.azure.cognitiveservices.vision.computervision.models.visualfeaturetypes?view=azure-dotnet
' https://azurelessons.com/extract-text-from-image-azure-cognitive-services/#:~:text=The%20Computer%20Vision%20API%20of%20Azure%20includes%20the,values%20that%20you%20got%20it%20from%20the%20image.


' https://cloud.google.com/vision?&gclsrc=3p.ds&
' https://cloud.google.com/vision/pricing - 1000 każdego detection miesięcznie darmowe
' https://cloud.google.com/vision/docs/detecting-logos
' https://cloud.google.com/vision/docs/detecting-landmarks 
' https://cloud.google.com/vision/docs/labels - czyli psy itp.
' https://cloud.google.com/vision/docs/detecting-faces - emocje rozpoznaje ponoć
' FILE_EXIF

'	reguły rename? np. dir\file z karty SD na dir_file?, DSCF1234 na yymmddhhmmss? dla latwiejszego uniqID 
' Descript.Ion 

'*) AUTOTAG (np. AzureAPI, cloudpic do katalogu temp)
'	typ/ name
'	username/ password
'	internaldata(np.tempalbum path)
'usagelimit
'IMPLEMENT: vblib, JSON na config
'	moze takze OCR na obrazku? moze jakies napisy same sie w ten sposób przepiszą? lokalny OCR (jak w comixlang)

'*) TRAINFACE - albo to w ramach TAGS?
'	name
'listof(piclink)

'rozpoznawanie twarzy Azure: 20 per minute, 30k trans per month free
'grupa(myfriends), milion osob (Anna), 248 twarzy
'"Computer vision Image Analysis 4.0": The New API includes image captioning, image tagging, Object detection people detection, And Read OCR functionality, available in the same Analyze Image operation. 
'https://learn.microsoft.com/en-us/azure/cognitive-services/computer-vision/quickstarts-sdk/identity-client-library?pivots=programming-language-csharp?pivots=programming-language-csharp

'Do unicode decore mozna dodac link
'https://util.unicode.org/UnicodeJsps/character.jsp?a=FE0F
'czyli pelne dane jednego znaku

'szczegolnie rozpoznawanie twarzy i autotagowanie powinno byc dostepne takze pozniej

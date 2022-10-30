Public Class Autotagger

End Class


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

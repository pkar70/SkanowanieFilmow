
' Instagram - NUGET
' Facebook - NUGET?
' adhoc (DIR)

' Publish, czyli po stronie serwisu są jakieś zmiany

Public MustInherit Class CloudPublish
    Implements AnyStorage, AnyCloudStorage

    Public Property konfiguracja As CloudConfig

    Public Property _PostProcs As PostProcBase()

    ' te dwa muszą się zgadzać z tym co w konfiguracja
    Public MustOverride Property sProvider As String
    Public Property eTyp As CloudTyp = CloudTyp.publish

    ''' <summary>
    ''' znaczenie zmienne, w zależności od konkretnego Providera; dla AdHoc: katalog docelowy
    ''' </summary>
    ''' <returns></returns>
    Public Property sZmienneZnaczenie As String

    Public MustOverride Async Function SendFileMain(oPic As OnePic) As Task(Of String) Implements AnyStorage.SendFileMain
    Public MustOverride Async Function VerifyFileExist(oPic As OnePic) As Task(Of String) Implements AnyStorage.VerifyFileExist
    Public MustOverride Async Function VerifyFile(oPic As OnePic, oCopyFromArchive As LocalStorage) As Task(Of String) Implements AnyStorage.VerifyFile
    Public MustOverride Async Function SendFiles(oPicki As List(Of OnePic)) As Task(Of String) Implements AnyStorage.SendFiles
    Public MustOverride Async Function GetFile(oPic As OnePic) As Task(Of String) Implements AnyStorage.GetFile
    Public MustOverride Async Function GetMBfreeSpace() As Task(Of Integer) Implements AnyStorage.GetMBfreeSpace

    Public MustOverride Async Function Login() As Task(Of String) Implements AnyCloudStorage.Login
    Protected MustOverride Async Function GetRemoteTagsMain(oPic As OnePic) As Task(Of String)
    Public MustOverride Async Function Delete(oPic As OnePic) As Task(Of String) Implements AnyCloudStorage.Delete
    Public MustOverride Async Function GetShareLink(oPic As OnePic) As Task(Of String) Implements AnyCloudStorage.GetShareLink
    Public MustOverride Async Function GetShareLink(oOneDir As OneDir) As Task(Of String) Implements AnyCloudStorage.GetShareLink
    Public MustOverride Async Function Logout() As Task(Of String) Implements AnyCloudStorage.Logout
    Public MustOverride Function CreateNew(oConfig As CloudConfig, oPostProcs As PostProcBase(), sDataDir As String) As AnyStorage Implements AnyCloudStorage.CreateNew


    Public Async Function SendFile(oPic As OnePic) As Task(Of String)
        DumpCurrMethod()
        ' sprawdź maski
        If Not oPic.MatchesMasks(konfiguracja.includeMask, konfiguracja.excludeMask) Then Return ""

        ' przeslij plik przez pipeline
        Dim sRet As String = Await oPic.RunPipeline(konfiguracja.defaultPostprocess, _PostProcs)
        If sRet <> "" Then Return sRet

        Return Await SendFileMain(oPic)
    End Function

    'Protected Function GetRemoteId(oPic As Vblib.OnePic) As String
    '    If oPic.Published Is Nothing Then Return ""

    '    Dim sId As String = ""
    '    If Not oPic.Published.TryGetValue(konfiguracja.nazwa, sId) Then Return ""

    '    Return sId
    'End Function

    Public Async Function GetRemoteTags(oPic As OnePic) As Task(Of String) Implements AnyCloudStorage.GetRemoteTags
        Dim sLink As String = Await GetShareLink(oPic)
        If sLink = "" Then Return "ERROR: nie mam zapisanego ID pliku"

        Return Await GetRemoteTagsMain(oPic)

    End Function
End Class


'App do Shutterfly:
'*) test logowania
'*) tworzenie folderu/albumu
'*) wrzucanie zdjęcia
'*) opisanie tagami z filename/EXIFa

' Serwisy w VBlib:
'* CreateAlbum(path) as oAlbum # stworzenie strukury podkatalogów, zakładam że najnizszy poziom to Album, ktory jest umieszczony w Folder
'* UploadPhoto(oAlbum, oResizedPicture, sTags) as oUploadedPhoto - albo service sam sobie skaluje	# wrzucanie fotek, serwis moze miec ograniczoną rozdzielczosc (np. do MordkoKsiazki daję mniejsze), zwraca mi jakiś ID który pozwala później do tej fotki sie dostac
'* AddToVirtualAlbum(oAlbum?, oUploadedPhoto)	# dodawanie do albumow, np. wedle tagów - Album typu "zdjecia z Wojtkiem", i samo tam trafia jak jest napisane ze Wojtek na zdjeciu jest
'* BrowseDir(path) as List(Of file)	# udaje chodzenie po katalogach, taki Norton Commander :)
'* GetMetadataFor(pathname) As JSON # opis ktory jest po stronie serwisu, ichniejsze rozpoznawanie twarzy , itp rzeczy; moge sobie dopisac do swoich znacznikow
'* GetSharingLink # jakos implementacja tego, historia linków? co bylo udostepnione a co nie
'* ShareBetweenServices # jakoś, z Shutterfly do FaceBook

'Serwisy dla mnie Do dokladniejszego sprawdzenia:
'* facebook - do szkolnych itp.
'* shutterfly - archiwum
'* shutterstock - sprzedażne
'* instagram
'- kto ma prawa, bo jako "zacheta" do sprawdzenia na shutterstock do kupienia mozna
'* ewentualnie jakies ktore robią konkursy po pare tygodniowo, licznik, raz na tydzien pokazuje app listę fotek z tego tygodnia i pozwala wybrac ktore wyslac


'* moze rozpoznawanie twarzy po stronie serwisu, albo inne atrybty nadawane przez serwis, i dodanie tego do EXIF?

'*) CLOUDSTORAGE  
'	typ/ name
'	username/ password
'	include: List(Of String), exclude: List(Of String)(np.tylko JPG)
'IMPLEMENT: vblib, JSON na config
'	UI: config, in_use yes/no
'	FUNKCJONALNOSC: permanent storage, Save, ApplyTags, Download?, GetTags, Dir(path)
'	to właściwie to samo co PublishPlace?. reapplytags y/n, reupload y/n


'*) PUBLISHPLACE  facebook|instagram|... (ale facebook moze byc dwa razy, np face-Public i face-rodzina ?)
'	typ/ name
'	username/ password
'	include: List(Of String), exclude: List(Of String)(np.tylko JPG)
'defaultsize [=1000, To dłuzszy bok ma miec max 1000]
'	defaultscalepercent [=50, To zmniejsza 2×]
'	IMPLEMENT: vblib, JSON na config
'	FUNKCJONALNOSC: permanent storage, Save, ApplyTags, Download?, GetTags, Dir(path) reapplytags y/n, reupload y/n



' może także email
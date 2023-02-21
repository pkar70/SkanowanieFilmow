'Public Class Auto_Meteo_Synop
'    Inherits Vblib.AutotaggerBase

'    Public Overrides ReadOnly Property Typek As Vblib.AutoTaggerType = Vblib.AutoTaggerType.WebPublic
'    Public Overrides ReadOnly Property Nazwa As String = ExifSource.AutoMeteo
'    Public Overrides ReadOnly Property MinWinVersion As String = "7.0"
'    Public Overrides ReadOnly Property DymekAbout As String = "Dane meteo - synop (Polska)"
'    Public Overrides ReadOnly Property includeMask As String = "*.*"

'    Public Overrides Function GetForFile(oFile As OnePic) As Task(Of ExifTag)
'        Throw New NotImplementedException()
'    End Function

'    Private Async Function SciagnijIndeks(uripath As String, filename As String) As Task


'        ' https://danepubliczne.imgw.pl/data/dane_pomiarowo_obserwacyjne/dane_meteorologiczne/
'        ' wykaz_stacji.csv
'    End Function


'    Private Function GetFileForMonth(year As Integer, month As Integer)
'        ' 1951_1955/1951_k.zip w nim k_d_1951.csv oraz k_d_t_1951.csv
'        ' 2001/2001_01_k.zip w nim k_d_01_2001.csv oraz k_d_t_01_2001.csv 
'    End Function

'    Private Function ZnajdzNajblizszy()
'        ' do wczytanego CSV dopisz wspolrzedne dla kodow stacji
'    End Function

'End Class

'Public Class Meteo_Data
'    Public Property Meteo_Klimat
'    Public Property Meteo_Klimat_t
'    Public Property Meteo_Opad
'    Public Property Meteo_Synop
'    Public Property Meteo_Synop_t
'End Class



'Public Class Meteo_Synop
'    Kod stacji                                       9
'Nazwa stacji                                    30
'Rok                                              4
'Miesiąc                                          2
'Dzień                                            2
'Maksymalna temperatura dobowa [°C]               6/1
'Status pomiaru TMAX                              1
'Minimalna temperatura dobowa [°C]                6/1
'Status pomiaru TMIN                              1
'Średnia temperatura dobowa [°C]                  8/1
'Status pomiaru STD                               1
'Temperatura minimalna przy gruncie [°C]          6/1
'Status pomiaru TMNG                              1
'Suma dobowa opadu [mm]                           8/1
'Status pomiaru SMDB                              1
'Rodzaj opadu [S/W/ ]                             1
'Wysokość pokrywy śnieżnej [cm]                   5
'Status pomiaru PKSN                              1
'Równoważnik wodny śniegu  [mm/cm]                6/1
'Status pomiaru RWSN                              1
'Usłonecznienie [godziny]                         6/1
'Status pomiaru USL                               1
'Czas trwania opadu deszczu [godziny]             6/1
'Status pomiaru DESZ                              1
'Czas trwania opadu śniegu [godziny]              6/1
'Status pomiaru SNEG                              1
'Czas trwania opadu deszczu ze śniegiem [godziny] 6/1
'Status pomiaru DISN                              1
'Czas trwania gradu [godziny]                     6/1
'Status pomiaru GRAD                              1
'Czas trwania mgły [godziny]                      6/1
'Status pomiaru MGLA                              1
'Czas trwania zamglenia  [godziny]                6/1
'Status pomiaru ZMGL                              1
'Czas trwania sadzi [godziny]                     6/1
'Status pomiaru SADZ                              1
'Czas trwania gołoledzi [godziny]                 6/1
'Status pomiaru GOLO                              1
'Czas trwania zamieci śnieżnej niskiej [godziny]  6/1
'Status pomiaru ZMNI                              1
'Czas trwania zamieci śnieżnej wysokiej [godziny] 6/1
'Status pomiaru ZMWS                              1
'Czas trwania zmętnienia [godziny]                6/1
'Status pomiaru ZMET                              1
'Czas trwania wiatru >=10m/s [godziny]            6/1
'Status pomiaru FF10                              1
'Czas trwania wiatru >15m/s [godziny]             6/1
'Status pomiaru FF15                              1
'Czas trwania burzy  [godziny]                    6/1
'Status pomiaru BRZA                              1
'Czas trwania rosy  [godziny]                     6/1
'Status pomiaru ROSA                              1
'Czas trwania szronu [godziny]                    6/1
'Status pomiaru SZRO                              1
'Wystąpienie pokrywy śnieżnej  [0/1]              3
'Status pomiaru DZPS                              1
'Wystąpienie błyskawicy  [0/1]                    3
'Status pomiaru DZBL                              1
'Stan gruntu [Z/R]                                1
'Izoterma dolna  [cm]                             5
'Status pomiaru IZD                               1
'Izoterma górna [cm]                              5
'Status pomiaru IZG                               1
'Aktynometria  [J/cm2]                            7
'Status pomiaru AKTN                              1
'End Class

'Public Class Meteo_Synop_t
'Kod stacji                                           9
'Nazwa stacji                                        30
'Rok                                                  4
'Miesiąc                                              2
'Dzień                                                2
'Średnie dobowe zachmurzenie ogólne [oktanty]         6/1
'Status pomiaru NOS                                   1
'Średnia dobowa prędkość wiatru [m/s]                 6/1
'Status pomiaru FWS                                   1
'Średnia dobowa temperatura [°C]                      5/1
'Status pomiaru TEMP                                  1
'Średnia dobowe ciśnienie pary wodnej [hPa]           5/1
'Status pomiaru CPW                                   1
'Średnia dobowa wilgotność względna [%]               8/1
'Status pomiaru WLGS                                  1
'Średnia dobowe ciśnienie na poziomie stacji [hPa]    7/1
'Status pomiaru PPPS                                  1
'Średnie dobowe ciśnienie na pozimie morza [hPa]      7/1
'Status pomiaru PPPM                                  1
'Suma opadu dzień  [mm]                               8/1
'Status pomiaru WODZ                                  1
'Suma opadu noc   [mm]                                8/1
'Status pomiaru WONO                                  1
'End Class
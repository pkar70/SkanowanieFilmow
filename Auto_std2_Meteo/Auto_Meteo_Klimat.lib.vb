'Public Class Auto_Meteo_Klimat
'    Inherits Vblib.AutotaggerBase

'    Public Overrides ReadOnly Property Typek As Vblib.AutoTaggerType = Vblib.AutoTaggerType.WebPublic
'    Public Overrides ReadOnly Property Nazwa As String = ExifSource.AutoMeteo
'    Public Overrides ReadOnly Property MinWinVersion As String = "7.0"
'    Public Overrides ReadOnly Property DymekAbout As String = "Dane meteo - klimat (Polska)"
'    Public Overrides ReadOnly Property includeMask As String = "*.*"

'    Public Overrides Function GetForFile(oFile As OnePic) As Task(Of ExifTag)
'        Throw New NotImplementedException()
'    End Function

'    Private Async Function SciagnijIndeks(uripath As String, filename As String) As Task
'        ' https://danepubliczne.imgw.pl/data/dane_pomiarowo_obserwacyjne/dane_hydrologiczne/
'        ' lista_stacji_hydro.csv

'        ' https://danepubliczne.imgw.pl/data/dane_pomiarowo_obserwacyjne/dane_meteorologiczne/
'        ' wykaz_stacji.csv
'    End Function
'End Class
'Public Class Meteo_Klimat
'    Kod stacji                              9
'Nazwa stacji                           30
'Rok                                     4
'Miesiąc                                 2
'Dzień                                   2
'Maksymalna temperatura dobowa [°C]      6/1
'Status pomiaru TMAX                     1
'Minimalna temperatura dobowa [°C]       6/1
'Status pomiaru TMIN                     1
'Średnia temperatura dobowa [°C]         8/1
'Status pomiaru STD                      1
'Temperatura minimalna przy gruncie [°C] 6/1
'Status pomiaru TMNG                     1
'Suma dobowa opadów [mm]                 8/1
'Status pomiaru SMDB                     1
'Rodzaj opadu  [S/W/ ]                   1
'Wysokość pokrywy śnieżnej [cm]          5
'Status pomiaru PKSN                     1

'End Class

'Public Class Meteo_Klimat_t
'    Kod stacji                                   9
'Nazwa stacji                                30
'Rok                                          4
'Miesiąc                                      2
'Dzień                                        2
'Średnia dobowa temperatura  [°C]             5/1
'Status pomiaru TEMP                          1
'Średnia dobowa wilgotność względna [%]       8/1
'Status pomiaru WLGS                          1
'Średnia dobowa prędkość wiatru [m/s]         6/1
'Status pomiaru FWS                           1
'Średnie dobowe zachmurzenie ogólne [oktanty] 6/1
'Status pomiaru NOS                           1
'End Class



' dla auto_std2_meteo

Public Class Meteo_Opad
    Inherits pkar.BaseStruct

    Public Property SumaDobowaOpadowMM As Double
    Public Property RodzajOpadu As String   ' S/W/ snieg/woda/nieznane
    Public Property WysokPokrywySnieznejCM As Double
    Public Property WysokSwiezoSpadlegoSnieguCM As Double
    Public Property GatunekSniegu As String
    Public Property RodzajPokrywySnieznej As String ' */sl(śl)/pl(pł)/prz(pr) to pokrywa/ślad/płaty/przerywana.
End Class

Public Class Meteo_Klimat
    Inherits pkar.BaseStruct

    'k_d
    Public Property TempMax As Double
    Public Property TempMin As Double
    Public Property TempAvg As Double
    Public Property TempMinGrunt As Double
    Public Property SumaOpadowMM As Double
    Public Property RodzajOpadu As String
    Public Property WysokPokrywySnieznejCM As Integer
    ' k_d_t
    Public Property HigroAvg As Double
    Public Property WindSpeedAvgMS As Double
    Public Property ZachmurzenieOktantyAvg As Double
End Class

Public Class Meteo_Synop
    Inherits pkar.BaseStruct

    ' s_d
    Public Property TempMax As Double
    Public Property TempMin As Double
    Public Property TempAvg As Double
    Public Property TempMinGrunt As Double
    Public Property SumaOpadowMM As Double
    Public Property RodzajOpadu As String
    Public Property WysokPokrywySnieznejCM As Integer
    Public Property RownowaznikWodnySnieguMMCM As Double
    Public Property HrsUslonecznienie As Double
    Public Property HrsDeszcz As Double
    Public Property HrsSnieg As Double
    Public Property HrsDeszczZeSniegiem As Double
    Public Property HrsGrad As Double
    Public Property HrsMgla As Double
    Public Property HrsZamglenie As Double
    Public Property HrsSadz As Double
    Public Property HrsGololedz As Double
    Public Property HrsZamiecNiska As Double
    Public Property HrsZamiecWysoka As Double
    Public Property HrsZmetnienie As Double
    Public Property HrsWiatrOd10MS As Double
    Public Property HrsWiatrOd15MS As Double
    Public Property HrsBurza As Double
    Public Property HrsRosa As Double
    Public Property HrsSzron As Double
    Public Property PokrywaSniezna As Integer
    Public Property Blyskawica As Integer
    Public Property StanGruntu As String
    Public Property IzotermaDolnaCM As Double
    Public Property IzotermaGornaCm As Double
    Public Property AktynometriaJCM2 As Double

    ' s_d_t
    Public Property ZachmurzenieOktantyAvg As Double
    Public Property WindSpeedAvgMS As Double
    Public Property CisnParyWodnejAvgHPa As Double
    Public Property HigroAvg As Double
    Public Property CisnStacjaAvgHPa As Double
    Public Property CisnMorzeAvgHPa As Double
    Public Property OpadDzienMM As Double
    Public Property OpadNocMM As Double


End Class
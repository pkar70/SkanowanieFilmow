Imports Vblib

Public Class HistogramWindow

    Private _listaDni As New List(Of JedenDzien)

    Private _oBufor As Vblib.BufferSortowania

    Public Sub New(pliki As Vblib.BufferSortowania)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        _oBufor = pliki
    End Sub

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        PoliczPliki()
        SkalujPliki()
        DymkujPliki()

        Me.Width = Math.Min(System.Windows.SystemParameters.FullPrimaryScreenWidth * 0.9, _listaDni.Count * 5 + 32)

        uiLista.ItemsSource = _listaDni
    End Sub

    Private Sub PoliczPliki()

        Dim oLista As List(Of Vblib.OnePic) = _oBufor.GetList

        Dim sDataPrev As String = ""
        Dim oDzienItem As JedenDzien = Nothing

        For Each oPic As Vblib.OnePic In oLista
            Dim dData As DateTime = oPic.GetExifOfType(Vblib.ExifSource.SourceFile).DateMin
            Dim sData As String = dData.ToString("yy.MM.dd")

            If sData <> sDataPrev Then
                Dim bFound As Boolean = False
                For Each oItem As JedenDzien In _listaDni
                    If oItem.data = sData Then
                        bFound = True
                        sDataPrev = sData
                        oDzienItem = oItem
                    End If
                Next

                If Not bFound Then
                    Dim oNew As New JedenDzien
                    oNew.data = sData
                    oNew.licznik = 0
                    If dData.DayOfWeek = DayOfWeek.Sunday Then oNew.kropka = "*"
                    _listaDni.Add(oNew)
                    sDataPrev = sData
                    oDzienItem = oNew
                End If
            End If

            oDzienItem.licznik += 1
        Next

    End Sub

    Private Sub SkalujPliki()
        ' przeskaluj tak, zeby maks nie było wieksze niz Me.Height - 50
        Dim iMax As Integer = 0
        For Each oItem As JedenDzien In _listaDni
            iMax = Math.Max(iMax, oItem.licznik)
        Next

        Dim dSkala As Double = (Me.Height - 100) / iMax

        For Each oItem As JedenDzien In _listaDni
            oItem.scaled = oItem.licznik * dSkala
        Next

    End Sub

    Private Sub DymkujPliki()
        For Each oItem As JedenDzien In _listaDni
            oItem.dymek = oItem.data & vbCrLf & oItem.licznik
        Next
    End Sub

    Private Class JedenDzien
        Public Property licznik As Integer
        Public Property scaled As Integer
        Public Property dymek As String
        Public Property kropka As String = ""
        Public Property data As String ' yymmdd
    End Class
End Class

Imports pkar.DotNetExtensions

Public Class DateRange
    Inherits pkar.BaseStruct

    ''' <summary>
    ''' Lower bound of DateRange; default: Date.MinValue
    ''' </summary>
    Public Property Min As Date = DefaultMin
    ''' <summary>
    ''' Upper bound of DateRange; default: Date.MaxValue
    ''' </summary>
    Public Property Max As Date = DefaultMax

    ''' <summary>
    ''' Lower bound of valid DateRange; default is DefaultMin (1700.01.01)
    ''' </summary>
    Public Property MinValid As Date = DefaultMinValid
    ''' <summary>
    ''' Upper bound of valid DateRange; default is DefaultMax (current date + 1 day)
    ''' </summary>
    Public Property MaxValid As Date = DefaultMaxValid

    ''' <summary>
    ''' Default to be used for lower bound of DateRange; default is  DefaultMinValid (Date.MinValue)
    ''' </summary>
    Public Shared Property DefaultMin As Date = Date.MinValue
    ''' <summary>
    ''' Default to be used for upper bound of DateRange; default is DefaultMaxValid (Date.MaxValue)
    ''' </summary>
    Public Shared Property DefaultMax As Date = Date.MaxValue

    ''' <summary>
    ''' Default to be used for lower bound of valid DateRange; default: 1700.01.01
    ''' </summary>
    Public Shared Property DefaultMinValid As Date = New Date(1700, 1, 1)
    ''' <summary>
    ''' Default to be used for upper bound of valid DateRange; default: current date + 1 day
    ''' </summary>
    Public Shared Property DefaultMaxValid As Date = Date.Now.AddDays(1)


    Public Sub New(minval As Date, maxval As Date)
        Min = minval
        Max = maxval
        MinValid = DefaultMinValid
        MaxValid = DefaultMaxValid
    End Sub

    Public Sub New()
        Min = DefaultMin
        Max = DefaultMax
        MinValid = DefaultMinValid
        MaxValid = DefaultMaxValid
    End Sub


    ''' <summary>
    ''' check if given testDate is inside Valid range
    ''' </summary>
    Public Function IsDateValid(testDate As Date) As Boolean
        If testDate < MinValid Then Return False
        If testDate > MaxValid Then Return False
        Return True
    End Function

    ''' <summary>
    ''' if newmin is Valid, then adjust Min to min(Min, newmin)
    ''' </summary>
    Public Sub AdjustMin(newmin As Date)
        If Not IsDateValid(newmin) Then Return
        If newmin > Min Then Return
        Min = newmin
    End Sub

    ''' <summary>
    ''' if newmax is Valid, then adjust Max to max(Max, newmax)
    ''' </summary>
    Public Sub AdjustMax(newmax As Date)
        If Not IsDateValid(newmax) Then Return
        If newmax < Max Then Return
        Max = newmax
    End Sub

    ''' <summary>
    ''' if newmin is Valid, then adjust Min to min(Min, newmin);
    ''' if newmax is Valid, then adjust Max to max(Max, newmax);
    ''' same as AdjustMin(newmin); AdjustMax(newmax)
    ''' </summary>
    Public Sub AdjustMinMax(newmin As Date, newmax As Date)
        AdjustMax(newmax)
        AdjustMin(newmin)
    End Sub


    ''' <summary>
    ''' checks if testDate is inside DateRange
    ''' </summary>
    ''' <returns>True only if testDate is invalid, and is between Min and Max</returns>
    Public Function Matches(testDate As Date) As Boolean
        If Not IsDateValid(testDate) Then Return False
        If testDate < Min Then Return False
        If testDate > Max Then Return False
        Return True
    End Function

    ''' <summary>
    ''' checks if current range is inside testRange (testRange overlaps current range)
    ''' </summary>
    Public Function IsInsideRange(testRange As DateRange) As Boolean
        If Min < testRange.Min Then Return False
        If Max > testRange.Max Then Return False
        Return True
    End Function

    ''' <summary>
    ''' checks if current range overlaps testRange (testRange is inside current range)
    ''' </summary>
    Public Function OverlapsRange(testRange As DateRange) As Boolean
        If Min > testRange.Min Then Return False
        If Max < testRange.Max Then Return False
        Return True
    End Function

    ''' <summary>
    ''' return range that is intersection of current range and testRange; using Valid range from current range
    ''' </summary>
    Public Function Intersection(testRange As DateRange) As DateRange
        Dim newmin As Date = Min.Max(testRange.Min)
        Dim newmax As Date = Max.Min(testRange.Max)

        Dim oRng As New DateRange(Min.Max(testRange.Min), Max.Min(testRange.Max))

        oRng.MaxValid = MaxValid
        oRng.MinValid = MinValid

        Return oRng
    End Function

    ''' <summary>
    ''' checks if current range and testRange has intersection; all dates are treated as valid
    ''' </summary>
    Public Function HasIntersection(testRange As DateRange) As Boolean
        If testRange.Max < Min Then Return False
        If testRange.Min > Max Then Return False
        Return True
    End Function

    ''' <summary>
    ''' return date that is half-between Min and Max
    ''' </summary>
    Public Function MidDate() As Date
        Dim oDateDiff As TimeSpan = Max - Min
        Return Min.AddMinutes(oDateDiff.TotalMinutes)
    End Function

    ''' <summary>
    ''' return date in string as long as common characters from Min and Max
    ''' </summary>
    Public Function ToStringCommon(Optional format As String = "yyyy.MM.dd") As String
        Dim strMin As String = Min.ToString(format)
        Dim strMax As String = Max.ToString(format)

        Return strMin.CommonPrefix(strMax)

        'Dim iLp As Integer
        'For iLp = Math.Min(strMin.Length, strMax.Length) - 1 To 0 Step -1
        '    If strMin(iLp) = strMax(iLp) Then Exit For
        'Next

        'Return strMin.Substring(0, iLp)
    End Function

    ''' <summary>
    ''' return "Min - Max"
    ''' </summary>
    Public Function ToStringRange(Optional format As String = "yyyy.MM.dd", Optional separator As String = " - ") As String
        Dim strMin As String = Min.ToString(format)
        Dim strMax As String = Max.ToString(format)
        Return strMin & separator & strMax
    End Function

End Class

Imports System.Windows.Media.Animation

Public Class ButtonLeftContext
    Inherits Button

    Public Overrides Sub OnApplyTemplate()
        'Dim dbkf As New DiscreteBooleanKeyFrame With {.KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), .Value = True}

        'Dim baukf As New BooleanAnimationUsingKeyFrames
        'baukf.KeyFrames.Clear()
        'baukf.KeyFrames.Add(dbkf)

        'Dim stb As New Storyboard

        'Dim trig As New EventTrigger(RoutedEvent.)
        'Dim stylik = New Style With {.TargetType = GetType(Button)}
        'stylik.Triggers.Add(trig)
        'Me.Style = stylik
        AddHandler Me.Click, Sub() Me.ContextMenu.IsOpen = Not Me.ContextMenu.IsOpen
    End Sub



    '        <Button VerticalAlignment="Center" Content="alamakota" Click="Button_Click" HorizontalAlignment="Center">
    '<Button.Style>
    '<Style TargetType = "{x:Type Button}" >
    '                <Style.Triggers>
    '                    <EventTrigger RoutedEvent="Click">
    '                        <EventTrigger.Actions>
    '                            <BeginStoryboard>
    '                                <Storyboard>
    '                                    <BooleanAnimationUsingKeyFrames Storyboard.TargetProperty="ContextMenu.IsOpen">
    '                                        <DiscreteBooleanKeyFrame KeyTime="0:0:0" Value="True"/>
    '                                    </BooleanAnimationUsingKeyFrames>
    '                                </Storyboard>
    '                            </BeginStoryboard>
    '                        </EventTrigger.Actions>
    '                    </EventTrigger>
    '                </Style.Triggers>
    '                </Style>
    '        </Button.Style>

End Class

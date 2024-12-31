Imports pkar


Public Class PicMenuLocks
    Inherits PicMenuBase

    Public Overrides Sub OnApplyTemplate()
        ' wywoływame było dwa razy! I głupi błąd
        'System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
        If Not String.IsNullOrWhiteSpace(Me.Header) Then Return

        MyBase.OnApplyTemplate()

        If Not InitEnableDisable("locking", "blokowanie archiwizacji", True) Then Return

        Me.Items.Clear()

        AddMenuItem("LOCK", "zablokowanie zdjęć - będą pomijane przy archiwizacjach",
        Sub()
            OneOrMany(Sub(x)
                          x.locked = True
                      End Sub
        )

            EventRaise(PicMenuModifies.Lock)
        End Sub
        )


        AddMenuItem("unlock", "odblokowanie zdjęć -  - będą normalnie archiwizowane",
        Sub()
            OneOrMany(Sub(x)
                          x.locked = False
                      End Sub
        )

            EventRaise(PicMenuModifies.Lock)
        End Sub
        )

    End Sub

End Class

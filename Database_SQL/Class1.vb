
' Warning: Microsoft.EntityFrameworkCore.SqlServer nuget musi by� 3.*
' NIE MOZE BYC 5.*, bo wtedy problem z Method 'get_Properties' does not have an implementation. in type 'Microsoft.Extensions.Configuration.ConfigurationBuilder' from assembly 'Microsoft.Extensions.Configuration, ...
' NIE MOZE BYC 6.* ani 7.*, bo wymagaj� .Net 6
' no i 3 nie jest deprecated, a 5 - i owszem.
' 3.1.12: .Net Std 2.0, Microsoft.Data.SQLClient >= 1.1.3, Microsoft.EntityFrameworkCore.Relational >= 3.1.32

Public Class Class1
    ' "DefaultConnection": "Server=myServerAddress; Database=myDataBase; Trusted_Connection=True; MultipleActiveResultSets=true"        
    ' https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/
    '    Public Class ApplicationDbContext :  DbContext
    '{
    '    Protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    '    {
    '        optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=Test");
    '    }
    '}

    ' rozr�nienie: LOCAL / REMOTE?
    ' IP, dla LOCAL bez znaczneia
    ' username, password, datatabase PICSORT default, ale mo�na zmieni�
    ' user - dodawany do Descriptions jako "kto wpisa�"
    ' TABLE OnePic, jednak bez zmian - czysty JSON chyba najlepiej, znaczy AutoID, JSON, mo�e co� jako parametry...
    ' TABLE RemoteDescriptions, u�ywaj�c ID z Table.OnePic, i dodatki kto co wpisuje
    ' konwerter JSON -> SQL, oraz SQL -> JSON (do cel�w cho�by archiwizacji?)
    ' backupowanie wczytanych RemoteDescriptions do JSON

    Public Function TryConnect() As Boolean
        ' if FALSE, to nie pozwala doda� descriptions po archiwizacji
    End Function

    Public Function GetDescriptionsForPic(guid As String) As List(Of OneRemoteDescription)

    End Function

    ' public KopiujDane(remote server,id) - do lokalnego serwera (cache) ?

End Class

Public Class OneRemoteDescription
    Public Property guid As String ' albo AutoID z table.OnePic
    Public Property author As String
End Class
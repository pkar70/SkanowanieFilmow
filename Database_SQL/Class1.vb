
' Warning: Microsoft.EntityFrameworkCore.SqlServer nuget musi byæ 3.*
' NIE MOZE BYC 5.*, bo wtedy problem z Method 'get_Properties' does not have an implementation. in type 'Microsoft.Extensions.Configuration.ConfigurationBuilder' from assembly 'Microsoft.Extensions.Configuration, ...
' NIE MOZE BYC 6.* ani 7.*, bo wymagaj¹ .Net 6
' no i 3 nie jest deprecated, a 5 - i owszem.
' 3.1.12: .Net Std 2.0, Microsoft.Data.SQLClient >= 1.1.3, Microsoft.EntityFrameworkCore.Relational >= 3.1.32

' https://github.com/apache/lucenenet/issues/311
' There was a breaking change introduced In IConfigurationBuilder between
' Microsoft.Extensions.Configuration.Abstractions version 1.1.2 And version 2.0.0
' where IConfigurationBuilder.Properties was changed from Dictionary<String, Object> To IDictionary<String, Object>
' BING:
' the iconfigurationbuilder.properties property was of type Dictionary<string, object> in .NET Core 2.21
' and of type IDictionary<string, object> in .NET 5.02 and .NET 6.03.

Public Class Class1
    ' "DefaultConnection": "Server=myServerAddress; Database=myDataBase; Trusted_Connection=True; MultipleActiveResultSets=true"        
    ' https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/
    ' https://www.connectionstrings.com/sql-server/
    ' https://learn.microsoft.com/en-us/sql/relational-databases/native-client/applications/using-connection-string-keywords-with-sql-server-native-client?view=sql-server-ver16
    '    Public Class ApplicationDbContext :  DbContext
    '{
    '    Protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    '    {
    '        optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=Test");
    '    }
    '}

    ' rozró¿nienie: LOCAL / REMOTE?
    ' IP, dla LOCAL bez znaczneia
    ' username, password, datatabase PICSORT default, ale mo¿na zmieniæ
    ' user - dodawany do Descriptions jako "kto wpisa³"
    ' TABLE OnePic, jednak bez zmian - czysty JSON chyba najlepiej, znaczy AutoID, JSON, mo¿e coœ jako parametry...
    ' TABLE RemoteDescriptions, u¿ywaj¹c ID z Table.OnePic, i dodatki kto co wpisuje
    ' konwerter JSON -> SQL, oraz SQL -> JSON (do celów choæby archiwizacji?)
    ' backupowanie wczytanych RemoteDescriptions do JSON

    Public Function TryConnect() As Boolean
        ' if FALSE, to nie pozwala dodaæ descriptions po archiwizacji
    End Function

    Public Function GetDescriptionsForPic(guid As String) As List(Of OneRemoteDescription)

    End Function

    ' public KopiujDane(remote server,id) - do lokalnego serwera (cache) ?

End Class

Public Class OneRemoteDescription
    Public Property guid As String ' albo AutoID z table.OnePic
    Public Property author As String
End Class
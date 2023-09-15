
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

Imports Vblib

Public Class DatabaseSQL
    Implements Vblib.DatabaseInterface

    Private _dataFilenameFull As String
    Private _configDir As String

    Sub New(configDir As String)
        ' katalog jest dla PreBackup oraz na to sk¹d czytaæ dane konfiguracyjne (jeœli takie kiedyœ bêd¹)
        _configDir = configDir
        _dataFilenameFull = IO.Path.Combine(_configDir, "sql.bck")
    End Sub

    Public ReadOnly Property IsQuick As Boolean Implements DatabaseInterface.IsQuick
        Get
            Return True
        End Get
    End Property

    Public ReadOnly Property IsEnabled As Boolean Implements DatabaseInterface.IsEnabled
        ' TRUE, gdy w³¹czony, oraz mamy dane pozwalaj¹ce na Connect
        Get
            If Not GetSettingsBool("uiSqlEnabled") Then Return False
            If GetSettingsBool("uiSqlTrusted") Then Return True
            If String.IsNullOrWhiteSpace(GetSettingsString("uiSqlUserName")) Then Return False
            Return Not String.IsNullOrWhiteSpace(GetSettingsString("dbase.sql.pswd"))
        End Get
    End Property

    Private _IsLoaded As Boolean = False

    Public ReadOnly Property IsLoaded As Boolean Implements DatabaseInterface.IsLoaded
        Get
            Return _IsLoaded
        End Get
    End Property

    Private _IsEditable As Boolean = False

    Public ReadOnly Property IsEditable As Boolean Implements DatabaseInterface.IsEditable
        Get
            Return _IsEditable
        End Get
    End Property

    ReadOnly Property Nazwa As String Implements DatabaseInterface.Nazwa
        Get
            Return "SQL"
        End Get
    End Property


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



    Public Function Count() As Integer Implements DatabaseInterface.Count
        Throw New NotImplementedException()
    End Function

    Public Function Connect() As Boolean Implements DatabaseInterface.Connect
        Throw New NotImplementedException()
    End Function

    Public Function Init() As Boolean Implements DatabaseInterface.Init
        Throw New NotImplementedException()
    End Function

    Public Function Load() As Boolean Implements DatabaseInterface.Load
        Throw New NotImplementedException()
    End Function

    Public Function AddFiles(nowe As IEnumerable(Of OnePic)) As Boolean Implements DatabaseInterface.AddFiles
        Throw New NotImplementedException()
    End Function

    Public Function PreBackup() As Boolean Implements DatabaseInterface.PreBackup
        ' zrób backup do pliku tekstowego
        Throw New NotImplementedException()
    End Function

    Public Function Search(query As SearchQuery, Optional channel As SearchQuery = Nothing) As IEnumerable(Of OnePic) Implements DatabaseInterface.Search
        Throw New NotImplementedException()
    End Function

    Public Function ImportFrom(prevDbase As DatabaseInterface) As Integer Implements DatabaseInterface.ImportFrom
        Throw New NotImplementedException()
    End Function

    Public Function AddExif(picek As OnePic, oExif As ExifTag) As Boolean Implements DatabaseInterface.AddExif
        Throw New NotImplementedException()
    End Function

    Public Function AddKeyword(picek As OnePic, oKwd As OneKeyword) As Boolean Implements DatabaseInterface.AddKeyword
        Throw New NotImplementedException()
    End Function

    Public Function AddDescription(picek As OnePic, sDesc As String) As Boolean Implements DatabaseInterface.AddDescription
        Throw New NotImplementedException()
    End Function

    Public Function GetAll() As IEnumerable(Of OnePic) Implements DatabaseInterface.GetAll
        Throw New NotImplementedException()
    End Function

    ' public KopiujDane(remote server,id) - do lokalnego serwera (cache) ?

    Public Function TryConnect() As Boolean
        ' if FALSE, to nie pozwala dodaæ descriptions po archiwizacji
    End Function

    Public Function GetDescriptionsForPic(guid As String) As List(Of OneRemoteDescription)

    End Function

End Class

Public Class OneRemoteDescription
    Public Property guid As String ' albo AutoID z table.OnePic
    Public Property author As String
End Class
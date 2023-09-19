
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

Imports System.IO
Imports System.Linq.Expressions
Imports System.Net
Imports System.Reflection.Metadata.Ecma335
Imports Microsoft
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Storage.ValueConversion
Imports Newtonsoft.Json
Imports pkar
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

    Protected Shared Function GetConnString() As String
        Dim ret As String = $"Server={GetSettingsString("uiSqlInstance")};"
        ret &= "Database=PicSort;"
        ret &= "Connect Timeout=3;"
        If Vblib.GetSettingsBool("uiSqlTrusted") Then
            ret &= "Trusted_Connection=True;"
        Else
            ret &= $"User Id={GetSettingsBool("uiSqlUserName")};"
            ret &= $"Password={GetSettingsBool("uiSqlPassword")}"
        End If

        Vblib.DumpMessage("Connection string: " & ret)
        Return ret

        ' Server=HOME-PKARO\MSSQLSERVER;Database=PicSort;Trusted_Connection=True;Connect Timeout=15;
        'Microsoft.Data.SqlClient.SqlException 'A network-related or instance-specific error occurred while establishing a connection to SQL Server. The server was not found or was not accessible. Verify that the instance name is correct and that SQL Server is configured to allow remote connections. (provider: SQL Network Interfaces,
        'error: 25 - Connection string is not valid)'

        ' Server=(localdb);Database=PicSort;Trusted_Connection=True;Connect Timeout=15;
        ' A network-related or instance-specific error occurred while establishing a connection to SQL Server. The server was not found or was not accessible. Verify that the instance name is correct and that SQL Server is configured to allow remote connections. (provider: SQL Network Interfaces,
        ' error: 51 - An instance name was not specified while connecting to a Local Database Runtime. Specify an instance name in the format (localdb)\instance_name.)'

        ' Server=(localdb)\MSSQLSERVER;Database=PicSort;Trusted_Connection=True;Connect Timeout=15;
        'Microsoft.Data.SqlClient.SqlException: 'A network-related or instance-specific error occurred while establishing a connection to SQL Server. The server was not found or was not accessible. Verify that the instance name is correct and that SQL Server is configured to allow remote connections. (provider: SQL Network Interfaces,
        'error: 50 - Local Database Runtime error occurred. The specified LocalDB instance does not exist.

        ' Server=(localdb)\MSSQLLocalDB;Database=PicSort;Trusted_Connection=True;Connect Timeout=15;

        ' SELECT @@SERVERNAME + '\' + @@SERVICENAME AS InstanceName -> HOME-PKARO\MSSQLSERVER
        ' HOME-PKARO          	NULL	HOME-PKARO          	1433

    End Function

    Private _dbCtx As DbContext
    Private _allPics As ArchivePics

    Public Function Connect() As Boolean Implements DatabaseInterface.Connect
        ' tylko próbuje siê ³¹czyæ, nic wiêcej

        Dim dbBld As New DbContextOptionsBuilder
        dbBld = dbBld.UseSqlServer(GetConnString)

        Dim _dbCtx As New DbContext(dbBld.Options)
        _dbCtx.Database.SetCommandTimeout(TimeSpan.FromSeconds(2))

        Try

            _allPics = New ArchivePics(GetConnString)
            ' Dim sql As String = _allPics.Database.GenerateCreateScript ' create table
            ' _allPics.Database.EnsureDeleted()
            _allPics.Database.EnsureCreated()

            'Dim retcnt = _allPics.zdjecia.Count
            'Dim cnt = _allPics.zdjecia.Count

            Return True
        Catch ex As Exception
            Return False
        End Try

    End Function

    Public Function Disconnect() As Boolean Implements DatabaseInterface.Disconnect

        _allPics?.SaveChanges()
        _allPics?.Dispose()
        _allPics = Nothing

        _dbCtx?.Dispose()
        _dbCtx = Nothing

        Return True
    End Function


    Public Function Count() As Integer Implements DatabaseInterface.Count
        If Not IsLoaded Then Return -1
        If _allPics Is Nothing Then Return -2

        Return _allPics.zdjecia.Count
    End Function

    Public Function Init() As Boolean Implements DatabaseInterface.Init
        Return True
    End Function

    Public Function Load() As Boolean Implements DatabaseInterface.Load
        Dim ret As Boolean = Connect()
        If Not ret Then Return False
        _IsLoaded = True
        Return True
    End Function

    Public Function AddFiles(nowe As IEnumerable(Of OnePic)) As Boolean Implements DatabaseInterface.AddFiles
        If Not IsLoaded Then If Not Load() Then Return False

        For Each oPic As OnePic In nowe
            oPic.serno = 0  ' czyli default
            _allPics.zdjecia.Add(oPic)
        Next

        Return _allPics.SaveChanges > 0

    End Function

    Public Function PreBackup() As Boolean Implements DatabaseInterface.PreBackup
        If Not IsLoaded Then If Not Load() Then Return False

        ' najpierw kasujemy starszy backup, i zachoowujemy backup ostatniego zapisu
        If IO.File.Exists(_dataFilenameFull) Then
            IO.File.Delete(_dataFilenameFull & ".old")
            IO.File.Move(_dataFilenameFull, _dataFilenameFull & ".old")
        End If

        Dim sqlBackup As New BaseList(Of OnePic)(_dataFilenameFull)
        For Each oPic As OnePic In _allPics.zdjecia
            sqlBackup.Add(oPic)
        Next

        sqlBackup.Save()

        Return sqlBackup.Count > 0
    End Function

    Public Function Search(query As SearchQuery) As IEnumerable(Of OnePic) Implements DatabaseInterface.Search
        If Not IsLoaded Then Return Nothing

        ' dziwne, ale 5 razy takie wysz³o (null) - dwa przecinki pod rz¹d, pewnie przy dodawaniu do archiwumm
        Return _allPics.zdjecia.Where(Function(x) x IsNot Nothing).Where(Function(x) x.CheckIfMatchesQuery(query))

    End Function

    ' do listy dodaje pasuj¹ce do kana³u
    Private Sub SearchChannel(lista As List(Of OnePic), channel As ShareChannel, sinceId As String)
        If Not IsLoaded Then Return

        Dim lastNum As Integer = _allPics.zdjecia.First(Function(x) x.PicGuid = sinceId).serno
        If String.IsNullOrWhiteSpace(sinceId) Then lastNum = -1

        For Each oItem As OnePic In _allPics.zdjecia.Where(Function(x) x.serno > lastNum)
            If oItem Is Nothing Then Continue For ' pomijamy ewentualne puste

            ' czy jest na liœcie wyj¹tków?
            If channel.exclusions.Contains(oItem.PicGuid) Then Continue For

            For Each queryDef As ShareQueryProcess In channel.queries
                If oItem.CheckIfMatchesQuery(queryDef.query) Then
                    oItem.toProcessed = queryDef.processing & channel.processing
                    If Not lista.Exists(Function(x) x.sSuggestedFilename = oItem.sSuggestedFilename) Then
                        lista.Add(oItem)
                    End If
                End If
            Next

        Next

    End Sub


    Public Function Search(channel As ShareChannel, sinceId As String) As IEnumerable(Of OnePic) Implements DatabaseInterface.Search
        If Not IsLoaded Then Return Nothing

        Dim lista As New List(Of OnePic)
        SearchChannel(lista, channel, sinceId)

        Return lista
    End Function

    Public Function Search(shareLogin As ShareLogin, sinceId As String) As IEnumerable(Of OnePic) Implements DatabaseInterface.Search
        If Not IsLoaded Then Return Nothing

        Dim lista As New List(Of OnePic)

        For Each channel As ShareChannel In shareLogin.channels
            SearchChannel(lista, channel, sinceId)
        Next

        ' *TODO* exclusions loginu

        For Each oItem As OnePic In lista
            oItem.toProcessed &= shareLogin.processing
        Next

        Return lista
    End Function
    Public Function ImportFrom(prevDbase As DatabaseInterface) As Integer Implements DatabaseInterface.ImportFrom
        If Not Connect() Then Return False
        _allPics.Database.EnsureDeleted()
        _allPics.Database.EnsureCreated()

        AddFiles(prevDbase.GetAll)
        Return _allPics.zdjecia.Count
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
        Return _allPics.zdjecia
    End Function

    ' public KopiujDane(remote server,id) - do lokalnego serwera (cache) ?

    Public Function GetDescriptionsForPic(guid As String) As List(Of OneRemoteDescription)

    End Function



#Region "Struktury"
#Region "ArchivePics"
    Protected Class ArchivePics
        Inherits DbContext

        Public Property zdjecia As DbSet(Of OnePic)

        Private _connString As String
        Public Sub New(connString As String)
            _connString = connString
        End Sub

        Protected Overrides Sub OnModelCreating(builder As ModelBuilder)
            builder.Entity(GetType(OnePic))

            ' to co jest <JsonIgnore> trzeba ignorowaæ równiez tutaj

            'Dim konwerterNULL = New ValueConverter(Of Stream, String)(
            '    Function(x) "",
            '    Function(x) Nothing
            '    )
            ' Public Property oContent As IO.Stream
            'builder.Entity(GetType(OnePic)).Property("oContent").HasConversion(konwerterNULL)
            builder.Entity(GetType(OnePic)).Ignore("oContent")
            'Public Property oOstatniExif As ExifTag
            builder.Entity(GetType(OnePic)).Ignore("oOstatniExif")
            'Public Property locked As Boolean = False
            builder.Entity(GetType(OnePic)).Ignore("locked")
            'Public Property _PipelineInput As Stream
            'Public Property _PipelineOutput As Stream
            builder.Entity(GetType(OnePic)).Ignore("_PipelineInput")
            builder.Entity(GetType(OnePic)).Ignore("_PipelineOutput")
            'Public Property toProcessed As String
            builder.Entity(GetType(OnePic)).Ignore("toProcessed")



            Dim konwerterJSONDictStrStr = New ValueConverter(Of Dictionary(Of String, String), String)(
                Function(x) JsonConvert.SerializeObject(x, New JsonSerializerSettings With {.NullValueHandling = NullValueHandling.Ignore, .DefaultValueHandling = DefaultValueHandling.Ignore}),
                Function(x) JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(x)
                )

            builder.Entity(GetType(OnePic)).Property("Published").HasConversion(konwerterJSONDictStrStr)

            Dim konwerterJSONListOneDescr = New ValueConverter(Of List(Of OneDescription), String)(
                Function(x) JsonConvert.SerializeObject(x, New JsonSerializerSettings With {.NullValueHandling = NullValueHandling.Ignore, .DefaultValueHandling = DefaultValueHandling.Ignore}),
                Function(x) JsonConvert.DeserializeObject(Of List(Of OneDescription))(x)
                )
            builder.Entity(GetType(OnePic)).Property("descriptions").HasConversion(konwerterJSONListOneDescr)
            builder.Entity(GetType(OnePic)).Property("editHistory").HasConversion(konwerterJSONListOneDescr)

            Dim konwerterJSONListExifs = New ValueConverter(Of List(Of ExifTag), String)(
                Function(x) JsonConvert.SerializeObject(x, New JsonSerializerSettings With {.NullValueHandling = NullValueHandling.Ignore, .DefaultValueHandling = DefaultValueHandling.Ignore}),
                Function(x) JsonConvert.DeserializeObject(Of List(Of ExifTag))(x)
                )
            builder.Entity(GetType(OnePic)).Property("Exifs").HasConversion(konwerterJSONListExifs)

            ' jednak niepotrzebne
            ''The property 'AutoWeatherHourSingle.preciptype' could not be mapped, because it is of type 'string[]' which is not a supported primitive type or a valid entity type. Either explicitly map this property, or ignore it using the '[NotMapped]' attribute or by using 'EntityTypeBuilder.Ignore' in 'OnModelCreating'.
            ''The property 'AutoWeatherDay.preciptype' could not be mapped, because it is of type 'string[]' which is not a supported primitive type or a valid entity type. Either explicitly map this property, or ignore it using the '[NotMapped]' attribute or by using 'EntityTypeBuilder.Ignore' in 'OnModelCreating'.
            'Dim konwerterJSONArray = New ValueConverter(Of String(), String)(
            '    Function(x) JsonConvert.SerializeObject(x, New JsonSerializerSettings With {.NullValueHandling = NullValueHandling.Ignore, .DefaultValueHandling = DefaultValueHandling.Ignore}),
            '    Function(x) JsonConvert.DeserializeObject(Of String())(x)
            '    )
            'builder.Entity(GetType(AutoWeatherDay)).Property("preciptype").HasConversion(konwerterJSONArray)
            'builder.Entity(GetType(AutoWeatherHourSingle)).Property("preciptype").HasConversion(konwerterJSONArray)

            'The entity type 'AutoWeatherDay' requires a primary key to be defined. If you intended to use a keyless entity type call 'HasNoKey()'.
            builder.Entity(GetType(AutoWeatherDay)).HasNoKey()
            builder.Entity(GetType(AutoWeatherHourSingle)).HasNoKey()

            'Unable to track an instance of type 'OnePic' because it does not have a primary key. Only entity types with primary keys may be tracked.
            builder.Entity(GetType(OnePic)).HasKey("serno")

            'Exception thrown: 'System.InvalidOperationException' in Microsoft.EntityFrameworkCore.dll
            ' No suitable constructor found for entity type 'ExifTag'. The following constructors had parameters that could not be bound to properties of the entity type: cannot bind 'sSource' in 'ExifTag(string sSource)'.

        End Sub

        Protected Overrides Sub OnConfiguring(builder As DbContextOptionsBuilder)
            builder.UseSqlServer(_connString)
        End Sub

    End Class

    ' kopia z vblib.OnePic, po modyfikacjach
    Protected Class SQLOnePic
        Inherits BaseStruct
        ' 'The entity type 'SQLOnePic' requires a primary key to be defined. If you intended to use a keyless entity type call 'HasNoKey()'.'

        ' na potrzeby SQL i EF
        <ComponentModel.DataAnnotations.Key>
        Public Property serno As Integer

        Public Property Archived As String
        Public Property CloudArchived As String

        ' The property 'SQLOnePic.Published' could not be mapped, because it is of type 'Dictionary<string, string>' which is not a supported primitive type or a valid entity type. Either explicitly map this property, or ignore it using the '[NotMapped]' attribute or by using 'EntityTypeBuilder.Ignore' in 'OnModelCreating'.
        Public Property Published As String ' Dictionary(Of String, String)
        Public Property TargetDir As String ' OneDirFlat.sId
        Public Property Exifs As String ' New List(Of ExifTag) ' ExifSource.SourceFile ..., )
        Public Property InBufferPathName As String
        ''' <summary>
        ''' z którego Ÿród³a pochodzi plik
        ''' </summary>
        ''' <returns></returns>
        Public Property sSourceName As String
        ''' <summary>
        ''' pe³ny id w Ÿródle - np. full pathname
        ''' </summary>
        ''' <returns></returns>
        Public Property sInSourceID As String    ' usually pathname
        Public Property sSuggestedFilename As String ' mia³o byæ ¿e np. scinanie WP_. ale jednak tego nie robiê (bo moge posortowac po dacie, albo po nazwach - i w tym drugim przypadku mam rozdzia³ na np. telefon i aparat)

        Public Property descriptions As String ' List(Of OneDescription)
        Public Property editHistory As String ' List(Of OneDescription)
        Public Property TagsChanged As Boolean = False

        Public Property fileTypeDiscriminator As String = Nothing   ' tu "|>", "*", które maj¹ byæ dodawane do miniaturek

        Public Property PicGuid As String = Nothing  ' 0xA420 ImageUniqueID ASCII!

    End Class


#End Region
#End Region
End Class

Public Class OneRemoteDescription
    Public Property guid As String ' albo AutoID z table.OnePic
    Public Property author As String
End Class


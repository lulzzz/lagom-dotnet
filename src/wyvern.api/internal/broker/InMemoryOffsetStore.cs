using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Akka.Configuration;
using Akka.Streams.Util;
using Dapper;
using wyvern.api.abstractions;

namespace wyvern.api.@internal.surfaces
{
    /// <summary>
    /// Offset Store for local development, not to be used in production
    /// </summary>
    public class InMemoryOffsetStore : IOffsetStore
    {
        public Task<IOffsetDao> Prepare(string processorId, string tag)
        {
            return Task.FromResult<IOffsetDao>(new InMemoryOffsetDao());
        }
    }

    public class SqlServerOffsetStore : IOffsetStore
    {
        public class OffsetStoreConfiguration
        {
            public String TableName { get; }
            public Option<String> SchemaName { get; }
            public String IdColumnName { get; }
            public String TagColumnName { get; }
            public String SequenceOffsetColumnName { get; }
            public String TimeUuidOffsetColumnName { get; }

            public TimeSpan MinBackoff { get; }
            public TimeSpan MaxBackoff { get; }
            public Double RandomBackoffFactor { get; }
            public TimeSpan GlobalPrepareTimeout { get; }
            public Option<String> Role { get; }

            public OffsetStoreConfiguration(Config config)
            {
                var conf = config.GetConfig("wyvern.persistence.read-side.sqlserver.tables.offset");
                var tableName = conf.GetString("tableName");
                var schemaName = conf.GetString("schemaName");
                var columnsCfg = conf.GetConfig("columnNames");
                var idColumnName = columnsCfg.GetString("readSideId");
                var tagColumnName = columnsCfg.GetString("tag");
                var sequenceOffsetColumnName = columnsCfg.GetString("sequenceOffset");
                var timeUuidOffsetColumnName = columnsCfg.GetString("timeUuidOffset");
            }

            public override string ToString()
            {
                return $"OffsetTableConfiguration({TableName},{SchemaName})";
            }
        }

        class OffsetRow
        {
            public String Id { get; set; }
            public String Tag { get; set; }
            public long? SequenceOffset { get; set; }
            public string TimeUuidOffset { get; set; }
        }

        Func<SqlConnection> SqlProvider { get; }
        OffsetStoreConfiguration Config { get; }

        public SqlServerOffsetStore(Func<SqlConnection> sqlProvider, OffsetStoreConfiguration config)
        {
            SqlProvider = sqlProvider;
            Config = config;
        }

        public Task<IOffsetDao> Prepare(string processorId, string tag)
        {
            using (var con = SqlProvider.Invoke())
            {
                con.Execute($@"
                    if not exists (select * from sysobjects where name='{Config.TableName}' and xtype='U')
                        CREATE TABLE [{Config.TableName}] (
                            {Config.IdColumnName} VARCHAR(255),
                            {Config.TagColumnName} VARCHAR(255),
                            {Config.SequenceOffsetColumnName} bigint,
                            {Config.TimeUuidOffsetColumnName} char(36),
                            PRIMARY KEY ({Config.IdColumnName}, {Config.TagColumnName})
                        );
                ");

            }
        }

    }

    public class SqlServerOffsetDao : IOffsetDao
    {
        SqlServerOffsetStore Store { get; }

        public SqlServerOffsetDao(SqlServerOffsetStore store)
        {
            Store = store;
        }
    }

}

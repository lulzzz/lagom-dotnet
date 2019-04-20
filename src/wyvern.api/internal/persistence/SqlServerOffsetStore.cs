using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Akka;
using Akka.Configuration;
using Akka.Persistence.Query;
using Akka.Streams.Util;
using Dapper;
using wyvern.api.abstractions;

namespace wyvern.api.@internal.surfaces
{

    internal class SqlServerOffsetStore : IOffsetStore
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
                TableName = conf.GetString("tableName");
                SchemaName = conf.GetString("schemaName");

                var columnsCfg = conf.GetConfig("columnNames");
                IdColumnName = columnsCfg.GetString("readSideId");
                TagColumnName = columnsCfg.GetString("tag");
                SequenceOffsetColumnName = columnsCfg.GetString("sequenceOffset");
                TimeUuidOffsetColumnName = columnsCfg.GetString("timeUuidOffset");
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

        /// <summary>
        /// Prepare the offset tables if they do not already exist, and return
        /// an offset dao object.  The dao object will contain the current
        /// offset value if it exists, or no offset if it does not exist.
        /// </summary>
        /// <param name="readSideId"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        public async Task<IOffsetDao> Prepare(string readSideId, string tag)
        {
            using (var con = SqlProvider.Invoke())
            {
                var query = $@"
                    if not exists (select * from sysobjects where name='{Config.TableName}' and xtype='U')
                        CREATE TABLE [{Config.TableName}] (
                            {Config.IdColumnName} VARCHAR(255),
                            {Config.TagColumnName} VARCHAR(255),
                            {Config.SequenceOffsetColumnName} bigint,
                            {Config.TimeUuidOffsetColumnName} char(36),
                            PRIMARY KEY ({Config.IdColumnName}, {Config.TagColumnName})
                        );
                ";
                con.Execute(query);
            }

            Offset offset = Offset.NoOffset();
            var offsetRow = await GetOffset(readSideId, tag);
            if (offsetRow?.SequenceOffset.HasValue == true)
            {
                offset = Sequence.Sequence(offsetRow.SequenceOffset.Value);
            }
            else
            {
                using (var con = SqlProvider.Invoke())
                {
                    var res = con.Execute($@"
                        insert into [{Config.TableName}] (
                            {Config.IdColumnName},
                            {Config.TagColumnName},
                            {Config.SequenceOffsetColumnName},
                            {Config.TimeUuidOffsetColumnName})
                        values (
                            @readSideId,
                            @tag,
                            @offset,
                            null
                        )
                    ", new
                    {
                        readSideId,
                        tag,
                        offset = 0L
                    });
                }
            }

            return new SqlServerOffsetDao(this, readSideId, tag, offset);
        }

        public Task<Done> UpdateOffset(string readSideId, string tag, Offset offset)
        {
            using (var con = SqlProvider.Invoke())
            {
                var res = con.Execute($"update {Config.TableName} set {Config.SequenceOffsetColumnName} = @offset where {Config.IdColumnName} = @readSideId and {Config.TagColumnName} = @tag",
                    new
                    {
                        offset = ((Sequence)offset).Value,
                        readSideId,
                        tag
                    });
                if (res != 1)
                {
                    throw new InvalidOperationException("SQL Update did not result in a single record update.");
                }
                return Task.FromResult(Done.Instance);
            }
        }

        /// <summary>
        /// Returns the offset row for the given readsideid and tag pair, or
        /// null if the offset row does not exist.
        /// </summary>
        /// <param name="readSideId"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        Task<OffsetRow> GetOffset(string readSideId, string tag)
        {
            using (var con = SqlProvider.Invoke())
            {
                var row = con.QueryFirstOrDefault<OffsetRow>($@"
                    select
                        {Config.IdColumnName} Id,
                        {Config.TagColumnName} Tag,
                        {Config.SequenceOffsetColumnName} SequenceOffset,
                        {Config.TimeUuidOffsetColumnName} TimeUuidOffset
                    from
                        [{Config.TableName}]
                    where
                        {Config.IdColumnName} = @readSideId AND
                        {Config.TagColumnName} = @tag
                ", new
                {
                    readSideId,
                    tag
                });
                return Task.FromResult(row);
            }
        }
    }
}
using OpenSmc.Ifrs17.Domain.DataModel;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Graph.CallRecords;
using Microsoft.Graph.SecurityNamespace;
using Systemorph.Vertex.Activities;
using Systemorph.Vertex.Collections;
using Systemorph.Vertex.DataSetReader;
using Systemorph.Vertex.DataSetReader.Csv;
using Systemorph.Vertex.DataSource.Common;
using Systemorph.Vertex.Export;
using Systemorph.Vertex.Export.Builders;
using Systemorph.Vertex.Export.Builders.Interfaces;
using Systemorph.Vertex.FileStorage;
using Systemorph.Vertex.Import.Builders;
using Systemorph.Vertex.Import.Mappings;
using Systemorph.Vertex.Persistence.EntityFramework.Conversions.Api;
using Systemorph.Vertex.Session;

namespace OpenSmc.Ifrs17.Domain.Utils;

public record StreamWrapper(Stream Stream, bool WillBeReused);

public static class ActivityLogMethods
{

    public static string ProcessNotification(this object obj)
    {
        return obj is ActivityMessageNotification amn ? amn.Message : "";
    }

}

public record IOActivity : KeyedRecord
{
    public string Username { get; init; }

    public DateTime StartDateTime { get; init; }

    public DateTime EndDateTime { get; init; }

    public ActivityLogStatus Status { get; init; }

    public string Category { get; init; }

    public string ExceptionMessage { get; init; }

    [Conversion(typeof(JsonConverter<string[]>))]
    public string[] ErrorMessages { get; init; }

    [Conversion(typeof(JsonConverter<string[]>))]
    public string[] WarningMessages { get; init; }

    [Conversion(typeof(JsonConverter<string[]>))]
    public string[] InfoMessages { get; init; }

    public Guid? SourceId { get; init; }

    public IOActivity(ActivityLog log, ISessionVariable session)
    {
        Id = Guid.NewGuid();
        Username = session.User.Name;
        StartDateTime = log.StartDateTime;
        EndDateTime = log.FinishDateTime;
        Status = log.Status;
        ErrorMessages = log.Errors.Select(x => x.ProcessNotification()).Distinct().ToArray();
        WarningMessages = log.Warnings.Select(x => x.ProcessNotification()).Distinct().ToArray();
        InfoMessages = log.Infos.Select(x => x.ProcessNotification()).Distinct().ToArray();
    }

    public IOActivity(Guid id)
    {
        Id = id;
    }
}

public record IOContent : KeyedRecord
{
    public DateTime CreationTime { get; init; }
    public byte[] SerializedContent { get; init; }
    public uint? Length { get; init; }
    public string Format { get; init; }
    protected IDataSetImportVariable DataSetReader { get; set; }
    protected ISessionVariable Session { get; set; }
    public string Name { get; init; }
    public string ContentType { get; init; }

    public IOContent()
    {
        Id = Guid.NewGuid();
    }
}

public record ExportFile : IOContent
{
    protected DocumentBuilder Builder { get; set; }

    public ExportFile(DocumentBuilder builder, IDataSetImportVariable importVariable, ISessionVariable session)
    {
        Builder = builder;
        DataSetReader = importVariable;
        Session = session;
        Id = Guid.NewGuid();
        CreationTime = DateTime.UtcNow;
    }

    public ExportFile(Guid id)
    {
        Id = id;
    }

    public async Task<ExportFile> InitializeExportDataAsync()
    {
        byte[] content;
        var mapping = await Builder.GetMappingAsync();
        var storage = mapping.Storage as IFileReadStorage;
        var stream = await storage.ReadAsync(mapping.FileName, Session.CancellationToken);
        using (var ms = new MemoryStream())
        {
            await stream.CopyToAsync(ms);
            content = ms.ToArray();
            stream.Close();
            await stream.DisposeAsync();
        }

        return this with
        {
            Name = Path.GetFileName(mapping.FileName),
            ContentType = Path.GetExtension(mapping.FileName),
            SerializedContent = content,
            Length = content == null ? null : (uint)content.Length,
            Format = mapping.Format
        };
    }
}

public abstract record KeyedImport : IOContent
{
    protected ImportOptions Options { get; set; }

    public KeyedImport()
    {
        Id = Guid.NewGuid();
    }

    public abstract KeyedImport WithOptions(ImportOptions options);

    public KeyedImport WithSession(ISessionVariable session)
    {
        return this with { Session = session };
    }

    public KeyedImport WithDataSetReader(IDataSetImportVariable importVariable)
    {
        return this with { DataSetReader = importVariable };
    }

    public async Task<KeyedImport> InitializeImportDataAsync()
    {
        var stream = await GenerateStreamWrapperAsync();
        var formatAndContent = await GetInformationFromStreamAsync(stream);
        return this with
        {
            CreationTime = DateTime.UtcNow,
            Format = formatAndContent.Format ?? Options.Format,
            SerializedContent = formatAndContent.Content,
            Length = formatAndContent.Content != null ? (uint)formatAndContent.Content.Length : null
        };
    }

    private async Task<StreamWrapper> GenerateStreamWrapperAsync()
    {
        var stream = Options switch
        {
            FileImportOptions fio => new StreamWrapper(await fio.Storage.ReadAsync(fio.FileName, Session.CancellationToken), true),
            StreamImportOptions streamImportOptions => new StreamWrapper(streamImportOptions.Stream, false),
            StringImportOptions stringImportOptions => new StreamWrapper(new MemoryStream(Encoding.ASCII.GetBytes(stringImportOptions.Content)), true),
            DataSetImportOptions dataSetImportOptions => new StreamWrapper(new MemoryStream(Encoding.ASCII.GetBytes(DataSetCsvSerializer.Serialize(dataSetImportOptions.DataSet))), true),
            _ => null
        };
        return stream;
    }

    private async Task<(string Format, byte[] Content)> GetInformationFromStreamAsync(StreamWrapper stream)
    {
        byte[] content;
        string format;
        using (var ms = new MemoryStream())
        {
            await stream.Stream.CopyToAsync(ms);
            content = ms.ToArray();
            ms.Position = 0;
            var dsRes = await DataSetReader.ReadFromStream(ms).ExecuteAsync();
            format = dsRes.Format;
            if (stream.WillBeReused)
            {
                stream.Stream.Position = 0;
            }
            else
            {
                stream.Stream.Close();
                await stream.Stream.DisposeAsync();
            }
        }

        return (format, content);
    }
}

public record ImportFile : KeyedImport
{
    public string Directory { get; init; }

    [Conversion(typeof(JsonConverter<string[]>))]
    public string[] Partition { get; init; }

    public string Source { get; init; }

    public ImportFile() : base()
    {
        ;
    }

    public ImportFile(FileImportOptions options, IDataSetImportVariable importVariable, ISessionVariable session)
    {
        Options = options;
        DataSetReader = importVariable;
        Session = session;
        string fileName = options.FileName;
        Id = Guid.NewGuid();
        Name = Path.GetFileName(fileName);
        Directory = Path.GetDirectoryName(fileName);
        ContentType = Path.GetExtension(fileName);
        Source = options.Storage.GetType().Name;
        Partition = GetInvolvedPartitions(options);
        // Andrey Katz: Options.TargetDataSource.Partion.GetCurrentPartitions(?? What do we put here, different classes might posess various partitions, e.g. Yield Curve has none ??)
    }

    public override ImportFile WithOptions(ImportOptions options)
    {
        if (options is FileImportOptions fio)
        {
            string fileName = fio.FileName;
            return this with
            {
                Options = fio,
                Name = Path.GetFileName(fileName),
                Directory = Path.GetDirectoryName(fileName),
                ContentType = Path.GetExtension(fileName),
                Source = options.Storage.GetType().Name,
                Partition = GetInvolvedPartitions(options)
            };
        }
        else
        {
            throw new Exception("The import options must be of file import options type");
        }
    }

    public ImportFile(Guid id)
    {
        Id = id;
        Options = null;
        DataSetReader = null;
    }


    private string[] GetInvolvedPartitions(ImportOptions options)
    {
        // TODO
        //Andrey Katz: Get all the relevant partitions here 
        return null;
    }
}

public record ImportString : KeyedImport
{
    public string Content { get; init; }

    public ImportString() : base()
    {
        ;
    }

    public ImportString(StringImportOptions options, IDataSetImportVariable importVariable, ISessionVariable session)
    {
        Options = options;
        DataSetReader = importVariable;
        Session = session;
        Id = Guid.NewGuid();
        Content = options.Content;
    }

    public override ImportString WithOptions(ImportOptions options)
    {
        if (options is StringImportOptions sgio)
            return this with
            {
                Options = sgio,
                Content = sgio.Content
            };
        else throw new Exception("The import options must be of string import options type");
    }

    public ImportString(Guid id)
    {
        Id = id;
        Options = null;
        DataSetReader = null;
    }
}

public record ImportDataSet : KeyedImport
{
    public ImportDataSet(DataSetImportOptions options, IDataSetImportVariable importVariable, ISessionVariable session)
    {
        Session = session;
        DataSetReader = importVariable;
        Options = options;
        Id = Guid.NewGuid();
    }

    public ImportDataSet() : base()
    {
        ;
    }

    public override ImportDataSet WithOptions(ImportOptions options)
    {
        if (options is DataSetImportOptions dsio) return this with { Options = dsio };
        else throw new Exception("The import options must be of data set import options type");
    }

    public ImportDataSet(Guid id)
    {
        Id = id;
        Options = null;
        DataSetReader = null;
    }
}

public record ImportStream : KeyedImport
{
    public ImportStream(StreamImportOptions options, IDataSetImportVariable importVariable, ISessionVariable session)
    {
        Session = session;
        DataSetReader = importVariable;
        Options = options;
        Id = Guid.NewGuid();
    }

    public ImportStream() : base()
    {
        ;
    }

    public override ImportStream WithOptions(ImportOptions options)
    {
        if (options is StreamImportOptions smio) return this with { Options = smio };
        else throw new Exception("The import options must be of stream import options type");
    }

    public ImportStream(Guid id)
    {
        Id = id;
        Options = null;
        DataSetReader = null;
    }
}

public record ImportBuilderWriter(ImportOptionsBuilder Builder)
{
    public static ISessionVariable Session { get; set; }
    public static IDataSource DataSource { get; set; }
    public static IDataSetImportVariable ImportVariable { get; set; }

    public async Task<ActivityLog> ExecuteAsync()
    {
        var log = await Builder.ExecuteAsync();
        var options = Builder.GetImportOptions();
        var activity = options switch
        {
            FileImportOptions fio => await ReportInputAndUpdateActivityAsync<FileImportOptions, ImportFile>(log, fio, "Import from File "),
            StringImportOptions sgio => await ReportInputAndUpdateActivityAsync<StringImportOptions, ImportString>(log, sgio, "Import from String"),
            StreamImportOptions smio => await ReportInputAndUpdateActivityAsync<StreamImportOptions, ImportStream>(log, smio, "Import from Stream"),
            DataSetImportOptions dsio => await ReportInputAndUpdateActivityAsync<DataSetImportOptions, ImportDataSet>(log, dsio, "Import from Data Set"),
            _ => null
        };
        if (activity is null) throw new Exception("Import Options object is not an instance of an appropriate class.");
        await DataSource.UpdateAsync<IOActivity>(activity.RepeatOnce());
        await DataSource.CommitAsync();
        return log;
    }

    private async Task<IOActivity> ReportInputAndUpdateActivityAsync<TOptions, TImport>(ActivityLog log, TOptions options, string categoryMessage)
        where TOptions : ImportOptions
        where TImport : KeyedImport, new()
    {
        var activity = new IOActivity(log, Session);
        try
        {
            var import = new TImport();
            import = await import.WithSession(Session)
                .WithDataSetReader(ImportVariable)
                .WithOptions(options)
                .InitializeImportDataAsync() as TImport;
            activity = activity with { SourceId = import.Id, Category = categoryMessage };
            await DataSource.UpdateAsync<TImport>(import.RepeatOnce());
        }
        catch (Exception e)
        {
            activity = activity with
            {
                SourceId = null,
                Category = categoryMessage,
                ExceptionMessage = e.Message
            };
        }

        return activity;
    }
}

//ImportBuilderWriter.Session = Session;
//ImportBuilderWriter.DataSource = DataSource;
//ImportBuilderWriter.ImportVariable = DataSetReader;

public record ExportBuilderWriter(DocumentBuilder Builder)
{
    public static ISessionVariable Session { get; set; }
    public static IDataSetImportVariable ImportVariable { get; set; }
    public static IDataSource DataSource { get; set; }

    public async Task<ExportResult> ExecuteAsync()
    {
        var exportResult = await Builder.ExecuteAsync();
        var exportFile = await new ExportFile(Builder, ImportVariable, Session).InitializeExportDataAsync();
        var activity = new IOActivity(exportResult.ActivityLog, Session) with
        {
            Category = "Export to File",
            SourceId = exportFile.Id
        };
        await DataSource.UpdateAsync<ExportFile>(exportFile.RepeatOnce());
        await DataSource.UpdateAsync<IOActivity>(activity.RepeatOnce());
        await DataSource.CommitAsync();
        return exportResult;
    }
}

//ExportBuilderWriter.Session = Session;
//ExportBuilderWriter.DataSource = DataSource;
//ExportBuilderWriter.ImportVariable = DataSetReader;

public static class WithActivityExtensions
{
    public static ImportBuilderWriter WithActivityLog(this ImportOptionsBuilder builder)
    {
        return new ImportBuilderWriter(builder);
    }


    public static ExportBuilderWriter WithActivityLog(this IDocumentBuilder builder)
    {
        return new ExportBuilderWriter(builder as DocumentBuilder);
    }
}
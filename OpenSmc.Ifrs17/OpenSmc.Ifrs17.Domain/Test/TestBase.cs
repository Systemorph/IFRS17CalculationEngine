using OpenSmc.DataSource.Abstractions;
using OpenSmc.Scopes.Proxy;
using OpenSmc.Workspace;
using Systemorph.Vertex.Activities;
using Systemorph.Vertex.Import;

namespace OpenSmc.Ifrs17.Domain.Tests;

public abstract class TestBase
{
    protected readonly IImportVariable Import;
    protected readonly IDataSource DataSource;
    protected readonly TestData TestData;
    protected readonly IWorkspaceVariable Work;
    protected readonly IActivityVariable Activity;
    protected readonly IScopeFactory Scopes;

    public TestBase(IImportVariable import, IDataSource dataSource,
        IWorkspaceVariable work, IActivityVariable activity, IScopeFactory scopes)
    {
        Import = import;
        DataSource = dataSource;
        TestData = new TestData();
        Work = work;
        Activity = activity;
        Scopes = scopes;
    }
}
using Systemorph.Vertex.Activities;
using Systemorph.Vertex.DataSource.Common;
using Systemorph.Vertex.Import;
using Systemorph.Vertex.Workspace;

namespace OpenSmc.Ifrs17.Domain.Tests;

public abstract class TestBase
{
    protected IImportVariable? Import;
    protected IDataSource? DataSource;
    protected TestData? TestData;
    protected IWorkspaceVariable? Work;
    protected IActivityVariable? Activity;

    public TestBase(IImportVariable import, IDataSource dataSource,
        IWorkspaceVariable work, IActivityVariable activity)
    {
        Import = import;
        DataSource = dataSource;
        TestData = new TestData();
        Work = work;
    }
}
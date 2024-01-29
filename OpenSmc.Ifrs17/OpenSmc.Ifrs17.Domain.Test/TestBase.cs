﻿using Autofac;
using Systemorph.Vertex.Activities;
using Systemorph.Vertex.DataSetReader;
using Systemorph.Vertex.DataSource.Common;
using Systemorph.Vertex.Import;
using Systemorph.Vertex.Scopes.Proxy;
using Systemorph.Vertex.Workspace;

namespace OpenSmc.Ifrs17.Domain.Test;

public abstract class TestBase
{
    protected readonly IImportVariable Import;
    protected readonly IDataSource DataSource;
    protected readonly TestData TestData;
    protected readonly IWorkspaceVariable Work;
    protected readonly IActivityVariable Activity;
    protected readonly IScopeFactory Scopes;

    public TestBase(IImportVariable import,
        IWorkspaceVariable work, IScopeFactory scopes)
    {
        Activity = new ActivityVariable();
        Import = import;
        //DataSource = dataSource;
        TestData = new TestData();
        DataSource = TestData.DataSource;
        Work = work;
        Scopes = scopes;
    }
}
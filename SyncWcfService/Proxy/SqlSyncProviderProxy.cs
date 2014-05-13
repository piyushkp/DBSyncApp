using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Synchronization.Data;
using SyncWcfService.Interface;

namespace SyncWcfService.Proxy
{
    public class SqlSyncProviderProxy : RelationalProviderProxy
    {
        ISqlSyncContract dbProxy;
        public SqlSyncProviderProxy(string scopeName, string connectionString)
            : base(scopeName, connectionString)
        { }

        protected override void CreateProxy()
        {
            //base.proxy = new SyncServiceReference.SqlSyncContractClient();
            this.dbProxy = base.proxy as ISqlSyncContract;
        }

        public void CreateScopeDescription(DbSyncScopeDescription scopeDescription)
        {
            this.dbProxy.CreateScopeDescription(scopeDescription);
        }

        public DbSyncScopeDescription GetScopeDescription(string ScopeName, string serverConnectionString)
        {
            return this.dbProxy.GetScopeDescription(ScopeName, serverConnectionString);
        }

        public bool NeedsScope(string ScopeName, string ClientConnectionString)
        {
            return this.dbProxy.NeedsScope(scopeName, ClientConnectionString);
        }
    }
}
using Microsoft.Synchronization;
using Microsoft.Synchronization.Data;
using Microsoft.Synchronization.Data.SqlServer;
using SyncWcfService.Helper;
using SyncWcfService.Interface;
using SyncWcfService.Proxy;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace SyncWcfService.Service
{
    // Author: Piyush Patel
    // Email: er.piyushpatel@gmail.com
    // LinkedIn Profile: http://www.linkedin.com/pub/piyush-patel/13/604/492/
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "SqlWebSyncService" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select SqlWebSyncService.svc or SqlWebSyncService.svc.cs at the Solution Explorer and start debugging.
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession)]
    public class SqlWebSyncService : RelationalWebSyncService, ISqlSyncContract
    {
        SqlSyncProvider dbProvider;

        uint _batchSize = 0;
        string syncStats = "";
        string batchSpoolLocation;
        public ServerSynchronizationHelper synchronizationHelper = new ServerSynchronizationHelper("");

        public Dictionary<string, SqlSyncProvider> providersCollection = new Dictionary<string, SqlSyncProvider>();
        protected override RelationalSyncProvider ConfigureProvider(string scopeName, string connectionString)
        {
            ServerSynchronizationHelper helper = new ServerSynchronizationHelper("");
            //this.dbProvider = helper.ConfigureSqlSyncProvider(scopeName, connectionString);
            return this.dbProvider;
        }

        #region ISqlSyncContract Members

        public void CreateScopeDescription(DbSyncScopeDescription scopeDescription)
        {
            SqlSyncScopeProvisioning prov = new SqlSyncScopeProvisioning((SqlConnection)this.peerProvider.Connection, scopeDescription);
            prov.Apply();
        }

        public DbSyncScopeDescription GetScopeDescription(string scopeName, string serverConnectionString)
        {
            this.peerProvider.Connection.ConnectionString = serverConnectionString;
            DbSyncScopeDescription scopeDesc = SqlSyncDescriptionBuilder.GetDescriptionForScope(scopeName, (SqlConnection)this.dbProvider.Connection);
            return scopeDesc;
        }

        public bool NeedsScope(string scopeName, string clientConnectionString)
        {
            this.peerProvider.Connection.ConnectionString = clientConnectionString;
            SqlSyncScopeProvisioning prov = new SqlSyncScopeProvisioning((SqlConnection)this.peerProvider.Connection);
            return !prov.ScopeExists(scopeName);
        }


        public bool Sync(Guid clientId, string clientConnectionString)
        {
            try
            {
                string serverConnectionString = ConfigurationSettings.AppSettings["ServerConnectionString"].ToString();

                string scopeName = "ClientScope-" + clientId;

                SqlSyncProvider serverProvider = synchronizationHelper.ConfigureSqlSyncProvider(scopeName, serverConnectionString, clientId);

                SqlSyncProvider destinationProvider = new SqlSyncProvider();

                SqlConnection cn = new SqlConnection();
                cn.ConnectionString = clientConnectionString;

                destinationProvider.Connection = cn;
                destinationProvider.ScopeName = scopeName;

                SqlSyncProvider destinationProxy = new SqlSyncProvider(
                  scopeName, ((SqlConnection)destinationProvider.Connection));

                serverProvider.MemoryDataCacheSize = 100000;

                if (!string.IsNullOrEmpty(this.batchSpoolLocation))
                {
                    serverProvider.BatchingDirectory = this.batchSpoolLocation;
                    destinationProxy.BatchingDirectory = this.batchSpoolLocation;
                }

                SyncOperationStatistics statistics = synchronizationHelper.SynchronizeProviders(scopeName, clientConnectionString, serverConnectionString, serverProvider, destinationProxy);

                TimeSpan diff = statistics.SyncEndTime.Subtract(statistics.SyncStartTime);

                destinationProxy.Dispose();

                this.syncStats = string.Format("Batching: {4} - Total Time To Synchronize = {0}:{1}:{2}:{3}",
                    diff.Hours, diff.Minutes, diff.Seconds, diff.Milliseconds, (this._batchSize > 0) ? "Enabled" : "Disabled");

                return true;
            }
            catch (FaultException ex)
            {
                return false;
            }
        }

        #endregion
    }
}

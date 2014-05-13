using Microsoft.Synchronization;
using Microsoft.Synchronization.Data;
using Microsoft.Synchronization.Data.SqlServer;
using SyncWcfService.Proxy;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

namespace SyncWcfService.Helper
{
    public class ServerSynchronizationHelper
    {
        String serverHostName;

        public ServerSynchronizationHelper(String serverHostName)
        {
            this.serverHostName = serverHostName;
        }

        /// <summary>
        /// Utility function that will create a SyncOrchestrator and synchronize the two passed in providers
        /// </summary>
        /// <param name="localProvider">Local store provider</param>
        /// <param name="remoteProvider">Remote store provider</param>
        /// <returns></returns>
        public SyncOperationStatistics SynchronizeProviders(string ScopeName, string clientConnectionString, string serverConnectionString, KnowledgeSyncProvider localProvider, KnowledgeSyncProvider remoteProvider)
        {
            SyncOrchestrator orchestrator = new SyncOrchestrator();

            orchestrator.LocalProvider = localProvider;
            orchestrator.RemoteProvider = remoteProvider;

            
            orchestrator.Direction = SyncDirectionOrder.UploadAndDownload;
            SyncConflictResolver ConflictResolver = new SyncConflictResolver();
            ConflictResolver.ClientDeleteServerUpdateAction = ResolveAction.ServerWins;
            ConflictResolver.ClientUpdateServerDeleteAction = ResolveAction.ServerWins;
            ConflictResolver.ClientInsertServerInsertAction = ResolveAction.ServerWins;
            ConflictResolver.ClientUpdateServerUpdateAction = ResolveAction.ServerWins;
            
            //Check to see if any provider is a SqlCe provider and if it needs schema
            CheckIfProviderNeedsSchema(localProvider as SqlSyncProvider, serverConnectionString);
            CheckIfProviderNeedsSchema(ScopeName, clientConnectionString, serverConnectionString, remoteProvider as SqlSyncProviderProxy);

            SyncOperationStatistics stats = orchestrator.Synchronize();
            return stats;
        }

        /// <summary>
        /// Check to see if the passed in  SqlSyncProvider needs Schema from server
        /// </summary>
        /// <param name="localProvider"></param>
        private void CheckIfProviderNeedsSchema(SqlSyncProvider localProvider, string serverConnectionString)
        {
            if (localProvider != null)
            {
                SqlConnection conn = (SqlConnection)localProvider.Connection;
                SqlSyncScopeProvisioning sqlConfig = new SqlSyncScopeProvisioning(conn);
                string scopeName = localProvider.ScopeName;

                if (!sqlConfig.ScopeExists(scopeName))
                {
                    SqlSyncProviderProxy serverProxy = new SqlSyncProviderProxy(scopeName, serverConnectionString);

                    DbSyncScopeDescription scopeDesc = serverProxy.GetScopeDescription(scopeName, serverConnectionString);

                    serverProxy.Dispose();

                    sqlConfig.PopulateFromScopeDescription(scopeDesc);
                    sqlConfig.Apply();
                }
            }
        }

        /// <summary>
        /// Check to see if the passed in SQL provider needs Schema from server.
        /// This method assumes the provider is remote and uses a proxy instead
        /// of directly leveraging a provider.
        /// </summary>
        /// <param name="localProvider"></param>
        private void CheckIfProviderNeedsSchema(string scopeName, string clientConnectionString, string serverConnectionString, SqlSyncProviderProxy remoteProxy)
        {
            if (remoteProxy != null && remoteProxy.NeedsScope(scopeName, clientConnectionString))
            {
                SqlSyncProviderProxy serverProxy = new SqlSyncProviderProxy(scopeName, serverConnectionString);

                DbSyncScopeDescription scopeDesc = serverProxy.GetScopeDescription(scopeName, serverConnectionString);

                serverProxy.Dispose();

                remoteProxy.CreateScopeDescription(scopeDesc);
            }
        }
        
        /// <summary>
        /// Configure the SqlSyncprovider.  Note that this method assumes you have a direct conection
        /// to the server as this is more of a design time use case vs. runtime use case.  We think
        /// of provisioning the server as something that occurs before an application is deployed whereas
        /// provisioning the client is somethng that happens during runtime (on intitial sync) after the 
        /// application is deployed.
        ///  
        /// </summary>
        /// <param name="hostName"></param>
        /// <returns></returns>
        public SqlSyncProvider ConfigureSqlSyncProvider(string ScopeName, string serverConnectionString, Guid clientId)
        {
            SqlSyncProvider provider = new SqlSyncProvider();
            provider.ScopeName = ScopeName;
            provider.Connection = new SqlConnection();
            provider.Connection.ConnectionString = serverConnectionString;

            DbSyncScopeDescription scopeDesc = new DbSyncScopeDescription(ScopeName);

            SqlSyncScopeProvisioning serverConfig = new SqlSyncScopeProvisioning((System.Data.SqlClient.SqlConnection)provider.Connection);

            if (!serverConfig.ScopeExists(ScopeName))
            {
                serverConfig.ObjectSchema = "dbo.";

                scopeDesc.Tables.Add(SqlSyncDescriptionBuilder.GetDescriptionForTable("Client", (System.Data.SqlClient.SqlConnection)provider.Connection));

                serverConfig.PopulateFromScopeDescription(scopeDesc);

                //indicate that the base table already exists and does not need to be created
                serverConfig.SetCreateTableDefault(DbSyncCreationOption.Skip);

                serverConfig.Tables["Client"].AddFilterColumn("ClientId");
                serverConfig.Tables["Client"].FilterClause = "[side].[ClientId] = '" + clientId + "'";

                //Create new selectchanges procedure for our scope

                serverConfig.SetCreateProceduresForAdditionalScopeDefault(DbSyncCreationOption.Create);

                //provision the server
                serverConfig.Apply();
            }

            //Register the BatchSpooled and BatchApplied events. These are fired when a provider is either enumerating or applying changes in batches.
            provider.BatchApplied += new EventHandler<DbBatchAppliedEventArgs>(provider_BatchApplied);
            provider.BatchSpooled += new EventHandler<DbBatchSpooledEventArgs>(provider_BatchSpooled);

            return provider;
        }

        /// <summary>
        ///  Create a SqlSyncProvider instance without provisioning its database. 
        /// </summary>
        /// <param name="sqlConnection"></param>
        /// <returns></returns>
        public SqlSyncProvider ConfigureSqlSyncProvider(string ScopeName, SqlConnection sqlConnection)
        {
            SqlSyncProvider provider = new SqlSyncProvider();
            //Set the scope name
            provider.ScopeName = ScopeName;

            //Set the connection.
            provider.Connection = sqlConnection;

            //Register event handlers

            //1. Register the BatchSpooled and BatchApplied events. These are fired when a provider is either enumerating or applying changes in batches.
            provider.BatchApplied += new EventHandler<DbBatchAppliedEventArgs>(provider_BatchApplied);
            provider.BatchSpooled += new EventHandler<DbBatchSpooledEventArgs>(provider_BatchSpooled);

            //Thats it. We are done configuring the SQL provider.
            return provider;
        }

        /// <summary>
        /// Called whenever an enumerating provider spools a batch file to the disk
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void provider_BatchSpooled(object sender, DbBatchSpooledEventArgs e)
        {
        }

        /// <summary>
        /// Calls when the destination provider successfully applies a batch file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void provider_BatchApplied(object sender, DbBatchAppliedEventArgs e)
        {
        }

        /// <summary>
        /// Reads the watermarks for each table from the batch spooled event. Denotes the max tickcount for each table in each batch
        /// </summary>
        /// <param name="dictionary">Watermark dictionary retrieved from DbBatchSpooledEventArgs</param>
        /// <returns>String</returns>
        private string ReadTableWatermarks(Dictionary<string, ulong> dictionary)
        {
            StringBuilder builder = new StringBuilder();
            Dictionary<string, ulong> dictionaryClone = new Dictionary<string, ulong>(dictionary);
            foreach (KeyValuePair<string, ulong> kvp in dictionaryClone)
            {
                builder.Append(kvp.Key).Append(":").Append(kvp.Value).Append(",");
            }
            return builder.ToString();
        }

    }
}

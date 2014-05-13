using Microsoft.Synchronization.Data;
using SyncWcfService.Fault;
using System;
using System.ServiceModel;

namespace SyncWcfService.Interface
{
    [ServiceContract(SessionMode = SessionMode.Required)]
    public interface ISqlSyncContract : IRelationalSyncContract
    {
        [OperationContract]
        [FaultContract(typeof(WebSyncFaultException))]
        void CreateScopeDescription(DbSyncScopeDescription scopeDescription);

        [OperationContract]
        [FaultContract(typeof(WebSyncFaultException))]
        DbSyncScopeDescription GetScopeDescription(string scopeName, string serverConnectionString);

        [OperationContract]
        [FaultContract(typeof(WebSyncFaultException))]
        bool NeedsScope(string scopeName, string clientConnectionString);

        [OperationContract]
        [FaultContract(typeof(WebSyncFaultException))]
        bool Sync(Guid clientId, string clientConnectionString);

    }
}

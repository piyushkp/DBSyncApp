using System.Configuration;
using System;
using System.Timers;
namespace SyncApp
{
    class Program
    {
        const double interval10Seconds = 10 * 1000; // milliseconds to 10 seconds
        static SyncService.SqlSyncContractClient _objservice = new SyncService.SqlSyncContractClient();
        static Guid clientId;
        static string clientConnectionString = ConfigurationSettings.AppSettings["ClientConnectionString"].ToString();

        static void Main(string[] args)
        {
            syncData();
        }

        public static void syncData()
        {
            try
            {
                Timer checkForTime = new Timer(interval10Seconds);
                checkForTime.Elapsed += new ElapsedEventHandler(checkForTime_Elapsed);
                checkForTime.Enabled = true;

                Console.WriteLine("Synchronization process started !!");
                clientId = new Guid();
                clientId = Guid.Parse(ConfigurationSettings.AppSettings["ClientId"].ToString());
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        public static void checkForTime_Elapsed(object sender, ElapsedEventArgs e)
        {
            
                var status = _objservice.Sync(clientId, clientConnectionString);

                if (status)
                {
                    Console.WriteLine("Databases are Synced successfully.");
                }
                else
                {
                    Console.WriteLine("Error in Synchronization process.");
                    Environment.Exit(0);
                }
        }
    }
}

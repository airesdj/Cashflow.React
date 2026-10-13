using CashFlow.Core.Classes;
using CashFlow.DataAccess.Repositories.Base;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CashFlow.DataAccess.Repositories
{
    public class MaintenanceRepository : BaseRepository
    {
        private readonly Context _context;
        public MaintenanceRepository(Context context, ILogger<BaseRepository> logger) : base(logger)
        {
            _context = context;
        }

        protected override void Dispose(bool disposing)
            => base.Dispose(disposing);


        public bool UploadDataViaDumpFile(string pathlog)
        {
            bool DataUploadSuccess = true;
            string dump_file_result_name = $"{pathlog}\\{DateTime.Now.ToString("yyyyMMdd")}_cashflow-fullDatabase-bkp.sql";
            string dump_file_result_name_str = $"{pathlog}\\{DateTime.Now.ToString("yyyyMMdd")}_cashflow-structure-bkp.sql";

            try
            {

                string server_details = "";
                string dumpmysqlScriptForStructure = "--no-data";
                string dumpmysqlScriptForData = "--no-create-db --no-create-info --skip-triggers --skip-opt --lock-tables --skip-extended-insert --complete-insert --compact";
                string dumpmysqlMainScriptStructure = "";
                string dumpmysqlMainScriptData = "";

                using (StringUtils builder = new StringUtils())
                {
                    builder.Append("-h").Append(Utils.ConnectionString.GetConnectionString_Host()).AppendSpace()
                           .Append("-P3308").AppendSpace()
                           .Append("-u").Append(Utils.ConnectionString.GetConnectionString_UserID()).AppendSpace()
                           .Append("-p").Append(Utils.ConnectionString.GetConnectionString_PassWD());

                    server_details = builder.ToString();

                    builder.Append($"{server_details} ")
                           .Append(dumpmysqlScriptForStructure).AppendSpace()
                           .Append(Utils.ConnectionString.GetConnectionString_Database()).AppendSpace()
                           .Append($"--result-file={dump_file_result_name_str}");

                    dumpmysqlMainScriptStructure = builder.ToString();

                    builder.Append($"{server_details} ")
                           .Append(dumpmysqlScriptForData).AppendSpace()
                           .Append(Utils.ConnectionString.GetConnectionString_Database()).AppendSpace()
                           .Append($"--result-file={dump_file_result_name}");

                    dumpmysqlMainScriptData = builder.ToString();
                }

                Log.Info("Data Upload Via Dump File has started to run.");
                RunProcess(dumpmysqlMainScriptStructure);
                RunProcess(dumpmysqlMainScriptData);
            }
            catch (Exception ex)
            {
                DataUploadSuccess = false;
                Log.Error(ex);
            }
            finally
            {
                Log.Info("Data Download Via Dump File has ended.");
            }

            return DataUploadSuccess;
        }

        private void RunProcess(string arguments)
        {
            ProcessStartInfo startinfo = new ProcessStartInfo();
            string mysqldump_binary = "D:\\MySQL\\MySQL8\\MySQL Server 8.0\\bin\\mysqldump.exe";

            using (Process MySQLDump = new Process())
            {
                startinfo.WindowStyle = ProcessWindowStyle.Hidden;
                startinfo.FileName = Path.GetFileName(mysqldump_binary);
                startinfo.WorkingDirectory = File.Exists(mysqldump_binary) ? Path.GetDirectoryName(mysqldump_binary) : string.Empty;
                startinfo.Arguments = arguments;
                startinfo.ErrorDialog = false;
                startinfo.RedirectStandardError = true;
                startinfo.RedirectStandardOutput = false;
                startinfo.UseShellExecute = false;
                MySQLDump.StartInfo = startinfo;
                MySQLDump.Start();

                MySQLDump.WaitForExit();

                var err = MySQLDump.StandardError.ReadToEnd();

                if (MySQLDump.ExitCode > 0)
                    Log.Error(err);

                MySQLDump.Close();
            }

            startinfo = null!;
        }
    }
}

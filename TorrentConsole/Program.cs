using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using TorrentConsole.Core;

namespace TorrentConsole
{
     class Program
    {
        [STAThread]
        static async Task Main(string[] args) 
        {
            if (args.Length == 0) {
                Console.WriteLine("Usage : TorrentClient <file.Torrent>");
                return;
            }
            var torrentPath = args[0];
            Console.WriteLine("Started!!!");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string outputfile = SelectFolder();
            if (string.IsNullOrEmpty(outputfile)) {  return; }
            var downloadpath = outputfile;

            

            TorrentMetaData metaData = TorrentMetaData.Load(torrentPath);
            

            var client = new TorrentClient(metaData,downloadpath);
            await client.StartAsync();

            Console.WriteLine("Press Enter to exit.");
            Console.ReadLine();
        }

        public static string SelectFolder()
        {
            string selectedPath = null;
            var thread = new Thread(() =>
            {
                using (var dialog = new FolderBrowserDialog())
                {
                    Console.WriteLine("Entered SelectFolder function...........");
                    dialog.Description = "Select Download Folder";
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        selectedPath =  dialog.SelectedPath;
                    }

                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            return selectedPath;


        }


    }
}

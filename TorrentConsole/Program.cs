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
            /* if (args.Length == 0) {
                Console.WriteLine("Usage : TorrentClient <file.Torrent>");
                return;
            } */
            var torrentPath = GetTorrentFile();

            Console.WriteLine("Started!!!");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (string.IsNullOrEmpty(torrentPath)) 
            {
                MessageBox.Show("No Torrent file is selected", " ERROR !", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
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

        public static string GetTorrentFile() 
        {
            string selectedfile = null;
            var thread = new Thread(() =>
            {
                using (var dialog = new OpenFileDialog()) 
                {
                    Console.WriteLine("Entered Selectfile function.....");
                    dialog.Filter = "Torrent Files (*.torrent)|*.torrent";
                    dialog.Title = "Select Torrent File";
                    if (dialog.ShowDialog() == DialogResult.OK) 
                    {
                        selectedfile = dialog.FileName;
                    }
                }
            
            }


            );
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            return selectedfile;
        }


    }
}

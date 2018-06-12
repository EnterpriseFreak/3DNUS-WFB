using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Diagnostics;

namespace _3DNUS
{
    public partial class Main : Form
    {
        string server = "http://nus.cdn.c.shop.nintendowifi.net/ccs/download/";
        public Main()
        {
            InitializeComponent();
        }

        private void b_download_Click(object sender, EventArgs e)
        {
            dlProgess.Value = 0; //Downloadfortschritt auf 0 setzen

            if (!File.Exists("brickMsg.txt"))
            {
                DialogResult brickWarning = MessageBox.Show("Die mit dem Programm erstellten CIA-Archive sollten sicher sein aber es kann immer etwas schief gehen und dein System bricken. Akezeptiertst du dieses Risiko? Wenn du dies nicht akzeptierst beende dieses Tool und entferne es von deinem PC.", "Bitte durchlesen!", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (brickWarning == DialogResult.Yes)
                {
                    File.Create("brickMsg.txt");
                }
                else
                {
                    Application.Exit();
                }
            }
            
            if (t_titleid.Text.Length == 0 || t_version.Text.Length == 0)
            {
                MessageBox.Show("Please enter a titleid/firmware and version/region");
                return;
            }

            string title = t_titleid.Text;
            string version = t_version.Text;
            singledownload(title, version);
            
        }

        private void singledownload(string title, string version)
        {
            string cd = Path.GetDirectoryName(Application.ExecutablePath);
            string ftmp = cd + "\\tmp";
            string downloadtmd = server + title + "/" + "tmd." + version;
            string downloadcetk = server + title + "/cetk";

            Directory.CreateDirectory(ftmp);

            try
            {
                WebClient dtmd = new WebClient();
                dtmd.DownloadFile(downloadtmd, @ftmp + "\\tmd");
                dtmd.DownloadFile(downloadcetk, @ftmp + "\\cetk");
            }
            catch
            {
                //log("\r\nError downloading title " + title + " v" + version + " make sure the entered title ID and versions are correct");
                return;
            }

            //amount of contents
            FileStream tmd = File.Open(ftmp + "\\tmd", FileMode.Open, FileAccess.Read);
            tmd.Seek(518, SeekOrigin.Begin);
            byte[] cc = new byte[2];
            tmd.Read(cc, 0, 2);
            Array.Reverse(cc);
            int contentcounter = BitConverter.ToInt16(cc, 0);

            dlProgess.Step = 100 / contentcounter;

            //download files
            WebClient contd = new WebClient();
            for (int i = 1; i <= contentcounter; i++)
            {
                int contentoffset = 2820 + (48 * (i - 1));
                tmd.Seek(contentoffset, SeekOrigin.Begin);
                byte[] cid = new byte[4];
                tmd.Read(cid, 0, 4);
                string contentid = BitConverter.ToString(cid).Replace("-", "");
                string downname = ftmp + "\\" + contentid;

                try
                {
                    contd.DownloadFile(server + title + "/" + contentid, @downname);
                }
                catch (WebException)
                {
                    MessageBox.Show("Der Download einer für den Titel notwendigen Datei ist fehlgeschlagen dies bedeutet meist das die von dir gewünschte Version von Nintendo entfernt wurde. Bitte versuche es mit einer neueren Version erneut.");
                    return;
                }

                dlProgess.PerformStep();
            }

            tmd.Close();

            if (c_cia.Checked)
            {
                //create cia
                string command;

                command = Environment.CurrentDirectory + "\\tmp" + " " + Environment.CurrentDirectory + "\\" + title + "\\v" + version + "\\" + title + ".cia";
                Directory.CreateDirectory(Environment.CurrentDirectory + "\\" + title + "\\v" + version + "\\");

                MessageBox.Show(command);

                Process create = new Process();
                create.StartInfo.FileName = Path.GetTempPath()+"make_cdn_cia.exe";
                create.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                create.StartInfo.Arguments = command;
                create.Start();
                create.WaitForExit();
                Directory.Delete(ftmp, true);
                MessageBox.Show("Download beendet, CIA wurde erstellt!", "Download fertig!");
            }
            else
            {
                Directory.Move(ftmp, cd + "\\" + title);
                MessageBox.Show("Download beendet, die einzelnen contents wurden gespeichert!", "Download fertig!");
            }
        }

        private void Main_Load(object sender, EventArgs e)
        {
            try
            {
                File.WriteAllBytes(Path.GetTempPath() + "make_cdn_cia.exe", Properties.Resources.make_cdn_cia);
            }
            catch (IOException)
            {
                MessageBox.Show("Ein Fehler ist aufgetreten als versucht wurde eine temporäre Datei zu erstellen!", "Fehler");
                Application.Exit();
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Form credits = new credits();

            credits.Show();
        }
    }
}

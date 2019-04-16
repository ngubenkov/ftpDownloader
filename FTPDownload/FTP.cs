using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Net.Mime;
using System.Diagnostics;

namespace FTPDownload
{ 
    public partial class FTP : Form
    {
        private WebRequest sizeRequest  ; // get size of file( sometimes events doesn't provide size
        private WebRequest request; // downloading request
        private NetworkCredential credentials; // for credentials

        public FTP()
        {
            InitializeComponent();
        }

        private void btnDownload_Click(object sender, EventArgs e) // download btn
        {
            if (string.IsNullOrEmpty(txtLink.Text)) // check for input
            {
                MessageBox.Show("Please enter url");
            }
            else // if user entered something -> go execution
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog(); // open dialog to save file
                saveFileDialog.FileName = getFileName(txtLink.Text); // insert default file name from link

                if (saveFileDialog.ShowDialog() == DialogResult.OK) // if path selected beging downloading
                {
                    Task.Run(() => downloadFTP(txtLink.Text, Path.GetFullPath(saveFileDialog.FileName).ToString())); // run downloading
                }
            }              
        }

        private void downloadFTP(string url, string path) // downloading
        {
            try
            {
                long sizeOfFile;        // size of file
                int hugeFileHelper = 1; // used for convertion of big files

                sizeRequest = WebRequest.Create(url); // get size of file( sometimes events doesn't provide size
                request = WebRequest.Create(url); // downloading request

                if (cbCredentials.Checked) // use credentials
                {
                    sizeRequest.Credentials = credentials;
                    request.Credentials = credentials;
                }

                // get size of file manually, sometimes automatically it doesn't work
                sizeRequest.Method = WebRequestMethods.Ftp.GetFileSize;
                if (sizeRequest.GetResponse().ContentLength > int.MaxValue) // check if it's huge file -> do convertion
                {
                    hugeFileHelper = 2048; // change this value for really huge files
                    sizeOfFile = (int)((long)sizeRequest.GetResponse().ContentLength / hugeFileHelper);
                }
                else // if file size can be fitted in int -> move on
                {
                    sizeOfFile = (int)sizeRequest.GetResponse().ContentLength;
                }

                progressBar.Invoke(     // set max value(size of file) of progress bar
                    (MethodInvoker)(() => progressBar.Maximum = (int)(sizeOfFile)));

                // Download the file
                request.Method = WebRequestMethods.Ftp.DownloadFile;

                using (Stream ftpStream = request.GetResponse().GetResponseStream())
                using (Stream fileStream = File.Create(path))
                {
                    byte[] buffer = new byte[163840]; // in case of memory/speed problems reduce this value
                    int read;
                    while ((read = ftpStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fileStream.Write(buffer, 0, read);
                        long position = (long)fileStream.Position;
                        progressBar.Invoke(
                            (MethodInvoker)(() => progressBar.Value = (int)(position / hugeFileHelper))); // update progress bar
                    }
                }
                progressBar.Invoke((MethodInvoker)(() => progressBar.Value = 0)); // set progress as 0 when finish
                MessageBox.Show("Download finished");
            }
            catch (WebException e)
            {
                if (e.Message == "The remote server returned an error: (530) Not logged in.") // Unauthorized access
                    MessageBox.Show("Use correct credentials to log in");

                else if (e.Message == "The remote server returned an error: (550) File unavailable (e.g., file not found, no access).") // no file or no access to file
                    MessageBox.Show("File unavailable (e.g., file not found, no access)");

                else // other error cases
                    MessageBox.Show(e.Message);
            }
            catch (Exception e)
            {
                if (e.Message == "Invalid URI: The format of the URI could not be determined.") // incorrect url
                    MessageBox.Show("Use correct correct url");

                else // other error cases
                    MessageBox.Show(e.Message);
            }
        }

        private string getFileName(string url) // return file name based on url
        {
            return url.Substring(url.LastIndexOf("/") + 1);
        }


        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void cbCredentials_CheckedChanged(object sender, EventArgs e)  // use credentials for downloading
        {
            if (cbCredentials.Checked)
            {
                credentials = new NetworkCredential(txtLogin.Text, txtPassword.Text); 
                txtLogin.ReadOnly = true;
                txtPassword.ReadOnly = true;
            }
            else
            {
                txtLogin.ReadOnly = false;
                txtPassword.ReadOnly = false;
            }
        }
    }
}

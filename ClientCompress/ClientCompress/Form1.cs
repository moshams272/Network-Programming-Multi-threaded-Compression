using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Compression;
using System.Threading;

namespace ClientCompress
{
    public partial class Form1 : Form
    {
        private string selectedFilePath = "";
        public Form1()
        {
            InitializeComponent();
        }
        private async Task ReadExactlyAsync(NetworkStream stream, byte[] buffer, int length)
        {
            int totalRead = 0;
            while (totalRead < length)
            {
                int read = await stream.ReadAsync(buffer, totalRead, length - totalRead);
                if (read == 0) throw new Exception("Connection to the server dropped unexpectedly.");
                totalRead += read;
            }
        }

        private void btnSelectFile_Click_1(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Select the file you want to compress";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    selectedFilePath = ofd.FileName;
                    lblStatus.Text = $"Selected: {Path.GetFileName(selectedFilePath)}";
                }
            }
        }

        private async void btnSend_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedFilePath) || !File.Exists(selectedFilePath))
            {
                MessageBox.Show("Please select a file first!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string serverIp = txtIP.Text;
            int port = 9050;

            try
            {
             
                btnSend.Enabled = false;
                btnSelectFile.Enabled = false;
                lblStatus.Text = "Status: Connecting and sending file...";

               
                byte[] originalData = File.ReadAllBytes(selectedFilePath);
                long originalSize = originalData.Length;

                using (TcpClient client = new TcpClient())
                {
                    await client.ConnectAsync(serverIp, port);
                    using (NetworkStream stream = client.GetStream())
                    {
                       
                        byte[] sizeBytes = BitConverter.GetBytes(originalSize);
                        await stream.WriteAsync(sizeBytes, 0, 8);

                       
                        await stream.WriteAsync(originalData, 0, originalData.Length);
                        lblStatus.Text = "Status: Sent. Waiting for server response...";

                       
                        byte[] compressedSizeBytes = new byte[8];
                        await ReadExactlyAsync(stream, compressedSizeBytes, 8);
                        long compressedSize = BitConverter.ToInt64(compressedSizeBytes, 0);

                      
                        byte[] compressedData = new byte[compressedSize];
                        await ReadExactlyAsync(stream, compressedData, (int)compressedSize);
                        lblStatus.Text = "Status: Compressed file received successfully. Saving...";

                      
                        SaveFileDialog sfd = new SaveFileDialog();
                        sfd.FileName = Path.GetFileName(selectedFilePath) + ".gz"; 
                        sfd.Title = "Save Compressed File";

                        if (sfd.ShowDialog() == DialogResult.OK)
                        {
                            File.WriteAllBytes(sfd.FileName, compressedData);
                            MessageBox.Show($"File saved successfully!\nOriginal Size: {originalSize} bytes\nCompressed Size: {compressedSize} bytes", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                
                btnSend.Enabled = true;
                btnSelectFile.Enabled = true;
                lblStatus.Text = "Status: Ready";
            }
        }
    }
}

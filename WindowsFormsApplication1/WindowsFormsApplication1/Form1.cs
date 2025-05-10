using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        byte[] fileBytes = null;
        int width = 0;
        int height = 0;
        int bmpType = 0;
        int headerSize = 0;
        string signature = "Encrypt@LeeLuSoft.BlogSpot.com";
        uint constant = 0x8088405;
        bool isFound = false;
        List<byte> pixels;
        uint password = 0;

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                txtPassword.Text = string.Empty;
                txtMessage.Text = string.Empty;

                txtFilePath.Text = openFileDialog1.FileName;
                fileBytes = File.ReadAllBytes(openFileDialog1.FileName);

                width = (int)(fileBytes[21] * Math.Pow(16, 8) + fileBytes[20] * Math.Pow(16, 4) + fileBytes[19] * Math.Pow(16, 2) + fileBytes[18]);
                height = (int)(fileBytes[25] * Math.Pow(16, 8) + fileBytes[24] * Math.Pow(16, 4) + fileBytes[23] * Math.Pow(16, 2) + fileBytes[22]);
                bmpType = (int)fileBytes[28];
                headerSize = (int)fileBytes[10];

                if (width * height >= 488)
                {


                    isFound = false;

                    if (bmpType == 24)
                    {
                        btnDecrypt.Enabled = true;
                        pixels = new List<byte>();
                        int size = 3 * width * height;
                        int zeroPad = width % 4;

                        for (int i = 0; i < height; i++)
                            for (int j = 0; j < (width * 3); j++)
                                pixels.Add(fileBytes[(headerSize + ((size + zeroPad * height)) - (i + 1) * (3 * width + zeroPad)) + j]);
                    }
                    else if (bmpType == 16)
                    {
                        btnDecrypt.Enabled = true;
                        pixels = new List<byte>();
                        int size = 2 * width * height;

                        for (int i = 0; i < height; i++)
                            for (int j = 0; j < (width * 2); j++)
                                pixels.Add(fileBytes[(headerSize + size - (i + 1) * (2 * width)) + j]);

                    }
                    else if (bmpType == 32)
                    {
                        btnDecrypt.Enabled = true;
                        pixels = new List<byte>();
                        int size = 4 * width * height;

                        for (int i = 0; i < height; i++)
                            for (int j = 0; j < (width * 4); j++)
                                pixels.Add(fileBytes[(headerSize + size + -(i + 1) * (4 * width)) + j]);
                    }
                    else
                    {
                        MessageBox.Show("Not Supported by Steganography X Plus");
                        btnDecrypt.Enabled = false;
                    }
                }
                else
                {
                    MessageBox.Show("Not Supported by Steganography X Plus");
                    btnDecrypt.Enabled = false;
                }
            }
        }

        private void btnDecrypt_Click(object sender, EventArgs e)
        {
            Stopwatch sw;
            sw = Stopwatch.StartNew();
            txtPassword.Text = string.Empty;
            txtMessage.Text = string.Empty;
            int zeroPad = 0;
            int jump = 0;

            if (bmpType == 24)
            {
                jump = 3;
                zeroPad = width % 4;
            }
            else if (bmpType == 32)
                jump = 4;
            else if (bmpType == 16)
                jump = 2;

            for (uint i = 1; i <= 0xFFFFFFFF; i++)
            {
                uint multiplier = i;
                int a = 0;
                byte b = 0;


                for (int j = jump - 1, k = 0, m = -1; j < 240 * jump; j += jump, k++)
                {

                    for (int n = 0; n < jump - 1; n++)
                    {
                        multiplier = multiplier * constant;
                        multiplier++;
                    }

                    multiplier = multiplier * constant;
                    byte val = (byte)((multiplier & 0xFF000000) >> 24);
                    multiplier++;

                    if (zeroPad > 0 && (j + 1) % (width * 3) == 0)
                    {
                        for (int n = 0; n < zeroPad; n++)
                        {
                            multiplier = multiplier * constant;
                            multiplier++;
                        }
                    }

                    if (k == 0)
                    {
                        a = 0;
                        b = 0;
                        m++;
                    }

                    a = (a + (((pixels[j] ^ val) & 1) << k));
                    b = (byte)((byte)(signature[m] << (7 - k)) >> (7 - k));

                    if (a != b)
                        break;

                    if (k == 7)
                        k = -1;

                    if (j == 240 * jump - 1)
                        isFound = true;
                }

                if (isFound)
                {
                    txtPassword.Text = Convert.ToString(i);
                    password = i;
                    break;
                }

            }


            if (isFound)
            {
                isFound = false;

                uint multiplier = password;
                int a = 0;
                List<byte> message = new List<byte>();

                for (int j = jump - 1, k = 0, m = -1; j < pixels.Count; j += jump, k++)
                {

                    for (int n = 0; n < jump - 1; n++)
                    {
                        multiplier = multiplier * constant;
                        multiplier++;
                    }
                    multiplier = multiplier * constant;
                    byte val = (byte)((multiplier & 0xFF000000) >> 24);
                    multiplier++;

                    if (zeroPad > 0 && (j + 1) % (width * 3) == 0)
                    {
                        for (int n = 0; n < zeroPad; n++)
                        {
                            multiplier = multiplier * constant;
                            multiplier++;
                        }
                    }

                    if (k == 0)
                    {
                        a = 0;
                        m++;
                    }

                    a = (a + (((pixels[j] ^ val) & 1) << k));

                    if (k == 7)
                    {
                        message.Add((byte)a);
                        if (a == 0)
                            break;
                        k = -1;
                    }
                }

                message = message.GetRange(60, message.Count - 61);
                txtMessage.Text = Encoding.UTF8.GetString(message.ToArray());

                sw.Stop();
            }
            else
            {
                sw.Stop();
                MessageBox.Show("Not found");

            }

            MessageBox.Show("Execution time:" + Convert.ToString(sw.ElapsedMilliseconds) + "ms");
        }

    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using CompAnalytics.X9.Document;
using CompAnalytics.X9.Records;
using System.IO;
//using X937FormApp;
using CompAnalytics.X9;

namespace X937FormApp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SqlConnection CN = Program.ConnectSQL("Data Source=Pegasus;initial catalog=X9;Trusted_Connection=Yes;MultipleActiveResultSets=True;");
            SqlDataReader HDR = Program.GetHeaderData("X937_Header_GET", 1, CN);
            SqlDataReader CLHDR = Program.GetHeaderData("X937_CashLetterHeader_GET", 1, CN);
            SqlDataReader BHDR = Program.GetHeaderData("X937_BundleHeader_GET", 1, CN);
            SqlDataReader ChkDR = Program.GetCheckData("X937_CheckData_Get", 1, 0, CN);

            X9Document doc = Program.Create(HDR, CLHDR, BHDR, ChkDR); // creates file

            HDR.Close();
            CLHDR.Close();
            BHDR.Close();
            ChkDR.Close();
            string outFilePath = @"C:\Temp\example3.x9";
            using (X9Writer writer = new X9Writer(doc))
            using (MemoryStream byteStream = new MemoryStream(writer.WriteX9Document()))
            using (FileStream x9FileStream = File.Create(outFilePath))
            {
                byteStream.CopyTo(x9FileStream);
            }
            //Update status in DB, upload file, etc...

            //Cleanup
            CN.Close();
            CN.Dispose();
            lblResult.Text = "Completed";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string path = @"C:\Temp\testbrinks.x9";
            DirectoryInfo imageOutDir = new DirectoryInfo(@"C:\Temp\SampleCheckImages");
            using (Stream x9File = File.OpenRead(path))
            using (X9Reader reader = new X9Reader(x9File))
            {
                X9Document doc = reader.ReadX9Document();
                reader.WriteImagesToDisk(imageOutDir);
            }
            lblResult.Text = "Images Exported";
        }
    }
}

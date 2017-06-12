using System;
using System.Windows.Forms;

namespace InGameDanmaku
{
    public partial class Manage : Form
    {

        public Manage()
        {
            InitializeComponent();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            InGameDanmaku.UpdateTitle();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MessageBox.Show(InGameDanmaku.Translate(textBox1.Text));
        }
    }
}

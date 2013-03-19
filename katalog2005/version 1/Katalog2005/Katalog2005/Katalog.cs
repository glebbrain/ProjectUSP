using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;


namespace Katalog2005
{
    public partial class Katalog : Form
    {
        public Katalog()
        {
            InitializeComponent();
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            setFormSize();                
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            authorization();
        }

        private void ������������������ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab != this.tabPage1)
            {
                tabControl1.SelectedTab = this.tabPage1;
            }
                
        }

       

        private void ����������������ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab != this.tabPage2)
            {
                tabControl1.SelectedTab = this.tabPage2;
            }
        }

        private void �������ExcelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        private void button2_Click(object sender, EventArgs e)
        {

        }

        private void dataGridView1_MouseDown(object sender, MouseEventArgs e)
        {
            loadPictureInPictureBox(e);
        }

        private void dataGridView1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down)
            {
                loadPictureInPictureBoxWithKeyEvent(dataGridView1.SelectedCells[0].RowIndex);

            }

            if (e.KeyCode == Keys.Up)
            {
                loadPictureInPictureBoxWithKeyEvent(dataGridView1.SelectedCells[0].RowIndex);
            }
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            sortInformWithTree(e); 
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (Katalog2005.WinFroms.search.search newSearchInBD = new Katalog2005.WinFroms.search.search(dataGridView1))
            {
                newSearchInBD.ShowDialog();
                newSearchInBD.Dispose();
            }
        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            ViewInformationAboutElement(e.RowIndex);
        }

        private void ��������������ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab != this.tabPage1)
            {
                tabControl1.SelectedTab = this.tabPage1;
            }
        }

        private void ������������������������������������ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (Katalog2005.WinFroms.AddInformationAboutElements.AddInformation AddForm = new Katalog2005.WinFroms.AddInformationAboutElements.AddInformation(1))
            {
                AddForm.ShowDialog();
            }
        }       
    }
}
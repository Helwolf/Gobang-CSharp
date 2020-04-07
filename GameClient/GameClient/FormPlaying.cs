using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
namespace GameClient
{
    public partial class FormPlaying : Form
    {
        private int tableIndex;
        private int side;
        
        private bool isGameStart=false;
        private DotColor[,] grid = new DotColor[16, 16]; 
        private Bitmap blackBitmap;
        private Bitmap whiteBitmap;
        private DotColor[] col = { DotColor.Black, DotColor.White};


        private bool isReceiveCommand = false;
        private Service service;
        delegate void LabelDelegate(Label label, string str);
        delegate void ButtonDelegate(Button button, bool flag);
        delegate void RadioButtonDelegate(RadioButton radioButton, bool flag);
        delegate void SetDotDelegate(int i, int j, int dotColor);
        LabelDelegate labelDelegate;
        ButtonDelegate buttonDelegate;
        RadioButtonDelegate radioButtonDelegate;
        public FormPlaying(int TableIndex, int Side, StreamWriter sw)
        {
            InitializeComponent();
            this.tableIndex = TableIndex;
            this.side = Side;
            labelDelegate = new LabelDelegate(SetLabel);
            buttonDelegate = new ButtonDelegate(SetButton);
            radioButtonDelegate = new RadioButtonDelegate(SetRadioButton);
            blackBitmap = new Bitmap(Properties.Resources.black);
            whiteBitmap = new Bitmap(Properties.Resources.white);
            service = new Service(listBox1, sw);
            isGameStart = false;
        }
        

        private void FormPlaying_Load(object sender, EventArgs e)
        {
            
            radioButton3.Checked = true;
            for (int i = 0; i <= grid.GetUpperBound(0); i++)
            {
                for (int j = 0; j <= grid.GetUpperBound(1); j++)
                {
                    grid[i, j] = DotColor.None;
                }
            }
            labelSide0.Text = "";
            labelSide1.Text = "";
            labelTimes1.Text = "局时：600 s";
            labelTimet1.Text = "步时：30 s";
            labelTimes2.Text = "步时：600 s";
            labelTimet2.Text = "局时：30 s";

        }
        
        public void SetLabel(Label label, string str)
        {
            if (label.InvokeRequired)
            {
                this.Invoke(labelDelegate, label, str);
            }
            else
            {
                label.Text = str;
            }
        }
        
        private void SetButton(Button button, bool flag)
        {
            if (button.InvokeRequired)
            {
                this.Invoke(buttonDelegate, button, flag);
            }
            else
            {
                button.Enabled = flag;
                groupBox1.Enabled = flag;
            }
        }
        
        private void SetRadioButton(RadioButton radioButton, bool flag)
        {
            if (radioButton.InvokeRequired)
            {
                this.Invoke(radioButtonDelegate, radioButton, flag);
            }
            else
            {
                radioButton.Checked = flag;
            }
        }
        
        public void SetDot(int i, int j, DotColor dotColor)
        {
            service.AddItemToListBox(string.Format("{0},{1},{2}", i, j, dotColor));
            grid[i, j] = dotColor;
            pictureBox1.Invalidate();
        }
        
        public void Restart(string str)
        {
            MessageBox.Show(str, "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            
            ResetGrid();
            SetButton(buttonStart, true);
        }
        
        private void ResetGrid()
        {
            
            for (int i = 0; i <= grid.GetUpperBound(0); i++)
            {
                for (int j = 0; j <= grid.GetUpperBound(1); j++)
                {
                    grid[i, j] = DotColor.None;
                }
            }
            
            pictureBox1.Invalidate();
        }
        
        public void SetLevel(string ss)
        {
            isReceiveCommand = true;
            switch (ss)
            {
                case "1":
                    SetRadioButton(radioButton1, true);
                    break;
                case "2":
                    SetRadioButton(radioButton2, true);
                    break;
                case "3":
                    SetRadioButton(radioButton3, true);
                    break;
                case "4":
                    SetRadioButton(radioButton4, true);
                    break;
                case "5":
                    SetRadioButton(radioButton5, true);
                    break;
            }
            isReceiveCommand = false;
        }
        
        private void radioButton_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton radiobutton = (RadioButton)sender;
            int level = int.Parse(radiobutton.Name[radiobutton.Name.Length - 1].ToString());
            int timet = level * 200;
            int times = level * 10;
            SetLabel(labelTimet2, "局时：" + timet + "s");
            SetLabel(labelTimes2, "步时：" + times + "s");
            SetLabel(labelTimet1, "局时：" + timet + "s");
            SetLabel(labelTimes1, "步时：" + times + "s");
            if (isReceiveCommand == false)
            {
                
                if (radiobutton.Checked == true)
                {
                    
                    service.SendToServer(string.Format("Level,{0},{1}",tableIndex, level));
                    

                }
            }
        }
        
        private void buttonSend_Click(object sender, EventArgs e)
        {
            
            service.SendToServer(string.Format("Talk,{0},{1}", tableIndex, textBox1.Text));
        }
        
        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                service.SendToServer(string.Format("Talk,{0},{1}", tableIndex, textBox1.Text));
            }
        }
        
        private void buttonHelp_Click(object sender, EventArgs e)
        {
            string str =
                "\n五子棋\n";
            MessageBox.Show(str, "帮助信息");
        }
        
        private void buttonStart_Click(object sender, EventArgs e)
        {
            service.SendToServer(string.Format("Start,{0},{1}", tableIndex, side));
            groupBox1.Enabled = false;
            this.buttonStart.Enabled = false;
        }
        
        private void buttonExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        
        private void FormPlaying_FormClosing(object sender, FormClosingEventArgs e)
        {
            
            service.SendToServer(string.Format("GetUp,{0},{1}", tableIndex, side));
        }
        
        
        
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            int x = (int)e.X / 20;
            int y = (int)e.Y / 20;
            if (isGameStart == false|| grid[x, y] != DotColor.None) return;
            
            
            if (!( x > 15 || y > 15))
            {
                service.SendToServer(string.Format("setDot,{0},{1},{2},{3}", tableIndex, side, x , y ));
                isGameStart = false;
            }
            
        }
        
        public void SetTableSideText(string sideString, string labelSideString, string listBoxString)
        {
            string s = "白方";
            if (sideString == "0")
            {
                s = "黑方：";
            }
            

            if (sideString == side.ToString())
            {
                SetLabel(labelSide1, s + labelSideString);
            }
            else
            {
                SetLabel(labelSide0, s + labelSideString);
            }
            service.AddItemToListBox(listBoxString);
        }

        public void SetTimeText(string sideString, string timet,string times)
        {
            if (sideString == side.ToString())
            {
                isGameStart = true;
                SetLabel(labelTimet2, "局时：" + timet +"s");
                SetLabel(labelTimes2, "步时：" + times + "s");
            }
            else
            {
                isGameStart = false;
                SetLabel(labelTimet1, "局时：" + timet + "s");
                SetLabel(labelTimes1, "步时：" + times + "s");
            }
            
        }



        public void ShowTalk(string talkMan, string str)
        {
            service.AddItemToListBox(string.Format("{0}说：{1}", talkMan, str));
        }
        
        public void ShowMessage(string str)
        {
            service.AddItemToListBox(str);
        }

        public void StopFormPlaying()
        {
            Application.Exit();
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            for (int i = 0; i <= grid.GetUpperBound(0); i++)
                for (int j = 0; j <= grid.GetUpperBound(1); j++)
                {
                    if (grid[i, j] != DotColor.None)
                    {
                        if (grid[i, j] == DotColor.Black)
                        {
                            g.DrawImage(blackBitmap, i * 20, j * 20);
                        }
                        else
                        {
                            g.DrawImage(whiteBitmap, i * 20, j * 20);
                        }
                    }
                }

        }
    }
}
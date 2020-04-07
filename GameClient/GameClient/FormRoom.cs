
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
namespace GameClient
{
    public partial class FormRoom : Form
    {
        private int maxPlayingTables;
        private CheckBox[,] checkBoxGameTables;
        private TcpClient client = null;
        private StreamWriter sw;
        private StreamReader sr;
        private Service service;
        private FormPlaying formPlaying;
        
        private bool normalExit = false;
        
        private bool isReceiveCommand = false;
        
        private int side = -1;
        public FormRoom()
        {
            InitializeComponent();
        }
        private void FormRoom_Load(object sender, EventArgs e)
        {
            
            maxPlayingTables = 0;
            textBoxLocal.ReadOnly = true;
            textBoxServer.ReadOnly = true;
        }
        
        private void buttonConnect_Click(object sender, EventArgs e)
        {
            if (textBoxName.Text != "" && !textBoxName.Text.Contains(","))
            {
                try
                {

                    client = new TcpClient("127.0.0.1", 1995);
                }
                catch
                {
                    MessageBox.Show("与服务器连接失败", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                groupBox1.Visible = true;
                textBoxLocal.Text = client.Client.LocalEndPoint.ToString();
                textBoxServer.Text = client.Client.RemoteEndPoint.ToString();
                buttonConnect.Enabled = false;

                NetworkStream netStream = client.GetStream();
                sr = new StreamReader(netStream, System.Text.Encoding.UTF8);
                sw = new StreamWriter(netStream, System.Text.Encoding.UTF8);
                service = new Service(listBox1, sw);

                service.SendToServer("Login," + textBoxName.Text.Trim());
                Thread threadReceive = new Thread(new ThreadStart(ReceiveData));
                threadReceive.Start();
            }
            else {
                MessageBox.Show("非法用户名！");
                return;
            }
        }
        

        private void ReceiveData()
        {
            bool exitWhile = false;
            while (exitWhile == false)
            {
                string receiveString = null;
                try
                {
                    receiveString = sr.ReadLine();
                }
                catch
                {
                    service.AddItemToListBox("接收数据失败");
                }
                if (receiveString == null)
                {
                    if (normalExit == false)
                    {
                        MessageBox.Show("与服务器失去联系，游戏无法继续！");
                    }
                    if (side != -1)
                    {
                        ExitFormPlaying();
                    }
                    side = -1;
                    normalExit = true;
                    
                    break;
                }
                service.AddItemToListBox("收到：" + receiveString);
                string[] splitString = receiveString.Split(',');
                string command=splitString[0].ToLower();
                switch (command)
                {
                    case "sorry":
                        MessageBox.Show("连接成功，但游戏室人数已满，无法进入。");
                        exitWhile = true;
                        break;
                    case "tables":
                        
                        string s = splitString[1];
                        
                        if (maxPlayingTables == 0)
                        {
                            
                            maxPlayingTables = s.Length / 2;
                            checkBoxGameTables = new CheckBox[maxPlayingTables, 2];
                            isReceiveCommand = true;
                            
                            for (int i = 0; i < maxPlayingTables; i++)
                            {
                                AddCheckBoxToPanel(s, i);
                            }
                            isReceiveCommand = false;
                        }
                        else
                        {
                            isReceiveCommand = true;
                            for (int i = 0; i < maxPlayingTables; i++)
                            {
                                for (int j = 0; j < 2; j++)
                                {
                                    if (s[2 * i + j] == '0')
                                    {
                                        UpdateCheckBox(checkBoxGameTables[i, j], false);
                                    }
                                    else
                                    {
                                        UpdateCheckBox(checkBoxGameTables[i, j], true);
                                    }
                                }
                            }
                            isReceiveCommand = false;
                        }
                        break;
                    case "sitdown":
                        
                        formPlaying.SetTableSideText(splitString[1], splitString[2],string.Format("{0}进入", splitString[2]));
                        break;
                    case "getup":
                        
                        if (side == int.Parse(splitString[1]))
                        {
                            
                            side = -1;
                        }
                        else
                        {
                            
                            formPlaying.SetTableSideText(splitString[1], "",
                                string.Format("{0}退出", splitString[2]));
                            formPlaying.Restart("敌人逃跑了，我方胜利了");
                        }
                        break;
                    case "lost":
                        
                        formPlaying.SetTableSideText(splitString[1], "",
                            string.Format("[{0}]与服务器失去联系", splitString[2]));
                        formPlaying.Restart("对家与服务器失去联系，游戏无法继续");
                        break;
                    case "talk":
                        
                        if (formPlaying != null)
                        {
                            
                            formPlaying.ShowTalk(splitString[1],
                                receiveString.Substring(splitString[0].Length +
                                splitString[1].Length + splitString[2].Length + 3));
                        }
                        break;
                    case "message":
                        
                        formPlaying.ShowMessage(splitString[1]);
                        break;
                    case "level":
                        
                        formPlaying.SetLevel(splitString[2]);
                        break;
                    case "setdot":
                        
                        formPlaying.SetDot(
                            int.Parse(splitString[1]),
                            int.Parse(splitString[2]),
                            (DotColor)int.Parse(splitString[3]));
                        break;

                    case "settime":
                        formPlaying.SetTimeText(splitString[1],splitString[2],splitString[3]);
                        break;

                    case "win":
                        string winner = "";
                        if ((DotColor)int.Parse(splitString[1]) == DotColor.Black)
                        {
                            winner = "黑方胜利！";
                        }
                        else
                        {
                            winner = "白方胜利！";
                        }
                        formPlaying.ShowMessage(winner);
                        formPlaying.Restart(winner);
                        break;
                }
            }
           
            Application.Exit();
        }
        delegate void ExitFormPlayingDelegate();
        
        private void ExitFormPlaying()
        {
            if (formPlaying.InvokeRequired == true)
            {
                ExitFormPlayingDelegate d = new ExitFormPlayingDelegate(ExitFormPlaying);
                this.Invoke(d);
            }
            else
            {
                formPlaying.Close();
            }
        }
        delegate void PanelDelegate(string s, int i);
        
        private void AddCheckBoxToPanel(string s, int i)
        {
            if (panel1.InvokeRequired == true)
            {
                PanelDelegate d = AddCheckBoxToPanel;
                this.Invoke(d, s, i);
            }
            else
            {
                Label label = new Label();
                label.Location = new Point(10, 15 + i * 30);
                label.Text = string.Format("第{0}桌：", i + 1);
                label.Width = 70;
                this.panel1.Controls.Add(label);
                CreateCheckBox(i, 0, s, "黑方");
                CreateCheckBox(i, 1, s, "白方");
            }
        }
        delegate void CheckBoxDelegate(CheckBox checkbox, bool isChecked);
        
        private void UpdateCheckBox(CheckBox checkbox, bool isChecked)
        {
            if (checkbox.InvokeRequired == true)
            {
                CheckBoxDelegate d = UpdateCheckBox;
                this.Invoke(d, checkbox, isChecked);
            }
            else
            {
                if (side == -1)
                {
                    checkbox.Enabled = !isChecked;
                }
                else
                {
                    
                    checkbox.Enabled = false;
                }
                
                checkbox.Checked = isChecked;
            }
        }
        
        private void CreateCheckBox(int i, int j, string s, string text)
        {
            int x = j == 0 ? 100 : 200;
            checkBoxGameTables[i, j] = new CheckBox();
            checkBoxGameTables[i, j].Name = string.Format("check{0:0000}{1:0000}", i, j);
            checkBoxGameTables[i, j].Width = 60;
            checkBoxGameTables[i, j].Location = new Point(x, 10 + i * 30);
            checkBoxGameTables[i, j].Text = text;
            checkBoxGameTables[i, j].TextAlign = ContentAlignment.MiddleLeft;
            if (s[2 * i + j] == '1')
            {
                
                checkBoxGameTables[i, j].Enabled = false;
                checkBoxGameTables[i, j].Checked = true;
            }
            else
            {
                
                checkBoxGameTables[i, j].Enabled = true;
                checkBoxGameTables[i, j].Checked = false;
            }
            this.panel1.Controls.Add(checkBoxGameTables[i, j]);
            checkBoxGameTables[i, j].CheckedChanged +=
                new EventHandler(checkBox_CheckedChanged);
        }
        
        private void checkBox_CheckedChanged(object sender, EventArgs e)
        {
            
            if (isReceiveCommand == true)
            {
                return;
            }
            CheckBox checkbox = (CheckBox)sender;
           
            if (checkbox.Checked == true)
            {
                int i = int.Parse(checkbox.Name.Substring(5, 4));
                int j = int.Parse(checkbox.Name.Substring(9, 4));
                side = j;
                
                service.SendToServer(string.Format("SitDown,{0},{1}", i, j));
                formPlaying = new FormPlaying(i, j, sw);
                formPlaying.Show();
            }
        }
        
        private void FormRoom_FormClosing(object sender, FormClosingEventArgs e)
        {
            
            if (client != null)
            {
                
                if (side != -1)
                {
                    MessageBox.Show("请先从游戏桌站起，返回游戏室，然后再退出");
                    e.Cancel = true;
                }
                else
                {
                    
                    if (normalExit == false)
                    {
                        normalExit = true;
                        
                        service.SendToServer("Logout");
                    }
                    
                    client.Close();       
                }
            }
        }
    }
}

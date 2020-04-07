//-------------FormServer.cs------------------//
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
namespace GameServer
{
    public partial class FormServer : Form
    {
        
        private int maxUsers;
        List<User> userList = new List<User>();
        private int maxTables;
        private GameTable[] gameTable;
        IPAddress localAddress;
        private int port = 1995;
        private TcpListener myListener;
        private Service service;
        public FormServer()
        {
            InitializeComponent();
            service = new Service(listBox1);
            localAddress = IPAddress.Parse("127.0.0.1");
        }
        
        private void FormServer_Load(object sender, EventArgs e)
        {
            listBox1.HorizontalScrollbar = true;
            buttonStop.Enabled = false;
        }
        
        private void buttonStart_Click(object sender, EventArgs e)
        {
            if (int.TryParse(textBoxMaxTables.Text, out maxTables) == false
                || int.TryParse(textBoxMaxUsers.Text, out maxUsers) == false)
            {
                MessageBox.Show("请输入在规定范围内的正整数");
                return;
            }
            if (maxUsers < 1 || maxUsers > 300)
            {
                MessageBox.Show("允许进入的人数只能在1-300之间");
                return;
            }
            if (maxTables < 1 || maxTables > 100)
            {
                MessageBox.Show("允许的桌数只能在1-100之间");
                return;
            }
            textBoxMaxUsers.Enabled = false;
            textBoxMaxTables.Enabled = false;
            gameTable = new GameTable[maxTables];
            for (int i = 0; i < maxTables; i++)
            {
                gameTable[i] = new GameTable(listBox1);
            }
            myListener = new TcpListener(localAddress, port);
            myListener.Start();
            service.AddItem(string.Format("开始在{0}:{1}监听客户连接", localAddress, port));
            ThreadStart ts = new ThreadStart(ListenClientConnect);
            Thread myThread = new Thread(ts);
            myThread.Start();
            buttonStart.Enabled = false;
            buttonStop.Enabled = true;
        }
        
        private void buttonStop_Click(object sender, EventArgs e)
        {
            
            for (int i = 0; i < maxTables; i++)
            {
                gameTable[i].StopTimer();
            }
            service.AddItem(string.Format("目前连接用户数：{0}", userList.Count));
            service.AddItem("开始停止服务，并依次使用户退出!");
            for (int i = 0; i < userList.Count; i++)
            {
                userList[i].client.Close();
            }
            
            myListener.Stop();
            buttonStart.Enabled = true;
            buttonStop.Enabled = false;
            textBoxMaxUsers.Enabled = true;
            textBoxMaxTables.Enabled = true;
        }
       
        private void ListenClientConnect()
        {
            while (true)
            {
                TcpClient newClient = null;
                try
                {
                   
                    newClient = myListener.AcceptTcpClient();
                }
                catch
                {
                    
                    break;
                }
                
                ParameterizedThreadStart pts = new ParameterizedThreadStart(ReceiveData);
                Thread threadReceive = new Thread(pts);
                User user = new User(newClient);
                threadReceive.Start(user);
                userList.Add(user);
                service.AddItem(string.Format("{0}进入", newClient.Client.RemoteEndPoint));
                service.AddItem(string.Format("当前连接用户数：{0}", userList.Count));
            }
        }
        
        private void ReceiveData(object obj)
        {
            User user = (User)obj;
            TcpClient client = user.client;
            
            bool normalExit = false;
            
            bool exitWhile = false;
            while (exitWhile == false)
            {
                string receiveString = null;
                try
                {
                    receiveString = user.sr.ReadLine();
                }
                catch
                {
                    
                    service.AddItem("接收数据失败");
                }
                
                if (receiveString == null)
                {
                    if (normalExit == false)
                    {
                        
                        if (client.Connected == true)
                        {
                            service.AddItem(string.Format(
                                "与{0}失去联系，已终止接收该用户信息",
                                 client.Client.RemoteEndPoint));
                        }
                        
                        RemoveClientfromPlayer(user);
                    }
                    
                    break;
                }
                service.AddItem(string.Format("来自{0}：{1}", user.userName, receiveString));
                string[] splitString = receiveString.Split(',');
                int tableIndex = -1;    
                int side = -1;          
                int anotherSide = -1;   
                string sendString = "";
                string command = splitString[0].ToLower();
                switch (command)
                {
                    case "login":
                        
                        if (userList.Count > maxUsers)
                        {
                            sendString = "Sorry";
                            service.SendToOne(user, sendString);
                            service.AddItem("人数已满，拒绝" +
                                splitString[1] + "进入游戏室");
                            exitWhile = true;
                        }
                        else
                        {
                            
                            user.userName = string.Format("[{0}--{1}]", splitString[1],
                                client.Client.RemoteEndPoint);
                            
                            sendString = "Tables," + this.GetOnlineString();
                            service.SendToOne(user, sendString);
                        }
                        break;
                    case "logout":
                        
                        service.AddItem(string.Format("{0}退出游戏室", user.userName));
                        normalExit = true;
                        exitWhile = true;
                        break;
                    case "sitdown":
                        
                        tableIndex = int.Parse(splitString[1]);
                        side = int.Parse(splitString[2]);
                        gameTable[tableIndex].gamePlayer[side].user = user;
                        gameTable[tableIndex].gamePlayer[side].someone = true;
                        service.AddItem(string.Format(
                            "{0}在第{1}桌第{2}座入座",
                            user.userName, tableIndex + 1, side + 1));
                        
                        anotherSide = (side + 1) % 2;
                        
                        if (gameTable[tableIndex].gamePlayer[anotherSide].someone == true)
                        {
                            
                            sendString = string.Format("SitDown,{0},{1}", anotherSide,
                              gameTable[tableIndex].gamePlayer[anotherSide].user.userName);
                            service.SendToOne(user, sendString);
                        }
                        
                        sendString = string.Format("SitDown,{0},{1}", side, user.userName);
                        service.SendToBoth(gameTable[tableIndex], sendString);
                        
                        service.SendToAll(userList, "Tables," + this.GetOnlineString());
                        break;
                    case "getup":
                        
                        tableIndex = int.Parse(splitString[1]);
                        side = int.Parse(splitString[2]);
                        service.AddItem(
                             string.Format("{0}离座,返回游戏室", user.userName));
                        gameTable[tableIndex].StopTimer();
                        
                        service.SendToBoth(gameTable[tableIndex],
                            string.Format("GetUp,{0},{1}", side, user.userName));
                        
                        gameTable[tableIndex].gamePlayer[side].someone = false;
                        gameTable[tableIndex].gamePlayer[side].started = false;
                        
                        anotherSide = (side + 1) % 2;
                        if (gameTable[tableIndex].gamePlayer[anotherSide].someone == true)
                        {
                            gameTable[tableIndex].gamePlayer[anotherSide].started = false;
                        }
                        
                        service.SendToAll(userList, "Tables," + this.GetOnlineString());
                        break;
                    case "level":
                        
                        tableIndex = int.Parse(splitString[1]);
                        gameTable[tableIndex].SetTimerLevel(int.Parse(splitString[2]));
                        service.SendToBoth(gameTable[tableIndex], receiveString);
                        break;
                    case "talk":
                        
                        tableIndex = int.Parse(splitString[1]);
                        
                        sendString = string.Format("Talk,{0},{1}", user.userName,
                            receiveString.Substring(splitString[0].Length +
                            splitString[1].Length));
                        service.SendToBoth(gameTable[tableIndex], sendString);
                        break;
                    case "start":
                        
                        tableIndex = int.Parse(splitString[1]);
                        side = int.Parse(splitString[2]);
                        gameTable[tableIndex].gamePlayer[side].started = true;
                        if (side == 0)
                        {
                            anotherSide = 1;
                            sendString = "Message,黑方已开始。";
                        }
                        else
                        {
                            anotherSide = 0;
                            sendString = "Message,白方已开始。";
                        }
                        service.SendToBoth(gameTable[tableIndex], sendString);
                        if (gameTable[tableIndex].gamePlayer[anotherSide].started == true)
                        {
                            gameTable[tableIndex].ResetGrid();
                            gameTable[tableIndex].StartTimer();
                        }
                        break;
                    case "setdot":
                        tableIndex = int.Parse(splitString[1]);
                        side = int.Parse(splitString[2]);
                        int xi = int.Parse(splitString[3]);
                        int xj = int.Parse(splitString[4]);
                        gameTable[tableIndex].SetDot(xi, xj, side);
                        break;
                    default:
                        service.SendToAll(userList, "什么意思啊：" + receiveString);
                        break;
                }
            }
            userList.Remove(user);
            client.Close();
            service.AddItem(string.Format("有一个退出，剩余连接用户数：{0}", userList.Count));
        }
       
        private void RemoveClientfromPlayer(User user)
        {
            for (int i = 0; i < gameTable.Length; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    if (gameTable[i].gamePlayer[j].user != null)
                    {
                        
                        if (gameTable[i].gamePlayer[j].user == user)
                        {
                            StopPlayer(i, j);
                            return;
                        }
                    }
                }
            }
        }
        

        private void StopPlayer(int i, int j)
        {
            gameTable[i].StopTimer();
            gameTable[i].gamePlayer[j].someone = false;
            gameTable[i].gamePlayer[j].started = false;
            
            int otherSide = (j + 1) % 2;
            if (gameTable[i].gamePlayer[otherSide].someone == true)
            {
                gameTable[i].gamePlayer[otherSide].started = false;
                
                if (gameTable[i].gamePlayer[otherSide].user.client.Connected == true)
                {
                    
                    service.SendToOne(gameTable[i].gamePlayer[otherSide].user,
                        string.Format("Lost,{0},{1}",
                         j, gameTable[i].gamePlayer[j].user.userName));
                }
            }
        }
        
        
        private string GetOnlineString()
        {
            string str = "";
            for (int i = 0; i < gameTable.Length; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    str += gameTable[i].gamePlayer[j].someone == true ? "1" : "0";
                }
            }
            return str;
        }
        
        private void FormDDServer_FormClosing(object sender, FormClosingEventArgs e)
        {
            
            if (myListener != null)
            {
                buttonStop_Click(null, null);
            }
        }    
    }
}
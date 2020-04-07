//-------------------GameTable.cs-----------------//
using System;
using System.Timers;
using System.Windows.Forms;
namespace GameServer
{
    class GameTable
    {
        private const int None = -1;
        private const int Black = 0;
        private const int White = 1;
        public Player[] gamePlayer;
        private int[,] grid = new int[16, 16];
        private System.Timers.Timer timer;
        private int NextdotColor = 0;
        private int defaultTimet = 10 * 60;
        private int defaultTimes = 30;
        private int times;
        private ListBox listbox;
        Random rnd = new Random();
        Service service;
        public GameTable(ListBox listbox)
        {
            gamePlayer = new Player[2];
            gamePlayer[0] = new Player();
            gamePlayer[1] = new Player();
            timer = new System.Timers.Timer();
            timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
            timer.Enabled = false;
            timer.Interval = 1000;
            this.listbox = listbox;
            service = new Service(listbox);
            ResetGrid();
        }

        

        public void ResetGrid()
        {
            for (int i = 0; i <= grid.GetUpperBound(0); i++)
            {
                for (int j = 0; j <= grid.GetUpperBound(1); j++)
                {
                    grid[i, j] = None;
                }
            }
            gamePlayer[0].timet= gamePlayer[1].timet = defaultTimet;
            times = defaultTimes;
            
            
        }

        public void StartTimer()
        {
            timer.Start();
        }

        public void StopTimer()
        {
            timer.Stop();
        }

        public void SetTimerLevel(int level)
        {
            defaultTimet = level * 200;
            defaultTimes = level * 10;
        }

        private void timer_Elapsed(object sender, EventArgs e)
        {
            if (gamePlayer[NextdotColor].timet > 0 && times > 0)
            {
                gamePlayer[NextdotColor].timet--;
                times--;
            }
            else
            {
                ShowWin(NextdotColor==Black?White:Black);
            }
            service.SendToBoth(this, string.Format("setTime,{0},{1},{2}",NextdotColor, gamePlayer[NextdotColor].timet,times));
        }

        public void SetDot(int x, int y, int dotColor)
        {

            NextdotColor = dotColor == Black ? White : Black;

            grid[x, y] = dotColor;
            int [,]diret={{-1,-1},{1,-1},{-1,0},{0,-1}};
            service.SendToBoth(this, string.Format("SetDot,{0},{1},{2}", x, y, dotColor));
            for(int i = 0; i < 4; i++)
            {
                int cnt = 1;
                for (int j = 0; j < 2; j++)
                {
                    int[] tmp = { x, y };
                    int dx = diret[i, 0], dy = diret[i, 1];
                    if (j == 1) {
                        dx = -dx; dy = -dy;
                    }
                    while (x > 0 && y > 0 && x < 15 && y < 15)
                    {
                        if (grid[tmp[0] + dx, tmp[1] + dy] == dotColor)
                        {
                            cnt++;
                            tmp[0] += dx; tmp[1] += dy;
                        }
                        else break;
                    }
                }
                if (cnt >= 5) { ShowWin(dotColor); break; }
            }
            times = defaultTimes;
                
        }
        
        private void ShowWin(int dotColor)
        {
            timer.Enabled = false;
            gamePlayer[0].started = false;
            gamePlayer[1].started = false;
            this.ResetGrid();
            service.SendToBoth(this, string.Format("Win,{0}",dotColor));
        }
       
        
    }
}

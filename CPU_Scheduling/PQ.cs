using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.Eventing;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Timers;
using System.Diagnostics;

namespace CPU_Scheduling
{
    public partial class PQ : Form
    {
        public PQ()
        {
            InitializeComponent();
            picBusy.Hide();
            picWaiting.Show();
        }


        public bool ran = false;

        public int Numpro;

        public int Max;

        public int Min;

        private int currentTime = -1;

        private int totalTurnarroundTime = 0;

        private int totalWaitingTime = 0;

        private bool enabled = false;

        Random rand = new Random();
        private int Normal(double mean, double stdDev, int max, int min)
        {
            int k = max + 1;
            while (k > max || k < min)
            {
                double u1 = 1.0 - rand.NextDouble(); //uniform(0,1] random doubles
                double u2 = 1.0 - rand.NextDouble();
                double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                             Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)


                double randNormal = mean + stdDev * randStdNormal; //random normal(mean,stdDev^2)
                k = (int)Math.Floor(randNormal);
            }

            return k;
        }


        ProcessPQ[] prolist;

        private List<ProcessPQ> ArrivalQueue = new List<ProcessPQ>();

        private List<ProcessPQ> ReadyQueue = new List<ProcessPQ>();

        ProcessPQ runProcess = null;

        ProgressBar bar = new ProgressBar();

        int[] Remain;

        public void populate()
        {
            prolist = new ProcessPQ[Numpro];
            Remain = new int[Numpro];

            for (int i = 0; i < Numpro; ++i)
            {
                prolist[i] = new ProcessPQ();

                double mean = (double)(Max + Min) / (double)2;
                double stdDev = (double)(Max - Min) / (double)6;
                prolist[i].Num = i;
                prolist[i].Arrival = (ran) ? Normal(mean, stdDev, Max, Min) : 0;
                prolist[i].Burst = (ran) ? Normal(mean, stdDev, Max, Min) : 1;
                prolist[i].Prior = (ran) ? Normal(mean, stdDev, Max, Min) : 0;

                flowLayoutPanel1.Controls.Add(prolist[i]);
            }
        }

        private void EnqueueByArrival(ProcessPQ process, List<ProcessPQ> queue)
        {
            if (queue.Count > 0)
            {
                for (int i = 0; i < queue.Count; ++i)
                {
                    if (process.Arrival < queue[i].Arrival)
                    {
                        queue.Insert(i, process);
                        return;
                    }
                }
            }
            queue.Add(process);
        }

        private void EnqueueByPriority(ProcessPQ process, List<ProcessPQ> queue)
        {
            if (queue.Count > 0)
            {
                for (int i = 0; i < queue.Count; ++i)
                {
                    if (process.Prior > queue[i].Prior)
                    {
                        queue.Insert(i, process);
                        return;
                    }
                }
            }
            queue.Add(process);
        }

        private ProcessPQ Dequeue(List<ProcessPQ> queue)
        {
            if (queue.Count > 0)
            {
                ProcessPQ process = queue[0];
                queue.RemoveAt(0);
                return process;
            }
            else return null;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (enabled) return;

            enabled = true;

            flowLayoutPanel1.Enabled = false;
            foreach (ProcessPQ process in prolist)
            {
                int temp = process.Arrival;
                Remain[process.Num] = process.Burst;
                EnqueueByArrival(process, ArrivalQueue);
            }
            timer1.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            currentTime++;

            while (ArrivalQueue.Count > 0 && ArrivalQueue[0].Arrival == currentTime)
            {
                ProcessPQ temp = Dequeue(ArrivalQueue);
                EnqueueByPriority(temp, ReadyQueue);
            }

            if (runProcess != null && Remain[runProcess.Num] == 0)
            {
                runProcess.setWait(currentTime - runProcess.Burst - runProcess.Arrival);
                totalWaitingTime += currentTime - runProcess.Burst - runProcess.Arrival;
                totalTurnarroundTime += currentTime - runProcess.Arrival;
                runProcess = null;
            }

            if (runProcess == null && ReadyQueue.Count > 0)
            {
                runProcess = Dequeue(ReadyQueue);
                runProcess.proStatus.Maximum = runProcess.Burst;

                int i = tableLayoutPanel1.ColumnCount++;

                Label k1 = new Label();
                k1.AutoSize = true;
                k1.Text = "P" + runProcess.Num.ToString();
                tableLayoutPanel1.Controls.Add(k1, i - 1, 0);

                bar = new ProgressBar();
                bar.Width = 25 * runProcess.Burst;
                bar.Maximum = runProcess.Burst;
                tableLayoutPanel1.Controls.Add(bar, i - 1, 1);

                Label k2 = new Label();
                k2.AutoSize = true;
                k2.Text = currentTime.ToString();
                tableLayoutPanel1.Controls.Add(k2, i - 1, 2);
            }

            if (runProcess != null)
            {
                Remain[runProcess.Num] -= 1;
                runProcess.proStatus.Value += 1;
                bar.Value += 1;
            }

            if (runProcess == null && ArrivalQueue.Count == 0 && ReadyQueue.Count == 0)
            {
                timer1.Stop();
                lbWaitT.Text = Math.Round((double)totalWaitingTime / (double)Numpro, 2).ToString();
                lbTurn.Text = Math.Round((double)totalTurnarroundTime / (double)Numpro, 2).ToString();
            }

            lbClock.Text = currentTime.ToString();
            lbQueue.Text = ReadyQueue.Count.ToString();
            lbStatus.Text = (runProcess == null) ? "Idle" : "Busy";
            lbCurrent.Text = "P" + ((runProcess == null) ? "" : runProcess.Num.ToString());
            String waitingProcessPQes = "";
            foreach (ProcessPQ process in ReadyQueue)
            {
                waitingProcessPQes += ("P" + process.Num.ToString() + "|");
            }
            lbWait.Text = waitingProcessPQes;

            if (runProcess == null) { picBusy.Hide(); picWaiting.Show(); }
            else { picBusy.Show(); picWaiting.Hide(); }
        }

        private void btnRestart_Click(object sender, EventArgs e)
        {
            enabled = false;
            flowLayoutPanel1.Enabled = true;
            picBusy.Hide(); picWaiting.Show();
            lbClock.Text = "0";
            lbQueue.Text = "0";
            lbStatus.Text = "Idle";
            lbCurrent.Text = "P";
            lbTurn.Text = "0";
            lbWaitT.Text = "0";

            flowLayoutPanel1.Controls.Clear();
            for (int i = tableLayoutPanel1.Controls.Count - 1; i >= 0; --i)
                tableLayoutPanel1.Controls[i].Dispose();

            tableLayoutPanel1.Controls.Clear();
            tableLayoutPanel1.ColumnStyles.Clear();
            tableLayoutPanel1.ColumnCount = 1;

            for (int i = 0; i < Numpro; i++)
            {
                prolist[i].stop();
                flowLayoutPanel1.Controls.Add(prolist[i]);
            }

            timer1.Stop();
            ArrivalQueue = new List<ProcessPQ>();
            ReadyQueue = new List<ProcessPQ>();
            currentTime = -1;
            bar = new ProgressBar();
            runProcess = null;
            totalTurnarroundTime = 0;
            totalWaitingTime = 0;
        }

        private void PQ_Load(object sender, EventArgs e)
        {
            this.FormClosed += new FormClosedEventHandler(formclose);
        }
        private void formclose(object sender, EventArgs e)
        {
            EntryForm k = new EntryForm();
            k.Show();
        }
    }
}

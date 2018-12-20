using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using QR.Common.Http;
using QR.Common.Tools;

namespace Orderbook
{
    public partial class Form1 : Form
    {
        public static decimal min = 0;
        public static int count = 0;


        enum OrderSide
        {
            Buy = 1,
            Sell = -1
        }

        enum OrderStatus
        {
            Working = 1,
            Filled = 2,
            Canceled = 3
        }

        private class OrderUpdate
        {
            public string Price { get; set; }
            public string Quantity { get; set; }
        }


        public Form1()
        {
            InitializeComponent();
            InitDataTableObject();




            this._dgvOrderbook.DataSource = this.sourceData;
            //_incomingOrderThread = new Thread(InComingWorkerProcessor);
            //_incomingOrderThread.Start();

            ProgressBar.CheckForIllegalCrossThreadCalls = false;
            tm.Interval = 1;
            tm.Tick += new EventHandler(tm_Tick);
            tm.Start();
        }

        /// <summary>
        /// 初始化DataTable结构
        /// </summary>
        private void InitDataTableObject()
        {

            sourceData.Columns.Add("Price");
            sourceData.Columns.Add("Quantity");

        }
        private OrderUpdate xx()
        {
            HttpWebPage openWeb = new HttpWebPage();
            string activeUrl = "http://www.mysdic.com/join/invite.html";
            string tempHtml = openWeb.DoGet(activeUrl);
            string count1 = tempHtml.Abstract("fnt_24", "详细内容");
            if (count1.Contains("招聘"))
            {
                List<string> ss = RegexHelper.GetMatchsList(@"\d{4}年\d{2}月\d{2}日", count1);
                if (ss.Count > 0)
                {
                    DateTime start = this.dateTimePicker1.Value;
                    DateTime end = new DateTime(
                        int.Parse(ss[0].Abstract("", "年")),
                        int.Parse(ss[0].Abstract("年", "月")),
                        int.Parse(ss[0].Abstract("月", "日")));
                    TimeSpan ts = end - start;
                    if (ts.TotalSeconds > 0)
                    {
                        this.dateTimePicker1.Value = end;
                        OrderUpdate record = new OrderUpdate();
                        record.Price = ss[0];
                        record.Quantity = count1.Abstract("blank\">​", "</a>");
                        return record;
                    }
                }

            }
            return null;
        }

        private Thread _incomingOrderThread;
        DataTable sourceData = new DataTable("Order");
        //更新sourceData的线程
        private Thread _updateUIThread;
        private System.Windows.Forms.Timer tm = new System.Windows.Forms.Timer();
        AutoResetEvent autoEvent = new AutoResetEvent(false);
        Queue<DataRow> query = new Queue<DataRow>();

        private void InComingWorkerProcessor()
        {
            while (true)
            {
                OrderUpdate update = xx();
                if (update != null)
                {
                    OnOrderUpdate(update);
                }
                this.toolStripStatusLabel1.Text = "第" + count + "次访问网站";
                count++;
                Thread.Sleep((int)(this.numericUpDown1.Value * 1000 * 60));
            }
        }

        void tm_Tick(object sender, EventArgs e)
        {
            HttpWebPage openWeb = new HttpWebPage();
            //openWeb.DoGet("");
            autoEvent.Set();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "暂停")
            {
                //暂停 grid update
                tm.Stop();
                if (this._incomingOrderThread.IsAlive)
                {
                    this._incomingOrderThread.Abort();
                }
                button1.Text = "开始";
            }
            else if (button1.Text == "开始")
            {

                //继续 grid update
                tm.Start();
                button1.Text = "暂停";
                _incomingOrderThread = new Thread(InComingWorkerProcessor);
                _incomingOrderThread.Start();
            }
        }

        private void OnOrderUpdate(OrderUpdate update)
        {
            DataRow row = OrderObjToDataRow(update);
            sourceData.Rows.Add(row);
        }
        private DataRow OrderObjToDataRow(OrderUpdate order)
        {
            DataRow row = sourceData.NewRow();

            row["Price"] = order.Price;
            row["Quantity"] = order.Quantity;

            return row;
        }

        /// <summary>
        /// 数据更新和处理
        /// </summary>
        /// <param name="table"></param>
        /// <param name="row"></param>
        private void DataRowDataDeal()
        {
            while (true)
            {
                autoEvent.WaitOne();
                if (query.Count > 0)
                {
                    bool containsFlag = false;
                    DataRow row = query.Dequeue();
                    sourceData.Rows.Add(row);
                }
            }
        }



        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (this._incomingOrderThread != null && this._incomingOrderThread.IsAlive)
            {
                this._incomingOrderThread.Abort();
            }
            if (this._updateUIThread != null && this._updateUIThread.IsAlive)
            {
                this._updateUIThread.Abort();
            }
        }

    }
}

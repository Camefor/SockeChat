using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SocketClient {
    public partial class FrmClient : Form {
        public FrmClient() {
            InitializeComponent();
        }

        /// <summary>
        /// 定义回调
        /// </summary>
        /// <param name="strValue"></param>
        private delegate void SetTextCallBack(string strValue);

        /// <summary>
        /// 声明
        /// </summary>
        private SetTextCallBack setTextCallBack;


        /// <summary>
        /// 定义接收服务端发送的消息回调
        /// </summary>
        /// <param name="strMsg"></param>
        private delegate void ReceiveMsgCallBack(string strMsg);


        /// <summary>
        /// 声明接收服务端发送的消息回调
        /// </summary>
        private ReceiveMsgCallBack receiveMsgCallBack;


        /// <summary>
        /// 创建连接的Socket
        /// </summary>
        Socket SocketSend;


        /// <summary>
        /// 创建接收客户端发送的消息的线程
        /// </summary>
        Thread threadReceive;

        private void btn_Connect_Click(object sender, EventArgs e) {
            try {
                SocketSend = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPAddress iP = IPAddress.Parse(txt_IP.Text.Trim());
                int port = Convert.ToInt32(txt_Port.Text.Trim());
                SocketSend.Connect(iP, port);
                //实例化回调
                setTextCallBack = new SetTextCallBack(SetValue);
                receiveMsgCallBack = new ReceiveMsgCallBack(SetValue);
                txt_Log.Invoke(setTextCallBack, "连接成功");

                //开启一个新的线程不停的接收服务器发送的消息
                threadReceive = new Thread(new ThreadStart(Receive));
                threadReceive.IsBackground = true;
                threadReceive.Start();


            } catch (Exception ex) {
                MessageBox.Show("连接服务器出错:" + ex.Message);
            }
        }

        /// <summary>
        /// 接收服务器发送的消息
        /// </summary>
        private void Receive() {
            try {
                while (true) {
                    byte[] buffer = new byte[2048];
                    //实际接收到的字节数
                    int r = SocketSend.Receive(buffer);
                    if (r == 0) {
                        break;
                    } else {
                        if (buffer[0] == 0) {
                            //标识
                            string str = Encoding.Default.GetString(buffer, 1, r - 1);
                            txt_Log.Invoke(receiveMsgCallBack, "接收远程服务器:" + SocketSend.RemoteEndPoint + "发送的信息:" + str);
                        }

                        //表示发送的是文件
                        if (buffer[0] == 1) {
                            SaveFileDialog sfd = new SaveFileDialog();
                            sfd.InitialDirectory = @"";
                            sfd.Title = "请选择要保存的文件";
                            sfd.Filter = "所有文件|*.*";
                            sfd.ShowDialog(this);

                            string strPath = sfd.FileName;
                            using (FileStream fsWrite = new FileStream(strPath, FileMode.OpenOrCreate, FileAccess.Write)) {
                                fsWrite.Write(buffer, 1, r - 1);
                            }

                            MessageBox.Show("保存文件成功");
                        }

                    }

                }
            } catch (Exception ex) {
                MessageBox.Show("接收服务端发送的消息出错:" + ex.Message);
            }
        }

        private void SetValue(string strValue) {

            txt_Log.AppendText(strValue + "\r\n");
        }

        /// <summary>
        /// 客户端给服务器发送消息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Send_Click(object sender, EventArgs e) {
            try {
                string strMsg = txt_Msg.Text.Trim();
                byte[] buffer = new byte[2048];
                buffer = Encoding.Default.GetBytes(strMsg);
                int receive = SocketSend.Send(buffer);
            } catch (Exception ex) {
                MessageBox.Show("发送消息出错:" + ex.Message);
            }
        }

        private void FrmClient_Load(object sender, EventArgs e) {
            Control.CheckForIllegalCrossThreadCalls = false;
            txt_IP.Text = "192.168.2.6";
            txt_Port.Text = "4521";
        }

        private void btn_CloseConnect_Click(object sender, EventArgs e) {
            try {
                SocketSend.Close();
                threadReceive.Abort();
            } catch (Exception) {
            }
        }
    }
}

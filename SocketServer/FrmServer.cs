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

namespace SocketServer {
    public partial class FrmServer : Form {
        public FrmServer() {
            InitializeComponent();
        }

        /// <summary>
        /// 定义回调:解决跨线程访问问题
        /// </summary>
        /// <param name="strValue"></param>
        private delegate void SetTextValueCallBack(string strValue);


        /// <summary>
        /// 定义接收客户端发送消息的回调
        /// </summary>
        /// <param name="strReceive"></param>
        private delegate void ReceiveMsgCallBack(string strReceive);


        /// <summary>
        /// 声明回调 发送消息
        /// </summary>
        private SetTextValueCallBack SetCallBack;

        /// <summary>
        /// 声明回调 接收客户端发送的消息
        /// </summary>
        private ReceiveMsgCallBack ReceiveCallBack;


        /// <summary>
        /// 定义回调 给Combox控件添加元素
        /// </summary>
        /// <param name="strItem"></param>
        private delegate void SetCmbItemCallBack(string strItem);


        /// <summary>
        /// 声明回调 给Combox控件添加元素
        /// </summary>
        private SetCmbItemCallBack SetCmbCallBack;


        /// <summary>
        /// 定义回调 发送文件
        /// </summary>
        /// <param name="bf"></param>
        private delegate void SendFileCallBack(byte[] bf);

        private SendFileCallBack SendCallBack;


        /// <summary>
        /// 用于通信的Socket
        /// </summary>
        private Socket SocketSend;


        /// <summary>
        /// 用于监听的Socket
        /// </summary>
        private Socket SocketWatch;

        /// <summary>
        /// 将远程连接的客户端的IP地址和Socket存入集合中（Combox中使用,指定客户端发送文件,消息）
        /// </summary>
        Dictionary<string, Socket> DisSocket = new Dictionary<string, Socket>();


        /// <summary>
        /// 创建监听连接的线程
        /// </summary>
        private Thread AcceptSocketThread;

        /// <summary>
        /// 接收客户端发送消息的线程
        /// </summary>
        private Thread threadReceive;


        /// <summary>
        /// 点击开始监听按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Start_Click(object sender, EventArgs e) {
            //当点击开始 监听时 在服务器端创建一个负责监听IP地址和端口号的Socket
            SocketWatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //获取IP地址
            IPAddress ip = IPAddress.Parse(txt_IP.Text.Trim());
            //端口
            int port = Convert.ToInt32(txt_Port.Text.Trim());

            //终结点：
            //在Internet中，TCP/IP 使用套接字(一个网络地址和一个服务端口号)来唯一标识设备。网络地址(IP)标识网络上的特定设备；端口号(Port)标识要连接到的该设备上的特定服务。
            //网络地址和服务端口的组合称为终结点，在 .NET 框架中正是由 EndPoint 类表示这个终结点，它提供表示网络资源或服务的抽象，用以标志网络地址等信息。
            //.Net同时也为每个受支持的地址族定义了 EndPoint 的子代；对于 IP 地址族，该类为 IPEndPoint。IPEndPoint 类包含应用程序连接到主机上的服务所需的主机和端口信息。
            /**
作者：UnityAsk
链接：https://www.jianshu.com/p/ffefd038ba11
来源：简书
著作权归作者所有。商业转载请联系作者获得授权，非商业转载请注明出处。
             * **/
            //终结点
            IPEndPoint point = new IPEndPoint(ip, port);

            //绑定IP地址和端口号
            SocketWatch.Bind(point);
            txt_Log.AppendText("监听成功" + "\r\n");
            //开始监听：设置最大可以同时连接多少个请求
            SocketWatch.Listen(10);

            //实例化回调
            SetCallBack = new SetTextValueCallBack(SetTextValue);
            ReceiveCallBack = new ReceiveMsgCallBack(ReceiveMsg);
            SetCmbCallBack = new SetCmbItemCallBack(AddCmbItem);
            SendCallBack = new SendFileCallBack(SendFile);

            //创建线程
            AcceptSocketThread = new Thread(new ParameterizedThreadStart(StartListen));
            AcceptSocketThread.IsBackground = true;
            AcceptSocketThread.Start(SocketWatch);

        }

        private void StartListen(object obj) {
            Socket socketWatch = obj as Socket;
            while (true) {
                //等待客户端的连接，并且创建一个用于通信的Socket
                SocketSend = socketWatch.Accept();
                //获取远程主机的ip地址和端口号
                string strIp = SocketSend.RemoteEndPoint.ToString();
                DisSocket.Add(strIp, SocketSend);
                cmb_Socket.Invoke(SetCmbCallBack, strIp);
                string strMsg = "远程主机:" + SocketSend.RemoteEndPoint + "连接成功";
                //使用回调
                txt_Log.Invoke(SetCallBack, strMsg);

                //定义接收客户端消息的线程
                Thread threadReceive = new Thread(new ParameterizedThreadStart(Receive));
                threadReceive.IsBackground = true;
                threadReceive.Start(SocketSend);

            }
        }


        /// <summary>
        /// 服务器端不停的接收客户端发送的消息
        /// </summary>
        /// <param name="obj"></param>
        private void Receive(object obj) {
            Socket socketSend = obj as Socket;

            while (true) {
                byte[] buffer = new byte[2048];
                int count = socketSend.Receive(buffer);
                if (count == 0) {//count 表示客户端关闭，要退出循环
                    break;
                } else {
                    string str = Encoding.Default.GetString(buffer, 0, count);
                    string strReceiveMsg = "接收：" + socketSend.RemoteEndPoint + "发送的消息:" + str;
                    txt_Log.Invoke(ReceiveCallBack, strReceiveMsg);
                }

            }
        }

        /// <summary>
        /// 回调委托需要执行的方法
        /// </summary>
        /// <param name="strValue"></param>
        private void SetTextValue(string strValue) {
            txt_Log.AppendText(strValue + "\r\n");
        }

        private void ReceiveMsg(string strMsg) {
            this.txt_Log.AppendText(strMsg + " \r \n");
        }

        private void AddCmbItem(string strItem) {
            cmb_Socket.Items.Add(strItem);
        }

        /// <summary>
        /// 停止监听
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Stop_Click(object sender, EventArgs e) {
            try {
                SocketWatch.Disconnect(true);
                SocketWatch.Dispose();
                SocketWatch.Close();


                SocketSend.Disconnect(true);
                SocketSend.Dispose();
                SocketSend.Close();
                //终止线程
                AcceptSocketThread.Abort();
                threadReceive.Abort();
            } catch (Exception ex) {
                var error = ex.Message;
            }

        }
        /// <summary>
        /// 服务器给客户端发送消息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Send_Click(object sender, EventArgs e) {
            try {
                string strMsg = txt_Msg.Text.Trim();
                byte[] buffer = Encoding.Default.GetBytes(strMsg);
                List<byte> list = new List<byte>();
                list.Add(0);
                list.AddRange(buffer);

                //将泛型集合转换为数组
                byte[] newBuffer = list.ToArray();
                if (cmb_Socket.SelectedIndex <0) {
                    MessageBox.Show("请选择要发送给谁");
                    return;
                }
                //获得用户选择的IP地址
                string ip = cmb_Socket.SelectedItem.ToString();
                DisSocket[ip].Send(newBuffer);

            } catch (Exception ex) {
                MessageBox.Show("给客户端发送消息出错:" + ex.Message);
            } finally {
                //DisSocket[ip].Dispose();

            }
        }

        /// <summary>
        /// 选择要发送的文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Select_Click(object sender, EventArgs e) {
            OpenFileDialog dia = new OpenFileDialog();
            //设置初始目录
            dia.InitialDirectory = @"";
            dia.Title = "请选择要发送的文件";
            //过滤文件类型
            dia.Filter = "所有文件|*.*";
            dia.ShowDialog();
            //将选择的文件的全路径赋值给文本框
            txt_FilePath.Text = dia.FileName;
        }

        /// <summary>
        /// 发送文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_SendFile_Click(object sender, EventArgs e) {
            List<byte> list = new List<byte>();
            string strPath = txt_FilePath.Text.Trim();
            using (FileStream sw=new FileStream (strPath,FileMode.Open,FileAccess.Read)) {
                byte[] buffer = new byte[2048];
                int r = sw.Read(buffer, 0, buffer.Length);
                list.Add(1);
                list.AddRange(buffer);

                byte[] newBuffer = list.ToArray();
                //发送
                btn_SendFile.Invoke(SendCallBack, newBuffer);

            }
        }

        private void SendFile(byte[] sendBuffer) {
            try {
                DisSocket[cmb_Socket.SelectedItem.ToString()].Send(sendBuffer, SocketFlags.None);
            } catch (Exception ex) {
                MessageBox.Show("发送文件出错:" + ex.Message);
            }
        }

        private void FrmServer_Load(object sender, EventArgs e) {
            txt_IP.Text = "192.168.2.6";
            txt_Port.Text = "4521";
        }
    }
}

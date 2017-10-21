using System;
using System.Windows.Forms;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;

//  Edit By:    Li Yunfei
//  20170105:   初次创建
//  20170116:   添加

//  文件输入、输出类
namespace AeroSpace.IO
{
    /// <summary>
    /// 文件读写
    /// </summary>
    public static partial class FileIO
    {
        /// <summary>
        /// 文件发送至远程主机(TCP方式)
        /// </summary>
        /// <param name="host">远程主机</param>
        /// <param name="filePath">完整文件路径(文件名不能超过64位字符)</param>
        public static void FileSender(IPEndPoint host, string filePath)
        {
            try
            {
                //  获取文件名(含后缀)
                string strFile = System.IO.Path.GetFileName(filePath);

                //  建立TCP Socket
                Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                if (!File.Exists(filePath)) throw new Exception("此文件不存在：" + filePath);

                sock.Connect(host);
                FileStream fs = File.Open(filePath, FileMode.Open);
                byte[] data = new byte[fs.Length];
                fs.Read(data, 0, data.Length);
                fs.Close();
                //文件名长度，固定1字节
                //byte[] fileNameLength = Encoding.UTF8.GetBytes(strFile.Length.ToString());
                //文件名
                byte[] fileName = new byte[64];
                byte[] realfileName = Encoding.UTF8.GetBytes(strFile);
                if (realfileName.Length > 64) throw new Exception("文件名长度超过64字符!");
                realfileName.CopyTo(fileName, 0);

                //文件长度,固定20字节
                byte[] filelength = Encoding.UTF8.GetBytes(data.Length.ToString("D20"));
                //合并数组—文件名长度+文件名
                //byte[] file = fileNameLength.Concat(fileName).ToArray();
                //合并数组—文件名+文件长度
                byte[] fileArry = fileName.Concat(filelength).ToArray();
                //合并数组—文件名+文件长度+文件内容
                byte[] sendData = fileArry.Concat(data).ToArray();
                //发送数据—文件名+文件长度+文件内容
                sock.Send(sendData, sendData.Length, 0);
                sock.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "文件传输出错！");
            }
        }

        /// <summary>
        /// 文件接受(外部创建一个后台线程，然后调用此子程序)(TCP方式)
        /// </summary>
        /// <param name="localhost">本地主机</param>
        /// <param name="filePathDir">文件存放路径(默认文件名为64位字符)</param>
        public static void FileReceive(IPEndPoint localhost, string filePathDir)
        {
            try
            {

                Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                sock.Bind(localhost);
                sock.Listen(10);
                while (true)
                {
                    Socket socketClient = sock.Accept();
                    //一次接收1024字节数据
                    byte[] recv = new byte[1024];
                    int byteLength = 0;
                    int fileLength = 0;
                    int recvLength = 0;
                    //接收文件扩展名
                    byteLength = socketClient.Receive(recv, 64, 0);
                    string fileName = Encoding.UTF8.GetString(recv, 0, byteLength);
                    //  文件名
                    fileName = fileName.TrimEnd('\0');
                    string strPath = Path.Combine(filePathDir, fileName);
                    FileStream fs = new FileStream(strPath, FileMode.Create);
                    //接收文件长度
                    byteLength = socketClient.Receive(recv, 20, 0);
                    string fileSize = Encoding.UTF8.GetString(recv, 0, byteLength);
                    fileLength = Convert.ToInt32(fileSize);
                    //接收文件内容
                    while (recvLength < fileLength)
                    {
                        byteLength = socketClient.Receive(recv, recv.Length, 0);
                        recvLength += byteLength;
                        fs.Write(recv, 0, byteLength);
                    }
                    fs.Flush();
                    fs.Close();

                    socketClient.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "文件接受出错！");
            }
        }

        /// <summary>
        /// 采用UDP方式向远程主机发送字符串(UDP方式)
        /// </summary>
        /// <param name="remoteIPEndPoint">远程主机</param>
        /// <param name="msg">需要发送的字符串</param>
        public static void SendMessageToRemoteHost(IPEndPoint remoteIPEndPoint, string msg)
        {
            try
            {
                byte[] bytes = Encoding.Default.GetBytes(msg);
                //  新建UdpClient
                UdpClient sendUdpClient = new UdpClient(0);
                sendUdpClient.Send(bytes, bytes.Length, remoteIPEndPoint);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "发送出错！");
            }
        }
    }
}

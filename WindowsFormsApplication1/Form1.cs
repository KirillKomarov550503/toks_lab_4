using System;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Windows.Forms;
namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private void UpdatePortsInfo()
        {
            while (true)
            {
                string[] portNames = SerialPort.GetPortNames();
                foreach (string name in portNames)
                {
                    this.Invoke((MethodInvoker)(delegate
                    {
                        if (!comboBox1.Items.Contains(name))
                            comboBox1.Items.Add(name);

                    }));
                }
                Thread.Sleep(5000);
            }
        }
        private SerialPort serialPort;
        private Thread thread;
        private void Form1_Load(object sender, EventArgs e)
        {
            textBox1.ReadOnly = true;
            textBox2.ReadOnly = true;
            textBox1.ScrollBars = ScrollBars.Vertical;
            textBox2.ScrollBars = ScrollBars.Vertical;
            textBox3.ScrollBars = ScrollBars.Vertical;
            button1.Text = "Connect";
            button2.Text = "Send";
            thread = new Thread(UpdatePortsInfo);
            thread.Start();

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "Connect")
            {
                try
                {
                    serialPort = new SerialPort(comboBox1.SelectedItem.ToString(), 9600, Parity.None, 8);
                    serialPort.Open();
                    if (serialPort.IsOpen)
                    {
                        textBox2.Text += "Connected to port " + comboBox1.SelectedItem.ToString() + "\r\n";
                        serialPort.DataReceived += DataReceived;
                        button1.Text = "Disconnect";
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
                catch (Exception ex)
                {
                    textBox2.Text += "Problems with connection to port\r\n";
                }
            }
            else
            {
                try
                {
                    serialPort.Close();
                    if (serialPort.IsOpen)
                    {
                        throw new Exception();
                    }
                    else
                    {
                        button1.Text = "Connect";
                    }
                }
                catch (Exception ex)
                {
                    textBox2.Text += "Problems with disconnection\r\n";
                }
            }
        }

        private static string receivedMessage = "";
        private static int collisionCount = 0;
        private void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int size = serialPort.BytesToRead;
            if (size > 0)
            {
                byte[] bytes = new byte[size];
                serialPort.Read(bytes, 0, bytes.Length);
                if (bytes[0] == 0x08)
                {
                    collisionCount++;
                    this.Invoke((MethodInvoker)(delegate
                    {
                        textBox2.Text += "X";
                    }));
                }
                else if (bytes[0] == 0x0A)
                {
                    this.Invoke((MethodInvoker)(delegate
                    {
                        textBox1.Text += receivedMessage + "\r\n";
                        receivedMessage = "";
                    }));
                }
                else
                {
                    collisionCount = 0;
                    string temp = Encoding.ASCII.GetString(bytes);
                    this.Invoke((MethodInvoker)(delegate
                    {
                        receivedMessage += temp;
                        textBox2.Text += "\r\n" + temp;
                    }));
                }
                if (collisionCount == 10)
                {
                    this.Invoke((MethodInvoker)(delegate
                    {
                        textBox2.Text += "Program received message with too much amount of collision\r\n";
                    }));
                }
            }

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            thread.Abort();
        }
        private static bool IsFree()
        {
            return ((Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds % 2) == 0;
        }

        private static bool IsCollise()
        {
            return ((Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds % 2) == 0;
        }

        private static void Delay(int tryNumber)
        {
            Thread.Sleep(new Random().Next((int)Math.Pow(2.0, (double)tryNumber)) * 10);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (textBox3.Text.Length > 0)
            {
                if (serialPort.IsOpen)
                {
                    if (IsFree())
                    {
                        textBox2.Text += "Channel is free" + "\r\n";
                    }
                    else
                    {
                        textBox2.Text += "Channel is busy" + "\r\n";
                        Thread.Sleep(1000);
                        textBox2.Text += "Channel is free" + "\r\n";
                    }
                    serialPort.RtsEnable = true;
                    byte[] bytes = Encoding.ASCII.GetBytes(textBox3.Text);
                    bool limit = false;
                    foreach (byte bt in bytes)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            if (IsCollise())
                            {
                                serialPort.Write(new byte[] { 0x08 }, 0, 1);
                                textBox2.Text += "X";
                                Delay(i);
                            }
                            else
                            {
                                if (i != 0)
                                    textBox2.Text += "\r\n";
                                break;
                            }
                            limit = i == 9;
                        }
                        if (!limit)
                        {
                            textBox2.Text += Encoding.ASCII.GetString(new byte[] { bt }) + "\r\n";
                            serialPort.Write(new byte[] { bt }, 0, 1);
                            Thread.Sleep(100);
                        }
                        else
                        {
                            textBox2.Text += " Program try to send symbol \"" + Encoding.ASCII.GetString(new byte[] { bt }) + "\" 10 times\r\n";
                            return;
                        }
                    }
                    serialPort.Write(new byte[] { 0x0A }, 0, 1);
                    Thread.Sleep(100);
                    serialPort.RtsEnable = false;
                }
                else
                    textBox2.Text += "You are not connected to any ports\r\n";
            }
            else
                textBox2.Text += "You are not input message\r\n";
        }

    }
}

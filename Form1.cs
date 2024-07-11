using System;
using System.Drawing;
using System.IO.Ports;
using System.Windows.Forms;
using Modbus.Device;
using ZedGraph;

namespace modbus_projesi_9
{
    public partial class Form1 : Form
    {
        private IModbusSerialMaster master;
        private SerialPort port;
        private GraphPane myPane;
        private PointPairList[] pointPairLists;
        private LineItem[] curves;
        private int time;

        public Form1()
        {
            InitializeComponent();
            InitializeModbus();

            myPane = zedGraphControl.GraphPane;
            myPane.Title.Text = "Modbus Veri Grafiği";
            myPane.XAxis.Title.Text = "Zaman";
            myPane.YAxis.Title.Text = "Değer";

            pointPairLists = new PointPairList[8]; 
            curves = new LineItem[8];
            Color[] colors = { Color.Blue, Color.Red, Color.Green, Color.Purple, Color.Orange, Color.Brown, Color.Pink, Color.Gray };

            for (int i = 0; i < 8; i++)
            {
                pointPairLists[i] = new PointPairList();
                curves[i] = myPane.AddCurve($"Veri {i + 1}", pointPairLists[i], colors[i], SymbolType.None);
            }
        }

        private void InitializeModbus()
        {
            try
            {
                port = new SerialPort("COM6"); 
                port.BaudRate = 9600;
                port.DataBits = 8;
                port.Parity = Parity.None;
                port.StopBits = StopBits.One;

                if (!port.IsOpen)
                {
                    port.Open();
                }

                master = ModbusSerialMaster.CreateRtu(port);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Seri port açılırken hata oluştu: {ex.Message}");
            }
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            if (port != null && port.IsOpen)
            {
                timer.Start();
            }
            else
            {
                MessageBox.Show("Seri port açık değil!");
            }
        }

        private void buttonDisconnect_Click(object sender, EventArgs e)
        {
            if (port != null && port.IsOpen)
            {
                timer.Stop();
                port.Close();
                port.Dispose();
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (port != null && port.IsOpen)
                {
                    byte slaveId = 2;
                    ushort startAddress = 0;
                    ushort numRegisters = 16; // 8 adet float veri için 16 adet register okunmalı

                    ushort[] registers = master.ReadHoldingRegisters(slaveId, startAddress, numRegisters);

                    for (int i = 0; i < 8; i++)
                    {
                        float value = ConvertRegistersToFloat(registers[i * 2 + 1], registers[i * 2]);
                        pointPairLists[i].Add(time, value);
                    }
                    time++;

                    zedGraphControl.AxisChange();
                    zedGraphControl.Invalidate();
                }
                else
                {
                    timer.Stop();
                    MessageBox.Show("Seri port açık değil!");
                }
            }
            catch (Exception ex)
            {
                timer.Stop();
                MessageBox.Show($"Hata: {ex.Message}");
            }
        }

        private float ConvertRegistersToFloat(ushort highOrderByte, ushort lowOrderByte)
        {
            // 2 adet 8-bit byte'ı 16-bit değere dönüştür
            ushort combinedValue = (ushort)((highOrderByte << 8) | lowOrderByte);

            // Dönüştürülmüş 16-bit değeri float'a dönüştür
            return combinedValue / 10.0f; // sıcaklık değeri 10'a bölünerek float değeri elde ediliyor
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                if (port != null && port.IsOpen)
                {
                    port.Close();
                    port.Dispose();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Seri port kapatılırken hata oluştu: {ex.Message}");
            }

            base.OnFormClosing(e);
        }
    }
}

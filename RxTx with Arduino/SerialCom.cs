using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;
using System.Timers;

namespace RxTx_with_Arduino
{
    public partial class SerialCom : Form
    {
        string serialDataIn;  // To get serial data 
        bool waitForEnd = false;
        private System.Timers.Timer repeatTimer; 
        private string fileContent; // To store the TXT file content
        private bool isRepeating = false; 

        public SerialCom()
        {
            InitializeComponent();
            repeatTimer = new System.Timers.Timer();
            repeatTimer.Elapsed += RepeatTimerElapsed; 
        }

        private void SerialCom_Load(object sender, EventArgs e)
        {
            buttonOPEN.Enabled = true;
            button_CLOSE.Enabled = false;
            button_SEND.Enabled = false;
            button_start_repeat.Enabled = false;
            button_browsefile.Enabled = false;
            progressBar_STATUSbar.Value = 0;
            labelSTATUS.Text = "Disconnected";
            button_start_repeat.Text = "Start Repeating";
            labelSTATUS.ForeColor = Color.Red;

            comboBox_lineEnding.Items.AddRange(new string[] { "None", "Line Feed (LF)", "Carriage Return (CR)", "CR + LF (CRLF)" });
            comboBox_lineEnding.SelectedIndex = 0;

            comboBox_BAUD_RATE.Text = "9600";
            string[] portLists = SerialPort.GetPortNames();
            comboBox_COM_PORT.Items.AddRange(portLists);
        }

        private void buttonOPEN_Click(object sender, EventArgs e)
        {
            try
            {
                serialPort1.PortName = comboBox_COM_PORT.Text;
                serialPort1.BaudRate = Convert.ToInt32(comboBox_BAUD_RATE.Text);
                serialPort1.Open();

                buttonOPEN.Enabled = false;
                button_CLOSE.Enabled = true;
                button_SEND.Enabled = true;
                button_browsefile.Enabled = true;
                button_start_repeat.Enabled = true;
                progressBar_STATUSbar.Value = 100;
                labelSTATUS.Text = "CONNECTED";
                button_start_repeat.Text = "Start Repeating";
                labelSTATUS.ForeColor = Color.Green;

                comboBox_BAUD_RATE.Text = "9600";
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message);
            }
        }

        private void button_CLOSE_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen)
            {
                try
                {
                    serialPort1.Close();

                    buttonOPEN.Enabled = true;
                    button_CLOSE.Enabled = false;
                    button_SEND.Enabled = false;
                    button_browsefile.Enabled = false;
                    button_start_repeat.Enabled = false;
                    progressBar_STATUSbar.Value = 0;
                    labelSTATUS.Text = "Disconnected";
                    richTextBox_Txt_Receiver.Text = "";
                    labelSTATUS.ForeColor = Color.Red;

                    comboBox_BAUD_RATE.Text = "9600";

                    sending_Status.Text = "Ready";
                    sending_Status.ForeColor = Color.Gray;
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.Message);
                }
            }
        }

        private void SerialCom_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (serialPort1.IsOpen)
            {
                try
                {
                    repeatTimer.Stop(); // Stop timer if running
                    serialPort1.Close();
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.Message);
                }
            }
        }

        private void button_SEND_Click(object sender, EventArgs e)
        {
            try
            {
                if (waitForEnd)
                {
                    button_SEND.Enabled = false;
                    MessageBox.Show("Please Wait!" + serialDataIn + "\n" + waitForEnd);
                    return;
                }

                string textToSend = textBox_Txt_Send.Text;
                if (!string.IsNullOrEmpty(fileContent) && string.IsNullOrEmpty(textToSend))
                {
                    textToSend = fileContent;
                }

                if (!string.IsNullOrEmpty(textToSend))
                {
                    serialPort1.Write(textToSend + GetLineEnding());
                    richTextBox_Txt_Receiver.Text += "";
                    textBox_Txt_Send.Text = "";
                    sending_Status.Text = "Message Sent"; // Immediate update
                    sending_Status.ForeColor = Color.Black;
                    button_SEND.Enabled = true; // Re-enable immediately
                    waitForEnd = false; // Reset for repeat
                    textbox_filepath.Text = ""; // clear the file path
                    fileContent = ""; // Clear fileContent after send


                }
                else
                {
                    MessageBox.Show("Invalid input or no file loaded");
                }
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message);
            }
        }

        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            serialDataIn = serialPort1.ReadExisting();
            this.Invoke(new EventHandler(ShowData));
        }

        private void ShowData(object sender, EventArgs e)
        {
            if (sending_Status.Text == "Message is sending...")
            {
                sendingStatus();
            }
            if (sending_Status.Text == "Ready")
            {
                receivingStatusAsync();
                // No acknowledgment: //serialPort1.Write("\n");
            }
            richTextBox_Txt_Receiver.Text += serialDataIn;
        }

        private async void sendingStatus()
        {
            button_SEND.Enabled = true;
            button_browsefile.Enabled = true;
            sending_Status.Text = "Message Sent";
            sending_Status.ForeColor = Color.Black;
            await Task.Delay(2000);
            sending_Status.Text = "Ready";
            sending_Status.ForeColor = Color.Black;
            waitForEnd = false;
        }

        private async Task receivingStatusAsync()
        {
            // Removed the condition serialDataIn == "\n" since we’re now sending \n after any received data
            button_SEND.Enabled = true;
            button_browsefile.Enabled = true;
            sending_Status.Text = "Message Received";
            sending_Status.ForeColor = Color.Black;
            await Task.Delay(2000);
            sending_Status.Text = "Ready";
            sending_Status.ForeColor = Color.Black;
            waitForEnd = false;
        }

        private void richTextBox_Txt_Receiver_TextChanged(object sender, EventArgs e)
        {
            richTextBox_Txt_Receiver.SelectionStart = richTextBox_Txt_Receiver.Text.Length;
            richTextBox_Txt_Receiver.ScrollToCaret();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            richTextBox_Txt_Receiver.Text = "";
        }

        private void textBox_Txt_Send_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                button_SEND_Click(sender, e);
            }
        }

        private void button_browsefile_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                Title = "Select a Text File"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                textbox_filepath.Text = openFileDialog.FileName;
                fileContent = File.ReadAllText(openFileDialog.FileName);
            }
        }



        private void RepeatTimerElapsed(object sender, ElapsedEventArgs e)
        {
            this.Invoke(new Action(() =>
            {
                if (serialPort1.IsOpen && !waitForEnd)
                {
                    serialPort1.Write(fileContent + GetLineEnding());
                    richTextBox_Txt_Receiver.Text += "";
                    sending_Status.Text = "Message sent (repeating)";
                    sending_Status.ForeColor = Color.Black;
                }
            }));
        }

        private string GetLineEnding()
        {
            switch (comboBox_lineEnding.SelectedIndex)
            {
                case 1: return "\n"; // Line Feed (LF)
                case 2: return "\r"; // Carriage Return (CR)
                case 3: return "\r\n"; // CR + LF (CRLF)
                case 0: // None
                default: return "";
            }
        }

        private void label1_Click(object sender, EventArgs e) { }
        private void label2_Click(object sender, EventArgs e) { }
        private void groupBox1_Enter(object sender, EventArgs e) { }
        private void label3_Click(object sender, EventArgs e) { }
        private void label4_Click(object sender, EventArgs e) { }
        private void SerialCom_FormClosed(object sender, FormClosedEventArgs e) { }
        private void Title_Click(object sender, EventArgs e) { }
        private void button_browsefile_HelpRequest(object sender, EventArgs e) { }

        private void textbox_filepath_TextChanged(object sender, EventArgs e)
        {

        }

        private void button_start_repeat_Click_1(object sender, EventArgs e)
        {
            
        }

        private void label2_Click_1(object sender, EventArgs e)
        {

        }

        private void button_start_repeat_Click(object sender, EventArgs e)
        {
            if (!serialPort1.IsOpen)
            {
                MessageBox.Show("Please open the serial port first!");
                return;
            }

            if (string.IsNullOrEmpty(fileContent))
            {
                MessageBox.Show("Please load a text file first!");
                return;
            }

            if (!int.TryParse(textBox_interval.Text, out int intervalSeconds) || intervalSeconds <= 0)
            {
                MessageBox.Show("Please enter a valid interval in seconds!");
                return;
            }

            if (!isRepeating)
            {
                repeatTimer.Interval = intervalSeconds * 1000;
                repeatTimer.Start();
                isRepeating = true;
                button_start_repeat.Text = "Stop Repeating";
                sending_Status.Text = "Repeating every " + intervalSeconds + " seconds";
            }
            else
            {
                repeatTimer.Stop();
                isRepeating = false;
                button_start_repeat.Text = "Start Repeating";
                sending_Status.Text = "Ready";
                sending_Status.ForeColor = Color.Gray;
            }
        }
    }
}
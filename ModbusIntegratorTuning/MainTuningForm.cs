using ModbusIntegratorEventClient;
using System;
using System.Windows.Forms;

namespace ModbusIntegratorTuning
{
    public partial class MainTuningForm : Form
    {
        private readonly EventClient _localEventClient;

        public MainTuningForm()
        {
            InitializeComponent();
            _localEventClient = new EventClient();
        }

        private void MainClientForm_Load(object sender, EventArgs e)
        {
            _localEventClient.Connect(new[] { "Fetching", "Archives" }, PropertyUpdate, ShowError, UpdateLocalConnectionStatus);

        }

        private void UpdateLocalConnectionStatus(Guid clientId, ClientConnectionStatus status)
        {
            var method = new MethodInvoker(() =>
            {
                switch (status)
                {
                    case ClientConnectionStatus.Opened:
                        scServerConnected.State = true;
                        break;
                    case ClientConnectionStatus.Opening:
                        scServerConnected.State = null;
                        break;
                    default:
                        scServerConnected.State = false;
                        break;
                }
                if (status == ClientConnectionStatus.Opening)
                {
                    listBox1.Items.Clear();
                    listBox2.Items.Clear();
                }
            });
            if (InvokeRequired)
                BeginInvoke(method);
            else
                method();
        }

        private void ShowError(string errormessage)
        {
            var method = new MethodInvoker(() =>
            {
                Text = errormessage;
            });
            if (InvokeRequired)
                BeginInvoke(method);
            else
                method();
        }

        private void PropertyUpdate(DateTime servertime, string category, string pointname, string propname, string value)
        {
            switch (category)
            {
                case "Fetching":
                    var method1 = new MethodInvoker(() =>
                    {
                        listBox1.Items.Insert(0, $"{pointname} {propname} {value}");
                    });
                    if (InvokeRequired)
                        BeginInvoke(method1);
                    else
                        method1();
                    break;
                case "Archives":
                    var method2 = new MethodInvoker(() =>
                    {
                        listBox2.Items.Insert(0, $"{pointname} {propname} {value}");
                    });
                    if (InvokeRequired)
                        BeginInvoke(method2);
                    else
                        method2();
                    break;
            }
        }

        private void MainClientForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _localEventClient.Disconnect();
        }

        private void MainClientForm_Click(object sender, EventArgs e)
        {
            _localEventClient.UpdateProperty("Fetching", "Client", "Date", $"{DateTime.Now.Date}", false);
            _localEventClient.UpdateProperty("Fetching", "Client", "Time", $"{DateTime.Now.TimeOfDay}", false);
        }

    }
}

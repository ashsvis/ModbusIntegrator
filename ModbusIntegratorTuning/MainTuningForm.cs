using ModbusIntegratorEventClient;
using System;
using System.Windows.Forms;

namespace ModbusIntegratorTuning
{
    public partial class MainTuningForm : Form
    {
        private readonly EventClient locEvClient;

        public MainTuningForm()
        {
            InitializeComponent();
            locEvClient = new EventClient();
        }

        private void MainClientForm_Load(object sender, EventArgs e)
        {
            locEvClient.Connect(new[] {  "config", "fetching", "archives" }, PropertyUpdate, ShowError, UpdateLocalConnectionStatus);

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
            switch (category.ToLower())
            {
                case "fetching":
                    var method1 = new MethodInvoker(() =>
                    {
                        listBox1.Items.Insert(0, $"{pointname} {propname} {value}");
                    });
                    if (InvokeRequired)
                        BeginInvoke(method1);
                    else
                        method1();
                    break;
                case "archives":
                    var method2 = new MethodInvoker(() =>
                    {
                        listBox2.Items.Insert(0, $"{pointname} {propname} {value}");
                    });
                    if (InvokeRequired)
                        BeginInvoke(method2);
                    else
                        method2();
                    break;
                case "config":
                    var method3 = new MethodInvoker(() =>
                    {
                        switch (pointname.ToLower())
                        {
                            case "add":
                                var tree = treeView1.Nodes;
                                var nodes = tree.Find(value, true);
                                if (nodes.Length == 0)
                                {
                                    treeView1.BeginUpdate();
                                    try
                                    {
                                        foreach (var item in value.Split('\\'))
                                        {
                                            nodes = tree.Find(item, false);
                                            if (nodes.Length == 0)
                                            {
                                                var node = new TreeNode(item) { Name = item };
                                                tree.Add(node);
                                                tree = node.Nodes;
                                            }
                                            else
                                                tree = nodes[0].Nodes;
                                        }
                                        treeView1.Sort();
                                    }
                                    finally
                                    {
                                        treeView1.EndUpdate();
                                    }
                                }
                                break;
                        }
                    });
                    if (InvokeRequired)
                        BeginInvoke(method3);
                    else
                        method3();
                    break;
            }
        }

        private void MainClientForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            locEvClient.Disconnect();
        }

        private void addItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.Nodes.Count == 0)
                AddNodeToTree(treeView1.Nodes, "default");
            else
            {
                var parentNode = treeView1.SelectedNode;
                if (parentNode == null)
                    AddNodeToTree(treeView1.Nodes, $"socket {treeView1.Nodes.Count}");
                else
                {
                    switch (parentNode.Level)
                    {
                        case 0:
                            AddNodeToTree(parentNode.Nodes, $"node {parentNode.Nodes.Count + 1}");
                            break;
                        case 1:
                            AddNodeToTree(parentNode.Nodes, $"item {parentNode.Nodes.Count + 1}");
                            break;
                    }
                }
            }
        }

        private void AddNodeToTree(TreeNodeCollection nodes, string name)
        {
            TreeNode node = new TreeNode(name) { Name = name };
            nodes.Add(node);
            treeView1.SelectedNode = node;
            locEvClient.UpdateProperty("config", "add", node.FullPath, node.FullPath);
        }

        private void treeView1_MouseDown(object sender, MouseEventArgs e)
        {
            treeView1.SelectedNode = treeView1.GetNodeAt(e.Location);
        }
    }
}

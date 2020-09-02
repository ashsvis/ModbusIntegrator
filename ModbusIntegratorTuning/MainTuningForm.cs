using ModbusIntegratorEventClient;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

namespace ModbusIntegratorTuning
{
    public partial class MainTuningForm : Form
    {
        private readonly EventClient locEvClient;
        private readonly Dictionary<string, string> dictionary = new Dictionary<string, string>();

        public MainTuningForm()
        {
            InitializeComponent();
            locEvClient = new EventClient();
        }

        private void MainClientForm_Load(object sender, EventArgs e)
        {
            Thread.Sleep(1000);
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
                        tsslStatus.Text = "Подключение актуально.";
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
                    //listBox1.Items.Clear();
                    //listBox2.Items.Clear();
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
                tsslStatus.Text = errormessage;
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
                        var key = $"{pointname}\\{propname}";
                        if (!dictionary.ContainsKey(key))
                            dictionary.Add(key, value);
                        else
                            dictionary[key] = value;
                    });
                    if (InvokeRequired)
                        BeginInvoke(method1);
                    else
                        method1();
                    break;
                case "archives":
                    var method2 = new MethodInvoker(() =>
                    {
                        //listBox2.Items.Insert(0, $"{pointname} {propname} {value}");
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
                                var tree = tvNodes.Nodes;
                                var nodes = tree.Find(propname, true);
                                if (nodes.Length == 0)
                                {
                                    tvNodes.BeginUpdate();
                                    try
                                    {
                                        foreach (var item in propname.Split('\\'))
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
                                        tvNodes.Sort();
                                    }
                                    finally
                                    {
                                        tvNodes.EndUpdate();
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
            if (tvNodes.Nodes.Count == 0)
                AddNodeToTree(tvNodes.Nodes, "default");
            else
            {
                var parentNode = tvNodes.SelectedNode;
                if (parentNode == null)
                    AddNodeToTree(tvNodes.Nodes, $"socket {tvNodes.Nodes.Count}");
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
            tvNodes.SelectedNode = node;
            locEvClient.UpdateProperty("config", "add", node.FullPath, node.FullPath);
        }

        private void treeView1_MouseDown(object sender, MouseEventArgs e)
        {
            tvNodes.SelectedNode = tvNodes.GetNodeAt(e.Location);
            if (tvNodes.SelectedNode == null) tsslStatus.Text = "";
        }

        private void tvNodes_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var akey = $"{e.Node?.FullPath}";
            lvProps.Items.Clear();
            foreach (var key in dictionary.Keys)
            {
                if (key.StartsWith(akey))
                {
                    var prop = $"{key.Substring(akey.Length + 1)}";
                    if (prop.IndexOf('\\') < 0)
                    {
                        lvProps.Items.Add(prop).SubItems.Add(dictionary[key]);
                    }
                }
            }
        }
    }
}

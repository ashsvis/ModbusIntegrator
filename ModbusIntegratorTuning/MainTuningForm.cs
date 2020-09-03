using ModbusIntegratorEventClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
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
            locEvClient.Connect(new[] { "config", "fetching", "archives" }, PropertyUpdate, ShowError, UpdateLocalConnectionStatus);
        }

        private void UpdateLocalConnectionStatus(Guid clientId, ClientConnectionStatus status)
        {
            var method = new MethodInvoker(() =>
            {
                switch (status)
                {
                    case ClientConnectionStatus.Opened:
                        scServerConnected.State = true;
                        tsslStatus.Text = "Подключение к серверу событий установлено.";
                        break;
                    case ClientConnectionStatus.Opening:
                        scServerConnected.State = null;
                        tsslStatus.Text = "Подключение к серверу событий...";
                        dictionary.Clear();
                        break;
                    default:
                        scServerConnected.State = false;
                        break;
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
                                var treeNodes = propname.StartsWith("root") ? tvSources : tvNodes;
                                var tree = treeNodes.Nodes;
                                var nodes = tree.Find(propname, true);
                                if (nodes.Length == 0)
                                {
                                    treeNodes.BeginUpdate();
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
                                        treeNodes.Sort();
                                    }
                                    finally
                                    {
                                        treeNodes.EndUpdate();
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

        //private void addItemToolStripMenuItem_Click(object sender, EventArgs e)
        //{
        //    if (tvNodes.Nodes.Count == 0)
        //        AddNodeToTree(tvNodes.Nodes, "default");
        //    else
        //    {
        //        var parentNode = tvNodes.SelectedNode;
        //        if (parentNode == null)
        //            AddNodeToTree(tvNodes.Nodes, $"socket {tvNodes.Nodes.Count}");
        //        else
        //        {
        //            switch (parentNode.Level)
        //            {
        //                case 0:
        //                    AddNodeToTree(parentNode.Nodes, $"node {parentNode.Nodes.Count + 1}");
        //                    break;
        //                case 1:
        //                    AddNodeToTree(parentNode.Nodes, $"item {parentNode.Nodes.Count + 1}");
        //                    break;
        //            }
        //        }
        //    }
        //}

        //private void AddNodeToTree(TreeNodeCollection nodes, string name)
        //{
        //    TreeNode node = new TreeNode(name) { Name = name };
        //    nodes.Add(node);
        //    tvNodes.SelectedNode = node;
        //    locEvClient.UpdateProperty("config", "add", node.FullPath, node.FullPath);
        //}

        private void treeView_MouseDown(object sender, MouseEventArgs e)
        {
            var treeNodes = (TreeView)sender;
            treeNodes.SelectedNode = treeNodes.GetNodeAt(e.Location);
            tsslStatus.Text = $"{treeNodes.SelectedNode?.FullPath}";
            if (treeNodes.SelectedNode == null)
            {
                lvProps.Items.Clear();
                lvProps.Columns.Clear();
            }
        }

        private void treeNodes_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var akey = $"{e.Node?.FullPath}";
            lvProps.BeginUpdate();
            try
            {
                lvProps.Items.Clear();
                lvProps.Columns.Clear();
                lvProps.Columns.Add(new ColumnHeader() { Text = "Property Name", Width = 100 });
                lvProps.Columns.Add(new ColumnHeader() { Text = "Property Value", Width = 100 });
                var items = new List<ListViewItem>();
                foreach (var key in dictionary.Keys)
                {
                    if (key.StartsWith(akey))
                    {
                        var prop = $"{key.Substring(akey.Length + 1)}";
                        if (prop.IndexOf('\\') < 0)
                        {
                            var vals = dictionary[key].Split(';');
                            if (prop.ToLower() == "#columns")
                            {
                                lvProps.Columns.Clear();
                                lvProps.Columns.Add(new ColumnHeader() { Text = "Property Name", Width = 100 });
                                foreach (var value in vals)
                                    lvProps.Columns.Add(new ColumnHeader() { Text = value, Width = 70 });
                            }
                            if (prop.StartsWith("#")) continue;
                            var lvi = new ListViewItem(prop) { Tag = key };
                            foreach (var value in vals)
                                lvi.SubItems.Add(value);
                            items.Add(lvi);
                        }
                    }
                }
                foreach (var item in items.OrderBy(x => x.Text))
                    lvProps.Items.Add(item);
                // настройка выравнивания целочисленных стобцов к правой стороне
                foreach (var column in lvProps.Columns.Cast<ColumnHeader>().Skip(1))
                {
                    var allIsInteger = true;
                    foreach (var lvi in lvProps.Items.Cast<ListViewItem>())
                    {
                        if (column.Index >= lvi.SubItems.Count) break;
                        var value = lvi.SubItems[column.Index].Text;
                        if (!int.TryParse(value, out int ival))
                        {
                            allIsInteger = false;
                            break;
                        }
                    }
                    if (allIsInteger)
                        column.TextAlign = HorizontalAlignment.Right;
                }
            }
            finally
            {
                lvProps.EndUpdate();
            }
        }

        private void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            serviceController1.Refresh();
            if (serviceController1.Status == ServiceControllerStatus.Running)
            {
                try
                {
                    serviceController1.MachineName = Environment.MachineName;
                    // чтобы это сработало, необходимо запустить это приложение с правами администратора.
                    serviceController1.Stop();
                    serviceController1.WaitForStatus(ServiceControllerStatus.Stopped);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Останов службы", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            serviceController1.Refresh();
            if (serviceController1.Status == ServiceControllerStatus.Stopped)
            {
                try
                {
                    serviceController1.MachineName = Environment.MachineName;
                    // чтобы это сработало, необходимо запустить это приложение с правами администратора.
                    serviceController1.Start();
                    serviceController1.WaitForStatus(ServiceControllerStatus.Running);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Запуск службы", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void lvProps_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvProps.SelectedItems.Count == 0) return;
            var lvi = lvProps.SelectedItems[0];
            tsslStatus.Text = $"{lvi.Tag}";
        }

        private int lastColumn = -1;

        private void lvProps_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            var lv = (ListView)sender;
            if (lastColumn != e.Column)
            {
                lv.ListViewItemSorter = new ListViewItemComparer(e.Column);
                lastColumn = e.Column;
            }
            else
            {
                if (lv.ListViewItemSorter is ListViewItemComparer)
                    lv.ListViewItemSorter = new ListViewItemReverseComparer(e.Column);
                else
                    lv.ListViewItemSorter = new ListViewItemComparer(e.Column);
            }
            if (lv.FocusedItem != null)
                lv.FocusedItem.EnsureVisible();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            locEvClient.Reconnect();
        }

        private void treeNodes_Leave(object sender, EventArgs e)
        {
            var treeNodes = (TreeView)sender;
            treeNodes.SelectedNode = null;
        }
    }

    // Implements the manual sorting of items by columns.
    public class ListViewItemComparer : IComparer
    {
        private readonly int col;

        public ListViewItemComparer()
        {
            col = 0;
        }

        public ListViewItemComparer(int column)
        {
            col = column;
        }

        public int Compare(object x, object y)
        {
            ListViewItem itemX = (ListViewItem)x;
            ListViewItem itemY = (ListViewItem)y;
            if (col < itemX.SubItems.Count && col < itemY.SubItems.Count)
            {
                if (int.TryParse(itemX.SubItems[col].Text, out int ix) && int.TryParse(itemY.SubItems[col].Text, out int iy))
                    return ix > iy ? 1 : ix < iy ? -1 : 0;
                else
                    return string.Compare(itemX.SubItems[col].Text, itemY.SubItems[col].Text);
            }
            else
                return 0;
        }
    }

    // Implements the manual reverse sorting of items by columns.
    public class ListViewItemReverseComparer : IComparer
    {
        private readonly int col;

        public ListViewItemReverseComparer()
        {
            col = 0;
        }

        public ListViewItemReverseComparer(int column)
        {
            col = column;
        }

        public int Compare(object x, object y)
        {
            ListViewItem itemX = (ListViewItem)x;
            ListViewItem itemY = (ListViewItem)y;
            if (col < itemX.SubItems.Count && col < itemY.SubItems.Count)
            {
                if (int.TryParse(itemX.SubItems[col].Text, out int ix) && int.TryParse(itemY.SubItems[col].Text, out int iy))
                    return ix < iy ? 1 : ix > iy ? -1 : 0;
                else
                    return string.Compare(itemY.SubItems[col].Text, itemX.SubItems[col].Text);
            }
            else
                return 0;
        }
    }

}

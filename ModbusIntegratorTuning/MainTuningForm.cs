using ModbusIntegratorEvent;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Windows.Forms;

namespace ModbusIntegratorTuning
{
    public partial class MainTuningForm : Form
    {
        private readonly EventClient locEvClient;
        private readonly Dictionary<string, string> config = new Dictionary<string, string>();
        private readonly Dictionary<string, string> fetching = new Dictionary<string, string>();

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
                        config.Clear();
                        fetching.Clear();
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
                        // для работы списка свойств
                        var key = $"{pointname}.{propname}";
                        if (!fetching.ContainsKey(key))
                            fetching.Add(key, value);
                        else
                            fetching[key] = value;

                        tsslStatus.Text = $"{category}:{key}={value}";

                        var found = false;
                        foreach (var lvi in lvValues.Items.Cast<ListViewItem>())
                        {
                            if ($"{lvi.Tag}" == key)
                            {
                                found = true;
                                lvi.SubItems[1].Text = value;
                                break;
                            }
                        }
                        if (!found)
                        {
                            var lvi = new ListViewItem(Path.GetFileName(key));
                            lvi.SubItems.Add(value);
                            lvi.Tag = key;
                            lvValues.Items.Add(lvi);
                            lvValues.Sort();
                        }

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
                        // для работы списка свойств
                        var key = $"{pointname}\\{propname}";
                        if (!config.ContainsKey(key))
                            config.Add(key, value);
                        else
                            config[key] = value;
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

            if (treeNodes == tvNodes)
                tvSources.SelectedNode = null;
            else
                tvNodes.SelectedNode = null;

            treeNodes.SelectedNode = treeNodes.GetNodeAt(e.Location);
            tsslStatus.Text = $"{treeNodes.SelectedNode?.FullPath}";
            if (treeNodes.SelectedNode == null)
            {
                lvProps.Items.Clear();
                lvProps.Columns.Clear();
                lvValues.Items.Clear();
                lvValues.Columns.Clear();
            }
        }

        private void treeNodes_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var akey = $"{e.Node?.FullPath}";

            foreach (var item in new[] {
                                    new { View = lvProps, Dict = config },
                                    new { View = lvValues, Dict = fetching }
                                })
            {
                item.View.BeginUpdate();
                try
                {
                    item.View.Items.Clear();
                    item.View.Columns.Clear();
                    item.View.Columns.Add(new ColumnHeader() { Text = "Property Name", Width = 100 });
                    item.View.Columns.Add(new ColumnHeader() { Text = "Property Value", Width = 100 });
                    //
                    var items = new List<ListViewItem>();
                    foreach (var key in item.Dict.Keys)
                    {
                        if (key.StartsWith(akey))
                        {
                            var prop = $"{key.Substring(akey.Length + 1)}";
                            if (prop.IndexOf('\\') < 0)
                            {
                                var vals = item.Dict[key].Split(';');
                                if (prop.ToLower() == "#columns")
                                {
                                    item.View.Columns.Clear();
                                    item.View.Columns.Add(new ColumnHeader() { Text = "Property Name", Width = 100 });
                                    foreach (var value in vals)
                                        item.View.Columns.Add(new ColumnHeader() { Text = value, Width = 70 });
                                }
                                if (prop.StartsWith("#")) continue;
                                var lvi = new ListViewItem(prop) { Tag = key };
                                foreach (var value in vals)
                                    lvi.SubItems.Add(value);
                                items.Add(lvi);
                            }
                        }
                    }
                    item.View.Items.AddRange(items.OrderBy(x => x.Text).ToArray());
                    ColumnAlligment(item.View);
                }
                finally
                {
                    item.View.EndUpdate();
                }
            }
        }

        private void ColumnAlligment(ListView lv)
        {
            // настройка выравнивания целочисленных стобцов к правой стороне
            foreach (var column in lv.Columns.Cast<ColumnHeader>().Skip(1))
            {
                var allIsInteger = true;
                foreach (var lvi in lv.Items.Cast<ListViewItem>())
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
            var lv = (ListView)sender;
            if (lv.SelectedItems.Count == 0) return;
            var lvi = lv.SelectedItems[0];
            tsslStatus.Text = $"{lvi.Tag}";
        }

        private void lvProps_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            var lv = (ListView)sender;
            var lastColumn = (int)(lv.Tag ?? -1); 
            if (lastColumn != e.Column)
            {
                lv.ListViewItemSorter = new ListViewItemComparer(e.Column);
                lv.Tag = e.Column;
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

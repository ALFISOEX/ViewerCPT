using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;

namespace ViewerCPT
{
    public partial class MainWindow : Window
    {
        XmlDocument xml;
        List<TreeItem> treeItems;
        TreeViewItem[] selectedItems;

        public MainWindow()
        {
            InitializeComponent();
            selectedItems = new TreeViewItem[1];

            var parcel = new TreeViewItem { Header = "Parcel"};
            var objectRealty = new TreeViewItem { Header = "ObjectRealty"};
            var spatialData = new TreeViewItem { Header = "SpatialData" };
            var bound = new TreeViewItem { Header = "Bound" };
            var zone = new TreeViewItem { Header = "Zone" };

            xml = new XmlDocument();
            xml.Load("24_21_1003001_2017-05-29_kpt11.xml");

            XmlNode cadastral_block = xml.SelectSingleNode("/extract_cadastral_plan_territory/cadastral_blocks/cadastral_block");

            treeItems = new List<TreeItem>
            {
                new TreeItem(parcel,
                    cadastral_block.SelectSingleNode("record_data/base_data/land_records").ChildNodes, "cad_number"),
                new TreeItem(objectRealty,
                    cadastral_block.SelectSingleNode("record_data/base_data/build_records").ChildNodes, "cad_number"),
                new TreeItem(objectRealty,
                    cadastral_block.SelectSingleNode("record_data/base_data/construction_records").ChildNodes, "cad_number"),
                new TreeItem(spatialData,
                    cadastral_block.SelectSingleNode("spatial_data").ChildNodes, "sk_id"),
                new TreeItem(bound,
                    cadastral_block.SelectSingleNode("municipal_boundaries").ChildNodes, "reg_numb_border"),
                new TreeItem(zone,
                    cadastral_block.SelectSingleNode("zones_and_territories_boundaries").ChildNodes, "reg_numb_border")
            };

            foreach (TreeItem treeItem in treeItems)
            {
                foreach (TreeViewItem treeViewItem in treeItem.Item.Items)
                {
                    treeViewItem.Selected += TreeViewItem_Selected;
                    treeViewItem.Unselected += TreeViewItem_Unselected;
                }
            }

            treeView.ItemsSource = new List<TreeViewItem> {
                parcel,
                objectRealty,
                spatialData,
                bound,
                zone
            };
        }

        private void TreeViewItem_Unselected(object sender, RoutedEventArgs e)
        {
            SaveNodes.IsEnabled = false;

            if (!IsPressedCtrl())
            {
                ClearTreeViewItems();
            }
        }

        private void ClearTreeViewItems()
        {
            foreach (TreeItem treeItem in treeItems)
            {
                treeItem.ClearTreeViewItem();
            }
        }

        private void SelectTreeViewItem(TreeViewItem item)
        {
            item.Background = new SolidColorBrush(SystemColors.HighlightColor);
            item.Foreground = new SolidColorBrush(SystemColors.HighlightTextColor);
        }

        private bool IsPressedCtrl()
        {
            return Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
        }

        private bool IsPressedShift()
        {
            return Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
        }

        private bool IsHaveSelectedItems() {
            return selectedItems[0] != null ? true : false;
        }

        private void TreeViewItem_Selected(object sender, RoutedEventArgs e)
        {
            var item = (TreeViewItem)sender;

            if (!SaveNodes.IsEnabled)
            {
                SaveNodes.IsEnabled = true;
            }

            if (!IsHaveSelectedItems())
            {
                selectedItems = new TreeViewItem[1];
                selectedItems[0] = item;
            }
            else
            {
                if (IsPressedCtrl())
                {
                    var tempTVI = selectedItems;
                    int lengthItems = selectedItems.Length;
                    selectedItems = new TreeViewItem[lengthItems + 1];

                    for (var i = 0; i < tempTVI.Length; i++)
                    {
                        selectedItems[i] = tempTVI[i];
                    }
                    selectedItems[lengthItems] = item;
                    SelectTreeViewItem(item);
                }
                else
                {
                    if (IsPressedShift())
                    {
                        var listItem = new List<TreeViewItem>();
                        foreach (TreeItem treeItem in treeItems)
                        {
                            foreach (TreeViewItem tvItem in treeItem.Item.Items)
                            {
                                listItem.Add(tvItem);
                            }
                        }

                        var startI = listItem.IndexOf(item);
                        var endI = listItem.IndexOf(selectedItems[0]);
                        var range = Math.Abs(startI - endI) + 1;
                        var tempList = listItem.GetRange(startI < endI ? startI : endI, range);
                        selectedItems = new TreeViewItem[range];

                        for (var i = 0; i < range; i++)
                        {
                            var tvItem = tempList[i];
                            selectedItems[i] = tvItem;
                            SelectTreeViewItem(tvItem);
                        }
                    }
                    else
                    {
                        selectedItems[0] = item;
                        ClearTreeViewItems();
                        SelectTreeViewItem(item);
                    }
                }
            }

            var key = item.Header.ToString();
            foreach (TreeItem treeItem in treeItems)
            {
                XmlNode node = treeItem.SearchKey(key);

                if (node != null)
                {
                    XMLViewer.NavigateToString("<?xml version=\"1.0\" encoding=\"utf-8\"?>" + node.OuterXml);
                    break;
                }
            }
        }

        private void SaveNodeXML(object sender, RoutedEventArgs e)
        {
            var doc = new XmlDocument();
            var nodesToWrite = doc.CreateElement("SavedItems");
            doc.AppendChild(nodesToWrite);

            foreach (TreeViewItem item in selectedItems)
            {
                var key = item.Header.ToString();
                foreach (TreeItem treeItem in treeItems)
                {
                    XmlNode node = treeItem.SearchKey(key);
                    if (node != null)
                    {
                        nodesToWrite.AppendChild(doc.ImportNode(node, true));
                        break;
                    }
                }
            }

            var save = new SaveFileDialog { DefaultExt = ".xml", Filter = "XML-файл (*.xml)|*.xml" };
            if (save.ShowDialog() == true)
            {
                doc.Save(save.FileName);
            }
        }
    }

    class TreeItem
    {
        public TreeViewItem Item;
        public XmlNodeList Nodes;
        public string NameKey;
        
        public TreeItem(TreeViewItem Item, XmlNodeList Nodes, string NameKey)
        {
            this.Item = Item;
            this.Nodes = Nodes;
            this.NameKey = NameKey;

            FillItem();
        }

        public XmlNode SearchKey(string key)
        {
            foreach (XmlNode node in Nodes)
            {
                if (IsHaveKey(node, key))
                {
                    return node;
                }
            }

            return null;
        }

        private bool IsHaveKey(XmlNode node, string key)
        {
            if (node.Name == NameKey && node.InnerText == key)
            {
                return true;
            }

            if (node.HasChildNodes)
            {
                foreach (XmlNode child in node.ChildNodes)
                {
                    if (IsHaveKey(child, key))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void FillItem()
        {
            foreach (XmlNode node in Nodes)
            {
                string key = GetKeyNode(node);
                if (key != null)
                {
                    var newItem = new TreeViewItem { Header = key };
                    Item.Items.Add(newItem);
                }
            }
        }

        private string GetKeyNode(XmlNode node)
        {
            if (node.Name == NameKey)
            {
                return node.InnerText;
            }

            if (node.HasChildNodes)
            {
                foreach (XmlNode child in node.ChildNodes)
                {
                    var keyNode = GetKeyNode(child);
                    if (keyNode != null)
                    {
                        return keyNode;
                    }
                }
            }

            return null;
        }

        public void ClearTreeViewItem()
        {
            foreach (TreeViewItem tvItem in Item.Items)
            {
                tvItem.Background = new SolidColorBrush(SystemColors.WindowColor);
                tvItem.Foreground = new SolidColorBrush(SystemColors.WindowTextColor);
            }
        }
    }
}
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
        // Переменная для работы с файлом XML
        XmlDocument xml;
        // Список элементов типа TreeItem
        List<TreeItem> treeItems;
        // Массив выделенных элементов дерева
        TreeViewItem[] selectedItems;

        public MainWindow()
        {
            InitializeComponent();
            selectedItems = new TreeViewItem[1];
            
            // Создание элементов дерева
            var parcel = new TreeViewItem { Header = "Parcel"};
            var objectRealty = new TreeViewItem { Header = "ObjectRealty"};
            var spatialData = new TreeViewItem { Header = "SpatialData" };
            var bound = new TreeViewItem { Header = "Bound" };
            var zone = new TreeViewItem { Header = "Zone" };

            // Загрузка XML файла
            xml = new XmlDocument();
            xml.Load("24_21_1003001_2017-05-29_kpt11.xml");

            // Путь к корневому узлу
            XmlNode cadastral_block = xml.SelectSingleNode("/extract_cadastral_plan_territory/cadastral_blocks/cadastral_block");

            // Заполнение массива элементами типа TreeItem
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

            // Определение событий для элементов дерева
            foreach (TreeItem treeItem in treeItems)
            {
                foreach (TreeViewItem treeViewItem in treeItem.Item.Items)
                {
                    treeViewItem.Selected += TreeViewItem_Selected;
                    treeViewItem.Unselected += TreeViewItem_Unselected;
                }
            }

            // Отображение элементов в дереве
            treeView.ItemsSource = new List<TreeViewItem> {
                parcel,
                objectRealty,
                spatialData,
                bound,
                zone
            };
        }

        // Событие при снятии выделения с элемента дерева
        private void TreeViewItem_Unselected(object sender, RoutedEventArgs e)
        {
            SaveNodes.IsEnabled = false;

            if (!IsPressedCtrl())
            {
                ClearTreeViewItems();
            }
        }

        // Убрать заливку с элементов дерева
        private void ClearTreeViewItems()
        {
            foreach (TreeItem treeItem in treeItems)
            {
                treeItem.ClearTreeViewItem();
            }
        }

        // Установить заливку для элементов дерева
        private void SelectTreeViewItem(TreeViewItem item)
        {
            item.Background = new SolidColorBrush(SystemColors.HighlightColor);
            item.Foreground = new SolidColorBrush(SystemColors.HighlightTextColor);
        }

        // Проверка на нажитие клавиши Ctrl
        private bool IsPressedCtrl()
        {
            return Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
        }

        // Проверка на нажитие клавиши Shift
        private bool IsPressedShift()
        {
            return Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
        }

        // Проверка на заполненность массива с выделенными элементами дерева
        private bool IsHaveSelectedItems() {
            return selectedItems[0] != null ? true : false;
        }

        // Событие при выделении элемента дерева
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

        // Сохранение выделенных элементов дерева в XML-файл
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
    // Класс для элемента дерева со списком узлов и названием идентификатора
    class TreeItem
    {
        // Элемент дерева
        public TreeViewItem Item;
        // Список узлов
        public XmlNodeList Nodes;
        // Название идентификатора
        public string NameKey;
        
        public TreeItem(TreeViewItem Item, XmlNodeList Nodes, string NameKey)
        {
            this.Item = Item;
            this.Nodes = Nodes;
            this.NameKey = NameKey;

            FillItem();
        }
        // Поиск узла по названию идентификатора
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
        // Поиск ключа в дереве
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
        // Заполнить элементами дерево
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
        // Получить идентификатор узла
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
        // Очистить элемент дерева
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
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using DeskFrame.Core;
using System;

#nullable enable

namespace DeskFrame
{
    public partial class CategoryManagerWindow : Window
    {
        private ObservableCollection<DesktopCategory> _categories;
        private InstanceController _controller;
        private bool _xamlLoaded;
        private System.Windows.Controls.ListView? GetCategoryList() => FindName("CategoryList") as System.Windows.Controls.ListView;
        private System.Windows.Controls.TextBox? GetNameBox() => FindName("NameBox") as System.Windows.Controls.TextBox;
        private System.Windows.Controls.TextBox? GetExtensionsBox() => FindName("ExtensionsBox") as System.Windows.Controls.TextBox;
        private System.Windows.Controls.TextBox? GetRegexBox() => FindName("RegexBox") as System.Windows.Controls.TextBox;
        private System.Windows.Controls.CheckBox? GetCatchAllBox() => FindName("CatchAllBox") as System.Windows.Controls.CheckBox;
        public CategoryManagerWindow(InstanceController controller)
        {
            ManualInitializeComponent();
            _controller = controller;
            var cats = DesktopCategoryManager.LoadCategories();
            _categories = new ObservableCollection<DesktopCategory>(cats.Select(c => new DesktopCategory
            {
                Name = c.Name,
                Enabled = c.Enabled,
                Extensions = c.Extensions.ToList(),
                Order = c.Order,
                Regex = c.Regex,
                CatchAll = c.CatchAll
            }).OrderBy(c => c.Order));
            var list = GetCategoryList();
            if (list != null)
            {
                list.ItemsSource = _categories;
                list.SelectionChanged += CategoryList_SelectionChanged;
            }
        }

        private void CategoryList_SelectionChanged(object? sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var list = GetCategoryList();
            var nameBox = GetNameBox();
            var extBox = GetExtensionsBox();
            var regexBox = GetRegexBox();
            var catchAll = GetCatchAllBox();
            if (list != null && list.SelectedItem is DesktopCategory cat && nameBox!=null && extBox!=null && regexBox!=null && catchAll!=null)
            {
                nameBox.Text = cat.Name;
                extBox.Text = string.Join(",", cat.Extensions);
                regexBox.Text = cat.Regex ?? string.Empty;
                catchAll.IsChecked = cat.CatchAll;
            }
        }

        private void Add_Click(object? sender, RoutedEventArgs e)
        {
            var newCat = new DesktopCategory { Name = "NeueKategorie", Enabled = true, Order = _categories.Count };
            _categories.Add(newCat);
            var list = GetCategoryList();
            if (list != null) list.SelectedItem = newCat;
        }
        private void Remove_Click(object? sender, RoutedEventArgs e)
        {
            var list = GetCategoryList();
            if (list != null && list.SelectedItem is DesktopCategory cat)
            {
                _categories.Remove(cat);
                Reorder();
            }
        }
        private void Up_Click(object? sender, RoutedEventArgs e)
        {
            var list = GetCategoryList();
            if (list != null && list.SelectedItem is DesktopCategory cat)
            {
                int index = _categories.IndexOf(cat);
                if (index > 0)
                {
                    _categories.Move(index, index - 1);
                    Reorder();
                }
            }
        }
        private void Down_Click(object? sender, RoutedEventArgs e)
        {
            var list = GetCategoryList();
            if (list != null && list.SelectedItem is DesktopCategory cat)
            {
                int index = _categories.IndexOf(cat);
                if (index < _categories.Count - 1)
                {
                    _categories.Move(index, index + 1);
                    Reorder();
                }
            }
        }
        private void Reorder()
        {
            for (int i = 0; i < _categories.Count; i++) _categories[i].Order = i;
        }
        private void Save_Click(object? sender, RoutedEventArgs e)
        {
            // Falls gerade selektierte bearbeitet wurde
            var list = GetCategoryList();
            var nameBox = GetNameBox();
            var extBox = GetExtensionsBox();
            var regexBox = GetRegexBox();
            var catchAll = GetCatchAllBox();
            if (list != null && list.SelectedItem is DesktopCategory selected && nameBox!=null && extBox!=null && regexBox!=null && catchAll!=null)
            {
                selected.Name = nameBox.Text.Trim();
                selected.Extensions = extBox.Text.Split(',', System.StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
                selected.Regex = string.IsNullOrWhiteSpace(regexBox.Text) ? null : regexBox.Text.Trim();
                selected.CatchAll = catchAll.IsChecked == true;
            }
            DesktopCategoryManager.SaveCategories(_categories);
            // Refresh: entferne alte Kategorie-Instanzen und neu erzeugen
            _controller.RefreshDesktopCategoryInstances();
        }
        private void ManualInitializeComponent()
        {
            if (_xamlLoaded) return;
            _xamlLoaded = true;
            var uri = new Uri("/DeskFrame;component/CategoryManagerWindow.xaml", UriKind.Relative);
            System.Windows.Application.LoadComponent(this, uri);
        }
        private void Close_Click(object? sender, RoutedEventArgs e) => Close();
    }
}

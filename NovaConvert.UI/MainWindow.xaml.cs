using NovaConvert.UI.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace NovaConvert.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
    }

    private async void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0 && DataContext is MainViewModel vm)
            {
                await vm.HandleDroppedFilesAsync(files);
            }
        }
    }

    private void CloseToast_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.IsToastVisible = false;
        }
    }

    private void Backdrop_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.CloseModalCommand.Execute(null);
        }
    }

    private void Dialog_MouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }
}

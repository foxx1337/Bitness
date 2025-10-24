using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Bitness.Core;

namespace Bitness.GUI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
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
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
            {
                LoadFile(files[0]);
            }
        }
    }

    private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        // Only open file dialog if no file is currently loaded (instruction text is visible)
        if (InstructionText.Visibility == Visibility.Visible)
        {
            OpenFileDialog();
        }
    }

    private void OpenFileDialog()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Executable and DLL files|*.exe;*.dll|All files|*.*",
            Title = "Select a file to analyze"
        };

        if (dialog.ShowDialog() == true)
        {
            LoadFile(dialog.FileName);
        }
    }

    private void LoadFile(string filePath)
    {
        try
        {
            StatusText.Text = "Analyzing...";
            ErrorText.Visibility = Visibility.Collapsed;

            var architecture = BitnessChecker.GetMachineTypeString(filePath);

            // Show file information
            FilePathText.Text = $"File: {filePath}";
            ArchitectureText.Text = $"Architecture: {architecture}";
            
            // Update visibility
            InstructionText.Visibility = Visibility.Collapsed;
            FileInfoPanel.Visibility = Visibility.Visible;
            
            StatusText.Text = "Analysis complete";
        }
        catch (FileNotFoundException)
        {
            ShowError($"File not found: {filePath}");
        }
        catch (InvalidDataException ex)
        {
            ShowError(ex.Message);
        }
        catch (IOException ex)
        {
            ShowError(ex.Message);
        }
        catch (Exception ex)
        {
            ShowError($"Unexpected error: {ex.Message}");
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
        StatusText.Text = "Error occurred";
        
        // Keep current display state - don't hide file info if it's already shown
    }

    public void LoadFileFromCommandLine(string filePath)
    {
        if (!string.IsNullOrEmpty(filePath))
        {
            LoadFile(filePath);
        }
    }
}

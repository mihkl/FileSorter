using Avalonia.Controls;
using System.IO;
using System;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Directory = System.IO.Directory;
using System.Linq;
using Avalonia.Interactivity;
using System.Threading.Tasks;

namespace FileSorter.Views;

public partial class MainWindow : Window
{
    private double sortedCount;
    public MainWindow()
    {
        InitializeComponent();
        SelectFolderUnsorted.Click += SelectFolderUnsorted_Click;
        SelectFolderMaster.Click += SelectFolderMaster_Click;
        SortButton.Click += SortButton_Click;
        SortingProgressBar.IsVisible = false;
    }
    private void SortFiles(string masterPath, string unsortedPath, IProgress<int> progress) {

        int totalCount = Directory.GetFiles(unsortedPath).Length;

        foreach (var file in Directory.EnumerateFiles(unsortedPath)) {
            DateTime dateTimeOriginal;

            try {
                var directories = ImageMetadataReader.ReadMetadata(file);
                var subIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();

                if (subIfdDirectory != null && subIfdDirectory.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out dateTimeOriginal)) {
                } else {
                    dateTimeOriginal = File.GetCreationTime(file);
                }
            }
            catch (Exception) {
                dateTimeOriginal = File.GetCreationTime(file);
            }

            string month = dateTimeOriginal.ToString("MMMM");
            string year = dateTimeOriginal.ToString("yyyy");

            string destinationPath = Path.Combine(masterPath, $"{year} {month}");
            if (!Directory.Exists(destinationPath)) {
                Directory.CreateDirectory(destinationPath);
            }
            File.Move(file, Path.Combine(destinationPath, Path.GetFileName(file)));
            sortedCount++;
            int percentComplete = (int)(sortedCount / totalCount * 100);
            progress.Report(percentComplete);
        }
    }
    private void ShowCompletionMessage() {
        PopupText.Text = $"{sortedCount} files sorted";
        CompletionPopup.IsOpen = true;
        SortingProgressBar.Value = 100;
    }
    private void ClosePopup_Click(object sender, RoutedEventArgs e) {
        CompletionPopup.IsOpen = false;
        SortingProgressBar.Value = 0;
        SortingProgressBar.IsVisible = false;
        sortedCount = 0;
    }

    private async void SelectFolderUnsorted_Click(object? sender, RoutedEventArgs e) {
        var folderPath = await SelectFolder();
        if (!string.IsNullOrEmpty(folderPath)) {
            UnsortedPath.Text = folderPath;
        }
    }

    private async void SelectFolderMaster_Click(object? sender, RoutedEventArgs e) {
        var folderPath = await SelectFolder();
        if (!string.IsNullOrEmpty(folderPath)) {
            MasterPath.Text = folderPath;
        }
    }

    private async void SortButton_Click(object? sender, RoutedEventArgs e) {
        string? masterPath = MasterPath.Text;
        string? unsortedPath = UnsortedPath.Text;
        if (string.IsNullOrEmpty(masterPath) || string.IsNullOrEmpty(unsortedPath)) {
            return;
        }
        SortingProgressBar.IsVisible = true;
        var progress = new Progress<int>(percent =>
        {
            SortingProgressBar.Value = percent;
        });
        await Task.Run(() => SortFiles(masterPath, unsortedPath, progress));

        ShowCompletionMessage();
    }

    private async Task<string?> SelectFolder() {
        var dialog = new OpenFolderDialog();
        var result = await dialog.ShowAsync(this);
        return result;
    }
}

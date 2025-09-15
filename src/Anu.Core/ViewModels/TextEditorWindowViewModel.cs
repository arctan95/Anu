using System.Threading.Tasks;
using Anu.Core.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Anu.Core.ViewModels;

public partial class TextEditorWindowViewModel : ViewModelBase
{
    [ObservableProperty] private string? _filePath;
    [ObservableProperty] private string? _text;

    public async Task SaveFile(string text)
    {
        if (FilePath != null)
        {
            await FileUtil.SaveFileAsync(FilePath, text);
        }
    }
}
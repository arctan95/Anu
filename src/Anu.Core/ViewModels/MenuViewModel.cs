using System.Collections;

namespace Anu.Core.ViewModels;

public partial class MenuViewModel : ViewModelBase
{
    private string _header;
    private IEnumerable? _itemSource;

    public string Header
    {
        set => _header = value;
        get => _header;
    }

    public IEnumerable? Items
    {
        set => _itemSource = value;
        get => _itemSource;
    }

}
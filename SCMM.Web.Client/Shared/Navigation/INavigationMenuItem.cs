
public interface INavigationMenuItem
{
}

public class NavigationMenuItemGroup : INavigationMenuItem
{
    public string Icon { set; get; }

    public string Title { set; get; }

    public IList<INavigationMenuItem> Children { get; set; }

    public bool Disabled { get; set; }
}

public class NavigationMenuItemLink : INavigationMenuItem
{
    public string Icon { set; get; }

    public string Title { set; get; }

    public string SubTitle { get; set; }

    public string Path { set; get; }

    public Action OnClick { set; get; }

    public bool Prefix { set; get; }

    public bool Disabled { get; set; }
}

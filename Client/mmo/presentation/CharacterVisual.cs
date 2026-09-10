using Godot;

namespace NuevoMMO.GodotClient;

public partial class CharacterVisual : Node2D
{
    public Node2D Body { get; private set; } = null!;
    public Node2D Tradition { get; private set; } = null!;
    public Node2D Head { get; private set; } = null!;
    public Node2D Chest { get; private set; } = null!;
    public Node2D Weapon { get; private set; } = null!;
    public Node2D OffHand { get; private set; } = null!;
    public Node2D Cape { get; private set; } = null!;
    public Node2D Effects { get; private set; } = null!;

    public override void _Ready()
    {
        Body = Layer("Body");
        Tradition = Layer("Tradition");
        Head = Layer("Head");
        Chest = Layer("Chest");
        Weapon = Layer("Weapon");
        OffHand = Layer("OffHand");
        Cape = Layer("Cape");
        Effects = Layer("Effects");
    }

    private Node2D Layer(string name)
    {
        var node = new Node2D { Name = name };
        AddChild(node);
        return node;
    }
}

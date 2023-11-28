namespace poqbox;

public static class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("-----------poqbox----------");
        Console.WriteLine("Testing environment for poq");

        PoqboxGame game = new();
        game.Run();
    }
}

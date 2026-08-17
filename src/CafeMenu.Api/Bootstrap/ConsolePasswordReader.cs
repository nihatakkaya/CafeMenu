namespace CafeMenu.Api.Bootstrap;

public sealed class ConsolePasswordReader : IConsolePasswordReader
{
    public string ReadPassword(string prompt)
    {
        if (Console.IsInputRedirected)
        {
            throw new InvalidOperationException("Interactive password input requires a terminal.");
        }

        Console.Write(prompt);

        var characters = new List<char>();

        while (true)
        {
            var key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return new string(characters.ToArray());
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (characters.Count > 0)
                {
                    characters.RemoveAt(characters.Count - 1);
                }

                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                characters.Add(key.KeyChar);
            }
        }
    }
}

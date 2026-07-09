using Jx3.MockServer;

try
{
    new Server(9000).Start();
}
catch (Exception ex)
{
    Console.WriteLine($"Fatal: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}

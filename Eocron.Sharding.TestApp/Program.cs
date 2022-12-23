var mode = Enum.Parse<TestMode>(args[0], true);
switch (mode)
{
    case TestMode.Stream:
        StreamMode();
        break;
    case TestMode.ErrorImmediately:
    default:
        throw new Exception("Test immediate exception");
}


static void StreamMode()
{
    while (true)
    {
        var line = Console.ReadLine();
        Console.WriteLine(line);
        if (line == "stop")
        {
            break;
        }

        if (line == "hang")
        {
            Thread.Sleep(Timeout.Infinite);
        }
    }
}
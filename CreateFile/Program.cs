if (args.Length != 3)
{
    Console.WriteLine("Usage: CreateFile <outputFilePath> <numberOfLines> <sampleStringsFilePath>");
    return;
}

string outputFilePath = args[0];
if (!int.TryParse(args[1], out int numberOfLines))
{
    Console.WriteLine("The number of lines must be an integer.");
    return;
}

string sampleStringsFilePath = args[2];
if (!File.Exists(sampleStringsFilePath))
{
    Console.WriteLine("The sample strings file does not exist.");
    return;
}

List<string> sampleStrings = new List<string>();
using (StreamReader reader = new StreamReader(sampleStringsFilePath))
{
    string line;
    while ((line = reader.ReadLine()) != null)
    {
        if (!string.IsNullOrWhiteSpace(line))
        {
            sampleStrings.Add(line.Trim());
        }
    }
}

if (sampleStrings.Count == 0)
{
    Console.WriteLine("The sample strings file is empty or contains only whitespace.");
    return;
}

Random random = new Random();

using (StreamWriter writer = new StreamWriter(outputFilePath))
{
    for (int i = 0; i < numberOfLines; i++)
    {
        int number = random.Next(1, 100000);
        string text = sampleStrings[random.Next(sampleStrings.Count)];
        writer.WriteLine($"{number}. {text}");
    }
}

Console.WriteLine($"File '{outputFilePath}' with {numberOfLines} lines created successfully.");
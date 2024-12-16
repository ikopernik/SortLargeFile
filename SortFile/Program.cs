using System.Collections.Generic;
using System.IO;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;

if (args.Length != 2)
{
    Console.WriteLine("Usage: SortFile <inputFilePath> <outputFilePath>");
    return;
}

string inputFilePath = args[0];
string outputFilePath = args[1];

if (!File.Exists(inputFilePath))
{
    Console.WriteLine("The input file does not exist.");
    return;
}

string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
Directory.CreateDirectory(tempDirectory);

int sampleSize = 1000; // Number of lines to sample for average length calculation
long totalLength = 0;
int lineCount = 0;

// Calculate the average length of the lines
using (StreamReader reader = new StreamReader(inputFilePath))
{
    string line;
    while (lineCount < sampleSize && (line = reader.ReadLine()) != null)
    {
        totalLength += line.Length;
        lineCount++;
    }
}

if (lineCount == 0)
{
    Console.WriteLine("The input file is empty.");
    return;
}

double averageLineLength = (double)totalLength / lineCount;

// Determine available physical memory
var availableMemoryCounter = new PerformanceCounter("Memory", "Available MBytes");
long availableMemory = (long)availableMemoryCounter.NextValue() * 1024 * 1024; // Convert MB to bytes

// Use a portion of the available memory for the chunk size
long chunkSizeInBytes = availableMemory / 16;
int chunkSize = (int)(chunkSizeInBytes / averageLineLength); // Number of lines per chunk

Console.WriteLine($"Average line length: {averageLineLength} bytes");
Console.WriteLine($"Available memory: {availableMemory / (1024 * 1024)} MB");
Console.WriteLine($"Chunk size: {chunkSize} lines");

// Split the input file into sorted chunks
using (StreamReader reader = new StreamReader(inputFilePath))
{
    List<(int Number, string Text)> lines = new List<(int, string)>();
    string line;
    int chunkIndex = 0;
    while ((line = reader.ReadLine()) != null)
    {
        int dotIndex = line.IndexOf('.');
        if (dotIndex > 0)
        {
            if (int.TryParse(line.Substring(0, dotIndex), out int number))
            {
                string text = line.Substring(dotIndex + 1).Trim();
                lines.Add((number, text));
            }
        }

        if (lines.Count >= chunkSize)
        {
            lines = lines.OrderBy(x => x.Text, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Number).ToList();
            WriteChunkToFile(Path.Combine(tempDirectory, $"chunk_{chunkIndex}.txt"), lines);
            lines.Clear();
            chunkIndex++;
            GC.Collect(); // Force garbage collection
        }
    }

    if (lines.Count > 0)
    {
        lines = lines.OrderBy(x => x.Text, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Number).ToList();
        WriteChunkToFile(Path.Combine(tempDirectory, $"chunk_{chunkIndex}.txt"), lines);
    }
}

// Iteratively merge sorted chunks
IterativeMergeChunks(tempDirectory, outputFilePath, 100);

// Clean up temporary files
Directory.Delete(tempDirectory, true);

Console.WriteLine($"File '{outputFilePath}' sorted successfully.");

static void WriteChunkToFile(string chunkFilePath, List<(int Number, string Text)> lines)
{
    using (StreamWriter writer = new StreamWriter(chunkFilePath))
    {
        foreach (var line in lines)
        {
            writer.WriteLine($"{line.Number}. {line.Text}");
        }
    }
}

static void IterativeMergeChunks(string tempDirectory, string outputFilePath, int maxOpenChunks)
{
    var chunkFiles = Directory.GetFiles(tempDirectory, "chunk_*.txt").ToList();
    int iteration = 0;

    while (chunkFiles.Count > 1)
    {
        var newChunkFiles = new List<string>();
        for (int i = 0; i < chunkFiles.Count; i += maxOpenChunks)
        {
            var currentChunks = chunkFiles.Skip(i).Take(maxOpenChunks).ToList();
            string mergedChunkFile = Path.Combine(tempDirectory, $"merged_{iteration}_{i / maxOpenChunks}.txt");
            MergeChunks(currentChunks, mergedChunkFile);
            newChunkFiles.Add(mergedChunkFile);
        }
        chunkFiles = newChunkFiles;
        iteration++;
    }

    // Rename the final merged chunk to the output file
    if (chunkFiles.Count == 1)
    {
        File.Move(chunkFiles[0], outputFilePath, true);
    }
}

static void MergeChunks(List<string> chunkFiles, string outputFilePath)
{
    var readers = new List<StreamReader>();
    var comparer = new LineComparer();
    var minHeap = new PriorityQueue<(string Line, int ChunkIndex), (string Line, int ChunkIndex)>(comparer);

    // Initialize readers and min-heap
    for (int i = 0; i < chunkFiles.Count; i++)
    {
        var reader = new StreamReader(chunkFiles[i]);
        readers.Add(reader);
        if (reader.Peek() >= 0)
        {
            var line = reader.ReadLine();
            minHeap.Enqueue((line, i), (line, i));
        }
    }

    using (StreamWriter writer = new StreamWriter(outputFilePath))
    {
        while (minHeap.Count > 0)
        {
            var smallest = minHeap.Dequeue();

            writer.WriteLine(smallest.Line);

            int chunkIndex = smallest.ChunkIndex;

            if (readers[chunkIndex].Peek() >= 0)
            {
                string nextLine = readers[chunkIndex].ReadLine();
                minHeap.Enqueue((nextLine, chunkIndex), (nextLine, chunkIndex));
            }

            if (minHeap.Count == 1)
            {
                // Only one chunk file is left. Read all text from it and write directly to the output file.
                int remainingChunkIndex = minHeap.Peek().ChunkIndex;

                writer.WriteLine(minHeap.Dequeue().Line);

                string line;
                while ((line = readers[remainingChunkIndex].ReadLine()) != null)
                {
                    writer.WriteLine(line);
                }
            }
        }
    }

    // Close readers
    foreach (var reader in readers)
    {
        reader.Close();
    }

    // Delete the original chunk files
    foreach (var chunkFile in chunkFiles)
    {
        File.Delete(chunkFile);
    }
}
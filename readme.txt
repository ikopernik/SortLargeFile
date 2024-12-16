I used book in the CreateFile\SampleStrings.txt to generate file for testing.
To generate file call CreateFile.exe output.txt 1000000 SampleStrings.txt
output.txt - file path and name of the output file
10000000 - number of lines you want to generate in the output file
SampleStrings.txt - dictionary file. Every line from this file is taken randomly.

To sort lines, call SortFile.exe input.txt output.txt

Notes. File is separated into smaller parts. Part in every file (chunk) is sorted and saved in the temporary file. Size of the chunk is calculated based on the free memory. So application shouldn't force Windows to swap memory.
After that chunks are combined in the output file. As I don't know how many chunks can be created, I don't want to open all of them simultaneously. The limit to the concurrently opened chunks is set to 100. If number of chunks exceeds this limit, they are processed iteratively. For example, we have 180 chunks. On the first iteration we process 100 and create temporary chunk from it, next we process 80 and create another temporary chunk. After that we combine these two big chunks into one output file.
using System.Diagnostics;

namespace LlmChat.API.Services;

public static class PythonService
{
    private const string documentationFilename = "/app/HandoverPDFs/HandoverDocumentation.pdf";

    // Creates temporary .py file, runs the file through external process, captures it's stdout
    public static async Task<string> RunPythonCode(string prompt)
    {
        string result = "";                                 // Stores output from python driver code
        string tempFile = $"{Path.GetRandomFileName()}.py";   // Temporary file, stores python driver code

        try
        {
            // Dynamically creates string containing python code
            string driverCode = DriverCodeGenerator(prompt, documentationFilename);

            // Writes (async) generated Python code to the temporary file
            await File.WriteAllTextAsync(tempFile, driverCode);

            // Starts a new process to execute the Python script (driver code)
            using (Process process = new Process())
            {
                process.StartInfo.FileName = "python3";             // Executable to run
                process.StartInfo.Arguments = tempFile;             // Pass the temporary file (driver code)
                process.StartInfo.RedirectStandardOutput = true;    // Capture stdout
                process.StartInfo.RedirectStandardError = true;     // Capture stderr
                process.StartInfo.UseShellExecute = false;          // False - allows redirection of stdout & err
                process.StartInfo.CreateNoWindow = true;            // Runs in the background (no window required)
                process.Start();                                    // Start process (execute script in temp file)

                var exitTask = process.WaitForExitAsync(); // Waits for process to finish

                // Time limit defined by Task delay, Which ever task finishes first
                if (await Task.WhenAny(
                        exitTask, Task.Delay(10000000)) == exitTask)
                {
                    result = await process.StandardOutput.ReadToEndAsync(); // Read process output
                }
                else
                {
                    process.Kill();
                    throw new TimeoutException();
                }
            }

            // Returns value caught in the stdout of external process
            return result;
        }
        catch (TimeoutException)     // Process (execution of driver code) ran for too long
        {
            return "Timeout after 10000sec";
        }
        finally
        {
            File.Delete(tempFile);      // Ensure temporary file is deleted after
        }
    }

    // Dynamically generates driver code, injects values into variables, returns through !!PRINT and stdout!!
    // Does not use return, the result value must be printed out to the standard output
    private static string DriverCodeGenerator(string prompt, string filename)
    {
        string driverCode = $@"

import weaviate
from weaviate.classes.config import Property, DataType
from weaviate.classes.config import Configure

import ollama
from ollama import Client

from pypdf import PdfReader
ollamaClient = Client(
    host=""http://ollama:11434"",
    headers={{'x-some-header': 'some-value'}}
)

def getStringsFromPDF(filename):

    try:
        # creating a pdf reader object
        reader = PdfReader(filename)
    except Exception as e:
        print(f""An error occurred: {{e}}"")

    # extracting text from page
    text = """"
    for page in reader.pages:
        text += ""\n"" + page.extract_text()

    paragraphs = []
    paragraph = "" ""
    for _character in text:
        if len(paragraph)>=2:
            if (_character == ""\n"" and paragraph[-2] == ""\n""):
                paragraphs.append(paragraph)
                paragraph = """"

        paragraph += _character

    return paragraphs

def restartOrCreateCollection(client, name = ""docs""):

    if client.collections.exists(name):
        client.collections.delete(name)
        collection = client.collections.get(name)
    else:
        collection = client.collections.create(
            name = name, # Name of the data collection
            properties=[
                Property(name=""text"",
                        data_type=DataType.TEXT), # Name and data type of the property
            ],
        )
    return collection

def saveVectorsFromListOfStrings(collection, strings):
    with collection.batch.dynamic() as batch:
        for _, d in enumerate(strings):
            # Generate embeddings
            embedding = ollamaClient.embeddings(model = ""all-minilm"",
            # embedding = ollama.embeddings(model = ""all-minilm"",
                                        prompt = d)
            # Add data object with text and embedding
            batch.add_object(properties = {{""text"" : d}},
                            vector = embedding[""embedding""],
            )

def craftPrompt(closestVectors, question):
    prompt_template = ""Based on this data:""
    for paragrapg in closestVectors:
        prompt_template += paragrapg + '\n'

    prompt_template += f""-------------------------\nRespond to this prompt: {{question}}""
    return prompt_template

def main():
    prompt = ""{prompt}""
    document = getStringsFromPDF(""{filename}"")
    try:
        print(""before connection -> "")
        client = weaviate.connect_to_custom(
            http_host=""weaviate"",        # Hostname for the HTTP API connection
            http_port=8080,              # Default is 80, WCD uses 443
            http_secure=False,           # Whether to use https (secure) for the HTTP API connection
            grpc_host=""weaviate"",        # Hostname for the gRPC API connection
            grpc_port=50051,              # Default is 50051, WCD uses 443
            grpc_secure=False           # Whether to use a secure channel for the gRPC API connection
        )
        print(""- after connection -> "")
        collection = restartOrCreateCollection(client)

        saveVectorsFromListOfStrings(collection, document)

        # An example prompt
        #prompt = ""At Part 4, the board doesn’t boot or signal. What to do?""

        # Generate an embedding for the prompt and retrieve the most relevant doc
        # response = ollama.embeddings(
        response = ollamaClient.embeddings(
        model = ""all-minilm"",
        prompt = prompt,
        )

        results = collection.query.near_vector(near_vector = response[""embedding""],
                                            limit = 3)

        closestVectors = []
        for result in results.objects:
            #print(result.properties['text'])
            closestVectors.append(result.properties['text'])

        prompt_template = craftPrompt(closestVectors, prompt)

        print(""\n"", prompt_template,""\n"")

        # llmOutput = ollama.generate(
        llmOutput = ollamaClient.generate(
        model = ""llama2"",
        prompt = prompt_template,
        )

        print(llmOutput['response'],""\n"")
    except Exception as e:
        print(f""An error occurred: {{e}}"")

    finally:
        client.close()

if __name__ == '__main__':
    main()
    ";
        return driverCode;
    }
}
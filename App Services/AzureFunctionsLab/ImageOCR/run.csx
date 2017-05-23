#r "System.IO"
#r "System.Runtime"
#r "System.Threading.Tasks"

#r "Microsoft.WindowsAzure.Storage"

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;
using Microsoft.WindowsAzure.Storage.Table;

public class ImageText : TableEntity
{
    public string Text { get; set; }
    public string Uri {get; set; }
}

public static void Run( ICloudBlob myBlob, ICollector<ImageText> outputTable, TraceWriter log) 
{
     try  
    {
        using (Stream imageFileStream = new MemoryStream())
        {

            myBlob.DownloadToStream(imageFileStream); 
            log.Info($"stream length = {imageFileStream.Length}"); // just to verify

            //
            var visionClient = new VisionServiceClient("KEY REMOVED for GIT");

            // reset stream position to begining 
            imageFileStream.Position = 0;
            // Upload an image and perform OCR
            var ocrResult = visionClient.RecognizeTextAsync(imageFileStream, "en");
            //log.Info($"ocrResult");

            string OCRText = LogOcrResults(ocrResult.Result);
            log.Info($"image text = {OCRText}");

            outputTable.Add(new ImageText()
                            {
                                PartitionKey = "TryFunctions",
                                RowKey = myBlob.Name,
                                Text = OCRText,
                                Uri = myBlob.Uri.ToString()
                            });            
        }

    }
    catch (Exception e) 
    {
        log.Info(e.Message);
    }
}

// helper function to parse OCR results 
static string LogOcrResults(OcrResults results)
{
    StringBuilder stringBuilder = new StringBuilder();
    if (results != null && results.Regions != null)
    {
        stringBuilder.Append(" ");
        stringBuilder.AppendLine();
        foreach (var item in results.Regions)
        {
            foreach (var line in item.Lines)
            {
                foreach (var word in line.Words)
                {
                    stringBuilder.Append(word.Text);
                    stringBuilder.Append(" ");
                }
                stringBuilder.AppendLine();
            }
            stringBuilder.AppendLine();
        }
    }
    return stringBuilder.ToString();
}
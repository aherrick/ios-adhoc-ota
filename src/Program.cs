using System.Text;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

if (args.Length != 8)
{
    Console.Error.WriteLine(
        "Expected 8 arguments: APP_NAME BUNDLE_ID VERSION IPA_NAME IPA_PATH STATIC_URL AZURE_STORAGE_ACCOUNT AZURE_STORAGE_KEY"
    );
    return 1;
}

var appName = args[0];
var bundleId = args[1];
var version = args[2];
var ipaName = args[3];
var ipaPath = args[4];
var staticUrl = args[5].TrimEnd('/');
var storageAccount = args[6];
var storageKey = args[7];

var workdir = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE") ?? "/github/workspace";
var outputDir = Path.Combine(workdir, "output-dotnet");
Directory.CreateDirectory(outputDir);

var ipaUrl = $"{staticUrl}/ios/{ipaName}";
var plistUrl = $"{staticUrl}/ios/manifest.plist";
var landingPageUrl = $"{staticUrl}/ios/index.html";

// Generate manifest.plist
var plistContent = ManifestGenerator.GeneratePlist(appName, bundleId, version, ipaUrl);
var plistPath = Path.Combine(outputDir, "manifest.plist");
File.WriteAllText(plistPath, plistContent, new UTF8Encoding(false));

// Generate index.html
var htmlContent = ManifestGenerator.GenerateHtml(appName, version, plistUrl);
var htmlPath = Path.Combine(outputDir, "index.html");
File.WriteAllText(htmlPath, htmlContent, new UTF8Encoding(false));

// Upload to Azure Blob static website ($web container)
var connectionString =
    $"DefaultEndpointsProtocol=https;AccountName={storageAccount};AccountKey={storageKey};EndpointSuffix=core.windows.net";
var serviceClient = new BlobServiceClient(connectionString);
var containerClient = serviceClient.GetBlobContainerClient("$web");
await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

async Task UploadAsync(string name, string filePath, string contentType)
{
    var blobClient = containerClient.GetBlobClient(name);
    await using var stream = File.OpenRead(filePath);
    await blobClient.UploadAsync(
        stream,
        new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } }
    );
    Console.WriteLine($"Uploaded: {name}");
}

await UploadAsync($"ios/{ipaName}", Path.Combine(workdir, ipaPath), "application/octet-stream");
await UploadAsync("ios/manifest.plist", plistPath, "application/xml");
await UploadAsync("ios/index.html", htmlPath, "text/html");

// Write GitHub Action outputs
var githubOutput = Environment.GetEnvironmentVariable("GITHUB_OUTPUT");
if (!string.IsNullOrEmpty(githubOutput))
{
    await File.AppendAllLinesAsync(
        githubOutput,
        [$"landing_page_url={landingPageUrl}", $"ipa_url={ipaUrl}", $"manifest_url={plistUrl}"]
    );
}

return 0;

internal static class ManifestGenerator
{
    public static string GeneratePlist(
        string appName,
        string bundleId,
        string version,
        string ipaUrl
    )
    {
        return $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
            <plist version="1.0">
            <dict>
                <key>items</key>
                <array>
                    <dict>
                        <key>assets</key>
                        <array>
                            <dict>
                                <key>kind</key>
                                <string>software-package</string>
                                <key>url</key>
                                <string>{ipaUrl}</string>
                            </dict>
                        </array>
                        <key>metadata</key>
                        <dict>
                            <key>bundle-identifier</key>
                            <string>{bundleId}</string>
                            <key>bundle-version</key>
                            <string>{version}</string>
                            <key>kind</key>
                            <string>software</string>
                            <key>title</key>
                            <string>{appName}</string>
                        </dict>
                    </dict>
                </array>
            </dict>
            </plist>
            """;
    }

    public static string GenerateHtml(string appName, string version, string plistUrl)
    {
        var encodedPlistUrl = Uri.EscapeDataString(plistUrl);
        var installUrl = $"itms-services://?action=download-manifest&url={encodedPlistUrl}";

        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Install {{appName}}</title>
                <style>
                    body { font-family: -apple-system, sans-serif; text-align: center; padding-top: 50px; }
                    .btn { background: #007aff; color: white; padding: 15px 30px; text-decoration: none; border-radius: 10px; font-size: 18px; }
                </style>
            </head>
            <body>
                <h1>{{appName}} v{{version}}</h1>
                <p><a href="{{installUrl}}" class="btn">Install App</a></p>
            </body>
            </html>
            """;
    }
}
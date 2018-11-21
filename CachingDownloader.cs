using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

static class CachingDownloader {
    const int CACHE_VERSION = 1;

    static readonly string CACHE_DIR = Path.Combine(Path.GetTempPath(), "github.com-AlekseyMartynov-bond-finder");
    static readonly HashAlgorithm HASH_ALGO = new SHA1CryptoServiceProvider();

    public static string Download(string url, TimeSpan ttl) {
        var cacheFileName = Path.Combine(CACHE_DIR, $"dl-{CACHE_VERSION}-{GetSha1String(url)}");

        if(!Directory.Exists(CACHE_DIR))
            Directory.CreateDirectory(CACHE_DIR);

        if(File.Exists(cacheFileName)) {
            if(DateTime.Now - new FileInfo(cacheFileName).LastWriteTime < ttl)
                return File.ReadAllText(cacheFileName);
        }

        using(var web = new WebClient()) {
            Console.Error.WriteLine(url);
            var text = web.DownloadString(url);
            File.WriteAllText(cacheFileName, text);
            return text;
        }
    }

    static string GetSha1String(string text) {
        return String.Join("", HASH_ALGO.ComputeHash(Encoding.UTF8.GetBytes(text)).Select(i => i.ToString("x2")));
    }

}

// See https://aka.ms/new-console-template for more information
using sysi.compiler;
using sysi.Utils;
using static System.Net.Mime.MediaTypeNames;

Console.WriteLine(Main.config.ignored_item_delimeter);
var filePath = "sysi/site/test.syl";
Console.WriteLine(new SyFile(filePath, File.ReadAllText(filePath), SyFile.Type.Syl).AsHtml());

public class Main {
    internal static Config config = Config.LoadConfig();
}

using sysi.compiler;
using sysi.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace sysi.Compiler {
    internal class SyCompiler {

        public static void CompileSite() {
            SyFragment.LoadFragments();
            foreach (string file in Directory.EnumerateFiles(Main.config.site_map, "*.syl", SearchOption.AllDirectories)) {
                if (Path.GetFileNameWithoutExtension(file).StartsWith(Main.config.ignored_item_delimiter)) {
                    Console.WriteLine($"Omitting {file}");

                    continue;
                }

                Console.WriteLine($"Compiling {file}");
                CompileSyFile(file);
            }
        }

        public static void CompileSyFile(string file, string? template = null) {
            var templatePath = Path.Combine(Main.config.template_folder, template ?? Main.config.default_template);
            var htmlFile = File.ReadAllText(templatePath);

            var syFile = new SyFile(file, File.ReadAllText(file), SyFile.Type.Syl);
            htmlFile = ReplaceFragments(htmlFile, syFile.AsHtml());

            var newPath = Path.Combine(Main.config.compiled_site, Path.GetRelativePath(Main.config.site_map, file));
            newPath = Path.ChangeExtension(newPath, "html");

            if (!File.Exists(newPath)) {
                Directory.CreateDirectory(Path.GetDirectoryName(newPath) ?? "");
                File.Create(newPath).Close();
            }
            File.WriteAllText(newPath, htmlFile);
        }

        private static string ReplaceFragments(string text, string body) {
            var fragmentRegex = new Regex("<!--\\s*\\!(\\w+)\\s*-->");

            var match = fragmentRegex.Match(text);
            if (!match.Success) {
                return text;
            }

            var name = match.Groups[1].Value;
            string fragmentText;
            if (name == "body") {
                fragmentText = body;
            }
            else if (name == "head") {
                fragmentText = "<title>My Web Page</title>";
            }
            else if (name == "navitems") {
                fragmentText = "<li><a>Home</a></li>";
            }
            else {
                fragmentText = SyFragment.fragmentMap.GetValueOrDefault(name)?.fragment ?? "";
            }

            string[] parts = fragmentRegex.Split(text, 2);

            return ReplaceFragments(parts[0] + fragmentText + parts[^1], body);
        }
    }
}

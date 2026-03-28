using sysi.compiler;
using System.Text.RegularExpressions;

namespace sysi.Compiler {
    internal class SyCompiler {

        public static void CompileSite() {
            SyFragment.LoadFragments();
            var tree = SyTree.BuildTree() as SyCategory;

            CompileSy(tree, tree);
        }
        public static void CompileSy(Sy sy, SyCategory? topLevelSy, string? template = null) {
            if (sy is SyFile) {
                var syFile = sy as SyFile;

                Console.WriteLine($"Compiling {syFile.path}");
                if (syFile.type.Equals(SyFile.Type.Text)) {
                    CompileTextFile(syFile);
                }
                else {
                    CompileSyFile(syFile, topLevelSy, template);
                }
            }
            else if (sy is SyCategory) {
                var syCategory = sy as SyCategory;
                if (syCategory.pageOnClick != null) {
                    CompileSyFile(syCategory.pageOnClick, topLevelSy, template, Path.ChangeExtension(syCategory.path, Path.GetExtension(syCategory.pageOnClick.path)));
                }
                foreach (Sy sychild in syCategory.children) {
                    CompileSy(sychild, topLevelSy, template);
                }
            }


        }
        private static void CompileSyFile(SyFile sy, SyCategory? topLevelSy, string? template = null, string? newPath = null) {
            var templatePath = Path.Combine(Main.config.template_folder, template ?? Main.config.default_template);
            var htmlFile = File.ReadAllText(templatePath);

            htmlFile = ReplaceTextFragments(htmlFile, sy.AsHtml(), sy, topLevelSy);
            if (newPath == null) {
                newPath = sy.GetCompiledPath();
            }
            else {
                newPath = Path.Combine(Main.config.compiled_site, Path.GetRelativePath(Main.config.site_map, newPath));
                newPath = Path.ChangeExtension(newPath, Main.config.page_extension);
                newPath = $"{Main.config.page_prefix}{Path.GetFileNameWithoutExtension(newPath)}";
            }

            if (!File.Exists(newPath)) {
                if (Path.GetDirectoryName(newPath) != null) {
                    Directory.CreateDirectory(Path.GetDirectoryName(newPath));
                    File.Create(newPath).Close();
                }
            }
            File.WriteAllText(newPath, htmlFile);
        }

        private static void CompileTextFile(SyFile sy) {
            var htmlFile = sy.AsHtml();

            var newPath = Path.Combine(Main.config.compiled_site, Path.GetRelativePath(Main.config.site_map, sy.path));
            newPath = Path.ChangeExtension(newPath, Main.config.page_extension);
            newPath = $"{Main.config.page_prefix}{Path.GetFileNameWithoutExtension(newPath)}";

            if (!File.Exists(newPath)) {
                Directory.CreateDirectory(Path.GetDirectoryName(newPath) ?? "");
                File.Create(newPath).Close();
            }
            File.WriteAllText(newPath, htmlFile);
        }

        private static string ReplaceTextFragments(string text, string body, Sy currentSy, SyCategory? topLevelSy = null) {
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
                fragmentText = $"<title>{currentSy.GetName()}</title>";
            }
            else if (name == "navitems" && topLevelSy != null) {
                fragmentText = MakeNavBar(currentSy, topLevelSy);
            }
            else {
                fragmentText = SyFragment.fragmentMap.GetValueOrDefault(name)?.fragment ?? "";
            }

            string[] parts = fragmentRegex.Split(text, 2);

            return ReplaceTextFragments(parts[0] + fragmentText + parts[^1], body, currentSy, topLevelSy);
        }

        private static string MakeNavBar(Sy currentSy,SyCategory topLevelSy) {
            string str = "";
            foreach (var category in topLevelSy.children) {
                string href = "";
                var subMenu = "";
                if (category is SyCategory) {
                    if (category.IsHidden() || category.IsIgnored()) {
                        continue;
                    }
                    subMenu += "<ul>";
                    if (((SyCategory)category)?.pageOnClick != null && ((SyCategory)category)?.pageOnClick?.path != "") {
                        var refPath = category.GetCompiledPath();
                        refPath = Path.GetRelativePath(Path.GetDirectoryName(currentSy.GetCompiledPath()), refPath);
                        href = $"href = {refPath}";
                    }
                    foreach (var subcategory in ((SyCategory)category).children) {
                        if (subcategory.IsHidden() || category.IsIgnored()) {
                            continue;
                        }
                        var subRefPath = subcategory.GetCompiledPath();
                        subRefPath = Path.GetRelativePath(Path.GetDirectoryName(currentSy.GetCompiledPath()), subRefPath);
                        var subhref =  $"href = {subRefPath}";

                        subMenu += $"<li><a {subhref}>{category.GetName()}</a></li>";
                    }
                    subMenu += "</ul>";
                }
                else {
                    var refPath = category.GetCompiledPath();
                    refPath = Path.GetRelativePath(Path.GetDirectoryName(currentSy.GetCompiledPath()), refPath);
                    href = $"href = {refPath}";
                }
                str += $"<li><a {href}> {category.GetName()} <a>";
                str += subMenu;
                str += "</li>";
            }
            return str;
        }
    }
}

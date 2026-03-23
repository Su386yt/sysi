using Pandoc;
using sysi.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using static sysi.compiler.SyCategory;

namespace sysi.compiler {
    internal class SyTree {
        public static SyCategory BuildTree() { return BuildSyFileTree(Main.config.site_map); }
        public static SyCategory BuildSyFileTree(string path) {
            List<Sy> tree = new List<Sy>();
            var files = Directory.GetFiles(path);

            foreach (var file in files) {
                SyFile.Type type;
                if (file.EndsWith(".syl")) {
                    type = SyFile.Type.Syl;
                }
                else if (file.EndsWith(".md")) {
                    type = SyFile.Type.Markdown;
                }
                else if (file.EndsWith(".html")) {
                    type = SyFile.Type.HTML;
                }
                else if (file.EndsWith(".txt")) {
                    type = SyFile.Type.Text;
                }
                else {
                    Console.WriteLine($"Omitting {file}");
                    continue;
                }
                if (Path.GetFileNameWithoutExtension(file).StartsWith(Main.config.ignored_item_delimiter)) {
                    Console.WriteLine($"Omitting {file}");
                    continue;
                }
                var text = File.ReadAllText(file);
                tree.Add(new SyFile(file, text, type));
            }

            var folders = Directory.GetDirectories(path);
            foreach (var folder in folders) {
                Console.WriteLine(folder);
                var filePath = Path.Combine(folder);
                tree.Add(BuildSyFileTree(filePath));
            }
            var config = new SyCategoryConfig();
            SyFile? pageOnClick = null;
            if (File.Exists(Path.Combine(path, ".about"))) {
                config = JsonSerializer.Deserialize<SyCategoryConfig>(File.ReadAllText(Path.Combine(path, ".about")));

                if (config?.page_on_click != null && config?.page_on_click != "") {
                    var filePath = Path.Combine(path, config.page_on_click);
                    var text = File.ReadAllText(filePath);
                    SyFile.Type type = SyFile.Type.Text;
                    if (filePath.EndsWith(".syl")) {
                        type = SyFile.Type.Syl;
                    }
                    else if (filePath.EndsWith(".md")) {
                        type = SyFile.Type.Markdown;
                    }
                    else if (filePath.EndsWith(".html")) {
                        type = SyFile.Type.HTML;
                    }
                    pageOnClick = new SyFile(filePath, text, type);
                }
            }

            return new SyCategory(path, tree.ToArray(), config, pageOnClick);
        }
    }


    internal abstract class Sy {
        public string path { get; }
        public Sy(string path) {
            this.path = path;
        }
        public bool IsHidden() {
            int length = Main.config.hidden_item_delimiter.Length;
            var name = Path.GetFileName(this.path);
            if (name.Length < length) {
                return false;
            }
            var start = name.Substring(0, length);
            return start.Equals(Main.config.hidden_item_delimiter);
        }
        public bool IsIgnored() {
            int length = Main.config.ignored_item_delimiter.Length;
            var name = Path.GetFileName(this.path);
            if (name.Length < length) {
                return false;
            }
            var start = name.Substring(0, length);
            return start.Equals(Main.config.ignored_item_delimiter);
        }
        public abstract string GetCompiledPath();
        public abstract string GetName();
    }
    internal class SyCategory : Sy {
        public Sy[] children { get; }
        private SyCategoryConfig? config { get; }
        public SyFile? pageOnClick { get; set; } = null;

        public SyCategory(string path, Sy[] children, SyCategoryConfig? config = null, SyFile? pageOnClick = null) : base(path) {
            this.config = config;
            this.children = children;
            this.pageOnClick = pageOnClick;
            children.OrderBy(o => o.path);
        }
        internal class SyCategoryConfig {
            public string? page_on_click { get; set; } = null;
        }
       
        public override string GetCompiledPath() {
            if (config?.page_on_click != null && config?.page_on_click != "") {
                var newPath = Path.GetFullPath(Path.Combine(this.path, config?.page_on_click ?? ""));
                Console.WriteLine(newPath);
                newPath = Path.Combine(Main.config.compiled_site, Path.GetRelativePath(Main.config.site_map, newPath));
                newPath = Path.GetFullPath(newPath);
                return Path.ChangeExtension(newPath, "html");
            }
            return "";
        }

        public override string GetName() {
            var name = Path.GetFileName(this.path);
            name = Path.GetFileNameWithoutExtension(name);
            var omitTitleDelimiterRegex = new Regex($@"^{Main.config.omit_title_delimiter}.*?{Main.config.omit_title_delimiter}");
            name = omitTitleDelimiterRegex.Replace(name, "");
            name.Replace(Main.config.hidden_item_delimiter, "");
            return name;
        }
    }

    internal class SyFile : Sy {
        string text { get; }
        public Type type { get; }
        public SyFile(string path, string text, Type type) : base(path) {
            this.text = text;
            this.type = type;
        }
        internal enum Type {
            Markdown,
            Syl,
            HTML,
            Text
        }

        internal string AsHtml() {
            if (type.Equals(Type.Syl)) {
                return CreateSyComponentTree(text, true).AsHtml();
            } else if (type.Equals(Type.HTML)) {
                return text;
            } else if (type.Equals(Type.Markdown)) {
                var outOptions = new HtmlOut { };

                var options = new Pandoc.Options().DefaultsFile;

                return PandocInstance.ConvertToText(
                    text,
                    new PandocMdIn(),
                    outOptions
                ).GetAwaiter().GetResult();
            }
            return text;
        }

        private static SyComponent CreateSyComponentTree(string text, bool mergeWhiteSpace = false) {
            var childrenMergeWhitespace = mergeWhiteSpace;
            // Look for blocks
            var str = text;
            str = str.Replace("\r\n", "\n");
            // Code blocks
            var codeBlockRegex = new Regex("`{3}(\\w+)?\\n([\\s\\S]+?)\\n`{3}", RegexOptions.Compiled);
            if (codeBlockRegex.Match(str).Success) {
                string[] parts = codeBlockRegex.Split(str, 2);
                string match = codeBlockRegex.Match(str).Groups[2].Value;
                return new SyComponent([CreateSyComponentTree(parts[0], childrenMergeWhitespace), new CodeBlockComponent([new TextComponent(match)]), CreateSyComponentTree(parts[^1], childrenMergeWhitespace)]); // The second merge whitespace is true, because we haven't checked the end of the string yet.
            }
            // Lists
            // Unnumbered lists
            var unnumberedListBlockRegex = new Regex("^(?:[ \\t]*[-*+] .*(?:\r?\n|$))+", RegexOptions.Compiled | RegexOptions.Multiline);
            if (unnumberedListBlockRegex.Match(str).Success) {
                string[] parts = unnumberedListBlockRegex.Split(str, 2);
                string match = unnumberedListBlockRegex.Match(str).Value;

                List<SyComponent> list = new();
                var listItemRegex = new Regex("^[ \\t]*[-*+] +(.*)", RegexOptions.Compiled | RegexOptions.Multiline);
                foreach (var line in listItemRegex.Matches(match).ToArray()) {
                    list.Add(CreateSyComponentTree((line?.Groups[1].Value) ?? ""));
                }

                return new SyComponent([CreateSyComponentTree(parts[0], childrenMergeWhitespace), new ListComponent(list.ToArray()), CreateSyComponentTree(parts[^1], childrenMergeWhitespace)]);
            }

            if (mergeWhiteSpace) {
                // Merge new lines
                var newLineRegex = new Regex(@"(?<!\r?\n)\r?\n(?!\r?\n)", RegexOptions.Compiled);
                str = newLineRegex.Replace(str, " ");
               

                var multiNewLineRegex = new Regex("\n{2,}", RegexOptions.Compiled | RegexOptions.Multiline);
                str = multiNewLineRegex.Replace(str, "\n");

                // Merge double spaces
                var multiSpaceRegex = new Regex(" {2,}", RegexOptions.Compiled | RegexOptions.Multiline);
                str = multiSpaceRegex.Replace(str, " ");
            }



            var headingRegex = new Regex("^(#{1,6})\\s+(.+?)$", RegexOptions.Compiled | RegexOptions.Multiline);
            if (headingRegex.Match(str).Success) {
                string[] parts = headingRegex.Split(str, 2);
                var match = headingRegex.Match(str).Groups;
                return new SyComponent([CreateSyComponentTree(parts[0], childrenMergeWhitespace), new HeadingComponent([CreateSyComponentTree(parts[2])], match[1].Length), CreateSyComponentTree(parts[^1], childrenMergeWhitespace)]);
            }

            var quoteRegex = new Regex("^>\\s+(.*)$", RegexOptions.Compiled | RegexOptions.Multiline);
            if (quoteRegex.Match(str).Success) {
                string[] parts = quoteRegex.Split(str, 2);
                return new SyComponent([CreateSyComponentTree(parts[0], childrenMergeWhitespace), new QuoteComponent([CreateSyComponentTree(parts[1])]), CreateSyComponentTree(parts[^1], childrenMergeWhitespace)]);
            }
                
            var paragraphRegex = new Regex(@"[^\r\n]+(?:\r?\n(?!)[^\r\n]+)*", RegexOptions.Compiled | RegexOptions.Multiline);
            if (paragraphRegex.Match(str).Success && mergeWhiteSpace) {
                string[] parts = paragraphRegex.Split(str, 2);
                string match = paragraphRegex.Match(str).Value.Trim('\n');
                return new SyComponent([CreateSyComponentTree(parts[0], childrenMergeWhitespace), new ParagraphComponent([CreateSyComponentTree(match)]), CreateSyComponentTree(parts[^1], childrenMergeWhitespace)]);
            }

            childrenMergeWhitespace = false;

            // Look for inline level
            var boldRegex = new Regex("(\\*\\*)(.+?)\\1", RegexOptions.Compiled | RegexOptions.Multiline);
            if (boldRegex.Match(str).Success) {
                string[] parts = boldRegex.Split(str, 2);
                return new SyComponent([CreateSyComponentTree(parts[0]), new BoldComponent([CreateSyComponentTree(parts[2])]), CreateSyComponentTree(parts[^1])]);
            }

            var underlineRegex = new Regex("(__)(.+?)\\1", RegexOptions.Compiled | RegexOptions.Multiline);
            if (underlineRegex.Match(str).Success) {
                string[] parts = underlineRegex.Split(str, 2);
                return new SyComponent([CreateSyComponentTree(parts[0]), new UnderlineComponent([CreateSyComponentTree(parts[2])]), CreateSyComponentTree(parts[^1])]);
            }

            var italicizeRegex = new Regex("(\\*|_)(.+?)\\1", RegexOptions.Compiled | RegexOptions.Multiline);
            if (italicizeRegex.Match(str).Success) {
                string[] parts = italicizeRegex.Split(str, 2);
                return new SyComponent([CreateSyComponentTree(parts[0]), new ItaliciseComponent([CreateSyComponentTree(parts[2])]), CreateSyComponentTree(parts[^1])]);
            }

            var strikethroughRegex = new Regex("(~~)(.+?)\\1", RegexOptions.Compiled | RegexOptions.Multiline);
            if (strikethroughRegex.Match(str).Success) {
                string[] parts = strikethroughRegex.Split(str, 2);
                return new SyComponent([CreateSyComponentTree(parts[0]), new ItaliciseComponent([CreateSyComponentTree(parts[2])]), CreateSyComponentTree(parts[^1])]);
            }

            var inlineCodeRegex = new Regex("(\\`\\`)(.+?)\\1", RegexOptions.Compiled | RegexOptions.Multiline);
            if (inlineCodeRegex.Match(str).Success) {
                string[] parts = inlineCodeRegex.Split(str, 2);
                return new SyComponent([CreateSyComponentTree(parts[0]), new InlineCodeComponent([CreateSyComponentTree(parts[2])]), CreateSyComponentTree(parts[^1])]);
            }

            return new TextComponent(text);
        }

        public override string GetCompiledPath() {
            var newPath = Path.Combine(Main.config.compiled_site, Path.GetRelativePath(Main.config.site_map, this.path));
            newPath = Path.GetFullPath(newPath);
            return Path.ChangeExtension(newPath, "html");
        }

        public override string GetName() {
            var name = Path.GetFileName(this.path);
            name = Path.GetFileNameWithoutExtension(name);
            var omitTitleDelimiterRegex = new Regex($@"^{Main.config.omit_title_delimiter}.*?{Main.config.omit_title_delimiter}");
            name = omitTitleDelimiterRegex.Replace(name, "");
            name.Replace(Main.config.hidden_item_delimiter, "");
            return name;
        }
    }

    internal class SyComponent {
        public virtual SyComponent[] children { get; set; }
        public virtual string AsHtml() {
            string[] childrenText = new string[children.Length];
            for (int i = 0; i < childrenText.Length; i++) {
                childrenText[i] = children[i].AsHtml();
            }

            return string.Join("", childrenText);
        }
        public SyComponent(SyComponent[] children = null) {
            this.children = children;
        }
    }

    internal class TextComponent : SyComponent {
        string value;
        public override string AsHtml() {
            return value;
        }
        public TextComponent(string value) : base() {
            this.value = value;
        }
    }

    internal class ParagraphComponent : SyComponent {
        public override string AsHtml() {
            var html = base.AsHtml();
            if (html.Length == 0 || html.IsWhiteSpace()) {
                return "";
            }
            return $"<p>\n{html}\n</p>\n";
        }
        public ParagraphComponent(SyComponent[] children) : base(children) {
            this.children = children;
        }
    }

    internal class HeadingComponent : SyComponent {
        int count;
        public override string AsHtml() {
            return $"<h{count}>{base.AsHtml()}</h{count}>\n";
        }
        public HeadingComponent(SyComponent[] children, int count) : base(children) {
            this.count = count;
        }
    }

    internal class QuoteComponent : SyComponent {
        public override string AsHtml() {
            return $"<blockquote>{base.AsHtml()}</blockquote>\n";
        }
        public QuoteComponent(SyComponent[] children) : base(children) {
        }
    }

    internal class CodeBlockComponent : SyComponent {
        public override string AsHtml() {
            return $"<pre><code>\n{base.AsHtml()}\n</pre></code>\n";
        }
        public CodeBlockComponent(SyComponent[] children) : base(children) {
            this.children = children;
        }
    }

    internal class ListComponent : SyComponent {
         public override string AsHtml() {
            string[] childrenText = new string[children.Length];
            for (int i = 0; i < childrenText.Length; i++) {
                childrenText[i] = $"<li>{children[i].AsHtml()}</li>\n";
            }

            return $"<ol>\n{string.Join("", childrenText)}\n</ol>\n";
        }
        public ListComponent(SyComponent[] children) : base(children) {
            this.children = children;
        }
    }

    internal class BoldComponent : SyComponent {
        public override string AsHtml() {
            return $"<strong>{base.AsHtml()}</strong>";
        }
        public BoldComponent(SyComponent[] children) : base(children) {
            this.children = children;
        }
    }

    internal class UnderlineComponent : SyComponent {
        public override string AsHtml() {
            return $"<u>{base.AsHtml()}</u>";
        }
        public UnderlineComponent(SyComponent[] children) : base(children) {
            this.children = children;
        }
    }

    internal class ItaliciseComponent : SyComponent {
        public override string AsHtml() {
            return $"<em>{base.AsHtml()}</em>";
        }
        public ItaliciseComponent(SyComponent[] children) : base(children) {
            this.children = children;
        }
    }

    internal class StrikethroughComponent : SyComponent {
        public override string AsHtml() {
            return $"<del>{base.AsHtml()}</del>";
        }
        public StrikethroughComponent(SyComponent[] children) : base(children) {
            this.children = children;
        }
    }

    internal class InlineCodeComponent : SyComponent {
        public override string AsHtml() {
            return $"<code>{base.AsHtml()}</code>";
        }
        public InlineCodeComponent(SyComponent[] children) : base(children) {
            this.children = children;
        }
    }

}

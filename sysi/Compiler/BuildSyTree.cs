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
    internal class BuildSyTree {
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
                else {
                    continue;
                }
                var filePath = Path.Combine(path, file);
                var text = File.ReadAllText(filePath);
                tree.Add(new SyFile(filePath, text, type));
            }

            var folders = Directory.GetDirectories(path);
            foreach (var folder in folders) {
                var filePath = Path.Combine(path, folder);
                tree.Add(BuildSyFileTree(filePath));
            }
            var config = new SyCategory.SyCategoryConfig();
            if (File.Exists(Path.Combine(path, ".about"))) {
                config = JsonSerializer.Deserialize<SyCategoryConfig>(File.ReadAllText(Path.Combine(path, ".about")));
            }

            return new SyCategory(path, tree.ToArray(), config);
        }
    }


    internal class Sy {
        public string path { get; }
        public Sy(string path) {
            this.path = path;
        }
    }

    internal class SyFile : Sy {
        public string text { get; }
        public Type type { get; }
        public SyFile(string path, string text, Type type) : base(path) {
            this.text = text;
            this.type = type;
        }
        internal enum Type {
            Markdown,
            Syl,
            HTML
        }

        internal string AsHtml() {
            return CreateSyComponentTree(text, true).AsHtml();
        }

        private static SyComponent CreateSyComponentTree(string text, bool mergeWhitespace = false) {
            // Look for blocks
            var str = text;
            str = str.Replace("\r\n", "\n");
            // Code blocks
            var codeBlockRegex = new Regex("`{3}(\\w+)?\\n([\\s\\S]+?)\\n`{3}", RegexOptions.Compiled);
            if (codeBlockRegex.Match(str).Success) {
                string[] parts = codeBlockRegex.Split(str, 2);
                string match = codeBlockRegex.Match(str).Groups[2].Value;
                return new SyComponent([CreateSyComponentTree(parts[0]), new CodeBlockComponent([new TextComponent(match)]), CreateSyComponentTree(parts[^1], true)]); // The second merge whitespace is true, because we haven't checked the end of the string yet.
            }
            // Lists


            
            if (mergeWhitespace) {
                // Merge new lines
                var newLineRegex = new Regex("(?<!\n)\n(?!\n)", RegexOptions.Compiled);
                str = newLineRegex.Replace(str, " ");

                var multiNewLineRegex = new Regex("\n{2,}", RegexOptions.Compiled);
                str = multiNewLineRegex.Replace(str, "\n");

                // Merge double spaces
                var multiSpaceRegex = new Regex(" {2,}", RegexOptions.Compiled);
                str = multiSpaceRegex.Replace(str, " ");
            }

            // Look for inline level
            var headingRegex = new Regex("^(#{1,6})\\s+(.+)$", RegexOptions.Compiled);
            if (headingRegex.Match(str).Success) {
                string[] parts = headingRegex.Split(str, 2);
                var match = headingRegex.Match(str).Groups;
                return new SyComponent([CreateSyComponentTree(parts[0]), new HeadingComponent([CreateSyComponentTree(parts[2])], match[1].Length), CreateSyComponentTree(parts[^1])]);
            }

            var quoteRegex = new Regex("^>\\s?(.+)$", RegexOptions.Compiled);
            if (quoteRegex.Match(str).Success) {
                string[] parts = quoteRegex.Split(str, 2);
                return new SyComponent([CreateSyComponentTree(parts[0]), new QuoteComponent([CreateSyComponentTree(parts[1])]), CreateSyComponentTree(parts[^1])]);
            }


            var boldRegex = new Regex("(\\*\\*)(.*?)\\1", RegexOptions.Compiled);
            if (boldRegex.Match(str).Success) {
                string[] parts = boldRegex.Split(str, 2);
                return new SyComponent([CreateSyComponentTree(parts[0]), new BoldComponent([CreateSyComponentTree(parts[2])]), CreateSyComponentTree(parts[^1])]);
            }

            var underlineRegex = new Regex("(__)(.*?)\\1", RegexOptions.Compiled);
            if (underlineRegex.Match(str).Success) {
                string[] parts = underlineRegex.Split(str, 2);
                return new SyComponent([CreateSyComponentTree(parts[0]), new UnderlineComponent([CreateSyComponentTree(parts[2])]), CreateSyComponentTree(parts[^1])]);
            }

            var italicizeRegex = new Regex("(\\*|_)(.*?)\\1", RegexOptions.Compiled);
            if (italicizeRegex.Match(str).Success) {
                string[] parts = italicizeRegex.Split(str, 2);
                return new SyComponent([CreateSyComponentTree(parts[0]), new ItaliciseComponent([CreateSyComponentTree(parts[2])]), CreateSyComponentTree(parts[^1])]);
            }

            var strikethroughRegex = new Regex("(~~)(.*?)\\1", RegexOptions.Compiled);
            if (strikethroughRegex.Match(str).Success) {
                string[] parts = strikethroughRegex.Split(str, 2);
                return new SyComponent([CreateSyComponentTree(parts[0]), new ItaliciseComponent([CreateSyComponentTree(parts[2])]), CreateSyComponentTree(parts[^1])]);
            }

            var inlineCodeRegex = new Regex("(\\`\\`)(.*?)\\1", RegexOptions.Compiled);
            if (inlineCodeRegex.Match(str).Success) {
                string[] parts = inlineCodeRegex.Split(str, 2);
                return new SyComponent([CreateSyComponentTree(parts[0]), new InlineCodeComponent([CreateSyComponentTree(parts[2])]), CreateSyComponentTree(parts[^1])]);
            }

            return new TextComponent(text);
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

    internal class HeadingComponent : SyComponent {
        int count;
        public override string AsHtml() {
            return $"<h{count}>{base.AsHtml()}<\\h{count}>";
        }
        public HeadingComponent(SyComponent[] children, int count) : base(children) {
            this.count = count;
        }
    }

    internal class QuoteComponent : SyComponent {
        public override string AsHtml() {
            return $"<blockquote>{base.AsHtml()}<\\blockquote>";
        }
        public QuoteComponent(SyComponent[] children) : base(children) {
        }
    }

    internal class CodeBlockComponent : SyComponent {
        public override string AsHtml() {
            return $"<pre><code>\n{base.AsHtml()}\n<\\pre><\\code>";
        }
        public CodeBlockComponent(SyComponent[] children) : base(children) {
            this.children = children;
        }
    }

    internal class BoldComponent : SyComponent {
        public override string AsHtml() {
            return $"<strong>{base.AsHtml()}<\\strong>";
        }
        public BoldComponent(SyComponent[] children) : base(children) {
            this.children = children;
        }
    }

    internal class UnderlineComponent : SyComponent {
        public override string AsHtml() {
            return $"<u>{base.AsHtml()}<\\u>";
        }
        public UnderlineComponent(SyComponent[] children) : base(children) {
            this.children = children;
        }
    }

    internal class ItaliciseComponent : SyComponent {
        public override string AsHtml() {
            return $"<em>{base.AsHtml()}<\\em>";
        }
        public ItaliciseComponent(SyComponent[] children) : base(children) {
            this.children = children;
        }
    }

    internal class StrikethroughComponent : SyComponent {
        public override string AsHtml() {
            return $"<del>{base.AsHtml()}<\\del>";
        }
        public StrikethroughComponent(SyComponent[] children) : base(children) {
            this.children = children;
        }
    }

    internal class InlineCodeComponent : SyComponent {
        public override string AsHtml() {
            return $"<code>{base.AsHtml()}<\\code>";
        }
        public InlineCodeComponent(SyComponent[] children) : base(children) {
            this.children = children;
        }
    }

    internal class SyCategory : Sy {
        Sy[] children { get; }
        SyCategoryConfig config { get; }

        public SyCategory(string path, Sy[] children) : base(path) {
            this.config = new SyCategoryConfig();
            this.children = children;
        }

        public SyCategory(string path, Sy[] children, SyCategoryConfig config) : base(path) {
            this.config = config;
            this.children = children;
            children.OrderBy(o => o.path);
        }
        internal class SyCategoryConfig {
            public string? page_on_click { get; set; } = null;
        }
    } 
}

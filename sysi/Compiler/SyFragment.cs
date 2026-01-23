using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace sysi.Compiler {
    internal class SyFragment {
        public static Dictionary<string, SyFragment> fragmentMap = new();

        public string name { get; } = "";
        public string fragment { get; } = "";
        public SyFragment(string name, string fragment) {
            this.name = name;
            this.fragment = fragment;

            fragmentMap.Add(name, this);
        }

        public static void LoadFragments() {
            var fragmentRegex = new Regex(@"<!--\s*\$(\w+)\s*-->\s+([\s\S]*?)<!--\s*\$\1\s*-->",  RegexOptions.Compiled);
            var file = File.ReadAllText(Path.Combine(Main.config.template_folder, Main.config.fragments_file));

            foreach (var match in fragmentRegex.Matches(file).ToArray()) {
                var name = match.Groups[1];
                var fragment = match.Groups[2];

                if (name == null || fragment == null) {
                    continue;
                }
                new SyFragment(name.Value, fragment.Value);
            }

        }

    }
}

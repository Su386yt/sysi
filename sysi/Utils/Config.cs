using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace sysi.Utils {

    internal class Config {
        private const string CONFIG_FOLDER = "sysi/site/";
        private const string CONFIG_PATH = CONFIG_FOLDER + ".config";

        public string hidden_item_delimiter { get; set; } = "_";
        public string ignored_item_delimiter { get; set; } = "_.";
        public string omit_title_delimiter { get; set; } = "-";
        public string site_map { get; set; } = "sysi/site";
        public string compiled_site { get; set; } = "sysi/compiled";
        public string home_page { get; set; } = "_home";
        public string template_folder { get; set; } = "sysi/templates";
        public string default_template { get; set; } = "default.html";
        public string fragments_file { get; set; } = "fragments.html";
        public GitHubConfig github { get; set; } = new GitHubConfig();

        public static Config LoadConfig() {
            Console.WriteLine(Path.GetFullPath(CONFIG_PATH));

            Config? config = null;
            if (File.Exists(CONFIG_PATH)) {
                config = JsonSerializer.Deserialize<Config>(File.ReadAllText(CONFIG_PATH));
            }
            else {
                Directory.CreateDirectory(CONFIG_FOLDER);
                File.Create(CONFIG_PATH).Close();
            }
            if (config == null) {
                config = new Config();
            }
            File.WriteAllText(CONFIG_PATH, JsonSerializer.Serialize<Config>(config, new JsonSerializerOptions { WriteIndented = true }));
            return config;
        }
        internal class GitHubConfig {
            public bool sync_projects { get; set; } = true;
            public string project_folder { get; set; } = "Projects";
            public string username { get; set; } = "";
            public string documentation_folder { get; set; } = "docs";
            public string[] featured_projects { get; set; } = [];
        }
    }

    
}

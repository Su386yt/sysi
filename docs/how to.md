# How to use

## Config

Default config file:
```json
{
  "hidden_item_delimiter": "_",
  "ignored_item_delimiter": "_.",
  "omit_title_delimiter": "-",
  "site_map": "sysi/site",
  "compiled_site": "sysi/compiled",
  "home_page": "_home",
  "template_folder": "sysi/templates",
  "default_template": "default.html",
  "fragments_file": "fragments.html",
  "github": {
    "sync_projects": true,
    "project_folder": "Projects",
    "username": "",
    "documentation_folder": "docs",
    "featured_projects": []
  }
}
```

``hidden_item_delimiter`` - Hides webpages from the sitemap (see [Hidden Items](#hidden-items)).

``ignored_item_delimiter`` - Ignores compilation of these items.

``omit_title_delimiter`` - Omits this portion of the title in declaring the page name.

``compiled_site`` - Folder in which to output compiled site

``site_map`` - Path where site map is located.

``home_page`` - Page to default.

``template_folder`` - Path where template is located (see [Templates](#templates)).

``default_template`` - Name of template used by default unless otherwise specified.

``fragments_file`` - Fragments file in the templates folder (see [Fragments](#fragments)). 

``github.sync_projects`` - Whether to sync github projects.

``project_folder`` - Folder in which to add GitHub projects

``username`` - GitHub projects

``documentation_folder`` - Folder in which to list GitHub projects.

``features_projects`` - List of names of projects to display on the dropdown for the folder. Others will be displayed under a page linked in "See more projects".

## Templates

### Fragments
Fragments are inserted at compile time and are delimited ``<!-- !fragment_name -->``. There are three builtin fragments (``<!-- !body_text -->``, ``<!-- !navitems -->``, and ``<!-- !title -->``) that are reserved.

Fragments should be declared in the file referred to in ``fragments_file`` in the config file. Each declaration fragment in the file is delimited by ``<!-- $fragment_name -->`` before and after the declaration of the fragment. Each fragment declaration must contain all CSS needed unless certain the CSS needed will be packaged some other way.

Example ``<!-- !navbar -->`` declaration:

```html
<!-- $navbar -->
<nav class="navbar">
    <h1> Site Title <\h1>

    <ul class="nav-links">
        <!-- !navitems -->
    </ul>
</nav>
<!-- $navbar -->
```

#### ``<!-- !title -->``
Inside the ``<head>`` div for page title information.

#### ``<!-- !body_text -->``
Inserts the compiled markdown or SyFile into the template.

#### ``<!-- !navitems -->``
Inserts the page title and a link in the form 
```html
<li> <a href = "page_link"> Page Name <\a> <\li>
```

## Structure

### Hidden items

## Special features

### GitHub integration

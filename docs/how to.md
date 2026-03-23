# How to use
Sysi supports four different types of files to generate a static site. **SyFiles** (``.syl``) are similar to Markdown, while adding support for LaTeX and (in the future) runnable code. These are compiled to HTML with the SyCompiler, while **markdown** files (``.md``) are converted to HTML via Pandoc and inserted into the ``<!-- !body -->`` fragment (see [Fragments](#fragments)). Standard **HTML** (``.html``) and their contents will be automatically inserted as a ``<!-- !body -->`` fragment into the template of choice, with no modification. Standard **text files** (``.txt``) may also be used, and these will be inserted into the compiled site with no modification (this allows writing of HTML with no modification). 

## Dependancies
- [Pandoc](https://pandoc.org/installing.html)
 - pandoc.exe is expected to be accessible in the current environment's Path.

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

Templates are needed to insert the body text (Markdown files and SyFiles) into the HTML files. Templates must package all necessary components (such as CSS), as external references are not (yet) supported. 

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

#### Builtin Fragments
##### ``<!-- !title -->``
Inside the ``<head>`` div for page title information.

##### ``<!-- !body_text -->``
Inserts the compiled markdown or SyFile into the template.

##### ``<!-- !navitems -->``
Inserts the page title and a link in the form 
```html
<li> <a href = "page_link"> Page Name <\a> <\li>
```

## Structure

To structure a website, the directory layout of the sitemap is used. The top-level directory will be displayed on the navigation bar, unless the hidden item delimiter is used to prefix the file. If an item in the top-level directory is another directory (a subdirectory), the item on the navigation bar will be hoverable, and a drop down menu with it's contents are used. Any further folder will link to list pages by default. 

A folder can have a default page when clicked on, by specifying a file titled ``_default`` (where ``_`` is replaced by the hidden item delimiter).

### Hidden items

Hidden items are omitted during compile time, but may still be used as reference by other files in the site.

## Special features

### GitHub integration
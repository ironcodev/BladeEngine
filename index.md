## Background

Template engines are a great tool for dynamic content generation, from creating mail messages, source code files, project scafolding to even generating complete projects.

Many different template engines can be found in the internet. Each engine has its own specific tag and syntax through which users can write templates (usually stored on disk). Next, the engine is compiles or renders the template and returns the generated result devoid of the tags that were embeded in the source template.

Some template engines like [Liquid](https://shopify.github.io/liquid/) are fairly simple. `Liquid` was first created by [shopify](https://www.shopify.com/) to ease creating web pages and websites. Then, it was made open source and developers implemented it in different programming languages.

Whereas template engines like [Liquid](https://shopify.github.io/liquid/) are abstract in their syntax and features -i.e. their syntax is independent of a specific programming language-, other template engines target a dedicated programming language.

For example [EJS](https://ejs.co/) is designed to be used in `nodejs` or [Razor](https://github.com/Antaris/RazorEngine) is a template engine for `.NET` with `C#/VB` syntax.

This second group of template engines provide wider and better templating features compared to basic features like model-binding that all engines have in common. The features list includes being able to use control structures such as `if-else`, `foreach`, `while`, calling arbitrary functions, etc.

With the ease of code generation that template engines provide, IDEs will also be more powerful, as they can lift a pain from developers by eliminating the need of creating repetetive files, writing redundant codes or copy-paste and manually modifing them each time.

## About
Blade is a template engine with an abstract or language-dependent tagging. It provides a few engine plugins each one target a specific programming language. This way the user is able to write his/her templates in any programming language he is nimble in, `Javascript`, `Java`, `C#`, `VB`, `Python`, etc.

```markdown
Syntax highlighted code block

# Header 1
## Header 2
### Header 3

- Bulleted
- List

1. Numbered
2. List

**Bold** and _Italic_ and `Code` text

[Link](url) and ![Image](src)
```

For more details see [GitHub Flavored Markdown](https://guides.github.com/features/mastering-markdown/).

### Jekyll Themes

Your Pages site will use the layout and styles from the Jekyll theme you have selected in your [repository settings](https://github.com/ironcodev/BladeEngine/settings/pages). The name of this theme is saved in the Jekyll `_config.yml` configuration file.

### Support or Contact

Having trouble with Pages? Check out our [documentation](https://docs.github.com/categories/github-pages-basics/) or [contact support](https://support.github.com/contact) and we’ll help you sort it out.

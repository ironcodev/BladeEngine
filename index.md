## Background

Template engines are a great tool for dynamic content generation, from creating mail messages, source code files, scafolding to even generating complete projects.

Many different template engines can be found in the internet. Each engine has its own specific tag and syntax through which users can write templates (usually stored on disk). Next, the engine compiles the template and returns the generated result devoid of the tags that had been embeded in the source template.

Some template engines like [Liquid](https://shopify.github.io/liquid/) are fairly simple. `Liquid` was first created by [shopify](https://www.shopify.com/) to ease creating web pages and websites. Then, it was made open source and developers implemented it in different programming languages.

Whereas template engines like [Liquid](https://shopify.github.io/liquid/) are abstract regarding their syntax and features -i.e. their syntax is independent of a specific programming language-, other template engines target a programming language.

For example [EJS](https://ejs.co/) is designed to be used in `nodejs` or [Razor](https://github.com/Antaris/RazorEngine) is a template engine for `.NET` with `C#/VB` syntax.

This second group of template engines provide wider and better templating features compared to basic features like model-binding that all engines have in common. The features list includes being able to use control structures such as `if-else`, `foreach`, `while`, calling arbitrary functions, etc.

With the ease of code generation that template engines provide, IDEs will also be more powerful, as they can lift a pain from developers by eliminating the need of creating repetetive files, writing redundant codes or copy-paste and manually modifing them each time.

## About
Blade is a template engine with an abstract or language-independent code embeding. For each programming language, a specific plugin is used that understands how to compile templates written for that language. So, the user is able to write templates in a programming language he is nimble in. Currently, `Blade` supports `Javascript`, `Java`, `C#`, `VB` and `Python`. It is possible to add other languages to the engine.

## Command Line Parameters
Here is list of CLI parameters.
```
blade [runner] [-e engine] [-c engine-config] [-i input-template] [-o output] [-on]
      [-r runner-output] [-rn] [pr] [-m model] [-ch cache-path] [-debug] [-v] [-?]

options:
    runner  :   run template
    -e      :   specify blade engine to parse the template. internal engines are:
                    csharp, java, vb, javascript, python
    -i      :   specify input balde template
    -o      :   specify filename to save generated content into. if no filename is specified, use automatic filename
    -on     :   do not overwrite output if already exists
    -r      :   in case of using 'runner', a filename to save the result of executing generated code
    -rn     :   do not overwrite runner output if already exists
    -pr     :   print runner output
    -c      :   engine config in json format or a filename that contains engine config in json format
    -ch     :   path of a cache dir where runners store their compilation stuff in it (defaul is %AppData%\blade\cache)
    -m      :   model in json format or a filename containing model in json format
    -debug  :   execute runner in debug mode
    -v      :   program version
    -?      :   show this help

example:
    blade -e csharp -i my-template.blade
    blade -e csharp -i my-template.blade -o my-template.cs
    blade -e csharp -i my-template.blade -c "{ 'UseStrongModel': true, 'StrongModelType': 'Dictionary<string, object>' }
"
    blade runner -e csharp -i my-template.blade -m "{ 'name': 'John Doe' }"
```

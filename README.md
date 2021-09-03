# Hecate
Text generation using grammars, originally created by [Aarne Uotila](https://github.com/Aarneus/blackhearts)

Sample Grammar syntax:
```
# Random furniture
furniture L
=> "the [=>floor]"
=> "a windowsill", plot.inside != null
=> "a desk", L.office != null
=> "a filing cabinet", L.office != null
```

Sample output:

> Chapter 1
> 
> The Blackhearts Detective Agency. There's no place like home. I feel a pang of hunger in my stomach. I'd fill my fridge if I could. Problem is, a detective's pay is not exactly luxurious. I need a job. And I need it fast. I'm throwing darts at the lord barrister's picture when the phone rings. It's Nellie. She is in a hurry. I furrow my brow. It doesn't seem good. She will owe me after this. I get ready to leave for the power plant.

# Notes about the Grammar language
## Syntax
The most basic rule is this simple one:
```
color => "red"
```

This is just a basic rule like those found in all context-free grammars. If you want to invoke other rules (i.e. nonterminal symbols) you do it like this:
```
key => "The [=>color] key."
```
...which will print `The red key.`

If you want to employ variables, the statements go after the text:
```
next_room => "kitchen", current_room == "foyer"
```

You can use flags to allow a similar rule to only apply once (e.g. so no two rules describing weather are used)
```
=> "It was raining.", flag weather
```

Rules can have parameters, where you can pass a variable to a rule:
```
name E => "she", previous_name == E.name
name E => "E.name", previous_name != E.name, previous_name = E.name
```

You can use the capitalization operator to capitalize rules:
```
intro => "this is fine"
story => "[^=>intro]"
```
...which will render `This is fine` when starting on rule `story`.

## State model
The variables are divided into local and global variables. Local variables are those that start with a capital letter.

Variables are all rootnodes and can have subvariables that are accessed by dot notation:
```
dog.owner.opinion.(dog.name)
```

Variables can be set by either modifying their value or by replacing them with another variable:
```
let dog.name = "Spot"
merchant.pet <- dog
```

In the previous example, merchant.pet.name would now be "Spot". If the variable or subvariable does not exist, it is equal to null:
```
cat.wings == null
```

Replacing a variable with null will delete that subvariable (and all of its subvariables, etc):
```
cat.wings <- null
```

## Rule selection
* The statements after the text on a rule are sorted into two categories; conditions and effects.
* Conditions are any statement that has one of the conditional operators (`==`, `!=`, `<`, `>`, `>=`, `<=`) and determine if the rule is possible at the given time.
* Effects are what happens after the rule has been selected.
* If multiple rules are available for a non-terminal, the applied rule is selected randomly, with each rule weighted by the number of conditions it has, so the rule with the most conditions has the highest chance of being chosen.
* This heuristic has been appropriated from Evans's presentation on Valve's dialogue system.
* If no rule applies, an empty string ("") is used instead.

# API

First you need a `StoryGenerator`:
```c#
var generator = new StoryGenerator();
```

With it, you can add a directory that contains your rules, a collection of `*.hec` files.
```c#
generator.ParseRuleDirectory("Game/MyRules");
```

You can add an initial state to your generator, to be able to control content from code:
```c#
var dict = new Dictionary<string, object> {
    {"megaflops", 42},
    {"myRecord, new Dictionary<string, object> {
        {"state", "good"}
    }
};
generator.SetBaseState(dict);
```

You can then use this from your data files like you'd use any other variable:
```
story => "The megaflops were up to [megaflops], state was [myRecord.state]!"
```

Finally, modifiers can be added to your generator, so if you have helper functions like:

```c#
public static object isOdd(object numberObj) {
    if (numberObj is int number) {
        return number % 2 ? "yes" : "no";
    }

    return "no";
}
```

You could register the modifier like this:
```c#
generator.RegisterModifier("isOdd", isOdd);
```

...and use it on your grammar:

```
story => "Were the megaflops odd? [^megaflops#isOdd]!"
```

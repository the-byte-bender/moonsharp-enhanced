# Duke's Unity Enhancements to Moonsharp

For my game Dehoarder 2, I was working on adding moddability to the game, and needed to add a scripting language.

Lua was a natural choice due to its fairly extensive use in gaming and ease of implementation.

MoonSharp was a natural choice because it is written in C# and has Unity support built in.

Along the way to integrating Moonsharp in my project, I ran into some bumps and pitfalls that I had to resolve. This fork represents
the culmination of that work.

## Link to Original Project

For main MoonSharp documentation and code, refer to:

https://www.moonsharp.org
https://github.com/moonsharp-devs/moonsharp

Many thanks to Marco Mastropaolo and the rest of the Moonsharp team and contributors for providing a tool that was at least 90%
of the way to what I needed.

## What I changed

My changes mainly focused on performance improvement, both in terms of speed, and in terms of memory usage.

The changes that I made were geared toward my workload, where I intended to run a few dozen scripts per frame. Your results may vary on the specifics,
but overall, adopting these changes should lead to significant performance gains.

### Pruning and Upgrading Repo

My interest is primarily in using MoonSharp with Unity, and all of these enhancements have been made with Unity in mind.
- I especially have no interest in maintaining .Net 3.5, .Net 4.0, or .Net Core 1.0 projects which are well out of support.
  - Critical projects have been upgraded to .Net 5. Why not .Net 6? Because .Net 5 is more aligned with the current state of Unity and supports C# 9.0 as Unity does.
  - PerformanceComparison, SynchProjects have been removed.
- I have no interest in the Flash-based debugger since Flash is long gone and have removed it.
- I am not experienced with Xamarin nor do I have tooling for it so I have removed it.

I've worked minimally with the Visual Studio projects and solutions to get them working under .Net 5.0. The DotNetCoreTestRunner runs and passes all tests.

This effort has eliminated many, but not all of the multiple redundant copies of source code in the repo that were being synchronized with an rsync script.
I have not updated or tested said rsync script. I'd rather focus on getting the repo to a state where there is no duplication of code.

### Saner, Injectable Stack Sizes

One of the first things that anyone that tries to seriously use Moonsharp in Unity realizes is that Script objects allocate a LOT of memory.
It turns out there is a pretty simple cause and fix for that.

In stock MoonSharp, both the execution stack and call stack default to 131,072 8-byte object pointers. This explains nearly 100% of why
Script objects allocate over 2MiB apiece.

I changed the default to something a bit smaller - 4,096 entries for each stack. But you can probably go much smaller depending on your scenario,
so I exposed a Script constructor that allows each stack size to be specified as a parameter. In my case, I was able to default to stacks with
only 16 entries. This is over a 99.98% reduction in memory footprint for each script.

### DynValue Footprint Reduction and Immutability

Ugh, DynValue. You smell like Variant from the bad old Visual Basic days. Aside from my disdain of the necessary evil of a dynamic type,
I found two things about DynValue that I was able to improve greatly:

The heap size of a DynValue was reduced from 112 bytes to 40 bytes. This is a huge win for memory performance, as DynValues are created constantly.

The size reduction was achieved by a combination of removing unnecessary state (including removal of a unique 8-byte ID for each value that was only
used by the debugger), and taking advantage of the ability to arrange data members to
leave as few gaps as possible when the .Net compiler tries to align everything on 8-byte boundaries.

In addition, I found there to be a lot of creation of readonly copies (tens and hundreds of thousands per frame in my project!) of DynValue. If
DynValue could provide a guarantee that its contents were immutable this would eliminate the need for all of this copying. This has been implemented,
and also allows the removal of state necessary to track whether a value is read-only.

In a future version, I might make DynValue a value type.

### AggressiveInline

Turning on AggressiveInline attribute helps for some methods, especially those that are primarily passthrough to lower-level methods, or only perform
a couple of instructions.

### Remove "Slop Matching" on member names

Yes, it can be kind of nice to have the system respond the same whether you call HelloWorld(), hello_world(), helloworld() or Hello_World(). However,
such niceties should not come with a runtime cost, and these nicities definitely do! Also such a "nicety" promotes inconsistency in code style. 
This change alone caused 60 unit tests to need attention due to inconsistent casing. On second thought, this isn't a nicety, it's a nightmare. Good riddance.

### Remove Custom Converters for data types

MoonSharp provides a facility for registering and using custom converters to convert values from C# types to Lua types and vice versa. I did not find
it necessary or useful for my case, and it was actively costing runtime performance, so I removed it.

### Backport ForEach to For

I wish we lived in an age where ForEach was universally as cheap as For. In Unity, still we have an issue where a trivial ForEach on an array or other
simple list type causes a small but additive amount memory allocation.

### Rewrite Require as C#

Some library functions in MoonSharp are implemented in embedded Lua scripts that are translated and executed in a Lua VM at runtime.
"require", a frequently used member, was one of them. This library function has been re-implemented in C# to reduce time spent compiling and executing
bytecode.

### Module Registration and Type Descriptor Caching

Module registration and type descriptor caching has been improved, eliminating repetition of expensive operations whose results do not change.

### Miscellaneous Other Changes

There were a few other minor changes and tweaks, possibly a couple of minor bug fixes that do not stand out in my memory. I've tried to keep reformatting
to a minimum.

### Tests

Tests related to removed features have been removed. Tests are updated and passing both in Unity environment and VS environment.

- Note: In VS environment, tests must be run by running the DotNetCoreTestRunner project directly. Tests will not run from Test Explorer due to how
the DotNetCoreeTestRunner project is currently structured.

## Summary & Conclusion

MoonSharp is really solid out of the box even with 7 years of minimal maintenance, but if you want to use it in Unity, especially for code run during
frame update, there are a few tweaks you are going to want to make. I've tried to package up as many of those tweaks as I could find here.

I hope you find this useful, and I hope (but do not require) that you credit me for this work if you make use of it. If you do wish to credit me, you can
say something along the lines of "MoonSharp performance enhancements courtesy of Duke/Smiling Cat Entertainment".

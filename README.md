# MoonSharp-enhanced

This is a fork of [Smiling Cat Entertainment's MoonSharp-UnityEnhancements](https://github.com/SmilingCatEntertainment/moonsharp-unityenhanced), which itself is a fork of the official [MoonSharp](https://github.com/moonsharp-devs/moonsharp) project. The goal of this project is to create a highly optimized Lua interpreter, specifically for use in scripting for a game server that might have thousands of script instances active.

## Credits

A huge thanks to Duke/Smiling Cat Entertainment for their work on the Unity-enhanced version. They did a ton of the heavy lifting in terms of optimization, and their work has been a massive head-start for this project. Also, credit to Marco Mastropaolo and the rest of the MoonSharp team for the original library.

## What we plan to do and might have done already

This fork is all about squeezing every last drop of performance out of MoonSharp and minimizing its memory footprint. We've removed unity related code, and will focus on the interpreter. The project has been upgraded to dotnet 8, and optimizations will continue, aiming for reducing the memory impact of script instances.

## Support? Maybe.

This fork is specifically driven by the needs of another project, but the results could be useful to others. Support is best effort, which might be a nicer way to say you're on your own. You're strongly advised to use the upstream [MoonSharp](https://github.com/moonsharp-devs/moonsharp) instead.

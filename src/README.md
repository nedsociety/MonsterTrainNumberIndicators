# Here the code is housed

## Prerequisite

Before trying to build the projects, make sure you have:

- The game
- [Mod Loader](https://steamcommunity.com/sharedfiles/filedetails/?id=2187468759) installed
- Visual Studio 2019 or higher w/ C# support

Then, open `SteamAppsDir.props` file at the root of the repository (not this directory) and adjust the path to your own.

A successful build will package the plugin onto `package` directory of the root of this repo. You may copy the "content" directory into the Steam Workshop directory to make it a testable local mod.

## `External` directory

Houses external plugins distributed alongside the plugins.

- [BepInEx.ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager) (prebuilt binary, tag v16.3)

## `Tools` directory

Tools for use in dev process.

- Mono binary for debugging the process with dnSpy.


# Assets Editor V2

Assets Editor is an open-source tool designed for modifying and managing client assets for both Tibia 12+ and Tibia 1098.

![Main interface](/Assets%20Editor/Resources/1.PNG)
![Search Window](/Assets%20Editor/Resources/2.PNG)
![OTB Editor](/Assets%20Editor/Resources/3.PNG)
![Sheet Editor](/Assets%20Editor/Resources/4.PNG)
![Import and Export](/Assets%20Editor/Resources/5.gif)
![Dark Mode](/Assets%20Editor/Resources/6.PNG)
![Lua Graphs](/Assets%20Editor/Resources/7.PNG)
![Lua Scripting](/Assets%20Editor/Resources/8.PNG)

### Features

- **Support for Tibia 12+**
- **Support for Tibia 10.98**
- **Object Modification**
- **Create/Copy/Delete Objects**
- **Import and Export**
- **Sprite Sheet Modifications**
- **New Search Window**
- **OTB Editor for Tibia 10.98**
- **Import Manager for Tibia 10.98**
- **Export to spr/dat**
- **Export to outfit/item images**
- **Large spritesheets**
- **Transparent items in spritesheets**
- **Lua support**

#### Prerequisites

- [.NET 8 Runtime] (release 2.0)
- [.NET 10 Runtime] (main branch)

#### Usage
- Download the latest release from the [Releases](https://github.com/Arch-Mina/Assets-Editor/releases) page.

#### Legacy export profiles

The command line exporter can generate legacy `.dat/.spr` pairs:

```powershell
& ".\Assets Editor.exe" export-legacy --profile cip860-extended --input <assets-or-bin-path> --output <client-path> [--overwrite] [--no-backup]
& ".\Assets Editor.exe" export-legacy --profile client11-15x --input <assets-or-bin-path> --output <client-path> [--overwrite] [--no-backup]
& ".\Assets Editor.exe" validate-legacy --profile client11-15x --dat <client-path>\Tibia.dat [--spr <client-path>\Tibia.spr]
```

The UI legacy exporter uses the same profiles. By default it writes to `legacy-exports\<profile>` under the application folder, and the dialog also has a custom output option when you want to export directly to another directory.

`cip860-extended` is intentionally a CipSoft 8.60 object layout with selected extensions, not a modern OTClient dat layout:

- Writes classic 8.60 dat attributes only.
- Does not write modern dat flags such as `Clothes`/attr 32, `Market`, `DefaultAction`, `Wrap`, or `TopEffect`.
- Writes extended `uint32` sprite ids for clients patched to read the extended `.spr`.
- Keeps the classic outfit layout. Outfit colors come from the game protocol fields `lookHead`, `lookBody`, `lookLegs`, `lookFeet`, and `lookAddons`, not from the modern `Clothes` dat flag.

This matters because the CipSoft 8.60 parser aborts on unknown modern dat attributes. A file that includes `Clothes`/attr 32 is not a valid `cip860-extended` export unless the client binary is separately patched to understand that flag.

`client11-15x` targets the extended client 11 executable used with 15.x assets:

- Writes the 10/11 dat layout with modern legacy flags enabled.
- Uses signature `0x00004A10` for `.dat` and `0x59E48E02` for `.spr`.
- Writes extended `uint32` sprite ids.
- Caps Market names at 29 characters. This client fails to open `Tibia.dat` when a Market name is 30 characters or longer, so the exporter truncates the name before serialization.

The exporter validates the generated `.dat` before writing the `.spr`. The validator checks the selected profile contract, parser alignment, Market name limits, frame group data, animation phase data, and sprite id references. Use `validate-legacy` on shared files before opening them in the CipSoft client; a validator error is safer than a client access violation on startup.

:sparkles: **Supporting the Project**

If you find this project useful and want to show your appreciation or support, you're welcome to do so through [PayPal](https://paypal.me/SpiderOT?country.x=EG&locale.x=en_US). Your support is entirely optional but greatly appreciated :heart:.

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

### Legacy export profiles

The command line exporter can generate legacy `.dat/.spr` pairs:

```powershell
& ".\Assets Editor.exe" export-legacy --profile cip860-extended --input <assets-or-bin-path> --output <client-path> [--overwrite] [--no-backup]
```

`cip860-extended` is intentionally a CipSoft 8.60 object layout with selected extensions, not a modern OTClient dat layout:

- Writes classic 8.60 dat attributes only.
- Does not write modern dat flags such as `Clothes`/attr 32, `Market`, `DefaultAction`, `Wrap`, or `TopEffect`.
- Writes extended `uint32` sprite ids for clients patched to read the extended `.spr`.
- Keeps the classic outfit layout. Outfit colors come from the game protocol fields `lookHead`, `lookBody`, `lookLegs`, `lookFeet`, and `lookAddons`, not from the modern `Clothes` dat flag.

This matters because the CipSoft 8.60 parser aborts on unknown modern dat attributes. A file that includes `Clothes`/attr 32 is not a valid `cip860-extended` export unless the client binary is separately patched to understand that flag.

:sparkles: **Supporting the Project**

If you find this project useful and want to show your appreciation or support, you're welcome to do so through [PayPal](https://paypal.me/SpiderOT?country.x=EG&locale.x=en_US). Your support is entirely optional but greatly appreciated :heart:.

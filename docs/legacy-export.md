# Legacy Export

Assets Editor can generate legacy `.dat/.spr` pairs from modern assets through either the command line or the UI legacy exporter.

## Command Line

Run these commands from the directory that contains `Assets Editor.exe`:

```powershell
& ".\Assets Editor.exe" export-legacy --profile cip860-extended --input <assets-or-bin-path> --output <client-path> [--overwrite] [--no-backup] [--no-item-flag-otml]
& ".\Assets Editor.exe" export-legacy --profile client11-15x --input <assets-or-bin-path> --output <client-path> [--overwrite] [--no-backup] [--no-item-flag-otml]
& ".\Assets Editor.exe" validate-legacy --profile client11-15x --dat <client-path>\Tibia.dat [--spr <client-path>\Tibia.spr]
```

The exporter writes `Tibia.dat`, `Tibia.spr`, and, unless disabled, `itemFlag.otml` to the selected output directory. Existing output files require `--overwrite`; backups are created by default unless `--no-backup` is passed.

## UI Export

The UI legacy exporter uses the same profiles as the CLI. By default it writes to `legacy-exports\<profile>` under the application folder, and the dialog also has a custom output option when you want to export directly to another directory.

## Profiles

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

## Item Flag Sidecar

Legacy export writes an `itemFlag.otml` sidecar next to `Tibia.dat` and `Tibia.spr` by default. This file preserves protobuf item flags that are not represented by the selected legacy `.dat` contract. Pass `--no-item-flag-otml` to skip it.

`itemFlag.otml` is an overlay for compatible OTClient forks. It does not affect clients unless they explicitly load it after the legacy things files, for example with `g_things.loadOtml("things/{version}/itemFlag.otml")`, and extend `ThingType::unserializeOtml()` to apply the supported tags.

Example sidecar:

```otml
items
  3046
    duration: true

  3047
    classification: 1

  3051
    duration: true
    cloth: ring
    decoKit: true

  2222
    proficiency: 2
    restrictVocation
      - knight
      - paladin
```

The sidecar may include tags such as `charges`, `duration`, `classification`, `decoKit`, `proficiency`, `skillWheelGem`, `imbueSlots`, `dualWielding`, `minimumLevel`, `weaponType`, and `restrictVocation`. For `cip860-extended`, it may also carry modern tags intentionally omitted from the 8.60 `.dat`, such as `cloth`, `defaultAction`, `wrap`, `unwrap`, and `topEffect`.

## Validation

The exporter validates the generated `.dat` before writing the `.spr`. The validator checks the selected profile contract, parser alignment, Market name limits, frame group data, animation phase data, and sprite id references. Use `validate-legacy` on shared files before opening them in the CipSoft client; a validator error is safer than a client access violation on startup.

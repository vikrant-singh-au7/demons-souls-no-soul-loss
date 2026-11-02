# Demon's Souls PS5 — No Soul Loss on Death

**Keep your souls after dying in Demon's Souls Remake on KytyPS5.** This small runtime patch retains the souls you were carrying instead of transferring them into a bloodstain. It leaves health, damage, earning souls, and spending souls alone.

## Download

**[Download the Windows installer ZIP from Releases](https://github.com/vikrant-singh-au7/demons-souls-no-soul-loss/releases/tag/v0.1.0)**

Choose **DemonsSouls-KeepSouls-v0.1.zip** under **Assets**. The release ZIP includes `KeepSoulsInstaller.exe`, the manual-install JSON patch, instructions, checksums, and installer source. GitHub's automatic **Source code** archives are not the compiled installer.

## Compatibility

| Item | Supported / tested |
| --- | --- |
| Game | Demon's Souls **PS5 remake** |
| Region / title ID | Europe — **PPSA01341** |
| Game version | **01.007.000** (1.007) |
| Emulator tested | **KytyPS5 v20261101**, Windows |
| Installer runtime | Windows with .NET Framework 4.x |

Other regions, versions, emulator builds, and native consoles are **not verified**. This is not a PS3/RPCS3 patch. KytyPS5 must support GoldHEN-style JSON game patches (`--game-patch`). The patch includes original-byte checks so a mismatched executable should not be patched blindly.

## Install with the EXE

1. Download and extract the release ZIP.
2. Close the game and KytyPS5 launcher.
3. Run **KeepSoulsInstaller.exe** and click **Browse**.
4. Select the KytyPS5 folder containing **kyty_emulator.exe**.
5. Keep **Back up this game's saves** checked.
6. Review **Enable Cheats**: this also activates any other cheats already enabled in your existing patch file.
7. Click **Install / Update patch**, reopen the launcher, and play.

The installer works offline. It merges this mod into `_Patches/PPSA01341.json`, preserves other mod entries, and optionally enables the launcher's Cheats setting. Existing graphics settings and extra command-line options are preserved. The original game executable is never modified.

Backups are stored under `KeepSoulsInstaller/install-<timestamp>` inside your selected emulator folder. The save backup covers `_SaveData/PPSA01341` when present. The installer is unsigned and does not automatically request administrator access; choose an emulator folder you can write to.

## Manual installation

If `_Patches/PPSA01341.json` does **not** already exist, create `_Patches` in the emulator folder and copy `patches/PPSA01341.json` there. Enable **Cheats** in the launcher. If you already have a patch file, use the installer to merge rather than overwriting your existing cheats.

For builds that do not read the launcher setting, the patch file can be selected through the build's `--game-patch` command-line option. This does not establish compatibility with other emulator builds.

## What changes

- Your carried souls are retained during the normal death/bloodstain transfer.
- Pending earned souls included in that transfer are retained too.
- The new bloodstain holds **zero souls**, preventing recovery of a duplicate copy.
- Souls already dropped before installation are **not** automatically restored.
- Health, damage, prices, earning souls, and spending souls are not patched.
- Other death consequences, including world/character tendency, are not intentionally changed.

## Test status and limits

**Experimental v0.1:** a gameplay tester confirmed that keeping souls after death works on the supported configuration. Five instruction-level tests also compare the original and patched transfer for zero souls, normal amounts, and pending earnings. Installer tests cover install/reinstall, merging, removal, version mismatch rejection, and save preservation.

Long play sessions, every death route, multiplayer, tutorial deaths, old bloodstains, and other builds have not been comprehensively tested. Make a save backup, test with a small known soul count, then check spending/earning and quit/reload before relying on the patch for a long playthrough. Please report unexpected behavior rather than assuming every death path is covered.

## Uninstall

Close KytyPS5, run the installer, select the same emulator folder, and click **Uninstall patch**. Other mod entries and saves are preserved. A same-name mod that existed before installation is restored to its earlier entry. Backups are kept.

Alternatively, disable this specific mod in the launcher's cheat selector and restart. Turning off all Cheats also disables your other cheats. Removing the patch does not reverse saved progress, spent souls, or tendency changes.

## Troubleshooting

- **Souls still disappear:** confirm PPSA01341 / 01.007.000, the correct emulator folder, Cheats enabled, and a full game restart. Check the log for `Successfully applied cheat: Keep carried souls on death; empty soul bloodstain`.
- **No matching module / patch rejected:** do not force the patch or reuse offsets for another version. Attach a description of your title ID, game version, emulator build and relevant patch-log lines to an issue.
- **Installer refuses an existing patch:** it may target a different game version or be malformed. It is intentionally not overwritten.
- **Flickering or low FPS:** this patch does not fix emulator graphics or performance. Any memory-protection workaround is separate and is not included here.

Do not upload save data, full game executables, game downloads, account credentials or personal paths in bug reports.

## Source and technical overview

The patch replaces a 27-byte instruction span at RVA `0x20357f` for the inspected v1.007 executable. The normal bloodstain routine totals pending plus carried souls. The replacement stores that total back into the carried-souls field and writes zero to the new bloodstain's soul field, instead of clearing carried souls. The JSON includes both original and replacement bytes.

Installer source is in [`src/Installer.cs`](src/Installer.cs); the patch is in [`patches/PPSA01341.json`](patches/PPSA01341.json). Build with [`build.ps1`](build.ps1), or run the compiled installer with `--self-test` to exercise isolated temporary fixtures without touching an installed game.

This repository distributes only the patch, installer source and documentation. No game executables, analysis images, saves or emulator binaries are included. This is an unofficial community project, not affiliated with Sony, Bluepoint, FromSoftware or the KytyPS5 developers.

## Report a problem

[Open an issue](https://github.com/vikrant-singh-au7/demons-souls-no-soul-loss/issues) with the game title ID/version, emulator build, whether the patch was reported as applied, and the death situation that failed. Search existing issues first.

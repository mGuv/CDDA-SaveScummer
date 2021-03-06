# CDDA Save Scummer

A lightweight tool that watches your CDDA save directory and makes a backup of every save.

This is kind of cheating as CDDA is meant to be permadeath; However, as someone without the time to really dedicate myself to CDDA, this helps me to take risks to learn the outcomes of things without being too heavily penalised for making mistakes. 

The aim is to only use this tool when learning and still have typical "Iron Man" saves by either disabling the tool or just not touching the backups.

**Note:** Renamed from CDDABackup as it was confused with being a back up repo of CDDA. The internal name/project is still called CDDABackup until I fix that.

## Usage

- Edit `appSettings.json` and change the value for `saveDirectory` which MUST be set to your CDDA Save directory. E.g. `C:\\CDDA Game Launcher\\cdda\\save`, this will vary based on where you installed it.
- Run the application
  - If you checked out the source, this can just be done with `dotnet run` from the `CDDABackup` project 
  directory
  - If you downloaded a built version, run the executable/equivalent for your OS
- Play CDDA!
  - As long CDDABackup is running, it will generate a backup after every save.

### Where is my CDDA Save Directory?

This will depend entirely on where you installed the game. If you installed it manually, find the path to your installation folder - or look at your CDDA shortcut to see where it goes. It is then the `save` directory in your installation folder.

If you used the CDDA Launcher, first navigate to where you installed the launcher, then go in the `cdda` folder and then `save`.

### Restoring a Save

Currently the tool does not automatically restore saves for you. To restore a save:

- FOLLOW THIS AT YOUR OWN RISK, any accidental deletion of saves/backups  is not the fault of CDDABackup
- Go to your CDDA save directory
- Look for a folder that matches the back up folder name, default is `CDDABackups`
- Go in this folder and you should see lots of `.zip` files with names that match your saves
- Copy the latest `.zip` (or whichever one you want) for your save, e.g. `MyWorld 2021-05-16 17-53.zip`
- Go up a folder, i.e. back to your CDDA save directory
- Go in to the matching save directory, e.g. `MyWorld`
- Delete everything in this directory, WARNING: will delete current save!
- Paste the `.zip` file here
- Extract the contents of this zip folder directly to this directory (i.e. `Extract Here` from WinRAR)
- It should look similar to how it did before (if it ended up in another folder, move the contents of that folder out)

## Configuration
- `saveDirectory` - The path to your CDDA save folder, also where the backups will be written.
- `backupFolderName` - The name of the folder the backups will be placed in, ensure it doesn't match one of your save names or that save will not get backed up.
- `timestampFormat` - This will get used to format the `DateTime`, overridable incase format causes issues on your OS
- `saveGracePeriodMilliseconds` - Time it takes for a save folder to stop changing before the backup is made. Lower it for more instant backups or raise it if it is making incomplete backups.
- `timeBetweenUpdatesMilliseconds` - Time it takes for the Backup to kick in and check if there is any work to do. Lower for more instant backups or raise if this application is using too much CPU.

## Direction

I wrote this very quickly to solve an immediate problem. It could do with lots of improving.

- Only write `X` number of backups per save to avoid this ballooning
- Add Restore Backup functionality so it will automatically delete the old save and restore the zipped backup
- Automatically detect where the user's CDDA save path is, rather than relying on config. 
  - This may be difficult depending on how the user has installed it and how many copies/versions of CDDA they have
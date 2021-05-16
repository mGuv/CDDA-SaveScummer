# CDDABackup

A lightweight tool that watches your CDDA save directory and makes a backup of every save.

This is kind of cheating as CDDA is meant to be permadeath; However, as someone without the time to really dedicate myself to CDDA, this helps me to take risks to learn the outcomes of things without being too heavily penalised for making mistakes. 

The aim is to only use this tool when learning and still have typical "Iron Man" saves by either disabling the tool or just not touching the backups.

## Usage

- Edit `appSettings.json` and change the value for `saveDirectory` which MUST be set to your CDDA Save directory. E.g. `C:\\CDDA Game Launcher\\cdda\\save`, this will vary based on where you installed it.
- Run the application
  - If you checked out the source, this can just be done with `dotnet run` from the `CDDABackup` project 
  directory
  - If you downloaded a built version, run the executable/equivalent for your OS

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
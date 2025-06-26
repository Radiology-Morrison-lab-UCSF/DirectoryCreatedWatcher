# Directory Created Watcher

This will watch a directory for a new directory or subdirectory to be created. When it is, it waits a preset amount of time, then executes a provided command.

See the unix-build script for how to build. Note that this is set up for linux on x64. If building for Mac or similar, you will need to change the URL to download the appropriate SDK.

You will usually want to combine this with `nohup` on linux. This lets you set it running, then exit the shell, like:

```
nohup DirectoryCreatedWatcher <arguments> &
```

To kill this after doing so, ssh back in and find the process id like so:
```
ps --user <your-user-id>
```
Then
```
kill <the process id from above>
```


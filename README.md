## Tina
A utility that watch file and print it to the console.

## What is this program?
In the Windows environment, Unity had an issue that did not print the build logs to the console. It is a program to solve this.
This utility supports Unity as well as several Windows programs (regardless of 32/64bit)

## How does it work?
Injection into the process using the EasyHook library and hooking the WriteFile WINAPI function.

## Notice
This project be strongly inspired and apopted code "EasyHook-Tutorial(https://github.com/EasyHook/EasyHook-Tutorials)"
EasyHook-Tutorial has published MIT License

# License
This project is licensed under the terms of the MIT license.

## Usage
```bash
Expected usage: Tina Watch <options>
<options> available:
  -f, --file=VALUE           The full path of the file to run.
  -w, --watch=VALUE          Specifies the file to watch for output to the
                               console.
```z
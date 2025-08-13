# Quick Sticky Note
A very simple sticky notes application stripped down to the bare minimum.

I made this because my needs are very simple:
Open a note on the desktop → type in → delete when done.
No fancy customization.
No unnecessary features.
Super simple, super easy setup.

## How it works
Right-click the desktop and use the context menu option for the .qnote file type:
<img width="519" height="425" alt="image" src="https://github.com/user-attachments/assets/f3da9100-b034-459d-bbd5-01f1fed8924c" />

This will:
* Create a .qnote file in your %LOCALAPPDATA%\QuickSticky\ folder.
* Open a sticky note window at your current cursor position.

## Closing a note
The note stays open until you click the X three times.
This safety measure avoids accidental closures.
<img width="439" height="365" alt="image" src="https://github.com/user-attachments/assets/392dfc66-5ebb-4e6a-97d2-b4183d4658c1" />

When closed, the program will:
* Delete the associated .qnote file.
* Terminate the note process/thread.

## Startup behavior
The installer sets the app to run on system startup.
On startup, it will:
* Look for existing .qnote files and reopen them.
* If none are found, the app immediately exits — nothing runs in the background unless you have a note on screen.

## Purpose
The goal is to have a quick, visible reminder for simple tasks — not to use the desktop as a calendar replacement.



# MechrevoControlCenterLanguageChanger
Language Changer(patcher) for Control Center(Mechrevo, Machenike, etc..)
======================================================
Mechrevo / XMG Control Center Language Changer v2.0
Author: ycukenta
======================================================
This utility allows you to easily patch/replace the UI language in the Control Center application with just one click, without permanently breaking the strict WindowsApps folder permissions. 

~~It is fully compatible with any laptop built on the Tongfang chassis (Mechrevo, XMG, Maibenben, Eluktronics, etc.).~~ But I'm not sure.

HOW TO USE:

1\. Extract the archive into any folder on your PC.
2\. Run "ControlCenterPatcher.exe" (it will automatically prompt for Administrator privileges).
3\. Select your desired translation from the menu. The program will automatically create a backup of your original files, apply the new language, and safely restore the system ownership (TrustedInstaller) to the folder.
4\. Restart your Control Center to enjoy the new language!

ADDING CUSTOM TRANSLATIONS:
You can easily create and use your own translations. Simply create a new folder inside the "Translations" directory, place your modified .json and .resx files there, and the patcher will automatically detect them on the next launch.

RESTORING ORIGINAL FILES:
If you need to revert the changes back to the factory defaults, simply select the "Restore Original Backup" option in the patcher's main menu.

Disclaimer: The source code of this patcher is licensed under the MIT License. Control Center original localization files (.json, .resx) are the intellectual property of their respective owners (Tongfang / Mechrevo / XMG) and are provided purely for modification and educational purposes.

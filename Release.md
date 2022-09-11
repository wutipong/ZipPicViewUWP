# Release Note

## Version 2022.09

Maintenance release. In this release, the project structure is refactored so it's more friendly to the developer. 

One major changes is the UI, which the navigation bar does not have background color anymore, and the bar does not extend into the title bar anymore as well. This is to match the visual style to the current Windows applications.

### What's Changed
* Bump SharpCompress from 0.28.3 to 0.29.0 in /MediaProvider by @dependabot in https://github.com/wutipong/ZipPicViewUWP/pull/4
* Feature/UI update by @wutipong in https://github.com/wutipong/ZipPicViewUWP/pull/5
* Fix/7zip not opening by @wutipong in https://github.com/wutipong/ZipPicViewUWP/pull/6
* refactor: move printing functionalities to the main project. by @wutipong in https://github.com/wutipong/ZipPicViewUWP/pull/7
* refactor: move MediaManager to the main project. by @wutipong in https://github.com/wutipong/ZipPicViewUWP/pull/8
* refactor: Move helpers to the main project by @wutipong in https://github.com/wutipong/ZipPicViewUWP/pull/9
* fix: change the way exception is handle. from return value to try-catch. by @wutipong in https://github.com/wutipong/ZipPicViewUWP/pull/10
* Feature/physical file optimize by @wutipong in https://github.com/wutipong/ZipPicViewUWP/pull/11

## New Contributors
* @dependabot made their first contribution in https://github.com/wutipong/ZipPicViewUWP/pull/4
* @wutipong made their first contribution in https://github.com/wutipong/ZipPicViewUWP/pull/5

**Full Changelog**: https://github.com/wutipong/ZipPicViewUWP/compare/2021.05...2022.09.0

## Version 2021.05
* Update dependencies. ZWP now requires Windows 10 1809.
* Add image filter.
* Minor update on the overall UI.

## Version 2020.11.1
* fix the viewer's image scaling options not being applied.

## Version 2020.11
* Add image viewer's scale mode option.
* Redesign the app icon.

## Version 2019.06
* Major UI design overhaul.
* Main color is now blue instead of red.
* Add an option to change between Light, Dark, and System Theme.
* Add advance through all folder option in Auto Advance.
* Major project structure change.

## Version 2019.05
* Add Drag-Drop support. The input file/folder can now be dragged into the window to open the file. 
* Image file is now properly closed after finish reading.
* The thumbnail is changed to 300x200 rectangle instead of 200x200 square.
* Minor UI adjustments.
* Source code are now being enforced by StyleCop Analyzer.

## Version 2019.04
* Fix a number of minor UI issues.
* The thumbnail images in the file list will fill the whole button.

## Version 2019.01
* Change version scheming to *year.month*. No more figuring out version number.
* Fix the printing where some images are not filling the printing area correctly.
* Add error image and dialog which displays when the program cannot open the image file.

## Version 1.2.8.0
* Add invisible image viewer control -- When an image is displaying, the whole screen area becomes a control. Tapping the area on the left makes the viewer to change to the previous one. Tap on the right, and the viewer advance to the next image. The remaining area toggles image controls visbility.

## Version 1.2.6.0
* Add "Copy to Clipboard" functionality 
* Add "Layout Option" in the print dialog
* Fix the issue where multiple "open file" dialog shows on top of each ohter.
* Fix the duplicate RAR file entries.

## Version 1.2.4.0

* Add support for more image file formats -- the supported format will be enumerated from the system's installed decoders.
* Display the original image resolution on the viewer panel.
* File names are sorted using natural sort algorithm.
* Fix the crash problem when selecting the root folder.
* Fix the 'reading content' dialog keep displaying problem when there's an error reading the folder/archive contents.

## Version 1.2.2.0

* Add Support for PDF files
* Asynchronously load folder thumbnail.

## Version 1.2.0.0

* Update new UI
* Add printing support
* Crash on certain RAR files fixed.

## Version 1.1.16.0
* Small UI Updates


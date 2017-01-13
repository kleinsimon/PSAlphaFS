# PSAlphaFS.net
.net module for Powershell implementing some features of AlphaFS, mainy for supporting long paths

CAUTION! Experimental. Please test before use.

This work was inspired by https://github.com/v2kiran/PSAlphaFS. Work in Progress, but the implemented cmdlets should work. We use it to crawl our (big) data tree and remove some temporary or old files.

By now, this module allows on-the-fly pipelineing of objects found using get-longchilditem. Thus, when searching a bigger data tree, objects are pushed in the pipe when found and can be processed using the other cmdlets. Watch-Pipeline is for watching such a crawling process, it displays live data of the objects found so far. You could do:

```
Get-LongChildItem -Recurce -Force -Include '*.jpg' -Exclude '*important*' -Path x:\ 
| Watch-Pipeline -Property Length -Byte
| Copy-LongItem -Destination B:\Backup -PassOriginal -LogVariable log
| Remove-LongItem -Whatif -LogVariable log
```
Implemented
-----------
* Get-LongChildItem
* Get-LongItem
* Copy-LongItem
* Move-LongItem
* Remove-LongItem
* Watch-Pipeline

Installation
------------
Just copy the Folder PSAlphaFSnet to one of the following locations:
* %UserProfile%\Documents\WindowsPowerShell\Modules
* %Windir%\System32\WindowsPowerShell\v1.0\Modules
* %ProgramFiles%\WindowsPowerShell\Modules

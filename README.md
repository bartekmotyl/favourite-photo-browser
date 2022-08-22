# Favourite Photo Browser
A simple USB/network photo browser with support for caching thumbnails and favourites to local drive

![Screenshot](doc/favourite-photo-browser-screenshot.png)

# Goals

The main reason why this program was created was to satify the following two needs: 
- be able to browse photos located on [NAS](https://en.wikipedia.org/wiki/Network-attached_storage) (or other location that may be slow to read) from my laptop 
- be able to mark favourite photos somehow, *without a need to save anything on the remote drive* (and definetelly without a need to update these photos e.g. to modify [EXIF](https://en.wikipedia.org/wiki/Exif) metadata)

There are already some tools that make the first point possible, but I haven't found a single tool that would satisfy these both needs. 

# Solution 

This program uses a local database (`photos.db`), located in root program directory) to store thumbnails of browsed photos (to improve browsing experience next time you visit the same folder). When browsing, photo can be marked as favourite (use `F` key) and this information is stored in local database along with the thumbnail. 
Finally, it is possible to export (copy to clipboard) paths of all photos marked as favourite in currently opened folder.     

# Implemnentation 

Program is implemented in C# / .Net 6 with UI implemnted in [Avalonia](https://avaloniaui.net/). This means it should work on many platforms, including: 
- Windows 
- Mac OS
- Linux

So far I have only tested Windows, looking for feedback of testing it on other platforms. 

# Release

The current versdion is an early alpha / MVP version, with basic UI but with both main features (browsing and marking favourites) already working.    

At the moment there is no distribution packege available to download (should be available soon though!), you would need to compile the tool yourself. 
On Windows the easiest way is to use [Visual Studio 2022 Community Edition](https://visualstudio.microsoft.com/vs/community/) which can be downloaded for free from Microsoft.
On Mac or Linux one may use [JetBrains Rider](https://www.jetbrains.com/rider/) (paid software) or compile using command line tools.    

# Contribution 

If you would like to help to make the program better (and especailly if you have experience with C# programming), please contact me! 


# Getting Started (Windows)

### Setup

* For a 64-bit machine running Windows 7 or newer,

* Download labs\_coordinate\_pictures\_winx64.zip

* Unzip to a writable directory

* Run vcredist\_x64.exe, which is Microsoft's C runtime, needed for viewing webp images

* Run labs\_coordinate\_pictures.exe; sorting files, browsing images, and sorting images into directories will now work

### Enable image editing

* Install Python 2

* Install the Python 2 package "Pillow"

    * With Python 2.7.9 and later, this can be done with the following,

    * Open an administrator command prompt and cd to C:\Python27\Scripts

    * Run pip install --upgrade pip

    * Run pip install pillow

* Run labs\_coordinate\_pictures.exe

* From the Options menu choose "set Python location", and set the location of python.exe, which is typically under C:\Python27

### Add context menu (optional)

This will let you conveniently start Coordinate Pictures by adding an item to the context menu that appears when right-clicking a directory.

![Screenshot showing context menu](https://github.com/downpoured/labs_coordinate_pictures/blob/master/doc/getstarted_context.png)

* Open Regedit

* Go to HKEY\_CLASSES\_ROOT\Folder\shell

* Create a new key named `RunCoordinatePictures`

* Under this key

    * Create a string value named "MUIVerb" with contents `Run CoordinatePictures`
    
    * Create a string value named "Icon" with contents set to `C:\path\to\labs_coordinate_pictures.exe`

    * Create a key named "Command" with (Default) contents set to `"C:\path\to\labs_coordinate_pictures.exe" "%1"`

### Other notes

Requires [.NET 4.5](https://www.microsoft.com/en-us/download/details.aspx?id=30653), which should be included in Windows 7 and newer as long as updates are installed.

The Options menu can be used to choose a location for the default image editor, jpeg crop tool, comparison tool, and so on.

Logs are saved to a file named log.txt.

### Dev notes

* File handles are held as briefly as possible, so that the current file can be easily edited or renamed.

* Listens to file events in the current directory, and so (unlike Windows image preview) will automatically refresh when new files are added to the current directory.

* Press Ctrl+T to run unit tests.

* img\_convert\_resize.py avoids writing temporary images by passing images over pipes where possible.

* in most software, converting an image important details in red, like red text, to jpg will create visible artifacts due to chroma subsampling. In img\_convert\_resize.py, add the flag `jpgHighQualityChromaSampling=True` to keep higher image quality, at the expense of larger filesize.

* img\_utils.py contains methods that might be useful for working with EXIF data, see `readThumbnails()`, `readExifCreationTime()`, `removeResolutionTags()`, and `removeAllExifTags()`.





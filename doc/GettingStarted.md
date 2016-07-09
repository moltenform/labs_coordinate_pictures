
# Getting Started (Windows)

### Setup

* For a 64-bit machine running Windows 7 or newer,

* Download labs\_coordinate\_pictures\_winx64.zip

* Unzip to a writable directory

* Run vcredist\_x64.exe, which will install Microsoft's C runtime, required for viewing webp images.

* Run labs\_coordinate\_pictures.exe. Sorting files, browsing images, and sorting images into directories will work.

### Enable image editing

* Install Python 2

* Install the Python 2 package "Pillow"

   * With Python 2.7.9, this can be done with the following,

   * Open an administrator command prompt and cd to C:\Python27\Scripts

   * Run pip install --upgrade pip

   * Run pip install pillow

* Run labs\_coordinate\_pictures.exe, from the Options menu choose "set Python location", and set the location of python.exe, which is typically under C:\Python27.

### Other notes

Requires [.NET 4.5](https://www.microsoft.com/en-us/download/details.aspx?id=30653), which should be included in Windows 7 and newer as long as updates are installed.

In labs\_coordinate\_pictures.exe, the Options menu can be used to choose a location for the default image editor, comparison tool, and so on.

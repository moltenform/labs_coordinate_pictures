
## Resize images while keeping exif metadata

Images saved in the JPEG image format are able to store metadata known as EXIF data. For example, most digital cameras create JPEG files with EXIF data that describes the camera model, date taken, flash mode, exposure time, f-stop, and more. 

By default, CoordinatePictures resizes images and keeps the 18 most useful fields of exif metadata. It stores the original filename in EXIF metadata under the 'Copyright' field, as I have found that it is sometimes useful to see the original camera-given filename.

### Walkthrough

* Open labs\_coordinate\_pictures.

* From the 'Pictures' menu, choose 'Assign pictures to categories...' 

* Click 'Browse...', choose a directory containing images and click OK.

This will open the gallery, see [Sorting Images](SortingImages.md) for more features that can be performed from here.

* Press the arrow keys to move to the next or previous image.

* As shown on the left,

    * Press Shift+Q to indicate that the current image should be resized to a height of 288 pixels.
    
    * Press Shift+W to indicate that the current image should be resized to a height of 432 pixels.
    
    * Press Shift+E to indicate that the current image should be resized to a height of 576 pixels.
    
    * Press Shift+1 to indicate that the current image should be resized to a height of 720 pixels.
    
    * Press Shift+2...
    
    * Press Shift+9 to indicate that the current image should be resized to a height of 1728 pixels.
    
    * Press Shift+P to indicate that the current image should be resized to a height of 1872 pixels.    
    
    * Press Shift+0 to indicate that the current image should be kept at full size.
    
* Press Ctrl+Z to undo marking an image to be resized.

* When all files are marked, we'll see the message 'looks done'.

* Close CoordinatePictures.

* Open ben\_python\_img/img\_resize\_keep\_exif.py in a text editor or code editor.

    * At the end of this file, look for the line beginning `dir =` and enter the full path to the directory containing images.
    
    * Uncomment the line beginning `# resizeAllAndKeepExif(dir, recurse` by deleting the `# ` characters.
    
    * Comment-out the line beginning `cleanup(dir, recurse)` by typing `# ` before the line if it is not already present.
    
    * Save changes to this file.
    
* Run ben\_python\_img/img\_resize\_keep\_exif.py

* Verify that the images were created as expected. A new resized image is created beside each file (except for the images told to be kept at full size).

* Open ben\_python\_img/img\_resize\_keep\_exif.py in a text editor or code editor

    * Comment-out the line beginning `# resizeAllAndKeepExif(dir, recurse` by typing `# ` before the line.
    
    * Uncomment the line beginning `# cleanup(dir, recurse)` by deleting the `# ` characters.
    
    * Save changes to this file.
    
* Run ben\_python\_img/img\_resize\_keep\_exif.py

* Now we're done, only the newest images remain.

### Details

* Original filenames will be saved into the EXIF field 'Copyright'. 

* The EXIF fields that will be saved are:

    * DateTimeOriginal

    * CreateDate (DateTimeDigitized)

    * Make

    * Model

    * LensInfo

    * FNumber

    * ExposureTime

    * ISO  (ISOSpeedRatings)

    * ExposureCompensation  (ExposureBiasValue)

    * FocalLength

    * ApertureValue

    * MaxApertureValue

    * MeteringMode

    * Flash

    * FlashEnergy

    * FocalLengthIn35mmFormat  (FocalLengthIn35mmFilm)

    * DigitalZoomRatio
    
    * To add to this list, or to configure so that all tags are saved, see ben\_python\_img/img\_utils.py and `transferMostUsefulExifTags`.
    
* JPEG quality is set to 94 by default (where 100 is the maximum). To change, open img\_convert\_resize.py and change the line that states `defaultJpgQuality = 94`.



# labs\_coordinate\_pictures

Tools I wrote back in 2014, released under the GPL license. 

* [Sort images into categories](https://moltenform.com/page/labs-coordinate-pictures/doc/sorting-images.html)  

* [Edit and resize images](https://moltenform.com/page/labs-coordinate-pictures/doc/modifying-images.html)  

* [Rename images](https://moltenform.com/page/labs-coordinate-pictures/doc/renaming-images.html)  

* [Resize images while keeping exif metadata](https://moltenform.com/page/labs-coordinate-pictures/doc/keeping-metadata.html)  

* Search for [differences in two similar folders](https://moltenform.com/page/labs-coordinate-pictures/doc/search-differences.html), [duplicate files](https://moltenform.com/page/labs-coordinate-pictures/doc/search-duplicates.html), and [duplicate files across two folders](https://moltenform.com/page/labs-coordinate-pictures/doc/search-duplicates-two.html)

* [Sync files from one folder to another](https://moltenform.com/page/labs-coordinate-pictures/doc/syncing-files.html)  

* [Set up and install](https://moltenform.com/page/labs-coordinate-pictures/doc/download-and-setup.html)  

<a href="#">![Screenshot](https://moltenform.com/page/labs-coordinate-pictures/doc/modifying-images-menu.png)</a>

Also,

* You can specify `jpgHighQualityChromaSampling=True` in img\_convert\_resize.py to save a higher quality jpg file that will better preserve certain details, like red text

* Use Edit->Set Rotation Tag to change or remove the exif rotation marker on a jpg image

* Use Edit->Convert Many Images for batch conversion/resize

* File handles are released quickly; the currently viewed file can be edited and renamed

* img\_convert\_resize.py will pass image data over pipes; it can convert from any image format into a webp without writing a temporary png file

* img\_utils.py contains additional useful methods for working with exif data like see `readThumbnails()`, `readExifCreationTime()`, `removeResolutionTags()`, and `removeAllExifTags()`

* Can be added to Windows context menu, see instructions at the end of the page [here](https://moltenform.com/page/labs-coordinate-pictures/doc/download-and-setup.html)

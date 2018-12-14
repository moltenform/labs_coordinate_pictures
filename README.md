# labs\_coordinate\_pictures

Tools I wrote back in 2014, released under the GPL license. 

* [Sort images into categories](https://moltenjs.com/page/labs-coordinate-pictures/doc/sorting_images.html)  

* [Edit and resize images](https://moltenjs.com/page/labs-coordinate-pictures/doc/modifying_images.html)  

* [Rename images](https://moltenjs.com/page/labs-coordinate-pictures/doc/renaming_images.html)  

* [Resize images while keeping exif metadata](https://moltenjs.com/page/labs-coordinate-pictures/doc/keeping_metadata.html)  

* Search for [differences in two similar folders](https://moltenjs.com/page/labs-coordinate-pictures/doc/search_differences.html), [duplicate files](https://moltenjs.com/page/labs-coordinate-pictures/doc/search_duplicates.html), and [duplicate files across two folders](https://moltenjs.com/page/labs-coordinate-pictures/doc/search_duplicates_two.html)

* [Sync files from one folder to another](https://moltenjs.com/page/labs-coordinate-pictures/doc/syncing_files.html)  

* [Set up and install](https://moltenjs.com/page/labs-coordinate-pictures/doc/download_and_setup.html)  

<a href="#">![Screenshot](https://moltenjs.com/page/labs-coordinate-pictures/doc/modifying-images-menu.png)</a>

Also,

* You can specify `jpgHighQualityChromaSampling=True` in img\_convert\_resize.py to save a higher quality jpg file that will better preserve certain details, like red text

* Use Edit->Set Rotation Tag to change or remove the exif rotation marker on a jpg image

* Use Edit->Convert Many Images for batch conversion/resize

* File handles are released quickly; the currently viewed file can be edited and renamed

* img\_convert\_resize.py will pass image data over pipes; it can convert from any image format into a webp without writing a temporary png file

* img\_utils.py contains additional useful methods for working with exif data like see `readThumbnails()`, `readExifCreationTime()`, `removeResolutionTags()`, and `removeAllExifTags()`

* Can be added to Windows context menu, see instructions at the end of the page [here](https://moltenjs.com/page/labs-coordinate-pictures/doc/download_and_setup.html)

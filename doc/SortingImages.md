
[1) Browse, resize, convert, and optimize images](#browse)

[2) Walkthrough, sort images into categories](#part1)

[3) Walkthrough, reduce file size of images](#part2)

[4) Walkthrough, rename images](#part3)

[5) List of Features](#listFeatures)

## 1) Browse, resize, convert, and optimize images <a id="browse"></a>

* Open labs\_coordinate\_pictures.

* From the 'Pictures' menu, choose 'Assign pictures to categories...' 

* Click 'Browse...', choose a directory containing images and click OK.

This will open the gallery. Some common actions:

* Press the arrow keys to move to the next or previous image.

* Press the Page Up/Page Down keys to skip ahead several images.

* To resize or convert an image, from the Edit menu, choose 'Convert/resize image...'

* To losslessly optimize jpg images, from the Edit menu, choose 'Save space by losslessly optimizing jpg...'

* To edit the current image, from the Edit menu, choose 'Edit image'.

* Press the Delete key to delete the current image (can be undone with Ctrl+Z).

* Press the h key to rename the current image (can be undone with Ctrl+Z).



## 2) Walkthrough, sort images into categories <a id="part1"></a>

I'll explain this with a real-world example. Let's say I've been downloading many images from the internet, about different topics. All of these images went into the default 'Downloads' directory and most have unhelpful filenames like "Cl0-ZobWgAAzl9i.jpg". How should I quickly organize these images?

I'll set up three subdirectories, 'drawings', 'photographs', and 'other'. At the end of this process, I'll have all the images placed into one of the three subdirectories, with reduced file sizes, and with better filenames.

* Open labs\_coordinate\_pictures.

* From the Pictures menu, choose 'Assign pictures to categories...' 

* Click 'Browse...', choose the directory containing the unorganized images and click OK.

* From the Categories menu, choose 'Edit Categories...' 

* Enter 'D/drawings/drawings|P/photos/photos|O/other/other' and click OK.

![Screenshot](https://github.com/downpoured/labs_coordinate_pictures/blob/master/doc/sortim_02_01.png)

We can see that the text on the left has been updated. From now on, we can press Shift+D to assign the current image into the 'drawing' category, Shift+P to assign the current image into the 'photos' category, and Shift+O to assign the current image into the 'others' category. (We can also press Ctrl+Z to undo any assignment).

* I'll press Shift+D because the current image is a drawing.

* I'll then press Delete, Shift+D, Shift+P, or Shift+O for every image. When done, I'll see:

![Screenshot](https://github.com/downpoured/labs_coordinate_pictures/blob/master/doc/sortim_03_01.png)

* Then, from the Categories menu, I'll choose Finish Categorizing, which moves the images into subdirectories, 'drawings', 'photographs', and 'other'.

All we've done is moved images into subdirectories, but we've done it quickly using keyboard shortcuts. 

(Behind the scenes, pressing Shift+D to assign the 'drawing' category will just append '\_\_MARKAS\_\_drawing' to the filename. Finish Categorizing looks for files with this type of name and moves them to the desired subdirectory). 



## 3) Walkthrough, reduce file size of images <a id="part2"></a>

The next step is to reduce file sizes.

* Open labs\_coordinate\_pictures.

* From the Pictures menu, choose 'Resize images...'

* Click 'Browse...', choose the directory containing the unorganized images and click OK.

Let's use lossless optimization, making file sizes smaller with no change in image quality.

* Press Ctrl+1 to convert png images to lossless webp.

* Press Ctrl+4 to mozjpeg to losslessly optimize jpg files.

We'll now step through each file and look at the file size to see if it is acceptible. We'll then check off the file, pressing Shift+A to give the image the 'size is good' category.

* Press Ctrl+2 to automatically check off small files <50kb that aren't worth our effort.

* To edit the current image, press Ctrl+E.

* To crop a jpg image losslessly, press Ctrl+Shift+X.

* If the image is too big, it can be resized by pressing Ctrl+[.

* If a file is a png or webp, but would probably be stored more efficiently as a jpg,

    * Press Ctrl+Shift+[, which will create several jpg images at different qualities.

    * Use arrow keys to browse through the resulting images.

    * When at the the image that has the best quality at a reasonable filesize, press Ctrl+] to delete the other temporary jpgs that were created.
	
* If a file is a reasonable size, press Shift+A to check it off.
	
* When all files are marked, we'll see the message 'looks done'.

* Then, from the Categories menu, we'll choose Finish Categorizing, which unchecks all images.



## 4) Walkthrough, rename images <a id="part3"></a>

The final step is to assign better filenames.

* Open labs\_coordinate\_pictures.

* From the Pictures menu, choose 'Resize images...' 

* Click 'Browse...', choose the directory containing the unorganized images and click OK.

* From the Rename menu, click 'Add numbered prefix'.

This will add a prefix with a number like ([0010]) to every filename. Why is this useful? It locks the order of images in place, such that we can now freely rename images without worrying about sort order. Without this, it would be cumbersome to rename every image in a directory because we would keep encountering images that we had already renamed. 

* Press h to rename the current image. (Conveniently, the prefix isn't shown and we don't have to be concerned about it, we can type in any new name and don't need to include the prefix).

* Press left/right arrow keys to browse images.

* Making sure each image has a reasonable name.

* From the Rename menu, click 'Remove numbered prefix', unless the original sort order of the images needs to be kept for some reason.

We are now complete; we have all the images placed into subdirectories, with reduced file sizes, and with better filenames.



## 5) List of features <a id="listFeatures"></a>

![Screenshot](https://github.com/downpoured/labs_coordinate_pictures/blob/master/doc/sortim_01_01.png)

In the screenshot above, the text s(452x452) means that the current image is 452 by 452 pixels, and has been resized down to fit the current dimensions of the window.

* Ctrl+Z to Undo deleting, moving, or renaming a file.

* Ctrl+Y to Redo.

* For large images, Ctrl+Click on the image to zoom in.

* Left, Right to go to the previous/next image.

* Page Up, Page Down to skip forwards/backwards.

* Home, End to go to the first/last image.

* Delete to delete the current image.

* h to rename the current image.

* Ctrl+Shift+H to replace in filename.

* Ctrl+W to show image in OS.

* Ctrl+C to copy path.

* Ctrl+E to edit image.

* Ctrl+Shift+E to edit in alt editor (set by Options->Set alt image editor location...)

* Ctrl+Shift+X to crop/rotate current image.

* Ctrl+[ to resize and/or convert current image.

* Ctrl+Shift+[ to convert current image to jpg.

* Ctrl+] to then choose the best jpg.

* Ctrl+1 to convert png to webp lossless (if the corresponding webp file is smaller).

    * runs `cwebp -lossless -z 9 -m 6 -q 100` (setting a high -q when making a lossless file means that more time is taken to generate a smaller file)
	
* Ctrl+4 to losslessly optimize jpg and remove thumbnails.

    * runs `jpegtran -optimise -progressive -copy all` (which optimizes jpg but keeps exif data).

    * runs `exiftool -ifd1:all= -PreviewImage= -overwrite_original` (which strips jpg thumbnails but keeps other metadata).

* Ctrl+3 to add numbered prefix (as described above).

* Ctrl+Shift+3 to remove numbered prefix (as described above).

